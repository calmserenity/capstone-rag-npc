# Capstone Report: State-Aware RAG NPC In A Procedural Unity Puzzle

## Abstract

This project implements a Unity garden puzzle in which Red, a rabbit, follows a
procedurally selected chain of riddles and generated logic puzzles to find Blue. Rock is a state-aware NPC
that receives typed player questions and a complete gameplay snapshot through a
Flask API. The backend retrieves focused passages from a curated knowledge base,
builds a persona- and spoiler-aware prompt, and can use Gemini to generate a
short in-character response. The verified capstone vertical slice contains a
three-hint procedural chain, unique-solution ordering puzzles, clue-point economy, chat UI, failure handling,
progress HUD, and Blue reveal/end state.

## Problem And Objectives

Static game hints cannot adapt to a procedurally generated puzzle state. A
general-purpose language model can adapt linguistically, but without grounded
context it may invent rules, reveal spoilers, or refer to the wrong generated
target. The project therefore combines retrieval-augmented generation with an
authoritative Unity-owned `GameState`.

The objectives were to:

- Create a playable Unity garden with movement and object interaction.
- Generate a solvable hint sequence from valid scene locations each run.
- Let Rock answer typed questions using retrieved project knowledge.
- Ground every request in the current procedural puzzle state.
- Enforce a clue-point cost and spoiler-control rules.
- Handle backend failure and delay without breaking play.
- Prepare repeatable backend and RAGAS evaluation workflows.

## Implemented Scope

The completed vertical slice follows the three-hint design in the implementation
notes and puzzle knowledge base. Three unique enabled interaction targets are
selected from the current scene. Reaching the active target opens a generated
ordering puzzle and does not itself advance progress. Each successful puzzle
grants a clue point and exposes the next riddle. Completing
the third target sets `blue_found`, reveals Blue near the garden gate, and shows
the completion banner.

The ten-puzzle, outer/inner-garden description in `game-concept.md` is an
expansion design. It is not presented as part of the verified vertical slice.

## Architecture

Unity owns gameplay and remains the source of truth. It sends a stateless request
containing the player question and a complete `GameState`. The Flask backend
validates the payload, constructs a retrieval query from both the question and
live state, retrieves up to three knowledge chunks, builds Rock's prompt, and
returns a structured response.

```text
Unity interaction and UI
  -> POST /chat with player_query + GameState
  -> state-aware FAISS retrieval (keyword fallback)
  -> Rock persona/spoiler/clue prompt
  -> Gemini generation (in-character failure fallback)
  -> npc_response + contexts + clue contract + latency
  -> Unity response display and clue-point update
```

The backend does not persist player progress. This keeps it stateless and avoids
disagreement between server memory and the procedural state in Unity.

## Procedural Puzzle Design

`PuzzleManager` discovers valid `GardenInteractable` targets and maps their
stable IDs to riddles. It filters null or unsupported targets, removes duplicate
IDs, randomizes candidates, and selects the configured number of hints. Because
the puzzle is a linear chain rather than a navigation graph, solvability is
validated by construction: every selected node is an existing enabled
interactable, and every node has a known riddle.

At each location, Unity generates a hidden order of three to five garden symbols,
derives randomized `before` and `immediately before` constraints, and shuffles the
visible symbols. It enumerates all possible permutations (at most 120) and accepts
the puzzle only when exactly one ordering satisfies every constraint. BFS is not
appropriate because this is a bounded constraint-permutation problem rather than
a pathfinding graph; exhaustive validation directly proves uniqueness.

Runtime state includes the full hint sequence, current index and riddle, found
locations, possible locations, player position, clue points, questions asked,
and completion flag. It also includes the active puzzle's visible symbols, rules,
latest attempt, attempt count, and solved/active flags. The hidden solution is
absent from the serializable contract. This same state drives the UI and RAG prompt.

## RAG Design

The knowledge base is divided into focused lore, mechanics, NPC, and puzzle
documents. Documents are split into bounded paragraph-oriented chunks and can be
embedded with `gemini-embedding-001`. Normalized vectors and chunk metadata are
stored in FAISS. At runtime, the player question is enriched with the current
riddle, hint index, area, objective, clue points, found locations, and completion
state before retrieval. Exact keyword matches provide precision and FAISS
embeddings provide semantic recall; results are de-duplicated. If the FAISS index
or embedding call is unavailable, keyword retrieval keeps the NPC usable.

The generation prompt requires Rock to remain sleepy, warm, brief, grounded,
question-first, and non-spoiling. Factual and descriptive claims must come from
retrieved passages or current state, and active target names are forbidden. With
zero clue points, Rock must not provide imagery that
reveals the answer. Unity blocks paid questions when no points remain, while the
backend repeats the restriction as a defense-in-depth prompt rule.

## Verification Results

### Automated Backend Tests

The Dockerized suite passes 20 tests. Coverage includes document loading,
chunking, embedding response handling, FAISS persistence/search, retrieval and
fallback, state-aware retrieval-query construction, prompt rules, generator
fallback, index rebuilding, request validation, response schema, and clue-point
contract behavior.

Six Unity EditMode tests pass. They validate exactly one solution across 3-, 4-,
and 5-symbol puzzles and 75 seeded generations, verify randomization, prove that
the public JSON omits the hidden solution, and demonstrate that reaching a target
does not advance progress while an incorrect attempt remains gated and the valid
solution advances it.

### Unity Runtime

Unity 6000.4.5f1 compiled with no errors. A bounded runtime verification produced
the sequence `bench -> garden_gate -> bird_bath`. Three correct discoveries
advanced the index from 0 to 3, recorded all three locations, set
`blue_found=true`, and created Blue. A wrong-target check was separately rejected
without changing progress.

### Unity-Backend Integration

A local-only Flask round trip verified request serialization, procedural state,
HTTP transport, JSON parsing, response display, and clue accounting without
external data transfer. The displayed response returned control to the player,
reduced clue points from 3 to 2, and incremented questions asked from 0 to 1.

### Failure And Delay

With Gemini disabled, Unity displayed the backend's in-character fallback and
re-enabled input. A separate five-second delayed backend confirmed that the UI
immediately displayed `Rock is listening...`, disabled input during the request,
did not spend a point early, then displayed the response and spent exactly one
point after success.

### Clean Startup

The Docker container and network were removed and recreated from the local image.
`/health` returned `ok`, and an invalid empty chat request returned HTTP 400. The
Unity Editor was restarted from disk, reconnected to MCP, remained outside Play
Mode, and reported zero compilation errors.

## Evaluation Workflow

`evaluation/datasets/garden_rag_cases.json` contains sixteen cases covering the core
riddles, six generated logic-puzzle states, incorrect attempts, a solved state,
no clue points, and completed gameplay.
`evaluation/scripts/run_ragas_evaluation.py` collects live backend responses and
computes context precision, faithfulness, and answer relevancy with Gemini-backed
RAGAS metrics. NPC generation defaults to Gemini 3.1 Flash-Lite while the independent
judge defaults to Gemini 3.5 Flash; the evaluator rejects matching model names. It
writes raw outputs, per-case CSV scores, and a JSON summary recording both models.

The previously approved live evaluation rebuilt the FAISS index
from 6 documents into 13 embedded chunks. A baseline RAGAS run exposed weak
faithfulness and relevance. After restructuring the tip knowledge, adding hybrid
retrieval, tightening the prompt, and including authoritative game state in the
evaluation grounding contexts, that ten-case pre-ordering-puzzle run produced context precision
`0.508333`, faithfulness `0.475000`, and answer relevancy `0.405503`. Faithfulness
improved from `0.100000` and answer relevancy from `0.122629`. Every tuned case
retrieved expected topic terms, responses stayed below 45 words, and active
riddles did not leak exact target names. These scores are retained as the baseline
for the earlier riddle-only implementation. The expanded 16-case independent-judge
workflow is implemented but its scoring run is intentionally deferred. The runner
persists raw responses and partial scores, retries temporary provider failures, and
refuses to use the same model for generation and judging.

## Limitations And Future Work

- Optionally run and report the expanded 16-case independent-judge RAGAS evaluation when judge quota is available.
- Expand the verified three-hint slice toward the ten-puzzle/two-area concept if a larger game is desired.
- Add durable save/load if runs must persist across application restarts.
- Produce the final narrated demo recording and submission package.

## Conclusion

The project demonstrates the central capstone claim: a procedural Unity puzzle
can provide an LLM NPC with authoritative live state, retrieve focused game
knowledge, enforce persona and spoiler constraints, and return guidance through
a resilient in-game chat loop. The procedural gameplay, Unity/backend boundary,
state contract, generated unique-solution puzzles, progression gate, clue economy,
failure behavior, delayed-response behavior, and end state are implemented and
verified locally. The earlier live FAISS/Gemini/RAGAS evidence supports the RAG
pipeline; the expanded procedural-state evaluation implementation is complete,
while new scoring is intentionally deferred.

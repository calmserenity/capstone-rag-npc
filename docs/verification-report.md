# Verification Report

This report separates implemented evidence from work that still requires an
external service or final submission activity.

## Scope Decision

The current capstone vertical slice is the procedurally generated three-hint
riddle chain. This scope is defined consistently by the implementation notes,
weekly build plan, game-state examples, and knowledge-base puzzle rules.

The ten-puzzle, outer/inner-garden progression in `game-concept.md` is retained
as the expansion design. It conflicts with the three-hint completion rule and
cannot simultaneously describe the same run. The README now makes this boundary
explicit instead of silently treating both as implemented.

## Verified Requirements

| Requirement | Evidence | Status |
| --- | --- | --- |
| Playable garden, Red, Rock, collisions, interactions | `GeneratedIsoGardenScene.unity`; `Assets/Screenshots/final-runtime-audit.png` | Verified |
| Procedural hint content | Runtime sequence `bench -> garden_gate -> bird_bath` | Verified |
| Procedural logic puzzles | `Assets/Screenshots/ordering-puzzle-runtime-ui.png`; randomized visible rules | Verified |
| Unique-solution validation | 75 seeded generations across 3-5 symbols; exhaustive solution count exactly 1 | Verified |
| Puzzle-gated progression | Reaching target kept index 0; wrong attempt kept index 0; valid solution advanced | Verified |
| Hidden-solution boundary | Unity JSON runtime inspection returned `containsSolution=False` | Verified |
| Solvable three-hint chain | Targets are selected uniquely from existing enabled interactables | Verified |
| State tracking | Runtime result: index 3, found 3, `blue_found=true` | Verified |
| Riddle/progress UI | `Assets/Screenshots/rag-hud-initial.png` | Verified |
| Opening instructions | `Assets/Screenshots/opening-instructions-popup.png` | Verified |
| Blue reveal/end state | `Assets/Screenshots/rag-puzzle-complete-blue-v2.png` | Verified |
| Unity chat HTTP integration | `Assets/Screenshots/rock-chat-offline-roundtrip.png` | Verified locally |
| Clue spending | Local round trip changed clue points 3 to 2 and questions 0 to 1 | Verified |
| Backend RAG components | Document, chunk, embedding, FAISS, hybrid retrieval, prompt, generator tests | Verified |
| Backend API contract | Docker test suite: 33 passing | Verified |
| Unity procedural-puzzle tests | EditMode suite: 9 passing | Verified |
| Failure fallback | Unity local-only round trip displayed generator fallback and recovered input | Verified |
| Slow response handling | Five-second local response kept loading state/input lock, then recovered and spent one point | Verified |
| Clean local startup | Docker container/network recreated; health OK; Unity restarted, connected, idle, compile-clean | Verified |
| Live FAISS index | 6 documents, 13 chunks, 13 Gemini embeddings/vectors | Verified |
| Live Unity-Gemini RAG | `Assets/Screenshots/rock-chat-live-gemini-faiss.png`; clue 3 to 2 | Verified |
| Original RAGAS evaluation | Ten valid paced riddle cases; reports written under `evaluation/reports/` | Verified |
| Expanded independent-judge evaluation | Six generated-puzzle cases added; runner, model-separation guard, retries, and resume support implemented; scoring intentionally deferred | Implemented, not run |
| Unity test runners | EditMode and PlayMode root suites passed; zero console errors/warnings | Verified |
| Final Play Mode exit | Entered Play Mode, captured `final-runtime-audit.png`, exited to idle, console clean | Verified |

## RAGAS Evaluation And Tuning

The approved live evaluation used ten cases across eight active riddles, zero
clue points, and completed gameplay. Gemini free-tier judging was serialized one
case per 65-second quota window. The final tuned scores are:

| Metric | Baseline | Tuned |
| --- | ---: | ---: |
| Context precision | 0.558333 | 0.508333 |
| Faithfulness | 0.100000 | 0.475000 |
| Answer relevancy | 0.122629 | 0.405503 |

Weak-case analysis found split Markdown table rows, semantic-only retrieval that
missed exact riddle terms, unsupported decorative language, and an evaluation
dataset that omitted authoritative game state from the model's grounding
contexts. The tuning pass restructured the tip knowledge, combined keyword
precision with FAISS semantic recall, strengthened evidence/no-spoiler prompt
rules, and included game state in the evaluated grounding context.

All ten tuned live cases retrieved at least one expected term, all responses
were below 45 words, and no active-riddle response named its exact target.
Faithfulness and answer relevancy improved substantially. Context precision is
not directly comparable because the tuned evaluation correctly includes the
additional authoritative state context; it is reported transparently rather
than optimized by removing inputs the model actually used.

## Pending Finalization

- Optional execution of the expanded 16-case Gemini-backed RAGAS run when judge quota is available.
- Final narrated gameplay recording and external submission upload remain human
  submission activities; they do not affect the verified executable system.

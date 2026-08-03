# Architecture

The project retains the proposal's Unity -> Python -> Vector DB -> LLM architecture.

```text
Unity Client
  | generate hidden ordering + randomized visible constraints
  | exhaustively validate exactly one solution
  | POST /chat: player query + complete visible GameState
  v
Flask Backend (stateless)
  | hybrid state-aware retrieval + deterministic progressive puzzle hints
  v
FAISS Vector Index + Knowledge Base
  | persona + retrieved context + query + visible game/puzzle state
  v
Gemini 3.1 Flash-Lite
  | structured JSON response
  v
Unity NPC Dialogue UI
```

## Responsibilities

- Unity owns gameplay, puzzle generation, hidden solutions, validation, progress, player input, and rendering.
- Flask validates each independent request and retrieves relevant passages. It calls the generator for normal dialogue and uses deterministic visible-rule reasoning for progressive ordering-puzzle hints.
- FAISS supplies semantic recall while keyword matching provides a local precision/fallback path.
- The backend stores no session progress. Unity sends an authoritative snapshot with every request.
- Docker packages the Python runtime; Unity remains the interactive client.

## Procedural Puzzle Generation

Each run selects three unique enabled garden targets. Reaching the target described
by the current riddle opens a symbol-ordering puzzle rather than advancing the hint.
Difficulty increases from three symbols to four and then five.

Unity generates the hidden answer first, derives randomized `before` and
`immediately_before` constraints from that answer, and shuffles the visible symbols
and rule order. It enumerates every possible permutation and accepts the puzzle only
when exactly one ordering satisfies every constraint. An incorrect attempt leaves
the puzzle active; a valid ordering unlocks the next riddle and awards a clue point.

Breadth-First Search is not used because this is a constraint-permutation problem,
not a spatial navigation graph. With at most five symbols there are at most
`5! = 120` candidate arrangements. Exhaustive enumeration is simpler and provides
the directly relevant proof: the number of satisfying arrangements is exactly one.

## State Synchronization And Safety

The active puzzle state contains its identifier, type, location, visible symbols,
visible constraints, current draft, latest submitted attempt, hint count, attempt
count, and active/solved flags. The serializable contract deliberately has no
solution field; the hidden answer never leaves Unity.

Retrieval enriches the player question with the current riddle, hint index, clue
points, discovered locations, completion state, and visible procedural-puzzle state.
For puzzle help, the backend enumerates the small visible constraint set. The first
request reveals the first item. Later requests cumulatively identify additional
incorrect positions in the latest submitted answer without naming replacements or
stating the complete order. Unity owns the hint counter, resets positional progress
after a new submission, and spends a clue point only when new guidance is delivered.

## Evaluation Architecture

RAGAS is an offline evaluation client rather than part of the runtime sequence. It
submits 16 controlled states to the same `/chat` endpoint and measures context
precision, faithfulness, and answer relevancy. NPC generation defaults to Gemini
3.1 Flash-Lite. The independent LLM-as-a-Judge defaults to Gemini 3.5 Flash, and the
runner aborts if the configured judge and generator model names are identical.

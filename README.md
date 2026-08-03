# Capstone RAG NPC

This repository contains a Unity game prototype and a Dockerized Python backend for a Retrieval-Augmented Generation NPC dialogue system.

## Project Goal

Build a state-aware NPC that can answer player questions in a procedurally generated puzzle environment by combining:

- Unity for the interactive game client.
- Flask for the local AI backend API.
- A curated knowledge base for game lore, mechanics, puzzles, and NPC persona.
- FAISS and embeddings for retrieval.
- Gemini for response generation.
- RAGAS for evaluation.

## Implemented Vertical Slice

The playable capstone slice uses the three-hint design described in
`knowledge-base/puzzles/three-hint-riddle-chain.md` and
`docs/week1-design-notes.md`:

1. Unity selects three unique valid garden interaction targets each run.
2. The HUD presents the current riddle and clue-point count.
3. Reaching the correct target generates a randomized 3-, 4-, or 5-symbol ordering puzzle.
4. Unity derives visible constraints from a hidden valid order and accepts only puzzles with exactly one solution.
5. Solving the generated puzzle advances the chain and rewards a clue point; reaching the location alone cannot advance it.
6. Red can ask Rock for state-aware help using the visible rules and latest attempt through the Flask RAG backend.
7. Completing the third generated puzzle reveals Blue and ends the run.

`docs/game-concept.md` also describes a larger ten-puzzle, two-area game. That is
the expansion target; it is not treated as part of the verified three-hint
vertical slice.

## Structure

```text
unity-client/     Unity project files
ai-backend/       Flask RAG backend
knowledge-base/   Game lore, mechanics, NPC, and puzzle documents
evaluation/       RAGAS datasets, scripts, and reports
docs/             Architecture and data contract notes
```

## Local Backend Setup

1. Copy `.env.example` to `.env`.
2. Add your Gemini API key to `.env`.
3. Run the backend:

```bash
docker compose up --build
```

The backend should be available at:

```text
http://localhost:5000
```

Health check:

```text
GET http://localhost:5000/health
```

Rock chat:

```text
POST http://localhost:5000/chat
```

The response includes `npc_response`, `retrieved_context`,
`clue_point_spent`, and `response_time_ms`.

Run the complete backend test suite in the same Docker environment:

```bash
docker compose run --rm --no-deps ai-backend pytest -q
```

## Verification

- Backend automated suite: 33 passing tests.
- Unity EditMode suite: 9 passing tests across 3-5 symbols and 75 seeded generations, including progression gating, progressive hint tracking, opening instructions, and hidden-solution serialization.
- Unity compilation: no errors on Unity `6000.4.5f1`.
- Procedural runtime: three unique valid targets, wrong-target rejection,
  ordered progression, clue rewards, and Blue reveal verified.
- Unity HTTP/UI path: verified locally with Gemini disabled, including state
  serialization, response display, loading recovery, and clue spending.
- Live Unity-to-FAISS-to-Gemini chat: verified with the generated procedural
  state displayed back in Rock's UI response.
- Final tuned RAGAS run (10 cases): context precision `0.508333`, faithfulness
  `0.475000`, and answer relevancy `0.405503`. Generated reports are under
  `evaluation/reports/` locally; the methodology and analysis are recorded in
  `docs/verification-report.md`.

Live FAISS rebuilding, Gemini generation verification, and RAGAS scoring send
the non-secret knowledge-base passages and test states to Gemini and therefore
must be run only with explicit approval for that external processing.

## Unity Setup Notes

When creating the Unity project inside `unity-client/`, configure Unity for Git:

- Version Control: Visible Meta Files
- Asset Serialization: Force Text

Commit Unity `.meta` files together with their assets.

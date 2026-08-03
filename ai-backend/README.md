# AI Backend

This service exposes the RAG-powered NPC dialogue API used by the Unity client.

## Responsibilities

- Receive player dialogue requests from Unity.
- Accept the current game state as structured JSON.
- Retrieve relevant knowledge from the project knowledge base.
- Build a persona-aware prompt.
- Generate NPC dialogue using Gemini.
- Return a structured JSON response.

Retrieval combines exact keyword matches with FAISS semantic matches and falls
back to keywords if embeddings or the index are unavailable. The prompt grounds
Rock in retrieved passages plus Unity's current procedural state and enforces
clue-point and no-spoiler rules.

## Development

From the repository root:

```bash
docker compose up --build
```

Health check:

```text
GET http://localhost:5000/health
```

Dialogue endpoint:

```text
POST http://localhost:5000/chat
```

Run the automated suite from an environment containing `requirements.txt`:

```bash
pytest -q
```

# AI Backend

This service will expose the RAG-powered NPC dialogue API used by the Unity client.

## Planned Responsibilities

- Receive player dialogue requests from Unity.
- Accept the current game state as structured JSON.
- Retrieve relevant knowledge from the project knowledge base.
- Build a persona-aware prompt.
- Generate NPC dialogue using Gemini.
- Return a structured JSON response.

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

# Architecture

The project uses a Unity client and a Python AI backend.

```text
Unity Client
  |
  | POST /chat
  v
Flask Backend
  |
  | retrieve relevant context
  v
FAISS Vector Index + Knowledge Base
  |
  | build prompt with persona + context + game state
  v
Gemini Model
  |
  | JSON response
  v
Unity NPC Dialogue UI
```

## Design Notes

- Unity remains responsible for gameplay, puzzle generation, player input, and rendering.
- The backend remains responsible for retrieval, prompt construction, and AI generation.
- The backend should stay stateless: Unity sends the current game state with every request.
- Docker is used for the backend, not for daily Unity development.

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

## Unity Setup Notes

When creating the Unity project inside `unity-client/`, configure Unity for Git:

- Version Control: Visible Meta Files
- Asset Serialization: Force Text

Commit Unity `.meta` files together with their assets.

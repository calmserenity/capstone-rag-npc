import json
import re
from collections.abc import Callable
from typing import Any

from app.rag.chunker import chunk_documents
from app.rag.document_loader import load_knowledge_documents
from app.rag.embeddings import embed_text
from app.rag.vector_store import (
    load_faiss_index,
    load_metadata,
    search_faiss_index,
)


STOPWORDS = {
    "a",
    "an",
    "and",
    "are",
    "can",
    "do",
    "for",
    "go",
    "help",
    "how",
    "i",
    "is",
    "it",
    "me",
    "my",
    "of",
    "on",
    "or",
    "should",
    "the",
    "to",
    "what",
    "where",
    "with",
}

EmbeddingFunction = Callable[[str], list[float]]


def retrieve_context(
    player_query: str,
    game_state: dict[str, Any] | None = None,
    top_k: int = 3,
    embedding_function: EmbeddingFunction = embed_text,
) -> list[str]:
    """Retrieve context with keyword precision plus FAISS semantic recall."""
    retrieval_query = build_retrieval_query(player_query, game_state or {})
    try:
        faiss_results = retrieve_context_with_faiss(
            retrieval_query, top_k, embedding_function
        )
    except Exception:
        faiss_results = []

    keyword_results = retrieve_context_with_keywords(retrieval_query, top_k)
    combined = []
    for result in [*keyword_results, *faiss_results]:
        if result not in combined:
            combined.append(result)
    return combined[:top_k]


def build_retrieval_query(player_query: str, game_state: dict[str, Any]) -> str:
    """Combine the question with authoritative procedural state for retrieval."""
    parts = [f"Player question: {player_query}"]
    state_labels = {
        "current_area": "Current area",
        "objective": "Objective",
        "current_riddle": "Current riddle",
        "current_hint_index": "Current hint index",
        "clue_points": "Clue points",
        "found_hint_locations": "Found hint locations",
        "blue_found": "Blue found",
    }
    for key, label in state_labels.items():
        value = game_state.get(key)
        if value not in (None, "", []):
            parts.append(f"{label}: {value}")

    active_puzzle = game_state.get("active_puzzle")
    if isinstance(active_puzzle, dict) and active_puzzle.get("puzzle_id"):
        safe_puzzle_state = {
            key: active_puzzle.get(key)
            for key in (
                "puzzle_id",
                "puzzle_type",
                "location_id",
                "symbols",
                "constraints",
                "player_attempt",
                "submitted_attempt",
                "attempts",
                "hints_given",
                "is_active",
                "is_solved",
            )
            if key in active_puzzle
        }
        parts.append(
            "Visible procedural puzzle state: "
            + json.dumps(safe_puzzle_state, sort_keys=True)
        )
    return "\n".join(parts)


def retrieve_context_with_faiss(
    player_query: str,
    top_k: int = 3,
    embedding_function: EmbeddingFunction = embed_text,
) -> list[str]:
    """Retrieve context with the saved FAISS index."""
    query_embedding = embedding_function(player_query)
    index = load_faiss_index()
    metadata = load_metadata()
    records = search_faiss_index(query_embedding, index, metadata, top_k)

    return [_format_metadata_record(record) for record in records]


def retrieve_context_with_keywords(player_query: str, top_k: int = 3) -> list[str]:
    """Retrieve context with simple keyword overlap as a safe fallback."""
    documents = load_knowledge_documents()
    chunks = chunk_documents(documents)
    query_keywords = _keywords(player_query)

    scored_chunks = []
    for chunk in chunks:
        chunk_keywords = _keywords(chunk.text)
        score = len(query_keywords & chunk_keywords)
        if score > 0:
            scored_chunks.append((score, chunk))

    scored_chunks.sort(key=lambda item: item[0], reverse=True)
    return [
        f"[{chunk.id}]\n{chunk.text}"
        for _, chunk in scored_chunks[:top_k]
    ]


def _format_metadata_record(record: dict) -> str:
    return f"[{record['chunk_id']}]\n{record['text']}"


def _keywords(text: str) -> set[str]:
    words = re.findall(r"[a-zA-Z0-9_]+", text.lower())
    return {word for word in words if word not in STOPWORDS and len(word) > 2}

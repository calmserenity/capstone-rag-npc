from collections.abc import Callable
from dataclasses import dataclass
from typing import Any

from app.config import load_settings
from app.rag.chunker import KnowledgeChunk


EmbeddingFunction = Callable[[str], list[float]]


@dataclass(frozen=True)
class EmbeddedChunk:
    chunk_id: str
    document_id: str
    category: str
    source: str
    text: str
    embedding: list[float]


def embed_text(text: str) -> list[float]:
    """Create an embedding vector for a single text value with Gemini."""
    settings = load_settings()
    if not settings.gemini_api_key:
        raise ValueError("GEMINI_API_KEY is required to generate embeddings.")

    from google import genai

    client = genai.Client(api_key=settings.gemini_api_key)
    response = client.models.embed_content(
        model=settings.gemini_embedding_model,
        contents=text,
    )
    return _extract_embedding_values(response)


def embed_chunks(
    chunks: list[KnowledgeChunk],
    embedding_function: EmbeddingFunction = embed_text,
) -> list[EmbeddedChunk]:
    """Generate embeddings for knowledge chunks while preserving metadata."""
    embedded_chunks: list[EmbeddedChunk] = []

    for chunk in chunks:
        embedded_chunks.append(
            EmbeddedChunk(
                chunk_id=chunk.id,
                document_id=chunk.document_id,
                category=chunk.category,
                source=chunk.source,
                text=chunk.text,
                embedding=embedding_function(chunk.text),
            )
        )

    return embedded_chunks


def _extract_embedding_values(response: Any) -> list[float]:
    embeddings = getattr(response, "embeddings", None)
    if embeddings:
        values = getattr(embeddings[0], "values", None)
        if values is not None:
            return [float(value) for value in values]

    embedding = getattr(response, "embedding", None)
    if embedding is not None:
        values = getattr(embedding, "values", None)
        if values is not None:
            return [float(value) for value in values]

    if isinstance(response, dict):
        if response.get("embeddings"):
            return [float(value) for value in response["embeddings"][0]["values"]]
        if response.get("embedding"):
            return [float(value) for value in response["embedding"]["values"]]

    raise ValueError("Gemini embedding response did not contain embedding values.")

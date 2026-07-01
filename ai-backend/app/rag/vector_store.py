import json
from pathlib import Path
from typing import Any

import faiss
import numpy as np

from app.rag.embeddings import EmbeddedChunk


DEFAULT_FAISS_DIR = Path("/app/data/faiss")
INDEX_FILENAME = "index.faiss"
METADATA_FILENAME = "metadata.json"


def build_faiss_index(embedded_chunks: list[EmbeddedChunk]):
    """Build a FAISS index from embedded chunks."""
    vectors = _embedding_matrix(embedded_chunks)
    index = faiss.IndexFlatIP(vectors.shape[1])
    index.add(vectors)
    return index


def save_faiss_index(
    index,
    embedded_chunks: list[EmbeddedChunk],
    output_dir: Path = DEFAULT_FAISS_DIR,
) -> None:
    """Save a FAISS index and chunk metadata side by side."""
    output_dir.mkdir(parents=True, exist_ok=True)

    faiss.write_index(index, str(output_dir / INDEX_FILENAME))
    (output_dir / METADATA_FILENAME).write_text(
        json.dumps(_metadata_records(embedded_chunks), indent=2),
        encoding="utf-8",
    )


def store_embeddings(
    embedded_chunks: list[EmbeddedChunk],
    output_dir: Path = DEFAULT_FAISS_DIR,
):
    """Build and save the FAISS index for embedded chunks."""
    index = build_faiss_index(embedded_chunks)
    save_faiss_index(index, embedded_chunks, output_dir)
    return index


def load_faiss_index(index_dir: Path = DEFAULT_FAISS_DIR):
    """Load a saved FAISS index from disk."""
    index_path = index_dir / INDEX_FILENAME
    if not index_path.exists():
        raise FileNotFoundError(f"FAISS index not found: {index_path}")

    return faiss.read_index(str(index_path))


def load_metadata(index_dir: Path = DEFAULT_FAISS_DIR) -> list[dict[str, Any]]:
    """Load chunk metadata saved beside the FAISS index."""
    metadata_path = index_dir / METADATA_FILENAME
    if not metadata_path.exists():
        raise FileNotFoundError(f"FAISS metadata not found: {metadata_path}")

    return json.loads(metadata_path.read_text(encoding="utf-8"))


def search_faiss_index(
    query_embedding: list[float],
    index,
    metadata: list[dict[str, Any]],
    top_k: int = 3,
) -> list[dict[str, Any]]:
    """Search a FAISS index and return matching metadata records."""
    if not query_embedding:
        raise ValueError("Query embedding cannot be empty.")
    if top_k <= 0:
        return []

    query_vector = np.array([query_embedding], dtype="float32")
    faiss.normalize_L2(query_vector)
    distances, indices = index.search(query_vector, top_k)

    results: list[dict[str, Any]] = []
    for distance, index_position in zip(distances[0], indices[0]):
        if index_position < 0 or index_position >= len(metadata):
            continue

        record = dict(metadata[index_position])
        record["score"] = float(distance)
        results.append(record)

    return results


def _embedding_matrix(embedded_chunks: list[EmbeddedChunk]) -> np.ndarray:
    if not embedded_chunks:
        raise ValueError("At least one embedded chunk is required.")

    dimensions = len(embedded_chunks[0].embedding)
    if dimensions == 0:
        raise ValueError("Embedding vectors cannot be empty.")

    vectors = np.array(
        [chunk.embedding for chunk in embedded_chunks],
        dtype="float32",
    )

    if vectors.ndim != 2 or vectors.shape[1] != dimensions:
        raise ValueError("Embedding vectors must have consistent dimensions.")

    faiss.normalize_L2(vectors)
    return vectors


def _metadata_records(embedded_chunks: list[EmbeddedChunk]) -> list[dict[str, Any]]:
    return [
        {
            "chunk_id": chunk.chunk_id,
            "document_id": chunk.document_id,
            "category": chunk.category,
            "source": chunk.source,
            "text": chunk.text,
        }
        for chunk in embedded_chunks
    ]

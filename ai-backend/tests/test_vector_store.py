import json
from pathlib import Path

import pytest

pytest.importorskip("faiss")

from app.rag.embeddings import EmbeddedChunk
from app.rag.vector_store import (
    INDEX_FILENAME,
    METADATA_FILENAME,
    build_faiss_index,
    load_faiss_index,
    load_metadata,
    search_faiss_index,
    store_embeddings,
)


def test_store_embeddings_writes_faiss_index_and_metadata(tmp_path: Path):
    embedded_chunks = [
        EmbeddedChunk(
            chunk_id="npc/rock.md#chunk-0",
            document_id="npc/rock.md",
            category="npc",
            source="rock.md",
            text="Rock is sleepy.",
            embedding=[1.0, 0.0, 0.0],
        ),
        EmbeddedChunk(
            chunk_id="puzzles/key.md#chunk-0",
            document_id="puzzles/key.md",
            category="puzzles",
            source="key.md",
            text="The key unlocks the gate.",
            embedding=[0.0, 1.0, 0.0],
        ),
    ]

    index = store_embeddings(embedded_chunks, tmp_path)
    metadata = json.loads((tmp_path / METADATA_FILENAME).read_text(encoding="utf-8"))

    assert index.ntotal == 2
    assert (tmp_path / INDEX_FILENAME).exists()
    assert metadata == [
        {
            "chunk_id": "npc/rock.md#chunk-0",
            "document_id": "npc/rock.md",
            "category": "npc",
            "source": "rock.md",
            "text": "Rock is sleepy.",
        },
        {
            "chunk_id": "puzzles/key.md#chunk-0",
            "document_id": "puzzles/key.md",
            "category": "puzzles",
            "source": "key.md",
            "text": "The key unlocks the gate.",
        },
    ]


def test_build_faiss_index_rejects_empty_embeddings():
    with pytest.raises(ValueError, match="At least one embedded chunk"):
        build_faiss_index([])


def test_search_faiss_index_returns_closest_metadata_record(tmp_path: Path):
    embedded_chunks = [
        EmbeddedChunk(
            chunk_id="puzzles/pond.md#chunk-0",
            document_id="puzzles/pond.md",
            category="puzzles",
            source="pond.md",
            text="The pond reflects the sky.",
            embedding=[1.0, 0.0],
        ),
        EmbeddedChunk(
            chunk_id="npc/rock.md#chunk-0",
            document_id="npc/rock.md",
            category="npc",
            source="rock.md",
            text="Rock is sleepy.",
            embedding=[0.0, 1.0],
        ),
    ]

    store_embeddings(embedded_chunks, tmp_path)
    index = load_faiss_index(tmp_path)
    metadata = load_metadata(tmp_path)

    results = search_faiss_index([0.9, 0.1], index, metadata, top_k=1)

    assert len(results) == 1
    assert results[0]["chunk_id"] == "puzzles/pond.md#chunk-0"
    assert results[0]["text"] == "The pond reflects the sky."
    assert "score" in results[0]

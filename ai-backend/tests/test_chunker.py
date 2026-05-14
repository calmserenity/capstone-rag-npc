from app.rag.chunker import chunk_documents
from app.rag.document_loader import KnowledgeDocument


def test_chunk_documents_preserves_metadata_and_limits_chunk_size():
    document = KnowledgeDocument(
        id="npc/rock.md",
        category="npc",
        source="rock.md",
        text=(
            "# Rock\n\n"
            "Rock is sleepy and kind.\n\n"
            "Rock gives clues without revealing direct answers."
        ),
    )

    chunks = chunk_documents([document], max_chars=45)

    assert len(chunks) == 3
    assert chunks[0].id == "npc/rock.md#chunk-0"
    assert chunks[0].document_id == "npc/rock.md"
    assert chunks[0].category == "npc"
    assert chunks[0].source == "rock.md"
    assert all(len(chunk.text) <= 45 for chunk in chunks)

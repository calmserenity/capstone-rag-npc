from app.rag.chunker import KnowledgeChunk
from app.rag.embeddings import _extract_embedding_values, embed_chunks


def test_embed_chunks_preserves_chunk_metadata():
    chunk = KnowledgeChunk(
        id="puzzles/outer-garden-key.md#chunk-0",
        document_id="puzzles/outer-garden-key.md",
        category="puzzles",
        source="outer-garden-key.md",
        text="The key unlocks the gate.",
        chunk_index=0,
    )

    embedded_chunks = embed_chunks(
        [chunk],
        embedding_function=lambda text: [float(len(text)), 1.0, 0.5],
    )

    assert len(embedded_chunks) == 1
    assert embedded_chunks[0].chunk_id == chunk.id
    assert embedded_chunks[0].document_id == chunk.document_id
    assert embedded_chunks[0].category == "puzzles"
    assert embedded_chunks[0].source == "outer-garden-key.md"
    assert embedded_chunks[0].text == "The key unlocks the gate."
    assert embedded_chunks[0].embedding == [25.0, 1.0, 0.5]


def test_extract_embedding_values_supports_dict_response_shape():
    response = {"embeddings": [{"values": ["0.1", 0.2, 3]}]}

    assert _extract_embedding_values(response) == [0.1, 0.2, 3.0]

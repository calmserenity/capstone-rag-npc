from app.rag.document_loader import KnowledgeDocument
from app.rag import retriever


def test_retrieve_context_with_faiss_formats_matching_records(monkeypatch):
    monkeypatch.setattr(retriever, "load_faiss_index", lambda: "fake-index")
    monkeypatch.setattr(
        retriever,
        "load_metadata",
        lambda: [
            {
                "chunk_id": "puzzles/pond.md#chunk-0",
                "text": "The pond reflects the sky.",
            },
        ],
    )
    monkeypatch.setattr(
        retriever,
        "search_faiss_index",
        lambda query_embedding, index, metadata, top_k: metadata,
    )

    context = retriever.retrieve_context_with_faiss(
        "Where does the sky rest?",
        embedding_function=lambda text: [1.0, 0.0],
    )

    assert context == [
        "[puzzles/pond.md#chunk-0]\nThe pond reflects the sky.",
    ]


def test_retrieve_context_falls_back_to_keyword_retrieval(monkeypatch):
    monkeypatch.setattr(
        retriever,
        "retrieve_context_with_faiss",
        lambda player_query, top_k, embedding_function: (_ for _ in ()).throw(
            FileNotFoundError("missing index")
        ),
    )
    monkeypatch.setattr(
        retriever,
        "load_knowledge_documents",
        lambda: [
            KnowledgeDocument(
                id="puzzles/pond.md",
                category="puzzles",
                source="pond.md",
                text="The pond reflects the sky.",
            ),
        ],
    )

    context = retriever.retrieve_context("sky", embedding_function=lambda text: [1.0])

    assert context == [
        "[puzzles/pond.md#chunk-0]\nThe pond reflects the sky.",
    ]

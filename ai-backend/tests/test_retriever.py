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


def test_retrieval_query_includes_live_puzzle_state():
    query = retriever.build_retrieval_query(
        "Where should I look?",
        {
            "current_riddle": "It reflects the sky, but it is not a mirror.",
            "current_hint_index": 1,
            "clue_points": 2,
            "found_hint_locations": ["bench"],
        },
    )

    assert "Where should I look?" in query
    assert "It reflects the sky" in query
    assert "Current hint index: 1" in query
    assert "Found hint locations: ['bench']" in query


def test_retrieval_query_includes_visible_generated_puzzle_but_not_solution():
    query = retriever.build_retrieval_query(
        "Why is my order wrong?",
        {
            "active_puzzle": {
                "puzzle_id": "ordering-1234",
                "puzzle_type": "symbol_ordering",
                "symbols": ["stone", "flower", "lantern"],
                "constraints": ["The flower comes before the lantern."],
                "player_attempt": ["lantern", "flower", "stone"],
                "attempts": 1,
                "is_active": True,
                "solution": ["flower", "stone", "lantern"],
            }
        },
    )

    assert "ordering-1234" in query
    assert "The flower comes before the lantern" in query
    assert "player_attempt" in query
    assert "solution" not in query

import re

from app.rag.chunker import chunk_documents
from app.rag.document_loader import load_knowledge_documents


def _keywords(text: str) -> set[str]:
    return {
        word
        for word in re.findall(r"[a-zA-Z]{3,}", text.lower())
        if word not in {"the", "and", "for", "that", "with", "from", "should"}
    }


def retrieve_context(player_query: str) -> list[str]:
    """Return knowledge snippets relevant to the player query.

    This simple retriever loads and chunks the knowledge base, then ranks chunks
    by keyword overlap until the embedding pipeline is added.
    """
    documents = load_knowledge_documents()
    if not documents:
        return ["No knowledge base documents were loaded."]

    chunks = chunk_documents(documents)
    if not chunks:
        return ["No knowledge base chunks were created."]

    query_keywords = _keywords(player_query)
    ranked_chunks = sorted(
        chunks,
        key=lambda chunk: len(query_keywords & _keywords(chunk.text)),
        reverse=True,
    )

    return [
        f"[{chunk.id}]\n{chunk.text}"
        for chunk in ranked_chunks[:3]
    ]

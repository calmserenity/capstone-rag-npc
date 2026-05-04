def retrieve_context(player_query: str) -> list[str]:
    """Return knowledge snippets relevant to the player query.

    This placeholder will later load the FAISS index and retrieve chunks from
    the curated knowledge base.
    """
    return [
        "Retriever placeholder: connect this to FAISS and the knowledge base.",
        f"Player query: {player_query}",
    ]

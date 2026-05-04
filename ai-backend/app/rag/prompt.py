from typing import Any


NPC_SYSTEM_PROMPT = """
You are a helpful in-game NPC guide.
Stay in character, answer concisely, and only use the provided context and game state.
If the answer is unknown, give an in-character response that admits uncertainty.
""".strip()


def build_prompt(
    player_query: str,
    game_state: dict[str, Any],
    retrieved_context: list[str],
) -> str:
    context_block = "\n".join(f"- {item}" for item in retrieved_context)

    return f"""
{NPC_SYSTEM_PROMPT}

Retrieved context:
{context_block}

Current game state:
{game_state}

Player question:
{player_query}
""".strip()

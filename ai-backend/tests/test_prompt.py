from app.rag.prompt import build_prompt


def test_build_prompt_includes_riddle_and_clue_point_rules():
    prompt = build_prompt(
        player_query="What does this riddle mean?",
        game_state={
            "clue_points": 0,
            "current_riddle": "It reflects the sky, but it is not a mirror.",
            "current_hint_index": 1,
            "blue_found": False,
        },
        retrieved_context=[
            "The pond reflects the sky but should not be named directly.",
        ],
    )

    assert "You are Rock" in prompt
    assert "clue_points is 0" in prompt
    assert "do not describe the answer, its object type, or its location" in prompt
    assert "Do not use imagery that points to the answer" in prompt
    assert "current_riddle" in prompt
    assert "It reflects the sky" in prompt
    assert "Answer the player's actual question immediately" in prompt
    assert "directly supported by the retrieved context or game state" in prompt
    assert "never state the exact answer, target display name" in prompt


def test_build_prompt_handles_generated_puzzle_without_hidden_solution():
    prompt = build_prompt(
        player_query="What did I place incorrectly?",
        game_state={
            "clue_points": 1,
            "active_puzzle": {
                "puzzle_id": "ordering-abcd",
                "symbols": ["stone", "flower", "lantern"],
                "constraints": [
                    "The flower comes before the lantern.",
                    "The lantern comes before the stone.",
                ],
                "player_attempt": ["lantern", "flower", "stone"],
                "attempts": 1,
                "is_active": True,
            },
        },
        retrieved_context=["Ordering puzzles are solved from visible constraints."],
    )

    assert "place the flower first" in prompt
    assert "Never state the complete correct ordering" in prompt
    assert "ordering-abcd" in prompt
    assert "solution" not in str({"active_puzzle": {"puzzle_id": "ordering-abcd"}})

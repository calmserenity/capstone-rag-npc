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
    assert "Do not directly name the target location" in prompt
    assert "It reflects the sky" in prompt

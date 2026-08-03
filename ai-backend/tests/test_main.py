from app import main


def test_chat_uses_game_state_for_retrieval_and_returns_clue_contract(monkeypatch):
    captured = {}

    def fake_retrieve(player_query, game_state):
        captured["query"] = player_query
        captured["state"] = game_state
        return ["[puzzles/pond]\nThe pond reflects the sky."]

    monkeypatch.setattr(main, "retrieve_context", fake_retrieve)
    monkeypatch.setattr(main, "generate_response", lambda prompt: "Mmm... seek where the sky rests.")

    response = main.app.test_client().post(
        "/chat",
        json={
            "player_query": "What does this riddle mean?",
            "game_state": {
                "clue_points": 2,
                "current_hint_index": 1,
                "current_riddle": "It reflects the sky, but it is not a mirror.",
            },
        },
    )

    body = response.get_json()
    assert response.status_code == 200
    assert captured["state"]["current_hint_index"] == 1
    assert body["npc_response"].startswith("Mmm")
    assert body["clue_point_spent"] is True
    assert body["response_time_ms"] >= 0
    assert len(body["retrieved_context"]) == 1


def test_chat_does_not_spend_when_no_clue_points(monkeypatch):
    monkeypatch.setattr(main, "retrieve_context", lambda query, state: [])
    monkeypatch.setattr(main, "generate_response", lambda prompt: "Mmm... inspect the garden first.")

    response = main.app.test_client().post(
        "/chat",
        json={"player_query": "Help me", "game_state": {"clue_points": 0}},
    )

    assert response.status_code == 200
    assert response.get_json()["clue_point_spent"] is False


def test_chat_guarantees_first_puzzle_item_when_model_omits_it(monkeypatch):
    monkeypatch.setattr(main, "retrieve_context", lambda query, state: [])
    monkeypatch.setattr(
        main,
        "generate_response",
        lambda prompt: "Mmm... compare the visible rules carefully.",
    )

    response = main.app.test_client().post(
        "/chat",
        json={
            "player_query": "Please give me a hint for this puzzle.",
            "game_state": {
                "clue_points": 1,
                "active_puzzle": {
                    "puzzle_id": "ordering-test",
                    "symbols": ["lantern", "flower", "stone"],
                    "constraints": [
                        "The flower comes before the stone.",
                        "The stone is immediately before the lantern.",
                    ],
                    "player_attempt": [],
                    "attempts": 0,
                    "is_active": True,
                    "is_solved": False,
                },
            },
        },
    )

    assert response.status_code == 200
    assert response.get_json()["npc_response"].startswith(
        "Mmm... place the flower first."
    )
    assert response.get_json()["puzzle_hint_given"] is True


def test_chat_returns_progressive_wrong_position_hint(monkeypatch):
    monkeypatch.setattr(main, "retrieve_context", lambda query, state: [])
    monkeypatch.setattr(
        main,
        "generate_response",
        lambda prompt: "This should not be used for a controlled puzzle hint.",
    )

    response = main.app.test_client().post(
        "/chat",
        json={
            "player_query": "Give me another hint.",
            "game_state": {
                "clue_points": 2,
                "active_puzzle": {
                    "puzzle_id": "ordering-test",
                    "symbols": ["lantern", "flower", "stone"],
                    "constraints": [
                        "The flower comes before the stone.",
                        "The stone is immediately before the lantern.",
                    ],
                    "submitted_attempt": ["flower", "lantern", "stone"],
                    "attempts": 1,
                    "hints_given": 1,
                    "is_active": True,
                    "is_solved": False,
                },
            },
        },
    )

    body = response.get_json()
    assert response.status_code == 200
    assert "position 2 is wrong" in body["npc_response"]
    assert body["clue_point_spent"] is True
    assert body["puzzle_hint_given"] is True


def test_chat_does_not_charge_for_positional_hint_before_submission(monkeypatch):
    monkeypatch.setattr(main, "retrieve_context", lambda query, state: [])

    response = main.app.test_client().post(
        "/chat",
        json={
            "player_query": "Give me another hint.",
            "game_state": {
                "clue_points": 2,
                "active_puzzle": {
                    "puzzle_id": "ordering-test",
                    "symbols": ["lantern", "flower", "stone"],
                    "constraints": [
                        "The flower comes before the stone.",
                        "The stone is immediately before the lantern.",
                    ],
                    "submitted_attempt": [],
                    "attempts": 0,
                    "hints_given": 1,
                    "is_active": True,
                    "is_solved": False,
                },
            },
        },
    )

    body = response.get_json()
    assert "submit a complete order first" in body["npc_response"]
    assert body["clue_point_spent"] is False
    assert body["puzzle_hint_given"] is False


def test_chat_rejects_empty_question():
    response = main.app.test_client().post(
        "/chat",
        json={"player_query": "", "game_state": {}},
    )

    assert response.status_code == 400
    assert response.get_json()["error"] == "Invalid request"

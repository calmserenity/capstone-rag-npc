from app.rag.puzzle_hints import (
    build_progressive_puzzle_hint,
    ensure_first_item_is_given,
    infer_first_item,
    infer_unique_order,
    required_first_item_hint,
)


def test_infers_first_item_from_visible_constraints():
    first_item = infer_first_item(
        ["lantern", "flower", "stone"],
        [
            "The flower comes before the stone.",
            "The stone is immediately before the lantern.",
        ],
    )

    assert first_item == "flower"


def test_infers_unique_order_from_visible_constraints():
    order = infer_unique_order(
        ["lantern", "flower", "stone"],
        [
            "The flower comes before the stone.",
            "The stone is immediately before the lantern.",
        ],
    )

    assert order == ("flower", "stone", "lantern")


def test_requires_first_item_for_active_puzzle_help():
    first_item = required_first_item_hint(
        "Can I get a hint? I am stuck.",
        {
            "clue_points": 1,
            "active_puzzle": {
                "symbols": ["berry", "stone", "feather", "mushroom"],
                "constraints": [
                    "The feather comes before the berry.",
                    "The mushroom is immediately before the stone.",
                    "The berry comes before the mushroom.",
                ],
                "is_active": True,
                "is_solved": False,
            },
        },
    )

    assert first_item == "feather"


def test_does_not_reveal_first_item_without_a_clue_point():
    first_item = required_first_item_hint(
        "Help me solve this puzzle",
        {
            "clue_points": 0,
            "active_puzzle": {
                "symbols": ["flower", "stone", "lantern"],
                "constraints": [
                    "The flower comes before the stone.",
                    "The stone comes before the lantern.",
                ],
                "is_active": True,
                "is_solved": False,
            },
        },
    )

    assert first_item is None


def test_enforcement_adds_first_item_when_model_omits_it():
    response = ensure_first_item_is_given(
        "Mmm... compare the visible rules carefully.",
        "watering can",
    )

    assert response.startswith("Mmm... place the watering can first.")


def test_enforcement_corrects_response_that_places_item_without_saying_first():
    response = ensure_first_item_is_given(
        "Place the flower after the stone.",
        "flower",
    )

    assert response.startswith("Mmm... place the flower first.")


def test_second_hint_identifies_first_additional_wrong_position():
    hint = build_progressive_puzzle_hint(
        "Can I have another hint?",
        {
            "clue_points": 2,
            "active_puzzle": {
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
    )

    assert hint is not None
    assert hint.consumes_clue is True
    assert "position 2 is wrong" in hint.response
    assert "lantern does not belong there" in hint.response
    assert "position 3" not in hint.response


def test_later_hint_cumulatively_identifies_more_wrong_positions():
    hint = build_progressive_puzzle_hint(
        "Tell me more places that are wrong.",
        {
            "clue_points": 1,
            "active_puzzle": {
                "symbols": ["lantern", "flower", "stone"],
                "constraints": [
                    "The flower comes before the stone.",
                    "The stone is immediately before the lantern.",
                ],
                "submitted_attempt": ["flower", "lantern", "stone"],
                "attempts": 1,
                "hints_given": 2,
                "is_active": True,
                "is_solved": False,
            },
        },
    )

    assert hint is not None
    assert hint.consumes_clue is True
    assert "position 2 (lantern)" in hint.response
    assert "position 3 (stone)" in hint.response


def test_positional_hint_requires_a_submitted_answer_without_spending():
    hint = build_progressive_puzzle_hint(
        "I need another hint.",
        {
            "clue_points": 2,
            "active_puzzle": {
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
    )

    assert hint is not None
    assert hint.consumes_clue is False
    assert "submit a complete order first" in hint.response


def test_inference_declines_when_first_item_is_ambiguous():
    first_item = infer_first_item(
        ["flower", "stone", "lantern"],
        ["The flower comes before the lantern."],
    )

    assert first_item is None

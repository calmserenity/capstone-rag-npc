import re
from dataclasses import dataclass
from itertools import permutations
from typing import Any


_PUZZLE_HELP_WORDS = re.compile(
    r"\b(help|hint|clue|stuck|start|begin|first|more|another|solve|solution|"
    r"wrong|incorrect|fail|failed|break|broke|violat\w*|"
    r"order|arrangement|rule|symbol|item|place|put)\b",
    re.IGNORECASE,
)
_CONSTRAINT = re.compile(
    r"^The (.+?) (comes before|is immediately before) the (.+?)\.$",
    re.IGNORECASE,
)


@dataclass(frozen=True)
class PuzzleHintDirective:
    response: str
    consumes_clue: bool


def build_progressive_puzzle_hint(
    player_query: str,
    game_state: dict[str, Any],
) -> PuzzleHintDirective | None:
    """Build the next deterministic hint for an active ordering puzzle."""
    active_puzzle = game_state.get("active_puzzle")
    if not _is_help_request_for_active_puzzle(player_query, active_puzzle):
        return None
    if int(game_state.get("clue_points", 0) or 0) <= 0:
        return PuzzleHintDirective(
            "Mmm... no whispers remain. Solve another garden clue first.",
            False,
        )

    solution = infer_unique_order(
        active_puzzle.get("symbols"),
        active_puzzle.get("constraints"),
    )
    if solution is None:
        return PuzzleHintDirective(
            "Mmm... I cannot safely read a unique order from those visible rules.",
            False,
        )

    hints_given = max(0, int(active_puzzle.get("hints_given", 0) or 0))
    if hints_given == 0:
        return PuzzleHintDirective(
            f"Mmm... place the {solution[0]} first.",
            True,
        )

    submitted_attempt = active_puzzle.get(
        "submitted_attempt",
        active_puzzle.get("player_attempt"),
    )
    if (
        int(active_puzzle.get("attempts", 0) or 0) <= 0
        or not isinstance(submitted_attempt, list)
        or len(submitted_attempt) != len(solution)
    ):
        return PuzzleHintDirective(
            "Mmm... submit a complete order first, then I can point out a wrong position.",
            False,
        )

    # Position one was already explained by the first hint, so later hints reveal
    # additional incorrect positions from the latest submitted answer.
    wrong_positions = [
        (index, submitted_attempt[index])
        for index in range(1, len(solution))
        if submitted_attempt[index] != solution[index]
    ]
    if not wrong_positions:
        return PuzzleHintDirective(
            "Mmm... the first-item hint already identifies the only remaining problem I can show.",
            False,
        )

    revealed_count = min(hints_given, len(wrong_positions))
    revealed_positions = wrong_positions[:revealed_count]
    consumes_clue = hints_given <= len(wrong_positions)
    return PuzzleHintDirective(
        _format_wrong_positions(revealed_positions),
        consumes_clue,
    )


def required_first_item_hint(
    player_query: str,
    game_state: dict[str, Any],
) -> str | None:
    """Compatibility helper returning the first item for the initial hint only."""
    directive = build_progressive_puzzle_hint(player_query, game_state)
    if directive is None or not directive.consumes_clue:
        return None
    active_puzzle = game_state.get("active_puzzle", {})
    if int(active_puzzle.get("hints_given", 0) or 0) != 0:
        return None
    solution = infer_unique_order(
        active_puzzle.get("symbols"),
        active_puzzle.get("constraints"),
    )
    return solution[0] if solution else None


def infer_first_item(symbols: Any, constraints: Any) -> str | None:
    solution = infer_unique_order(symbols, constraints)
    return solution[0] if solution else None


def infer_unique_order(symbols: Any, constraints: Any) -> tuple[str, ...] | None:
    """Solve only from public visible constraints and require one valid ordering."""
    if not isinstance(symbols, list) or not 2 <= len(symbols) <= 5:
        return None
    if not all(isinstance(symbol, str) and symbol.strip() for symbol in symbols):
        return None
    if not isinstance(constraints, list) or not constraints:
        return None

    canonical_symbols = {symbol.casefold(): symbol for symbol in symbols}
    parsed_constraints: list[tuple[str, str, bool]] = []
    for constraint in constraints:
        if not isinstance(constraint, str):
            return None
        match = _CONSTRAINT.fullmatch(constraint.strip())
        if match is None:
            return None

        first = canonical_symbols.get(match.group(1).casefold())
        second = canonical_symbols.get(match.group(3).casefold())
        if first is None or second is None:
            return None
        parsed_constraints.append(
            (first, second, match.group(2).casefold() == "is immediately before")
        )

    valid_orders = [
        order
        for order in permutations(symbols)
        if all(
            _constraint_is_satisfied(order, first, second, immediate)
            for first, second, immediate in parsed_constraints
        )
    ]
    return valid_orders[0] if len(valid_orders) == 1 else None


def ensure_first_item_is_given(npc_response: str, first_item: str | None) -> str:
    """Compatibility guard retained for callers outside the chat endpoint."""
    if first_item is None:
        return npc_response
    return f"Mmm... place the {first_item} first."


def _is_help_request_for_active_puzzle(
    player_query: str,
    active_puzzle: Any,
) -> bool:
    return (
        bool(_PUZZLE_HELP_WORDS.search(player_query))
        and isinstance(active_puzzle, dict)
        and bool(active_puzzle.get("is_active"))
        and not bool(active_puzzle.get("is_solved"))
    )


def _format_wrong_positions(wrong_positions: list[tuple[int, Any]]) -> str:
    if len(wrong_positions) == 1:
        index, item = wrong_positions[0]
        return (
            f"Mmm... in your latest submitted answer, position {index + 1} "
            f"is wrong: {item} does not belong there."
        )

    details = ", ".join(
        f"position {index + 1} ({item})"
        for index, item in wrong_positions
    )
    return (
        "Mmm... these places in your latest submitted answer are wrong: "
        f"{details}."
    )


def _constraint_is_satisfied(
    order: tuple[str, ...],
    first: str,
    second: str,
    immediate: bool,
) -> bool:
    first_index = order.index(first)
    second_index = order.index(second)
    return (
        second_index == first_index + 1
        if immediate
        else first_index < second_index
    )

import logging
from time import perf_counter

from flask import Flask, jsonify, request
from pydantic import ValidationError

from app.rag.generator import generate_response
from app.rag.prompt import build_prompt
from app.rag.puzzle_hints import build_progressive_puzzle_hint
from app.rag.retriever import retrieve_context
from app.schemas import ChatRequest, ChatResponse


app = Flask(__name__)
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


@app.get("/health")
def health() -> tuple[dict[str, str], int]:
    return {"status": "ok"}, 200


@app.post("/chat")
def chat():
    started_at = perf_counter()
    try:
        chat_request = ChatRequest.model_validate(request.get_json(force=True))
    except (ValidationError, TypeError) as error:
        details = error.errors() if isinstance(error, ValidationError) else str(error)
        return jsonify({"error": "Invalid request", "details": details}), 400

    clue_points = int(chat_request.game_state.get("clue_points", 0) or 0)
    retrieved_context = retrieve_context(chat_request.player_query, chat_request.game_state)
    prompt = build_prompt(
        player_query=chat_request.player_query,
        game_state=chat_request.game_state,
        retrieved_context=retrieved_context,
    )
    puzzle_hint = build_progressive_puzzle_hint(
        chat_request.player_query,
        chat_request.game_state,
    )
    npc_response = (
        puzzle_hint.response
        if puzzle_hint is not None
        else generate_response(prompt)
    )
    elapsed_ms = round((perf_counter() - started_at) * 1000)
    clue_point_spent = (
        puzzle_hint.consumes_clue
        if puzzle_hint is not None
        else clue_points > 0
    )
    puzzle_hint_given = puzzle_hint is not None and puzzle_hint.consumes_clue

    logger.info(
        "chat query=%r hint_index=%r clue_points=%d chunks=%d latency_ms=%d response=%r",
        chat_request.player_query,
        chat_request.game_state.get("current_hint_index"),
        clue_points,
        len(retrieved_context),
        elapsed_ms,
        npc_response,
    )

    response = ChatResponse(
        npc_response=npc_response,
        retrieved_context=retrieved_context,
        clue_point_spent=clue_point_spent,
        puzzle_hint_given=puzzle_hint_given,
        response_time_ms=elapsed_ms,
    )
    return jsonify(response.model_dump()), 200

from flask import Flask, jsonify, request
from pydantic import ValidationError

from app.rag.generator import generate_response
from app.rag.prompt import build_prompt
from app.rag.retriever import retrieve_context
from app.schemas import ChatRequest, ChatResponse


app = Flask(__name__)


@app.get("/health")
def health() -> tuple[dict[str, str], int]:
    return {"status": "ok"}, 200


@app.post("/chat")
def chat():
    try:
        chat_request = ChatRequest.model_validate(request.get_json(force=True))
    except ValidationError as error:
        return jsonify({"error": "Invalid request", "details": error.errors()}), 400

    retrieved_context = retrieve_context(chat_request.player_query)
    prompt = build_prompt(
        player_query=chat_request.player_query,
        game_state=chat_request.game_state,
        retrieved_context=retrieved_context,
    )
    npc_response = generate_response(prompt)

    response = ChatResponse(
        npc_response=npc_response,
        retrieved_context=retrieved_context,
    )
    return jsonify(response.model_dump()), 200

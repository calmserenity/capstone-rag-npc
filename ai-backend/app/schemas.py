from typing import Any

from pydantic import BaseModel, Field


class ChatRequest(BaseModel):
    player_query: str = Field(..., min_length=1)
    game_state: dict[str, Any] = Field(default_factory=dict)


class ChatResponse(BaseModel):
    npc_response: str
    retrieved_context: list[str] = Field(default_factory=list)

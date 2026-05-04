import os
from dataclasses import dataclass


@dataclass(frozen=True)
class Settings:
    gemini_api_key: str
    gemini_model: str
    backend_host: str
    backend_port: int


def load_settings() -> Settings:
    return Settings(
        gemini_api_key=os.getenv("GEMINI_API_KEY", ""),
        gemini_model=os.getenv("GEMINI_MODEL", "gemini-2.5-flash"),
        backend_host=os.getenv("BACKEND_HOST", "0.0.0.0"),
        backend_port=int(os.getenv("BACKEND_PORT", "5000")),
    )

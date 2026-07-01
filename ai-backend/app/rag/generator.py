from collections.abc import Callable
from typing import Any

from app.config import load_settings


GenerationFunction = Callable[[str], Any]

FALLBACK_RESPONSE = (
    "Mmm. Rock is having trouble hearing the garden clearly right now. "
    "Look closely at the riddle and the place it describes."
)


def generate_response(
    prompt: str,
    generation_function: GenerationFunction | None = None,
) -> str:
    """Generate NPC dialogue from a completed prompt."""
    if generation_function is None:
        generation_function = _generate_with_gemini

    try:
        response = generation_function(prompt)
        return _extract_response_text(response)
    except Exception:
        return FALLBACK_RESPONSE


def _generate_with_gemini(prompt: str) -> Any:
    settings = load_settings()
    if not settings.gemini_api_key:
        raise ValueError("GEMINI_API_KEY is required to generate NPC responses.")

    from google import genai

    client = genai.Client(api_key=settings.gemini_api_key)
    return client.models.generate_content(
        model=settings.gemini_model,
        contents=prompt,
    )


def _extract_response_text(response: Any) -> str:
    if isinstance(response, str):
        return _clean_response_text(response)

    text = getattr(response, "text", None)
    if text is not None:
        return _clean_response_text(text)

    if isinstance(response, dict):
        if response.get("text") is not None:
            return _clean_response_text(response["text"])

        candidates = response.get("candidates") or []
        if candidates:
            parts = candidates[0].get("content", {}).get("parts", [])
            joined_text = " ".join(
                str(part["text"])
                for part in parts
                if isinstance(part, dict) and part.get("text")
            )
            return _clean_response_text(joined_text)

    return FALLBACK_RESPONSE


def _clean_response_text(text: Any) -> str:
    cleaned_text = str(text).strip()
    if not cleaned_text:
        return FALLBACK_RESPONSE
    return cleaned_text

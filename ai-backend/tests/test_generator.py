from app.rag.generator import FALLBACK_RESPONSE, generate_response


class FakeGeminiResponse:
    text = "The sky rests where the water is quiet."


def test_generate_response_uses_generation_function_text():
    response = generate_response(
        "prompt",
        generation_function=lambda prompt: FakeGeminiResponse(),
    )

    assert response == "The sky rests where the water is quiet."


def test_generate_response_supports_dict_candidate_shape():
    response = generate_response(
        "prompt",
        generation_function=lambda prompt: {
            "candidates": [
                {
                    "content": {
                        "parts": [
                            {"text": "Think of a place that reflects the sky."},
                        ],
                    },
                },
            ],
        },
    )

    assert response == "Think of a place that reflects the sky."


def test_generate_response_returns_fallback_on_error():
    def broken_generation(prompt: str):
        raise RuntimeError("Gemini unavailable")

    assert generate_response("prompt", broken_generation) == FALLBACK_RESPONSE

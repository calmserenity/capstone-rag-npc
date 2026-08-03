import argparse
import asyncio
import json
import math
import os
import time
from pathlib import Path
from urllib import request

import pandas as pd
from datasets import Dataset
from google import genai
from ragas import evaluate
from ragas.embeddings.base import BaseRagasEmbeddings
from ragas.llms import llm_factory
from ragas.metrics import AnswerRelevancy, ContextPrecision, Faithfulness
from ragas.run_config import RunConfig


EVALUATION_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CASES = EVALUATION_ROOT / "datasets" / "garden_rag_cases.json"
DEFAULT_REPORTS = EVALUATION_ROOT / "reports"


class LegacyGeminiEmbeddings(BaseRagasEmbeddings):
    """Expose google-genai embeddings through the interface used by RAGAS 0.4 metrics."""

    def __init__(self, client: genai.Client, model: str):
        super().__init__()
        self.client = client
        self.model = model

    def embed_documents(self, texts: list[str]) -> list[list[float]]:
        response = self.client.models.embed_content(model=self.model, contents=texts)
        return [embedding.values for embedding in response.embeddings]

    def embed_query(self, text: str) -> list[float]:
        return self.embed_documents([text])[0]

    async def aembed_documents(self, texts: list[str]) -> list[list[float]]:
        return await asyncio.to_thread(self.embed_documents, texts)

    async def aembed_query(self, text: str) -> list[float]:
        return await asyncio.to_thread(self.embed_query, text)


def call_chat(backend_url: str, question: str, game_state: dict) -> dict:
    payload = json.dumps(
        {"player_query": question, "game_state": game_state}
    ).encode("utf-8")
    chat_request = request.Request(
        f"{backend_url.rstrip('/')}/chat",
        data=payload,
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    with request.urlopen(chat_request, timeout=60) as response:
        return json.loads(response.read().decode("utf-8"))


def collect_dataset(
    cases: list[dict], backend_url: str, request_delay: float
) -> tuple[Dataset, list[dict]]:
    rows = []
    raw_results = []
    for case in cases:
        response = call_chat(backend_url, case["question"], case["game_state"])
        contexts = response.get("retrieved_context", [])
        row = {
            "question": case["question"],
            "answer": response["npc_response"],
            "contexts": [
                f"Authoritative game state:\n{json.dumps(case['game_state'], sort_keys=True)}",
                *contexts,
            ],
            "ground_truth": case["ground_truth"],
        }
        rows.append(row)
        raw_results.append(
            {
                **case,
                **response,
                "expected_context_terms_found": [
                    term
                    for term in case.get("expected_context_terms", [])
                    if term.lower() in "\n".join(contexts).lower()
                ],
            }
        )
        if request_delay > 0 and case is not cases[-1]:
            time.sleep(request_delay)
    return Dataset.from_list(rows), raw_results


def dataset_from_raw(raw_results: list[dict]) -> Dataset:
    return Dataset.from_list(
        [
            {
                "question": row["question"],
                "answer": row["npc_response"],
                "contexts": [
                    "Authoritative game state:\n"
                    + json.dumps(row["game_state"], sort_keys=True),
                    *row.get("retrieved_context", []),
                ],
                "ground_truth": row["ground_truth"],
            }
            for row in raw_results
        ]
    )


def main() -> None:
    parser = argparse.ArgumentParser(description="Evaluate Rock's live RAG responses with RAGAS.")
    parser.add_argument("--backend-url", default="http://localhost:5000")
    parser.add_argument("--cases", type=Path, default=DEFAULT_CASES)
    parser.add_argument("--reports", type=Path, default=DEFAULT_REPORTS)
    parser.add_argument(
        "--reuse-raw",
        action="store_true",
        help="Judge latest_raw.json without calling the live backend again.",
    )
    parser.add_argument(
        "--collect-only",
        action="store_true",
        help="Collect and save live responses without running RAGAS judges.",
    )
    parser.add_argument(
        "--request-delay",
        type=float,
        default=5.0,
        help="Seconds between live backend requests (free-tier-safe default: 5).",
    )
    parser.add_argument(
        "--judge-warmup",
        type=float,
        default=60.0,
        help="Seconds to wait between response collection and judging.",
    )
    parser.add_argument(
        "--case-delay",
        type=float,
        default=65.0,
        help="Seconds between per-case RAGAS judging batches.",
    )
    parser.add_argument(
        "--case-retries",
        type=int,
        default=3,
        help="Whole-case retries for temporary judge failures.",
    )
    parser.add_argument(
        "--retry-delay",
        type=float,
        default=65.0,
        help="Seconds to wait before retrying a failed judge case.",
    )
    args = parser.parse_args()

    api_key = os.getenv("GEMINI_API_KEY") or os.getenv("GOOGLE_API_KEY")
    if not api_key:
        raise SystemExit("GEMINI_API_KEY or GOOGLE_API_KEY is required for RAGAS evaluation.")

    args.reports.mkdir(parents=True, exist_ok=True)
    raw_path = args.reports / "latest_raw.json"
    if args.reuse_raw:
        if not raw_path.exists():
            raise SystemExit(f"Cannot reuse missing raw report: {raw_path}")
        raw_results = json.loads(raw_path.read_text(encoding="utf-8"))
        dataset = dataset_from_raw(raw_results)
    else:
        cases = json.loads(args.cases.read_text(encoding="utf-8"))
        dataset, raw_results = collect_dataset(
            cases, args.backend_url, args.request_delay
        )
        raw_path.write_text(
            json.dumps(raw_results, indent=2),
            encoding="utf-8",
        )
    if args.collect_only:
        print(json.dumps({"collected_cases": len(raw_results)}, indent=2))
        return

    client = genai.Client(api_key=api_key)
    generator_model = os.getenv("GEMINI_MODEL", "gemini-3.1-flash-lite")
    model = os.getenv("RAGAS_GEMINI_MODEL", "gemini-3.5-flash")
    if model == generator_model:
        raise SystemExit(
            "RAGAS_GEMINI_MODEL must differ from GEMINI_MODEL so the LLM-as-a-Judge "
            "evaluation is independent from NPC generation."
        )
    evaluator_llm = llm_factory(
        model, provider="google", client=client, max_retries=3
    )
    evaluator_embeddings = LegacyGeminiEmbeddings(
        client,
        os.getenv("GEMINI_EMBEDDING_MODEL", "gemini-embedding-001"),
    )
    metrics = [
        ContextPrecision(llm=evaluator_llm),
        Faithfulness(llm=evaluator_llm),
        AnswerRelevancy(
            llm=evaluator_llm, embeddings=evaluator_embeddings, strictness=1
        ),
    ]
    run_config = RunConfig(timeout=120, max_retries=3, max_workers=1)
    if args.judge_warmup > 0:
        time.sleep(args.judge_warmup)

    partial_path = args.reports / "latest_scores.partial.csv"
    score_frames = []
    start_index = 0
    if partial_path.exists():
        partial_scores = pd.read_csv(partial_path)
        expected_questions = list(dataset["question"][: len(partial_scores)])
        if list(partial_scores.get("user_input", partial_scores.get("question", []))) == expected_questions:
            score_frames.append(partial_scores)
            start_index = len(partial_scores)
            print(json.dumps({"resuming_from_case": start_index}, indent=2))

    for index in range(start_index, len(dataset)):
        last_error = None
        for attempt in range(1, max(1, args.case_retries) + 1):
            try:
                result = evaluate(
                    dataset.select([index]),
                    metrics=metrics,
                    embeddings=evaluator_embeddings,
                    run_config=run_config,
                    raise_exceptions=True,
                )
                score_frames.append(result.to_pandas())
                last_error = None
                break
            except Exception as error:
                last_error = error
                if attempt >= max(1, args.case_retries):
                    raise
                print(
                    json.dumps(
                        {
                            "case": index,
                            "attempt": attempt,
                            "retrying_after_seconds": args.retry_delay,
                            "error": str(error),
                        },
                        indent=2,
                    )
                )
                time.sleep(args.retry_delay)
        if last_error is not None:
            raise last_error
        pd.concat(score_frames, ignore_index=True).to_csv(
            partial_path, index=False
        )
        if args.case_delay > 0 and index < len(dataset) - 1:
            time.sleep(args.case_delay)

    scores = pd.concat(score_frames, ignore_index=True)
    scores.to_csv(args.reports / "latest_scores.csv", index=False)
    if partial_path.exists():
        partial_path.unlink()
    metric_names = ["context_precision", "faithfulness", "answer_relevancy"]
    summary = {
        name: round(float(scores[name].mean()), 6)
        for name in metric_names
        if name in scores.columns and not math.isnan(float(scores[name].mean()))
    }
    summary["judge_model"] = model
    summary["generator_model"] = generator_model
    summary["evaluated_cases"] = len(scores)
    (args.reports / "latest_summary.json").write_text(
        json.dumps(summary, indent=2),
        encoding="utf-8",
    )
    print(json.dumps(summary, indent=2))


if __name__ == "__main__":
    main()

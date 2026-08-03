# Evaluation

This folder contains RAGAS evaluation materials.

Suggested organization:

- `datasets/`: generated or curated evaluation questions and expected contexts.
- `scripts/`: scripts for running RAGAS evaluation.
- `reports/`: generated metric reports and analysis.

Primary metrics planned in the proposal:

- Faithfulness
- Answer relevance
- Context precision

## Dataset And Runner

`datasets/garden_rag_cases.json` contains 16 cases covering all core riddle types,
changing procedural game states, six generated ordering-puzzle configurations,
incorrect attempts, a solved puzzle, zero clue points, and the completed game.

Install the evaluation-only dependencies and run the backend before evaluating:

```powershell
python -m pip install -r evaluation/requirements.txt
python evaluation/scripts/run_ragas_evaluation.py
```

To keep evaluation dependencies isolated, Docker can be used instead:

```powershell
docker build -t capstone-rag-npc-evaluation evaluation
docker run --rm --env-file .env `
  --add-host host.docker.internal:host-gateway `
  -v ${PWD}/evaluation/reports:/evaluation/reports `
  capstone-rag-npc-evaluation `
  python scripts/run_ragas_evaluation.py `
  --backend-url http://host.docker.internal:5000
```

The runner sends each case to the live `/chat` endpoint, then measures context
precision, faithfulness, and answer relevancy with Gemini-backed RAGAS metrics.
NPC generation defaults to `gemini-3.1-flash-lite`; the independent LLM-as-a-Judge
defaults to `gemini-3.5-flash`. Gemini Pro can be selected when the API
project has Pro quota. The runner aborts if `RAGAS_GEMINI_MODEL` and
`GEMINI_MODEL` match, preventing accidental self-evaluation.
Reports are written to `evaluation/reports/latest_raw.json`,
`latest_scores.csv`, and `latest_summary.json`.

The default delays serialize judging and stay below the Gemini free-tier request
limit. To re-judge saved live responses without spending clue-generation calls,
add `--reuse-raw`. Use `--collect-only` to refresh the live-response evidence
without immediately invoking the judges. A successful run removes the temporary
`latest_scores.partial.csv`; if judging is interrupted, that file preserves the
completed per-case scores for diagnosis.

Running the evaluator sends the test questions, retrieved knowledge-base chunks,
and generated Rock answers to Gemini. Do not run it with private content unless
that external processing is approved.

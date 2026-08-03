# Final Demo Script

Use this short sequence for the narrated capstone recording.

1. Show `docker compose up -d` and `GET /health` returning `ok`.
2. Open `GeneratedIsoGardenScene` and enter Play Mode.
3. Point out Red, Rock, the HUD riddle, progress, and clue-point counter.
4. Interact with a wrong target and show that progress does not advance.
5. Approach Rock, ask a question about the current riddle, and show the loading
   state, grounded response, and one clue point spent.
6. Follow the three procedurally selected targets. Show each correct interaction
   advancing the riddle and awarding a clue point.
7. Show Blue appearing and the completion state after the third target.
8. Exit Play Mode and show the clean Unity Console.
9. Briefly show `evaluation/reports/latest_summary.json`, identify Gemini 3.1
   Flash-Lite as the NPC generator and Gemini 3.5 Flash as the configured independent judge, and
   explain the sixteen-case RAGAS workflow and six generated-puzzle states. If a
   fresh score report has not been run, identify the displayed scores as the
   earlier ten-case baseline.

Do not display `.env` or the Gemini API key during recording.

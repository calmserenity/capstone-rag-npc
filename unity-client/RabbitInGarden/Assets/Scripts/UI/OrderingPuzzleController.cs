using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class OrderingPuzzleController : MonoBehaviour
{
    private readonly List<string> attempt = new List<string>();
    private readonly List<Button> symbolButtons = new List<Button>();
    private PuzzleManager puzzleManager;
    private GameObject puzzleRoot;
    private Text attemptText;
    private Text feedbackText;
    private PlayerMovement2D playerMovement;

    public static bool IsAnyOpen { get; private set; }

    public static void Open(PuzzleManager manager)
    {
        OrderingPuzzleController controller = FindFirstObjectByType<OrderingPuzzleController>(FindObjectsInactive.Include);
        if (controller == null)
        {
            controller = new GameObject("OrderingPuzzleController").AddComponent<OrderingPuzzleController>();
        }
        controller.Show(manager);
    }

    private void Show(PuzzleManager manager)
    {
        puzzleManager = manager;
        playerMovement = FindFirstObjectByType<PlayerMovement2D>();
        BuildUi();
        RestoreSavedAttempt();
        RefreshAttempt();
        feedbackText.text = attempt.Count > 0
            ? "Your previous order is restored. Clear it to try another order."
            : "Choose every symbol in order. Stuck? Leave and talk to Rock.";
        puzzleRoot.SetActive(true);
        IsAnyOpen = true;
        if (playerMovement != null) playerMovement.enabled = false;
    }

    private void BuildUi()
    {
        if (puzzleRoot != null) Destroy(puzzleRoot);

        Canvas canvas = new GameObject(
            "OrderingPuzzleCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.transform.SetParent(transform, false);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;

        puzzleRoot = new GameObject("OrderingPuzzlePanel", typeof(RectTransform), typeof(Image));
        puzzleRoot.transform.SetParent(canvas.transform, false);
        RectTransform panel = puzzleRoot.GetComponent<RectTransform>();
        Place(panel, 0.20f, 0.15f, 0.80f, 0.85f);
        puzzleRoot.GetComponent<Image>().color = new Color(0.07f, 0.08f, 0.06f, 0.96f);

        Text title = CreateText("Title", puzzleRoot.transform, "Garden Ordering Puzzle", 34, TextAnchor.MiddleCenter);
        Place(title.rectTransform, 0.06f, 0.86f, 0.94f, 0.97f);

        ActivePuzzleState state = puzzleManager.GetActivePuzzleState();
        string rules = string.Join("\n", state.constraints.Select((rule, index) => $"{index + 1}. {rule}"));
        Text rulesText = CreateText("Rules", puzzleRoot.transform, rules, 22, TextAnchor.UpperLeft);
        Place(rulesText.rectTransform, 0.08f, 0.53f, 0.92f, 0.84f);

        attemptText = CreateText("Attempt", puzzleRoot.transform, "", 24, TextAnchor.MiddleCenter);
        Place(attemptText.rectTransform, 0.08f, 0.40f, 0.92f, 0.51f);

        symbolButtons.Clear();
        float buttonWidth = 0.78f / state.symbols.Length;
        for (int i = 0; i < state.symbols.Length; i++)
        {
            string symbol = state.symbols[i];
            Button button = CreateButton($"Symbol_{symbol}", puzzleRoot.transform, ToTitle(symbol));
            Place(button.GetComponent<RectTransform>(), 0.10f + i * buttonWidth, 0.27f,
                0.10f + (i + 1) * buttonWidth - 0.01f, 0.38f);
            button.onClick.AddListener(() => SelectSymbol(symbol, button));
            symbolButtons.Add(button);
        }

        Button clear = CreateButton("Clear", puzzleRoot.transform, "Clear");
        Place(clear.GetComponent<RectTransform>(), 0.08f, 0.10f, 0.28f, 0.21f);
        clear.onClick.AddListener(ClearAttempt);

        Button leave = CreateButton("LeavePuzzle", puzzleRoot.transform, "Leave & Find Rock");
        Place(leave.GetComponent<RectTransform>(), 0.34f, 0.10f, 0.66f, 0.21f);
        leave.onClick.AddListener(LeavePuzzle);

        Button submit = CreateButton("Submit", puzzleRoot.transform, "Submit");
        Place(submit.GetComponent<RectTransform>(), 0.72f, 0.10f, 0.92f, 0.21f);
        submit.onClick.AddListener(SubmitAttempt);

        feedbackText = CreateText("Feedback", puzzleRoot.transform, "", 18, TextAnchor.MiddleCenter);
        Place(feedbackText.rectTransform, 0.08f, 0.01f, 0.92f, 0.09f);
    }

    private void SelectSymbol(string symbol, Button button)
    {
        attempt.Add(symbol);
        button.interactable = false;
        RefreshAttempt();
    }

    private void ClearAttempt()
    {
        attempt.Clear();
        foreach (Button button in symbolButtons) button.interactable = true;
        RefreshAttempt();
    }

    private void LeavePuzzle()
    {
        if (attempt.Count > 0)
        {
            puzzleManager.SaveActivePuzzleDraft(attempt.ToArray());
        }

        Close();
    }

    private void SubmitAttempt()
    {
        ActivePuzzleState state = puzzleManager.GetActivePuzzleState();
        if (attempt.Count != state.symbols.Length)
        {
            feedbackText.text = "Place every symbol before submitting.";
            return;
        }

        if (!puzzleManager.SubmitActivePuzzle(attempt.ToArray()))
        {
            ClearAttempt();
            feedbackText.text = "That order does not satisfy every rule. Study the chain and try again.";
            return;
        }
        Close();
    }

    private void RestoreSavedAttempt()
    {
        attempt.Clear();
        ActivePuzzleState state = puzzleManager.GetActivePuzzleState();
        if (state.player_attempt == null || state.player_attempt.Length == 0)
        {
            return;
        }

        foreach (string symbol in state.player_attempt)
        {
            Button button = symbolButtons.FirstOrDefault(candidate =>
                string.Equals(candidate.name, $"Symbol_{symbol}", System.StringComparison.Ordinal));
            if (button == null || !button.interactable)
            {
                continue;
            }

            attempt.Add(symbol);
            button.interactable = false;
        }
    }

    private void RefreshAttempt()
    {
        attemptText.text = attempt.Count == 0
            ? "Your order: (empty)"
            : $"Your order: {string.Join("  >  ", attempt.Select(ToTitle))}";
    }

    private void Close()
    {
        IsAnyOpen = false;
        if (playerMovement != null) playerMovement.enabled = true;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        IsAnyOpen = false;
    }

    private static Text CreateText(string name, Transform parent, string value, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.text = value;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, string label)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.GetComponent<Image>().color = new Color(0.67f, 0.50f, 0.27f, 1f);
        Text text = CreateText("Text", buttonObject.transform, label, 18, TextAnchor.MiddleCenter);
        Place(text.rectTransform, 0f, 0f, 1f, 1f);
        return buttonObject.GetComponent<Button>();
    }

    private static void Place(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = new Vector2(left, bottom);
        rect.anchorMax = new Vector2(right, top);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static string ToTitle(string value)
    {
        return string.Join(" ", value.Split(' ').Select(word => char.ToUpperInvariant(word[0]) + word.Substring(1)));
    }
}

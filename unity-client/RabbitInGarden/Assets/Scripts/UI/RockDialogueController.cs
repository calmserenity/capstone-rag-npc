using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

public class RockDialogueController : MonoBehaviour
{
    [SerializeField] private BackendClient backendClient;
    [SerializeField] private GameStateBuilder gameStateBuilder;
    [SerializeField] private GameObject dialogueRoot;
    [SerializeField] private InputField questionInput;
    [SerializeField] private Text responseText;
    [SerializeField] private Button askButton;
    [SerializeField] private Button closeButton;

    private PlayerMovement2D playerMovement;
    private string defaultResponse = "Rock hums quietly. Ask what the garden remembers.";
    private bool hasOpened;
    private static RockDialogueController activeController;

    public static bool IsAnyOpen => activeController != null
        && activeController.dialogueRoot != null
        && activeController.dialogueRoot.activeSelf;

    private void Awake()
    {
        activeController = this;
        EnsureDependencies();
        EnsureEventSystem();
        EnsureDialogueUi();

        if (askButton != null)
        {
            askButton.onClick.AddListener(AskRock);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseDialogue);
        }
    }

    private void Start()
    {
        if (!hasOpened)
        {
            CloseDialogue();
        }
    }

    private void Update()
    {
        if (dialogueRoot == null || !dialogueRoot.activeSelf)
        {
            return;
        }

        if (ClosePressed())
        {
            CloseDialogue();
        }
    }

    public static void OpenAny()
    {
        RockDialogueController controller = FindExistingController();
        if (controller == null)
        {
            GameObject controllerObject = new GameObject("RockDialogueController");
            controllerObject.AddComponent<BackendClient>();
            controllerObject.AddComponent<GameStateBuilder>();
            controller = controllerObject.AddComponent<RockDialogueController>();
        }

        controller.OpenDialogue();
    }

    public void OpenDialogue()
    {
        hasOpened = true;
        EnsureDependencies();
        EnsureEventSystem();
        EnsureDialogueUi();

        if (dialogueRoot != null)
        {
            dialogueRoot.SetActive(true);
        }

        activeController = this;
        SetInteractable(true);

        if (responseText != null && string.IsNullOrWhiteSpace(responseText.text))
        {
            responseText.text = defaultResponse;
        }

        if (questionInput != null)
        {
            StartCoroutine(FocusQuestionInputNextFrame());
        }

        SetPlayerMovement(false);
    }

    public void CloseDialogue()
    {
        if (dialogueRoot != null)
        {
            dialogueRoot.SetActive(false);
        }

        SetPlayerMovement(true);
    }

    public void AskRock()
    {
        if (backendClient == null || gameStateBuilder == null || questionInput == null)
        {
            SetResponse("Rock is not connected yet.");
            return;
        }

        string question = questionInput.text.Trim();
        if (string.IsNullOrEmpty(question))
        {
            SetResponse("Mmm... ask Rock with a few little words.");
            return;
        }

        if (!gameStateBuilder.CanAskRock)
        {
            SetResponse("Mmm... no whispers remain. Solve another garden clue first.");
            return;
        }

        SetInteractable(false);
        SetResponse("Rock is listening...");

        GameState gameState = gameStateBuilder.Build();
        StartCoroutine(backendClient.SendChat(
            question,
            gameState,
            response =>
            {
                if (response.clue_point_spent)
                {
                    gameStateBuilder.SpendCluePoint();
                }
                if (response.puzzle_hint_given)
                {
                    gameStateBuilder.RecordPuzzleHintGiven();
                }
                SetResponse(response.npc_response);
                SetInteractable(true);
            },
            error =>
            {
                SetResponse($"Rock cannot hear the garden right now. {error}");
                SetInteractable(true);
            }));
    }

    private static RockDialogueController FindExistingController()
    {
        RockDialogueController[] controllers = FindObjectsByType<RockDialogueController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        return controllers.Length > 0 ? controllers[0] : null;
    }

    private void EnsureDependencies()
    {
        if (backendClient == null)
        {
            backendClient = GetComponent<BackendClient>();
        }

        if (backendClient == null)
        {
            backendClient = gameObject.AddComponent<BackendClient>();
        }

        if (gameStateBuilder == null)
        {
            gameStateBuilder = GetComponent<GameStateBuilder>();
        }

        if (gameStateBuilder == null)
        {
            gameStateBuilder = gameObject.AddComponent<GameStateBuilder>();
        }

        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement2D>();
        }
    }

    private void EnsureDialogueUi()
    {
        if (dialogueRoot != null && questionInput != null && responseText != null && askButton != null)
        {
            return;
        }

        Canvas canvas = CreateCanvas();

        dialogueRoot = new GameObject("RockDialoguePanel", typeof(RectTransform), typeof(Image));
        dialogueRoot.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = dialogueRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.18f, 0.05f);
        panelRect.anchorMax = new Vector2(0.82f, 0.34f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = dialogueRoot.GetComponent<Image>();
        panelImage.color = new Color(0.08f, 0.07f, 0.06f, 0.88f);

        Text title = CreateText("Title", dialogueRoot.transform, "Rock", 26, FontStyle.Bold, TextAnchor.MiddleLeft);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.05f, 0.78f);
        titleRect.anchorMax = new Vector2(0.78f, 0.95f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        responseText = CreateText("ResponseText", dialogueRoot.transform, defaultResponse, 18, FontStyle.Normal, TextAnchor.UpperLeft);
        RectTransform responseRect = responseText.rectTransform;
        responseRect.anchorMin = new Vector2(0.05f, 0.32f);
        responseRect.anchorMax = new Vector2(0.95f, 0.76f);
        responseRect.offsetMin = Vector2.zero;
        responseRect.offsetMax = Vector2.zero;

        questionInput = CreateInput(dialogueRoot.transform);
        RectTransform inputRect = questionInput.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.05f, 0.08f);
        inputRect.anchorMax = new Vector2(0.76f, 0.25f);
        inputRect.offsetMin = Vector2.zero;
        inputRect.offsetMax = Vector2.zero;

        askButton = CreateButton("AskButton", dialogueRoot.transform, "Ask");
        RectTransform askRect = askButton.GetComponent<RectTransform>();
        askRect.anchorMin = new Vector2(0.78f, 0.08f);
        askRect.anchorMax = new Vector2(0.92f, 0.25f);
        askRect.offsetMin = Vector2.zero;
        askRect.offsetMax = Vector2.zero;

        closeButton = CreateButton("CloseButton", dialogueRoot.transform, "x");
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.91f, 0.80f);
        closeRect.anchorMax = new Vector2(0.96f, 0.94f);
        closeRect.offsetMin = Vector2.zero;
        closeRect.offsetMax = Vector2.zero;
    }

    private Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("RockDialogueCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;

        return canvas;
    }

    private InputField CreateInput(Transform parent)
    {
        GameObject inputObject = new GameObject("QuestionInput", typeof(RectTransform), typeof(Image), typeof(InputField));
        inputObject.transform.SetParent(parent, false);

        Image background = inputObject.GetComponent<Image>();
        background.color = new Color(0.94f, 0.91f, 0.84f, 0.96f);

        Text text = CreateText("Text", inputObject.transform, "", 18, FontStyle.Normal, TextAnchor.MiddleLeft);
        text.color = new Color(0.09f, 0.08f, 0.06f, 1f);
        SetStretch(text.rectTransform, 16f, 6f, 16f, 6f);

        Text placeholder = CreateText("Placeholder", inputObject.transform, "Ask Rock...", 18, FontStyle.Italic, TextAnchor.MiddleLeft);
        placeholder.color = new Color(0.33f, 0.30f, 0.24f, 0.62f);
        SetStretch(placeholder.rectTransform, 16f, 6f, 16f, 6f);

        InputField input = inputObject.GetComponent<InputField>();
        input.textComponent = text;
        input.placeholder = placeholder;
        input.lineType = InputField.LineType.SingleLine;
        input.onEndEdit.AddListener(value =>
        {
            if (SubmitPressed())
            {
                AskRock();
            }
        });

        return input;
    }

    private Button CreateButton(string name, Transform parent, string label)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.72f, 0.55f, 0.31f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.72f, 0.55f, 0.31f, 1f);
        colors.highlightedColor = new Color(0.86f, 0.68f, 0.39f, 1f);
        colors.pressedColor = new Color(0.55f, 0.39f, 0.20f, 1f);
        colors.disabledColor = new Color(0.30f, 0.28f, 0.24f, 0.75f);
        button.colors = colors;

        Text text = CreateText("Text", buttonObject.transform, label, 18, FontStyle.Bold, TextAnchor.MiddleCenter);
        text.color = Color.white;
        SetStretch(text.rectTransform, 0f, 0f, 0f, 0f);

        return button;
    }

    private Text CreateText(string name, Transform parent, string value, int fontSize, FontStyle style, TextAnchor anchor)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.text = value;
        text.font = GetDefaultFont();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = anchor;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;

        Shadow shadow = textObject.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);

        return text;
    }

    private static Font GetDefaultFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return font;
    }

    private static void SetStretch(RectTransform rectTransform, float left, float bottom, float right, float top)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(left, bottom);
        rectTransform.offsetMax = new Vector2(-right, -top);
    }

    private void EnsureEventSystem()
    {
        GameObject eventSystemObject;
        if (EventSystem.current != null)
        {
            eventSystemObject = EventSystem.current.gameObject;
        }
        else
        {
            eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            EventSystem.current = eventSystemObject.GetComponent<EventSystem>();
        }

#if ENABLE_INPUT_SYSTEM
        StandaloneInputModule oldInputModule = eventSystemObject.GetComponent<StandaloneInputModule>();
        if (oldInputModule != null)
        {
            Destroy(oldInputModule);
        }

        if (eventSystemObject.GetComponent<InputSystemUIInputModule>() == null)
        {
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }
#else
        if (eventSystemObject.GetComponent<StandaloneInputModule>() == null)
        {
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }
#endif
    }

    private IEnumerator FocusQuestionInputNextFrame()
    {
        yield return null;

        if (questionInput == null || dialogueRoot == null || !dialogueRoot.activeSelf)
        {
            yield break;
        }

        Canvas.ForceUpdateCanvases();

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(questionInput.gameObject);
        }

        questionInput.Select();
        questionInput.ActivateInputField();
    }

    private void SetPlayerMovement(bool enabled)
    {
        if (playerMovement != null)
        {
            playerMovement.enabled = enabled;
        }
    }

    private void SetResponse(string message)
    {
        if (responseText != null)
        {
            responseText.text = message;
        }
    }

    private void SetInteractable(bool value)
    {
        if (askButton != null)
        {
            askButton.interactable = value;
        }

        if (questionInput != null)
        {
            questionInput.interactable = value;
        }
    }

    private static bool SubmitPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
    }

    private static bool ClosePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}

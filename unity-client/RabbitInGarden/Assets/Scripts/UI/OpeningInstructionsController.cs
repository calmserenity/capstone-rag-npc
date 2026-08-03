using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

public class OpeningInstructionsController : MonoBehaviour
{
    private GameObject popupRoot;
    private Button beginButton;
    private PlayerMovement2D playerMovement;
    private PlayerInteractor2D playerInteractor;
    private bool movementWasEnabled;
    private bool interactionWasEnabled;

    public bool IsOpen => popupRoot != null && popupRoot.activeSelf;

    [RuntimeInitializeOnLoadMethod]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<OpeningInstructionsController>() != null)
        {
            return;
        }

        GameObject controllerObject = new GameObject("OpeningInstructionsController");
        DontDestroyOnLoad(controllerObject);
        controllerObject.AddComponent<OpeningInstructionsController>();
    }

    private void Awake()
    {
        EnsureEventSystem();
        BuildPopup();
    }

    private void Start()
    {
        Show();
    }

    private void Update()
    {
        if (IsOpen && ConfirmPressed())
        {
            Dismiss();
        }
    }

    public void Show()
    {
        if (popupRoot == null)
        {
            BuildPopup();
        }

        CaptureAndBlockPlayerInput();
        popupRoot.SetActive(true);

        if (EventSystem.current != null && beginButton != null)
        {
            EventSystem.current.SetSelectedGameObject(beginButton.gameObject);
        }
    }

    public void Dismiss()
    {
        if (!IsOpen)
        {
            return;
        }

        popupRoot.SetActive(false);
        RestorePlayerInput();
    }

    private void BuildPopup()
    {
        if (popupRoot != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject(
            "OpeningInstructionsCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        popupRoot = new GameObject(
            "OpeningInstructionsPopup",
            typeof(RectTransform),
            typeof(Image));
        popupRoot.transform.SetParent(canvasObject.transform, false);
        Stretch(popupRoot.GetComponent<RectTransform>());
        popupRoot.GetComponent<Image>().color = new Color(0.025f, 0.035f, 0.025f, 0.78f);

        GameObject panel = new GameObject(
            "InstructionCard",
            typeof(RectTransform),
            typeof(Image),
            typeof(Outline));
        panel.transform.SetParent(popupRoot.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.22f, 0.18f);
        panelRect.anchorMax = new Vector2(0.78f, 0.82f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.10f, 0.98f);

        Outline panelOutline = panel.GetComponent<Outline>();
        panelOutline.effectColor = new Color(0.88f, 0.68f, 0.32f, 0.95f);
        panelOutline.effectDistance = new Vector2(4f, -4f);

        GameObject stepBadge = new GameObject(
            "StepBadge",
            typeof(RectTransform),
            typeof(Image));
        stepBadge.transform.SetParent(panel.transform, false);
        Place(stepBadge.GetComponent<RectTransform>(), 0.07f, 0.83f, 0.28f, 0.94f);
        stepBadge.GetComponent<Image>().color = new Color(0.84f, 0.58f, 0.22f, 1f);
        Text badgeText = CreateText(
            "StepBadgeText",
            stepBadge.transform,
            "YOUR FIRST STEP",
            19,
            FontStyle.Bold,
            TextAnchor.MiddleCenter);
        Stretch(badgeText.rectTransform, 10f, 4f, 10f, 4f);

        Text title = CreateText(
            "GoalTitle",
            panel.transform,
            "Find Blue",
            48,
            FontStyle.Bold,
            TextAnchor.MiddleLeft);
        title.color = new Color(1f, 0.92f, 0.60f, 1f);
        Place(title.rectTransform, 0.07f, 0.68f, 0.93f, 0.82f);

        Text instruction = CreateText(
            "InstructionText",
            panel.transform,
            "Blue is hiding somewhere in the garden.\n\n"
            + "Find Rock and talk to him first. Rock will guide you with riddles "
            + "and give hints when a puzzle gets tricky.",
            27,
            FontStyle.Normal,
            TextAnchor.UpperLeft);
        instruction.color = new Color(0.96f, 0.95f, 0.86f, 1f);
        Place(instruction.rectTransform, 0.07f, 0.36f, 0.93f, 0.67f);

        GameObject controlsPanel = new GameObject(
            "ControlsPanel",
            typeof(RectTransform),
            typeof(Image));
        controlsPanel.transform.SetParent(panel.transform, false);
        Place(controlsPanel.GetComponent<RectTransform>(), 0.07f, 0.22f, 0.93f, 0.34f);
        controlsPanel.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.05f, 0.82f);
        Text controls = CreateText(
            "ControlsText",
            controlsPanel.transform,
            "MOVE:  WASD / ARROW KEYS       TALK & INTERACT:  E",
            20,
            FontStyle.Bold,
            TextAnchor.MiddleCenter);
        controls.color = new Color(0.84f, 0.90f, 0.72f, 1f);
        Stretch(controls.rectTransform, 12f, 4f, 12f, 4f);

        beginButton = CreateButton(panel.transform, "Begin Search");
        Place(beginButton.GetComponent<RectTransform>(), 0.32f, 0.06f, 0.68f, 0.18f);
        beginButton.onClick.AddListener(Dismiss);

        popupRoot.SetActive(false);
    }

    private void CaptureAndBlockPlayerInput()
    {
        playerMovement = FindAnyObjectByType<PlayerMovement2D>();
        if (playerMovement != null)
        {
            movementWasEnabled = playerMovement.enabled;
            playerMovement.enabled = false;
        }

        playerInteractor = FindAnyObjectByType<PlayerInteractor2D>();
        if (playerInteractor != null)
        {
            interactionWasEnabled = playerInteractor.enabled;
            playerInteractor.enabled = false;
        }
    }

    private void RestorePlayerInput()
    {
        if (playerMovement != null)
        {
            playerMovement.enabled = movementWasEnabled;
        }

        if (playerInteractor != null)
        {
            playerInteractor.enabled = interactionWasEnabled;
        }
    }

    private static Button CreateButton(Transform parent, string label)
    {
        GameObject buttonObject = new GameObject(
            "BeginSearchButton",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(Outline));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.77f, 0.48f, 0.16f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.77f, 0.48f, 0.16f, 1f);
        colors.highlightedColor = new Color(1f, 0.72f, 0.27f, 1f);
        colors.selectedColor = new Color(1f, 0.72f, 0.27f, 1f);
        colors.pressedColor = new Color(0.58f, 0.33f, 0.10f, 1f);
        button.colors = colors;

        Outline outline = buttonObject.GetComponent<Outline>();
        outline.effectColor = new Color(1f, 0.92f, 0.62f, 0.95f);
        outline.effectDistance = new Vector2(3f, -3f);

        Text buttonText = CreateText(
            "ButtonText",
            buttonObject.transform,
            label,
            25,
            FontStyle.Bold,
            TextAnchor.MiddleCenter);
        Stretch(buttonText.rectTransform, 8f, 4f, 8f, 4f);
        return button;
    }

    private static Text CreateText(
        string name,
        Transform parent,
        string value,
        int fontSize,
        FontStyle style,
        TextAnchor alignment)
    {
        GameObject textObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Text),
            typeof(Shadow));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.text = value;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;

        Shadow shadow = textObject.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(2f, -2f);
        return text;
    }

    private static void Place(
        RectTransform rect,
        float left,
        float bottom,
        float right,
        float top)
    {
        rect.anchorMin = new Vector2(left, bottom);
        rect.anchorMax = new Vector2(right, top);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Stretch(
        RectTransform rect,
        float left = 0f,
        float bottom = 0f,
        float right = 0f,
        float top = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void EnsureEventSystem()
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

    private static bool ConfirmPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null
            && (keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame
                || keyboard.spaceKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.KeypadEnter)
            || Input.GetKeyDown(KeyCode.Space);
#endif
    }
}

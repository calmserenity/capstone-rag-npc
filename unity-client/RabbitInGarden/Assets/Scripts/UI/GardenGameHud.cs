using UnityEngine;
using UnityEngine.UI;

public class GardenGameHud : MonoBehaviour
{
    private PuzzleManager puzzleManager;
    private GameStateBuilder gameStateBuilder;
    private Text statusText;
    private Text bannerText;
    private GameObject blue;

    private void Awake()
    {
        puzzleManager = FindAnyObjectByType<PuzzleManager>();
        gameStateBuilder = FindAnyObjectByType<GameStateBuilder>();
        CreateHud();

        if (puzzleManager != null)
        {
            puzzleManager.PuzzleCompleted += RevealBlue;
        }
    }

    private void OnDestroy()
    {
        if (puzzleManager != null)
        {
            puzzleManager.PuzzleCompleted -= RevealBlue;
        }
    }

    private void Update()
    {
        if (puzzleManager == null || gameStateBuilder == null || statusText == null)
        {
            return;
        }

        GameState state = gameStateBuilder.Build();
        if (state.blue_found)
        {
            statusText.text = $"Blue found!  |  Clue points: {state.clue_points}";
            return;
        }

        int shownIndex = Mathf.Min(state.current_hint_index + 1, Mathf.Max(1, state.max_hints));
        statusText.text =
            $"Garden clue {shownIndex}/{state.max_hints}  |  Rock whispers: {state.clue_points}\n" +
            $"Riddle: {state.current_riddle}";
    }

    private void CreateHud()
    {
        GameObject canvasObject = new GameObject(
            "GardenGameHudCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;

        GameObject panel = new GameObject("RiddlePanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.02f, 0.82f);
        panelRect.anchorMax = new Vector2(0.52f, 0.97f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.05f, 0.82f);

        statusText = CreateText("RiddleStatus", panel.transform, 22, TextAnchor.MiddleLeft);
        statusText.rectTransform.anchorMin = Vector2.zero;
        statusText.rectTransform.anchorMax = Vector2.one;
        statusText.rectTransform.offsetMin = new Vector2(24f, 12f);
        statusText.rectTransform.offsetMax = new Vector2(-24f, -12f);

        bannerText = CreateText("CompletionBanner", canvasObject.transform, 42, TextAnchor.MiddleCenter);
        bannerText.rectTransform.anchorMin = new Vector2(0.18f, 0.42f);
        bannerText.rectTransform.anchorMax = new Vector2(0.82f, 0.60f);
        bannerText.rectTransform.offsetMin = Vector2.zero;
        bannerText.rectTransform.offsetMax = Vector2.zero;
        bannerText.color = new Color(1f, 0.92f, 0.55f, 1f);
        bannerText.gameObject.SetActive(false);
    }

    private static Text CreateText(string name, Transform parent, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        Shadow shadow = textObject.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
        shadow.effectDistance = new Vector2(2f, -2f);
        return text;
    }

    private void RevealBlue()
    {
        if (blue != null)
        {
            return;
        }

        GameObject red = GameObject.Find("Rabbit_Center");
        if (red == null)
        {
            Debug.LogWarning("[Puzzle] Blue could not be revealed because Rabbit_Center was not found.");
            return;
        }

        blue = Instantiate(red);
        blue.name = "Blue";
        blue.transform.SetParent(red.transform.parent, true);

        GardenInteractable[] interactables = FindObjectsByType<GardenInteractable>();
        GardenInteractable gate = System.Array.Find(
            interactables,
            item => item.PuzzleLocationId == "garden_gate");
        Vector3 revealPosition = gate != null
            ? gate.transform.position + new Vector3(0.45f, 0.12f, 0f)
            : red.transform.position + new Vector3(0.8f, 0.2f, 0f);
        blue.transform.position = revealPosition;

        SpriteRenderer renderer = blue.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = new Color(0.38f, 0.72f, 1f, 1f);
            renderer.sortingOrder += 1;
        }

        DisableComponent<PlayerMovement2D>(blue);
        DisableComponent<PlayerInteractor2D>(blue);
        DisableComponent<PlayerInteraction2D>(blue);
        DisableComponent<RabbitSpriteAnimator>(blue);

        Rigidbody2D body = blue.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.simulated = false;
        }

        Collider2D collider = blue.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        bannerText.text = "You found Blue!\nThe garden remembers your pawprints.";
        bannerText.gameObject.SetActive(true);
        Debug.Log("[Puzzle] Blue was revealed at the garden gate.");
    }

    private static void DisableComponent<T>(GameObject target) where T : Behaviour
    {
        T component = target.GetComponent<T>();
        if (component != null)
        {
            component.enabled = false;
        }
    }
}

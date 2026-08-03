using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerInteractor2D : MonoBehaviour
{
    [SerializeField, Tooltip("Used only when an interactable has no valid object-specific radius.")]
    private float interactionRadius = 0.65f;
    [SerializeField] private Vector3 promptOffset = new Vector3(0f, 0.28f, 0f);
    [SerializeField] private int promptSortingOrder = 100;
#if !ENABLE_INPUT_SYSTEM
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
#endif

    private GardenInteractable currentTarget;
    private GardenInteractable[] interactables;
    private GameObject promptRoot;
    private TextMesh promptShadow;
    private TextMesh promptLabel;
    private float refreshTimer;

    private void Awake()
    {
        CreatePrompt();
        RefreshInteractables();
    }

    private void Update()
    {
        if (RockDialogueController.IsAnyOpen || OrderingPuzzleController.IsAnyOpen)
        {
            currentTarget = null;
            UpdatePrompt();
            return;
        }

        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f)
        {
            RefreshInteractables();
        }

        GardenInteractable nearest = FindNearestInteractable();
        bool interactPressed = InteractPressed();

        if (nearest != currentTarget)
        {
            currentTarget = nearest;
        }

        UpdatePrompt();

        if (currentTarget != null && interactPressed)
        {
            currentTarget.Interact(gameObject);
        }
        else if (currentTarget == null && interactPressed)
        {
            Debug.Log("[Interaction] Failed: no interactable object nearby.");
        }
    }

    private GardenInteractable FindNearestInteractable()
    {
        GardenInteractable nearest = null;
        float nearestDistance = float.MaxValue;

        if (interactables == null)
        {
            return null;
        }

        for (int i = 0; i < interactables.Length; i++)
        {
            GardenInteractable candidate = interactables[i];
            if (candidate == null || !candidate.isActiveAndEnabled)
            {
                continue;
            }

            float distance = Vector2.Distance(transform.position, candidate.transform.position);
            float candidateRadius = candidate.InteractionRadius > 0f
                ? candidate.InteractionRadius
                : interactionRadius;
            if (distance > candidateRadius)
            {
                continue;
            }

            if (distance < nearestDistance)
            {
                nearest = candidate;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private void UpdatePrompt()
    {
        if (promptRoot == null)
        {
            return;
        }

        if (currentTarget == null)
        {
            promptRoot.SetActive(false);
            return;
        }

        promptRoot.SetActive(true);
        promptRoot.transform.position = currentTarget.transform.position + promptOffset;

        string promptText = $"E - {currentTarget.interactionName}";
        if (promptShadow != null)
        {
            promptShadow.text = promptText;
        }

        if (promptLabel != null)
        {
            promptLabel.text = promptText;
        }
    }

    private void CreatePrompt()
    {
        promptRoot = new GameObject("InteractionPrompt_e");
        promptRoot.transform.localScale = Vector3.one;

        promptShadow = CreatePromptText("Shadow", Color.black, promptSortingOrder);
        promptShadow.transform.localPosition = new Vector3(0.01f, -0.01f, 0f);

        promptLabel = CreatePromptText("Label", Color.white, promptSortingOrder + 1);
        promptLabel.transform.localPosition = Vector3.zero;

        promptRoot.SetActive(false);
    }

    private TextMesh CreatePromptText(string name, Color color, int sortingOrder)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(promptRoot.transform, false);

        TextMesh text = textObject.AddComponent<TextMesh>();
        text.text = "E";
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.characterSize = 0.035f;
        text.fontSize = 42;
        text.color = color;

        MeshRenderer meshRenderer = textObject.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingOrder = sortingOrder;
        }

        return text;
    }

    private void RefreshInteractables()
    {
        interactables = FindObjectsByType<GardenInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        refreshTimer = 0.5f;
    }

    private void OnDestroy()
    {
        if (promptRoot != null)
        {
            Destroy(promptRoot);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }

    private bool InteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(interactionKey);
#endif
    }
}

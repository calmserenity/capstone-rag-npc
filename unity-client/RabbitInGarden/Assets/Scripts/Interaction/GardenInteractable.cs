using System;
using System.Text.RegularExpressions;
using UnityEngine;

public class GardenInteractable : MonoBehaviour
{
    public string interactionName = "Garden Object";
    [SerializeField] private string puzzleLocationId;
    [SerializeField, Min(0.05f)] private float interactionRadius = 0.65f;
    public bool canInteract = true;
    [TextArea]
    public string interactionMessage = "Red notices something in the garden.";
    public Color interactedColor = new Color(1f, 0.95f, 0.55f, 1f);

    private Color originalColor = Color.white;
    private SpriteRenderer spriteRenderer;
    private Renderer meshRenderer;
    private bool hasInteracted;

    public string PuzzleLocationId => ResolvePuzzleLocationId();
    public float InteractionRadius => Mathf.Max(0.05f, interactionRadius);

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        meshRenderer = GetComponent<Renderer>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        else if (meshRenderer != null)
        {
            originalColor = meshRenderer.sharedMaterial != null ? meshRenderer.sharedMaterial.color : Color.white;
        }
    }

    public bool Interact(GameObject interactor)
    {
        if (!canInteract)
        {
            Debug.Log($"[Interaction] Failed: {interactor.name} could not interact with {interactionName}.");
            return false;
        }

        hasInteracted = true;
        Debug.Log($"[Interaction] Success: {interactor.name} interacted with {interactionName}. {interactionMessage}");

        if (string.Equals(interactionName, "Rock", StringComparison.OrdinalIgnoreCase))
        {
            RockDialogueController.OpenAny();
        }
        else
        {
            PuzzleManager puzzleManager = FindFirstObjectByType<PuzzleManager>();
            if (puzzleManager == null)
            {
                GameObject managerObject = new GameObject("PuzzleManager");
                puzzleManager = managerObject.AddComponent<PuzzleManager>();
            }

            if (puzzleManager != null)
            {
                puzzleManager.TryDiscoverLocation(PuzzleLocationId);
            }
        }

        return true;
    }

    public bool Interact()
    {
        return Interact(gameObject);
    }

    public string GetPrompt()
    {
        return $"Press E to inspect {interactionName}";
    }

    private string ResolvePuzzleLocationId()
    {
        if (!string.IsNullOrWhiteSpace(puzzleLocationId))
        {
            return puzzleLocationId.Trim().ToLowerInvariant();
        }

        string source = string.Equals(interactionName, "Garden Object", StringComparison.OrdinalIgnoreCase)
            ? gameObject.name
            : interactionName;
        string normalized = Regex.Replace(source.ToLowerInvariant(), "[^a-z0-9]+", "_").Trim('_');

        if (normalized.Contains("sunflower")) return "sunflower_patch";
        if (normalized.Contains("flower")) return "flower_bed";
        if (normalized.Contains("watering")) return "watering_can";
        if (normalized.Contains("bird") || normalized.Contains("bath")) return "bird_bath";
        if (normalized.Contains("lantern") || normalized.Contains("lamp")) return "old_lantern";
        if (normalized.Contains("pond") || normalized.Contains("water")) return "pond";
        if (normalized.Contains("root") || normalized.Contains("tree")) return "tree_roots";
        if (normalized.Contains("stone") || normalized.Contains("path")) return "stone_path";
        if (normalized.Contains("bench")) return "bench";
        if (normalized.Contains("gate")) return "garden_gate";
        return normalized;
    }

    private void ApplyInteractedLook()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = interactedColor;
            return;
        }

        if (meshRenderer != null)
        {
            meshRenderer.material.color = interactedColor;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = hasInteracted ? interactedColor : originalColor;
        Gizmos.DrawWireSphere(transform.position, InteractionRadius);
    }
}

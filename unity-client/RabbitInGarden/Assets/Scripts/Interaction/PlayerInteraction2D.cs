using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerInteraction2D : MonoBehaviour
{
#if !ENABLE_INPUT_SYSTEM
    [SerializeField] private KeyCode interactKey = KeyCode.E;
#endif
    [SerializeField] private float interactionRadius = 1.1f;
    [SerializeField] private LayerMask interactableLayers = ~0;

    private void Update()
    {
        if (InteractPressed())
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        GardenInteractable nearest = FindNearestInteractable();
        if (nearest == null)
        {
            Debug.Log("Red reaches out, but there is nothing close enough to inspect.");
            return;
        }

        nearest.Interact(gameObject);
    }

    private GardenInteractable FindNearestInteractable()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactionRadius, interactableLayers);
        GardenInteractable nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            GardenInteractable interactable = hit.GetComponentInParent<GardenInteractable>();
            if (interactable == null)
            {
                continue;
            }

            float distance = Vector2.Distance(transform.position, interactable.transform.position);
            if (distance < nearestDistance)
            {
                nearest = interactable;
                nearestDistance = distance;
            }
        }

        return nearest;
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
        return Input.GetKeyDown(interactKey);
#endif
    }
}

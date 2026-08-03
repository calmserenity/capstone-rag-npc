using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private bool useIsometricMovement = true;
    [SerializeField] private bool constrainToGarden = true;
    [SerializeField] private Vector2 gardenCenter = new Vector2(-0.11f, 0.05f);
    [SerializeField] private Vector2 gardenHalfSize = new Vector2(3.35f, 1.32f);
    [SerializeField] private bool spawnAtStartPosition = true;
    [SerializeField] private Vector2 startPosition = new Vector2(1.15f, 0.82f);
    [SerializeField] private float collisionSkin = 0.02f;
    [SerializeField] private bool usePathAwareMovement = true;
    [SerializeField] private string pathObjectNameToken = "StonePath";
    [SerializeField] private float pathAssistRadius = 0.48f;
    [SerializeField, Range(0f, 1f)] private float pathAssistStrength = 0.25f;
    [SerializeField] private float pathNeighborRadius = 0.86f;
    [SerializeField] private float pathDirectionThreshold = 0.2f;

    private Rigidbody2D body;
    private Vector2 moveInput;
    private Vector2 moveDirection;
    private readonly RaycastHit2D[] collisionHits = new RaycastHit2D[8];
    private readonly List<Transform> pathNodes = new List<Transform>();

    public Vector2 MoveInput => moveInput;
    public Vector2 MoveDirection => moveDirection;
    public bool IsMoving => moveInput.sqrMagnitude > 0.01f;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        SpawnAtStartPosition();
        CachePathNodes();
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        moveInput = ReadKeyboardInput();
#else
        moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")).normalized;

        if (moveInput == Vector2.zero)
        {
            moveInput = ReadKeyInput();
        }
#endif

        moveDirection = ResolveMoveDirection(moveInput);
    }

    private void FixedUpdate()
    {
        Vector2 movement = MoveDirection;
        Vector2 targetPosition = body.position + movement * moveSpeed * Time.fixedDeltaTime;

        if (constrainToGarden)
        {
            targetPosition = ClampToGarden(targetPosition);
        }

        Vector2 nextPosition = ResolveCollision(body.position, targetPosition);

        body.linearVelocity = Vector2.zero;
        body.MovePosition(nextPosition);
    }

    private void SpawnAtStartPosition()
    {
        if (!spawnAtStartPosition)
        {
            return;
        }

        Vector3 position = transform.position;
        position.x = startPosition.x;
        position.y = startPosition.y;
        transform.position = position;
        body.position = startPosition;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
    }

    private Vector2 ResolveMoveDirection(Vector2 input)
    {
        if (input == Vector2.zero)
        {
            return Vector2.zero;
        }

        Vector2 baseDirection = useIsometricMovement ? ToIsometricDirection(input) : input;

        if (TryGetPathAwareDirection(input, out Vector2 pathDirection))
        {
            float assistStrength = Mathf.Clamp01(pathAssistStrength);
            if (assistStrength <= 0f)
            {
                return baseDirection;
            }

            Vector2 assistedDirection = Vector2.Lerp(baseDirection, pathDirection, assistStrength);
            return assistedDirection.sqrMagnitude > Mathf.Epsilon ? assistedDirection.normalized : baseDirection;
        }

        return baseDirection;
    }

    private bool TryGetPathAwareDirection(Vector2 input, out Vector2 pathDirection)
    {
        pathDirection = Vector2.zero;

        if (!usePathAwareMovement)
        {
            return false;
        }

        if (pathNodes.Count == 0)
        {
            CachePathNodes();
        }

        if (pathNodes.Count == 0)
        {
            return false;
        }

        Transform nearestNode = FindNearestPathNode(body.position, pathAssistRadius);
        if (nearestNode == null)
        {
            return false;
        }

        Vector2 intent = GetScreenIntent(input);
        if (intent == Vector2.zero)
        {
            return false;
        }

        float bestScore = pathDirectionThreshold;
        Vector2 bestDirection = Vector2.zero;
        Vector2 currentPosition = nearestNode.position;

        for (int i = 0; i < pathNodes.Count; i++)
        {
            Transform node = pathNodes[i];
            if (node == null || node == nearestNode)
            {
                continue;
            }

            Vector2 delta = (Vector2)node.position - currentPosition;
            float distance = delta.magnitude;
            if (distance <= Mathf.Epsilon || distance > pathNeighborRadius)
            {
                continue;
            }

            Vector2 direction = delta / distance;
            float score = Vector2.Dot(direction, intent);
            if (score > bestScore)
            {
                bestScore = score;
                bestDirection = direction;
            }
        }

        if (bestDirection == Vector2.zero)
        {
            return false;
        }

        pathDirection = bestDirection;
        return true;
    }

    private Transform FindNearestPathNode(Vector2 position, float maxDistance)
    {
        Transform nearestNode = null;
        float nearestDistanceSqr = maxDistance * maxDistance;

        for (int i = 0; i < pathNodes.Count; i++)
        {
            Transform node = pathNodes[i];
            if (node == null)
            {
                continue;
            }

            float distanceSqr = ((Vector2)node.position - position).sqrMagnitude;
            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearestNode = node;
            }
        }

        return nearestNode;
    }

    private void CachePathNodes()
    {
        pathNodes.Clear();

        if (string.IsNullOrWhiteSpace(pathObjectNameToken))
        {
            return;
        }

        Transform[] sceneTransforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        for (int i = 0; i < sceneTransforms.Length; i++)
        {
            Transform sceneTransform = sceneTransforms[i];
            if (sceneTransform.name.IndexOf(pathObjectNameToken, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                pathNodes.Add(sceneTransform);
            }
        }
    }

    private Vector2 ResolveCollision(Vector2 currentPosition, Vector2 targetPosition)
    {
        Vector2 delta = targetPosition - currentPosition;
        float distance = delta.magnitude;

        if (distance <= Mathf.Epsilon)
        {
            return currentPosition;
        }

        ContactFilter2D filter = new ContactFilter2D
        {
            useTriggers = false,
            useLayerMask = false
        };

        int hitCount = body.Cast(delta / distance, filter, collisionHits, distance + collisionSkin);
        if (hitCount == 0)
        {
            return targetPosition;
        }

        float nearestDistance = distance;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = collisionHits[i];
            if (hit.collider == null || hit.collider.isTrigger)
            {
                continue;
            }

            nearestDistance = Mathf.Min(nearestDistance, Mathf.Max(0f, hit.distance - collisionSkin));
        }

        return currentPosition + delta.normalized * nearestDistance;
    }

    private Vector2 ClampToGarden(Vector2 position)
    {
        Vector2 offset = position - gardenCenter;
        float diamondDistance = Mathf.Abs(offset.x) / gardenHalfSize.x + Mathf.Abs(offset.y) / gardenHalfSize.y;

        if (diamondDistance <= 1f)
        {
            return position;
        }

        return gardenCenter + offset / diamondDistance;
    }

    private static Vector2 ToIsometricDirection(Vector2 input)
    {
        if (input == Vector2.zero)
        {
            return Vector2.zero;
        }

        Vector2 isometric = new Vector2(input.x - input.y, (input.x + input.y) * 0.5f);
        return isometric.normalized;
    }

    private static Vector2 GetScreenIntent(Vector2 input)
    {
        float horizontal = Mathf.Abs(input.x);
        float vertical = Mathf.Abs(input.y);

        if (vertical >= horizontal && vertical > 0f)
        {
            return new Vector2(0f, Mathf.Sign(input.y));
        }

        if (horizontal > 0f)
        {
            return new Vector2(Mathf.Sign(input.x), 0f);
        }

        return Vector2.zero;
    }

    private static Vector2 ReadKeyInput()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            horizontal -= 1f;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            horizontal += 1f;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            vertical -= 1f;
        }

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            vertical += 1f;
        }

        return new Vector2(horizontal, vertical).normalized;
    }

#if ENABLE_INPUT_SYSTEM
    private static Vector2 ReadKeyboardInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return Vector2.zero;
        }

        float horizontal = 0f;
        float vertical = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            horizontal -= 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            horizontal += 1f;
        }

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            vertical -= 1f;
        }

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            vertical += 1f;
        }

        return new Vector2(horizontal, vertical).normalized;
    }
#endif
}

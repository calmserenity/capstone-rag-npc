using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(SpriteRenderer))]
public class RabbitSpriteAnimator : MonoBehaviour
{
    [SerializeField] private PlayerMovement2D movement;
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] moveFrames;
    [SerializeField] private Sprite[] diagonalUpMoveFrames;
    [SerializeField] private Sprite[] diagonalDownMoveFrames;
    [SerializeField] private float idleFramesPerSecond = 2f;
    [SerializeField] private float moveFramesPerSecond = 8f;
    [SerializeField] private bool flipWithHorizontalInput = true;
    [SerializeField] private float facingDeadZone = 0.05f;
    [SerializeField] private float diagonalFrameThreshold = 0.15f;

    private const string FramePathPattern =
        "Assets/Art/GeneratedUsableIso/RabbitAnimation/IsoBlackRabbitRibbon_Frame_{0:00}.png";

    private SpriteRenderer spriteRenderer;
    private float frameTimer;
    private int frameIndex;
    private bool wasMoving;
    private Sprite[] activeFrames;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (movement == null)
        {
            movement = GetComponent<PlayerMovement2D>();
        }

        EnsureFramesLoaded();
        ApplyFrame();
    }

    private void Update()
    {
        bool isMoving = movement != null && movement.IsMoving;
        UpdateFacing();

        Sprite[] frames = GetFrames(isMoving);

        if (frames == null || frames.Length == 0)
        {
            return;
        }

        if (isMoving != wasMoving || frames != activeFrames)
        {
            wasMoving = isMoving;
            activeFrames = frames;
            frameIndex = 0;
            frameTimer = 0f;
            ApplyFrame();
        }

        float framesPerSecond = isMoving ? moveFramesPerSecond : idleFramesPerSecond;
        frameTimer += Time.deltaTime;

        if (frameTimer < 1f / framesPerSecond)
        {
            return;
        }

        frameTimer = 0f;
        frameIndex = (frameIndex + 1) % frames.Length;
        ApplyFrame();
    }

    private void ApplyFrame()
    {
        bool isMoving = movement != null && movement.IsMoving;
        Sprite[] frames = GetFrames(isMoving);

        if (frames == null || frames.Length == 0)
        {
            return;
        }

        spriteRenderer.sprite = frames[Mathf.Clamp(frameIndex, 0, frames.Length - 1)];
    }

    private void UpdateFacing()
    {
        if (!flipWithHorizontalInput || movement == null)
        {
            return;
        }

        Vector2 direction = movement.MoveDirection;
        if (Mathf.Abs(direction.x) > facingDeadZone)
        {
            spriteRenderer.flipX = direction.x > 0f;
        }
    }

    private Sprite[] GetFrames(bool isMoving)
    {
        if (!isMoving || movement == null)
        {
            return idleFrames;
        }

        Vector2 direction = movement.MoveDirection;
        if (direction.y > diagonalFrameThreshold && diagonalUpMoveFrames != null && diagonalUpMoveFrames.Length > 0)
        {
            return diagonalUpMoveFrames;
        }

        if (direction.y < -diagonalFrameThreshold && diagonalDownMoveFrames != null && diagonalDownMoveFrames.Length > 0)
        {
            return diagonalDownMoveFrames;
        }

        return moveFrames;
    }

    private void EnsureFramesLoaded()
    {
#if UNITY_EDITOR
        if (idleFrames == null || idleFrames.Length == 0)
        {
            idleFrames = LoadFrames(1, 3);
        }

        if (moveFrames == null || moveFrames.Length == 0)
        {
            moveFrames = LoadFrames(4, 10);
        }

        if (diagonalUpMoveFrames == null || diagonalUpMoveFrames.Length == 0)
        {
            diagonalUpMoveFrames = LoadDirectionalFrames("DiagonalUp", 1, 4);
        }

        if (diagonalDownMoveFrames == null || diagonalDownMoveFrames.Length == 0)
        {
            diagonalDownMoveFrames = LoadDirectionalFrames("DiagonalDown", 1, 4);
        }
#endif
    }

#if UNITY_EDITOR
    private static Sprite[] LoadFrames(int firstFrame, int lastFrame)
    {
        int count = lastFrame - firstFrame + 1;
        Sprite[] frames = new Sprite[count];

        for (int i = 0; i < count; i++)
        {
            string path = string.Format(FramePathPattern, firstFrame + i);
            frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        return frames;
    }

    private static Sprite[] LoadDirectionalFrames(string direction, int firstFrame, int lastFrame)
    {
        int count = lastFrame - firstFrame + 1;
        Sprite[] frames = new Sprite[count];

        for (int i = 0; i < count; i++)
        {
            string path = $"Assets/Art/GeneratedUsableIso/RabbitAnimation/IsoBlackRabbitRibbon_{direction}_Frame_{firstFrame + i:00}.png";
            frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        return frames;
    }
#endif
}

using UnityEngine;

/// <summary>
/// Animates the Golden Retriever sheet without duplicating the character movement logic.
/// The sheet contains 8 directions with 4 frames each: idle on the top row and walk below.
/// </summary>
public class DogSpriteAnimator : MonoBehaviour
{
    private const int DirectionCount = 8;
    private const int FramesPerDirection = 4;
    private const int CellSize = 32;

    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Texture2D spriteSheet;
    [SerializeField] private float idleFramesPerSecond = 4f;
    [SerializeField] private float walkFramesPerSecond = 10f;
    [SerializeField] private float pixelsPerUnit = 32f;

    private Sprite[] frames;
    private Vector2 movement;
    private int direction;
    private int frame;
    private float frameTimer;
    private bool wasMoving;

    private void Awake()
    {
        CreateFrames();
        ShowCurrentFrame();
    }

    private void LateUpdate()
    {
        bool isMoving = movement.sqrMagnitude > 0.01f;
        int newDirection = isMoving ? GetDirection(movement) : direction;

        if (newDirection != direction || isMoving != wasMoving)
        {
            direction = newDirection;
            frame = 0;
            frameTimer = 0f;
            wasMoving = isMoving;
            ShowCurrentFrame();
        }

        float framesPerSecond = isMoving ? walkFramesPerSecond : idleFramesPerSecond;
        if (framesPerSecond <= 0f)
        {
            return;
        }

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / framesPerSecond;
        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frame = (frame + 1) % FramesPerDirection;
            ShowCurrentFrame();
        }
    }

    public void SetMovement(Vector2 value)
    {
        movement = value;
    }

    private void CreateFrames()
    {
        if (spriteSheet == null || targetRenderer == null)
        {
            enabled = false;
            return;
        }

        frames = new Sprite[DirectionCount * FramesPerDirection * 2];

        for (int animationRow = 0; animationRow < 2; animationRow++)
        {
            // Texture coordinates start at the bottom. The source sheet has idle above walk.
            float y = animationRow == 0 ? CellSize : 0f;
            for (int directionIndex = 0; directionIndex < DirectionCount; directionIndex++)
            {
                for (int frameIndex = 0; frameIndex < FramesPerDirection; frameIndex++)
                {
                    int column = directionIndex * FramesPerDirection + frameIndex;
                    int index = GetFrameIndex(animationRow, directionIndex, frameIndex);
                    frames[index] = Sprite.Create(
                        spriteSheet,
                        new Rect(column * CellSize, y, CellSize, CellSize),
                        new Vector2(0.5f, 0f),
                        pixelsPerUnit,
                        0,
                        SpriteMeshType.FullRect);
                    frames[index].name = $"Dog_{(animationRow == 0 ? "Idle" : "Walk")}_{directionIndex}_{frameIndex}";
                }
            }
        }
    }

    private void ShowCurrentFrame()
    {
        if (frames == null || targetRenderer == null)
        {
            return;
        }

        int animationRow = wasMoving ? 1 : 0;
        targetRenderer.sprite = frames[GetFrameIndex(animationRow, direction, frame)];
    }

    private static int GetFrameIndex(int animationRow, int directionIndex, int frameIndex)
    {
        return animationRow * DirectionCount * FramesPerDirection
            + directionIndex * FramesPerDirection
            + frameIndex;
    }

    private static int GetDirection(Vector2 value)
    {
        bool left = value.x < -0.1f;
        bool right = value.x > 0.1f;
        bool down = value.y < -0.1f;
        bool up = value.y > 0.1f;

        if (down) return left ? 7 : right ? 1 : 0;
        if (up) return left ? 5 : right ? 3 : 4;
        return left ? 6 : 2;
    }

    private void OnDestroy()
    {
        if (frames == null)
        {
            return;
        }

        foreach (Sprite generatedFrame in frames)
        {
            if (generatedFrame != null)
            {
                Destroy(generatedFrame);
            }
        }
    }
}

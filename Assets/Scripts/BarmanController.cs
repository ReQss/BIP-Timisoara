using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator), typeof(Rigidbody2D))]
public sealed class BarmanController : MonoBehaviour
{
    public enum FaceDirection
    {
        SouthWest,
        SouthEast,
        NorthWest,
        NorthEast
    }

    [SerializeField] private FaceDirection startingDirection = FaceDirection.SouthEast;
    [SerializeField, Min(0f)] private float moveSpeed = 2.5f;

    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int Speed = Animator.StringToHash("Speed");

    private Animator animator;
    private Rigidbody2D body;
    private InputAction moveAction;
    private Vector2 movement;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        body = GetComponent<Rigidbody2D>();
        moveAction = new InputAction("Barman Move", InputActionType.Value);
        moveAction.AddBinding("<Gamepad>/leftStick");
        moveAction.AddBinding("<Gamepad>/dpad");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/i")
            .With("Down", "<Keyboard>/k")
            .With("Left", "<Keyboard>/j")
            .With("Right", "<Keyboard>/l");

        SetFacing(startingDirection);
        SetWalking(false);
    }

    private void OnEnable()
    {
        moveAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        movement = Vector2.zero;

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }

    private void OnDestroy()
    {
        moveAction?.Dispose();
    }

    private void Update()
    {
        movement = moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        movement = Vector2.ClampMagnitude(movement, 1f);

        if (movement.sqrMagnitude > 0.001f)
        {
            FaceDirection direction;
            if (movement.y > 0f)
            {
                direction = movement.x > 0f
                    ? FaceDirection.NorthEast
                    : FaceDirection.NorthWest;
            }
            else
            {
                direction = movement.x > 0f
                    ? FaceDirection.SouthEast
                    : FaceDirection.SouthWest;
            }

            SetFacing(direction);
        }

        SetWalking(movement.sqrMagnitude > 0.001f);
    }

    private void FixedUpdate()
    {
        body.linearVelocity = movement * moveSpeed;
    }

    public void SetFacing(FaceDirection direction)
    {
        startingDirection = direction;
        Vector2 facing = DirectionToVector(direction);

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        animator.SetFloat(MoveX, facing.x);
        animator.SetFloat(MoveY, facing.y);
    }

    public void SetWalking(bool walking)
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        animator.SetFloat(Speed, walking ? 1f : 0f);
    }

    private static Vector2 DirectionToVector(FaceDirection direction)
    {
        return direction switch
        {
            FaceDirection.SouthWest => new Vector2(-1f, -1f),
            FaceDirection.SouthEast => new Vector2(1f, -1f),
            FaceDirection.NorthWest => new Vector2(-1f, 1f),
            _ => new Vector2(1f, 1f)
        };
    }
}

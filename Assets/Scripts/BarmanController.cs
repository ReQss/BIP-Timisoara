using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator), typeof(Rigidbody2D))]
public sealed class BarmanController : MonoBehaviour, IBeverageCarrier
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
    [SerializeField, Min(0.25f)] private float customerInteractionDistance = 1.25f;

    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int Speed = Animator.StringToHash("Speed");

    private Animator animator;
    private DogSpriteAnimator dogSpriteAnimator;
    private Rigidbody2D body;
    private InputAction moveAction;
    private Vector2 movement;
    private BeverageDefinition heldBeverage;
    private SpriteRenderer heldBeverageRenderer;
    private bool fridgeMenuOpen;

    public BeverageType HeldBeverage => heldBeverage.type;
    public bool UsesCatControls => false;
    public Transform CarrierTransform => transform;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        dogSpriteAnimator = GetComponent<DogSpriteAnimator>();
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
        CreateHeldBeverageDisplay();
    }

    private void OnEnable()
    {
        moveAction?.Enable();
    }

    private void Start()
    {
        IgnoreCatCollisions();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        movement = Vector2.zero;

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }

        dogSpriteAnimator?.SetMovement(Vector2.zero);
    }

    private void OnDestroy()
    {
        moveAction?.Dispose();
    }

    private void Update()
    {
        if (GameManager.IsGameplayInputBlocked || fridgeMenuOpen)
        {
            movement = Vector2.zero;
            dogSpriteAnimator?.SetMovement(Vector2.zero);
            SetWalking(false);
            return;
        }

        movement = moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        movement = Vector2.ClampMagnitude(movement, 1f);
        dogSpriteAnimator?.SetMovement(movement);

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

        bool keyboardInteract = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool gamepadInteract = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
        if (keyboardInteract || gamepadInteract)
        {
            Interact();
        }
    }

    private void Interact()
    {
        BeverageFridge fridge = FindAnyObjectByType<BeverageFridge>();
        if (fridge != null && fridge.IsInRange(transform.position))
        {
            fridge.ShowDrinkSelection(this);
            return;
        }

        if (heldBeverage.type == BeverageType.None)
        {
            return;
        }

        FrogCustomer[] customers = FindObjectsByType<FrogCustomer>();
        FrogCustomer closest = null;
        float closestDistance = customerInteractionDistance;
        foreach (FrogCustomer customer in customers)
        {
            if (!customer.IsWaitingForOrder || customer.RequestedBeverage != heldBeverage.type)
            {
                continue;
            }

            float distance = Vector2.Distance(transform.position, customer.transform.position);
            if (distance <= closestDistance)
            {
                closest = customer;
                closestDistance = distance;
            }
        }

        if (closest != null &&
            closest.TryServe(heldBeverage.type, transform.position, BeverageServer.Dog))
        {
            heldBeverage = default;
            UpdateHeldBeverageDisplay();
        }
    }

    private void CreateHeldBeverageDisplay()
    {
        GameObject display = new GameObject("Held Beverage");
        display.transform.SetParent(transform, false);
        display.transform.localPosition = new Vector3(0.48f, 0.3f, 0f);
        display.transform.localScale = Vector3.one * 0.9f;
        heldBeverageRenderer = display.AddComponent<SpriteRenderer>();
        heldBeverageRenderer.sortingOrder = 2100;
        heldBeverageRenderer.enabled = false;
    }

    public void SetHeldBeverage(BeverageDefinition beverage)
    {
        heldBeverage = beverage;
        UpdateHeldBeverageDisplay();
    }

    public void SetFridgeMenuOpen(bool open)
    {
        fridgeMenuOpen = open;
        if (open)
        {
            movement = Vector2.zero;
            body.linearVelocity = Vector2.zero;
            dogSpriteAnimator?.SetMovement(Vector2.zero);
            SetWalking(false);
        }
    }

    private void IgnoreCatCollisions()
    {
        Collider2D[] dogColliders = GetComponentsInChildren<Collider2D>();
        CatMovement[] characters = FindObjectsByType<CatMovement>();
        foreach (CatMovement character in characters)
        {
            if (character.gameObject == gameObject || character.GetComponent<BarmanController>() != null)
            {
                continue;
            }

            Rigidbody2D catBody = character.GetComponent<Rigidbody2D>();
            if (catBody != null)
            {
                catBody.linearVelocity = Vector2.zero;
                catBody.angularVelocity = 0f;
            }

            Collider2D[] catColliders = character.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D dogCollider in dogColliders)
            foreach (Collider2D catCollider in catColliders)
            {
                Physics2D.IgnoreCollision(dogCollider, catCollider, true);
            }
        }
    }

    private void UpdateHeldBeverageDisplay()
    {
        if (heldBeverageRenderer == null)
        {
            return;
        }

        heldBeverageRenderer.sprite = heldBeverage.icon;
        heldBeverageRenderer.enabled = heldBeverage.type != BeverageType.None && heldBeverage.icon != null;
    }

    private void FixedUpdate()
    {
        body.linearVelocity = GameManager.IsGameplayInputBlocked
            ? Vector2.zero
            : movement * moveSpeed;
    }

    public void SetFacing(FaceDirection direction)
    {
        startingDirection = direction;

        if (dogSpriteAnimator != null)
        {
            return;
        }

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
        if (dogSpriteAnimator != null)
        {
            return;
        }

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

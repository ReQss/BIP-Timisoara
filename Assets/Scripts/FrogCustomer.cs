using System;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class FrogCustomer : MonoBehaviour
{
    private enum CustomerState
    {
        WalkingToSeat,
        Deciding,
        WaitingForOrder,
        Eating,
        WalkingToToilet,
        UsingToilet,
        Leaving
    }

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 1.8f;
    [SerializeField, Min(0.01f)] private float arrivalDistance = 0.04f;

    [Header("Visit timing")]
    [SerializeField] private Vector2 decisionTimeRange = new Vector2(1.5f, 3f);
    [SerializeField] private Vector2 eatingTimeRange = new Vector2(4f, 7f);
    [SerializeField, Range(0f, 1f)] private float toiletChance = 0.35f;
    [SerializeField, Min(0.5f)] private float toiletUseSeconds = 3.5f;
    [SerializeField, Min(0.1f)] private float serveDistance = 1.15f;

    [Header("Directional frames")]
    [SerializeField] private Sprite[] idleFront;
    [SerializeField] private Sprite[] idleBack;
    [SerializeField] private Sprite[] idleLeft;
    [SerializeField] private Sprite[] idleRight;
    [SerializeField] private Sprite[] idleFrontRight;
    [SerializeField] private Sprite[] idleBackRight;
    [SerializeField] private Sprite[] idleFrontLeft;
    [SerializeField] private Sprite[] idleBackLeft;
    [SerializeField] private Sprite[] runFront;
    [SerializeField] private Sprite[] runBack;
    [SerializeField] private Sprite[] runLeft;
    [SerializeField] private Sprite[] runRight;
    [SerializeField] private Sprite[] runFrontRight;
    [SerializeField] private Sprite[] runBackRight;
    [SerializeField] private Sprite[] runFrontLeft;
    [SerializeField] private Sprite[] runBackLeft;
    [SerializeField, Min(1f)] private float idleFramesPerSecond = 4f;
    [SerializeField, Min(1f)] private float runFramesPerSecond = 10f;

    private CafeCustomerDirector director;
    private CafeDestination seat;
    private CafeDestination toilet;
    private Transform exit;
    private SpriteRenderer spriteRenderer;
    private CustomerState state;
    private Vector3 destination;
    private Vector2 facing = Vector2.down;
    private float stateTimer;
    private float animationTime;
    private bool served;

    private SpriteRenderer orderSpriteRenderer;
    [SerializeField] private BeverageDefinition[] beverages;
    private BeverageDefinition order;
    private int orderTaskId = -1;

    public BeverageType RequestedBeverage => state == CustomerState.WaitingForOrder ? order.type : BeverageType.None;
    public bool IsWaitingForOrder => state == CustomerState.WaitingForOrder;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        CreateOrderDisplay();
    }

    public void Initialize(CafeCustomerDirector owner, CafeDestination reservedSeat, Transform exitPoint)
    {
        BeverageDefinition[] currentMenu = TaskManager.Instance?.GetCafeBeverages();
        if (currentMenu == null || currentMenu.Length == 0)
        {
            currentMenu = FindAnyObjectByType<BeverageFridge>()?.Beverages;
        }
        if (currentMenu != null && currentMenu.Length > 0)
        {
            beverages = currentMenu;
        }

        director = owner;
        seat = reservedSeat;
        exit = exitPoint;
        destination = seat.transform.position;
        state = CustomerState.WalkingToSeat;
        SetOrderUi(false);
    }

    private void Update()
    {
        animationTime += Time.deltaTime;

        switch (state)
        {
            case CustomerState.WalkingToSeat:
            case CustomerState.WalkingToToilet:
            case CustomerState.Leaving:
                UpdateWalking();
                break;
            case CustomerState.Deciding:
                UpdateTimedState(BeginWaitingForOrder);
                break;
            case CustomerState.WaitingForOrder:
                UpdateWaitingForOrder();
                break;
            case CustomerState.Eating:
                UpdateTimedState(FinishEating);
                break;
            case CustomerState.UsingToilet:
                UpdateTimedState(LeaveToExit);
                break;
        }

        UpdateAnimation();
    }

    private void UpdateWalking()
    {
        Vector3 delta = destination - transform.position;
        if (delta.sqrMagnitude <= arrivalDistance * arrivalDistance)
        {
            transform.position = destination;
            ArriveAtDestination();
            return;
        }

        facing = delta.normalized;
        transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
    }

    private void ArriveAtDestination()
    {
        if (state == CustomerState.WalkingToSeat)
        {
            state = CustomerState.Deciding;
            stateTimer = UnityEngine.Random.Range(decisionTimeRange.x, decisionTimeRange.y);
            facing = seat.SeatedFacing;
            SetOrderUi(false);
        }
        else if (state == CustomerState.WalkingToToilet)
        {
            state = CustomerState.UsingToilet;
            stateTimer = toiletUseSeconds;
            spriteRenderer.enabled = false;
            SetOrderUi(false);
        }
        else
        {
            CompleteVisit();
        }
    }

    private void UpdateTimedState(Action onComplete)
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            onComplete();
        }
    }

    private void BeginWaitingForOrder()
    {
        state = CustomerState.WaitingForOrder;
        if (beverages == null || beverages.Length == 0)
        {
            Debug.LogError("Cafe beverage menu is unavailable; customer visit was cancelled.", this);
            LeaveToExit();
            return;
        }

        order = beverages[UnityEngine.Random.Range(0, beverages.Length)];
        orderSpriteRenderer.sprite = order.icon;
        orderSpriteRenderer.enabled = order.icon != null;
        orderTaskId = TaskManager.Instance != null ? TaskManager.Instance.AddCustomerOrder(this, order) : -1;
        SetOrderUi(true);
    }

    private void UpdateWaitingForOrder()
    {
        // Orders intentionally have no patience timer. The customer waits until served.
    }

    public bool TryServe(BeverageType beverage, Vector3 serverPosition)
    {
        if (state != CustomerState.WaitingForOrder || beverage != order.type ||
            Vector2.Distance(transform.position, serverPosition) > serveDistance)
        {
            return false;
        }

        served = true;
        if (orderTaskId >= 0)
        {
            TaskManager.Instance?.CompleteCustomerOrder(orderTaskId);
            orderTaskId = -1;
        }
        state = CustomerState.Eating;
        stateTimer = UnityEngine.Random.Range(eatingTimeRange.x, eatingTimeRange.y);
        SetOrderUi(false);
        return true;
    }

    private void FinishEating()
    {
        seat?.Release();
        seat = null;
        toilet = UnityEngine.Random.value < toiletChance ? director.TryReserveToilet() : null;

        if (toilet != null)
        {
            state = CustomerState.WalkingToToilet;
            destination = toilet.transform.position;
            SetOrderUi(false);
        }
        else
        {
            LeaveToExit();
        }
    }

    private void LeaveToExit()
    {
        spriteRenderer.enabled = true;
        toilet?.Release();
        toilet = null;
        seat?.Release();
        seat = null;
        state = CustomerState.Leaving;
        destination = exit.position;
        SetOrderUi(false);
    }

    private void CompleteVisit()
    {
        director.CustomerFinished(this, served);
        Destroy(gameObject);
    }

    private void UpdateAnimation()
    {
        bool walking = state == CustomerState.WalkingToSeat ||
                       state == CustomerState.WalkingToToilet ||
                       state == CustomerState.Leaving;
        Sprite[] frames = SelectFrames(walking);
        if (frames == null || frames.Length == 0)
        {
            return;
        }

        float fps = walking ? runFramesPerSecond : idleFramesPerSecond;
        spriteRenderer.sprite = frames[Mathf.FloorToInt(animationTime * fps) % frames.Length];
        spriteRenderer.sortingOrder = 1000 - Mathf.RoundToInt(transform.position.y * 10f);
    }

    private Sprite[] SelectFrames(bool walking)
    {
        bool diagonal = Mathf.Abs(facing.x) > 0.2f && Mathf.Abs(facing.y) > 0.2f;
        if (diagonal)
        {
            Sprite[] diagonalFrames;
            if (facing.y < 0f)
            {
                diagonalFrames = facing.x < 0f
                    ? (walking ? runFrontLeft : idleFrontLeft)
                    : (walking ? runFrontRight : idleFrontRight);
            }
            else
            {
                diagonalFrames = facing.x < 0f
                    ? (walking ? runBackLeft : idleBackLeft)
                    : (walking ? runBackRight : idleBackRight);
            }

            if (diagonalFrames != null && diagonalFrames.Length > 0)
            {
                return diagonalFrames;
            }
        }

        if (Mathf.Abs(facing.x) > Mathf.Abs(facing.y))
        {
            return facing.x < 0f ? (walking ? runLeft : idleLeft) : (walking ? runRight : idleRight);
        }

        return facing.y > 0f ? (walking ? runBack : idleBack) : (walking ? runFront : idleFront);
    }

    private void SetOrderUi(bool visible)
    {
        if (orderSpriteRenderer != null)
        {
            orderSpriteRenderer.gameObject.SetActive(visible);
        }
    }

    private void CreateOrderDisplay()
    {
        GameObject spriteObject = new GameObject("Wanted Beverage Sprite");
        spriteObject.transform.SetParent(transform, false);
        spriteObject.transform.localPosition = new Vector3(0f, 1.05f, 0f);
        spriteObject.transform.localScale = Vector3.one * 0.38f;
        orderSpriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
        orderSpriteRenderer.sortingOrder = 2100;
        orderSpriteRenderer.enabled = false;
    }

    public void ConfigureAnimationFrames(
        Sprite[] frontIdle, Sprite[] backIdle, Sprite[] leftIdle, Sprite[] rightIdle,
        Sprite[] frontRun, Sprite[] backRun, Sprite[] leftRun, Sprite[] rightRun,
        Sprite[] frontRightIdle = null, Sprite[] backRightIdle = null,
        Sprite[] frontLeftIdle = null, Sprite[] backLeftIdle = null,
        Sprite[] frontRightRun = null, Sprite[] backRightRun = null,
        Sprite[] frontLeftRun = null, Sprite[] backLeftRun = null)
    {
        idleFront = frontIdle;
        idleBack = backIdle;
        idleLeft = leftIdle;
        idleRight = rightIdle;
        runFront = frontRun;
        runBack = backRun;
        runLeft = leftRun;
        runRight = rightRun;
        idleFrontRight = frontRightIdle;
        idleBackRight = backRightIdle;
        idleFrontLeft = frontLeftIdle;
        idleBackLeft = backLeftIdle;
        runFrontRight = frontRightRun;
        runBackRight = backRightRun;
        runFrontLeft = frontLeftRun;
        runBackLeft = backLeftRun;
    }

    public void ConfigureBeverages(BeverageDefinition[] menu)
    {
        beverages = menu;
    }

    private void OnDestroy()
    {
        if (orderTaskId >= 0)
        {
            if (TaskManager.Instance != null)
            {
                TaskManager.Instance.CancelCustomerOrder(orderTaskId);
            }
        }
        seat?.Release();
        toilet?.Release();
    }
}

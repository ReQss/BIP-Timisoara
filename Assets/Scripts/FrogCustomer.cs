using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
    [SerializeField, Min(1f)] private float patienceSeconds = 18f;
    [SerializeField] private Vector2 eatingTimeRange = new Vector2(4f, 7f);
    [SerializeField, Range(0f, 1f)] private float toiletChance = 0.35f;
    [SerializeField, Min(0.5f)] private float toiletUseSeconds = 3.5f;
    [SerializeField, Min(0.1f)] private float barmanServeDistance = 1.15f;

    [Header("Directional frames")]
    [SerializeField] private Sprite[] idleFront;
    [SerializeField] private Sprite[] idleBack;
    [SerializeField] private Sprite[] idleLeft;
    [SerializeField] private Sprite[] idleRight;
    [SerializeField] private Sprite[] runFront;
    [SerializeField] private Sprite[] runBack;
    [SerializeField] private Sprite[] runLeft;
    [SerializeField] private Sprite[] runRight;
    [SerializeField, Min(1f)] private float idleFramesPerSecond = 4f;
    [SerializeField, Min(1f)] private float runFramesPerSecond = 10f;

    private CafeCustomerDirector director;
    private CafeDestination seat;
    private CafeDestination toilet;
    private Transform exit;
    private Transform barman;
    private SpriteRenderer spriteRenderer;
    private CustomerState state;
    private Vector3 destination;
    private Vector2 facing = Vector2.down;
    private float stateTimer;
    private float patienceRemaining;
    private float animationTime;
    private bool served;

    private Canvas orderCanvas;
    private Text orderText;
    private Button serveButton;
    private Image patienceFill;

    private static readonly string[] Orders = { "Tea", "Coffee", "Cake", "Soup" };

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        CreateOrderUi();
    }

    public void Initialize(CafeCustomerDirector owner, CafeDestination reservedSeat, Transform exitPoint)
    {
        director = owner;
        seat = reservedSeat;
        exit = exitPoint;
        destination = seat.transform.position;
        state = CustomerState.WalkingToSeat;
        barman = FindAnyObjectByType<BarmanController>()?.transform;
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
            ShowMessage("Hmm...");
        }
        else if (state == CustomerState.WalkingToToilet)
        {
            state = CustomerState.UsingToilet;
            stateTimer = toiletUseSeconds;
            spriteRenderer.enabled = false;
            ShowMessage("Occupied");
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
        patienceRemaining = patienceSeconds;
        string order = Orders[UnityEngine.Random.Range(0, Orders.Length)];
        orderText.text = order + "?";
        serveButton.gameObject.SetActive(true);
        serveButton.interactable = true;
        patienceFill.transform.parent.gameObject.SetActive(true);
        SetOrderUi(true);
    }

    private void UpdateWaitingForOrder()
    {
        patienceRemaining -= Time.deltaTime;
        patienceFill.fillAmount = Mathf.Clamp01(patienceRemaining / patienceSeconds);

        bool keyboardServe = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        bool gamepadServe = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
        if ((keyboardServe || gamepadServe) && barman != null &&
            Vector2.Distance(transform.position, barman.position) <= barmanServeDistance)
        {
            ServeOrder();
        }

        if (patienceRemaining <= 0f)
        {
            ShowMessage("Too slow!");
            LeaveToExit();
        }
    }

    public void ServeOrder()
    {
        if (state != CustomerState.WaitingForOrder)
        {
            return;
        }

        served = true;
        state = CustomerState.Eating;
        stateTimer = UnityEngine.Random.Range(eatingTimeRange.x, eatingTimeRange.y);
        serveButton.gameObject.SetActive(true);
        serveButton.interactable = false;
        patienceFill.transform.parent.gameObject.SetActive(false);
        ShowMessage("Yum!");
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
        if (Mathf.Abs(facing.x) > Mathf.Abs(facing.y))
        {
            return facing.x < 0f ? (walking ? runLeft : idleLeft) : (walking ? runRight : idleRight);
        }

        return facing.y > 0f ? (walking ? runBack : idleBack) : (walking ? runFront : idleFront);
    }

    private void ShowMessage(string message)
    {
        orderText.text = message;
        serveButton.gameObject.SetActive(true);
        serveButton.interactable = false;
        patienceFill.transform.parent.gameObject.SetActive(false);
        SetOrderUi(true);
    }

    private void SetOrderUi(bool visible)
    {
        orderCanvas.gameObject.SetActive(visible);
    }

    private void CreateOrderUi()
    {
        GameObject canvasObject = new GameObject(
            "Order UI", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.transform.localPosition = new Vector3(0f, 0.92f, 0f);
        canvasObject.transform.localScale = Vector3.one * 0.0075f;
        orderCanvas = canvasObject.GetComponent<Canvas>();
        orderCanvas.renderMode = RenderMode.WorldSpace;
        orderCanvas.sortingOrder = 2000;
        RectTransform canvasRect = (RectTransform)canvasObject.transform;
        canvasRect.sizeDelta = new Vector2(130f, 58f);

        Image panel = canvasObject.AddComponent<Image>();
        panel.color = new Color(0.12f, 0.16f, 0.18f, 0.94f);

        GameObject buttonObject = new GameObject("Serve Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(canvasObject.transform, false);
        RectTransform buttonRect = (RectTransform)buttonObject.transform;
        buttonRect.anchorMin = new Vector2(0.06f, 0.35f);
        buttonRect.anchorMax = new Vector2(0.94f, 0.94f);
        buttonRect.offsetMin = buttonRect.offsetMax = Vector2.zero;
        buttonObject.GetComponent<Image>().color = new Color(0.38f, 0.72f, 0.42f, 1f);
        serveButton = buttonObject.GetComponent<Button>();
        serveButton.onClick.AddListener(ServeOrder);

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform textRect = (RectTransform)textObject.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        orderText = textObject.GetComponent<Text>();
        orderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        orderText.fontSize = 22;
        orderText.alignment = TextAnchor.MiddleCenter;
        orderText.color = Color.white;

        GameObject patienceBackground = new GameObject("Patience", typeof(RectTransform), typeof(Image));
        patienceBackground.transform.SetParent(canvasObject.transform, false);
        RectTransform patienceRect = (RectTransform)patienceBackground.transform;
        patienceRect.anchorMin = new Vector2(0.06f, 0.1f);
        patienceRect.anchorMax = new Vector2(0.94f, 0.25f);
        patienceRect.offsetMin = patienceRect.offsetMax = Vector2.zero;
        patienceBackground.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f, 1f);

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(patienceBackground.transform, false);
        RectTransform fillRect = (RectTransform)fillObject.transform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
        patienceFill = fillObject.GetComponent<Image>();
        patienceFill.color = new Color(1f, 0.72f, 0.2f, 1f);
        patienceFill.type = Image.Type.Filled;
        patienceFill.fillMethod = Image.FillMethod.Horizontal;
        patienceFill.fillOrigin = (int)Image.OriginHorizontal.Left;
    }

    public void ConfigureAnimationFrames(
        Sprite[] frontIdle, Sprite[] backIdle, Sprite[] leftIdle, Sprite[] rightIdle,
        Sprite[] frontRun, Sprite[] backRun, Sprite[] leftRun, Sprite[] rightRun)
    {
        idleFront = frontIdle;
        idleBack = backIdle;
        idleLeft = leftIdle;
        idleRight = rightIdle;
        runFront = frontRun;
        runBack = backRun;
        runLeft = leftRun;
        runRight = rightRun;
    }

    private void OnDestroy()
    {
        seat?.Release();
        toilet?.Release();
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class BeverageFridge : MonoBehaviour
{
    private sealed class SelectionSession
    {
        public IBeverageCarrier carrier;
        public GameObject canvas;
        public readonly List<Image> buttons = new List<Image>();
        public int selectedIndex;
        public int openedFrame;
        public float nextStickMoveTime;
    }

    [SerializeField] private BeverageDefinition[] beverages;
    [SerializeField, Min(0.25f)] private float interactionDistance = 1.25f;

    private readonly List<SelectionSession> sessions = new List<SelectionSession>();
    private SplitScreenManager splitScreen;

    public BeverageDefinition[] Beverages => beverages;

    public bool IsInRange(Vector3 position)
    {
        return Vector2.Distance(position, transform.position) <= interactionDistance;
    }

    public void ShowDrinkSelection(IBeverageCarrier carrier)
    {
        if (carrier == null || beverages == null || beverages.Length == 0)
        {
            return;
        }

        SelectionSession existing = sessions.Find(session => session.carrier == carrier);
        if (existing != null)
        {
            CloseSelection(existing);
            return;
        }

        EnsureEventSystem();
        splitScreen ??= FindAnyObjectByType<SplitScreenManager>();
        splitScreen?.PushMenuSplit();

        SelectionSession session = new SelectionSession
        {
            carrier = carrier,
            selectedIndex = 0,
            openedFrame = Time.frameCount
        };
        sessions.Add(session);
        carrier.SetFridgeMenuOpen(true);

        session.canvas = new GameObject(
            carrier.UsesCatControls ? "Cat Fridge Selection" : "Dog Fridge Selection",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = session.canvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = FindPlayerCamera(carrier);
        canvas.planeDistance = 1f;
        canvas.sortingOrder = 5000;
        CanvasScaler scaler = session.canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = new GameObject("White Drink Table", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(session.canvas.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(700f, 550f);
        panel.GetComponent<Image>().color = Color.white;

        CreateLabel(panel.transform, carrier.UsesCatControls ? "CAT — CHOOSE A DRINK" : "DOG — CHOOSE A DRINK",
            new Vector2(0f, 240f), new Vector2(600f, 46f), 28, FontStyle.Bold);

        GameObject gridObject = new GameObject("Drinks", typeof(RectTransform), typeof(GridLayoutGroup));
        gridObject.transform.SetParent(panel.transform, false);
        RectTransform gridRect = gridObject.GetComponent<RectTransform>();
        gridRect.anchorMin = gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.sizeDelta = new Vector2(640f, 410f);
        gridRect.anchoredPosition = new Vector2(0f, -20f);
        GridLayoutGroup grid = gridObject.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(118f, 90f);
        grid.spacing = new Vector2(10f, 10f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.childAlignment = TextAnchor.UpperCenter;

        int visibleCount = Mathf.Min(20, beverages.Length);
        for (int i = 0; i < visibleCount; i++)
        {
            BeverageDefinition choice = beverages[i];
            Image image = CreateDrinkButton(gridObject.transform, choice, () => SelectDrink(session, choice));
            session.buttons.Add(image);
        }

        GameObject close = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
        close.transform.SetParent(panel.transform, false);
        RectTransform closeRect = close.GetComponent<RectTransform>();
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-12f, -12f);
        closeRect.sizeDelta = new Vector2(42f, 42f);
        close.GetComponent<Image>().color = new Color(0.9f, 0.9f, 0.9f);
        close.GetComponent<Button>().onClick.AddListener(() => CloseSelection(session));
        CreateLabel(close.transform, "X", Vector2.zero, new Vector2(42f, 42f), 22, FontStyle.Bold);
        UpdateSelectionHighlight(session);
    }

    private void Update()
    {
        for (int i = sessions.Count - 1; i >= 0; i--)
        {
            UpdateSession(sessions[i]);
        }
    }

    private void UpdateSession(SelectionSession session)
    {
        if (session.canvas == null || Time.frameCount == session.openedFrame)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        bool cat = session.carrier.UsesCatControls;
        bool left = keyboard != null && keyboard[cat ? Key.A : Key.J].wasPressedThisFrame;
        bool right = keyboard != null && keyboard[cat ? Key.D : Key.L].wasPressedThisFrame;
        bool up = keyboard != null && keyboard[cat ? Key.W : Key.I].wasPressedThisFrame;
        bool down = keyboard != null && keyboard[cat ? Key.S : Key.K].wasPressedThisFrame;

        // The project's gamepad belongs to the dog, so it never moves the cat's menu.
        Gamepad gamepad = cat ? null : Gamepad.current;
        if (gamepad != null)
        {
            left |= gamepad.dpad.left.wasPressedThisFrame;
            right |= gamepad.dpad.right.wasPressedThisFrame;
            up |= gamepad.dpad.up.wasPressedThisFrame;
            down |= gamepad.dpad.down.wasPressedThisFrame;
            if (Time.unscaledTime >= session.nextStickMoveTime)
            {
                Vector2 stick = gamepad.leftStick.ReadValue();
                if (Mathf.Abs(stick.x) > 0.65f || Mathf.Abs(stick.y) > 0.65f)
                {
                    if (Mathf.Abs(stick.x) > Mathf.Abs(stick.y))
                    {
                        left |= stick.x < 0f;
                        right |= stick.x > 0f;
                    }
                    else
                    {
                        down |= stick.y < 0f;
                        up |= stick.y > 0f;
                    }
                    session.nextStickMoveTime = Time.unscaledTime + 0.18f;
                }
            }
        }

        if (left) MoveSelection(session, -1, 0);
        else if (right) MoveSelection(session, 1, 0);
        else if (up) MoveSelection(session, 0, -1);
        else if (down) MoveSelection(session, 0, 1);

        bool confirm = keyboard != null && keyboard[cat ? Key.E : Key.Space].wasPressedThisFrame;
        confirm |= gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
        if (confirm && session.selectedIndex < beverages.Length)
        {
            SelectDrink(session, beverages[session.selectedIndex]);
        }
    }

    public void Configure(BeverageDefinition[] definitions)
    {
        beverages = definitions;
    }

    private Camera FindPlayerCamera(IBeverageCarrier carrier)
    {
        PlayerCameraFollow[] follows = FindObjectsByType<PlayerCameraFollow>();
        foreach (PlayerCameraFollow follow in follows)
        {
            if (follow.Player == carrier.CarrierTransform)
            {
                return follow.GetComponent<Camera>();
            }
        }

        string wantedName = carrier.UsesCatControls ? "CatCamera" : "DogCamera";
        foreach (Camera camera in FindObjectsByType<Camera>(FindObjectsInactive.Include))
        {
            if (camera.name == wantedName) return camera;
        }
        return Camera.main;
    }

    private static Image CreateDrinkButton(Transform parent, BeverageDefinition beverage, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(beverage.displayName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        Image background = buttonObject.GetComponent<Image>();
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(action);

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(buttonObject.transform, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0f, -5f);
        iconRect.sizeDelta = new Vector2(56f, 56f);
        Image icon = iconObject.GetComponent<Image>();
        icon.sprite = beverage.icon;
        icon.preserveAspect = true;
        CreateLabel(buttonObject.transform, beverage.displayName, new Vector2(0f, -31f), new Vector2(110f, 24f), 13, FontStyle.Normal);
        return background;
    }

    private void MoveSelection(SelectionSession session, int horizontal, int vertical)
    {
        const int columns = 5;
        int rows = Mathf.CeilToInt(session.buttons.Count / (float)columns);
        int row = session.selectedIndex / columns;
        int column = session.selectedIndex % columns;
        column = (column + horizontal + columns) % columns;
        row = (row + vertical + rows) % rows;
        session.selectedIndex = Mathf.Min(row * columns + column, session.buttons.Count - 1);
        UpdateSelectionHighlight(session);
    }

    private static void UpdateSelectionHighlight(SelectionSession session)
    {
        for (int i = 0; i < session.buttons.Count; i++)
        {
            session.buttons[i].color = i == session.selectedIndex
                ? new Color(0.55f, 0.82f, 1f)
                : new Color(0.94f, 0.94f, 0.94f);
        }
    }

    private void SelectDrink(SelectionSession session, BeverageDefinition beverage)
    {
        session.carrier.SetHeldBeverage(beverage);
        CloseSelection(session);
    }

    private void CloseSelection(SelectionSession session)
    {
        if (!sessions.Remove(session)) return;
        session.carrier?.SetFridgeMenuOpen(false);
        if (session.canvas != null) Destroy(session.canvas);
        splitScreen?.PopMenuSplit();
    }

    private static Text CreateLabel(Transform parent, string value, Vector2 position, Vector2 size, int fontSize, FontStyle style)
    {
        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(parent, false);
        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Text label = labelObject.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = value;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.black;
        return label;
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
    }

    private void OnDestroy()
    {
        while (sessions.Count > 0) CloseSelection(sessions[0]);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.35f, 0.85f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}

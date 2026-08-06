using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class BeverageFridge : MonoBehaviour
{
    [SerializeField] private BeverageDefinition[] beverages;
    [SerializeField, Min(0.25f)] private float interactionDistance = 1.25f;

    private GameObject selectionCanvas;
    private readonly List<Image> drinkButtons = new List<Image>();
    private IBeverageCarrier selectingCarrier;
    private int selectedIndex;
    private int openedFrame;
    private float nextStickMoveTime;

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

        if (selectionCanvas != null)
        {
            CloseSelection();
            return;
        }

        EnsureEventSystem();
        selectionCanvas = new GameObject(
            "Fridge Drink Selection",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas canvas = selectionCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        CanvasScaler scaler = selectionCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        selectingCarrier = carrier;
        selectingCarrier.SetFridgeMenuOpen(true);
        selectedIndex = 0;
        openedFrame = Time.frameCount;
        drinkButtons.Clear();

        GameObject panel = new GameObject("White Drink Table", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(selectionCanvas.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(720f, 560f);
        panel.GetComponent<Image>().color = Color.white;

        CreateLabel(panel.transform, "CHOOSE A DRINK", new Vector2(0f, 245f), new Vector2(620f, 48f), 30, FontStyle.Bold);

        GameObject gridObject = new GameObject("Drinks", typeof(RectTransform), typeof(GridLayoutGroup));
        gridObject.transform.SetParent(panel.transform, false);
        RectTransform gridRect = gridObject.GetComponent<RectTransform>();
        gridRect.anchorMin = gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.sizeDelta = new Vector2(650f, 420f);
        gridRect.anchoredPosition = new Vector2(0f, -20f);
        GridLayoutGroup grid = gridObject.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(120f, 92f);
        grid.spacing = new Vector2(10f, 10f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.childAlignment = TextAnchor.UpperCenter;

        int visibleCount = Mathf.Min(20, beverages.Length);
        for (int i = 0; i < visibleCount; i++)
        {
            BeverageDefinition choice = beverages[i];
            Image buttonImage = CreateDrinkButton(gridObject.transform, choice, () =>
            {
                SelectDrink(choice);
            });
            drinkButtons.Add(buttonImage);
        }

        GameObject close = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
        close.transform.SetParent(panel.transform, false);
        RectTransform closeRect = close.GetComponent<RectTransform>();
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-12f, -12f);
        closeRect.sizeDelta = new Vector2(42f, 42f);
        close.GetComponent<Image>().color = new Color(0.9f, 0.9f, 0.9f);
        close.GetComponent<Button>().onClick.AddListener(CloseSelection);
        CreateLabel(close.transform, "X", Vector2.zero, new Vector2(42f, 42f), 22, FontStyle.Bold);
        UpdateSelectionHighlight();
    }

    private void Update()
    {
        if (selectionCanvas == null || Time.frameCount == openedFrame)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;

        bool catControls = selectingCarrier != null && selectingCarrier.UsesCatControls;
        Key leftKey = catControls ? Key.A : Key.J;
        Key rightKey = catControls ? Key.D : Key.L;
        Key upKey = catControls ? Key.W : Key.I;
        Key downKey = catControls ? Key.S : Key.K;
        bool left = WasPressed(keyboard, leftKey, Key.LeftArrow) || (gamepad != null && gamepad.dpad.left.wasPressedThisFrame);
        bool right = WasPressed(keyboard, rightKey, Key.RightArrow) || (gamepad != null && gamepad.dpad.right.wasPressedThisFrame);
        bool up = WasPressed(keyboard, upKey, Key.UpArrow) || (gamepad != null && gamepad.dpad.up.wasPressedThisFrame);
        bool down = WasPressed(keyboard, downKey, Key.DownArrow) || (gamepad != null && gamepad.dpad.down.wasPressedThisFrame);

        if (gamepad != null && Time.unscaledTime >= nextStickMoveTime)
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
                nextStickMoveTime = Time.unscaledTime + 0.18f;
            }
        }

        if (left) MoveSelection(-1, 0);
        else if (right) MoveSelection(1, 0);
        else if (up) MoveSelection(0, -1);
        else if (down) MoveSelection(0, 1);

        bool keyboardConfirm = keyboard != null && (catControls
            ? keyboard.eKey.wasPressedThisFrame
            : keyboard.spaceKey.wasPressedThisFrame);
        bool confirm = keyboardConfirm ||
                       (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);
        if (confirm && selectedIndex >= 0 && selectedIndex < beverages.Length)
        {
            SelectDrink(beverages[selectedIndex]);
        }
    }

    public void Configure(BeverageDefinition[] definitions)
    {
        beverages = definitions;
    }

    private static Image CreateDrinkButton(Transform parent, BeverageDefinition beverage, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(beverage.displayName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        Image background = buttonObject.GetComponent<Image>();
        background.color = new Color(0.94f, 0.94f, 0.94f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(action);

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(buttonObject.transform, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0f, -5f);
        iconRect.sizeDelta = new Vector2(58f, 58f);
        Image icon = iconObject.GetComponent<Image>();
        icon.sprite = beverage.icon;
        icon.preserveAspect = true;

        CreateLabel(buttonObject.transform, beverage.displayName, new Vector2(0f, -32f), new Vector2(112f, 25f), 14, FontStyle.Normal);
        return background;
    }

    private void MoveSelection(int horizontal, int vertical)
    {
        if (drinkButtons.Count == 0)
        {
            return;
        }

        const int columns = 5;
        int rows = Mathf.CeilToInt(drinkButtons.Count / (float)columns);
        int row = selectedIndex / columns;
        int column = selectedIndex % columns;
        column = (column + horizontal + columns) % columns;
        row = (row + vertical + rows) % rows;
        int candidate = row * columns + column;
        if (candidate >= drinkButtons.Count)
        {
            candidate = drinkButtons.Count - 1;
        }
        selectedIndex = candidate;
        UpdateSelectionHighlight();
    }

    private void UpdateSelectionHighlight()
    {
        for (int i = 0; i < drinkButtons.Count; i++)
        {
            drinkButtons[i].color = i == selectedIndex
                ? new Color(0.55f, 0.82f, 1f)
                : new Color(0.94f, 0.94f, 0.94f);
        }
    }

    private void SelectDrink(BeverageDefinition beverage)
    {
        selectingCarrier?.SetHeldBeverage(beverage);
        CloseSelection();
    }

    private void CloseSelection()
    {
        selectingCarrier?.SetFridgeMenuOpen(false);
        selectingCarrier = null;
        if (selectionCanvas != null)
        {
            Destroy(selectionCanvas);
            selectionCanvas = null;
        }
        drinkButtons.Clear();
    }

    private static bool WasPressed(Keyboard keyboard, Key first, Key second)
    {
        return keyboard != null && (keyboard[first].wasPressedThisFrame || keyboard[second].wasPressedThisFrame);
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
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
    }

    private void OnDestroy()
    {
        CloseSelection();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.35f, 0.85f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}

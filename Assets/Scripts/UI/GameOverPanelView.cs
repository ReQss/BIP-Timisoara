using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameOverPanelView : MonoBehaviour
{
    private static readonly Color InkColor = new Color(0.24f, 0.14f, 0.29f);
    private static readonly Color MutedInkColor = new Color(0.38f, 0.29f, 0.43f);
    private static readonly Color OverlayColor = new Color(0.09f, 0.07f, 0.13f, 0.82f);

    private GameManager gameManager;
    private TMP_FontAsset pixelFont;
    private Sprite panelSprite;
    private Sprite cardSprite;
    private Sprite buttonSprite;
    private TextMeshProUGUI servedValue;
    private TextMeshProUGUI unhappyValue;
    private TextMeshProUGUI moneyValue;
    private TextMeshProUGUI satisfactionValue;

    public void Initialize(
        GameManager owner,
        TMP_FontAsset font,
        Sprite pastelPanel,
        Sprite pastelCard,
        Sprite pastelButton)
    {
        gameManager = owner;
        pixelFont = font;
        panelSprite = pastelPanel;
        cardSprite = pastelCard;
        buttonSprite = pastelButton;

        Image overlay = GetComponent<Image>();
        if (overlay != null)
        {
            overlay.sprite = null;
            overlay.color = OverlayColor;
        }

        if (transform.Find("GameOverContent") == null)
        {
            DisableLegacyContent();
            BuildContent();
        }
        else
        {
            CacheLabels();
        }
    }

    public void Show(int served, int unhappy, int moneyEarned)
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        CacheLabels();

        int resolvedVisits = served + unhappy;
        int satisfaction = resolvedVisits > 0 ? Mathf.RoundToInt(served * 100f / resolvedVisits) : 0;
        SetValue(servedValue, served.ToString());
        SetValue(unhappyValue, unhappy.ToString());
        SetValue(moneyValue, "$" + moneyEarned);
        SetValue(satisfactionValue, satisfaction + "%");
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void DisableLegacyContent()
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
    }

    private void BuildContent()
    {
        RectTransform content = CreatePanel("GameOverContent", transform, panelSprite, Color.white);
        content.anchorMin = new Vector2(0.5f, 0.5f);
        content.anchorMax = new Vector2(0.5f, 0.5f);
        content.pivot = new Vector2(0.5f, 0.5f);
        content.sizeDelta = new Vector2(620f, 640f);

        CreateLabel("Title", content, "SHIFT OVER!", 43f, InkColor, FontStyles.Bold,
            new Vector2(0f, 252f), new Vector2(520f, 64f));
        CreateLabel("Subtitle", content, "TODAY'S CAFE REPORT", 21f, MutedInkColor, FontStyles.Bold,
            new Vector2(0f, 207f), new Vector2(500f, 38f));

        servedValue = CreateStatCard(content, "Served", "ORDERS SERVED", new Vector2(-132f, 92f));
        unhappyValue = CreateStatCard(content, "Unhappy", "UNHAPPY GUESTS", new Vector2(132f, 92f));
        moneyValue = CreateStatCard(content, "Money", "MONEY EARNED", new Vector2(-132f, -70f));
        satisfactionValue = CreateStatCard(content, "Satisfaction", "SATISFACTION", new Vector2(132f, -70f));

        RectTransform buttonRect = CreatePanel("PlayAgainButton", content, buttonSprite, Color.white);
        buttonRect.anchoredPosition = new Vector2(0f, -238f);
        buttonRect.sizeDelta = new Vector2(324f, 102f);
        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonRect.GetComponent<Image>();
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.92f, 0.94f);
        colors.pressedColor = new Color(0.9f, 0.72f, 0.78f);
        button.colors = colors;
        button.onClick.AddListener(OnPlayAgainClicked);
        CreateLabel("Label", buttonRect, "PLAY AGAIN", 29f, InkColor, FontStyles.Bold,
            new Vector2(0f, 2f), new Vector2(270f, 58f));
    }

    private TextMeshProUGUI CreateStatCard(Transform parent, string name, string label, Vector2 position)
    {
        RectTransform card = CreatePanel(name + "Panel", parent, cardSprite, Color.white);
        card.anchoredPosition = position;
        card.sizeDelta = new Vector2(238f, 144f);
        CreateLabel("Label", card, label, 17f, MutedInkColor, FontStyles.Bold,
            new Vector2(0f, 32f), new Vector2(204f, 34f));
        return CreateLabel("Value", card, "0", 34f, InkColor, FontStyles.Bold,
            new Vector2(0f, -13f), new Vector2(190f, 58f));
    }

    private void CacheLabels()
    {
        servedValue = FindValue("ServedPanel");
        unhappyValue = FindValue("UnhappyPanel");
        moneyValue = FindValue("MoneyPanel");
        satisfactionValue = FindValue("SatisfactionPanel");
    }

    private TextMeshProUGUI FindValue(string panelName)
    {
        Transform value = transform.Find("GameOverContent/" + panelName + "/Value");
        return value != null ? value.GetComponent<TextMeshProUGUI>() : null;
    }

    private void OnPlayAgainClicked()
    {
        gameManager?.ReturnToStartPanel();
    }

    private static RectTransform CreatePanel(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        Image image = panel.GetComponent<Image>();
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = color;
        return rect;
    }

    private TextMeshProUGUI CreateLabel(
        string name, Transform parent, string text, float size, Color color, FontStyles style,
        Vector2 position, Vector2 dimensions, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);
        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        if (pixelFont != null)
        {
            label.font = pixelFont;
        }
        label.fontSize = size;
        label.fontStyle = style;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
        return label;
    }

    private static void SetValue(TextMeshProUGUI label, string value)
    {
        if (label != null)
        {
            label.text = value;
        }
    }
}

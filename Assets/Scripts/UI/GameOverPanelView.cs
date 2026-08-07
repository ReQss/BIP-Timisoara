using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameOverPanelView : MonoBehaviour
{
    private static readonly Color PanelColor = new Color(0.12f, 0.09f, 0.16f, 0.97f);
    private static readonly Color CardColor = new Color(0.24f, 0.18f, 0.29f, 0.96f);
    private static readonly Color AccentColor = new Color(1f, 0.76f, 0.32f);

    private GameManager gameManager;
    private TextMeshProUGUI servedValue;
    private TextMeshProUGUI unhappyValue;
    private TextMeshProUGUI moneyValue;
    private TextMeshProUGUI satisfactionValue;

    public void Initialize(GameManager owner)
    {
        gameManager = owner;
        if (transform.Find("GameOverContent") == null)
        {
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

    private void BuildContent()
    {
        RectTransform content = CreatePanel("GameOverContent", transform, PanelColor);
        content.anchorMin = new Vector2(0.5f, 0.5f);
        content.anchorMax = new Vector2(0.5f, 0.5f);
        content.pivot = new Vector2(0.5f, 0.5f);
        content.sizeDelta = new Vector2(540f, 610f);

        CreateLabel("Title", content, "GAME OVER", 42f, AccentColor, FontStyles.Bold,
            new Vector2(0f, 244f), new Vector2(480f, 64f));
        CreateLabel("Subtitle", content, "SHIFT STATISTICS", 20f, Color.white, FontStyles.Bold,
            new Vector2(0f, 194f), new Vector2(480f, 38f));

        RectTransform statistics = CreatePanel("StatisticsPanel", content, new Color(0.07f, 0.05f, 0.1f, 0.7f));
        statistics.anchoredPosition = new Vector2(0f, 20f);
        statistics.sizeDelta = new Vector2(460f, 310f);

        servedValue = CreateStatCard(statistics, "Served", "ORDERS SERVED", 105f);
        unhappyValue = CreateStatCard(statistics, "Unhappy", "UNHAPPY CUSTOMERS", 35f);
        moneyValue = CreateStatCard(statistics, "Money", "MONEY EARNED", -35f);
        satisfactionValue = CreateStatCard(statistics, "Satisfaction", "SATISFACTION", -105f);

        RectTransform buttonRect = CreatePanel("PlayAgainButton", content, AccentColor);
        buttonRect.anchoredPosition = new Vector2(0f, -245f);
        buttonRect.sizeDelta = new Vector2(300f, 72f);
        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonRect.GetComponent<Image>();
        button.onClick.AddListener(OnPlayAgainClicked);
        CreateLabel("Label", buttonRect, "PLAY AGAIN", 28f, new Color(0.16f, 0.1f, 0.18f), FontStyles.Bold,
            Vector2.zero, buttonRect.sizeDelta);
    }

    private TextMeshProUGUI CreateStatCard(Transform parent, string name, string label, float y)
    {
        RectTransform card = CreatePanel(name + "Panel", parent, CardColor);
        card.anchoredPosition = new Vector2(0f, y);
        card.sizeDelta = new Vector2(410f, 58f);
        CreateLabel("Label", card, label, 18f, new Color(0.92f, 0.88f, 0.95f), FontStyles.Normal,
            new Vector2(-75f, 0f), new Vector2(235f, 50f), TextAlignmentOptions.MidlineLeft);
        return CreateLabel("Value", card, "0", 25f, AccentColor, FontStyles.Bold,
            new Vector2(145f, 0f), new Vector2(95f, 50f), TextAlignmentOptions.MidlineRight);
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
        Transform value = transform.Find("GameOverContent/StatisticsPanel/" + panelName + "/Value");
        return value != null ? value.GetComponent<TextMeshProUGUI>() : null;
    }

    private void OnPlayAgainClicked()
    {
        gameManager?.ReturnToStartPanel();
    }

    private static RectTransform CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        panel.GetComponent<Image>().color = color;
        return rect;
    }

    private static TextMeshProUGUI CreateLabel(
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

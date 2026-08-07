using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds and displays the end-of-shift summary inside the scene's GameOverPanel.
/// Keeping the presentation here means the panel can be restyled in one place without
/// coupling the game timer to individual text fields.
/// </summary>
public sealed class GameOverPanel : MonoBehaviour
{
    private static readonly Color OverlayColor = new Color(0.04f, 0.06f, 0.08f, 0.88f);
    private static readonly Color CardColor = new Color(0.12f, 0.16f, 0.2f, 0.98f);
    private static readonly Color AccentColor = new Color(1f, 0.76f, 0.31f);

    private GameManager gameManager;
    private bool built;

    public void Configure(GameManager owner)
    {
        gameManager = owner;
    }

    public void Show()
    {
        BuildIfNeeded();
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void BuildIfNeeded()
    {
        if (built)
        {
            RefreshStatistics();
            return;
        }

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        Image overlay = GetComponent<Image>();
        if (overlay == null)
        {
            overlay = gameObject.AddComponent<Image>();
        }
        overlay.sprite = null;
        overlay.color = OverlayColor;

        GameObject dialog = CreatePanel("End Of Shift", transform, CardColor);
        RectTransform dialogRect = dialog.GetComponent<RectTransform>();
        dialogRect.anchorMin = dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
        dialogRect.pivot = new Vector2(0.5f, 0.5f);
        dialogRect.anchoredPosition = Vector2.zero;
        dialogRect.sizeDelta = new Vector2(580f, 510f);

        CreateText("Title", dialog.transform, "SHIFT COMPLETE", 38f, AccentColor,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -30f), new Vector2(-40f, 52f), TextAlignmentOptions.Center, FontStyles.Bold);
        CreateText("Subtitle", dialog.transform, "Here is how your cafe did", 20f, Color.white,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -76f), new Vector2(-40f, 34f), TextAlignmentOptions.Center, FontStyles.Normal);

        CreateStatCard(dialog.transform, "EARNINGS", "earningsValue", new Vector2(-143f, 75f), "0");
        CreateStatCard(dialog.transform, "ORDERS SERVED", "ordersValue", new Vector2(143f, 75f), "0");
        CreateStatCard(dialog.transform, "HAPPY GUESTS", "guestsValue", new Vector2(0f, -70f), "0");

        GameObject replayButton = CreatePanel("Play Again", dialog.transform, new Color(0.21f, 0.57f, 0.36f));
        RectTransform buttonRect = replayButton.GetComponent<RectTransform>();
        buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, -190f);
        buttonRect.sizeDelta = new Vector2(310f, 64f);
        Button button = replayButton.AddComponent<Button>();
        button.targetGraphic = replayButton.GetComponent<Image>();
        button.onClick.AddListener(PlayAgain);
        CreateText("Label", replayButton.transform, "PLAY AGAIN", 25f, Color.white,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center, FontStyles.Bold);

        built = true;
        RefreshStatistics();
    }

    private void CreateStatCard(Transform parent, string label, string valueName, Vector2 position, string value)
    {
        GameObject card = CreatePanel(label, parent, new Color(1f, 1f, 1f, 0.07f));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = position;
        cardRect.sizeDelta = new Vector2(255f, 110f);
        CreateText("Label", card.transform, label, 15f, new Color(0.75f, 0.8f, 0.85f),
            new Vector2(0f, 0.52f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-12f, -4f), TextAlignmentOptions.Center, FontStyles.Bold);
        CreateText(valueName, card.transform, value, 34f, Color.white,
            new Vector2(0f, 0f), new Vector2(1f, 0.62f), Vector2.zero, new Vector2(-12f, -4f), TextAlignmentOptions.Center, FontStyles.Bold);
    }

    private void RefreshStatistics()
    {
        TaskManager tasks = TaskManager.Instance;
        CafeCustomerDirector customers = FindAnyObjectByType<CafeCustomerDirector>();
        SetValue("earningsValue", "$" + (tasks != null ? tasks.Money : 0));
        SetValue("ordersValue", (tasks != null ? tasks.CompletedCustomerOrders : 0).ToString());
        SetValue("guestsValue", (customers != null ? customers.ServedCount : 0).ToString());
    }

    private void SetValue(string name, string value)
    {
        Transform field = transform.Find("End Of Shift/" + name) ?? FindChildRecursively(transform, name);
        if (field != null)
        {
            field.GetComponent<TextMeshProUGUI>().text = value;
        }
    }

    private void PlayAgain()
    {
        Hide();
        gameManager?.StartGame();
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private static void CreateText(string name, Transform parent, string value, float fontSize, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta,
        TextAlignmentOptions alignment, FontStyles style)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = style;
    }

    private static Transform FindChildRecursively(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                return child;
            }
            Transform result = FindChildRecursively(child, name);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }
}

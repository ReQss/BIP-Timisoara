using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class JobItemInList
{
    public Job job = null;
    public TextMeshProUGUI jobDescription;
    public TextMeshProUGUI timeLeft;
}
public class UIHandler : MonoBehaviour
{
    public GameObject pauseMenu;
    [Header("Customer order UI")]
    [SerializeField] private TMP_FontAsset pixelUiFont;
    [SerializeField] private Sprite customerOrderPanelBackground;
    [SerializeField] private Sprite customerDrinkBackground;
    [SerializeField] private Sprite customerOrderBadgeBackground;
    //5 elements for jobs
    public List<JobItemInList> jobItemsInList = new List<JobItemInList>();
    private readonly List<CustomerOrderRow> customerOrderRows = new List<CustomerOrderRow>();
    private RectTransform customerOrderPanel;
    private TextMeshProUGUI customerOrderCountText;
    private GameObject customerOrderEmptyState;
    private TextMeshProUGUI moneyText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        BindExitConfirmationButton();
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.IsGameplayInputBlocked)
        {
            return;
        }

        if(Input.GetKeyDown(KeyCode.Escape))
        {
            pauseMenu.SetActive(!pauseMenu.activeSelf);
        }
    }
    public JobItemInList GetFirstFreeJobItemInList()
    {
        for(int i = 0; i < jobItemsInList.Count; i++)
        {
            if(jobItemsInList[i].job == null || jobItemsInList[i].jobDescription.text == "")
            {
                return jobItemsInList[i];
            }
        }
        return null;
    }
    public void AddJobTextToList(Job job)
    {
        JobItemInList newJobItem = GetFirstFreeJobItemInList();
        if(newJobItem == null)
        {
            Debug.LogWarning("No free job item in list to add job text");
            return;
        }
        newJobItem.job = job;
        newJobItem.jobDescription.text = job.assignedTask.taskDescription;
        newJobItem.timeLeft.text = job.waitingTime.ToString();
    }

    public void RefreshCustomerOrders(IReadOnlyList<CustomerOrderTask> orders)
    {
        if (this == null || !isActiveAndEnabled)
        {
            return;
        }

        EnsureCustomerOrderPanel();
        if (customerOrderPanel == null)
        {
            return;
        }

        if (customerOrderCountText != null)
        {
            customerOrderCountText.text = orders.Count.ToString();
        }
        if (customerOrderEmptyState != null)
        {
            customerOrderEmptyState.SetActive(orders.Count == 0);
        }

        while (customerOrderRows.Count < orders.Count)
        {
            customerOrderRows.Add(CreateCustomerOrderRow(customerOrderRows.Count));
        }

        for (int i = 0; i < customerOrderRows.Count; i++)
        {
            bool visible = i < orders.Count;
            CustomerOrderRow row = customerOrderRows[i];
            if (row.root == null)
            {
                customerOrderRows.Clear();
                RefreshCustomerOrders(orders);
                return;
            }
            row.root.SetActive(visible);
            if (!visible)
            {
                continue;
            }

            CustomerOrderTask order = orders[i];
            row.icon.sprite = order.beverage.icon;
            row.icon.enabled = order.beverage.icon != null;
            row.label.text = order.beverage.displayName;
        }
    }

    public void SetMoney(int amount)
    {
        if (moneyText == null)
        {
            TextMeshProUGUI[] labels = FindObjectsByType<TextMeshProUGUI>();
            foreach (TextMeshProUGUI label in labels)
            {
                string value = label.text.Trim().TrimStart('$');
                bool namedMoneyValue = label.name == "Money Value";
                bool legacyMoneyValue = int.TryParse(value, out int parsed) && parsed == 300;
                if (namedMoneyValue || legacyMoneyValue)
                {
                    moneyText = label;
                    break;
                }
            }
        }

        if (moneyText != null)
        {
            moneyText.text = amount.ToString();
        }
    }

    private void EnsureCustomerOrderPanel()
    {
        if (customerOrderPanel != null)
        {
            return;
        }

        customerOrderRows.Clear();

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null && jobItemsInList.Count > 0 && jobItemsInList[0].jobDescription != null)
        {
            canvas = jobItemsInList[0].jobDescription.GetComponentInParent<Canvas>();
        }

        if (canvas == null)
        {
            canvas = FindAnyObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            return;
        }

        GameObject panel = new GameObject("Customer Order Tasks", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        customerOrderPanel = panel.GetComponent<RectTransform>();
        customerOrderPanel.anchorMin = new Vector2(1f, 1f);
        customerOrderPanel.anchorMax = new Vector2(1f, 1f);
        customerOrderPanel.pivot = new Vector2(1f, 1f);
        customerOrderPanel.anchoredPosition = new Vector2(-22f, -84f);
        customerOrderPanel.sizeDelta = new Vector2(320f, 330f);
        Image panelImage = panel.GetComponent<Image>();
        panelImage.sprite = customerOrderPanelBackground;
        panelImage.type = customerOrderPanelBackground != null ? Image.Type.Sliced : Image.Type.Simple;
        panelImage.color = customerOrderPanelBackground != null
            ? Color.white
            : new Color(0.18f, 0.13f, 0.22f, 0.94f);

        GameObject heading = new GameObject("Heading", typeof(RectTransform), typeof(TextMeshProUGUI));
        heading.transform.SetParent(panel.transform, false);
        RectTransform headingRect = heading.GetComponent<RectTransform>();
        headingRect.anchorMin = new Vector2(0f, 1f);
        headingRect.anchorMax = new Vector2(1f, 1f);
        headingRect.pivot = new Vector2(0.5f, 1f);
        headingRect.anchoredPosition = new Vector2(-12f, -20f);
        headingRect.sizeDelta = new Vector2(-74f, 38f);
        TextMeshProUGUI headingText = heading.GetComponent<TextMeshProUGUI>();
        headingText.text = "CUSTOMER ORDERS";
        if (pixelUiFont != null) headingText.font = pixelUiFont;
        headingText.fontSize = 21f;
        headingText.fontStyle = FontStyles.Bold;
        headingText.alignment = TextAlignmentOptions.MidlineLeft;
        headingText.color = new Color(0.25f, 0.15f, 0.3f);
        headingText.raycastTarget = false;

        GameObject badge = new GameObject("Order Count", typeof(RectTransform), typeof(Image));
        badge.transform.SetParent(panel.transform, false);
        RectTransform badgeRect = badge.GetComponent<RectTransform>();
        badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(1f, 1f);
        badgeRect.pivot = new Vector2(1f, 1f);
        badgeRect.anchoredPosition = new Vector2(-24f, -15f);
        badgeRect.sizeDelta = new Vector2(48f, 48f);
        Image badgeImage = badge.GetComponent<Image>();
        badgeImage.sprite = customerOrderBadgeBackground;
        badgeImage.type = customerOrderBadgeBackground != null ? Image.Type.Sliced : Image.Type.Simple;
        badgeImage.color = customerOrderBadgeBackground != null ? Color.white : new Color(0.78f, 0.9f, 0.77f);

        GameObject count = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
        count.transform.SetParent(badge.transform, false);
        RectTransform countRect = count.GetComponent<RectTransform>();
        countRect.anchorMin = Vector2.zero;
        countRect.anchorMax = Vector2.one;
        countRect.offsetMin = Vector2.zero;
        countRect.offsetMax = Vector2.zero;
        customerOrderCountText = count.GetComponent<TextMeshProUGUI>();
        customerOrderCountText.text = "0";
        if (pixelUiFont != null) customerOrderCountText.font = pixelUiFont;
        customerOrderCountText.fontSize = 22f;
        customerOrderCountText.fontStyle = FontStyles.Bold;
        customerOrderCountText.alignment = TextAlignmentOptions.Center;
        customerOrderCountText.color = new Color(0.24f, 0.35f, 0.24f);
        customerOrderCountText.raycastTarget = false;

        customerOrderEmptyState = new GameObject("Empty State", typeof(RectTransform), typeof(TextMeshProUGUI));
        customerOrderEmptyState.transform.SetParent(panel.transform, false);
        RectTransform emptyRect = customerOrderEmptyState.GetComponent<RectTransform>();
        emptyRect.anchorMin = new Vector2(0f, 1f);
        emptyRect.anchorMax = new Vector2(1f, 1f);
        emptyRect.pivot = new Vector2(0.5f, 1f);
        emptyRect.anchoredPosition = new Vector2(0f, -104f);
        emptyRect.sizeDelta = new Vector2(-58f, 80f);
        TextMeshProUGUI emptyText = customerOrderEmptyState.GetComponent<TextMeshProUGUI>();
        emptyText.text = "NO ORDERS YET\nKEEP AN EYE ON THE TABLES!";
        if (pixelUiFont != null) emptyText.font = pixelUiFont;
        emptyText.fontSize = 17f;
        emptyText.alignment = TextAlignmentOptions.Center;
        emptyText.color = new Color(0.42f, 0.32f, 0.46f);
        emptyText.raycastTarget = false;
    }

    private CustomerOrderRow CreateCustomerOrderRow(int index)
    {
        GameObject root = new GameObject("Order " + (index + 1), typeof(RectTransform), typeof(Image));
        root.transform.SetParent(customerOrderPanel, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -72f - index * 48f);
        rect.sizeDelta = new Vector2(-42f, 42f);
        Image background = root.GetComponent<Image>();
        background.sprite = customerDrinkBackground;
        background.type = customerDrinkBackground != null ? Image.Type.Sliced : Image.Type.Simple;
        background.color = customerDrinkBackground != null
            ? Color.white
            : new Color(1f, 1f, 1f, 0.08f);

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(root.transform, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(9f, 0f);
        // Two physical pixels per source pixel keeps 16x16 art evenly scaled.
        iconRect.sizeDelta = new Vector2(32f, 32f);
        Image icon = iconObject.GetComponent<Image>();
        icon.preserveAspect = true;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(root.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(50f, 0f);
        labelRect.offsetMax = new Vector2(-10f, 0f);
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        if (pixelUiFont != null) label.font = pixelUiFont;
        label.fontSize = 17f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = customerDrinkBackground != null
            ? new Color(0.22f, 0.15f, 0.25f)
            : Color.white;
        label.raycastTarget = false;

        return new CustomerOrderRow(root, icon, label);
    }
    
       
    public void CleanJobList()
    {
        foreach (var jobItem in jobItemsInList)
        {
            jobItem.job = null;
            jobItem.jobDescription.text = "";
            jobItem.timeLeft.text = "";
        }
    }
    public void RemoveJobTextFromList(int jobId)
    {
        JobItemInList jobToRemove = jobItemsInList.Find(job => job.job != null && job.job.jobId == jobId);
        if (jobToRemove != null)
        {
            jobToRemove.job = null;
            jobToRemove.jobDescription.text = "";
            jobToRemove.timeLeft.text = "";
        }
        //reorganize the list and push items to the top of the list
        //sort by job id
        jobItemsInList.Sort((a, b) => 
        {
            if (a.job == null && b.job == null) return 0;
            if (a.job == null) return 1;
            if (b.job == null) return -1;
            return a.job.jobId.CompareTo(b.job.jobId);
        });
    }
    public void LoadScene(string sceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit(0);
#endif
    }

    private void BindExitConfirmationButton()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button button in buttons)
        {
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null || !string.Equals(label.text.Trim(), "YES", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Replace the serialized prefab event entirely. Prefab instance targets
            // can become invalid in a standalone build even when they work in-editor.
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(QuitGame);
        }
    }

    public void ActiveOrDisablePanel(GameObject panel)
    {
        panel.SetActive(!panel.activeSelf);
    }

    private void OnDestroy()
    {
        customerOrderRows.Clear();
        customerOrderPanel = null;
        customerOrderCountText = null;
        customerOrderEmptyState = null;
        moneyText = null;
    }
}

internal sealed class CustomerOrderRow
{
    public readonly GameObject root;
    public readonly Image icon;
    public readonly TextMeshProUGUI label;

    public CustomerOrderRow(GameObject root, Image icon, TextMeshProUGUI label)
    {
        this.root = root;
        this.icon = icon;
        this.label = label;
    }
}

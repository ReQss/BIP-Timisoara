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
    [SerializeField] private Sprite customerDrinkBackground;
    //5 elements for jobs
    public List<JobItemInList> jobItemsInList = new List<JobItemInList>();
    private readonly List<CustomerOrderRow> customerOrderRows = new List<CustomerOrderRow>();
    private RectTransform customerOrderPanel;
    private TextMeshProUGUI moneyText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

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
        customerOrderPanel.anchoredPosition = new Vector2(-24f, -90f);
        customerOrderPanel.sizeDelta = new Vector2(280f, 260f);
        panel.GetComponent<Image>().color = new Color(0.08f, 0.11f, 0.13f, 0.9f);

        GameObject heading = new GameObject("Heading", typeof(RectTransform), typeof(TextMeshProUGUI));
        heading.transform.SetParent(panel.transform, false);
        RectTransform headingRect = heading.GetComponent<RectTransform>();
        headingRect.anchorMin = new Vector2(0f, 1f);
        headingRect.anchorMax = new Vector2(1f, 1f);
        headingRect.pivot = new Vector2(0.5f, 1f);
        headingRect.anchoredPosition = new Vector2(0f, -10f);
        headingRect.sizeDelta = new Vector2(-20f, 34f);
        TextMeshProUGUI headingText = heading.GetComponent<TextMeshProUGUI>();
        headingText.text = "CUSTOMER ORDERS";
        headingText.fontSize = 22f;
        headingText.fontStyle = FontStyles.Bold;
        headingText.alignment = TextAlignmentOptions.Center;
        headingText.color = new Color(1f, 0.86f, 0.55f);
    }

    private CustomerOrderRow CreateCustomerOrderRow(int index)
    {
        GameObject root = new GameObject("Order " + (index + 1), typeof(RectTransform), typeof(Image));
        root.transform.SetParent(customerOrderPanel, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -50f - index * 40f);
        rect.sizeDelta = new Vector2(-20f, 34f);
        Image background = root.GetComponent<Image>();
        background.sprite = customerDrinkBackground;
        background.color = customerDrinkBackground != null
            ? Color.white
            : new Color(1f, 1f, 1f, 0.08f);

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(root.transform, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(5f, 0f);
        // Two physical pixels per source pixel keeps 16x16 art evenly scaled.
        iconRect.sizeDelta = new Vector2(32f, 32f);
        Image icon = iconObject.GetComponent<Image>();
        icon.preserveAspect = true;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(root.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(42f, 0f);
        labelRect.offsetMax = new Vector2(-6f, 0f);
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.fontSize = 18f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = customerDrinkBackground != null
            ? new Color(0.22f, 0.15f, 0.25f)
            : Color.white;

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
    public void ActiveOrDisablePanel(GameObject panel)
    {
        panel.SetActive(!panel.activeSelf);
    }

    private void OnDestroy()
    {
        customerOrderRows.Clear();
        customerOrderPanel = null;
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

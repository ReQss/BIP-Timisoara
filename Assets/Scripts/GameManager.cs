using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject startPanel;

    [Header("Timer")]
    [SerializeField] private float startTime = 120f; // 2 minuty

    private bool isGameActive;
    private float currentTime;

    private void Awake()
    {
        isGameActive = false;
    }

    private void Start()
    {
        currentTime = startTime;
        UpdateTimerUI();
    }

    private void Update()
    {
        if (!isGameActive)
            return;

        currentTime -= Time.deltaTime;

        if (currentTime <= 0f)
        {
            currentTime = 0f;
            isGameActive = false;
            timerText.text = "OVER";
            return;
        }

        UpdateTimerUI();
    }

    public void StartGame()
    {
        currentTime = startTime;
        isGameActive = true;

        if (startPanel != null)
            startPanel.SetActive(false);

        UpdateTimerUI();
    }

    public bool IsGameActive()
    {
        return isGameActive;
    }

    private void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);

        timerText.text = $"{minutes}:{seconds:00}";
    }
}
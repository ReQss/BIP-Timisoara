using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Timer")]
    [SerializeField] private float startTime = 120f; // 2 minuty

    private bool isGameActive;
    private float currentTime;
    private int gameStartedFrame = -1;

    private static GameManager instance;

    public static bool IsGameplayInputBlocked =>
        instance != null && (!instance.isGameActive || instance.IsStartInputBlocked());

    private void Awake()
    {
        instance = this;
        isGameActive = false;
        if (gameOverPanel != null)
        {
            gameOverPanel.GetComponent<GameOverPanel>()?.Configure(this);
            gameOverPanel.SetActive(false);
        }
    }

    private void Start()
    {
        currentTime = startTime;

        if (startPanel != null)
            startPanel.SetActive(true);

        UpdateTimerUI();
    }

    private void Update()
    {
        if (startPanel != null && startPanel.activeInHierarchy)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
                StartGame();

            return;
        }

        if (!isGameActive)
            return;

        currentTime -= Time.deltaTime;

        if (currentTime <= 0f)
        {
            currentTime = 0f;
            isGameActive = false;
            timerText.text = "OVER";
            ShowGameOverPanel();
            return;
        }

        UpdateTimerUI();
    }

    public void StartGame()
    {
        currentTime = startTime;
        isGameActive = true;
        gameStartedFrame = Time.frameCount;
        TaskManager.Instance?.ResetRoundStatistics();
        FindAnyObjectByType<CafeCustomerDirector>()?.ResetRoundStatistics();

        if (startPanel != null)
            startPanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        UpdateTimerUI();
    }

    public bool IsGameActive()
    {
        return isGameActive;
    }

    private bool IsStartInputBlocked()
    {
        bool startMenuVisible = startPanel != null && startPanel.activeInHierarchy;
        return startMenuVisible || Time.frameCount == gameStartedFrame;
    }

    private void ShowGameOverPanel()
    {
        if (gameOverPanel == null)
        {
            return;
        }

        GameOverPanel panel = gameOverPanel.GetComponent<GameOverPanel>();
        if (panel == null)
        {
            panel = gameOverPanel.AddComponent<GameOverPanel>();
            panel.Configure(this);
        }
        panel.Show();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);

        timerText.text = $"{minutes}:{seconds:00}";
    }
}

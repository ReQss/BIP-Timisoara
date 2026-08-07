using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_FontAsset pixelUiFont;
    [SerializeField] private Sprite gameOverPanelSprite;
    [SerializeField] private Sprite gameOverCardSprite;
    [SerializeField] private Sprite gameOverButtonSprite;

    [Header("Timer")]
    [SerializeField] private float startTime = 120f; // 2 minuty

    private bool isGameActive;
    private float currentTime;
    private int gameStartedFrame = -1;
    private GameOverPanelView gameOverView;

    private static GameManager instance;
    private static bool startImmediatelyAfterReload;

    public static bool IsGameplayInputBlocked =>
        instance != null && (!instance.isGameActive || Time.frameCount == instance.gameStartedFrame);

    // Scenes without a GameManager retain their previous always-running behaviour.
    public static bool IsGameActiveNow => instance == null || instance.isGameActive;

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

        EnsureGameOverPanel();
        gameOverView?.Hide();

        bool shouldStartImmediately = startImmediatelyAfterReload;
        startImmediatelyAfterReload = false;
        if (shouldStartImmediately)
        {
            StartGame();
            return;
        }

        if (startPanel != null)
        {
            startPanel.SetActive(true);
        }

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
            EndGame();
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

        gameOverView?.Hide();

        UpdateTimerUI();
    }

    public bool IsGameActive()
    {
        return isGameActive;
    }

    public void ReturnToStartPanel()
    {
        isGameActive = false;
        gameStartedFrame = -1;

        FindAnyObjectByType<BeverageFridge>()?.CloseAllSelections();
        FindAnyObjectByType<CafeCustomerDirector>()?.ResetRound();
        TaskManager.Instance?.ResetRound();

        gameOverView?.Hide();
        if (startPanel != null)
        {
            startPanel.SetActive(true);
        }

        currentTime = startTime;
        UpdateTimerUI();
    }

    public void RestartGame()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex < 0)
        {
            ReturnToStartPanel();
            StartGame();
            return;
        }

        startImmediatelyAfterReload = true;
        SceneManager.LoadScene(activeScene.buildIndex, LoadSceneMode.Single);
    }

    private void EndGame()
    {
        isGameActive = false;
        if (timerText != null)
        {
            timerText.text = "OVER";
        }

        EnsureGameOverPanel();
        CafeCustomerDirector director = FindAnyObjectByType<CafeCustomerDirector>();
        int served = TaskManager.Instance != null
            ? TaskManager.Instance.CompletedOrderCount
            : director != null ? director.ServedCount : 0;
        int moneyEarned = TaskManager.Instance != null ? TaskManager.Instance.MoneyEarned : 0;
        int catDelivered = TaskManager.Instance != null ? TaskManager.Instance.CatDeliveredCount : 0;
        int dogDelivered = TaskManager.Instance != null ? TaskManager.Instance.DogDeliveredCount : 0;
        gameOverView?.Show(served, moneyEarned, catDelivered, dogDelivered);
    }

    private void EnsureGameOverPanel()
    {
        if (gameOverPanel == null)
        {
            gameOverPanel = FindSceneObject("GameOverPanel");
        }

        if (gameOverPanel == null)
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("A Canvas is required to display the game-over panel.", this);
                return;
            }

            gameOverPanel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameOverPanel.transform.SetParent(canvas.transform, false);
            RectTransform rect = gameOverPanel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            gameOverPanel.GetComponent<Image>().color = new Color(0.04f, 0.03f, 0.06f, 0.88f);
        }

        gameOverPanel.transform.SetAsLastSibling();
        RectTransform panelRect = gameOverPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
        }
        gameOverView = gameOverPanel.GetComponent<GameOverPanelView>();
        if (gameOverView == null)
        {
            gameOverView = gameOverPanel.AddComponent<GameOverPanelView>();
        }
        gameOverView.Initialize(this, pixelUiFont, gameOverPanelSprite, gameOverCardSprite, gameOverButtonSprite);
    }

    private static GameObject FindSceneObject(string objectName)
    {
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform descendant in descendants)
            {
                if (descendant.name == objectName)
                {
                    return descendant.gameObject;
                }
            }
        }

        return null;
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
        if (timerText == null)
        {
            return;
        }

        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);

        timerText.text = $"{minutes}:{seconds:00}";
    }
}

using UnityEngine;
using UnityEngine.UI;

public class SplitScreenManager : MonoBehaviour
{
    [Header("Players")]
    [SerializeField] private Transform player1;
    [SerializeField] private Transform player2;

    [Header("Cameras")]
    [SerializeField] private Camera camera1;
    [SerializeField] private Camera camera2;

    [Header("Camera Follow Scripts")]
    [SerializeField] private PlayerCameraFollow follow1;
    [SerializeField] private PlayerCameraFollow follow2;

    [Header("Divider")]
    [SerializeField] private SplitScreenDivider divider;
    [SerializeField] private Sprite dividerSprite;

    [Header("Diagonal Split Rendering")]
    [SerializeField] private DiagonalSplitScreenComposite diagonalComposite;

    [Header("Distances")]
    [SerializeField] private float splitDistance = 10f;
    [SerializeField] private float mergeDistance = 7f;

    [Header("Animation Smoothness")]
    [SerializeField] private float smoothTime = 0.3f; // Czas trwania płynnego przejścia

    private float splitAmount;
    private float splitVelocity; // Używane wewnętrznie przez Mathf.SmoothDamp
    private int menuSplitLocks;

    private void Awake()
    {
        EnsureDivider();
        EnsureDiagonalComposite();
    }

    public void PushMenuSplit()
    {
        menuSplitLocks++;
        splitAmount = 1f;
        splitVelocity = 0f;
        UpdateRects();
    }

    public void PopMenuSplit()
    {
        menuSplitLocks = Mathf.Max(0, menuSplitLocks - 1);
    }

    private void Start()
    {
        // Na starcie ustawiamy pełen ekran dla kamery 1
        camera1.rect = new Rect(0f, 0f, 1f, 1f);
        camera2.rect = new Rect(0f, 0f, 1f, 1f);

        splitAmount = 0f;
    }

    private void Update()
    {
        if (player1 == null || player2 == null) return;

        float distance = Vector2.Distance(player1.position, player2.position);

        // Histereza - określenie docelowego stanu podziału
        float targetSplit = splitAmount;
        if (menuSplitLocks > 0)
        {
            targetSplit = 1f;
        }
        else if (distance > splitDistance)
        {
            targetSplit = 1f;
        }
        else if (distance < mergeDistance)
        {
            targetSplit = 0f;
        }

        // Płynna interpolacja z użyciem SmoothDamp (efekt wyhamowania przy końcu)
        splitAmount = Mathf.SmoothDamp(
            splitAmount, 
            targetSplit, 
            ref splitVelocity, 
            smoothTime);

        // Przekazanie wartości do skryptów podążania
        if (follow1 != null) follow1.SetSplitAmount(splitAmount);
        if (follow2 != null) follow2.SetSplitAmount(splitAmount);

        UpdateRects();
    }

    private void UpdateRects()
    {
        if (divider != null)
        {
            divider.SetSplitAmount(splitAmount);
        }

        if (diagonalComposite != null)
        {
            diagonalComposite.SetSplitAmount(splitAmount);
            return;
        }

        // Wyłączenie 2. kamery gdy podział nie jest używany (optymalizacja)
        if (splitAmount < 0.01f)
        {
            camera1.rect = new Rect(0f, 0f, 1f, 1f);
            camera2.enabled = false;
            return;
        }

        if (!camera2.enabled) camera2.enabled = true;

        // Obliczanie proporcji ekranu
        float leftWidth = Mathf.Lerp(1f, 0.5f, splitAmount);

        camera1.rect = new Rect(0f, 0f, leftWidth, 1f);
        camera2.rect = new Rect(leftWidth, 0f, 1f - leftWidth, 1f);
    }

    private void EnsureDivider()
    {
        if (divider != null)
        {
            divider.SetSprite(dividerSprite);
            return;
        }

        GameObject canvasObject = new GameObject(
            "Split Screen Divider Canvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas dividerCanvas = canvasObject.GetComponent<Canvas>();
        dividerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        dividerCanvas.overrideSorting = true;
        dividerCanvas.sortingOrder = 50;

        GameObject lineObject = new GameObject("Split Screen Divider", typeof(RectTransform));

        lineObject.transform.SetParent(canvasObject.transform, false);
        Image lineImage = lineObject.AddComponent<Image>();
        lineImage.raycastTarget = false;

        divider = lineObject.AddComponent<SplitScreenDivider>();
        divider.SetSprite(dividerSprite);
    }

    private void EnsureDiagonalComposite()
    {
        if (diagonalComposite == null)
        {
            diagonalComposite = gameObject.AddComponent<DiagonalSplitScreenComposite>();
        }

        diagonalComposite.Initialize(camera1, camera2);
    }
}

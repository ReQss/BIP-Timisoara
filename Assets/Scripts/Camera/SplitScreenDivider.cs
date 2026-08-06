using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws a decorative UI divider over the split-screen boundary.
/// Attach it to a UI Image on a Screen Space - Overlay canvas. The Image's
/// Source Image is intentionally exposed so it can be replaced with a sprite.
/// </summary>
[RequireComponent(typeof(Image))]
public class SplitScreenDivider : MonoBehaviour
{
    [Header("Appearance")]
    [SerializeField, Range(-45f, 45f)] private float angle = 15f;
    [SerializeField, Min(1f)] private float thickness = 8f;
    [SerializeField] private Color color = new Color(0.12f, 0.08f, 0.05f, 1f);

    [Header("Animation")]
    [SerializeField, Min(0f)] private float edgePadding = 24f;
    [SerializeField] private AnimationCurve visibility = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private RectTransform dividerRect;
    private RectTransform canvasRect;
    private Image image;

    private void Awake()
    {
        dividerRect = (RectTransform)transform;
        image = GetComponent<Image>();
        canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();

        dividerRect.anchorMin = new Vector2(0.5f, 0.5f);
        dividerRect.anchorMax = new Vector2(0.5f, 0.5f);
        dividerRect.pivot = new Vector2(0.5f, 0.5f);
        dividerRect.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    /// <summary>Optionally replaces the default UI line with a game-specific sprite.</summary>
    public void SetSprite(Sprite sprite)
    {
        if (image == null)
        {
            image = GetComponent<Image>();
        }

        image.sprite = sprite;
    }

    /// <summary>Called by SplitScreenManager with its smoothly animated split value.</summary>
    public void SetSplitAmount(float splitAmount)
    {
        if (dividerRect == null || canvasRect == null)
        {
            return;
        }

        float amount = Mathf.Clamp01(splitAmount);
        float visibleAmount = visibility.Evaluate(amount);
        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;

        // This is the same diagonal seam used by DiagonalSplitScreenComposite.
        float aspect = canvasWidth / canvasHeight;
        float diagonalOffset = Mathf.Tan(angle * Mathf.Deg2Rad) / aspect * 0.5f;
        float boundary = Mathf.Lerp(1f + diagonalOffset, 0.5f, amount);
        float boundaryX = (boundary - 0.5f) * canvasWidth;
        dividerRect.anchoredPosition = new Vector2(boundaryX, 0f);

        // Extra length keeps the angled line beyond both screen edges.
        float fullLength = canvasHeight / Mathf.Cos(angle * Mathf.Deg2Rad) + edgePadding * 2f;
        dividerRect.sizeDelta = new Vector2(thickness, fullLength * visibleAmount);

        Color dividerColor = color;
        dividerColor.a *= visibleAmount;
        image.color = dividerColor;
        image.enabled = visibleAmount > 0.001f;
    }
}

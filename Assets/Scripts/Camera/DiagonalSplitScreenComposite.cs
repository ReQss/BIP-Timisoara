using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders both cameras into full-screen textures and clips them at a shared
/// diagonal seam. Camera.rect cannot create a non-rectangular viewport.
/// </summary>
public class DiagonalSplitScreenComposite : MonoBehaviour
{
    private const string ShaderName = "UI/DiagonalSplitMask";

    [SerializeField, Range(-45f, 45f)] private float seamAngle = 15f;
    [SerializeField] private int sortingOrder = -100;

    private Camera camera1;
    private Camera camera2;
    private Canvas canvas;
    private RawImage leftImage;
    private RawImage rightImage;
    private Material leftMaterial;
    private Material rightMaterial;
    private RenderTexture leftTexture;
    private RenderTexture rightTexture;
    private int textureWidth;
    private int textureHeight;
    private int screenWidth;
    private int screenHeight;

    public void Initialize(Camera firstCamera, Camera secondCamera)
    {
        camera1 = firstCamera;
        camera2 = secondCamera;

        if (camera1 == null || camera2 == null)
        {
            enabled = false;
            return;
        }

        CreateOverlay();
        camera1.rect = new Rect(0f, 0f, 1f, 1f);
        camera2.rect = new Rect(0f, 0f, 1f, 1f);
        camera1.enabled = true;
        camera2.enabled = false;
        canvas.enabled = false;
    }

    public void SetSplitAmount(float amount)
    {
        if (!enabled || leftMaterial == null || rightMaterial == null)
        {
            return;
        }

        float splitAmount = Mathf.Clamp01(amount);
        if (splitAmount < 0.01f)
        {
            canvas.enabled = false;
            camera1.targetTexture = null;
            camera2.targetTexture = null;
            camera1.ResetAspect();
            camera2.ResetAspect();
            camera1.enabled = true;
            camera2.enabled = false;
            return;
        }

        canvas.enabled = true;
        camera1.enabled = true;
        camera2.enabled = true;
        EnsureRenderTextures();

        float aspect = (float)Screen.width / Screen.height;
        float diagonalOffset = Mathf.Tan(seamAngle * Mathf.Deg2Rad) / aspect * 0.5f;
        float boundary = Mathf.Lerp(1f + diagonalOffset, 0.5f, splitAmount);

        ConfigureCameraPanels(boundary, diagonalOffset, aspect);

        leftMaterial.SetFloat("_Boundary", boundary);
        rightMaterial.SetFloat("_Boundary", boundary);
        leftMaterial.SetFloat("_Aspect", aspect);
        rightMaterial.SetFloat("_Aspect", aspect);
        leftMaterial.SetFloat("_Angle", seamAngle);
        rightMaterial.SetFloat("_Angle", seamAngle);
    }

    private void CreateOverlay()
    {
        if (canvas != null)
        {
            return;
        }

        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogError($"Could not find {ShaderName}. The diagonal split cannot be rendered.");
            enabled = false;
            return;
        }

        GameObject canvasObject = new GameObject("Diagonal Split Screen", typeof(Canvas), typeof(CanvasScaler));
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        leftMaterial = new Material(shader);
        rightMaterial = new Material(shader);
        leftMaterial.SetFloat("_Side", -1f);
        rightMaterial.SetFloat("_Side", 1f);

        leftImage = CreateImage("Left Camera", leftMaterial);
        rightImage = CreateImage("Right Camera", rightMaterial);
    }

    private RawImage CreateImage(string objectName, Material material)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(RawImage));
        imageObject.transform.SetParent(canvas.transform, false);

        RectTransform rect = (RectTransform)imageObject.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        RawImage image = imageObject.GetComponent<RawImage>();
        image.material = material;
        image.raycastTarget = false;
        return image;
    }

    private void EnsureRenderTextures()
    {
        if (Screen.width == screenWidth && Screen.height == screenHeight && leftTexture != null)
        {
            camera1.targetTexture = leftTexture;
            camera2.targetTexture = rightTexture;
            return;
        }

        ReleaseRenderTextures();
        screenWidth = Screen.width;
        screenHeight = Screen.height;
        // The camera projection is set to each panel's aspect every frame.
        // Keeping the textures full size avoids repeatedly reallocating them
        // during the split animation.
        textureWidth = Screen.width;
        textureHeight = Screen.height;

        leftTexture = CreateRenderTexture("Left Camera Texture");
        rightTexture = CreateRenderTexture("Right Camera Texture");
        camera1.targetTexture = leftTexture;
        camera2.targetTexture = rightTexture;
        leftImage.texture = leftTexture;
        rightImage.texture = rightTexture;
    }

    private void ConfigureCameraPanels(float boundary, float diagonalOffset, float screenAspect)
    {
        // The panels shrink with the animated seam. Their cameras use the
        // exact same aspect ratio, so the view changes its field of view
        // naturally instead of being stretched by the UI.
        float leftMin = -diagonalOffset;
        float leftWidth = boundary + diagonalOffset - leftMin;
        float rightMin = boundary - diagonalOffset;
        float rightWidth = 1f + diagonalOffset - rightMin;

        SetPanelRect(leftImage.rectTransform, leftMin, leftWidth);
        SetPanelRect(rightImage.rectTransform, rightMin, rightWidth);

        leftMaterial.SetFloat("_RectMinX", leftMin);
        rightMaterial.SetFloat("_RectMinX", rightMin);
        leftMaterial.SetFloat("_RectWidth", leftWidth);
        rightMaterial.SetFloat("_RectWidth", rightWidth);

        camera1.aspect = screenAspect * leftWidth;
        camera2.aspect = screenAspect * rightWidth;
    }

    private static void SetPanelRect(RectTransform rect, float minX, float width)
    {
        rect.anchorMin = new Vector2(minX, 0f);
        rect.anchorMax = new Vector2(minX + width, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private RenderTexture CreateRenderTexture(string textureName)
    {
        RenderTexture texture = new RenderTexture(textureWidth, textureHeight, 24, RenderTextureFormat.ARGB32)
        {
            name = textureName,
            filterMode = FilterMode.Point,
            antiAliasing = 1
        };
        texture.Create();
        return texture;
    }

    private void ReleaseRenderTextures()
    {
        if (leftTexture != null)
        {
            leftTexture.Release();
            Destroy(leftTexture);
            leftTexture = null;
        }

        if (rightTexture != null)
        {
            rightTexture.Release();
            Destroy(rightTexture);
            rightTexture = null;
        }
    }

    private void OnDestroy()
    {
        ReleaseRenderTextures();
        if (leftMaterial != null) Destroy(leftMaterial);
        if (rightMaterial != null) Destroy(rightMaterial);
        if (canvas != null) Destroy(canvas.gameObject);
    }
}

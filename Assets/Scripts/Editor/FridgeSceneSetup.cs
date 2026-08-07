#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;

public static class FridgeSceneSetup
{
    private const string ScenePath = "Assets/Scenes/The cafe.unity";
    private const string UiSheetPath = "Assets/UI/UIBundleFree/PastelUIFree.png";
    private const string PixelFontPath = "Assets/Fonts/PixelifySans-Bold SDF.asset";

    [MenuItem("Tools/Cat Cafe/Put Fridge And UI In Scene")]
    public static void ApplyToCafeScene()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        CreateOrUpdateFridge(scene, null);
        AssignPastelUiTheme(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    public static void CreateOrUpdateFridge(Scene scene, BeverageDefinition[] beverages)
    {
        Sprite menuBackground = LoadSprite(UiSheetPath, "PastelUIFree_0");
        GameObject fridge = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Beverage Fridge");
        bool created = fridge == null;
        if (created)
        {
            fridge = new GameObject("Beverage Fridge");
            SceneManager.MoveGameObjectToScene(fridge, scene);
        }

        SpriteRenderer renderer = fridge.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = fridge.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 20;

        BeverageFridge component = fridge.GetComponent<BeverageFridge>();
        if (component == null) component = fridge.AddComponent<BeverageFridge>();
        if (beverages != null && beverages.Length > 0) component.Configure(beverages);

        Transform oldTemplate = fridge.transform.Find("Fridge Menu Template");
        if (oldTemplate != null) UnityEngine.Object.DestroyImmediate(oldTemplate.gameObject);
        FridgeMenuView template = CreateMenuTemplate(fridge.transform, menuBackground);
        SerializedObject fridgeData = new SerializedObject(component);
        fridgeData.FindProperty("menuTemplate").objectReferenceValue = template;
        fridgeData.ApplyModifiedPropertiesWithoutUndo();

        if (created)
        {
            Tilemap kitchen = FindTilemap(scene, "Kitchen");
            Bounds bounds = kitchen != null
                ? kitchen.localBounds
                : new Bounds(new Vector3(4f, 2f), new Vector3(4f, 4f));
            Vector3 center = kitchen != null ? kitchen.transform.TransformPoint(bounds.center) : bounds.center;
            fridge.transform.position = center + new Vector3(bounds.extents.x - 0.75f, 0f, 0f);
        }

        EditorUtility.SetDirty(fridge);
        EditorUtility.SetDirty(component);
    }

    private static FridgeMenuView CreateMenuTemplate(Transform parent, Sprite backgroundSprite)
    {
        GameObject root = new GameObject("Fridge Menu Template", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(FridgeMenuView));
        root.transform.SetParent(parent, false);
        root.transform.localScale = Vector3.one;

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.planeDistance = 1f;
        canvas.sortingOrder = 5000;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = CreateUiObject("Pastel Drink Menu", root.transform, typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(700f, 550f);
        Image panelImage = panel.GetComponent<Image>();
        panelImage.sprite = backgroundSprite;
        panelImage.color = Color.white;

        Text title = CreateLabel(panel.transform, "CHOOSE A DRINK", new Vector2(0f, 220f),
            new Vector2(560f, 46f), 28, FontStyle.Bold);
        title.gameObject.name = "Title";

        GameObject gridObject = CreateUiObject("Drinks", panel.transform, typeof(GridLayoutGroup));
        RectTransform gridRect = gridObject.GetComponent<RectTransform>();
        gridRect.anchorMin = gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.sizeDelta = new Vector2(600f, 390f);
        gridRect.anchoredPosition = new Vector2(0f, -28f);
        GridLayoutGroup grid = gridObject.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(108f, 85f);
        grid.spacing = new Vector2(10f, 10f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.childAlignment = TextAnchor.UpperCenter;

        GameObject closeObject = CreateUiObject("Close", panel.transform, typeof(Image), typeof(Button));
        RectTransform closeRect = closeObject.GetComponent<RectTransform>();
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-48f, -42f);
        closeRect.sizeDelta = new Vector2(42f, 42f);
        closeObject.GetComponent<Image>().color = new Color(0.98f, 0.77f, 0.8f);
        CreateLabel(closeObject.transform, "X", Vector2.zero, new Vector2(42f, 42f), 22, FontStyle.Bold);

        FridgeMenuView view = root.GetComponent<FridgeMenuView>();
        SerializedObject viewData = new SerializedObject(view);
        viewData.FindProperty("menuCanvas").objectReferenceValue = canvas;
        viewData.FindProperty("title").objectReferenceValue = title;
        viewData.FindProperty("drinksContainer").objectReferenceValue = gridRect;
        viewData.FindProperty("closeButton").objectReferenceValue = closeObject.GetComponent<Button>();
        viewData.ApplyModifiedPropertiesWithoutUndo();
        root.SetActive(false);
        return view;
    }

    private static void AssignPastelUiTheme(Scene scene)
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelFontPath);
        Sprite panel = LoadSprite(UiSheetPath, "PastelUIFree_1");
        Sprite row = LoadSprite(UiSheetPath, "PastelUIFree_4");
        Sprite card = LoadSprite(UiSheetPath, "PastelUIFree_2");
        foreach (UIHandler handler in scene.GetRootGameObjects()
                     .SelectMany(root => root.GetComponentsInChildren<UIHandler>(true)))
        {
            SerializedObject data = new SerializedObject(handler);
            data.FindProperty("pixelUiFont").objectReferenceValue = font;
            data.FindProperty("customerOrderPanelBackground").objectReferenceValue = panel;
            data.FindProperty("customerDrinkBackground").objectReferenceValue = row;
            data.FindProperty("customerOrderBadgeBackground").objectReferenceValue = card;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(handler);
        }

        foreach (GameManager manager in scene.GetRootGameObjects()
                     .SelectMany(root => root.GetComponentsInChildren<GameManager>(true)))
        {
            SerializedObject data = new SerializedObject(manager);
            data.FindProperty("pixelUiFont").objectReferenceValue = font;
            data.FindProperty("gameOverPanelSprite").objectReferenceValue =
                LoadSprite(UiSheetPath, "PastelUIFree_0");
            data.FindProperty("gameOverCardSprite").objectReferenceValue = card;
            data.FindProperty("gameOverButtonSprite").objectReferenceValue =
                LoadSprite(UiSheetPath, "PastelUIFree_82");
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
        }
    }

    private static Sprite LoadSprite(string path, string spriteName)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .FirstOrDefault(sprite => sprite.name == spriteName);
    }

    private static Tilemap FindTilemap(Scene scene, string objectName)
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
            .FirstOrDefault(tilemap => string.Equals(tilemap.name, objectName, StringComparison.OrdinalIgnoreCase));
    }

    private static GameObject CreateUiObject(string name, Transform parent, params Type[] components)
    {
        Type[] allComponents = new Type[components.Length + 1];
        allComponents[0] = typeof(RectTransform);
        Array.Copy(components, 0, allComponents, 1, components.Length);
        GameObject result = new GameObject(name, allComponents);
        result.transform.SetParent(parent, false);
        return result;
    }

    private static Text CreateLabel(Transform parent, string value, Vector2 position, Vector2 size,
        int fontSize, FontStyle style)
    {
        GameObject labelObject = CreateUiObject("Label", parent, typeof(Text));
        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Text label = labelObject.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = value;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.22f, 0.15f, 0.25f);
        return label;
    }
}
#endif

using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class FrogCafeSetup
{
    private const string CharacterSheet = "Assets/Assets/Sprites/8Direction_TopDown_Character Sprites_ByBossNelNel/SpriteSheet.png";
    private const string BeverageFolder = "Assets/Assets/Sprites/PixelBeveragesAndDrink16x16/Items";
    private const string PrefabPath = "Assets/Prefabs/Frog Customer.prefab";
    private const string ScenePath = "Assets/Scenes/The cafe.unity";
    private const string SystemName = "Cafe Customer System";

    [MenuItem("Tools/Cat Cafe/Setup Traveller Customers")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Stop Play Mode before setting up cafe customers.");
            return;
        }

        Sprite[][] directions = LoadCharacterDirections();
        BeverageDefinition[] beverages = LoadBeverages();
        FrogCustomer prefab = CreateCustomerPrefab(directions, beverages);
        ConfigureCafeScene(prefab, beverages);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Traveller customers, eight table seats, beverage orders, order UI, and fridge are ready in The cafe scene.");
    }

    private static Sprite[][] LoadCharacterDirections()
    {
        TextureImporter importer = AssetImporter.GetAtPath(CharacterSheet) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException("Missing traveller sprite sheet: " + CharacterSheet);
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 32f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        Sprite[] sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(CharacterSheet)
            .OfType<Sprite>()
            .OrderByDescending(sprite => sprite.rect.y)
            .ThenBy(sprite => sprite.rect.x)
            .ToArray();
        if (sprites.Length < 72)
        {
            throw new InvalidOperationException("Expected at least 72 frames in the traveller sprite sheet, found " + sprites.Length + ".");
        }

        // Rows are authored clockwise: south, south-east, east, north-east,
        // north, north-west, west, south-west. Column zero is idle.
        Sprite[][] rows = new Sprite[8][];
        for (int row = 0; row < rows.Length; row++)
        {
            rows[row] = sprites.Skip(row * 9).Take(9).ToArray();
        }

        return rows;
    }

    private static BeverageDefinition[] LoadBeverages()
    {
        string[] names =
        {
            "Coffee", "Tea", "Cocoa", "Latte", "Espresso",
            "Iced Coffee", "Milk Tea", "Green Tea", "Berry Tea", "Lemon Tea",
            "Cola", "Orange Soda", "Lemon Soda", "Grape Soda", "Sparkling Water",
            "Orange Juice", "Apple Juice", "Berry Juice", "Lemonade", "Fruit Punch"
        };

        BeverageDefinition[] result = new BeverageDefinition[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            result[i] = LoadBeverage((BeverageType)(i + 1), names[i], i + 1);
        }
        return result;
    }

    private static BeverageDefinition LoadBeverage(BeverageType type, string name, int itemNumber)
    {
        string path = BeverageFolder + "/Item" + itemNumber + ".png";
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException("Missing beverage sprite: " + path);
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 16f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        Sprite icon = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        return new BeverageDefinition { type = type, displayName = name, icon = icon };
    }

    private static FrogCustomer CreateCustomerPrefab(Sprite[][] rows, BeverageDefinition[] beverages)
    {
        GameObject root = new GameObject("Traveller Customer");
        try
        {
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = rows[0][0];
            renderer.sortingOrder = 1000;
            FrogCustomer customer = root.AddComponent<FrogCustomer>();

            Sprite[] Idle(int row) => new[] { rows[row][0] };
            Sprite[] Walk(int row) => rows[row].Skip(1).Take(8).ToArray();
            customer.ConfigureAnimationFrames(
                Idle(0), Idle(4), Idle(6), Idle(2),
                Walk(0), Walk(4), Walk(6), Walk(2),
                Idle(1), Idle(3), Idle(7), Idle(5),
                Walk(1), Walk(3), Walk(7), Walk(5));
            customer.ConfigureBeverages(beverages);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            return prefab.GetComponent<FrogCustomer>();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void ConfigureCafeScene(FrogCustomer prefab, BeverageDefinition[] beverages)
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        GameObject existing = scene.GetRootGameObjects().FirstOrDefault(root => root.name == SystemName);
        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(existing);
        }

        GameObject system = new GameObject(SystemName);
        SceneManager.MoveGameObjectToScene(system, scene);
        CafeCustomerDirector director = system.AddComponent<CafeCustomerDirector>();

        Tilemap insideTables = FindTilemap(scene, "Tables");
        Bounds cafeBounds = insideTables != null ? insideTables.localBounds : new Bounds(Vector3.zero, new Vector3(12f, 8f));
        const float customerPositionYOffset = -0.6f;
        Vector3 entrancePosition = new Vector3(1.74f, -1.75f + customerPositionYOffset, 0f);
        Transform entrance = CreateMarker(system.transform, "Entrance", entrancePosition);
        Transform exit = CreateMarker(system.transform, "Exit", entrancePosition + Vector3.left * 0.65f);

        Vector3[] chairPositions =
        {
            new Vector3(-0.08f, 2.17f + customerPositionYOffset, 0f),
            new Vector3(3.43f, 0.36f + customerPositionYOffset, 0f),
            new Vector3(6.01f, 2.65f + customerPositionYOffset, 0f),
            new Vector3(7.41f, -1.61f + customerPositionYOffset, 0f),
            new Vector3(11.03f, 0.08f + customerPositionYOffset, 0f)
        };
        CafeDestination[] seats = new CafeDestination[chairPositions.Length];
        for (int i = 0; i < seats.Length; i++)
        {
            seats[i] = CreateDestination(system.transform, "Chair " + (i + 1), chairPositions[i], CafeDestination.DestinationKind.Seat, Vector2.up);
        }

        CafeDestination toilet = CreateDestination(
            system.transform,
            "Toilet",
            new Vector3(cafeBounds.max.x - 0.5f, cafeBounds.max.y - 0.5f, 0f),
            CafeDestination.DestinationKind.Toilet,
            Vector2.down);
        director.Configure(prefab, entrance, exit, seats, toilet);

        CreateFridge(scene, beverages);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = system;
    }

    private static void CreateFridge(Scene scene, BeverageDefinition[] beverages)
    {
        GameObject oldFridge = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Beverage Fridge");
        if (oldFridge != null)
        {
            UnityEngine.Object.DestroyImmediate(oldFridge);
        }

        GameObject fridge = new GameObject("Beverage Fridge");
        SceneManager.MoveGameObjectToScene(fridge, scene);
        Tile tallCupboard = AssetDatabase.LoadAssetAtPath<Tile>("Assets/TallCupboard.asset");
        SpriteRenderer renderer = fridge.AddComponent<SpriteRenderer>();
        renderer.sprite = tallCupboard != null ? tallCupboard.sprite : beverages[0].icon;
        renderer.color = new Color(0.65f, 0.88f, 1f);
        renderer.sortingOrder = 20;
        BeverageFridge component = fridge.AddComponent<BeverageFridge>();
        component.Configure(beverages);

        Tilemap kitchen = FindTilemap(scene, "Kitchen");
        Bounds bounds = kitchen != null ? kitchen.localBounds : new Bounds(new Vector3(4f, 2f), new Vector3(4f, 4f));
        Vector3 position = kitchen != null ? kitchen.transform.TransformPoint(bounds.center) : bounds.center;
        fridge.transform.position = position + new Vector3(bounds.extents.x - 0.75f, 0f, 0f);
    }

    private static Tilemap FindTilemap(Scene scene, string objectName)
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
            .FirstOrDefault(tilemap => string.Equals(tilemap.name, objectName, StringComparison.OrdinalIgnoreCase));
    }

    private static Transform CreateMarker(Transform parent, string name, Vector3 position)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(parent);
        marker.transform.position = position;
        return marker.transform;
    }

    private static CafeDestination CreateDestination(
        Transform parent, string name, Vector3 position, CafeDestination.DestinationKind kind, Vector2 facing)
    {
        Transform marker = CreateMarker(parent, name, position);
        CafeDestination destination = marker.gameObject.AddComponent<CafeDestination>();
        destination.Configure(kind, facing);
        return destination;
    }
}

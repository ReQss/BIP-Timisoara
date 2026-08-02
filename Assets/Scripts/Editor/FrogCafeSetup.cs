using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class FrogCafeSetup
{
    private const string FrogFolder = "Assets/Assets/Sprites/Casual Frog Traveler";
    private const string PrefabPath = "Assets/Prefabs/Frog Customer.prefab";
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string SystemName = "Cafe Customer System";

    static FrogCafeSetup()
    {
        EditorApplication.delayCall += SetupIfNeeded;
    }

    [MenuItem("Tools/Cat Cafe/Setup Frog Customers")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Stop Play Mode before setting up frog customers.");
            return;
        }

        Sprite[][] frames = LoadAndConfigureFrames();
        FrogCustomer prefab = CreateCustomerPrefab(frames);
        AddSystemToScene(prefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Frog cafe customers are ready. Move the entrance, seat, and toilet markers under 'Cafe Customer System' to match the furniture.");
    }

    private static void SetupIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || AssetDatabase.IsAssetImportWorkerProcess())
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null &&
            AssetDatabase.IsValidFolder(FrogFolder))
        {
            Setup();
        }
    }

    private static Sprite[][] LoadAndConfigureFrames()
    {
        string[] folders =
        {
            "Idle Sprites/Front Idle", "Idle Sprites/Back Idle", "Idle Sprites/Left Idle", "Idle Sprites/Right Idle",
            "Run Sprites/Front Sprites", "Run Sprites/Back Sprites", "Run Sprites/Left Sprites", "Run Sprites/Right Sprites"
        };

        Sprite[][] result = new Sprite[folders.Length][];
        for (int i = 0; i < folders.Length; i++)
        {
            string path = FrogFolder + "/" + folders[i];
            string[] texturePaths = AssetDatabase.FindAssets("t:Texture2D", new[] { path })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(assetPath => assetPath, StringComparer.Ordinal)
                .ToArray();

            if (texturePaths.Length == 0)
            {
                throw new InvalidOperationException("No frog frames found in " + path);
            }

            result[i] = texturePaths.Select(LoadFrame).ToArray();
        }

        return result;
    }

    private static Sprite LoadFrame(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException("Could not import frog frame " + path);
        }

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        bool pivotChanged = settings.spriteAlignment != (int)SpriteAlignment.Custom ||
                            settings.spritePivot != new Vector2(0.5f, 0f);
        bool changed = importer.spritePixelsPerUnit != 64f || importer.filterMode != FilterMode.Point ||
                       importer.textureCompression != TextureImporterCompression.Uncompressed || importer.mipmapEnabled ||
                       importer.spriteImportMode != SpriteImportMode.Single || pivotChanged;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 64f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(0.5f, 0f);
        importer.SetTextureSettings(settings);
        if (changed)
        {
            importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        if (sprite == null)
        {
            throw new InvalidOperationException("Could not load frog sprite from " + path);
        }

        return sprite;
    }

    private static FrogCustomer CreateCustomerPrefab(Sprite[][] frames)
    {
        GameObject root = new GameObject("Frog Customer");
        try
        {
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0][0];
            renderer.sortingOrder = 1000;

            FrogCustomer customer = root.AddComponent<FrogCustomer>();
            customer.ConfigureAnimationFrames(
                frames[0], frames[1], frames[2], frames[3],
                frames[4], frames[5], frames[6], frames[7]);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            return prefab.GetComponent<FrogCustomer>();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void AddSystemToScene(FrogCustomer prefab)
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        GameObject existing = scene.GetRootGameObjects().FirstOrDefault(root => root.name == SystemName);
        if (existing != null)
        {
            CafeCustomerDirector currentDirector = existing.GetComponent<CafeCustomerDirector>();
            if (currentDirector != null)
            {
                EditorUtility.SetDirty(currentDirector);
            }
            return;
        }

        GameObject system = new GameObject(SystemName);
        SceneManager.MoveGameObjectToScene(system, scene);
        CafeCustomerDirector director = system.AddComponent<CafeCustomerDirector>();

        Transform entrance = CreateMarker(system.transform, "Entrance", new Vector3(-5.2f, -2.4f, 0f));
        Transform exit = CreateMarker(system.transform, "Exit", new Vector3(-5.8f, -2.4f, 0f));
        CafeDestination[] seats =
        {
            CreateDestination(system.transform, "Seat 1", new Vector3(-2.8f, -0.4f, 0f), CafeDestination.DestinationKind.Seat),
            CreateDestination(system.transform, "Seat 2", new Vector3(0.2f, -0.4f, 0f), CafeDestination.DestinationKind.Seat),
            CreateDestination(system.transform, "Seat 3", new Vector3(2.4f, 1.2f, 0f), CafeDestination.DestinationKind.Seat)
        };
        CafeDestination toilet = CreateDestination(
            system.transform, "Toilet", new Vector3(3.8f, 2.8f, 0f), CafeDestination.DestinationKind.Toilet);

        director.Configure(prefab, entrance, exit, seats, toilet);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = system;
    }

    private static Transform CreateMarker(Transform parent, string name, Vector3 position)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(parent);
        marker.transform.position = position;
        return marker.transform;
    }

    private static CafeDestination CreateDestination(
        Transform parent, string name, Vector3 position, CafeDestination.DestinationKind kind)
    {
        Transform marker = CreateMarker(parent, name, position);
        CafeDestination destination = marker.gameObject.AddComponent<CafeDestination>();
        destination.Configure(kind);
        return destination;
    }
}

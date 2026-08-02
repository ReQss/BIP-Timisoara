using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Imports the Small Kitchen pack as pixel-perfect scene sprites. The source pack is an
/// irregular isometric atlas, not a repeating tileset, so its props are sliced by their
/// authored bounds instead of being forced onto a Tilemap grid.
/// </summary>
public sealed class SmallKitchenImporter : AssetPostprocessor
{
    private const string Folder = "Assets/Assets/Sprites/smallkitchen/";
    private const string RoomPath = Folder + "wallsnfloor.png";
    private const string PropsPath = Folder + "assets.png";
    private const string PaletteRoot = "Assets/Tile Palettes";
    private const string PaletteFolder = PaletteRoot + "/Small Kitchen";
    private const string TileFolder = PaletteFolder + "/Tiles";
    private const string PaletteName = "Small Kitchen Palette";
    private const string PalettePath = PaletteFolder + "/" + PaletteName + ".prefab";
    private const int PropsAtlasHeight = 205;
    private const float PixelsPerUnit = 32f;

    private readonly struct PropSlice
    {
        public PropSlice(string name, int x, int top, int width, int height)
        {
            Name = name;
            X = x;
            Top = top;
            Width = width;
            Height = height;
        }

        public string Name { get; }
        public int X { get; }
        public int Top { get; }
        public int Width { get; }
        public int Height { get; }
    }

    private static readonly PropSlice[] PropSlices =
    {
        new("TallCupboard", 11, 14, 32, 71),
        new("ProduceBasket", 51, 11, 19, 20),
        new("JarGreen", 74, 14, 9, 14),
        new("JarBrown", 85, 14, 9, 14),
        new("JarWhite", 96, 14, 9, 14),
        new("JarRed", 108, 14, 9, 14),
        new("BlueFlower", 144, 30, 14, 24),
        new("BowlTan", 52, 36, 9, 8),
        new("BowlPink", 63, 36, 9, 8),
        new("BowlGray", 74, 37, 8, 7),
        new("BowlCream", 84, 37, 6, 7),
        new("BowlBlue", 92, 38, 6, 6),
        new("ServingDishBlue", 102, 41, 15, 13),
        new("CupBlue", 131, 43, 9, 12),
        new("PottedPlant", 162, 40, 17, 14),
        new("PotGreen", 52, 47, 9, 14),
        new("PotBlue", 63, 47, 9, 14),
        new("PotBrown", 74, 47, 9, 14),
        new("TeapotBlue", 88, 47, 10, 14),
        new("Cutlery", 86, 65, 12, 19),
        new("CanisterBlue", 114, 58, 16, 26),
        new("Placemat", 134, 57, 35, 18),
        new("FoodBasket", 50, 68, 29, 20),
        new("Cookware", 100, 69, 11, 14),
        new("Sacks", 50, 83, 29, 26),
        new("Board", 169, 76, 14, 16),
        new("SmallDish", 158, 87, 9, 9),
        new("ChairLeft", 110, 89, 25, 38),
        new("ChairRight", 136, 85, 26, 42),
        new("Utensil", 171, 94, 7, 8),
        new("StoneOven", 11, 90, 36, 89),
        new("DiningTable", 50, 96, 57, 52),
        new("WallCabinet", 159, 128, 31, 34),
        new("KitchenCounter", 81, 133, 104, 63)
    };

    private void OnPreprocessTexture()
    {
        if (assetPath != RoomPath && assetPath != PropsPath)
        {
            return;
        }

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;

        if (assetPath == RoomPath)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(0.5f, 0f);
            importer.SetTextureSettings(settings);
            return;
        }

        importer.spriteImportMode = SpriteImportMode.Multiple;
        ConfigurePropSlices(importer);
    }

    [MenuItem("Tools/Cat Cafe/Reimport Small Kitchen Sprites")]
    private static void ReimportSprites()
    {
        AssetDatabase.ImportAsset(RoomPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(PropsPath, ImportAssetOptions.ForceUpdate);
        Debug.Log("Small Kitchen is ready: drag the room sprite or any sliced prop into a scene.");
    }

    [MenuItem("GameObject/Cat Cafe/Small Kitchen Room", false, 10)]
    private static void CreateRoomInScene(MenuCommand menuCommand)
    {
        Sprite roomSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoomPath);
        if (roomSprite == null)
        {
            AssetDatabase.ImportAsset(RoomPath, ImportAssetOptions.ForceUpdate);
            roomSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoomPath);
        }

        GameObject room = new GameObject("Small Kitchen Room");
        GameObjectUtility.SetParentAndAlign(room, menuCommand.context as GameObject);
        SpriteRenderer renderer = room.AddComponent<SpriteRenderer>();
        renderer.sprite = roomSprite;
        renderer.sortingOrder = -100;

        Undo.RegisterCreatedObjectUndo(room, "Create Small Kitchen Room");
        Selection.activeGameObject = room;
    }

    [MenuItem("Tools/Cat Cafe/Create or Update Small Kitchen Tile Palette")]
    private static void CreateOrUpdateTilePaletteFromMenu()
    {
        CreateOrUpdateTilePalette(true);
    }

    [InitializeOnLoadMethod]
    private static void ImportNewPackAutomatically()
    {
        EditorApplication.delayCall += () =>
        {
            TextureImporter room = AssetImporter.GetAtPath(RoomPath) as TextureImporter;
            TextureImporter props = AssetImporter.GetAtPath(PropsPath) as TextureImporter;

            if (room != null &&
                (room.textureType != TextureImporterType.Sprite || room.spritePixelsPerUnit != PixelsPerUnit))
            {
                AssetDatabase.ImportAsset(RoomPath, ImportAssetOptions.ForceUpdate);
            }

            int importedPropCount = AssetDatabase.LoadAllAssetRepresentationsAtPath(PropsPath)
                .OfType<Sprite>()
                .Count();
            if (props != null &&
                (props.spriteImportMode != SpriteImportMode.Multiple || importedPropCount != PropSlices.Length))
            {
                AssetDatabase.ImportAsset(PropsPath, ImportAssetOptions.ForceUpdate);
            }

            CreateOrUpdateTilePalette(false);
        };
    }

    private static void CreateOrUpdateTilePalette(bool forceUpdate)
    {
        if (!forceUpdate && AssetDatabase.LoadAssetAtPath<GameObject>(PalettePath) != null)
        {
            return;
        }

        Sprite[] sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(PropsPath)
            .OfType<Sprite>()
            .OrderBy(sprite => System.Array.FindIndex(
                PropSlices,
                slice => slice.Name == sprite.name))
            .ToArray();

        if (sprites.Length != PropSlices.Length)
        {
            if (forceUpdate)
            {
                Debug.LogError(
                    $"Cannot build {PaletteName}: expected {PropSlices.Length} kitchen props, " +
                    $"but found {sprites.Length}. Reimport the kitchen sprites first.");
            }

            return;
        }

        EnsureAssetFolder("Assets", "Tile Palettes");
        EnsureAssetFolder(PaletteRoot, "Small Kitchen");
        EnsureAssetFolder(PaletteFolder, "Tiles");

        TileBase[] tiles = new TileBase[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];
            string tilePath = TileFolder + "/" + sprite.name + ".asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.name = sprite.name;
                AssetDatabase.CreateAsset(tile, tilePath);
            }

            tile.sprite = sprite;
            tile.color = Color.white;
            tile.transform = Matrix4x4.identity;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            tiles[i] = tile;
        }

        GameObject palette = AssetDatabase.LoadAssetAtPath<GameObject>(PalettePath);
        if (palette == null)
        {
            palette = GridPaletteUtility.CreateNewPalette(
                PaletteFolder,
                PaletteName,
                GridLayout.CellLayout.Isometric,
                GridPalette.CellSizing.Manual,
                new Vector3(1f, 0.5f, 1f),
                GridLayout.CellSwizzle.XYZ,
                TransparencySortMode.CustomAxis,
                new Vector3(0f, 1f, 0f));
        }

        GameObject paletteContents = PrefabUtility.LoadPrefabContents(PalettePath);
        try
        {
            Tilemap tilemap = paletteContents.GetComponentInChildren<Tilemap>();
            tilemap.ClearAllTiles();

            const int columns = 6;
            for (int i = 0; i < tiles.Length; i++)
            {
                tilemap.SetTile(new Vector3Int(i % columns, -(i / columns), 0), tiles[i]);
            }

            tilemap.CompressBounds();
            PrefabUtility.SaveAsPrefabAsset(paletteContents, PalettePath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(paletteContents);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Created {PaletteName} with {tiles.Length} kitchen props at {PalettePath}.");
    }

    private static void EnsureAssetFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    private static void ConfigurePropSlices(TextureImporter importer)
    {
        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();

        Dictionary<string, GUID> existingIds = dataProvider.GetSpriteRects()
            .GroupBy(sprite => sprite.name)
            .ToDictionary(group => group.Key, group => group.First().spriteID);

        SpriteRect[] spriteRects = new SpriteRect[PropSlices.Length];
        for (int i = 0; i < PropSlices.Length; i++)
        {
            PropSlice slice = PropSlices[i];
            spriteRects[i] = new SpriteRect
            {
                name = slice.Name,
                rect = new Rect(
                    slice.X,
                    PropsAtlasHeight - slice.Top - slice.Height,
                    slice.Width,
                    slice.Height),
                alignment = SpriteAlignment.Custom,
                pivot = new Vector2(0.5f, 0f),
                border = Vector4.zero,
                spriteID = existingIds.TryGetValue(slice.Name, out GUID existingId)
                    ? existingId
                    : GUID.Generate()
            };
        }

        dataProvider.SetSpriteRects(spriteRects);

        ISpriteNameFileIdDataProvider nameProvider =
            dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        nameProvider?.SetNameFileIdPairs(
            spriteRects.Select(sprite => new SpriteNameFileIdPair(sprite.name, sprite.spriteID)));

        dataProvider.Apply();
    }
}

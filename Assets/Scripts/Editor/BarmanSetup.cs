using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BarmanSetup
{
    private const string SpriteSheetPath = "Assets/Assets/Sprites/Barman/people.png";
    private const string AnimationFolder = "Assets/Animations/Barman";
    private const string ControllerPath = AnimationFolder + "/Barman.controller";
    private const string PrefabPath = "Assets/Prefabs/Barman.prefab";
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/Cat Cafe/Setup Barman")]
    public static void Setup()
    {
        ConfigureSpriteSheet();
        EnsureFolder("Assets", "Animations");
        AssetDatabase.DeleteAsset(AnimationFolder);
        EnsureFolder("Assets/Animations", "Barman");
        EnsureFolder("Assets", "Prefabs");

        Dictionary<string, AnimationClip> clips = CreateAnimationClips();
        AnimatorController controller = CreateAnimatorController(clips);
        GameObject prefab = CreatePrefab(controller);
        AddBarmanAlongsideCats(prefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Barman setup complete. The barman uses people_0 through people_39 and is controlled by a gamepad or IJKL.");
    }

    private static void ConfigureSpriteSheet()
    {
        TextureImporter importer = AssetImporter.GetAtPath(SpriteSheetPath) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException("Could not find the barman sprite sheet at " + SpriteSheetPath);
        }

        bool changed = importer.spritePixelsPerUnit != 32f
            || importer.filterMode != FilterMode.Point
            || importer.textureCompression != TextureImporterCompression.Uncompressed
            || importer.mipmapEnabled;

        importer.spritePixelsPerUnit = 32f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;

        if (changed)
        {
            importer.SaveAndReimport();
        }
    }

    private static Dictionary<string, AnimationClip> CreateAnimationClips()
    {
        Sprite[] sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(SpriteSheetPath)
            .OfType<Sprite>()
            .Where(sprite => TryGetFrameIndex(sprite.name, out int index) && index >= 0 && index <= 39)
            .OrderBy(sprite => GetFrameIndex(sprite.name))
            .ToArray();

        if (sprites.Length != 40)
        {
            throw new InvalidOperationException(
                "Expected people_0 through people_39, but found " + sprites.Length + " matching sprites.");
        }

        Sprite[] southWest = sprites.Skip(0).Take(9).ToArray();
        Sprite[] southEast = sprites.Skip(10).Take(9).ToArray();
        Sprite[] northWest = sprites.Skip(20).Take(9).ToArray();
        Sprite[] northEast = sprites.Skip(30).Take(9).ToArray();

        return new Dictionary<string, AnimationClip>
        {
            ["IdleSouthWest"] = CreateClip("IdleSouthWest", new[] { sprites[0] }, 1f, false),
            ["IdleSouthEast"] = CreateClip("IdleSouthEast", new[] { sprites[10] }, 1f, false),
            ["IdleNorthWest"] = CreateClip("IdleNorthWest", new[] { sprites[20] }, 1f, false),
            ["IdleNorthEast"] = CreateClip("IdleNorthEast", new[] { sprites[30] }, 1f, false),
            ["WalkSouthWest"] = CreateClip("WalkSouthWest", southWest, 10f, true),
            ["WalkSouthEast"] = CreateClip("WalkSouthEast", southEast, 10f, true),
            ["WalkNorthWest"] = CreateClip("WalkNorthWest", northWest, 10f, true),
            ["WalkNorthEast"] = CreateClip("WalkNorthEast", northEast, 10f, true)
        };
    }

    private static bool TryGetFrameIndex(string spriteName, out int index)
    {
        const string prefix = "people_";
        index = -1;
        return spriteName.StartsWith(prefix, StringComparison.Ordinal)
            && int.TryParse(spriteName.Substring(prefix.Length), out index);
    }

    private static int GetFrameIndex(string spriteName)
    {
        TryGetFrameIndex(spriteName, out int index);
        return index;
    }

    private static AnimationClip CreateClip(string name, IReadOnlyList<Sprite> sprites, float frameRate, bool loop)
    {
        string path = AnimationFolder + "/" + name + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { name = name };
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.frameRate = frameRate;
        ObjectReferenceKeyframe[] frames = new ObjectReferenceKeyframe[sprites.Count];
        for (int i = 0; i < sprites.Count; i++)
        {
            frames[i] = new ObjectReferenceKeyframe { time = i / frameRate, value = sprites[i] };
        }

        AnimationUtility.SetObjectReferenceCurve(
            clip,
            new EditorCurveBinding
            {
                path = string.Empty,
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            },
            frames);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController CreateAnimatorController(IReadOnlyDictionary<string, AnimationClip> clips)
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idle = stateMachine.AddState("Idle");
        idle.motion = CreateDirectionalBlendTree(controller, "Idle Direction", clips, "Idle");
        AnimatorState walk = stateMachine.AddState("Walk");
        walk.motion = CreateDirectionalBlendTree(controller, "Walk Direction", clips, "Walk");
        stateMachine.defaultState = idle;

        AnimatorStateTransition startWalking = idle.AddTransition(walk);
        startWalking.hasExitTime = false;
        startWalking.duration = 0.05f;
        startWalking.AddCondition(AnimatorConditionMode.Greater, 0.01f, "Speed");

        AnimatorStateTransition stopWalking = walk.AddTransition(idle);
        stopWalking.hasExitTime = false;
        stopWalking.duration = 0.05f;
        stopWalking.AddCondition(AnimatorConditionMode.Less, 0.01f, "Speed");
        return controller;
    }

    private static BlendTree CreateDirectionalBlendTree(
        AnimatorController controller,
        string name,
        IReadOnlyDictionary<string, AnimationClip> clips,
        string prefix)
    {
        BlendTree tree = new BlendTree
        {
            name = name,
            blendType = BlendTreeType.SimpleDirectional2D,
            blendParameter = "MoveX",
            blendParameterY = "MoveY",
            useAutomaticThresholds = false
        };
        AssetDatabase.AddObjectToAsset(tree, controller);
        tree.AddChild(clips[prefix + "SouthWest"], new Vector2(-1f, -1f));
        tree.AddChild(clips[prefix + "SouthEast"], new Vector2(1f, -1f));
        tree.AddChild(clips[prefix + "NorthWest"], new Vector2(-1f, 1f));
        tree.AddChild(clips[prefix + "NorthEast"], new Vector2(1f, 1f));
        return tree;
    }

    private static GameObject CreatePrefab(RuntimeAnimatorController controller)
    {
        GameObject root = new GameObject("Barman");
        try
        {
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 10;
            renderer.sprite = AssetDatabase.LoadAllAssetRepresentationsAtPath(SpriteSheetPath)
                .OfType<Sprite>()
                .First(sprite => sprite.name == "people_10");

            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.55f, 0.35f);
            collider.offset = new Vector2(0f, -0.45f);

            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;

            Animator animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            root.AddComponent<BarmanController>();

            return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void AddBarmanAlongsideCats(GameObject prefab)
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        GameObject[] roots = scene.GetRootGameObjects();
        GameObject existingBarman = roots.FirstOrDefault(root => root.name == "Barman");
        GameObject[] cats = roots.Where(root => root.name.StartsWith("Cat", StringComparison.Ordinal)).ToArray();

        if (existingBarman == null)
        {
            existingBarman = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            existingBarman.name = "Barman";
            existingBarman.transform.position = cats.Length > 0
                ? cats[0].transform.position + new Vector3(1.25f, 0f, 0f)
                : Vector3.zero;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static partial class EditorGenerator
{
    private const string PrefabRoot = "Assets/_Prefabs";
    private const string ArtRoot = PrefabRoot + "/_GeneratedArt";
    private const string WalkSpriteDir = "Assets/walk";

    [MenuItem("Tools/Packet Courier/Generate Prefabs")]
    public static void GeneratePrefabs()
    {
        EnsureFolder(PrefabRoot);
        EnsureFolder(ArtRoot);
        EnsureWalkSpriteImportSettings();

        CreatePlayerAnimatorController();
        CreatePlayerPrefab();
        CreateGroundPrefab();
        CreatePlatformOneWayPrefab();
        CreateSpikePrefab();
        CreateEnemyPrefab();
        CreateLaserPrefab();
        CreateMovingPlatformPrefab();
        CreateFirewallGatePrefab();
        CreateCheckpointPrefab();
        CreateDataBitPrefab();
        CreateSnippetPrefab();
        CreateServerBGoalPrefab();
        CreateFanVortexPrefab();

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog(
            "Packet Courier",
            "Semua prefab selesai dibuat di Assets/_Prefabs/.\nBuka Tools > Packet Courier > Setup Scene untuk menyiapkan scene kosong.",
            "OK"
        );
    }

    private static void EnsureFolder(string assetPath)
    {
        string[] parts = assetPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static void EnsureWalkSpriteImportSettings()
    {
        if (!Directory.Exists(WalkSpriteDir)) return;

        foreach (string path in Directory.GetFiles(WalkSpriteDir, "*.png"))
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = 32f;
            importer.SaveAndReimport();
        }
    }

    private static Sprite LoadWalkSprite(int index)
    {
        string name = index == 0 ? "walk" : $"walk{index}";
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{WalkSpriteDir}/{name}.png");
    }

    private static Sprite LoadOrCreateSprite(string name, int w, int h, Color color, float ppu, bool autoTiling = false)
    {
        string path = $"{ArtRoot}/{name}.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null)
        {
            if (!autoTiling)
                EnsureFullRectMesh(path);
            return existing;
        }

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                tex.SetPixel(x, y, color);
        tex.filterMode = FilterMode.Point;
        tex.Apply();

        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);

        if (AssetImporter.GetAtPath(path) is TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.spritePixelsPerUnit = ppu;
            if (!autoTiling)
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteGenerateFallbackPhysicsShape = true;
                importer.SetTextureSettings(settings);
            }
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void EnsureFullRectMesh(string path)
    {
        if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        if (settings.spriteMeshType == SpriteMeshType.FullRect) return;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    private static void CreatePlayerAnimatorController()
    {
        string path = PrefabRoot + "/PlayerAnimator.controller";
        var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(path);
        if (controller == null)
            controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(path);

        UnityEditor.Animations.AnimatorControllerLayer layer = controller.layers[0];
        UnityEditor.Animations.AnimatorStateMachine sm = layer.stateMachine;

        if (!HasParameter(controller, "isMoving"))
            controller.AddParameter("isMoving", AnimatorControllerParameterType.Bool);
        if (!HasParameter(controller, "speedMultiplier"))
            controller.AddParameter("speedMultiplier", AnimatorControllerParameterType.Float);
        if (!HasParameter(controller, "facing"))
            controller.AddParameter("facing", AnimatorControllerParameterType.Float);

        foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (sub is AnimationClip clip && (clip.name == "PlayerIdle" || clip.name == "PlayerRun"))
                UnityEngine.Object.DestroyImmediate(sub, true);
        }

        AnimationClip idleClip = new AnimationClip { frameRate = 10f, name = "PlayerIdle" };
        SetSpriteCurve(idleClip, new Sprite[] { LoadWalkSprite(0) ?? LoadOrCreateSprite("player_placeholder", 16, 16, Color.cyan, 32f) });
        SetLoop(idleClip);
        AssetDatabase.AddObjectToAsset(idleClip, controller);

        Sprite[] walkSprites = new Sprite[5];
        for (int i = 0; i < 5; i++)
            walkSprites[i] = LoadWalkSprite(i) ?? LoadOrCreateSprite("player_placeholder", 16, 16, Color.cyan, 32f);
        AnimationClip runClip = new AnimationClip { frameRate = 10f, name = "PlayerRun" };
        SetSpriteCurve(runClip, walkSprites);
        SetLoop(runClip);
        AssetDatabase.AddObjectToAsset(runClip, controller);

        var idle = FindOrAddState(sm, "Idle");
        idle.motion = idleClip;
        var run = FindOrAddState(sm, "Run");
        run.motion = runClip;

        if (idle.transitions.Length == 0)
        {
            var idleToRun = idle.AddTransition(run);
            idleToRun.hasExitTime = false;
            idleToRun.duration = 0f;
            idleToRun.AddCondition(AnimatorConditionMode.If, 0f, "isMoving");
        }

        if (run.transitions.Length == 0)
        {
            var runToIdle = run.AddTransition(idle);
            runToIdle.hasExitTime = false;
            runToIdle.duration = 0f;
            runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "isMoving");
        }

        AssetDatabase.SaveAssets();
    }

    private static bool HasParameter(UnityEditor.Animations.AnimatorController controller, string name)
    {
        for (int i = 0; i < controller.parameters.Length; i++)
            if (controller.parameters[i].name == name)
                return true;
        return false;
    }

    private static UnityEditor.Animations.AnimatorState FindOrAddState(UnityEditor.Animations.AnimatorStateMachine sm, string name)
    {
        foreach (var child in sm.states)
            if (child.state.name == name)
                return child.state;
        return sm.AddState(name);
    }

    private static void SetSpriteCurve(AnimationClip clip, params Sprite[] frames)
    {
        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe { time = i / clip.frameRate, value = frames[i] };
        }
        keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / clip.frameRate, value = frames[0] };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
    }

    private static void SetLoop(AnimationClip clip)
    {
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
    }

    private static void CreatePlayerPrefab()
    {
        string path = PrefabRoot + "/Player.prefab";
        GameObject go = new GameObject("Player");
        go.tag = "Player";
        go.layer = 0;

        go.transform.localScale = new Vector3(1f, 1f, 1f);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.5f, 1f);

        go.AddComponent<PlayerHealth>();
        go.AddComponent<PlayerMode>();
        go.AddComponent<PlayerController>();
        go.AddComponent<PlayerAnimator>();

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadWalkSprite(0) ?? LoadOrCreateSprite("player_placeholder", 16, 16, Color.cyan, 32f);
        sr.sortingOrder = 10;

        var anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(
            PrefabRoot + "/PlayerAnimator.controller");

        SavePrefab(go, path);
    }

    private static void CreateGroundPrefab()
    {
        string path = PrefabRoot + "/Ground.prefab";
        GameObject go = new GameObject("Ground");
        go.layer = 0;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);
        col.autoTiling = true;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadOrCreateSprite("ground", 1, 1, new Color(0.35f, 0.35f, 0.55f), 16f, autoTiling: true);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(1f, 1f);
        sr.sortingOrder = 0;

        SavePrefab(go, path);
    }

    private static void CreatePlatformOneWayPrefab()
    {
        string path = PrefabRoot + "/Platform_OneWay.prefab";
        GameObject go = new GameObject("Platform_OneWay");
        go.layer = 0;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 0.5f);
        col.autoTiling = true;
        col.usedByEffector = true;

        var eff = go.AddComponent<PlatformEffector2D>();
        eff.useOneWay = true;
        eff.surfaceArc = 160f;
        eff.useColliderMask = false;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadOrCreateSprite("platform_oneway", 1, 1, new Color(0.3f, 0.6f, 1f), 16f, autoTiling: true);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(1f, 0.5f);
        sr.sortingOrder = 5;

        SavePrefab(go, path);
    }

    private static void CreateSpikePrefab()
    {
        string path = PrefabRoot + "/Spike.prefab";
        GameObject go = new GameObject("Spike");
        go.layer = 0;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.7f, 0.7f);
        col.isTrigger = true;

        go.AddComponent<Hazard_Spike>();

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadOrCreateSprite("spike", 7, 7, new Color(0.9f, 0.25f, 0.25f), 32f);
        sr.sortingOrder = 4;

        SavePrefab(go, path);
    }

    private static void CreateEnemyPrefab()
    {
        string path = PrefabRoot + "/Enemy_Patrol.prefab";
        GameObject go = new GameObject("Enemy_Patrol");
        go.layer = 0;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        go.AddComponent<Enemy_Patrol>();

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadOrCreateSprite("enemy", 8, 8, Color.red, 32f);
        sr.sortingOrder = 6;

        SavePrefab(go, path);
    }

    private static void CreateLaserPrefab()
    {
        string path = PrefabRoot + "/Laser_Beam.prefab";
        GameObject go = new GameObject("Laser_Beam");
        go.layer = 0;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.3f, 3f);
        col.isTrigger = true;

        go.AddComponent<Laser_Beam>();

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadOrCreateSprite("laser", 3, 30, new Color(1f, 0.2f, 0.2f), 32f);
        sr.sortingOrder = 4;

        SavePrefab(go, path);
    }

    private static void CreateMovingPlatformPrefab()
    {
        string path = PrefabRoot + "/Platform_Moving.prefab";
        GameObject go = new GameObject("Platform_Moving");
        go.layer = 0;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2f, 0.5f);
        col.autoTiling = true;

        go.AddComponent<Platform_Moving>();

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadOrCreateSprite("platform_moving", 2, 1, new Color(0.2f, 0.9f, 0.3f), 32f, autoTiling: true);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(2f, 0.5f);
        sr.sortingOrder = 5;

        SavePrefab(go, path);
    }

    private static void SavePrefab(GameObject go, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }
}
#endif

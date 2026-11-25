#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class EditorGenerator
{
    private const string LevelsRoot = "Assets/_Levels";
    private const string ScenesRoot = "Assets/_Scenes";

    private static readonly Dictionary<char, string> TileToPrefab = new Dictionary<char, string>
    {
        { '#', PrefabRoot + "/Ground.prefab" },
        { '-', PrefabRoot + "/Platform_OneWay.prefab" },
        { '=', PrefabRoot + "/Platform_Moving.prefab" },
        { 'S', PrefabRoot + "/Spike.prefab" },
        { 'E', PrefabRoot + "/Enemy_Patrol.prefab" },
        { 'L', PrefabRoot + "/Laser_Beam.prefab" },
        { 'P', PrefabRoot + "/Player.prefab" },
        { 'G', PrefabRoot + "/ServerB_Goal.prefab" },
        { 'B', PrefabRoot + "/DataBit.prefab" },
        { 'N', PrefabRoot + "/Snippet.prefab" },
        { 'C', PrefabRoot + "/Checkpoint_TCP.prefab" },
        { 'V', PrefabRoot + "/Fan_Vortex.prefab" },
    };

    [MenuItem("Tools/Packet Courier/Generate Level Scenes")]
    public static void GenerateLevelScenes()
    {
        if (!Directory.Exists(LevelsRoot) || Directory.GetFiles(LevelsRoot, "*.txt").Length == 0)
        {
            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Packet Courier", "Tidak ada file layout di Assets/_Levels/.", "OK");
            return;
        }

        EnsureFolder(ScenesRoot);
        BuildLevelScenes();
        BuildMetaScenes();
        RegisterBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("Packet Courier", "Level scenes dibuat di Assets/_Scenes/ dan didaftarkan di Build Settings.", "OK");
    }

    private static void BuildLevelScenes()
    {
        EnsureLevelSpriteMeshes();
        EnsureFlyingEnemyPrefab();
        foreach (string file in Directory.GetFiles(LevelsRoot, "World*_Segment*.txt"))
        {
            string sceneName = Path.GetFileNameWithoutExtension(file);
            int segmentIndex = ParseSegmentIndex(sceneName);
            int worldIndex = int.Parse(sceneName.Substring(5, sceneName.IndexOf('_') - 5)) - 1;
            string scenePath = $"{ScenesRoot}/{sceneName}.unity";

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EnsureInputManager();
            EnsureCamera();
            EnsureLevelManager();
            EnsureHUD();
            EnsureEventSystem();

            var contentRoot = new GameObject("LevelContent");
            var lm = Object.FindAnyObjectByType<LevelManager>();
            lm.worldIndex = worldIndex;
            lm.segmentIndex = segmentIndex;

            BuildLevelFromFile(file, contentRoot.transform);
            InstallFlyingEncounters(worldIndex, segmentIndex);
            if (worldIndex == 3) Object.FindAnyObjectByType<PlayerController>().gravity = 42;
            Camera.main.backgroundColor = new Color[] { new Color(0.035f,0.06f,0.07f), new Color(0.09f,0.045f,0.055f), new Color(0.025f,0.08f,0.1f), new Color(0.11f,0.12f,0.16f), new Color(0.12f,0.02f,0.04f) }[Mathf.Clamp(worldIndex, 0, 4)];

            lm.playerSpawn = FindChildByName(contentRoot.transform, "Player");
            lm.serverBPoint = FindChildByName(contentRoot.transform, "ServerB_Goal");

            if (worldIndex == 1 && segmentIndex == 3) // World 2 Segment 4: Mini-Boss The Censor
            {
                var bossGo = new GameObject("Boss_TheCensor");
                bossGo.transform.SetParent(contentRoot.transform);
                bossGo.AddComponent<Boss_TheCensor>();
                bossGo.transform.position = new Vector3(lm.serverBPoint != null ? lm.serverBPoint.position.x - 16f : 330f, -8f, 0f);
            }
            else if (worldIndex == 3 && segmentIndex == 4) // World 4 Segment 5: Mini-Boss Solaris
            {
                var bossGo = new GameObject("Boss_Solaris");
                bossGo.transform.SetParent(contentRoot.transform);
                bossGo.AddComponent<Boss_Solaris>();
                bossGo.transform.position = new Vector3(lm.serverBPoint != null ? lm.serverBPoint.position.x - 18f : 255f, -8f, 0f);
            }
            else if (worldIndex == 4) // World 5: Final Boss (Boss Akhir)
            {
                var bossGo = new GameObject("Boss_Final");
                bossGo.transform.SetParent(contentRoot.transform);
                bossGo.AddComponent<Boss_Final>();
                bossGo.transform.position = new Vector3(lm.serverBPoint != null ? lm.serverBPoint.position.x - 18f : 430f, -8f, 0f);
            }

            if (!EditorSceneManager.SaveScene(scene, scenePath))
                Debug.LogError($"[EditorLevelGenerator] Gagal menyimpan scene: {scenePath}");
        }
    }

    public static void EnsureLevelSpriteMeshes()
    {
        foreach (char tile in new[] { '#', '-', '=' })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TileToPrefab[tile]);
            var renderer = prefab != null ? prefab.GetComponent<SpriteRenderer>() : null;
            if (renderer == null || renderer.sprite == null) continue;

            string path = AssetDatabase.GetAssetPath(renderer.sprite);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (settings.spriteMeshType == SpriteMeshType.FullRect) continue;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }

    private static int ParseSegmentIndex(string sceneName)
    {
        int idx = sceneName.LastIndexOf("Segment");
        if (idx < 0) return 0;
        if (int.TryParse(sceneName.Substring(idx + "Segment".Length), out int n)) return n - 1;
        return 0;
    }

    private static void BuildLevelFromFile(string file, Transform parent)
    {
        string[] lines = File.ReadAllLines(file);

        for (int row = 0; row < lines.Length; row++)
        {
            string line = lines[row];
            int col = 0;
            int i = 0;
            while (i < line.Length)
            {
                char c = line[i];

                if (c == ' ')
                {
                    col++; i++;
                    continue;
                }

                if (c == '#' || c == '-')
                {
                    int length = 1;
                    while (i + length < line.Length && line[i + length] == c) length++;
                    var ground = PlacePrefab(TileToPrefab[c], col, row, parent);
                    ground.transform.position += Vector3.right * (length - 1) * 0.5f;
                    float thickness = c == '-' ? 0.5f : 1;
                    var collider = ground.GetComponent<BoxCollider2D>();
                    collider.autoTiling = false;
                    collider.size = new Vector2(length, thickness);
                    var render = ground.GetComponent<SpriteRenderer>(); render.drawMode = SpriteDrawMode.Tiled; render.tileMode = SpriteTileMode.Continuous; render.size = collider.size;
                    i += length; col += length; continue;
                }
                if ("abcHRTZD".IndexOf(c) >= 0)
                {
                    PlaceSpecial(c, col, row, parent);
                    col++; i++; continue;
                }

                if (c == 'F')
                {
                    int start = i;
                    i++;
                    while (i < line.Length && char.IsDigit(line[i])) i++;
                    string token = line.Substring(start, i - start);
                    if (int.TryParse(token.Substring(1), out int port))
                    {
                        GameObject gate = PlacePrefab(PrefabRoot + "/Firewall_Gate.prefab", col, row, parent);
                        if (gate != null)
                        {
                            var g = gate.GetComponent<Gate_Firewall>();
                            if (g != null) g.requiredPort = port;
                        }
                    }
                    col++;
                    continue;
                }

                if (TileToPrefab.TryGetValue(c, out string prefabPath))
                    PlacePrefab(prefabPath, col, row, parent);

                col++; i++;
            }
        }
    }

    private static GameObject PlacePrefab(string prefabPath, int col, int row, Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[EditorLevelGenerator] Prefab tidak ditemukan: {prefabPath}");
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = prefab.name + "_" + col + "_" + row;
        instance.transform.position = new Vector3(col, -row, 0);
        if (instance.TryGetComponent<Enemy_Patrol>(out var enemy)) { enemy.damageAmount = 15; enemy.damageCooldown = 0.8f; instance.transform.position += Vector3.up * 0.1f; }
        if (instance.TryGetComponent<Hazard_Spike>(out var spike)) { spike.damageType = PlayerHealth.DamageType.TTLDamage; spike.ttlDamage = 15; }
        if (instance.TryGetComponent<Laser_Beam>(out var beam)) { beam.damageAmount = 15; beam.damageCooldown = 0.8f; }
        return instance;
    }

    private static Transform FindChildByName(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name.StartsWith(name + "_"))
                return parent.GetChild(i);
        return null;
    }

    private static void RegisterBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>();
        var order = new List<string> { "Menu" };
        for (int world = 0; world < WorldRules.Count; world++)
            for (int segment = 1; segment <= WorldRules.Segments(world); segment++) order.Add($"World{world + 1}_Segment{segment}");
        order.Add("Hub"); order.Add("GameOver");
        foreach (string name in order)
        {
            string path = $"{ScenesRoot}/{name}.unity";
            if (File.Exists(path))
                scenes.Add(new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void BuildMetaScenes()
    {
        BuildMetaScene("Menu", typeof(MenuUI));
        BuildMetaScene("GameOver", typeof(GameOverUI));
        BuildMetaScene("Hub", typeof(HubUI));
    }

    private static void BuildMetaScene(string sceneName, System.Type uiComponent)
    {
        string scenePath = $"{ScenesRoot}/{sceneName}.unity";

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EnsureMetaCamera();
        EnsureEventSystem();
        var go = new GameObject(sceneName);
        go.AddComponent(uiComponent);

        if (!EditorSceneManager.SaveScene(scene, scenePath))
            Debug.LogError($"[EditorLevelGenerator] Gagal menyimpan scene: {scenePath}");
    }

    [MenuItem("Tools/Packet Courier/Refresh Menu Scenes")]
    public static void RefreshMetaScenes()
    {
        if (EditorApplication.isPlaying) return;
        BuildMetaScenes();
        RegisterBuildSettings();
        AssetDatabase.SaveAssets();
    }

    private static void EnsureMetaCamera()
    {
        if (Camera.main != null) return;
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.025f, 0.04f, 0.08f);
        camera.transform.position = new Vector3(0, 0, -10);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<UnityEngine.EventSystems.EventSystem>();
        go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }
}
#endif

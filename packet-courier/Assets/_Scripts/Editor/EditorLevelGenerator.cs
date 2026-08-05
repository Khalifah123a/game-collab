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
        RegisterBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("Packet Courier", "Level scenes dibuat di Assets/_Scenes/ dan didaftarkan di Build Settings.", "OK");
    }

    private static void BuildLevelScenes()
    {
        foreach (string file in Directory.GetFiles(LevelsRoot, "World1_Segment*.txt"))
        {
            string sceneName = Path.GetFileNameWithoutExtension(file);
            int segmentIndex = ParseSegmentIndex(sceneName);
            string scenePath = $"{ScenesRoot}/{sceneName}.unity";

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EnsureInputManager();
            EnsureCamera();
            EnsureLevelManager();
            EnsureHUD();

            var contentRoot = new GameObject("LevelContent");
            var lm = Object.FindObjectOfType<LevelManager>();
            lm.worldIndex = 0;
            lm.segmentIndex = segmentIndex;

            BuildLevelFromFile(file, contentRoot.transform);

            lm.playerSpawn = FindChildByName(contentRoot.transform, "Player");
            lm.serverBPoint = FindChildByName(contentRoot.transform, "ServerB_Goal");

            if (!EditorSceneManager.SaveScene(scene, scenePath))
                Debug.LogError($"[EditorLevelGenerator] Gagal menyimpan scene: {scenePath}");
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
        instance.name = prefab.name;
        instance.transform.position = new Vector3(col, -row, 0);
        return instance;
    }

    private static Transform FindChildByName(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name == name)
                return parent.GetChild(i);
        return null;
    }

    private static void RegisterBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>();
        string[] order = {
            "Menu", "World1_Segment1", "World1_Segment2",
            "World1_Segment3", "Hub", "GameOver"
        };
        foreach (string name in order)
        {
            string path = $"{ScenesRoot}/{name}.unity";
            if (File.Exists(path))
                scenes.Add(new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif

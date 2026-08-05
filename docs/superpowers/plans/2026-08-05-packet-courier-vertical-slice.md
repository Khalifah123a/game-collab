# Packet Courier — Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make Packet Courier playable end-to-end (`Menu → World1_Segment1..3 → Hub`, with `GameOver` + retry) by adding a text-layout level generator, three tutorial levels, menu/game-over/hub scenes, and build-settings registration.

**Architecture:** Extend the existing `EditorGenerator` partial class (in `Assets/_Scripts/Editor/`) with a new file `EditorLevelGenerator.cs` that parses `Assets/_Levels/*.txt` ASCII grids into prefab instances, composes scenes with the existing `Ensure*` helpers, and registers them in `EditorBuildSettings`. Three small runtime UI scripts (`MenuUI`, `GameOverUI`, `HubUI`) drive the flow via three small additions to `GameManager`.

**Tech Stack:** Unity 2022.3.20f1, C#, UnityEditor batch mode (`-executeMethod`) for verification. No test framework in this project — verification is batch-mode generation + filesystem/build-settings assertions.

## Global Constraints

- Unity editor executable: `C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe`
- Project root: `C:\Users\AXIOO\game-collab\packet-courier`
- Unity log files go to `C:\Users\AXIOO\AppData\Local\Temp\opencode\` (pre-approved temp dir)
- Prefab paths (exact names from `Assets/_Prefabs/`): `Ground`, `Platform_OneWay`, `Platform_Moving`, `Spike`, `Enemy_Patrol`, `Laser_Beam`, `Firewall_Gate`, `Checkpoint_TCP`, `DataBit`, `Snippet`, `ServerB_Goal`, `Player`, `PlayerAnimator.controller`
- Level scene names exactly: `World1_Segment1`, `World1_Segment2`, `World1_Segment3` (GameManager builds these from `LoadSegment`); meta scenes exactly: `Menu`, `Hub`, `GameOver`
- Build settings order must be: `Menu`, `World1_Segment1`, `World1_Segment2`, `World1_Segment3`, `Hub`, `GameOver`
- Do NOT call `EditorUtility.DisplayDialog` in batch mode (can hang) — guard with `if (!Application.isBatchMode)`
- `EditorBuildSettings.asset` currently has `m_Scenes: []` — must end up with the 6 entries above
- All new code must compile against C# without `using UnityEditor;` in runtime scripts (runtime scripts use `UnityEngine` only)

---
### Task 1: GameManager public flow API

**Files:**
- Modify: `packet-courier/Assets/_Scripts/Core/GameManager.cs` (insert methods after `StartRun`, i.e. after line 54)

**Interfaces:**
- Consumes: existing private `LoadOrReload(string)` and `LoadSegment(int)` (already in file)
- Produces: `public static GameManager Ensure()`, `public void RetrySegment()`, `public void ReturnToMenu()` — used by Task 4 UI scripts

- [ ] **Step 1: Add the three methods**

Insert directly after the closing brace of `StartRun` (line 54):

```csharp
    public static GameManager Ensure()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("GameManager");
        return go.AddComponent<GameManager>();
    }

    public void RetrySegment()
    {
        dataBitsCollectedThisRun = 0;
        isInRun = true;
        state = GameState.Run;
        LoadSegment(currentSegmentIndex);
    }

    public void ReturnToMenu()
    {
        isInRun = false;
        state = GameState.Menu;
        LoadOrReload("Menu");
    }
```

- [ ] **Step 2: Compile check via batch mode**

Run:
```powershell
& "C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe" -batchmode -quit -nographics -projectPath "C:\Users\AXIOO\game-collab\packet-courier" -logFile "C:\Users\AXIOO\AppData\Local\Temp\opencode\unity-task1.log"
```
Expected: `Select-String -Path "C:\Users\AXIOO\AppData\Local\Temp\opencode\unity-task1.log" -Pattern "error CS"` returns NOTHING. If it matches, fix the compile error before proceeding.

- [ ] **Step 3: Commit**

```bash
git add packet-courier/Assets/_Scripts/Core/GameManager.cs
git commit -m "feat: GameManager Ensure/RetrySegment/ReturnToMenu for UI flow"
```

---
### Task 2: Level layout text files

**Files:**
- Create: `packet-courier/Assets/_Levels/World1_Segment1.txt`
- Create: `packet-courier/Assets/_Levels/World1_Segment2.txt`
- Create: `packet-courier/Assets/_Levels/World1_Segment3.txt`

**Interfaces:**
- Consumes: nothing (pure data)
- Produces: the `.txt` files Task 3 parses. Format: one row per line, cell `(col, row)` → world `(col, -row)`. Symbols: `#`=Ground, `-`=Platform_OneWay, `=`=Platform_Moving, `S`=Spike, `E`=Enemy_Patrol, `L`=Laser_Beam, `P`=Player (spawn), `G`=ServerB_Goal, `B`=DataBit, `N`=Snippet, `C`=Checkpoint_TCP, `F<port>`=Firewall_Gate (e.g. `F22`, `F443`), ` `=empty. `F<port>` consumes multiple characters but occupies a single cell column.

- [ ] **Step 1: Create `World1_Segment1.txt`** ("Handshake", no hazards, teaches move/jump/dash)

Each line is exactly 46 chars. `#` fills row 10 (ground), `P` at col 1, `C` at col 22, `G` at col 44.

```
                                              
                                              
                                              
                                              
                                              
                B                            B
               ---                           
      B       ---                B            
     ---                 ---    ---    ---   G
  P                     C                    
##############################################
```

Verify each row is 46 chars; the four content rows above are: `B` at col 16 and col 37; platforms at cols 5-7, 10-12, 15-17, 26-28, 36-38; `B` at col 6 and col 27; `P` at col 1, `C` at col 22; `G` at col 44.

- [ ] **Step 2: Create `World1_Segment2.txt`** ("Firewall", introduces spikes + mode toggle)

Each line is exactly 46 chars. `#` fills row 10, `P` at col 1, `C` at col 9 (before the spikes), spikes `SSS` at cols 21-23 on the ground, reward `N` above the far platform, patrol `E` on an elevated platform, `G` at col 43.

```
                                              
                                              
                                              
                                              
                                              
                         B                  
                        ---                  
                         N         E         
                            ---      ---     
 P       C           SSS                     
##############################################
```

Verify: row 5 `B` at col 25; row 6 platform cols 24-26; row 7 `N` at col 29 and `E` at col 36; row 8 platforms cols 28-30 and 35-37; row 9 `P` at col 1, `C` at col 9, `SSS` at cols 21-23; row 10 ground.

- [ ] **Step 3: Create `World1_Segment3.txt`** ("Root Access", firewall gates + laser + patrol)

Each line is exactly 48 chars. `#` fills row 10, `P` at col 1. Gate 1 `F22` at col 8 (must press Q to switch from default port 80). Laser `L` at col 15 (clear it by jumping from the platform at cols 11-13 row 7). Gate 2 `F443` at col 23 (press E twice from 22, or Q twice from 80). `E` patrol at col 31 on platform 30-32, climb platforms at 38-40 and 42-44, `G` at col 46. NOTE: `F22` and `F443` tokens are multi-char but each occupies ONE cell column (the generator consumes the digits).

```
                                                
                                                
                                                
                                                
                                                
                B             B         B      
               ---           ---              B
                         N    E          --- G 
                       ---         ---    ---  
 P      F22     L      F443                    
################################################
```

Verify: row 5 `B` at col 12, 33, 40; row 6 platforms cols 11-13 and 21-23, `N` at col 35, `B` at col 41; row 7 `E` at col 31, platform cols 42-44, `G` at col 46; row 8 platforms cols 30-32 and 38-40; row 9 `P` at col 1, `F22` (1 cell at col 8), `L` at col 15, `F443` (1 cell at col 23); row 10 ground.

- [ ] **Step 4: Commit**

```bash
git add packet-courier/Assets/_Levels/
git commit -m "feat: World1 level layouts (text grids)"
```

---
### Task 3: Text-layout level generator

**Files:**
- Create: `packet-courier/Assets/_Scripts/Editor/EditorLevelGenerator.cs`

**Interfaces:**
- Consumes: `EditorGenerator.PrefabRoot` (private const), `EditorGenerator.EnsureFolder(string)` (private static), `EditorGenerator.EnsureInputManager/EnsureCamera/EnsureLevelManager/EnsureHUD` (private static, defined in `EditorGeneratorSetup.cs`) — all reachable because this file declares the same `partial class EditorGenerator`. Consumes the `Assets/_Levels/*.txt` files from Task 2 and the prefabs from `Assets/_Prefabs/`.
- Produces: `public static void GenerateLevelScenes()` (menu item `Tools/Packet Courier/Generate Level Scenes`) which builds the 3 level scenes into `Assets/_Scenes/` and registers them (and any other existing scene files there) in `EditorBuildSettings`. Task 5 will extend this method.

- [ ] **Step 1: Write the file**

```csharp
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
```

- [ ] **Step 2: Run the generator in batch mode**

Run:
```powershell
& "C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe" -batchmode -quit -nographics -projectPath "C:\Users\AXIOO\game-collab\packet-courier" -executeMethod EditorGenerator.GenerateLevelScenes -logFile "C:\Users\AXIOO\AppData\Local\Temp\opencode\unity-task3.log"
```

- [ ] **Step 3: Assert the results**

Run (in PowerShell):
```powershell
$scenes = @("World1_Segment1","World1_Segment2","World1_Segment3")
foreach ($s in $scenes) { "scene: $s -> $(Test-Path 'packet-courier\Assets\_Scenes\$s.unity')" }
Select-String -Path "C:\Users\AXIOO\AppData\Local\Temp\opencode\unity-task3.log" -Pattern "error CS" | Select-Object -ExpandProperty Line
Select-String -Path "packet-courier\ProjectSettings\EditorBuildSettings.asset" -Pattern "World1_Segment"
```
Expected: each scene `True`; no `error CS` lines; build settings asset lists the 3 segment paths.

- [ ] **Step 4: Commit**

```bash
git add packet-courier/Assets/_Scripts/Editor/EditorLevelGenerator.cs packet-courier/Assets/_Scenes/ packet-courier/ProjectSettings/EditorBuildSettings.asset
git commit -m "feat: text-layout level generator for World1 segments"
```

---
### Task 4: Runtime UI scripts

**Files:**
- Create: `packet-courier/Assets/_Scripts/UI/MenuUI.cs`
- Create: `packet-courier/Assets/_Scripts/UI/GameOverUI.cs`
- Create: `packet-courier/Assets/_Scripts/UI/HubUI.cs`

**Interfaces:**
- Consumes: `GameManager.Ensure()`, `GameManager.Instance.StartRun(int)`, `GameManager.Instance.RetrySegment()`, `GameManager.Instance.ReturnToMenu()` (Task 1), `GameManager.Instance.totalDataBits` (existing public field)
- Produces: components added to meta scenes in Task 5 (`MenuUI`, `GameOverUI`, `HubUI`). Each builds its own UI in `Awake` (mirrors the `EnsureHUD` pattern in `EditorGeneratorSetup.cs`). No `UnityEditor` references.

- [ ] **Step 1: Create `MenuUI.cs`**

```csharp
using UnityEngine;

public class MenuUI : MonoBehaviour
{
    private void Awake()
    {
        var canvas = CreateCanvas();
        CreateLabel(canvas.transform, "PACKET COURIER", new Vector2(0.3f, 0.7f), new Vector2(0.7f, 0.85f), 34);
        CreateLabel(canvas.transform, "World 1", new Vector2(0.35f, 0.62f), new Vector2(0.65f, 0.7f), 18);
        CreateButton(canvas.transform, "Start Run", new Vector2(0.4f, 0.4f), new Vector2(0.6f, 0.5f), () => GameManager.Ensure().StartRun(0));
        CreateButton(canvas.transform, "Quit", new Vector2(0.4f, 0.25f), new Vector2(0.6f, 0.35f), () => Application.Quit());
    }

    private static Canvas CreateCanvas()
    {
        var go = new GameObject("Menu_Canvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<UnityEngine.UI.CanvasScaler>();
        go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        return canvas;
    }

    private static void CreateLabel(Transform parent, string text, Vector2 aMin, Vector2 aMax, int fontSize)
    {
        var go = new GameObject("Label_" + text.Replace(" ", "_"));
        go.transform.SetParent(parent, false);
        var txt = go.AddComponent<UnityEngine.UI.Text>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        txt.text = text;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
    }

    private static void CreateButton(Transform parent, string label, Vector2 aMin, Vector2 aMax, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Button_" + label.Replace(" ", "_"));
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.15f, 0.2f, 0.35f, 0.9f);
        var btn = go.AddComponent<UnityEngine.UI.Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        CreateLabel(go.transform, label, Vector2.zero, Vector2.one, 20);
    }
}
```

- [ ] **Step 2: Create `GameOverUI.cs`**

```csharp
using UnityEngine;

public class GameOverUI : MonoBehaviour
{
    private void Awake()
    {
        var canvas = CreateCanvas();
        CreateLabel(canvas.transform, "PACKET LOST", new Vector2(0.3f, 0.7f), new Vector2(0.7f, 0.85f), 34);
        CreateLabel(canvas.transform, "TTL depleted. Connection severed.", new Vector2(0.3f, 0.6f), new Vector2(0.7f, 0.68f), 16);
        CreateButton(canvas.transform, "Retry Segment", new Vector2(0.4f, 0.4f), new Vector2(0.6f, 0.5f), () => GameManager.Instance?.RetrySegment());
        CreateButton(canvas.transform, "Back to Menu", new Vector2(0.4f, 0.25f), new Vector2(0.6f, 0.35f), () => GameManager.Instance?.ReturnToMenu());
    }

    private static Canvas CreateCanvas()
    {
        var go = new GameObject("GameOver_Canvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<UnityEngine.UI.CanvasScaler>();
        go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        return canvas;
    }

    private static void CreateLabel(Transform parent, string text, Vector2 aMin, Vector2 aMax, int fontSize)
    {
        var go = new GameObject("Label_" + text.Replace(" ", "_"));
        go.transform.SetParent(parent, false);
        var txt = go.AddComponent<UnityEngine.UI.Text>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        txt.text = text;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = new Color(1f, 0.6f, 0.6f);
    }

    private static void CreateButton(Transform parent, string label, Vector2 aMin, Vector2 aMax, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Button_" + label.Replace(" ", "_"));
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.35f, 0.15f, 0.15f, 0.9f);
        var btn = go.AddComponent<UnityEngine.UI.Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        CreateLabel(go.transform, label, Vector2.zero, Vector2.one, 20);
    }
}
```

- [ ] **Step 3: Create `HubUI.cs`**

```csharp
using UnityEngine;

public class HubUI : MonoBehaviour
{
    private void Awake()
    {
        var canvas = CreateCanvas();
        CreateLabel(canvas.transform, "RUN COMPLETE", new Vector2(0.3f, 0.7f), new Vector2(0.7f, 0.85f), 34);
        CreateLabel(canvas.transform, "World 1 cleared. World 2 locked.", new Vector2(0.3f, 0.58f), new Vector2(0.7f, 0.66f), 16);
        CreateButton(canvas.transform, "Back to Menu", new Vector2(0.4f, 0.3f), new Vector2(0.6f, 0.4f), () => GameManager.Instance?.ReturnToMenu());
    }

    private static Canvas CreateCanvas()
    {
        var go = new GameObject("Hub_Canvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<UnityEngine.UI.CanvasScaler>();
        go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        return canvas;
    }

    private static void CreateLabel(Transform parent, string text, Vector2 aMin, Vector2 aMax, int fontSize)
    {
        var go = new GameObject("Label_" + text.Replace(" ", "_"));
        go.transform.SetParent(parent, false);
        var txt = go.AddComponent<UnityEngine.UI.Text>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        txt.text = text;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
    }

    private static void CreateButton(Transform parent, string label, Vector2 aMin, Vector2 aMax, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Button_" + label.Replace(" ", "_"));
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.15f, 0.3f, 0.2f, 0.9f);
        var btn = go.AddComponent<UnityEngine.UI.Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        CreateLabel(go.transform, label, Vector2.zero, Vector2.one, 20);
    }
}
```

- [ ] **Step 4: Compile check via batch mode**

Run:
```powershell
& "C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe" -batchmode -quit -nographics -projectPath "C:\Users\AXIOO\game-collab\packet-courier" -logFile "C:\Users\AXIOO\AppData\Local\Temp\opencode\unity-task4.log"
```
Expected: `Select-String -Path "...\unity-task4.log" -Pattern "error CS"` returns NOTHING.

- [ ] **Step 5: Commit**

```bash
git add packet-courier/Assets/_Scripts/UI/
git commit -m "feat: MenuUI, GameOverUI, HubUI runtime scripts"
```

---
### Task 5: Meta scenes (Menu / GameOver / Hub) + final build order

**Files:**
- Modify: `packet-courier/Assets/_Scripts/Editor/EditorLevelGenerator.cs` (extend `GenerateLevelScenes` + add `BuildMetaScenes`, `EnsureEventSystem`, `BuildMetaScene`)

**Interfaces:**
- Consumes: `MenuUI`, `GameOverUI`, `HubUI` from Task 4
- Produces: the 3 meta scenes in `Assets/_Scenes/` and the final 6-entry build order (Menu first). `RegisterBuildSettings()` already exists from Task 3 and skips missing files, so it is reused unchanged.

- [ ] **Step 1: Extend `GenerateLevelScenes` to also build meta scenes**

Replace the body of `GenerateLevelScenes` so it calls `BuildMetaScenes()` before `RegisterBuildSettings()`:

```csharp
        EnsureFolder(ScenesRoot);
        BuildLevelScenes();
        BuildMetaScenes();
        RegisterBuildSettings();
```

- [ ] **Step 2: Add the meta-scene builders**

Append these methods to the `EditorLevelGenerator.cs` file (inside the partial class, before the final `#endif`):

```csharp
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
        EnsureEventSystem();
        var go = new GameObject(sceneName);
        go.AddComponent(uiComponent);

        if (!EditorSceneManager.SaveScene(scene, scenePath))
            Debug.LogError($"[EditorLevelGenerator] Gagal menyimpan scene: {scenePath}");
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<UnityEngine.EventSystems.EventSystem>();
        go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }
```

- [ ] **Step 3: Run the generator in batch mode**

Run:
```powershell
& "C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe" -batchmode -quit -nographics -projectPath "C:\Users\AXIOO\game-collab\packet-courier" -executeMethod EditorGenerator.GenerateLevelScenes -logFile "C:\Users\AXIOO\AppData\Local\Temp\opencode\unity-task5.log"
```

- [ ] **Step 4: Assert all 6 scenes + build order**

Run (in PowerShell):
```powershell
$scenes = @("Menu","World1_Segment1","World1_Segment2","World1_Segment3","Hub","GameOver")
foreach ($s in $scenes) { "scene: $s -> $(Test-Path 'packet-courier\Assets\_Scenes\$s.unity')" }
Select-String -Path "C:\Users\AXIOO\AppData\Local\Temp\opencode\unity-task5.log" -Pattern "error CS" | Select-Object -ExpandProperty Line
Select-String -Path "packet-courier\ProjectSettings\EditorBuildSettings.asset" -Pattern "m_Scenes|_Scenes/" | Select-Object -ExpandProperty Line
```
Expected: all 6 scenes `True`; no `error CS`; the build settings asset lists 6 enabled entries in order Menu, World1_Segment1, World1_Segment2, World1_Segment3, Hub, GameOver.

- [ ] **Step 5: Commit**

```bash
git add packet-courier/Assets/_Scripts/Editor/EditorLevelGenerator.cs packet-courier/Assets/_Scenes/ packet-courier/ProjectSettings/EditorBuildSettings.asset
git commit -m "feat: generate Menu/GameOver/Hub scenes and final build order"
```

---
### Task 6: End-to-end verification

**Files:** none (verification only)

**Interfaces:**
- Consumes: everything from Tasks 1-5

- [ ] **Step 1: Full clean regeneration**

Delete the 6 generated scenes and re-run from scratch to prove the generator is idempotent and self-contained:
```powershell
Remove-Item packet-courier\Assets\_Scenes\*.unity -ErrorAction SilentlyContinue
& "C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe" -batchmode -quit -nographics -projectPath "C:\Users\AXIOO\game-collab\packet-courier" -executeMethod EditorGenerator.GenerateLevelScenes -logFile "C:\Users\AXIOO\AppData\Local\Temp\opencode\unity-final.log"
```

- [ ] **Step 2: Assert final state**

```powershell
$scenes = @("Menu","World1_Segment1","World1_Segment2","World1_Segment3","Hub","GameOver")
$missing = $scenes | Where-Object { -not (Test-Path "packet-courier\Assets\_Scenes\$_.unity") }
"missing scenes: $($missing.Count) (expect 0)"
$errors = Select-String -Path "C:\Users\AXIOO\AppData\Local\Temp\opencode\unity-final.log" -Pattern "error CS"
"compile errors: $($errors.Count) (expect 0)"
$buildEntries = (Select-String -Path "packet-courier\ProjectSettings\EditorBuildSettings.asset" -Pattern "_Scenes/").Line.Count
"build entries: $buildEntries (expect 6)"
```
Expected: 0 missing, 0 compile errors, 6 build entries.

- [ ] **Step 3: Manual playtest (interactive, by user)**

1. Open Unity → `Tools > Packet Courier > Generate Prefabs` (regenerates the fixed animator), then `Tools > Packet Courier > Generate Level Scenes`.
2. Open `Assets/_Scenes/Menu.unity`, press Play. Click **Start Run**.
3. Play through Segment 1 (no hazards) → Segment 2 (toggle encrypted before spikes) → Segment 3 (switch ports Q/E at the gates, jump over the laser from the platform) → land in Hub.
4. Die deliberately in a segment → GameOver appears → **Retry Segment** reloads the same segment; **Back to Menu** works.
5. Confirm the player animation plays (regression check).

## Self-Review

- **Spec coverage:** Every spec section maps to a task — generator + tile set (T3), level content (T2), UI scripts (T4), meta scenes + build order (T5), GameManager API (T1), end-to-end verification (T6). The "reuse Ensure* helpers" requirement is satisfied by T3 calling `EnsureInputManager/EnsureCamera/EnsureLevelManager/EnsureHUD`. The `Menu` scene creates GameManager at runtime via `GameManager.Ensure()` (spec §GameManager additions).
- **Placeholder scan:** No TBD/TODO; every code step has full source. The only intentional data is in the `.txt` layouts, which are given verbatim.
- **Type consistency:** `Ensure()` / `RetrySegment()` / `ReturnToMenu()` signatures in T1 match usage in T4. `BuildMetaScene(string, System.Type)` in T5 matches calls with `typeof(MenuUI)` etc. `RegisterBuildSettings()` is defined once in T3 and reused in T5 unchanged. Gate parsing uses `Gate_Firewall.requiredPort` (verified field). Prefab paths use verified asset names.

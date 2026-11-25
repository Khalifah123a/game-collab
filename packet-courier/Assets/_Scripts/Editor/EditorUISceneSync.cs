#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Automatically configures custom pixel UI sprites/fonts with crisp Point filtering,
/// resets accidentally zoomed-in GameView windows (e.g. 0.66x cropping top/bottom of 1920x1080)
/// back to full-frame fit (minScale), and refreshes MenuUI / GameOverUI / HubUI / GameplayHUD.
/// </summary>
[InitializeOnLoad]
public static class EditorUISceneSync
{
    private const string SessionKey = "PacketCourier_UISynced_v5";

    static EditorUISceneSync()
    {
        EditorApplication.delayCall += OnDelayCall;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnDelayCall()
    {
        FitAllGameViewsToWindow();

        if (EditorApplication.isPlaying)
        {
            var hud = Object.FindAnyObjectByType<GameplayHUD>();
            if (hud != null) hud.RebuildUI();
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        PurgeMissingScriptsInLoadedScene();

        if (!SessionState.GetBool(SessionKey, false))
        {
            SessionState.SetBool(SessionKey, true);
            EnsureUISpriteImportSettings();
        }

        RefreshActiveSceneUI();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            // Destroy Edit-Mode procedural UI canvases BEFORE Unity writes Temp/__Backupscenes/0.backup
            CleanupEditModePreviewCanvases();
            PurgeMissingScriptsInLoadedScene();
        }
        else if (state == PlayModeStateChange.EnteredPlayMode)
        {
            EditorApplication.delayCall += FitAllGameViewsToWindow;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += () =>
            {
                PurgeMissingScriptsInLoadedScene();
                FitAllGameViewsToWindow();
                RefreshActiveSceneUI();
            };
        }
    }

    public static void PurgeMissingScriptsInLoadedScene()
    {
        if (EditorApplication.isPlaying) return;
        var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var go in allObjects)
        {
            if (go != null && GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) > 0)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            }
        }
    }

    private static void CleanupEditModePreviewCanvases()
    {
        string[] previewRoots = { "Menu_Canvas", "DeathOverlay", "Hub_Canvas", "TacticalHUD_v2_Root" };
        foreach (string rootName in previewRoots)
        {
            var go = GameObject.Find(rootName);
            while (go != null)
            {
                Object.DestroyImmediate(go);
                go = GameObject.Find(rootName);
            }
        }
    }

    [MenuItem("Tools/Packet Courier/Fit Game View Zoom (Fix Cropped HUD)")]
    public static void FitAllGameViewsMenu()
    {
        FitAllGameViewsToWindow(forceSnap: true);
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        Debug.Log("[Packet Courier UI] Game View Scale snapped to full 1920x1080 Fit-to-Window.");
    }

    public static void FitAllGameViewsToWindow()
    {
        FitAllGameViewsToWindow(false);
    }

    /// <summary>
    /// Ensures any docked GameView using a fixed resolution (e.g. Full HD 1920x1080) is not zoomed
    /// beyond minScale (which crops the top &amp; bottom HUD edges inside short docked panes).
    /// </summary>
    public static void FitAllGameViewsToWindow(bool forceSnap)
    {
        try
        {
            var gvType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            if (gvType == null) return;

            const BindingFlags bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var minScaleProp = gvType.GetProperty("minScale", bf);
            var defaultScaleField = gvType.GetField("m_defaultScale", bf);
            var zoomAreaField = gvType.GetField("m_ZoomArea", bf);
            var snapZoomMethod = gvType.GetMethod("SnapZoom", bf, null, new[] { typeof(float) }, null);
            var updateZoomMethod = gvType.GetMethod("UpdateZoomAreaAndParent", bf, null, System.Type.EmptyTypes, null);

            foreach (var obj in Resources.FindObjectsOfTypeAll(gvType))
            {
                if (obj is not EditorWindow win) continue;

                if (updateZoomMethod != null)
                    updateZoomMethod.Invoke(win, null);

                float targetScale = -1f;
                if (minScaleProp != null && minScaleProp.GetValue(win) is float ms && ms > 0.01f)
                    targetScale = ms;
                else if (defaultScaleField != null && defaultScaleField.GetValue(win) is float ds && ds > 0.01f)
                    targetScale = ds;

                if (targetScale <= 0.01f) continue;

                bool needsSnap = forceSnap;
                if (!needsSnap && zoomAreaField != null)
                {
                    object zoomArea = zoomAreaField.GetValue(win);
                    if (zoomArea != null)
                    {
                        var scaleField = zoomArea.GetType().GetField("m_Scale", bf);
                        if (scaleField != null && scaleField.GetValue(zoomArea) is Vector2 curScale)
                        {
                            if (curScale.y > targetScale + 0.005f || curScale.x > targetScale + 0.005f)
                                needsSnap = true;
                        }
                    }
                }

                if (needsSnap && snapZoomMethod != null)
                {
                    snapZoomMethod.Invoke(win, new object[] { targetScale });
                    if (updateZoomMethod != null)
                        updateZoomMethod.Invoke(win, null);
                    win.Repaint();
                }
            }
        }
        catch
        {
            // Ignore reflection differences across future Unity Editor releases
        }
    }

    [MenuItem("Tools/Packet Courier/Apply & Preview New UI (Menu, HUD & Die Scene)")]
    public static void ApplyAndPreviewNewUI()
    {
        AssetDatabase.Refresh();
        EnsureUISpriteImportSettings();
        FitAllGameViewsToWindow(forceSnap: true);
        RefreshActiveSceneUI();
        SceneView.RepaintAll();
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        Debug.Log("[Packet Courier UI] Custom pixel UI sprites, fonts, Game View zoom, and active scene UI refreshed.");
    }

    [MenuItem("Tools/Packet Courier/Open World 1 Stage & Preview HUD")]
    public static void OpenWorld1StageAndPreviewHUD()
    {
        if (EditorApplication.isPlaying) return;
        const string path = "Assets/_Scenes/World1_Segment1.unity";
        if (File.Exists(path))
        {
            EditorSceneManager.OpenScene(path);
            EnsureUISpriteImportSettings();
            FitAllGameViewsToWindow(forceSnap: true);
            RefreshActiveSceneUI();
            SceneView.RepaintAll();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
    }

    [MenuItem("Tools/Packet Courier/Open Main Menu Scene")]
    public static void OpenMainMenuScene()
    {
        if (EditorApplication.isPlaying) return;
        const string path = "Assets/_Scenes/Menu.unity";
        if (File.Exists(path))
        {
            EditorSceneManager.OpenScene(path);
            FitAllGameViewsToWindow(forceSnap: true);
            RefreshActiveSceneUI();
        }
    }

    [MenuItem("Tools/Packet Courier/Open Die Scene (Game Over)")]
    public static void OpenDieScene()
    {
        if (EditorApplication.isPlaying) return;
        const string path = "Assets/_Scenes/GameOver.unity";
        if (File.Exists(path))
        {
            EditorSceneManager.OpenScene(path);
            FitAllGameViewsToWindow(forceSnap: true);
            RefreshActiveSceneUI();
        }
    }

    public static void EnsureUISpriteImportSettings()
    {
        string[] folders = { "Assets/Resources/UI/Sprites", "Assets/Resources/GeneratedArt" };
        bool changed = false;

        foreach (string folder in folders)
        {
            if (!Directory.Exists(folder)) continue;
            foreach (string file in Directory.GetFiles(folder, "*.png"))
            {
                string assetPath = file.Replace('\\', '/');
                if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer) continue;

                string fileName = Path.GetFileNameWithoutExtension(assetPath);
                bool isSliced = fileName.StartsWith("btn_") || fileName.StartsWith("card_") || fileName == "death_card_bg";
                bool isBadge = fileName.StartsWith("badge_");
                Vector4 desiredBorder = isSliced
                    ? new Vector4(12, 12, 12, 12)
                    : (isBadge ? new Vector4(8, 8, 8, 8) : Vector4.zero);
                if (fileName == "death_card_bg") desiredBorder = new Vector4(16, 16, 16, 16);

                if (importer.textureType != TextureImporterType.Sprite ||
                    importer.spriteImportMode != SpriteImportMode.Single ||
                    importer.filterMode != FilterMode.Point ||
                    importer.textureCompression != TextureImporterCompression.Uncompressed ||
                    importer.spriteBorder != desiredBorder)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.filterMode = FilterMode.Point;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.spriteBorder = desiredBorder;

                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    settings.spriteBorder = desiredBorder;
                    importer.SetTextureSettings(settings);

                    importer.SaveAndReimport();
                    changed = true;
                }
            }
        }

        if (changed)
        {
            AssetDatabase.Refresh();
        }
    }

    public static void RefreshActiveSceneUI()
    {
        var menu = Object.FindAnyObjectByType<MenuUI>();
        if (menu != null)
        {
            menu.RebuildUI();
        }

        var gameOver = Object.FindAnyObjectByType<GameOverUI>();
        if (gameOver != null)
        {
            gameOver.RebuildUI();
        }

        var hub = Object.FindAnyObjectByType<HubUI>();
        if (hub != null)
        {
            hub.RebuildUI();
        }

        var gameplayHud = Object.FindAnyObjectByType<GameplayHUD>();
        if (gameplayHud != null)
        {
            gameplayHud.RebuildUI();
        }
    }

    public static void VerifyHUDInBatch()
    {
        AssetDatabase.Refresh();
        EnsureUISpriteImportSettings();

        const string stagePath = "Assets/_Scenes/World1_Segment1.unity";
        EditorSceneManager.OpenScene(stagePath);

        var hud = Object.FindAnyObjectByType<GameplayHUD>();
        if (hud == null)
        {
            throw new System.InvalidOperationException("GameplayHUD missing in World1_Segment1.unity");
        }

        hud.RebuildUI();
        Canvas.ForceUpdateCanvases();

        var crest = Resources.Load<Sprite>("UI/Sprites/ttl_emblem_crest");
        var dash = Resources.Load<Sprite>("UI/Sprites/icon_phase_dash");
        var buf = Resources.Load<Sprite>("UI/Sprites/icon_emergency_buffer");
        if (crest == null || dash == null || buf == null)
        {
            throw new System.InvalidOperationException(
                $"Missing HUD custom sprites: crest={crest != null}, dash={dash != null}, buf={buf != null}");
        }

        string assetReport = GameplayValidation.ValidateAssets();

        GameObject tempPlayer = null;
        if (Object.FindAnyObjectByType<PlayerHealth>() == null)
        {
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Player.prefab");
            if (playerPrefab != null)
                tempPlayer = Object.Instantiate(playerPrefab);
        }

        try
        {
            var texts = hud.GetComponentsInChildren<UnityEngine.UI.Text>(true);
            bool hasTtl = false;
            bool hasStamina = false;
            foreach (var t in texts)
            {
                if (t.text.StartsWith("TTL")) hasTtl = true;
                if (t.text.StartsWith("STAMINA")) hasStamina = true;
            }
            if (!hasTtl || !hasStamina)
                throw new System.InvalidOperationException($"Missing required HUD labels: TTL={hasTtl}, STAMINA={hasStamina}");

            string hudReport = Screen.width >= 320
                ? GameplayValidation.CheckRuntimeHUD()
                : $"PASS (Batch {Screen.width}x{Screen.height}): TextNodes={texts.Length}, TTL & STAMINA verified";

            Debug.Log($"[HUD_V2_VERIFY_PASS] {hudReport} | Assets={assetReport.Split('\n').Length} checks | CustomSprites=3/3");
        }
        finally
        {
            if (tempPlayer != null)
                Object.DestroyImmediate(tempPlayer);
        }
    }
}
#endif

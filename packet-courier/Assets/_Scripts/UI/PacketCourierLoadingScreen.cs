using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Cinematic Cyberpunk Loading Screen: "Tukang paket mengirim server ke server lain".
/// Shows the animated courier running along a data cable between two server racks,
/// carrying a glowing data packet payload, with live TCP protocol handshake logs,
/// bandwidth progress, and smooth asynchronous scene loading.
/// </summary>
public class PacketCourierLoadingScreen : MonoBehaviour
{
    private static PacketCourierLoadingScreen _instance;

    private Canvas _canvas;
    private CanvasGroup _canvasGroup;
    private RectTransform _cableRt;
    private RectTransform _courierRt;
    private Image _courierImage;
    private Text _headerText;
    private Text _serverOriginText;
    private Text _serverDestText;
    private Text _logConsole;
    private Text _progressPercentText;
    private Image _progressBarFill;
    private Image _packetGlowImage;

    private Sprite[] _courierFrames;
    private int _currentFrame;
    private float _frameTimer;

    private readonly List<string> _consoleLines = new List<string>();

    public static void LoadScene(string sceneName, string originName = null, string destName = null)
    {
        if (!Application.isPlaying)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        var screen = Ensure();
        if (!screen.gameObject.activeSelf) screen.gameObject.SetActive(true);
        screen.StartCoroutine(screen.LoadSceneRoutine(sceneName, originName, destName));
    }

    public static PacketCourierLoadingScreen Ensure()
    {
        if (_instance != null)
        {
            if (!_instance.gameObject.activeSelf) _instance.gameObject.SetActive(true);
            return _instance;
        }
        var go = new GameObject("PacketCourierLoadingScreen");
        _instance = go.AddComponent<PacketCourierLoadingScreen>();
        DontDestroyOnLoad(go);
        return _instance;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        LoadAssets();
        BuildUI();
        if (_canvas != null) _canvas.enabled = false;
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
        }
    }

    private void LoadAssets()
    {
        _courierFrames = new Sprite[4];
        for (int i = 0; i < 4; i++)
        {
            _courierFrames[i] = Resources.Load<Sprite>("GeneratedArt/courier_run_" + i);
            if (_courierFrames[i] == null)
            {
                _courierFrames[i] = Resources.Load<Sprite>("PlayerSprites/run" + (i + 1));
            }
        }
    }

    private void BuildUI()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 999;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();
        _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // 1. Dark Cyberspace Backdrop
        var bg = CreatePanel(transform, "Background", Vector2.zero, Vector2.one, new Color(0.03f, 0.05f, 0.09f, 0.98f));
        bg.raycastTarget = true;

        // 2. Top Header & Title
        _headerText = CreateText(transform, "Header", new Vector2(0.1f, 0.88f), new Vector2(0.9f, 0.96f), 26, new Color(0.2f, 0.95f, 1f, 1f));
        _headerText.fontStyle = FontStyle.Bold;
        _headerText.text = "MEMUAT STAGE...\nMengirim paket ke server tujuan";

        // 3. Server Racks (Origin & Destination)
        var serverSprite = Resources.Load<Sprite>("GeneratedArt/server_rack");
        if (serverSprite == null) serverSprite = CreateServerRackFallback();

        // Server A (Origin - Left)
        var serverA = CreatePanel(transform, "Server_Origin", new Vector2(0.12f, 0.38f), new Vector2(0.24f, 0.72f), Color.white);
        serverA.sprite = serverSprite;
        serverA.type = Image.Type.Simple;

        _serverOriginText = CreateText(transform, "Label_ServerOrigin", new Vector2(0.08f, 0.28f), new Vector2(0.28f, 0.38f), 17, new Color(0.4f, 0.9f, 1f, 0.95f));
        _serverOriginText.text = "[ORIGIN SERVER : ALPHA]\nHOST: 192.168.1.1\nPORT: 8080 [TRANSMITTING]";

        // Server B (Destination - Right)
        var serverB = CreatePanel(transform, "Server_Dest", new Vector2(0.76f, 0.38f), new Vector2(0.88f, 0.72f), Color.white);
        serverB.sprite = serverSprite;
        serverB.type = Image.Type.Simple;

        _serverDestText = CreateText(transform, "Label_ServerDest", new Vector2(0.72f, 0.28f), new Vector2(0.92f, 0.38f), 17, new Color(0f, 1f, 0.75f, 0.95f));
        _serverDestText.text = "[TARGET SERVER : BETA]\nHOST: 10.0.4.2\nPORT: 8080 [RECEIVING]";

        // 4. Fiber Optic Cable Bridge
        var cable = CreatePanel(transform, "DataCable", new Vector2(0.24f, 0.52f), new Vector2(0.76f, 0.535f), new Color(0f, 0.85f, 1f, 0.85f));
        _cableRt = cable.rectTransform;

        // 5. The Running Courier carrying the packet
        var courierGo = new GameObject("CourierRunner", typeof(RectTransform), typeof(Image));
        courierGo.transform.SetParent(transform, false);
        _courierRt = courierGo.GetComponent<RectTransform>();
        _courierRt.anchorMin = new Vector2(0.24f, 0.53f);
        _courierRt.anchorMax = new Vector2(0.24f, 0.53f);
        _courierRt.pivot = new Vector2(0.5f, 0f);
        _courierRt.sizeDelta = new Vector2(96f, 96f);

        _courierImage = courierGo.GetComponent<Image>();
        if (_courierFrames != null && _courierFrames.Length > 0 && _courierFrames[0] != null)
        {
            _courierImage.sprite = _courierFrames[0];
        }

        // Glowing Data Packet carried by Courier
        var packetGo = new GameObject("CourierPacketGlow", typeof(RectTransform), typeof(Image));
        packetGo.transform.SetParent(courierGo.transform, false);
        var packetRt = packetGo.GetComponent<RectTransform>();
        packetRt.anchoredPosition = new Vector2(22f, 48f);
        packetRt.sizeDelta = new Vector2(28f, 28f);
        _packetGlowImage = packetGo.GetComponent<Image>();
        var pkgSprite = Resources.Load<Sprite>("GeneratedArt/packet_integrity_icon") ?? Resources.Load<Sprite>("GeneratedArt/package_backpack");
        if (pkgSprite != null) _packetGlowImage.sprite = pkgSprite;
        _packetGlowImage.color = new Color(1f, 0.85f, 0.2f, 1f);

        // 6. Progress Bar (Bottom)
        var barBg = CreatePanel(transform, "ProgressBar_BG", new Vector2(0.25f, 0.18f), new Vector2(0.75f, 0.21f), new Color(0.1f, 0.16f, 0.25f, 1f));
        _progressBarFill = CreatePanel(barBg.transform, "Fill", Vector2.zero, new Vector2(0f, 1f), new Color(0f, 1f, 0.9f, 1f));

        _progressPercentText = CreateText(transform, "ProgressPercent", new Vector2(0.25f, 0.22f), new Vector2(0.75f, 0.26f), 18, Color.white);
        _progressPercentText.text = "BUFFERING STREAM: 0% [0 KB / 128 KB]";

        // 7. Live TCP Handshake Log Console
        _logConsole = CreateText(transform, "LogConsole", new Vector2(0.25f, 0.05f), new Vector2(0.75f, 0.16f), 14, new Color(0.55f, 0.85f, 1f, 0.8f));
        _logConsole.alignment = TextAnchor.UpperLeft;
    }

    private IEnumerator LoadSceneRoutine(string sceneName, string originName, string destName)
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (_canvas != null) _canvas.enabled = true;
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = true;
        }

        // 1. Duck current BGM immediately when entering warp/loading screen
        AudioManager.DuckBGMForWarp(0.12f, 0.4f);

        // 2. Identify destination World and Stage for Sector Intro
        var gm = GameManager.Instance;
        int curWorld = gm != null ? gm.currentWorldIndex + 1 : 1;
        int curSegment = gm != null ? gm.currentSegmentIndex + 1 : 1;

        int targetWorld = curWorld;
        int targetSegment = curSegment;
        bool isWorldScene = false;

        if (sceneName.StartsWith("World"))
        {
            isWorldScene = true;
            string[] parts = sceneName.Split('_');
            if (parts.Length == 2)
            {
                int.TryParse(parts[0].Replace("World", ""), out targetWorld);
                int.TryParse(parts[1].Replace("Segment", ""), out targetSegment);
            }
        }

        // Set Sector Intro labels
        if (isWorldScene)
        {
            string worldTitle = WorldRules.Title(targetWorld - 1).ToUpperInvariant();
            if (_headerText != null)
            {
                _headerText.text = TutorialWorldBuilder.IsTutorialWorldActive
                    ? "WORLD 00 — TUTORIAL\nMenyiapkan arena latihan..."
                    : $"WORLD {targetWorld} — {worldTitle}\nMenuju Stage {targetSegment}...";
            }
            _serverDestText.text = string.IsNullOrEmpty(destName)
                ? $"SERVER B: {worldTitle}\n10.0.{targetWorld}.{targetSegment}\nSiap menerima paket"
                : destName;
        }
        else if (sceneName == "Hub")
        {
            if (_headerText != null)
            {
                _headerText.text = "WORLD SELESAI\nKembali ke Hub...";
            }
            _serverDestText.text = string.IsNullOrEmpty(destName)
                ? "HUB KURIR\n127.0.0.1\nKoneksi aman"
                : destName;
        }
        else if (sceneName == "Menu")
        {
            if (_headerText != null)
            {
                _headerText.text = "MENU UTAMA\nKembali ke layar awal...";
            }
            _serverDestText.text = string.IsNullOrEmpty(destName)
                ? "MENU UTAMA\nSiap berangkat"
                : destName;
        }

        _serverOriginText.text = string.IsNullOrEmpty(originName)
            ? $"SERVER A\n192.168.{curWorld}.{curSegment}\nWorld {curWorld} Stage {curSegment}"
            : originName;

        _consoleLines.Clear();
        AddLog("[TCP 3-WAY HANDSHAKE] Menghubungkan ke server tujuan...");
        if (isWorldScene)
        {
            AddLog($"[ROUTE TRACE] Target Sector: World {targetWorld} ({WorldRules.Title(targetWorld - 1)}), Stage {targetSegment} OK.");
        }
        else
        {
            AddLog($"[ROUTE TRACE] Hop 1: Gateway 10.0.0.1 -> Transit Cable OK.");
        }

        // Determine destination audio track
        AudioManager.BGMTrack targetTrack = isWorldScene ? AudioManager.GetWorldBGMTrack(targetWorld - 1) :
                                           (sceneName == "Hub" ? AudioManager.BGMTrack.SafeZone :
                                           (sceneName == "Menu" ? AudioManager.BGMTrack.Menu : AudioManager.BGMTrack.None));
        bool isDifferentTrack = AudioManager.Instance != null && AudioManager.Instance.CurrentBGMTrack != targetTrack && targetTrack != AudioManager.BGMTrack.None;
        bool trackFaded = false;

        // Smooth fade-in
        float fadeIn = 0.22f;
        float elapsed = 0f;
        while (elapsed < fadeIn)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeIn);
            yield return null;
        }
        _canvasGroup.alpha = 1f;

        // Async Load
        AsyncOperation asyncOp = null;
        try
        {
            asyncOp = SceneManager.LoadSceneAsync(sceneName);
            if (asyncOp != null)
            {
                asyncOp.allowSceneActivation = false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PacketCourierLoadingScreen] Scene load exception: {ex.Message}");
        }

        float minTransferTime = 1.35f;
        float timer = 0f;
        float displayProgress = 0f;
        int logStep = 0;

        while (timer < minTransferTime || (asyncOp != null && asyncOp.progress < 0.9f))
        {
            timer += Time.unscaledDeltaTime;
            float asyncNorm = asyncOp != null ? Mathf.Clamp01(asyncOp.progress / 0.9f) : 1f;
            float timeNorm = Mathf.Clamp01(timer / minTransferTime);
            displayProgress = Mathf.Min(asyncNorm, timeNorm);

            // Animate Courier Running along the cable
            AnimateCourier(displayProgress);

            // Animate Packet Glow pulse
            if (_packetGlowImage != null)
            {
                float pulse = 0.8f + 0.2f * Mathf.Sin(Time.unscaledTime * 14f);
                _packetGlowImage.rectTransform.localScale = Vector3.one * pulse;
            }

            // Update Progress Bar
            if (_progressBarFill != null)
            {
                _progressBarFill.rectTransform.anchorMax = new Vector2(displayProgress, 1f);
            }
            if (_progressPercentText != null)
            {
                int kb = (int)(displayProgress * 128f);
                _progressPercentText.text = $"STREAMING DATA PACKETS: {(int)(displayProgress * 100f)}% [{kb} KB / 128 KB]";
            }

            // If transitioning to another world / track, fade out current track completely before scene switch
            if (isDifferentTrack && !trackFaded && timer > minTransferTime * 0.65f)
            {
                trackFaded = true;
                AudioManager.FadeOutBGM(0.4f);
            }

            // Dynamic Handshake Logs
            if (timer > 0.25f && logStep == 0)
            {
                AddLog("[ACK RECEIVED] SYN-ACK received. RTT latency: 14ms.");
                logStep = 1;
            }
            else if (timer > 0.55f && logStep == 1)
            {
                AddLog("[PAYLOAD STREAM] Transmitting payload blocks [0x4A...0x9F].");
                logStep = 2;
            }
            else if (timer > 0.85f && logStep == 2)
            {
                AddLog("[CHECKSUM VERIFIED] SHA256 integrity 100%. 0 packet drop.");
                logStep = 3;
            }
            else if (timer > 1.15f && logStep == 3)
            {
                AddLog("[TRANSMISSION COMPLETE] Packet delivered! Entering destination server.");
                logStep = 4;
            }

            yield return null;
        }

        // Complete! Activate scene
        if (asyncOp != null)
        {
            asyncOp.allowSceneActivation = true;
            while (!asyncOp.isDone)
            {
                yield return null;
            }
        }

        // Smooth fade-out
        float fadeOut = 0.25f;
        elapsed = 0f;
        while (elapsed < fadeOut)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOut);
            yield return null;
        }

        if (_canvas != null) _canvas.enabled = false;
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
        }
    }

    private void AnimateCourier(float progress)
    {
        if (_courierRt == null) return;

        // Stride across the cable: anchor X smoothly runs from Server A (0.24) to Server B (0.76)
        float startX = 0.24f;
        float endX = 0.74f;
        float curX = Mathf.Lerp(startX, endX, progress);

        // Gentle footstep bounce
        float bob = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 16f)) * 8f;

        _courierRt.anchorMin = new Vector2(curX, 0.53f);
        _courierRt.anchorMax = new Vector2(curX, 0.53f);
        _courierRt.anchoredPosition = new Vector2(0f, bob);

        // Frame cycle
        _frameTimer += Time.unscaledDeltaTime;
        if (_frameTimer >= 0.08f)
        {
            _frameTimer = 0f;
            _currentFrame = (_currentFrame + 1) % 4;
            if (_courierFrames != null && _courierFrames.Length > _currentFrame && _courierFrames[_currentFrame] != null)
            {
                _courierImage.sprite = _courierFrames[_currentFrame];
            }
        }
    }

    private void AddLog(string line)
    {
        _consoleLines.Add(line);
        if (_consoleLines.Count > 4) _consoleLines.RemoveAt(0);
        if (_logConsole != null)
        {
            _logConsole.text = string.Join("\n", _consoleLines);
        }
    }

    private static Image CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    private static Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var txt = go.GetComponent<Text>();
        txt.font = fontSize >= 22 ? PixelUIKit.GetDisplayFont() : PixelUIKit.GetMonoFont();
        txt.fontSize = fontSize;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = color;
        txt.raycastTarget = false;
        return txt;
    }

    private static Sprite CreateServerRackFallback()
    {
        int w = 32, h = 64;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool frame = x == 0 || x == w - 1 || y == 0 || y == h - 1;
                bool slot = y % 8 == 0;
                bool led = (x == 4 || x == 8) && (y % 8 == 4);
                if (frame) pixels[y * w + x] = new Color32(40, 60, 90, 255);
                else if (slot) pixels[y * w + x] = new Color32(20, 30, 45, 255);
                else if (led) pixels[y * w + x] = new Color32(0, 255, 180, 255);
                else pixels[y * w + x] = new Color32(12, 18, 28, 255);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
    }
}

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Precision Speedrun &amp; Split Timer HUD for competitive packet routing.
/// Integrates seamlessly with GameplayHUD v2's Top-Right Chronometer Blade so no duplicate
/// legacy box overlaps Top-Left vitals, while preserving standalone fallback support.
/// </summary>
public class SpeedrunTimerHUD : MonoBehaviour
{
    public static SpeedrunTimerHUD Instance { get; private set; }

    private Canvas _canvas;
    private GameObject _containerGo;
    private Text _timeText;
    private Text _splitText;
    private float _runTimer;
    private bool _isRunning;
    private GameplayHUD _cachedGameplayHud;
    private float _hudCheckTimer;

    private const string PrefsEnabledKey = "PacketCourier_SpeedrunEnabled";
    public static bool IsEnabled
    {
        get => PlayerPrefs.GetInt(PrefsEnabledKey, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(PrefsEnabledKey, value ? 1 : 0);
            PlayerPrefs.Save();
            if (Instance != null && Instance._canvas != null)
                Instance._canvas.enabled = value;
        }
    }

    public static SpeedrunTimerHUD Ensure()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("SpeedrunTimerHUD");
        return go.AddComponent<SpeedrunTimerHUD>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildHUD();
    }

    private void BuildHUD()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.pixelPerfect = true;
        _canvas.sortingOrder = 890;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Font displayFont = PixelUIKit.GetDisplayFont();
        Font monoFont = PixelUIKit.GetMonoFont();

        _containerGo = new GameObject("TimerContainer", typeof(RectTransform));
        _containerGo.transform.SetParent(transform, false);
        var bg = _containerGo.AddComponent<Image>();
        Vector2[] bladePoly =
        {
            new Vector2(14, 0), new Vector2(236, 0), new Vector2(236, 54), new Vector2(0, 54), new Vector2(0, 14)
        };
        bg.sprite = TacticalHUDGraphics.GetPolygonSprite(
            "speedrun_standalone_blade",
            236,
            54,
            bladePoly,
            new Color32(7, 14, 27, 238),
            TacticalHUDGraphics.ColElectricCyan,
            1);
        bg.raycastTarget = false;

        var cRt = _containerGo.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(1f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(1f, 1f);
        cRt.anchoredPosition = new Vector2(-24f, -20f);
        cRt.sizeDelta = new Vector2(236f, 54f);

        // Timer text
        var timeGO = new GameObject("TimeText", typeof(RectTransform));
        timeGO.transform.SetParent(_containerGo.transform, false);
        _timeText = timeGO.AddComponent<Text>();
        _timeText.font = displayFont;
        _timeText.fontSize = 18;
        _timeText.color = TacticalHUDGraphics.ColElectricCyan;
        _timeText.alignment = TextAnchor.MiddleRight;
        _timeText.raycastTarget = false;
        _timeText.text = "00:00.000";
        var tRt = timeGO.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 1f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(-6f, -4f);
        tRt.sizeDelta = new Vector2(-20f, 26f);

        // Split text
        var splitGO = new GameObject("SplitText", typeof(RectTransform));
        splitGO.transform.SetParent(_containerGo.transform, false);
        _splitText = splitGO.AddComponent<Text>();
        _splitText.font = monoFont;
        _splitText.fontSize = 11;
        _splitText.color = TacticalHUDGraphics.ColNeonMint;
        _splitText.alignment = TextAnchor.MiddleRight;
        _splitText.raycastTarget = false;
        _splitText.text = "SPLIT // READY";
        var sRt = splitGO.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0f, 0f);
        sRt.anchorMax = new Vector2(1f, 0f);
        sRt.pivot = new Vector2(0.5f, 0f);
        sRt.anchoredPosition = new Vector2(-6f, 4f);
        sRt.sizeDelta = new Vector2(-20f, 20f);

        _canvas.enabled = IsEnabled;
        _isRunning = true;
    }

    private void Update()
    {
        _hudCheckTimer -= Time.unscaledDeltaTime;
        if (_hudCheckTimer <= 0f)
        {
            _hudCheckTimer = 0.5f;
            if (_cachedGameplayHud == null)
                _cachedGameplayHud = FindAnyObjectByType<GameplayHUD>();

            bool showStandalone = _cachedGameplayHud == null || !_cachedGameplayHud.isActiveAndEnabled;
            if (_containerGo != null && _containerGo.activeSelf != showStandalone)
                _containerGo.SetActive(showStandalone);
        }

        if (!_isRunning || Time.timeScale == 0f) return;
        _runTimer += Time.deltaTime;

        if (_containerGo != null && _containerGo.activeSelf && _timeText != null)
        {
            int minutes = Mathf.FloorToInt(_runTimer / 60f);
            int seconds = Mathf.FloorToInt(_runTimer % 60f);
            int milliseconds = Mathf.FloorToInt((_runTimer * 1000f) % 1000f);
            _timeText.text = $"{minutes:00}:{seconds:00}.{milliseconds:000}";
        }
    }

    public void RecordSplit(string checkpointLabel, float deltaSeconds)
    {
        if (_cachedGameplayHud == null)
            _cachedGameplayHud = FindAnyObjectByType<GameplayHUD>();

        if (_cachedGameplayHud != null)
            _cachedGameplayHud.ShowSplitDelta(checkpointLabel, deltaSeconds);

        if (_splitText == null) return;
        if (deltaSeconds <= 0f)
        {
            _splitText.text = $"{checkpointLabel}: {deltaSeconds:F2}s";
            _splitText.color = TacticalHUDGraphics.ColNeonMint;
        }
        else
        {
            _splitText.text = $"{checkpointLabel}: +{deltaSeconds:F2}s";
            _splitText.color = TacticalHUDGraphics.ColCrimson;
        }
    }

    public void ResetTimer()
    {
        _runTimer = 0f;
    }

    public static void Toggle()
    {
        IsEnabled = !IsEnabled;
    }
}

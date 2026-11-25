using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Wall-cling stamina bar. Visible only while the player is actively clinging;
/// hidden otherwise per Tahap 2 contract (HUD hanya saat cling).
/// Styled as a compact pixel-crisp chamfered tactical micro-gauge tracking beside the courier.
/// </summary>
public class ClingStaminaHUD : MonoBehaviour
{
    public static ClingStaminaHUD Instance { get; private set; }

    private Canvas _canvas;
    private RectTransform _canvasRt;
    private RectTransform _gaugeRt;
    private Image _bg;
    private Image _fill;
    private Text _label;
    private PlayerController _player;
    private PlayerStamina _stamina;
    private Camera _cam;

    public static ClingStaminaHUD Ensure()
    {
        if (Instance != null) return Instance;
        return new GameObject("ClingStaminaHUD").AddComponent<ClingStaminaHUD>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildCanvas();
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (_player == null) _player = FindAnyObjectByType<PlayerController>();
        if (_stamina == null && _player != null) _stamina = _player.GetComponent<PlayerStamina>();
        if (_player == null || _stamina == null)
        {
            SetVisible(false);
            return;
        }

        bool clinging = _player.isClinging || _stamina.isClinging;
        SetVisible(clinging);
        if (!clinging) return;

        float cap = Mathf.Max(1f, _stamina.Capacity);
        float frac = Mathf.Clamp01(_stamina.Current / cap);
        _fill.fillAmount = frac;

        if (frac <= 0.25f)
        {
            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 16f);
            _fill.color = Color.Lerp(TacticalHUDGraphics.ColCrimson, Color.white, pulse * 0.35f);
            _label.color = TacticalHUDGraphics.ColCrimson;
        }
        else if (frac <= 0.55f)
        {
            _fill.color = TacticalHUDGraphics.ColAmber;
            _label.color = TacticalHUDGraphics.ColAmber;
        }
        else
        {
            _fill.color = TacticalHUDGraphics.ColNeonMint;
            _label.color = TacticalHUDGraphics.ColSkyDash;
        }

        UpdateDiegeticPosition();
    }

    private void UpdateDiegeticPosition()
    {
        if (_gaugeRt == null || _canvasRt == null || _player == null) return;
        if (_cam == null) _cam = Camera.main;
        if (_cam == null)
        {
            _gaugeRt.anchoredPosition = new Vector2(0f, -210f);
            return;
        }

        Vector3 worldAnchor = _player.transform.position + new Vector3(0f, 1.05f, 0f);
        Vector3 screenPt = _cam.WorldToScreenPoint(worldAnchor);
        if (screenPt.z < 0f) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRt, screenPt, null, out Vector2 localPt))
        {
            Vector2 halfSize = _canvasRt.rect.size * 0.5f;
            localPt.x = Mathf.Clamp(localPt.x, -halfSize.x + 110f, halfSize.x - 110f);
            localPt.y = Mathf.Clamp(localPt.y, -halfSize.y + 80f, halfSize.y - 100f);
            _gaugeRt.anchoredPosition = localPt;
        }
    }

    private void SetVisible(bool visible)
    {
        if (_canvas != null && _canvas.gameObject.activeSelf != visible)
            _canvas.gameObject.SetActive(visible);
    }

    private void BuildCanvas()
    {
        var go = new GameObject("Cling_Canvas", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        _canvas = go.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.pixelPerfect = true;
        _canvas.sortingOrder = 25;
        _canvasRt = go.GetComponent<RectTransform>();

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();

        var bgGo = new GameObject("ClingBar_Bg", typeof(RectTransform));
        bgGo.transform.SetParent(go.transform, false);
        _gaugeRt = bgGo.GetComponent<RectTransform>();
        _gaugeRt.anchorMin = new Vector2(0.5f, 0.5f);
        _gaugeRt.anchorMax = new Vector2(0.5f, 0.5f);
        _gaugeRt.pivot = new Vector2(0.5f, 0.5f);
        _gaugeRt.sizeDelta = new Vector2(132f, 16f);

        Vector2[] framePoly =
        {
            new Vector2(5, 0), new Vector2(127, 0), new Vector2(132, 8),
            new Vector2(127, 16), new Vector2(5, 16), new Vector2(0, 8)
        };
        _bg = bgGo.AddComponent<Image>();
        _bg.sprite = TacticalHUDGraphics.GetPolygonSprite(
            "cling_gauge_frame",
            132,
            16,
            framePoly,
            new Color32(7, 15, 28, 238),
            TacticalHUDGraphics.ColBorderSteel,
            2);
        _bg.raycastTarget = false;

        var fillGo = new GameObject("ClingBar_Fill", typeof(RectTransform));
        fillGo.transform.SetParent(bgGo.transform, false);
        var fillRt = fillGo.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(1f, 1f);
        fillRt.offsetMin = new Vector2(6f, 4f);
        fillRt.offsetMax = new Vector2(-6f, -4f);
        _fill = fillGo.AddComponent<Image>();
        _fill.sprite = TacticalHUDGraphics.GetWhitePixel();
        _fill.type = Image.Type.Filled;
        _fill.fillMethod = Image.FillMethod.Horizontal;
        _fill.fillAmount = 1f;
        _fill.color = TacticalHUDGraphics.ColNeonMint;
        _fill.raycastTarget = false;

        var lblGo = new GameObject("ClingBar_Tag", typeof(RectTransform));
        lblGo.transform.SetParent(bgGo.transform, false);
        var lblRt = lblGo.GetComponent<RectTransform>();
        lblRt.anchorMin = new Vector2(0.5f, 1f);
        lblRt.anchorMax = new Vector2(0.5f, 1f);
        lblRt.pivot = new Vector2(0.5f, 0f);
        lblRt.anchoredPosition = new Vector2(0f, 2f);
        lblRt.sizeDelta = new Vector2(96f, 14f);
        _label = lblGo.AddComponent<Text>();
        _label.font = PixelUIKit.GetDisplayFont();
        _label.fontSize = 9;
        _label.alignment = TextAnchor.MiddleCenter;
        _label.color = TacticalHUDGraphics.ColSkyDash;
        _label.text = "GRIP";
        _label.raycastTarget = false;
    }
}

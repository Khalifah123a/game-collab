using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tactical Action-Platformer HUD (v2 Redesign — SANABI / Katana ZERO / Nine Sols / Dead Cells inspired).
/// Zero full-width SaaS bars; 89% unobstructed center viewport; sculpted 45-degree chamfered cyber plates,
/// 12 skewed (-14 deg) TTL cells with white-gold Drain Ghost lag, 2D Linear Packet Route Track with live
/// courier slider, speedrun split blade with +delta bit pickup pop, and 3 octagonal ability sockets.
/// </summary>
public class GameplayHUD : MonoBehaviour
{
    private const int TtlCellCount = 12;
    private const float RouteTrackPixelWidth = 386f;
    private const float VitalsBaseX = 26f;
    private const float VitalsBaseY = -26f;

    private Canvas _canvas;
    private CanvasScaler _scaler;
    private RectTransform _uiRoot;
    private Rect _lastSafeArea;

    // --- Zone 1: Top-Left Sculpted Vitals Rig ---
    private RectTransform _vitalsRigRt;
    private Image _ttlHousingImg;
    private Image _ttlTopRailImg;
    private Image _crestBackingImg;
    private readonly Image[] _topClampTeeth = new Image[6];
    private readonly Image[] _botClampTeeth = new Image[6];
    private readonly Image[] _ttlCells = new Image[TtlCellCount];
    private Text _ttlTagLabel;
    private Text _ttlNumShadow;
    private Text _ttlNumMain;
    private Text _ttlUnitLabel;

    private readonly Image[] _dashPipFills = new Image[3];
    private Text _staminaTagLabel;

    private Image _integritySealImg;
    private Text _integrityPctText;
    private Image _gradeStampImg;
    private Text _gradeStampText;
    private GameObject _modeChipGo;
    private Text _modeChipText;
    private Text _modeHintText;
    private Image _questTrackerPlateImg;
    private Image _questLeftBarImg;
    private Text _questHeaderText;
    private Text _questPrimaryText;
    private Text _questOptionalText;

    // Cached state sprites for zero-GC per-frame updates
    private Sprite _housingOptimalSpr, _housingWarnSpr, _housingCritSpr;
    private Sprite _crestBackOptimalSpr, _crestBackWarnSpr, _crestBackCritSpr;
    private Sprite _toothLitTopSpr, _toothDimTopSpr, _toothLitBotSpr, _toothDimBotSpr;
    private Sprite _cellCyanSpr, _cellMintSpr, _cellAmberSpr, _cellCrimsonSpr, _cellGhostSpr, _cellEmptySpr;
    private Sprite _dashPipReadySpr, _dashPipChargeSpr;
    private Sprite _gradeSSpr, _gradeASpr, _gradeBSpr, _gradeCSpr;
    private Sprite _sockReadyCyanSpr, _sockReadyMintSpr, _sockWarnAmberSpr, _sockPassiveMagSpr, _sockDepletedSpr;
    private Sprite _sockSmallPassiveSpr, _sockSmallDepletedSpr;

    // --- Drain Ghost State ---
    private float _lastTtl = -1f;
    private float _ghostTtl = -1f;
    private float _ghostHoldTimer;

    // --- Zone 2: Top-Center 2D Linear Packet Route Track & Boss Bar ---
    private RectTransform _routeFillRt;
    private RectTransform _courierSliderRt;
    private readonly Image[] _routeClampTeeth = new Image[5];
    private GameObject _quizPipGo;
    private RectTransform _quizPipRt;
    private Text _quizPipText;
    private GameObject _secretPipGo;
    private RectTransform _secretPipRt;
    private Text _secretPipText;
    private GameObject _tcpPipGo;
    private RectTransform _tcpPipRt;
    private Text _tcpPipText;
    private Text _destTagText;
    private Image _destTagImg;
    private Image _destChevronImg;

    private RectTransform _bossContainer;
    private RectTransform _bossFillRt;
    private RectTransform _bossGhostRt;
    private Text _bossTitleText;
    private float _bossGhostPct = 1f;

    // --- Zone 3: Top-Right Speedrun Split Blade & DataBits Purse ---
    private Text _sectorStampText;
    private Text _chronoMainText;
    private Text _chronoMillisText;
    private Text _purseTotalText;
    private Text _purseDeltaText;
    private Text _splitNoticeText;
    private int _lastTotalBits = -1;
    private int _accumulatedBitDelta;
    private float _bitDeltaTimer;
    private float _splitNoticeTimer;

    // --- Zone 4: Bottom-Left 3 Tactical Ability Sockets ---
    private Image _sock1FrameImg, _sock1IconImg;
    private RectTransform _sock1VeilRt, _sock1SweepLineRt;
    private Text _sock1TimerText, _sock1KeyText;

    private Image _sock2FrameImg, _sock2IconImg;
    private RectTransform _sock2VeilRt, _sock2SweepLineRt;
    private Text _sock2TimerText, _sock2KeyText;

    private Image _sock3FrameImg, _sock3IconImg;
    private Text _sock3KeyText;

    // --- Zone 5: System Hint & Pause Banner ---
    private Text _pauseBannerText;
    private Text _zoneStatusText;

    // --- Cached World References ---
    private PlayerHealth _health;
    private PlayerStamina _stamina;
    private PlayerController _player;
    private PlayerCombat _combat;
    private PlayerMode _playerMode;

    private ServerB_Goal _cachedGoal;
    private Checkpoint_TCP[] _checkpoints;
    private SoftwareQuizBoard[] _quizBoards;
    private SecretMemoryCore[] _secretCores;
    private HiddenHermitNPC _cachedHermit;
    private float _routeStartX;
    private float _routeEndX = 120f;
    private bool _routeOriginCaptured;
    private float _scanTimer;

    private void Start()
    {
        RebuildUI();
        ScanStageLandmarks(forceOriginCapture: true);
    }

    public void RebuildUI()
    {
        EnsureCanvas();
        BuildCachedSprites();
        CleanupExistingRoots();

        _uiRoot = CreateRect("TacticalHUD_v2_Root", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        _lastSafeArea = Rect.zero;
        ApplySafeArea();

        BuildPeripheralVignettes(_uiRoot);
        BuildTopLeftVitalsRig(_uiRoot);
        BuildTopCenterRouteTrack(_uiRoot);
        BuildTopRightChronoPurse(_uiRoot);
        BuildBottomLeftSkillSockets(_uiRoot);
        BuildBottomRightSystemHint(_uiRoot);

        Canvas.ForceUpdateCanvases();
    }

    private void CleanupExistingRoots()
    {
        _uiRoot = null;
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var childGo = transform.GetChild(i).gameObject;
            if (Application.isPlaying)
            {
                childGo.SetActive(false);
                Destroy(childGo);
            }
            else
            {
                DestroyImmediate(childGo);
            }
        }
    }

    private void ApplySafeArea()
    {
        if (_uiRoot == null) return;
        Rect sa = Screen.safeArea;
        if (sa == _lastSafeArea || Screen.width <= 0 || Screen.height <= 0) return;
        _lastSafeArea = sa;

        Vector2 aMin = sa.position;
        Vector2 aMax = sa.position + sa.size;
        aMin.x = Mathf.Clamp01(aMin.x / Screen.width);
        aMin.y = Mathf.Clamp01(aMin.y / Screen.height);
        aMax.x = Mathf.Clamp01(aMax.x / Screen.width);
        aMax.y = Mathf.Clamp01(aMax.y / Screen.height);

        _uiRoot.anchorMin = aMin;
        _uiRoot.anchorMax = aMax;
        _uiRoot.offsetMin = Vector2.zero;
        _uiRoot.offsetMax = Vector2.zero;
    }

    private void EnsureCanvas()
    {
        var rootRt = GetComponent<RectTransform>();
        if (rootRt == null) rootRt = gameObject.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;
        rootRt.pivot = new Vector2(0.5f, 0.5f);

        _canvas = GetComponent<Canvas>();
        if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.pixelPerfect = true;
        _canvas.sortingOrder = 20;

        _scaler = GetComponent<CanvasScaler>();
        if (_scaler == null) _scaler = gameObject.AddComponent<CanvasScaler>();
        _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _scaler.referenceResolution = new Vector2(1920f, 1080f);
        _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
    }

    private void BuildCachedSprites()
    {
        // TTL Mechanical Housing: M0 0 L328 0 L342 14 L330 30 L0 30 Z
        Vector2[] housingPoly =
        {
            new Vector2(0, 0), new Vector2(328, 0), new Vector2(342, 14), new Vector2(330, 30), new Vector2(0, 30)
        };
        _housingOptimalSpr = TacticalHUDGraphics.GetPolygonSprite("hud_ttl_housing_opt", 342, 30, housingPoly,
            TacticalHUDGraphics.ColChassisDark, TacticalHUDGraphics.ColBorderSteel, 2);
        _housingWarnSpr = TacticalHUDGraphics.GetPolygonSprite("hud_ttl_housing_warn", 342, 30, housingPoly,
            new Color32(18, 14, 5, 242), new Color32(146, 96, 10, 255), 2);
        _housingCritSpr = TacticalHUDGraphics.GetPolygonSprite("hud_ttl_housing_crit", 342, 30, housingPoly,
            new Color32(26, 6, 13, 245), TacticalHUDGraphics.ColCrimson, 2);

        // Crest Rotated Diamond Backing: M38 0 L76 38 L38 76 L0 38 Z
        Vector2[] crestPoly =
        {
            new Vector2(38, 0), new Vector2(76, 38), new Vector2(38, 76), new Vector2(0, 38)
        };
        _crestBackOptimalSpr = TacticalHUDGraphics.GetPolygonSprite("hud_crest_opt", 76, 76, crestPoly,
            new Color32(6, 13, 24, 250), TacticalHUDGraphics.ColBorderSteel, 2);
        _crestBackWarnSpr = TacticalHUDGraphics.GetPolygonSprite("hud_crest_warn", 76, 76, crestPoly,
            new Color32(18, 14, 5, 250), TacticalHUDGraphics.ColAmber, 2);
        _crestBackCritSpr = TacticalHUDGraphics.GetPolygonSprite("hud_crest_crit", 76, 76, crestPoly,
            new Color32(26, 6, 13, 250), TacticalHUDGraphics.ColCrimson, 2);

        // Clamp Teeth
        Vector2[] topToothPoly = { new Vector2(0, 0), new Vector2(12, 0), new Vector2(9, 4), new Vector2(3, 4) };
        Vector2[] botToothPoly = { new Vector2(3, 0), new Vector2(9, 0), new Vector2(12, 4), new Vector2(0, 4) };
        _toothLitTopSpr = TacticalHUDGraphics.GetPolygonSprite("hud_tooth_top_lit", 12, 4, topToothPoly,
            TacticalHUDGraphics.ColNeonMint, default, 0);
        _toothDimTopSpr = TacticalHUDGraphics.GetPolygonSprite("hud_tooth_top_dim", 12, 4, topToothPoly,
            new Color32(51, 65, 85, 220), default, 0);
        _toothLitBotSpr = TacticalHUDGraphics.GetPolygonSprite("hud_tooth_bot_lit", 12, 4, botToothPoly,
            new Color32(0, 255, 163, 165), default, 0);
        _toothDimBotSpr = TacticalHUDGraphics.GetPolygonSprite("hud_tooth_bot_dim", 12, 4, botToothPoly,
            new Color32(30, 41, 59, 200), default, 0);

        // 12 Skewed (-14 deg) Energy Cells: M5 0 L23 0 L18 18 L0 18 Z
        Vector2[] cellPoly = { new Vector2(5, 0), new Vector2(23, 0), new Vector2(18, 18), new Vector2(0, 18) };
        _cellCyanSpr = TacticalHUDGraphics.GetPolygonSprite("hud_cell_cyan", 23, 18, cellPoly,
            TacticalHUDGraphics.ColElectricCyan, default, 0, new Color32(224, 242, 254, 255), 3);
        _cellMintSpr = TacticalHUDGraphics.GetPolygonSprite("hud_cell_mint", 23, 18, cellPoly,
            TacticalHUDGraphics.ColNeonMint, default, 0, new Color32(209, 250, 229, 255), 3);
        _cellAmberSpr = TacticalHUDGraphics.GetPolygonSprite("hud_cell_amber", 23, 18, cellPoly,
            TacticalHUDGraphics.ColAmber, default, 0, new Color32(254, 240, 138, 255), 3);
        _cellCrimsonSpr = TacticalHUDGraphics.GetPolygonSprite("hud_cell_crimson", 23, 18, cellPoly,
            TacticalHUDGraphics.ColCrimson, default, 0, new Color32(255, 204, 213, 255), 3);
        _cellGhostSpr = TacticalHUDGraphics.GetPolygonSprite("hud_cell_ghost", 23, 18, cellPoly,
            TacticalHUDGraphics.ColGhostGold, default, 0, TacticalHUDGraphics.ColGhostCream, 6);
        _cellEmptySpr = TacticalHUDGraphics.GetPolygonSprite("hud_cell_empty", 23, 18, cellPoly,
            TacticalHUDGraphics.ColCellEmptyFill, TacticalHUDGraphics.ColCellEmptyRim, 1);

        // Skewed Dash Capacitor Pips: M4 0 L50 0 L46 10 L0 10 Z
        Vector2[] dashPipPoly = { new Vector2(4, 0), new Vector2(50, 0), new Vector2(46, 10), new Vector2(0, 10) };
        _dashPipReadySpr = TacticalHUDGraphics.GetPolygonSprite("hud_dash_ready", 50, 10, dashPipPoly,
            TacticalHUDGraphics.ColSkyDash, default, 0, new Color32(224, 242, 254, 255), 2);
        _dashPipChargeSpr = TacticalHUDGraphics.GetPolygonSprite("hud_dash_charge", 50, 10, dashPipPoly,
            TacticalHUDGraphics.ColDashRecharging, default, 0);

        // Grade Micro Stamps: M4 0 L54 0 L58 4 L58 20 L4 20 L0 16 L0 4 Z
        Vector2[] gradePoly =
        {
            new Vector2(4, 0), new Vector2(54, 0), new Vector2(58, 4),
            new Vector2(58, 20), new Vector2(4, 20), new Vector2(0, 16), new Vector2(0, 4)
        };
        _gradeSSpr = TacticalHUDGraphics.GetPolygonSprite("hud_grade_s", 58, 20, gradePoly,
            new Color32(46, 33, 5, 250), TacticalHUDGraphics.ColGold, 1);
        _gradeASpr = TacticalHUDGraphics.GetPolygonSprite("hud_grade_a", 58, 20, gradePoly,
            new Color32(7, 26, 43, 250), TacticalHUDGraphics.ColElectricCyan, 1);
        _gradeBSpr = TacticalHUDGraphics.GetPolygonSprite("hud_grade_b", 58, 20, gradePoly,
            new Color32(31, 21, 5, 250), TacticalHUDGraphics.ColAmber, 1);
        _gradeCSpr = TacticalHUDGraphics.GetPolygonSprite("hud_grade_c", 58, 20, gradePoly,
            new Color32(36, 7, 17, 250), TacticalHUDGraphics.ColCrimson, 1);

        // Ability Sockets (58x58 & 48x48 Octagons)
        Vector2[] sock58Poly =
        {
            new Vector2(8, 0), new Vector2(50, 0), new Vector2(58, 8), new Vector2(58, 50),
            new Vector2(50, 58), new Vector2(8, 58), new Vector2(0, 50), new Vector2(0, 8)
        };
        _sockReadyCyanSpr = TacticalHUDGraphics.GetPolygonSprite("hud_sock_cyan", 58, 58, sock58Poly,
            new Color32(8, 21, 38, 240), TacticalHUDGraphics.ColElectricCyan, 2);
        _sockReadyMintSpr = TacticalHUDGraphics.GetPolygonSprite("hud_sock_mint", 58, 58, sock58Poly,
            new Color32(6, 26, 20, 240), TacticalHUDGraphics.ColNeonMint, 2);
        _sockWarnAmberSpr = TacticalHUDGraphics.GetPolygonSprite("hud_sock_amber", 58, 58, sock58Poly,
            new Color32(20, 16, 6, 240), new Color32(146, 96, 10, 255), 2);
        _sockPassiveMagSpr = TacticalHUDGraphics.GetPolygonSprite("hud_sock_mag58", 58, 58, sock58Poly,
            new Color32(24, 10, 36, 240), TacticalHUDGraphics.ColMagenta, 2);
        _sockDepletedSpr = TacticalHUDGraphics.GetPolygonSprite("hud_sock_dep58", 58, 58, sock58Poly,
            new Color32(9, 14, 23, 225), new Color32(30, 41, 59, 255), 2);

        Vector2[] sock48Poly =
        {
            new Vector2(10, 0), new Vector2(38, 0), new Vector2(48, 10), new Vector2(48, 38),
            new Vector2(38, 48), new Vector2(10, 48), new Vector2(0, 38), new Vector2(0, 10)
        };
        _sockSmallPassiveSpr = TacticalHUDGraphics.GetPolygonSprite("hud_sock_mag48", 48, 48, sock48Poly,
            new Color32(24, 10, 36, 240), TacticalHUDGraphics.ColMagenta, 2);
        _sockSmallDepletedSpr = TacticalHUDGraphics.GetPolygonSprite("hud_sock_dep48", 48, 48, sock48Poly,
            new Color32(9, 14, 23, 220), new Color32(30, 41, 59, 255), 2);
    }

    // =========================================================================
    // SUBTLE FRAMELESS PERIPHERAL VIGNETTES (NO FULL-WIDTH BARS)
    // =========================================================================
    private static void BuildPeripheralVignettes(RectTransform root)
    {
        float[] alphas = { 0.42f, 0.24f, 0.10f };
        for (int i = 0; i < alphas.Length; i++)
        {
            var topBand = CreateRect($"TopVignette_{i}", root, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -(i + 1) * 22f), new Vector2(0f, -i * 22f));
            AddSolidImage(topBand, new Color(0.012f, 0.024f, 0.047f, alphas[i]));

            var botBand = CreateRect($"BotVignette_{i}", root, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, i * 20f), new Vector2(0f, (i + 1) * 20f));
            AddSolidImage(botBand, new Color(0.012f, 0.024f, 0.047f, alphas[i] * 0.85f));
        }
    }

    // =========================================================================
    // ZONE 1: TOP-LEFT SCULPTED ASYMMETRIC VITALS RIG
    // =========================================================================
    private void BuildTopLeftVitalsRig(RectTransform root)
    {
        _vitalsRigRt = CreateAnchoredBox("HUD_TopLeft_VitalsRig", root,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(VitalsBaseX, VitalsBaseY), new Vector2(440f, 136f));

        // 1. Bevel Lip Shadow under TTL Housing
        Vector2[] housingPoly =
        {
            new Vector2(0, 0), new Vector2(328, 0), new Vector2(342, 14), new Vector2(330, 30), new Vector2(0, 30)
        };
        Sprite lipSpr = TacticalHUDGraphics.GetPolygonSprite("hud_ttl_lip", 342, 30, housingPoly,
            TacticalHUDGraphics.ColBevelShadow, default, 0);
        PlaceSprite("TTL_ChassisBevelLip", _vitalsRigRt, new Vector2(54f, -37f), new Vector2(342f, 30f), lipSpr);

        // 2. Main Chamfered TTL Housing + Top Structural Rail
        _ttlHousingImg = PlaceSprite("TTL_MechanicalHousing", _vitalsRigRt, new Vector2(52f, -34f), new Vector2(342f, 30f), _housingOptimalSpr);
        var topRailRt = CreateTopLeftBox("TTL_TopAccentRail", _vitalsRigRt, new Vector2(56f, -34f), new Vector2(324f, 2f));
        _ttlTopRailImg = AddSolidImage(topRailRt, new Color32(0, 255, 163, 255));

        // 3. Mechanical Clamp Teeth above & below housing
        float[] toothOffsets = { 92f, 140f, 188f, 236f, 284f, 332f };
        for (int i = 0; i < 6; i++)
        {
            _topClampTeeth[i] = PlaceSprite($"TopClampTooth_{i}", _vitalsRigRt,
                new Vector2(toothOffsets[i], -29f), new Vector2(12f, 4f), _toothLitTopSpr);
            _botClampTeeth[i] = PlaceSprite($"BotClampTooth_{i}", _vitalsRigRt,
                new Vector2(toothOffsets[i] - 3f, -66f), new Vector2(12f, 4f), _toothLitBotSpr);
        }

        // 4. 12 Skewed Parallelogram TTL Energy Cells
        for (int i = 0; i < TtlCellCount; i++)
        {
            float cx = 72f + i * 24f;
            _ttlCells[i] = PlaceSprite($"TTL_Cell_{i + 1}", _vitalsRigRt,
                new Vector2(cx, -40f), new Vector2(23f, 18f), i < 2 ? _cellCyanSpr : _cellMintSpr);
        }

        // 5. Frameless Crisp Numeric Telemetry Above Bar (keeps "TTL" prefix for GameplayValidation)
        _ttlTagLabel = PlaceText("TTL_LabelTag", _vitalsRigRt, "TTL",
            new Vector2(76f, -9f), new Vector2(44f, 18f), 12, new Color32(0, 255, 163, 255), TextAnchor.MiddleLeft, mono: false);
        _ttlNumShadow = PlaceText("TTL_NumberShadow", _vitalsRigRt, "100",
            new Vector2(118f, -2f), new Vector2(68f, 28f), 22, new Color32(2, 4, 8, 255), TextAnchor.MiddleLeft, mono: false);
        _ttlNumMain = PlaceText("TTL_NumberMain", _vitalsRigRt, "100",
            new Vector2(116f, 0f), new Vector2(68f, 28f), 22, Color.white, TextAnchor.MiddleLeft, mono: false);
        _ttlUnitLabel = PlaceText("TTL_MaxUnit", _vitalsRigRt, "/100s",
            new Vector2(180f, -9f), new Vector2(140f, 18f), 12, new Color32(148, 163, 184, 255), TextAnchor.MiddleLeft, mono: true);

        // 6. Under-Slung Angled 3-Pip Stamina / Vector Dash Capacitor Rail
        Vector2[] dashPlatePoly = { new Vector2(0, 0), new Vector2(262, 0), new Vector2(252, 18), new Vector2(8, 18) };
        Sprite dashPlateSpr = TacticalHUDGraphics.GetPolygonSprite("hud_dash_plate", 262, 18, dashPlatePoly,
            new Color32(6, 12, 23, 238), new Color32(21, 43, 70, 255), 1);
        PlaceSprite("Dash_UnderRailPlate", _vitalsRigRt, new Vector2(64f, -71f), new Vector2(262f, 18f), dashPlateSpr);

        Vector2[] dashPipPoly = { new Vector2(4, 0), new Vector2(50, 0), new Vector2(46, 10), new Vector2(0, 10) };
        Sprite dashPipTroughSpr = TacticalHUDGraphics.GetPolygonSprite("hud_dash_trough", 50, 10, dashPipPoly,
            new Color32(10, 25, 43, 255), new Color32(3, 105, 161, 255), 1);

        for (int i = 0; i < 3; i++)
        {
            float dx = 76f + i * 54f;
            PlaceSprite($"DashPipTrough_{i + 1}", _vitalsRigRt, new Vector2(dx, -75f), new Vector2(50f, 10f), dashPipTroughSpr);
            var fillImg = PlaceSprite($"DashPipFill_{i + 1}", _vitalsRigRt, new Vector2(dx, -75f), new Vector2(50f, 10f), _dashPipReadySpr);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.fillAmount = 1f;
            _dashPipFills[i] = fillImg;
        }

        // Starts with "STAMINA" to satisfy GameplayValidation.CheckRuntimeHUD()
        _staminaTagLabel = PlaceText("Stamina_MicroTag", _vitalsRigRt, "STAMINA",
            new Vector2(242f, -73f), new Vector2(80f, 14f), 9, new Color32(125, 211, 252, 255), TextAnchor.MiddleLeft, mono: false);

        // 7. Overlapping Left Diamond Armor Crest
        Vector2[] crestPoly = { new Vector2(38, 0), new Vector2(76, 38), new Vector2(38, 76), new Vector2(0, 38) };
        Sprite crestShadowSpr = TacticalHUDGraphics.GetPolygonSprite("hud_crest_shadow", 76, 76, crestPoly,
            TacticalHUDGraphics.ColBevelShadow, default, 0);
        PlaceSprite("Crest_ArmorShadow", _vitalsRigRt, new Vector2(0f, -11f), new Vector2(76f, 76f), crestShadowSpr);
        _crestBackingImg = PlaceSprite("Crest_ArmorBacking", _vitalsRigRt, new Vector2(0f, -8f), new Vector2(76f, 76f), _crestBackOptimalSpr);
        PlaceSprite("Crest_PixelEmblem", _vitalsRigRt, new Vector2(2f, -10f), new Vector2(72f, 72f), TacticalHUDGraphics.GetTTLEmblemCrest());

        // 8. Under-Crest Packet Integrity Seal + Dynamic Grade Stamp + Mode/Quest Micro-Chip
        Vector2[] sealPoly =
        {
            new Vector2(6, 0), new Vector2(162, 0), new Vector2(168, 6),
            new Vector2(168, 26), new Vector2(6, 26), new Vector2(0, 20), new Vector2(0, 6)
        };
        Sprite sealSpr = TacticalHUDGraphics.GetPolygonSprite("hud_integrity_seal", 168, 26, sealPoly,
            new Color32(7, 18, 30, 238), new Color32(22, 53, 77, 255), 1);
        _integritySealImg = PlaceSprite("Integrity_SealPlate", _vitalsRigRt, new Vector2(16f, -95f), new Vector2(168f, 26f), sealSpr);

        Sprite shieldSpr = TacticalHUDGraphics.LoadSprite("GeneratedArt/packet_integrity_icon") ?? PixelUIKit.GetIcon("shield");
        PlaceSprite("Integrity_ShieldIcon", _vitalsRigRt, new Vector2(24f, -99f), new Vector2(18f, 18f), shieldSpr);

        _integrityPctText = PlaceText("Integrity_PctText", _vitalsRigRt, "100%",
            new Vector2(48f, -99f), new Vector2(56f, 18f), 11, new Color32(226, 232, 240, 255), TextAnchor.MiddleLeft, mono: false);

        _gradeStampImg = PlaceSprite("Integrity_GradeStamp", _vitalsRigRt, new Vector2(118f, -98f), new Vector2(58f, 20f), _gradeSSpr);
        _gradeStampText = PlaceText("Integrity_GradeText", _vitalsRigRt, "S-RANK",
            new Vector2(118f, -98f), new Vector2(58f, 20f), 8, TacticalHUDGraphics.ColGold, TextAnchor.MiddleCenter, mono: false);

        Vector2[] modePoly =
        {
            new Vector2(5, 0), new Vector2(115, 0), new Vector2(120, 5),
            new Vector2(120, 22), new Vector2(5, 22), new Vector2(0, 17), new Vector2(0, 5)
        };
        Sprite modeSpr = TacticalHUDGraphics.GetPolygonSprite("hud_mode_chip", 120, 22, modePoly,
            new Color32(8, 22, 36, 230), new Color32(30, 58, 95, 255), 1);
        var modeImg = PlaceSprite("Mode_MicroChip", _vitalsRigRt, new Vector2(192f, -97f), new Vector2(120f, 22f), modeSpr);
        _modeChipGo = modeImg.gameObject;
        _modeChipText = PlaceText("Mode_MicroChipText", _modeChipGo.GetComponent<RectTransform>(), "PLN · PORT 80",
            Vector2.zero, new Vector2(120f, 22f), 9, new Color32(125, 211, 252, 255), TextAnchor.MiddleCenter, mono: true);
        _modeHintText = PlaceText("Mode_MicroChipHint", _vitalsRigRt, "[TAB] MODE | [Q/E] PORT",
            new Vector2(192f, -114f), new Vector2(140f, 14f), 7, new Color32(100, 149, 237, 210), TextAnchor.MiddleCenter, mono: true);

        // 9. Persona 3 Reload Mandatory & Optional Quest Tracker Card under Vitals Rig
        Vector2[] questCardPoly =
        {
            new Vector2(6, 0), new Vector2(342, 0), new Vector2(348, 6),
            new Vector2(348, 54), new Vector2(6, 54), new Vector2(0, 48), new Vector2(0, 6)
        };
        Sprite questCardSpr = TacticalHUDGraphics.GetPolygonSprite("hud_quest_tracker_plate", 348, 54, questCardPoly,
            new Color32(6, 16, 28, 240), new Color32(0, 215, 255, 230), 1);
        _questTrackerPlateImg = PlaceSprite("Quest_TrackerPlate", _vitalsRigRt, new Vector2(16f, -128f), new Vector2(348f, 54f), questCardSpr);

        var leftBarRt = CreateTopLeftBox("Quest_LeftAccentBar", _vitalsRigRt, new Vector2(18f, -132f), new Vector2(4f, 46f));
        _questLeftBarImg = AddSolidImage(leftBarRt, TacticalHUDGraphics.ColElectricCyan);

        _questHeaderText = PlaceText("Quest_HeaderTag", _vitalsRigRt, "MISI WAJIB [1/2] // PROTOKOL PAKET",
            new Vector2(28f, -131f), new Vector2(328f, 15f), 9, TacticalHUDGraphics.ColElectricCyan, TextAnchor.MiddleLeft, mono: false);

        _questPrimaryText = PlaceText("Quest_PrimaryObjective", _vitalsRigRt, "► AMBIL PAKET DATA DI NPC KURIR [E]",
            new Vector2(28f, -146f), new Vector2(328f, 18f), 11, Color.white, TextAnchor.MiddleLeft, mono: false);

        _questOptionalText = PlaceText("Quest_OptionalObjective", _vitalsRigRt, "◆ MISI OPSIONAL: Bicara ke NPC Bounty di Safe Zone",
            new Vector2(28f, -163f), new Vector2(328f, 15f), 9, new Color32(148, 196, 235, 235), TextAnchor.MiddleLeft, mono: true);
    }

    // =========================================================================
    // ZONE 2: TOP-CENTER 2D LINEAR PACKET ROUTE TRACK & BOSS BAR
    // =========================================================================
    private void BuildTopCenterRouteTrack(RectTransform root)
    {
        var routeRt = CreateAnchoredBox("HUD_TopCenter_RouteTrack", root,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(568f, 54f));

        // Origin Node [A]
        Vector2[] nodeAPoly =
        {
            new Vector2(4, 0), new Vector2(24, 0), new Vector2(28, 9),
            new Vector2(24, 18), new Vector2(4, 18), new Vector2(0, 9)
        };
        Sprite nodeASpr = TacticalHUDGraphics.GetPolygonSprite("hud_route_node_a", 28, 18, nodeAPoly,
            new Color32(9, 24, 41, 238), TacticalHUDGraphics.ColSkyDash, 1);
        PlaceSprite("Route_OriginNodeA", routeRt, new Vector2(6f, -13f), new Vector2(28f, 18f), nodeASpr);
        PlaceText("Route_OriginLabelA", routeRt, "A", new Vector2(6f, -13f), new Vector2(28f, 18f),
            9, TacticalHUDGraphics.ColSkyDash, TextAnchor.MiddleCenter, mono: false);

        // Route Track Trough + Top Emerald Structural Rail + Traversed Fill
        var troughBorderRt = CreateTopLeftBox("Route_TrackBorder", routeRt, new Vector2(41f, -17f), new Vector2(388f, 10f));
        AddSolidImage(troughBorderRt, new Color32(30, 58, 95, 255));
        var troughInnerRt = CreateTopLeftBox("Route_TrackTrough", routeRt, new Vector2(42f, -18f), new Vector2(RouteTrackPixelWidth, 8f));
        AddSolidImage(troughInnerRt, new Color32(9, 20, 34, 230));

        var topRailRt = CreateTopLeftBox("Route_TopEmeraldRail", routeRt, new Vector2(42f, -14f), new Vector2(RouteTrackPixelWidth, 2f));
        AddSolidImage(topRailRt, new Color32(0, 255, 163, 110));

        _routeFillRt = CreateTopLeftBox("Route_TraversedFill", routeRt, new Vector2(42f, -19f), new Vector2(120f, 6f));
        AddSolidImage(_routeFillRt, TacticalHUDGraphics.ColNeonMint);

        // 5 Mechanical Clamp Teeth along route
        float[] rTeeth = { 82f, 154f, 226f, 298f, 370f };
        for (int i = 0; i < 5; i++)
        {
            _routeClampTeeth[i] = PlaceSprite($"Route_ClampTooth_{i + 1}", routeRt,
                new Vector2(rTeeth[i], -9f), new Vector2(10f, 4f), _toothDimTopSpr);
        }

        // Dynamic Waypoint Pip 1: CS Quiz Board (Magenta Diamond)
        Vector2[] diamondPipPoly = { new Vector2(5, 0), new Vector2(10, 5), new Vector2(5, 10), new Vector2(0, 5) };
        Sprite quizPipSpr = TacticalHUDGraphics.GetPolygonSprite("hud_pip_quiz", 10, 10, diamondPipPoly,
            TacticalHUDGraphics.ColVioletQuiz, default, 0);
        _quizPipRt = CreateTopLeftBox("Route_PipQuiz", routeRt, new Vector2(150f, -17f), new Vector2(10f, 10f));
        _quizPipGo = _quizPipRt.gameObject;
        var qImg = _quizPipGo.AddComponent<Image>();
        qImg.sprite = quizPipSpr;
        qImg.raycastTarget = false;
        _quizPipText = PlaceText("Route_LabelQuiz", _quizPipRt, "? 18m",
            new Vector2(-24f, -13f), new Vector2(58f, 14f), 9, TacticalHUDGraphics.ColVioletQuiz, TextAnchor.MiddleCenter, mono: true);

        // Dynamic Waypoint Pip 2: Secret Memory Core Sonar (Gold Circle)
        Sprite secretPipSpr = TacticalHUDGraphics.GetCirclePipSprite("hud_pip_secret", 10, TacticalHUDGraphics.ColGold);
        _secretPipRt = CreateTopLeftBox("Route_PipSecret", routeRt, new Vector2(240f, -17f), new Vector2(10f, 10f));
        _secretPipGo = _secretPipRt.gameObject;
        var sImg = _secretPipGo.AddComponent<Image>();
        sImg.sprite = secretPipSpr;
        sImg.raycastTarget = false;
        _secretPipText = PlaceText("Route_LabelSecret", _secretPipRt, "14m^",
            new Vector2(-24f, -13f), new Vector2(58f, 14f), 9, TacticalHUDGraphics.ColGold, TextAnchor.MiddleCenter, mono: true);

        // Dynamic Waypoint Pip 3: Next TCP Checkpoint (Cyan Triangle)
        Vector2[] triPipPoly = { new Vector2(5, 0), new Vector2(10, 9), new Vector2(0, 9) };
        Sprite tcpPipSpr = TacticalHUDGraphics.GetPolygonSprite("hud_pip_tcp", 10, 9, triPipPoly,
            TacticalHUDGraphics.ColElectricCyan, default, 0);
        _tcpPipRt = CreateTopLeftBox("Route_PipTCP", routeRt, new Vector2(320f, -17f), new Vector2(10f, 9f));
        _tcpPipGo = _tcpPipRt.gameObject;
        var tImg = _tcpPipGo.AddComponent<Image>();
        tImg.sprite = tcpPipSpr;
        tImg.raycastTarget = false;
        _tcpPipText = PlaceText("Route_LabelTCP", _tcpPipRt, "SYNC 28m",
            new Vector2(-32f, -13f), new Vector2(74f, 14f), 9, TacticalHUDGraphics.ColSkyDash, TextAnchor.MiddleCenter, mono: true);

        // Live Sliding Courier Diamond Marker (rendered on top of pips!)
        var sliderImg = PlaceSprite("Route_CourierSlider", routeRt,
            new Vector2(160f, -8f), new Vector2(28f, 28f), TacticalHUDGraphics.GetTTLEmblemCrest());
        _courierSliderRt = sliderImg.rectTransform;

        // Destination Server B Extraction Tag + Crisp Right Chevron
        Vector2[] destPoly =
        {
            new Vector2(6, 0), new Vector2(118, 0), new Vector2(124, 10),
            new Vector2(118, 20), new Vector2(6, 20), new Vector2(0, 10)
        };
        Sprite destSpr = TacticalHUDGraphics.GetPolygonSprite("hud_route_dest", 124, 20, destPoly,
            new Color32(5, 34, 24, 238), TacticalHUDGraphics.ColNeonMint, 1);
        _destTagImg = PlaceSprite("Route_DestServerBTag", routeRt, new Vector2(438f, -12f), new Vector2(124f, 20f), destSpr);
        _destTagText = PlaceText("Route_DestServerBText", routeRt, "SRV-B 115m",
            new Vector2(444f, -12f), new Vector2(98f, 20f), 9, TacticalHUDGraphics.ColNeonMint, TextAnchor.MiddleCenter, mono: false);

        Vector2[] chevPoly = { new Vector2(0, 0), new Vector2(6, 4.5f), new Vector2(0, 9) };
        Sprite chevSpr = TacticalHUDGraphics.GetPolygonSprite("hud_route_chev", 6, 9, chevPoly,
            TacticalHUDGraphics.ColNeonMint, default, 0);
        _destChevronImg = PlaceSprite("Route_DestChevron", routeRt, new Vector2(546f, -17.5f), new Vector2(6f, 9f), chevSpr);

        // Chamfered Boss Health Bar (only active during Boss encounters)
        _bossContainer = CreateAnchoredBox("HUD_BossBarContainer", root,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(480f, 38f));
        Vector2[] bossPlatePoly =
        {
            new Vector2(8, 0), new Vector2(472, 0), new Vector2(480, 8),
            new Vector2(472, 16), new Vector2(8, 16), new Vector2(0, 8)
        };
        Sprite bossFrameSpr = TacticalHUDGraphics.GetPolygonSprite("hud_boss_frame", 480, 16, bossPlatePoly,
            new Color32(22, 6, 12, 240), TacticalHUDGraphics.ColCrimson, 2);
        PlaceSprite("Boss_BarHousing", _bossContainer, new Vector2(0f, -18f), new Vector2(480f, 16f), bossFrameSpr);

        _bossGhostRt = CreateTopLeftBox("Boss_BarGhostFill", _bossContainer, new Vector2(10f, -21f), new Vector2(460f, 10f));
        AddSolidImage(_bossGhostRt, TacticalHUDGraphics.ColGhostGold);

        _bossFillRt = CreateTopLeftBox("Boss_BarMainFill", _bossContainer, new Vector2(10f, -21f), new Vector2(460f, 10f));
        AddSolidImage(_bossFillRt, TacticalHUDGraphics.ColCrimson);

        _bossTitleText = PlaceText("Boss_TitleLabel", _bossContainer, "ROOTKIT CORE",
            new Vector2(0f, 0f), new Vector2(480f, 16f), 11, new Color32(255, 143, 163, 255), TextAnchor.MiddleCenter, mono: false);
        _bossContainer.gameObject.SetActive(false);
    }

    // =========================================================================
    // ZONE 3: TOP-RIGHT SPEEDRUN SPLIT CHRONOMETER & DATABITS PURSE
    // =========================================================================
    private void BuildTopRightChronoPurse(RectTransform root)
    {
        var trRt = CreateAnchoredBox("HUD_TopRight_ChronoPurse", root,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -26f), new Vector2(256f, 106f));

        // Chronometer Angled Blade: M16 0 L238 0 L238 42 L0 42 L0 16 Z
        Vector2[] chronoPoly =
        {
            new Vector2(16, 0), new Vector2(238, 0), new Vector2(238, 42), new Vector2(0, 42), new Vector2(0, 16)
        };
        Sprite chronoShadowSpr = TacticalHUDGraphics.GetPolygonSprite("hud_chrono_shadow", 238, 42, chronoPoly,
            TacticalHUDGraphics.ColBevelShadow, default, 0);
        Sprite chronoPlateSpr = TacticalHUDGraphics.GetPolygonSprite("hud_chrono_plate", 238, 42, chronoPoly,
            new Color32(7, 14, 27, 238), new Color32(26, 51, 82, 255), 1);

        PlaceSprite("Chrono_BevelShadow", trRt, new Vector2(18f, -3f), new Vector2(238f, 42f), chronoShadowSpr);
        PlaceSprite("Chrono_BladePlate", trRt, new Vector2(16f, 0f), new Vector2(238f, 42f), chronoPlateSpr);

        var rightCyanEdge = CreateTopLeftBox("Chrono_RightNeonEdge", trRt, new Vector2(251f, 0f), new Vector2(3f, 42f));
        AddSolidImage(rightCyanEdge, TacticalHUDGraphics.ColElectricCyan);

        _sectorStampText = PlaceText("Chrono_SectorStamp", trRt, "W1-1",
            new Vector2(28f, -11f), new Vector2(54f, 20f), 10, TacticalHUDGraphics.ColSkyDash, TextAnchor.MiddleLeft, mono: false);
        _chronoMainText = PlaceText("Chrono_MainTime", trRt, "00:00",
            new Vector2(86f, -6f), new Vector2(114f, 30f), 22, new Color32(248, 250, 252, 255), TextAnchor.MiddleRight, mono: false);
        _chronoMillisText = PlaceText("Chrono_Millis", trRt, ".00",
            new Vector2(202f, -12f), new Vector2(44f, 22f), 13, TacticalHUDGraphics.ColElectricCyan, TextAnchor.MiddleLeft, mono: false);

        // Purse Angled Blade: M10 0 L156 0 L156 30 L0 30 L0 10 Z
        Vector2[] pursePoly =
        {
            new Vector2(10, 0), new Vector2(156, 0), new Vector2(156, 30), new Vector2(0, 30), new Vector2(0, 10)
        };
        Sprite pursePlateSpr = TacticalHUDGraphics.GetPolygonSprite("hud_purse_plate", 156, 30, pursePoly,
            new Color32(7, 14, 27, 225), new Color32(56, 43, 9, 255), 1);
        PlaceSprite("Purse_BladePlate", trRt, new Vector2(98f, -50f), new Vector2(156f, 30f), pursePlateSpr);

        var rightGoldEdge = CreateTopLeftBox("Purse_RightGoldEdge", trRt, new Vector2(251f, -50f), new Vector2(3f, 30f));
        AddSolidImage(rightGoldEdge, TacticalHUDGraphics.ColGold);

        Sprite bitSpr = TacticalHUDGraphics.LoadSprite("GeneratedArt/databit") ?? TacticalHUDGraphics.GetTTLEmblemCrest();
        PlaceSprite("Purse_DataBitIcon", trRt, new Vector2(110f, -54f), new Vector2(22f, 22f), bitSpr);

        _purseTotalText = PlaceText("Purse_TotalValue", trRt, "0",
            new Vector2(138f, -53f), new Vector2(106f, 24f), 15, TacticalHUDGraphics.ColGold, TextAnchor.MiddleRight, mono: false);

        // Transient +Bits Pickup Delta Pop (Dead Cells style)
        _purseDeltaText = PlaceText("Purse_PickupDelta", trRt, "",
            new Vector2(4f, -53f), new Vector2(88f, 24f), 12, TacticalHUDGraphics.ColNeonMint, TextAnchor.MiddleRight, mono: false);

        // Optional Checkpoint Split Delta Line
        _splitNoticeText = PlaceText("Chrono_SplitDelta", trRt, "",
            new Vector2(56f, -84f), new Vector2(198f, 18f), 10, TacticalHUDGraphics.ColNeonMint, TextAnchor.MiddleRight, mono: true);
    }

    // =========================================================================
    // ZONE 4: BOTTOM-LEFT 3 TACTICAL OVERCLOCK & ABILITY SOCKETS
    // =========================================================================
    private void BuildBottomLeftSkillSockets(RectTransform root)
    {
        var blRt = CreateAnchoredBox("HUD_BottomLeft_SkillSockets", root,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(28f, 20f), new Vector2(210f, 76f));

        Vector2[] sock58Poly =
        {
            new Vector2(8, 0), new Vector2(50, 0), new Vector2(58, 8), new Vector2(58, 50),
            new Vector2(50, 58), new Vector2(8, 58), new Vector2(0, 50), new Vector2(0, 8)
        };
        Sprite sock58Shadow = TacticalHUDGraphics.GetPolygonSprite("hud_sock58_sh", 58, 58, sock58Poly,
            TacticalHUDGraphics.ColBevelShadow, default, 0);

        Vector2[] sock48Poly =
        {
            new Vector2(10, 0), new Vector2(38, 0), new Vector2(48, 10), new Vector2(48, 38),
            new Vector2(38, 48), new Vector2(10, 48), new Vector2(0, 38), new Vector2(0, 10)
        };
        Sprite sock48Shadow = TacticalHUDGraphics.GetPolygonSprite("hud_sock48_sh", 48, 48, sock48Poly,
            TacticalHUDGraphics.ColBevelShadow, default, 0);

        // --- SOCKET 1: VECTOR / PHASE DASH [SHIFT] ---
        PlaceSprite("Sock1_Shadow", blRt, new Vector2(0f, -12f), new Vector2(58f, 58f), sock58Shadow);
        _sock1FrameImg = PlaceSprite("Sock1_Frame", blRt, new Vector2(0f, -8f), new Vector2(58f, 58f), _sockReadyCyanSpr);
        _sock1IconImg = PlaceSprite("Sock1_Icon", blRt, new Vector2(9f, -15f), new Vector2(40f, 40f), TacticalHUDGraphics.GetPhaseDashIcon());

        _sock1VeilRt = CreateTopLeftBox("Sock1_CDVeil", blRt, new Vector2(3f, -11f), new Vector2(52f, 0f));
        AddSolidImage(_sock1VeilRt, new Color32(4, 7, 12, 208));
        _sock1SweepLineRt = CreateTopLeftBox("Sock1_CDSweepLine", blRt, new Vector2(3f, -11f), new Vector2(52f, 2f));
        AddSolidImage(_sock1SweepLineRt, TacticalHUDGraphics.ColAmber);
        _sock1SweepLineRt.gameObject.SetActive(false);

        _sock1TimerText = PlaceText("Sock1_CDTimer", blRt, "",
            new Vector2(0f, -20f), new Vector2(58f, 24f), 13, TacticalHUDGraphics.ColGold, TextAnchor.MiddleCenter, mono: false);
        BuildKeycapPill(blRt, "Sock1_Keycap", new Vector2(5f, -58f), new Vector2(48f, 17f),
            new Color32(15, 30, 51, 255), TacticalHUDGraphics.ColSkyDash, "SHIFT", out _sock1KeyText);

        // --- SOCKET 2: SECONDARY OVERCLOCK (NANO PARRY [Q] / CYBER BLASTER [F]) ---
        PlaceSprite("Sock2_Shadow", blRt, new Vector2(70f, -12f), new Vector2(58f, 58f), sock58Shadow);
        _sock2FrameImg = PlaceSprite("Sock2_Frame", blRt, new Vector2(70f, -8f), new Vector2(58f, 58f), _sockDepletedSpr);
        Sprite parrySpr = TacticalHUDGraphics.LoadSprite("GeneratedArt/shield_parry") ?? PixelUIKit.GetIcon("shield");
        _sock2IconImg = PlaceSprite("Sock2_Icon", blRt, new Vector2(79f, -16f), new Vector2(40f, 40f), parrySpr);

        _sock2VeilRt = CreateTopLeftBox("Sock2_CDVeil", blRt, new Vector2(73f, -11f), new Vector2(52f, 0f));
        AddSolidImage(_sock2VeilRt, new Color32(4, 7, 12, 208));
        _sock2SweepLineRt = CreateTopLeftBox("Sock2_CDSweepLine", blRt, new Vector2(73f, -11f), new Vector2(52f, 2f));
        AddSolidImage(_sock2SweepLineRt, TacticalHUDGraphics.ColAmber);
        _sock2SweepLineRt.gameObject.SetActive(false);

        _sock2TimerText = PlaceText("Sock2_CDTimer", blRt, "",
            new Vector2(70f, -20f), new Vector2(58f, 24f), 12, TacticalHUDGraphics.ColGold, TextAnchor.MiddleCenter, mono: false);
        BuildKeycapPill(blRt, "Sock2_Keycap", new Vector2(85f, -58f), new Vector2(28f, 17f),
            new Color32(28, 22, 8, 255), TacticalHUDGraphics.ColAmber, "Q", out _sock2KeyText);

        // --- SOCKET 3: PASSIVE HARDWARE RELIC (EMERGENCY BUFFER / MAGNET / BATTERY) [AUTO] ---
        PlaceSprite("Sock3_Shadow", blRt, new Vector2(142f, -17f), new Vector2(48f, 48f), sock48Shadow);
        _sock3FrameImg = PlaceSprite("Sock3_Frame", blRt, new Vector2(142f, -14f), new Vector2(48f, 48f), _sockSmallDepletedSpr);
        _sock3IconImg = PlaceSprite("Sock3_Icon", blRt, new Vector2(149f, -21f), new Vector2(34f, 34f), TacticalHUDGraphics.GetEmergencyBufferIcon());
        BuildKeycapPill(blRt, "Sock3_Keycap", new Vector2(146f, -58f), new Vector2(40f, 15f),
            new Color32(38, 14, 54, 255), TacticalHUDGraphics.ColMagenta, "AUTO", out _sock3KeyText, fontSize: 7);
    }

    private static void BuildKeycapPill(
        RectTransform parent,
        string name,
        Vector2 pos,
        Vector2 size,
        Color32 fill,
        Color32 border,
        string label,
        out Text keyText,
        int fontSize = 8)
    {
        var lipRt = CreateTopLeftBox(name + "_Lip", parent, pos + new Vector2(0f, -2f), size);
        AddSolidImage(lipRt, new Color32(2, 6, 13, 255));

        var borderRt = CreateTopLeftBox(name + "_Border", parent, pos, size);
        AddSolidImage(borderRt, border);

        var capRt = CreateTopLeftBox(name + "_Cap", parent, pos + new Vector2(1f, -1f), size - new Vector2(2f, 2f));
        AddSolidImage(capRt, fill);

        keyText = PlaceText(name + "_Text", borderRt, label, Vector2.zero, size,
            fontSize, new Color32(226, 232, 240, 255), TextAnchor.MiddleCenter, mono: false);
    }

    // =========================================================================
    // ZONE 5: BOTTOM-RIGHT WHISPER-QUIET SYSTEM HINT & PAUSE OVERLAY
    // =========================================================================
    private void BuildBottomRightSystemHint(RectTransform root)
    {
        var statusRt = CreateAnchoredBox("HUD_BottomRight_ZoneStatus", root,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 40f), new Vector2(360f, 22f));
        _zoneStatusText = PlaceText("ZoneStatusText", statusRt, "SAFE ZONE // TTL STABIL",
            Vector2.zero, new Vector2(360f, 22f), 12, TacticalHUDGraphics.ColNeonMint, TextAnchor.MiddleRight, mono: true);

        var hintRt = CreateAnchoredBox("HUD_BottomRight_SystemHint", root,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 18f), new Vector2(340f, 20f));
        PlaceText("SystemHintText", hintRt, "[F1] TUTORIAL   [TAB] HUB   [ESC] PAUSE",
            Vector2.zero, new Vector2(340f, 20f), 11, new Color(0.58f, 0.64f, 0.72f, 0.78f), TextAnchor.MiddleRight, mono: true);

        var pauseRt = CreateAnchoredBox("HUD_CenterPauseLabel", root,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 56f));
        _pauseBannerText = PlaceText("PauseText", pauseRt, "",
            Vector2.zero, new Vector2(360f, 56f), 28, TacticalHUDGraphics.ColElectricCyan, TextAnchor.MiddleCenter, mono: false);
    }

    // =========================================================================
    // LIVE PER-FRAME TELEMETRY & ANIMATION LOOP
    // =========================================================================
    private void Update()
    {
        if (_uiRoot == null) return;
        ApplySafeArea();

        if (_health == null) _health = FindAnyObjectByType<PlayerHealth>();
        if (_player == null) _player = FindAnyObjectByType<PlayerController>();
        if (_stamina == null && _player != null) _stamina = _player.GetComponent<PlayerStamina>();
        if (_combat == null && _player != null) _combat = _player.GetComponent<PlayerCombat>();
        if (_playerMode == null && _player != null) _playerMode = _player.GetComponent<PlayerMode>();

        if (_health == null || _player == null || _stamina == null) return;

        _scanTimer += Time.unscaledDeltaTime;
        if (_scanTimer >= 1.5f)
        {
            _scanTimer = 0f;
            ScanStageLandmarks(forceOriginCapture: false);
        }

        var gm = GameManager.Instance;

        UpdateTopLeftVitals(gm);
        UpdateRouteTrackAndWaypoints();
        UpdateBossBar();
        UpdateTopRightChronoAndPurse(gm);
        UpdateBottomLeftSockets();

        if (_zoneStatusText != null)
        {
            bool inSafe = SafeZone.IsPlayerSafe;
            int wIdx = gm != null ? gm.currentWorldIndex : 0;
            bool hasPkg = gm != null && gm.packageStates != null &&
                          wIdx >= 0 && wIdx < gm.packageStates.Length &&
                          gm.packageStates[wIdx] >= PackageState.Active;
            if (inSafe)
            {
                _zoneStatusText.text = hasPkg
                    ? "[SAFE ZONE // TTL AMAN]  PAKET SIAP -> KELUAR KE KANAN"
                    : "[SAFE ZONE // TTL AMAN]  WAJIB: AMBIL PAKET [E] / UPGRADE SHOP";
                _zoneStatusText.color = TacticalHUDGraphics.ColNeonMint;
            }
            else
            {
                _zoneStatusText.text = hasPkg
                    ? "[ZONA AKTIF // TTL MENYUSUT]  ANTAR PAKET KE PENERIMA / SERVER B >>"
                    : "[ZONA AKTIF // TTL MENYUSUT]  WAJIB: AMBIL PAKET DI NPC KURIR!";
                _zoneStatusText.color = _health.ttlCurrent <= _health.ttlMax * 0.35f
                    ? TacticalHUDGraphics.ColAmber
                    : TacticalHUDGraphics.ColElectricCyan;
            }
        }

        if (Input.GetKeyDown(KeyCode.F1) && !PauseMenuUI.IsPaused && !DialogUI.IsOpen)
        {
            CyberOpeningAndTutorialUI.StartSequence(includeOpeningPrologue: false);
        }

        if (_pauseBannerText != null)
        {
            _pauseBannerText.text = (gm != null && gm.state == GameManager.GameState.Paused) ? "// PAUSED //" : "";
        }
    }

    private void UpdateTopLeftVitals(GameManager gm)
    {
        float maxTtl = Mathf.Max(1f, _health.ttlMax);
        float curTtl = Mathf.Clamp(_health.ttlCurrent, 0f, maxTtl);
        float ttlFrac = Mathf.Clamp01(curTtl / maxTtl);

        // Drain Ghost Lag calculation (0.45s hold on sharp impact loss, smooth catch-up)
        if (_lastTtl < 0f || curTtl > _ghostTtl)
        {
            _ghostTtl = curTtl;
            _ghostHoldTimer = 0f;
        }
        else if (_lastTtl - curTtl > 1.25f)
        {
            _ghostHoldTimer = 0.45f;
        }
        _lastTtl = curTtl;

        if (_ghostHoldTimer > 0f)
        {
            _ghostHoldTimer -= Time.deltaTime;
        }
        else if (_ghostTtl > curTtl)
        {
            _ghostTtl = Mathf.MoveTowards(_ghostTtl, curTtl, maxTtl * 0.28f * Time.deltaTime);
        }

        int activeCells = curTtl <= 0.01f ? 0 : Mathf.Clamp(Mathf.CeilToInt(ttlFrac * TtlCellCount), 1, TtlCellCount);
        int ghostCells = Mathf.Clamp(Mathf.CeilToInt(Mathf.Clamp01(_ghostTtl / maxTtl) * TtlCellCount), activeCells, TtlCellCount);

        bool isCrit = ttlFrac <= 0.20f;
        bool isWarn = !isCrit && ttlFrac <= 0.50f;

        // Housing, Crest & Rail dynamic state
        if (isCrit)
        {
            float strobe = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 12.5f);
            _ttlHousingImg.sprite = _housingCritSpr;
            _crestBackingImg.sprite = _crestBackCritSpr;
            _ttlTopRailImg.color = Color.Lerp(new Color32(255, 51, 102, 255), new Color32(255, 180, 195, 255), strobe);
            _ttlTagLabel.color = new Color32(255, 51, 102, 255);
            _ttlNumMain.color = Color.Lerp(new Color32(255, 51, 102, 255), Color.white, strobe * 0.5f);

            float glitchX = Mathf.Sin(Time.unscaledTime * 42f) > 0.82f ? 2f : 0f;
            _vitalsRigRt.anchoredPosition = new Vector2(VitalsBaseX + glitchX, VitalsBaseY);
        }
        else if (isWarn)
        {
            _ttlHousingImg.sprite = _housingWarnSpr;
            _crestBackingImg.sprite = _crestBackWarnSpr;
            _ttlTopRailImg.color = TacticalHUDGraphics.ColAmber;
            _ttlTagLabel.color = TacticalHUDGraphics.ColAmber;
            _ttlNumMain.color = new Color32(254, 240, 138, 255);
            _vitalsRigRt.anchoredPosition = new Vector2(VitalsBaseX, VitalsBaseY);
        }
        else
        {
            _ttlHousingImg.sprite = _housingOptimalSpr;
            _crestBackingImg.sprite = _crestBackOptimalSpr;
            _ttlTopRailImg.color = TacticalHUDGraphics.ColNeonMint;
            _ttlTagLabel.color = TacticalHUDGraphics.ColNeonMint;
            _ttlNumMain.color = Color.white;
            _vitalsRigRt.anchoredPosition = new Vector2(VitalsBaseX, VitalsBaseY);
        }

        // 12 Skewed Cells + Clamp Teeth
        for (int i = 0; i < TtlCellCount; i++)
        {
            if (i < activeCells)
            {
                if (isCrit) _ttlCells[i].sprite = _cellCrimsonSpr;
                else if (isWarn) _ttlCells[i].sprite = _cellAmberSpr;
                else _ttlCells[i].sprite = i < 2 ? _cellCyanSpr : _cellMintSpr;
            }
            else if (i < ghostCells)
            {
                _ttlCells[i].sprite = _cellGhostSpr;
            }
            else
            {
                _ttlCells[i].sprite = _cellEmptySpr;
            }
        }

        int litTeeth = Mathf.CeilToInt(ttlFrac * 6f);
        for (int i = 0; i < 6; i++)
        {
            bool lit = i < litTeeth;
            _topClampTeeth[i].sprite = lit ? _toothLitTopSpr : _toothDimTopSpr;
            _botClampTeeth[i].sprite = lit ? _toothLitBotSpr : _toothDimBotSpr;
        }

        // Numeric Readout ("TTL" stays untouched on _ttlTagLabel)
        string numStr = Mathf.CeilToInt(curTtl).ToString(CultureInfo.InvariantCulture);
        _ttlNumShadow.text = numStr;
        _ttlNumMain.text = numStr;
        _ttlUnitLabel.text = SafeZone.IsPlayerSafe
            ? $"/{Mathf.CeilToInt(maxTtl)}s · SAFE"
            : $"/{Mathf.CeilToInt(maxTtl)}s";
        _ttlUnitLabel.color = SafeZone.IsPlayerSafe
            ? (Color)TacticalHUDGraphics.ColNeonMint
            : new Color32(148, 163, 184, 255);

        // 3-Pip Stamina / Vector Dash Capacitors
        float stamFrac = Mathf.Clamp01(_stamina.Current / Mathf.Max(1f, _stamina.Capacity));
        for (int i = 0; i < 3; i++)
        {
            float pipFrac = Mathf.Clamp01((stamFrac - i / 3f) * 3f);
            _dashPipFills[i].fillAmount = pipFrac;
            _dashPipFills[i].sprite = pipFrac >= 0.99f ? _dashPipReadySpr : _dashPipChargeSpr;
        }
        _staminaTagLabel.text = $"STAMINA {Mathf.RoundToInt(stamFrac * 100f)}%";
        _staminaTagLabel.color = (_player.isClinging || _stamina.isClinging)
            ? (Color)TacticalHUDGraphics.ColAmber
            : new Color32(125, 211, 252, 255);

        // Packet Integrity Seal & Grade Stamp
        var quests = QuestService.Instance;
        var pim = PacketIntegrityManager.Instance;
        int worldIdx = gm != null ? gm.currentWorldIndex : 0;

        if (quests != null && pim != null)
        {
            PackageState pkgState = quests.GetPackageState(worldIdx);
            if (pkgState == PackageState.Active)
            {
                int pct = Mathf.CeilToInt(pim.currentIntegrity);
                DeliveryGrade grade = PacketIntegrityManager.CalculateGrade(pim.currentIntegrity);
                _integrityPctText.text = $"{pct}%";
                ApplyGradeVisual(grade);
            }
            else if (quests.IsPackageDelivered(worldIdx))
            {
                _integrityPctText.text = "SENT";
                _gradeStampImg.sprite = _gradeSSpr;
                _gradeStampText.text = "DONE";
                _gradeStampText.color = TacticalHUDGraphics.ColNeonMint;
            }
            else
            {
                _integrityPctText.text = "SEAL";
                _gradeStampImg.sprite = _gradeASpr;
                _gradeStampText.text = "PICKUP";
                _gradeStampText.color = TacticalHUDGraphics.ColSkyDash;
            }
        }

        // Mode / Side-Quest Micro-Chip (only shown when relevant so World 1 stays ultra-clean)
        var opt = OptionalQuestService.Get(worldIdx, 0);
        if (opt != null && !opt.Claimed && opt.CanClaim)
        {
            if (!_modeChipGo.activeSelf) _modeChipGo.SetActive(true);
            _modeChipText.text = $"+{opt.BonusBits} CLAIM!";
            _modeChipText.color = TacticalHUDGraphics.ColGold;
        }
        else if (opt != null && !opt.Claimed && opt.Progress > 0)
        {
            if (!_modeChipGo.activeSelf) _modeChipGo.SetActive(true);
            _modeChipText.text = $"QST · {opt.Progress}/{opt.Required}";
            _modeChipText.color = (Color)TacticalHUDGraphics.ColSkyDash;
        }
        else if (gm != null && gm.currentWorldIndex > 0 && _playerMode != null)
        {
            if (!_modeChipGo.activeSelf) _modeChipGo.SetActive(true);
            if (_modeHintText != null && !_modeHintText.gameObject.activeSelf) _modeHintText.gameObject.SetActive(true);
            _modeChipText.text = _playerMode.isEncrypted
                ? $"ENC · P{_playerMode.currentPort}"
                : $"PLN · P{_playerMode.currentPort}";
            _modeChipText.color = _playerMode.isEncrypted
                ? (Color)TacticalHUDGraphics.ColNeonMint
                : (Color)TacticalHUDGraphics.ColSkyDash;
        }
        else
        {
            if (_modeChipGo != null && _modeChipGo.activeSelf) _modeChipGo.SetActive(false);
            if (_modeHintText != null && _modeHintText.gameObject.activeSelf) _modeHintText.gameObject.SetActive(false);
        }

        // Persona 3 Reload Mandatory & Optional Quest Tracker Card update
        if (_questHeaderText != null && _questPrimaryText != null && _questOptionalText != null)
        {
            PackageState pkgState = PackageState.None;
            if (quests != null)
            {
                pkgState = quests.GetPackageState(worldIdx);
            }
            else if (gm != null && gm.packageStates != null && worldIdx >= 0 && worldIdx < gm.packageStates.Length)
            {
                pkgState = gm.packageStates[worldIdx];
            }

            if (pkgState >= PackageState.Delivered)
            {
                _questHeaderText.text = "MISI WAJIB [SELESAI]  //  PORTAL SERVER B TERBUKA";
                _questHeaderText.color = TacticalHUDGraphics.ColNeonMint;
                _questPrimaryText.text = "✔ PAKET TERKIRIM — MASUK PORTAL SERVER B!";
                _questPrimaryText.color = TacticalHUDGraphics.ColNeonMint;
                if (_questLeftBarImg != null) _questLeftBarImg.color = TacticalHUDGraphics.ColNeonMint;
            }
            else if (pkgState == PackageState.Active)
            {
                _questHeaderText.text = "MISI WAJIB [2/2]  //  IKUTI PANAH KECIL KE PENERIMA";
                _questHeaderText.color = TacticalHUDGraphics.ColElectricCyan;
                _questPrimaryText.text = "► SERAHKAN PAKET KE NPC PENERIMA [E] >>";
                _questPrimaryText.color = Color.white;
                if (_questLeftBarImg != null) _questLeftBarImg.color = TacticalHUDGraphics.ColElectricCyan;
            }
            else
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
                Color warnCol = Color.Lerp(TacticalHUDGraphics.ColElectricCyan, TacticalHUDGraphics.ColGold, pulse);
                _questHeaderText.text = "MISI WAJIB [1/2]  //  IKUTI PANAH KE NPC PAKET";
                _questHeaderText.color = warnCol;
                _questPrimaryText.text = "► AMBIL PAKET DATA DI NPC KURIR [E]";
                _questPrimaryText.color = Color.white;
                if (_questLeftBarImg != null) _questLeftBarImg.color = warnCol;
            }

            if (opt != null)
            {
                if (opt.Claimed)
                {
                    _questOptionalText.text = $"✔ BOUNTY SELESAI: {opt.ProgressLabel}";
                    _questOptionalText.color = TacticalHUDGraphics.ColNeonMint;
                }
                else if (opt.CanClaim)
                {
                    _questOptionalText.text = $"★ BOUNTY SIAP KLAIM [+{opt.BonusBits} DB]: {opt.Label}";
                    _questOptionalText.color = TacticalHUDGraphics.ColGold;
                }
                else
                {
                    _questOptionalText.text = $"◆ MISI OPSIONAL: {opt.ProgressLabel} [+{opt.BonusBits} DB]";
                    _questOptionalText.color = new Color32(148, 210, 250, 245);
                }
            }
            else
            {
                _questOptionalText.text = "◆ MISI OPSIONAL: Bicara ke NPC Bounty di Safe Zone";
                _questOptionalText.color = new Color32(148, 178, 212, 220);
            }
        }
    }

    private void ApplyGradeVisual(DeliveryGrade grade)
    {
        switch (grade)
        {
            case DeliveryGrade.S:
                _gradeStampImg.sprite = _gradeSSpr;
                _gradeStampText.text = "S-RANK";
                _gradeStampText.color = TacticalHUDGraphics.ColGold;
                break;
            case DeliveryGrade.A:
                _gradeStampImg.sprite = _gradeASpr;
                _gradeStampText.text = "A-RANK";
                _gradeStampText.color = TacticalHUDGraphics.ColElectricCyan;
                break;
            case DeliveryGrade.B:
                _gradeStampImg.sprite = _gradeBSpr;
                _gradeStampText.text = "B-RANK";
                _gradeStampText.color = TacticalHUDGraphics.ColAmber;
                break;
            default:
                _gradeStampImg.sprite = _gradeCSpr;
                _gradeStampText.text = "C-RANK";
                _gradeStampText.color = TacticalHUDGraphics.ColCrimson;
                break;
        }
    }

    private void ScanStageLandmarks(bool forceOriginCapture)
    {
        _cachedGoal = FindAnyObjectByType<ServerB_Goal>();
        _checkpoints = FindObjectsByType<Checkpoint_TCP>();
        _quizBoards = FindObjectsByType<SoftwareQuizBoard>();
        _secretCores = FindObjectsByType<SecretMemoryCore>();
        _cachedHermit = FindAnyObjectByType<HiddenHermitNPC>();

        if (_player == null) _player = FindAnyObjectByType<PlayerController>();

        if (!_routeOriginCaptured || forceOriginCapture)
        {
            var safeZones = FindObjectsByType<SafeZone>();
            float minSafeX = float.MaxValue;
            foreach (var sz in safeZones)
            {
                if (sz != null && sz.transform.position.x < minSafeX)
                    minSafeX = sz.transform.position.x;
            }

            if (minSafeX < float.MaxValue)
                _routeStartX = minSafeX;
            else if (_player != null)
                _routeStartX = _player.transform.position.x;

            _routeOriginCaptured = true;
        }

        if (_cachedGoal != null)
        {
            _routeEndX = Mathf.Max(_routeStartX + 20f, _cachedGoal.transform.position.x);
        }
    }

    private void UpdateRouteTrackAndWaypoints()
    {
        Vector2 pPos = _player.transform.position;
        float span = Mathf.Max(20f, _routeEndX - _routeStartX);
        float routeFrac = Mathf.Clamp01((pPos.x - _routeStartX) / span);

        // Traversed fill & live courier diamond slider
        _routeFillRt.sizeDelta = new Vector2(Mathf.Max(2f, RouteTrackPixelWidth * routeFrac), 6f);
        float sliderCenterX = 42f + RouteTrackPixelWidth * routeFrac;
        _courierSliderRt.anchoredPosition = new Vector2(sliderCenterX - 14f, -8f);

        for (int i = 0; i < 5; i++)
        {
            float toothFrac = (i + 0.5f) / 5f;
            _routeClampTeeth[i].sprite = routeFrac >= toothFrac ? _toothLitTopSpr : _toothDimTopSpr;
        }

        // 1) Nearest Unsolved Quiz Board Pip
        SoftwareQuizBoard nearestQuiz = null;
        float nearestQuizDist = float.MaxValue;
        if (_quizBoards != null)
        {
            for (int i = 0; i < _quizBoards.Length; i++)
            {
                var qb = _quizBoards[i];
                if (qb == null || !qb.gameObject.activeInHierarchy || qb.isSolved) continue;
                float d = Vector2.Distance(pPos, qb.transform.position);
                if (d < nearestQuizDist)
                {
                    nearestQuizDist = d;
                    nearestQuiz = qb;
                }
            }
        }

        if (nearestQuiz != null && nearestQuizDist <= 95f)
        {
            if (!_quizPipGo.activeSelf) _quizPipGo.SetActive(true);
            float qFrac = Mathf.Clamp01((nearestQuiz.transform.position.x - _routeStartX) / span);
            _quizPipRt.anchoredPosition = new Vector2(42f + RouteTrackPixelWidth * qFrac - 5f, -17f);
            _quizPipText.text = $"? {Mathf.RoundToInt(nearestQuizDist)}m";
        }
        else if (_quizPipGo.activeSelf)
        {
            _quizPipGo.SetActive(false);
        }

        // 2) Secret Memory Core / Hermit Sonar Pip (within 52m)
        Transform secretTarget = null;
        float secretDist = float.MaxValue;
        if (_secretCores != null)
        {
            for (int i = 0; i < _secretCores.Length; i++)
            {
                var sc = _secretCores[i];
                if (sc == null || !sc.gameObject.activeInHierarchy) continue;
                float d = Vector2.Distance(pPos, sc.transform.position);
                if (d < secretDist)
                {
                    secretDist = d;
                    secretTarget = sc.transform;
                }
            }
        }
        if (secretTarget == null && _cachedHermit != null && _cachedHermit.gameObject.activeInHierarchy)
        {
            float d = Vector2.Distance(pPos, _cachedHermit.transform.position);
            if (d < secretDist)
            {
                secretDist = d;
                secretTarget = _cachedHermit.transform;
            }
        }

        if (secretTarget != null && secretDist <= 52f)
        {
            if (!_secretPipGo.activeSelf) _secretPipGo.SetActive(true);
            float sFrac = Mathf.Clamp01((secretTarget.position.x - _routeStartX) / span);
            _secretPipRt.anchoredPosition = new Vector2(42f + RouteTrackPixelWidth * sFrac - 5f, -17f);
            float dy = secretTarget.position.y - pPos.y;
            string vCue = dy > 2.5f ? "▲" : (dy < -2.5f ? "▼" : "");
            _secretPipText.text = $"{Mathf.RoundToInt(secretDist)}m{vCue}";
        }
        else if (_secretPipGo.activeSelf)
        {
            _secretPipGo.SetActive(false);
        }

        // 3) Next Unactivated TCP Checkpoint Pip
        Checkpoint_TCP nextCp = null;
        float minCpDist = float.MaxValue;
        if (_checkpoints != null)
        {
            for (int i = 0; i < _checkpoints.Length; i++)
            {
                var cp = _checkpoints[i];
                if (cp == null || !cp.gameObject.activeInHierarchy || cp.isActivated) continue;
                float dx = cp.transform.position.x - pPos.x;
                if (dx >= -4f)
                {
                    float d = Vector2.Distance(pPos, cp.transform.position);
                    if (d < minCpDist)
                    {
                        minCpDist = d;
                        nextCp = cp;
                    }
                }
            }
        }

        if (nextCp != null)
        {
            if (!_tcpPipGo.activeSelf) _tcpPipGo.SetActive(true);
            float cFrac = Mathf.Clamp01((nextCp.transform.position.x - _routeStartX) / span);
            _tcpPipRt.anchoredPosition = new Vector2(42f + RouteTrackPixelWidth * cFrac - 5f, -17f);
            _tcpPipText.text = $"SYNC {Mathf.RoundToInt(minCpDist)}m";
        }
        else if (_tcpPipGo.activeSelf)
        {
            _tcpPipGo.SetActive(false);
        }

        // 4) Destination Server B Extraction Tag
        if (_cachedGoal != null)
        {
            float goalDist = Vector2.Distance(pPos, _cachedGoal.transform.position);
            if (_cachedGoal.GateLocked)
            {
                _destTagText.text = $"LOCKED {Mathf.RoundToInt(goalDist)}m";
                _destTagText.color = TacticalHUDGraphics.ColAmber;
                _destChevronImg.color = TacticalHUDGraphics.ColAmber;
            }
            else if (goalDist <= 8f)
            {
                _destTagText.text = "EXIT OPEN";
                _destTagText.color = TacticalHUDGraphics.ColNeonMint;
                _destChevronImg.color = TacticalHUDGraphics.ColNeonMint;
            }
            else
            {
                _destTagText.text = $"SRV-B {Mathf.RoundToInt(goalDist)}m";
                _destTagText.color = TacticalHUDGraphics.ColNeonMint;
                _destChevronImg.color = TacticalHUDGraphics.ColNeonMint;
            }
        }
        else
        {
            _destTagText.text = "SECTOR";
        }
    }

    private void UpdateBossBar()
    {
        var boss = BossTracker.ActiveBoss;
        if (boss != null && !boss.IsDead)
        {
            if (!_bossContainer.gameObject.activeSelf)
            {
                _bossContainer.gameObject.SetActive(true);
                _bossGhostPct = boss.HealthPercent;
            }
            float pct = Mathf.Clamp01(boss.HealthPercent);
            if (pct > _bossGhostPct) _bossGhostPct = pct;
            else _bossGhostPct = Mathf.MoveTowards(_bossGhostPct, pct, 0.35f * Time.deltaTime);

            string title = $"{boss.BossName}  [{boss.HitPoints}/{boss.MaxHitPoints}]";
            var fillImg = _bossFillRt != null ? _bossFillRt.GetComponent<Image>() : null;
            if (boss is Boss_Grounded gb)
            {
                int rem = Mathf.Clamp(gb.TotalPhases - gb.CurrentPhase + 1, 1, gb.TotalPhases);
                string pips = new string('◆', rem) + new string('◇', Mathf.Max(0, gb.TotalPhases - rem));
                title = $"{gb.BossName}  {pips}  [{gb.HitPoints}/{gb.MaxHitPoints}]";
                if (fillImg != null)
                {
                    fillImg.color = gb.CurrentPhase == 1
                        ? (Color)TacticalHUDGraphics.ColElectricCyan
                        : (gb.CurrentPhase == 2 ? (Color)TacticalHUDGraphics.ColAmber : (Color)TacticalHUDGraphics.ColCrimson);
                }
            }
            else if (fillImg != null)
            {
                fillImg.color = TacticalHUDGraphics.ColCrimson;
            }

            _bossTitleText.text = title;
            _bossFillRt.sizeDelta = new Vector2(460f * pct, 10f);
            _bossGhostRt.sizeDelta = new Vector2(460f * Mathf.Max(pct, _bossGhostPct), 10f);
        }
        else if (Boss_Rootkit.Instance != null && !Boss_Rootkit.Instance.IsDead)
        {
            var rk = Boss_Rootkit.Instance;
            if (!_bossContainer.gameObject.activeSelf)
            {
                _bossContainer.gameObject.SetActive(true);
                _bossGhostPct = rk.HealthPercent;
            }
            float pct = Mathf.Clamp01(rk.HealthPercent);
            _bossGhostPct = Mathf.MoveTowards(_bossGhostPct, pct, 0.35f * Time.deltaTime);
            _bossTitleText.text = $"{rk.bossName}  [{rk.hitPoints}/{rk.maxHitPoints}]";
            _bossFillRt.sizeDelta = new Vector2(460f * pct, 10f);
            _bossGhostRt.sizeDelta = new Vector2(460f * Mathf.Max(pct, _bossGhostPct), 10f);
        }
        else if (_bossContainer.gameObject.activeSelf)
        {
            _bossContainer.gameObject.SetActive(false);
        }
    }

    private void UpdateTopRightChronoAndPurse(GameManager gm)
    {
        int wIdx = gm != null ? gm.currentWorldIndex : 0;
        int sIdx = gm != null ? gm.currentSegmentIndex : 0;
        _sectorStampText.text = TutorialWorldBuilder.IsTutorialWorldActive
            ? "W0-1"
            : $"W{wIdx + 1}-{sIdx + 1}";

        float runSec = gm != null ? gm.runTime : Time.timeSinceLevelLoad;
        int mins = Mathf.FloorToInt(runSec / 60f);
        int secs = Mathf.FloorToInt(runSec % 60f);
        int centis = Mathf.FloorToInt((runSec * 100f) % 100f);
        _chronoMainText.text = $"{mins:00}:{secs:00}";
        _chronoMillisText.text = $".{centis:00}";

        int totalBits = gm != null ? (gm.totalDataBits + gm.dataBitsCollectedThisRun) : 0;
        if (_lastTotalBits < 0)
        {
            _lastTotalBits = totalBits;
        }
        else if (totalBits > _lastTotalBits)
        {
            _accumulatedBitDelta += (totalBits - _lastTotalBits);
            _bitDeltaTimer = 2.2f;
            _lastTotalBits = totalBits;
        }
        else if (totalBits < _lastTotalBits)
        {
            _lastTotalBits = totalBits;
        }

        _purseTotalText.text = totalBits.ToString("N0", CultureInfo.InvariantCulture);

        if (_bitDeltaTimer > 0f)
        {
            _bitDeltaTimer -= Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(_bitDeltaTimer / 0.5f);
            _purseDeltaText.text = $"+{_accumulatedBitDelta}";
            Color c = TacticalHUDGraphics.ColNeonMint;
            c.a = alpha;
            _purseDeltaText.color = c;
            if (_bitDeltaTimer <= 0f) _accumulatedBitDelta = 0;
        }
        else if (!string.IsNullOrEmpty(_purseDeltaText.text))
        {
            _purseDeltaText.text = "";
        }

        if (_splitNoticeTimer > 0f)
        {
            _splitNoticeTimer -= Time.unscaledDeltaTime;
            if (_splitNoticeTimer <= 0f) _splitNoticeText.text = "";
        }
    }

    public void ShowSplitDelta(string checkpointLabel, float deltaSeconds)
    {
        if (_splitNoticeText == null) return;
        _splitNoticeTimer = 3.2f;
        if (deltaSeconds <= 0f)
        {
            _splitNoticeText.text = $"{checkpointLabel} {deltaSeconds:F2}s";
            _splitNoticeText.color = TacticalHUDGraphics.ColNeonMint;
        }
        else
        {
            _splitNoticeText.text = $"{checkpointLabel} +{deltaSeconds:F2}s";
            _splitNoticeText.color = TacticalHUDGraphics.ColCrimson;
        }
    }

    private void UpdateBottomLeftSockets()
    {
        // --- SOCKET 1: VECTOR / PHASE DASH [SHIFT] ---
        float dashCd = Mathf.Max(0f, _player.dashCooldownTimer);
        float dashCost = 25f * OverclockManager.StaminaCostMult;
        bool hasPhase = OverclockManager.HasPhaseDash;
        bool stamLow = _stamina.Current < dashCost;

        if (dashCd > 0.02f)
        {
            _sock1FrameImg.sprite = _sockWarnAmberSpr;
            _sock1IconImg.color = new Color(1f, 1f, 1f, 0.38f);
            float cdFrac = Mathf.Clamp01(dashCd / 0.65f);
            float veilH = 52f * cdFrac;
            _sock1VeilRt.sizeDelta = new Vector2(52f, veilH);
            _sock1SweepLineRt.gameObject.SetActive(true);
            _sock1SweepLineRt.anchoredPosition = new Vector2(3f, -11f - veilH);
            _sock1TimerText.text = dashCd.ToString("0.0", CultureInfo.InvariantCulture);
        }
        else if (stamLow || _player.airDashesLeft == 0)
        {
            _sock1FrameImg.sprite = _sockDepletedSpr;
            _sock1IconImg.color = new Color(1f, 1f, 1f, 0.30f);
            _sock1VeilRt.sizeDelta = new Vector2(52f, 0f);
            _sock1SweepLineRt.gameObject.SetActive(false);
            _sock1TimerText.text = "";
        }
        else
        {
            _sock1FrameImg.sprite = hasPhase ? _sockPassiveMagSpr : _sockReadyCyanSpr;
            _sock1IconImg.color = Color.white;
            _sock1VeilRt.sizeDelta = new Vector2(52f, 0f);
            _sock1SweepLineRt.gameObject.SetActive(false);
            _sock1TimerText.text = "";
        }

        // --- SOCKET 2: SECONDARY OVERCLOCK (NANO PARRY [Q] OR CYBER BLASTER [F]) ---
        bool hasParry = OverclockManager.HasParry;
        bool hasBlaster = OverclockManager.HasBlaster;

        if (hasParry)
        {
            Sprite parrySpr = TacticalHUDGraphics.LoadSprite("GeneratedArt/shield_parry") ?? PixelUIKit.GetIcon("shield");
            _sock2IconImg.sprite = parrySpr;
            _sock2KeyText.text = "Q";

            if (_combat != null && _combat.IsParrying)
            {
                _sock2FrameImg.sprite = _sockReadyMintSpr;
                _sock2IconImg.color = Color.white;
                _sock2VeilRt.sizeDelta = new Vector2(52f, 0f);
                _sock2SweepLineRt.gameObject.SetActive(false);
                _sock2TimerText.text = "ON";
                _sock2TimerText.color = TacticalHUDGraphics.ColNeonMint;
            }
            else
            {
                _sock2FrameImg.sprite = _sockReadyCyanSpr;
                _sock2IconImg.color = Color.white;
                _sock2VeilRt.sizeDelta = new Vector2(52f, 0f);
                _sock2SweepLineRt.gameObject.SetActive(false);
                _sock2TimerText.text = "";
            }
        }
        else if (hasBlaster)
        {
            _sock2IconImg.sprite = PixelUIKit.GetIcon("bolt");
            _sock2KeyText.text = "F";
            float shootCd = _combat != null ? _combat.ShootCooldownRemaining : 0f;
            if (shootCd > 0.01f)
            {
                _sock2FrameImg.sprite = _sockWarnAmberSpr;
                _sock2IconImg.color = new Color(1f, 1f, 1f, 0.38f);
                float veilH = 52f * Mathf.Clamp01(shootCd / PlayerCombat.ShootCooldownDuration);
                _sock2VeilRt.sizeDelta = new Vector2(52f, veilH);
                _sock2SweepLineRt.gameObject.SetActive(true);
                _sock2SweepLineRt.anchoredPosition = new Vector2(73f, -11f - veilH);
                _sock2TimerText.text = shootCd.ToString("0.0", CultureInfo.InvariantCulture);
                _sock2TimerText.color = TacticalHUDGraphics.ColGold;
            }
            else
            {
                _sock2FrameImg.sprite = _sockReadyCyanSpr;
                _sock2IconImg.color = Color.white;
                _sock2VeilRt.sizeDelta = new Vector2(52f, 0f);
                _sock2SweepLineRt.gameObject.SetActive(false);
                _sock2TimerText.text = "";
            }
        }
        else
        {
            _sock2FrameImg.sprite = _sockDepletedSpr;
            _sock2IconImg.color = new Color(1f, 1f, 1f, 0.20f);
            _sock2VeilRt.sizeDelta = new Vector2(52f, 0f);
            _sock2SweepLineRt.gameObject.SetActive(false);
            _sock2TimerText.text = "";
            _sock2KeyText.text = "Q";
        }

        // --- SOCKET 3: PASSIVE RELIC (EMERGENCY BUFFER / MAGNET / BATTERY) ---
        bool hasPassive = OverclockManager.IsBufferAvailableThisSegment ||
                          OverclockManager.MagnetLevel > 0 ||
                          OverclockManager.BatteryLevel > 0 ||
                          OverclockManager.HasTripleJump;
        _sock3FrameImg.sprite = hasPassive ? _sockSmallPassiveSpr : _sockSmallDepletedSpr;
        _sock3IconImg.color = hasPassive ? Color.white : new Color(1f, 1f, 1f, 0.22f);
        _sock3KeyText.color = hasPassive
            ? new Color32(240, 171, 252, 255)
            : new Color32(100, 116, 139, 255);
    }

    // =========================================================================
    // LOW-LEVEL PIXEL-CRISP UI BUILDERS
    // =========================================================================
    private static RectTransform CreateRect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = oMin;
        rt.offsetMax = oMax;
        return rt;
    }

    private static RectTransform CreateAnchoredBox(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    private static RectTransform CreateTopLeftBox(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        return CreateAnchoredBox(name, parent, Vector2.up, Vector2.up, pos, size);
    }

    private static Image AddSolidImage(RectTransform rt, Color color)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = TacticalHUDGraphics.GetWhitePixel();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static Image PlaceSprite(string name, Transform parent, Vector2 pos, Vector2 size, Sprite sprite)
    {
        var rt = CreateTopLeftBox(name, parent, pos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite != null ? sprite : TacticalHUDGraphics.GetWhitePixel();
        img.color = Color.white;
        img.raycastTarget = false;
        return img;
    }

    private static Text PlaceText(
        string name,
        Transform parent,
        string content,
        Vector2 pos,
        Vector2 size,
        int fontSize,
        Color color,
        TextAnchor align,
        bool mono)
    {
        var rt = CreateTopLeftBox(name, parent, pos, size);
        var txt = rt.gameObject.AddComponent<Text>();
        txt.text = content;
        txt.font = mono ? PixelUIKit.GetMonoFont() : PixelUIKit.GetDisplayFont();
        txt.fontSize = fontSize;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = align;
        txt.color = color;
        txt.raycastTarget = false;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        return txt;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Persona 3 Reload (P3R) inspired 3D-Layered Kinetic Cyberpunk Main Menu for Packet Courier.
/// Fixes:
/// 1. Zero RectMask2D on rotated RectTransforms (eliminates Unity's axis-aligned diagonal text clipping).
/// 2. Zero GameObject.Find inside OnDisable/OnDestroy (eliminates 'go.IsActive()' assertion error).
/// 3. Custom procedural 3D-beveled skewed parallelogram blade sprites with slanted ends, 6px 3D bottom lip,
///    diagonal halftone cyber texture, protruding 3D number/icon crest, and multi-layer offset shadow plates.
/// </summary>
[ExecuteAlways]
public class MenuUI : MonoBehaviour
{
    private class KineticBladeItem
    {
        public int Index;
        public RectTransform RootRt;
        public RectTransform DeepShadowRt;
        public RectTransform CyanWingRt;
        public Image CyanWingImg;
        public Image MainPlateImg;
        public Image CrestBadgeImg;
        public Image CrestIconImg;
        public Text IndexText;
        public Text TitleShadowText;
        public Text TitleText;
        public Text SubText;
        public Text KeyBadgeText;
        public Image KeyBadgeBg;
        public Vector2 BaseAnchorMin;
        public Vector2 BaseAnchorMax;
        public float CurrentSlide;
        public float CurrentScale = 1f;
        public string Description;
        public Action OnActivate;
        public bool IsDanger;
    }

    // Persona 3 Reload Signature Palette
    private static readonly Color ColAbyssNavy = new Color(0.016f, 0.031f, 0.078f, 1f);     // #040814
    private static readonly Color ColDeepCobalt = new Color(0.047f, 0.110f, 0.235f, 1f);    // #0C1C3C
    private static readonly Color ColRoyalBlue = new Color(0.067f, 0.200f, 0.435f, 1f);     // #11336F
    private static readonly Color ColP3Cyan = new Color(0.0f, 0.918f, 1.0f, 1f);            // #00EAFF
    private static readonly Color ColP3Aqua = new Color(0.22f, 0.98f, 0.86f, 1f);           // #38FADB
    private static readonly Color ColStarkWhite = new Color(0.97f, 0.99f, 1.0f, 1f);        // #F8FCFF
    private static readonly Color ColCrimsonAlert = new Color(1.0f, 0.14f, 0.34f, 1f);      // #FF2457

    private Canvas _canvas;
    private GameObject _mainPanel;
    private GameObject _worldSelectPanel;
    private GameObject _settingsPanel;
    private int _defaultWorldIndex;
    private int _selectedBladeIndex;

    // Animated P3R Backdrop & Hero Elements
    private RectTransform _cyanSlashMainRt;
    private RectTransform _compassRingRt;
    private RectTransform _heroCyanShadowRt;
    private RectTransform _heroBlackShadowRt;
    private RectTransform _heroMainRt;
    private RectTransform _activeCursorArrowRt;
    private Text _contextRibbonText;
    private Text _sectorPreviewTitleText;
    private Text _sectorPreviewMetaText;

    private readonly List<KineticBladeItem> _blades = new List<KineticBladeItem>();
    private readonly List<RectTransform> _floatingMotes = new List<RectTransform>();
    private readonly List<float> _moteSpeeds = new List<float>();
    private readonly List<float> _motePhase = new List<float>();

    // Cached Procedural 3D Parallelogram Blade Sprites
    private static Sprite _cachedRingWatermark;
    private static Sprite _bladeIdleSpr;
    private static Sprite _bladeSelectedWhiteSpr;
    private static Sprite _bladeDangerIdleSpr;
    private static Sprite _bladeDangerSelectedSpr;
    private static Sprite _bladeWingCyanSpr;
    private static Sprite _bladeWingCrimsonSpr;
    private static Sprite _bladeDeepShadowSpr;
    private static Sprite _crestCyanSpr;
    private static Sprite _crestCrimsonSpr;
    private static Sprite _crestNavySpr;

    private void Awake()
    {
        RebuildUI();
    }

    private void OnEnable()
    {
        if (_canvas == null)
            RebuildUI();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
        {
            DestroyDirectCanvasOnly();
        }
    }

    private void OnDestroy()
    {
        DestroyDirectCanvasOnly();
    }

    private void Start()
    {
        if (Application.isPlaying)
        {
            AudioManager.PlayStageOpeningBGM(AudioManager.BGMTrack.Menu, 0.65f);
        }
    }

    /// <summary>
    /// Safely destroys only the direct _canvas reference without calling GameObject.Find,
    /// avoiding Unity's 'go.IsActive()' assertion during OnDisable / OnDestroy.
    /// </summary>
    private void DestroyDirectCanvasOnly()
    {
        if (_canvas != null)
        {
            var go = _canvas.gameObject;
            _canvas = null;
            if (go != null)
            {
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }
        }
    }

    private void CleanupStaleCanvasesForRebuild()
    {
        DestroyDirectCanvasOnly();

        // Only sweep active canvases during RebuildUI (never during OnDisable/OnDestroy)
        var allCanvases = FindObjectsByType<Canvas>();
        for (int i = 0; i < allCanvases.Length; i++)
        {
            var c = allCanvases[i];
            if (c != null && c.gameObject != null && c.gameObject.name == "Menu_Canvas")
            {
                if (Application.isPlaying)
                {
                    c.gameObject.name = "Menu_Canvas_Destroyed";
                    Destroy(c.gameObject);
                }
                else
                {
                    DestroyImmediate(c.gameObject);
                }
            }
        }
    }

    public void RebuildUI()
    {
        CleanupStaleCanvasesForRebuild();
        _blades.Clear();
        _floatingMotes.Clear();
        _moteSpeeds.Clear();
        _motePhase.Clear();

        EnsureBladeSpritesGenerated();
        EnsureActiveCamera();
        _canvas = CreateCanvas();

        BuildP3RBackdrop(_canvas.transform);
        BuildP3RTopHeader(_canvas.transform);
        BuildMainMenu();
        BuildWorldSelectPanel();
        BuildSettingsPanel();
        BuildP3RBottomRibbon(_canvas.transform);

        SelectBlade(0, playSound: false, instant: true);
        MarkDontSaveRecursively(_canvas.gameObject);
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        AnimateP3RScene();

        if (_worldSelectPanel != null && _worldSelectPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
                ClosePanels();
            return;
        }

        if (_settingsPanel != null && _settingsPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
                ClosePanels();
            return;
        }

        if (_blades.Count > 0)
        {
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                SelectBlade((_selectedBladeIndex + 1) % _blades.Count, playSound: true);
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                SelectBlade((_selectedBladeIndex - 1 + _blades.Count) % _blades.Count, playSound: true);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                CyclePreviewSector(-1);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                CyclePreviewSector(1);
            }
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
        {
            if (_selectedBladeIndex >= 0 && _selectedBladeIndex < _blades.Count)
            {
                AudioManager.PlayUIClick();
                _blades[_selectedBladeIndex].OnActivate?.Invoke();
            }
        }
        else if (Input.GetKeyDown(KeyCode.T))
        {
            AudioManager.PlayUIClick();
            LaunchTutorialOnboarding();
        }
        else if (Input.GetKeyDown(KeyCode.Tab))
        {
            AudioManager.PlayUIClick();
            OpenWorldSelect();
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            AudioManager.PlayUIClick();
            OpenSettings();
        }
    }

    private void SelectBlade(int index, bool playSound, bool instant = false)
    {
        if (_blades.Count == 0) return;
        int clamped = Mathf.Clamp(index, 0, _blades.Count - 1);
        bool changed = _selectedBladeIndex != clamped;
        _selectedBladeIndex = clamped;

        if (playSound && changed && Application.isPlaying)
        {
            AudioManager.PlayUIClick();
        }

        if (_contextRibbonText != null)
        {
            _contextRibbonText.text = _blades[_selectedBladeIndex].Description;
        }

        for (int i = 0; i < _blades.Count; i++)
        {
            ApplyBladeVisualState(_blades[i], i == _selectedBladeIndex, instant);
        }
    }

    private void ApplyBladeVisualState(KineticBladeItem b, bool selected, bool instant)
    {
        if (b == null || b.RootRt == null) return;
        EnsureBladeSpritesGenerated();

        if (selected)
        {
            // Persona 3 Reload Selected State: 3D Beveled Stark White Parallelogram + Electric Cyan Wing + Crimson 3D Crest
            b.MainPlateImg.sprite = b.IsDanger ? _bladeDangerSelectedSpr : _bladeSelectedWhiteSpr;
            b.CyanWingImg.sprite = b.IsDanger ? _bladeWingCrimsonSpr : _bladeWingCyanSpr;
            b.CyanWingImg.color = Color.white;
            b.CrestBadgeImg.sprite = b.IsDanger ? _crestNavySpr : _crestCrimsonSpr;
            b.CrestIconImg.color = ColStarkWhite;
            b.IndexText.color = ColStarkWhite;

            b.TitleText.color = b.IsDanger ? ColStarkWhite : ColAbyssNavy;
            b.TitleShadowText.color = b.IsDanger ? new Color(0f, 0f, 0f, 0.55f) : new Color(0f, 0.85f, 1f, 0.40f);
            b.SubText.color = b.IsDanger ? new Color(1f, 0.90f, 0.94f, 1f) : new Color(0.06f, 0.22f, 0.44f, 1f);

            if (b.KeyBadgeBg != null) b.KeyBadgeBg.sprite = _crestNavySpr;
            if (b.KeyBadgeText != null) b.KeyBadgeText.color = b.IsDanger ? ColCrimsonAlert : ColP3Cyan;
        }
        else
        {
            // Unselected State: 3D Beveled Obsidian-Cobalt Parallelogram + Cyan Rim + Raised Cyan 3D Crest
            b.MainPlateImg.sprite = b.IsDanger ? _bladeDangerIdleSpr : _bladeIdleSpr;
            b.CyanWingImg.sprite = b.IsDanger ? _bladeWingCrimsonSpr : _bladeWingCyanSpr;
            b.CyanWingImg.color = new Color(1f, 1f, 1f, 0.35f);
            b.CrestBadgeImg.sprite = b.IsDanger ? _crestCrimsonSpr : _crestCyanSpr;
            b.CrestIconImg.color = b.IsDanger ? ColStarkWhite : ColAbyssNavy;
            b.IndexText.color = b.IsDanger ? ColStarkWhite : ColAbyssNavy;

            b.TitleText.color = ColStarkWhite;
            b.TitleShadowText.color = new Color(0f, 0f, 0f, 0.85f);
            b.SubText.color = new Color(0.55f, 0.80f, 0.96f, 0.95f);

            if (b.KeyBadgeBg != null) b.KeyBadgeBg.sprite = _crestNavySpr;
            if (b.KeyBadgeText != null) b.KeyBadgeText.color = b.IsDanger ? ColCrimsonAlert : new Color(0.55f, 0.86f, 1f, 1f);
        }

        if (instant)
        {
            b.CurrentSlide = selected ? 0.024f : 0f;
            b.CurrentScale = selected ? 1.045f : 1f;
            b.RootRt.anchorMin = b.BaseAnchorMin + new Vector2(b.CurrentSlide, 0f);
            b.RootRt.anchorMax = b.BaseAnchorMax + new Vector2(b.CurrentSlide, 0f);
            b.RootRt.localScale = new Vector3(b.CurrentScale, b.CurrentScale, 1f);
        }
    }

    private void AnimateP3RScene()
    {
        float t = Time.unscaledTime;
        float dt = Time.unscaledDeltaTime;

        // 1. Animate 3D Kinetic Menu Blades
        for (int i = 0; i < _blades.Count; i++)
        {
            var b = _blades[i];
            if (b.RootRt == null) continue;
            bool isSel = (i == _selectedBladeIndex);
            float targetSlide = isSel ? 0.026f : 0f;
            float targetScale = isSel ? 1.045f : 1.0f;

            b.CurrentSlide = Mathf.Lerp(b.CurrentSlide, targetSlide, 18f * dt);
            b.CurrentScale = Mathf.Lerp(b.CurrentScale, targetScale, 18f * dt);

            b.RootRt.anchorMin = b.BaseAnchorMin + new Vector2(b.CurrentSlide, 0f);
            b.RootRt.anchorMax = b.BaseAnchorMax + new Vector2(b.CurrentSlide, 0f);
            b.RootRt.localScale = new Vector3(b.CurrentScale, b.CurrentScale, 1f);

            if (isSel)
            {
                if (b.CyanWingRt != null)
                {
                    float wx = 9f + 3.5f * Mathf.Sin(t * 7.5f);
                    float wy = -7f - 2.5f * Mathf.Cos(t * 7.5f);
                    b.CyanWingRt.anchoredPosition = new Vector2(wx, wy);
                }
                if (b.DeepShadowRt != null)
                {
                    b.DeepShadowRt.anchoredPosition = new Vector2(15f, -13f);
                }
            }
            else
            {
                if (b.CyanWingRt != null) b.CyanWingRt.anchoredPosition = new Vector2(4f, -4f);
                if (b.DeepShadowRt != null) b.DeepShadowRt.anchoredPosition = new Vector2(8f, -8f);
            }
        }

        // 2. Move P3R Targeting Diamond Cursor next to active blade
        if (_activeCursorArrowRt != null && _selectedBladeIndex >= 0 && _selectedBladeIndex < _blades.Count)
        {
            var sel = _blades[_selectedBladeIndex];
            float midY = (sel.BaseAnchorMin.y + sel.BaseAnchorMax.y) * 0.5f;
            float targetX = sel.BaseAnchorMin.x + sel.CurrentSlide - 0.024f + 0.004f * Mathf.Sin(t * 10f);
            Vector2 curMin = _activeCursorArrowRt.anchorMin;
            float newY = Mathf.Lerp(curMin.y, midY - 0.018f, 20f * dt);
            _activeCursorArrowRt.anchorMin = new Vector2(targetX, newY);
            _activeCursorArrowRt.anchorMax = new Vector2(targetX + 0.020f, newY + 0.036f);
        }

        // 3. Rotate Geometric Watermark Ring & Sway Diagonal Slash
        if (_compassRingRt != null)
        {
            _compassRingRt.Rotate(0f, 0f, -7.5f * dt);
        }

        if (_cyanSlashMainRt != null)
        {
            float sway = Mathf.Sin(t * 1.4f) * 6f;
            _cyanSlashMainRt.anchoredPosition = new Vector2(sway, 0f);
        }

        // 4. Parallax sway on Hero Character & Twin P3R Silhouettes
        if (_heroMainRt != null && _heroCyanShadowRt != null && _heroBlackShadowRt != null)
        {
            float bob = Mathf.Sin(t * 2.2f) * 4f;
            float cyanDrift = 14f + 4f * Mathf.Sin(t * 3.5f);
            _heroMainRt.anchoredPosition = new Vector2(0f, bob);
            _heroCyanShadowRt.anchoredPosition = new Vector2(cyanDrift, -8f - bob * 0.5f);
            _heroBlackShadowRt.anchoredPosition = new Vector2(30f, -18f + bob * 0.3f);
        }

        // 5. Float Aqua/Cyan Tartarus Light Motes upward
        for (int i = 0; i < _floatingMotes.Count; i++)
        {
            var mrt = _floatingMotes[i];
            if (mrt == null) continue;
            float spd = _moteSpeeds[i];
            float ph = _motePhase[i];
            Vector2 aMin = mrt.anchorMin;
            Vector2 aMax = mrt.anchorMax;
            float w = aMax.x - aMin.x;
            float h = aMax.y - aMin.y;

            aMin.y += spd * dt;
            aMin.x += Mathf.Sin(t * 2f + ph) * 0.012f * dt;
            if (aMin.y > 0.96f) aMin.y = 0.03f;
            aMax.x = aMin.x + w;
            aMax.y = aMin.y + h;
            mrt.anchorMin = aMin;
            mrt.anchorMax = aMax;
        }
    }

    private void EnsureActiveCamera()
    {
        Camera targetCam = Camera.main;
        if (targetCam == null) targetCam = FindAnyObjectByType<Camera>();
        if (targetCam == null)
        {
            var camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = ColAbyssNavy;
            camObj.transform.position = new Vector3(0, 0, -10);
            targetCam = cam;
        }
        else
        {
            targetCam.clearFlags = CameraClearFlags.SolidColor;
            targetCam.backgroundColor = ColAbyssNavy;
        }

        if (FindAnyObjectByType<AudioListener>() == null && targetCam != null)
        {
            if (targetCam.GetComponent<AudioListener>() == null)
                targetCam.gameObject.AddComponent<AudioListener>();
        }
    }

    // =========================================================================
    // 1. PERSONA 3 RELOAD DIAGONAL SLASH BACKDROP
    // =========================================================================
    private void BuildP3RBackdrop(Transform root)
    {
        var bg = CreateRectPanel(root, "P3R_AbyssBase", Vector2.zero, Vector2.one, ColAbyssNavy);
        bg.raycastTarget = false;

        var rightFlood = CreateRotatedSlash(root, "CobaltFloodSlash", new Vector2(0.36f, -0.25f), new Vector2(1.18f, 1.25f), -13f, ColDeepCobalt);
        rightFlood.raycastTarget = false;

        Sprite racks = Resources.Load<Sprite>("GeneratedArt/bg_w1_layer1");
        if (racks != null)
        {
            var rRight = CreateSpriteImage(root, "CitySilhouettes_Right", racks, new Vector2(0.38f, 0.04f), new Vector2(1.0f, 0.68f), new Color(0.0f, 0.72f, 1.0f, 0.19f));
            rRight.preserveAspect = false;
            var rLeft = CreateSpriteImage(root, "CitySilhouettes_Left", racks, new Vector2(0.0f, 0.04f), new Vector2(0.42f, 0.45f), new Color(0.0f, 0.55f, 0.90f, 0.10f));
            rLeft.preserveAspect = false;
        }

        var ringImg = CreateSpriteImage(root, "P3R_GeometricWatermark", GetWatermarkRingSprite(), new Vector2(0.50f, 0.18f), new Vector2(0.96f, 0.92f), new Color(0f, 0.90f, 1f, 0.11f));
        ringImg.preserveAspect = true;
        _compassRingRt = ringImg.rectTransform;

        var cyanBand = CreateRotatedSlash(root, "P3R_ElectricCyanSlash", new Vector2(0.46f, -0.30f), new Vector2(0.67f, 1.30f), -14f, new Color(0f, 0.88f, 1f, 0.86f));
        cyanBand.raycastTarget = false;
        _cyanSlashMainRt = cyanBand.rectTransform;

        var whiteRazor = CreateRotatedSlash(root, "P3R_StarkWhiteRazor", new Vector2(0.438f, -0.30f), new Vector2(0.452f, 1.30f), -14f, new Color(1f, 1f, 1f, 0.92f));
        whiteRazor.raycastTarget = false;

        CreateRotatedSlash(root, "P3R_CrimsonHairline", new Vector2(0.682f, -0.30f), new Vector2(0.689f, 1.30f), -14f, new Color(1f, 0.15f, 0.36f, 0.78f)).raycastTarget = false;
        CreateRotatedSlash(root, "P3R_LeftObsidianShard", new Vector2(-0.15f, -0.20f), new Vector2(0.435f, 1.25f), -14f, new Color(0.018f, 0.035f, 0.082f, 0.94f)).raycastTarget = false;

        for (int i = 0; i < 22; i++)
        {
            float px = 0.05f + (i * 0.043f) % 0.90f;
            float py = 0.05f + ((i * 29) % 85) * 0.01f;
            float sizeW = (i % 4 == 0) ? 0.007f : 0.004f;
            float sizeH = sizeW * 2.1f;
            Color mCol = (i % 3 == 0)
                ? new Color(1f, 1f, 1f, 0.38f)
                : ((i % 2 == 0) ? new Color(0f, 0.95f, 1f, 0.34f) : new Color(0.22f, 0.98f, 0.85f, 0.28f));

            var mote = CreateRotatedSlash(root, "P3RMote_" + i, new Vector2(px, py), new Vector2(px + sizeW, py + sizeH), 45f, mCol);
            mote.raycastTarget = false;
            _floatingMotes.Add(mote.rectTransform);
            _moteSpeeds.Add(0.03f + (i % 5) * 0.014f);
            _motePhase.Add(i * 0.9f);
        }
    }

    // =========================================================================
    // 2. FRAMELESS TOP-RIGHT DATA BITS PILL & BOTTOM KINETIC RIBBON
    // =========================================================================
    private void BuildP3RTopHeader(Transform root)
    {
        // True Persona 3 Reload frameless top edge: NO boxy horizontal bar or cutting cyan rule!
        // Only a sleek floating dark-obsidian DataBits telemetry badge in the top-right corner.
        var gm = Application.isPlaying ? GameManager.Ensure() : GameManager.Instance;
        int totalBits = gm != null ? gm.totalDataBits : 500;

        var badgeShadow = CreateRectPanel(root, "TopPurseShadow", new Vector2(0.753f, 0.903f), new Vector2(0.973f, 0.961f), new Color(0f, 0f, 0f, 0.48f));
        badgeShadow.raycastTarget = false;

        var purseCard = PixelUIKit.CreateSteppedPanel(
            root,
            "TopRightPurseBadge",
            new Vector2(0.748f, 0.908f),
            new Vector2(0.968f, 0.966f),
            new Color32(7, 16, 32, 238),
            new Color32(0, 232, 255, 255),
            new Color32(4, 10, 20, 255),
            3);
        purseCard.raycastTarget = false;

        var accentBar = CreateRectPanel(purseCard.transform, "CyanLeftEdge", new Vector2(0.015f, 0.14f), new Vector2(0.032f, 0.86f), ColP3Cyan);
        accentBar.raycastTarget = false;

        var iconImg = CreateSpriteImage(purseCard.transform, "BitsIcon", PixelUIKit.GetIcon("bolt"), new Vector2(0.055f, 0.22f), new Vector2(0.145f, 0.80f), ColP3Cyan);
        iconImg.preserveAspect = true;

        PixelUIKit.CreatePixelLabel(
            purseCard.transform,
            "PurseTag",
            "DATA BITS",
            new Vector2(0.16f, 0.12f),
            new Vector2(0.50f, 0.88f),
            12,
            new Color(0.60f, 0.84f, 0.98f, 0.95f),
            TextAnchor.MiddleLeft,
            true);

        PixelUIKit.CreatePixelLabel(
            purseCard.transform,
            "PurseAmount",
            $"{totalBits:N0} DB",
            new Vector2(0.48f, 0.10f),
            new Vector2(0.94f, 0.90f),
            16,
            ColStarkWhite,
            TextAnchor.MiddleRight,
            false);
    }

    private void BuildP3RBottomRibbon(Transform root)
    {
        var botBar = CreateRectPanel(root, "BottomRibbon", new Vector2(0f, 0f), new Vector2(1f, 0.065f), new Color(0.02f, 0.035f, 0.075f, 0.96f));
        botBar.raycastTarget = false;
        CreateRectPanel(botBar.transform, "TopWhiteRule", new Vector2(0f, 0.93f), Vector2.one, ColStarkWhite).raycastTarget = false;
        CreateRectPanel(botBar.transform, "TopCyanRule", new Vector2(0f, 0.85f), new Vector2(1f, 0.93f), ColP3Cyan).raycastTarget = false;

        var tagBox = CreateSpriteImage(botBar.transform, "PromptTag", _crestCrimsonSpr, new Vector2(0.025f, 0.12f), new Vector2(0.15f, 0.82f), Color.white);
        tagBox.type = Image.Type.Simple;
        PixelUIKit.CreatePixelLabel(tagBox.transform, "TagLbl", "INFO KOMANDO", new Vector2(0.08f, 0.10f), new Vector2(0.92f, 0.90f), 12, ColStarkWhite, TextAnchor.MiddleCenter, false);

        _contextRibbonText = PixelUIKit.CreatePixelLabel(
            botBar.transform,
            "ContextDescription",
            "Mulai dari World 00: Simulasi Tutorial untuk berlatih mekanik, lalu lanjut ke World 1: Localhost.",
            new Vector2(0.165f, 0.08f),
            new Vector2(0.64f, 0.84f),
            14,
            ColStarkWhite,
            TextAnchor.MiddleLeft,
            true);

        PixelUIKit.CreatePixelLabel(
            botBar.transform,
            "ShortcutLegend",
            "[W/S] PILIH   [ENTER] EKSEKUSI   [A/D] GANTI SEKTOR   [T] TUTORIAL   [ESC] KELUAR",
            new Vector2(0.65f, 0.08f),
            new Vector2(0.975f, 0.84f),
            12,
            ColP3Cyan,
            TextAnchor.MiddleRight,
            true);
    }

    // =========================================================================
    // 3. MAIN PANEL: P3R 3D TITLE LOCKUP + 3D KINETIC BLADES + HERO STAGE
    // =========================================================================
    private void BuildMainMenu()
    {
        _mainPanel = new GameObject("Main_Panel", typeof(RectTransform));
        _mainPanel.transform.SetParent(_canvas.transform, false);
        var rt = _mainPanel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var gm = Application.isPlaying ? GameManager.Ensure() : GameManager.Instance;
        int highestUnlocked = 0;
        for (int w = WorldRules.Count - 1; w >= 0; w--)
        {
            if (IsWorldUnlocked(gm, w))
            {
                highestUnlocked = w;
                break;
            }
        }
        // Start brand-new sessions on World 00 (Tutorial Sandbox) unless already graduated or higher worlds unlocked
        _defaultWorldIndex = (!TutorialWorldBuilder.HasGraduatedTutorialWorld && highestUnlocked == 0) ? -1 : highestUnlocked;

        BuildP3RTitleLockup(_mainPanel.transform);
        BuildP3RKineticMenuStack(_mainPanel.transform);
        BuildP3RHeroStageRight(_mainPanel.transform, gm);
    }

    private void BuildP3RTitleLockup(Transform parent)
    {
        // NOTE: NO RectMask2D on rotated titleRoot or children so text is 100% unclipped!
        var titleRoot = new GameObject("P3R_TitleLockup", typeof(RectTransform));
        titleRoot.transform.SetParent(parent, false);
        var trt = titleRoot.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.038f, 0.725f);
        trt.anchorMax = new Vector2(0.470f, 0.945f);
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        trt.localRotation = Quaternion.Euler(0f, 0f, -4.2f);

        // 3D Offset Cyan Wing behind PACKET
        CreateSpriteImage(titleRoot.transform, "PacketShadow", _bladeWingCyanSpr, new Vector2(0.02f, 0.44f), new Vector2(0.79f, 0.93f), Color.white);

        // 3D Beveled Stark White Parallelogram Blade for "PACKET"
        var whiteBlade = CreateSpriteImage(titleRoot.transform, "PacketWhiteBlade", _bladeSelectedWhiteSpr, new Vector2(0.0f, 0.49f), new Vector2(0.77f, 0.98f), Color.white);

        var logoImg = CreateSpriteImage(whiteBlade.transform, "ParcelEmblem", PixelUIKit.GetParcelLogo(), new Vector2(0.05f, 0.16f), new Vector2(0.22f, 0.88f), Color.white);
        logoImg.preserveAspect = true;

        PixelUIKit.CreatePixelLabel(whiteBlade.transform, "TitlePacketShadow", "PACKET", new Vector2(0.245f, 0.08f), new Vector2(0.955f, 0.90f), 42, new Color(0f, 0.85f, 1f, 0.45f), TextAnchor.MiddleLeft, false);
        PixelUIKit.CreatePixelLabel(whiteBlade.transform, "TitlePacket", "PACKET", new Vector2(0.24f, 0.12f), new Vector2(0.95f, 0.94f), 42, ColAbyssNavy, TextAnchor.MiddleLeft, false);

        // 3D Offset Crimson Wing behind COURIER
        CreateSpriteImage(titleRoot.transform, "CourierShadow", _bladeWingCrimsonSpr, new Vector2(0.14f, 0.0f), new Vector2(0.98f, 0.49f), Color.white);

        // 3D Beveled Obsidian Parallelogram Blade for "COURIER"
        var blackBlade = CreateSpriteImage(titleRoot.transform, "CourierBlackBlade", _bladeIdleSpr, new Vector2(0.12f, 0.05f), new Vector2(0.96f, 0.54f), Color.white);

        PixelUIKit.CreatePixelLabel(blackBlade.transform, "TitleCourierShadow", "COURIER", new Vector2(0.075f, 0.10f), new Vector2(0.715f, 0.90f), 38, new Color(0f, 0f, 0f, 0.9f), TextAnchor.MiddleLeft, false);
        PixelUIKit.CreatePixelLabel(blackBlade.transform, "TitleCourier", "COURIER", new Vector2(0.07f, 0.14f), new Vector2(0.71f, 0.94f), 38, ColP3Cyan, TextAnchor.MiddleLeft, false);

        // 3D Crimson Parallelogram Tag inside Courier Blade
        var verBadge = CreateSpriteImage(blackBlade.transform, "ReloadTag", _crestCrimsonSpr, new Vector2(0.71f, 0.18f), new Vector2(0.95f, 0.86f), Color.white);
        PixelUIKit.CreatePixelLabel(verBadge.transform, "ReloadTxt", "RELOAD", new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.90f), 12, ColStarkWhite, TextAnchor.MiddleCenter, false);
    }

    private void BuildP3RKineticMenuStack(Transform parent)
    {
        var pointerImg = CreateRotatedSlash(parent, "P3R_ActivePointer", new Vector2(0.022f, 0.56f), new Vector2(0.042f, 0.596f), -45f, ColCrimsonAlert);
        pointerImg.raycastTarget = false;
        _activeCursorArrowRt = pointerImg.rectTransform;

        string startLabel = _defaultWorldIndex < 0
            ? "MULAI TUTORIAL  [W0]"
            : $"MULAI MISI  [W{_defaultWorldIndex + 1}]";
        string startSub = _defaultWorldIndex < 0
            ? "WORLD KHUSUS AWAL // 4 ZONA LATIHAN -> WORLD 1"
            : $"SEKTOR: {WorldRules.Title(_defaultWorldIndex).ToUpperInvariant()} // {WorldRules.Segments(_defaultWorldIndex)} STAGE";

        AddKineticBlade(
            parent,
            index: 0,
            numTag: "01",
            iconName: "play",
            title: startLabel,
            subtitle: startSub,
            keyHint: "ENTER",
            description: "Terjun ke World yang dipilih (World 00 Tutorial untuk pemula, atau World 1 - 5 ekspedisi utama).",
            aMin: new Vector2(0.046f, 0.555f),
            aMax: new Vector2(0.468f, 0.652f),
            isDanger: false,
            onClick: LaunchSelectedWorld);

        AddKineticBlade(
            parent,
            index: 1,
            numTag: "02",
            iconName: "terminal",
            title: "DUNIA TUTORIAL  [W0]",
            subtitle: "WORLD KHUSUS AWAL // PROLOG + 4 ZONA LATIHAN",
            keyHint: "KEY T",
            description: "Masuk ke World 00: Simulasi Tutorial untuk menyaksikan prolog cerita 2099 dan melatih seluruh mekanik kurir sebelum World 1.",
            aMin: new Vector2(0.058f, 0.446f),
            aMax: new Vector2(0.480f, 0.543f),
            isDanger: false,
            onClick: LaunchTutorialOnboarding);

        AddKineticBlade(
            parent,
            index: 2,
            numTag: "03",
            iconName: "route",
            title: "PILIH SEKTOR & BOSS RUSH",
            subtitle: "PETA WORLD 1 - 5 & TANTANGAN 3 BOSS",
            keyHint: "TAB",
            description: "Buka peta jaringan penuh untuk memilih World 1 sampai World 5 atau masuk ke mode Boss Rush.",
            aMin: new Vector2(0.070f, 0.337f),
            aMax: new Vector2(0.492f, 0.434f),
            isDanger: false,
            onClick: OpenWorldSelect);

        AddKineticBlade(
            parent,
            index: 3,
            numTag: "04",
            iconName: "keys",
            title: "KONTROL & SISTEM",
            subtitle: "TOMBOL, TOUCH MOBILE & GETAR GAMEPAD",
            keyHint: "KEY C",
            description: "Lihat panduan tombol lengkap serta atur kontrol sentuh mobile dan efek getar gamepad.",
            aMin: new Vector2(0.082f, 0.228f),
            aMax: new Vector2(0.504f, 0.325f),
            isDanger: false,
            onClick: OpenSettings);

        AddKineticBlade(
            parent,
            index: 4,
            numTag: "05",
            iconName: "power",
            title: "PUTUSKAN KONEKSI",
            subtitle: "KELUAR DARI TERMINAL PACKET COURIER",
            keyHint: "ESC",
            description: "Simpan sesi logistik saat ini dan keluar menuju desktop.",
            aMin: new Vector2(0.094f, 0.119f),
            aMax: new Vector2(0.516f, 0.216f),
            isDanger: true,
            onClick: () => Application.Quit());
    }

    private void AddKineticBlade(
        Transform parent,
        int index,
        string numTag,
        string iconName,
        string title,
        string subtitle,
        string keyHint,
        string description,
        Vector2 aMin,
        Vector2 aMax,
        bool isDanger,
        Action onClick)
    {
        // NOTE: NO RectMask2D on rotated bladeGo so text is never clipped diagonally!
        var bladeGo = new GameObject($"P3R_Blade_{index}_{numTag}", typeof(RectTransform), typeof(Button));
        bladeGo.transform.SetParent(parent, false);
        var rt = bladeGo.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localRotation = Quaternion.Euler(0f, 0f, -3.2f);

        // Layer 0: Deep Drop Shadow Parallelogram (+11px, -10px) for real 3D elevation
        var deepShadow = CreateSpriteImage(bladeGo.transform, "Deep3DShadow", _bladeDeepShadowSpr, Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, 0.85f));
        var deepShadowRt = deepShadow.rectTransform;
        deepShadowRt.anchoredPosition = new Vector2(10f, -9f);

        // Layer 1: Electric Cyan / Crimson 3D Offset Wing Plate (+6px, -5px)
        var wingImg = CreateSpriteImage(bladeGo.transform, "OffsetCyanWing", isDanger ? _bladeWingCrimsonSpr : _bladeWingCyanSpr, Vector2.zero, Vector2.one, Color.white);
        var wingRt = wingImg.rectTransform;
        wingRt.anchoredPosition = new Vector2(6f, -5f);

        // Layer 2: Main 3D-Beveled Parallelogram Blade Plate (with 6px bottom bevel lip, inner highlight, & diagonal halftone)
        var plateImg = CreateSpriteImage(bladeGo.transform, "BladePlate3D", isDanger ? _bladeDangerIdleSpr : _bladeIdleSpr, Vector2.zero, Vector2.one, Color.white);
        plateImg.raycastTarget = true;

        // Layer 3: Protruding 3D Skewed Number & Icon Crest Badge on Left
        var crestImg = CreateSpriteImage(plateImg.transform, "CrestBadge3D", isDanger ? _crestCrimsonSpr : _crestCyanSpr, new Vector2(0.032f, 0.16f), new Vector2(0.205f, 0.88f), Color.white);

        var iconImg = CreateSpriteImage(crestImg.transform, "CrestIcon", PixelUIKit.GetIcon(iconName), new Vector2(0.10f, 0.22f), new Vector2(0.42f, 0.82f), ColAbyssNavy);
        iconImg.preserveAspect = true;

        var idxTxt = PixelUIKit.CreatePixelLabel(
            crestImg.transform,
            "IndexNum",
            numTag,
            new Vector2(0.42f, 0.14f),
            new Vector2(0.92f, 0.90f),
            20,
            ColAbyssNavy,
            TextAnchor.MiddleCenter,
            false);

        // Layer 4: Bold Command Title (with 3D Drop Shadow) & Subtitle inside safe margins
        var titleShadowTxt = PixelUIKit.CreatePixelLabel(
            plateImg.transform,
            "CommandTitleShadow",
            title,
            new Vector2(0.228f, 0.44f),
            new Vector2(0.808f, 0.88f),
            20,
            new Color(0f, 0f, 0f, 0.85f),
            TextAnchor.MiddleLeft,
            false);

        var titleTxt = PixelUIKit.CreatePixelLabel(
            plateImg.transform,
            "CommandTitle",
            title,
            new Vector2(0.224f, 0.47f),
            new Vector2(0.804f, 0.91f),
            20,
            ColStarkWhite,
            TextAnchor.MiddleLeft,
            false);

        var subTxt = PixelUIKit.CreatePixelLabel(
            plateImg.transform,
            "CommandSub",
            subtitle,
            new Vector2(0.224f, 0.14f),
            new Vector2(0.804f, 0.45f),
            11,
            new Color(0.56f, 0.80f, 0.96f, 0.95f),
            TextAnchor.MiddleLeft,
            true);

        // Layer 5: 3D Skewed Keycap Pill on Right
        var keyBg = CreateSpriteImage(plateImg.transform, "KeyBadge3D", _crestNavySpr, new Vector2(0.815f, 0.22f), new Vector2(0.962f, 0.82f), Color.white);
        var keyTxt = PixelUIKit.CreatePixelLabel(keyBg.transform, "KeyTxt", keyHint, new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.88f), 11, ColP3Cyan, TextAnchor.MiddleCenter, false);

        var item = new KineticBladeItem
        {
            Index = index,
            RootRt = rt,
            DeepShadowRt = deepShadowRt,
            CyanWingRt = wingRt,
            CyanWingImg = wingImg,
            MainPlateImg = plateImg,
            CrestBadgeImg = crestImg,
            CrestIconImg = iconImg,
            IndexText = idxTxt,
            TitleShadowText = titleShadowTxt,
            TitleText = titleTxt,
            SubText = subTxt,
            KeyBadgeBg = keyBg,
            KeyBadgeText = keyTxt,
            BaseAnchorMin = aMin,
            BaseAnchorMax = aMax,
            Description = description,
            OnActivate = onClick,
            IsDanger = isDanger
        };
        _blades.Add(item);

        var btn = bladeGo.GetComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = plateImg;
        int captured = index;
        btn.onClick.AddListener(() =>
        {
            SelectBlade(captured, playSound: false);
            AudioManager.PlayUIClick();
            onClick?.Invoke();
        });

        var trigger = bladeGo.AddComponent<EventTrigger>();
        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(_ => SelectBlade(captured, playSound: true));
        trigger.triggers.Add(enterEntry);
    }

    // =========================================================================
    // 4. RIGHT-SIDE HERO CHARACTER STAGE & QUICK SECTOR SWITCHER
    // =========================================================================
    private void BuildP3RHeroStageRight(Transform parent, GameManager gm)
    {
        var stageRoot = new GameObject("P3R_HeroStageRight", typeof(RectTransform));
        stageRoot.transform.SetParent(parent, false);
        var srt = stageRoot.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.53f, 0.09f);
        srt.anchorMax = new Vector2(0.97f, 0.92f);
        srt.offsetMin = Vector2.zero;
        srt.offsetMax = Vector2.zero;

        // 3D Diagonal Pedestal Slabs under Hero's Feet
        var pedShadow = CreateSpriteImage(stageRoot.transform, "PedestalBlackShadow", _bladeDeepShadowSpr, new Vector2(0.06f, 0.21f), new Vector2(0.96f, 0.31f), Color.white);
        pedShadow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -7f);

        var pedCyan = CreateSpriteImage(stageRoot.transform, "PedestalCyanSlab", _bladeWingCyanSpr, new Vector2(0.04f, 0.23f), new Vector2(0.94f, 0.33f), Color.white);
        pedCyan.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -7f);

        var whiteStep = CreateSpriteImage(stageRoot.transform, "PedestalWhiteTop", _bladeSelectedWhiteSpr, new Vector2(0.08f, 0.28f), new Vector2(0.88f, 0.35f), Color.white);
        whiteStep.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -7f);
        PixelUIKit.CreatePixelLabel(whiteStep.transform, "HeroUnitTag", "UNIT K-01 // PACKET COURIER PROTOTYPE", new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.90f), 12, ColAbyssNavy, TextAnchor.MiddleCenter, false);

        // Load Hero Courier Frames
        var frames = new List<Sprite>();
        string[] paths = { "PlayerSprites/idle1", "PlayerSprites/idle2", "PlayerSprites/run1", "PlayerSprites/run2", "PlayerSprites/run3", "PlayerSprites/run4" };
        foreach (string p in paths)
        {
            Sprite s = Resources.Load<Sprite>(p);
            if (s != null) frames.Add(s);
        }
        if (frames.Count == 0)
        {
            for (int i = 0; i < 4; i++)
            {
                Sprite s = Resources.Load<Sprite>("GeneratedArt/courier_run_" + i);
                if (s != null) frames.Add(s);
            }
        }
        Sprite heroSpr = frames.Count > 0 ? frames[0] : PixelUIKit.GetParcelLogo();

        // Layer 1: Giant Jet-Black Offset Silhouette Shadow
        var blackHero = CreateSpriteImage(stageRoot.transform, "HeroBlackSilhouette", heroSpr, new Vector2(0.18f, 0.32f), new Vector2(0.84f, 0.92f), new Color(0.01f, 0.02f, 0.05f, 0.85f));
        blackHero.preserveAspect = true;
        _heroBlackShadowRt = blackHero.rectTransform;

        // Layer 2: Giant Electric Cyan Pop Silhouette Shadow
        var cyanHero = CreateSpriteImage(stageRoot.transform, "HeroCyanSilhouette", heroSpr, new Vector2(0.18f, 0.32f), new Vector2(0.84f, 0.92f), new Color(0f, 0.92f, 1f, 0.72f));
        cyanHero.preserveAspect = true;
        _heroCyanShadowRt = cyanHero.rectTransform;

        // Layer 3: Crisp Foreground Hero Cutout
        var mainHero = CreateSpriteImage(stageRoot.transform, "HeroForegroundSprite", heroSpr, new Vector2(0.18f, 0.32f), new Vector2(0.84f, 0.92f), Color.white);
        mainHero.preserveAspect = true;
        _heroMainRt = mainHero.rectTransform;

        if (frames.Count > 1 && Application.isPlaying)
        {
            var anim = mainHero.gameObject.AddComponent<PixelSpriteAnimator>();
            anim.hideFlags = HideFlags.DontSave;
            anim.targetImage = mainHero;
            anim.frames = frames.ToArray();
            anim.framesPerSecond = 5.5f;
        }

        // Operator ARIA-7 Floating 3D Holo Badge (NO RectMask2D on rotated card!)
        var ariaCard = CreateSpriteImage(stageRoot.transform, "AriaCommsBadge", _bladeIdleSpr, new Vector2(0.52f, 0.75f), new Vector2(0.98f, 0.94f), Color.white);
        ariaCard.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -3.5f);
        var ariaPort = CreateSpriteImage(ariaCard.transform, "AriaAvatar", CyberOpeningAndTutorialUI.GetOperatorPortraitSprite(), new Vector2(0.06f, 0.16f), new Vector2(0.28f, 0.86f), Color.white);
        ariaPort.preserveAspect = true;
        PixelUIKit.CreatePixelLabel(ariaCard.transform, "AriaTitle", "OPERATOR ARIA-7", new Vector2(0.31f, 0.54f), new Vector2(0.94f, 0.86f), 12, ColP3Cyan, TextAnchor.MiddleLeft, false);
        PixelUIKit.CreatePixelLabel(ariaCard.transform, "AriaQuote", "\"Sinyal siap. Pilih sektor atau tekan [T] untuk latihan!\"", new Vector2(0.31f, 0.16f), new Vector2(0.94f, 0.54f), 10, ColStarkWhite, TextAnchor.MiddleLeft, true);

        // Bottom-Right 3D Quick Sector Switcher Card (NO RectMask2D on rotated card!)
        CreateSpriteImage(stageRoot.transform, "SectorCardWing", _bladeWingCyanSpr, new Vector2(0.05f, 0.01f), new Vector2(0.97f, 0.21f), Color.white).rectTransform.localRotation = Quaternion.Euler(0f, 0f, -2.5f);
        var sectorCard = CreateSpriteImage(stageRoot.transform, "ActiveSectorSelectorCard", _bladeIdleSpr, new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.22f), Color.white);
        sectorCard.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -2.5f);
        sectorCard.raycastTarget = true;

        PixelUIKit.CreatePixelLabel(sectorCard.transform, "SecHeader", "TARGET EKSPEDISI AKTIF  //  GANTI CEPAT [A / D]", new Vector2(0.16f, 0.66f), new Vector2(0.84f, 0.88f), 11, ColP3Cyan, TextAnchor.MiddleCenter, true);

        _sectorPreviewTitleText = PixelUIKit.CreatePixelLabel(
            sectorCard.transform,
            "SecTitle",
            "",
            new Vector2(0.16f, 0.34f),
            new Vector2(0.84f, 0.66f),
            19,
            ColStarkWhite,
            TextAnchor.MiddleCenter,
            false);

        _sectorPreviewMetaText = PixelUIKit.CreatePixelLabel(
            sectorCard.transform,
            "SecMeta",
            "",
            new Vector2(0.16f, 0.12f),
            new Vector2(0.84f, 0.35f),
            11,
            ColP3Aqua,
            TextAnchor.MiddleCenter,
            true);

        // 3D Prev [<] and Next [>] Sector Buttons
        var prevBtnGo = CreateSpriteImage(sectorCard.transform, "PrevSectorBtn", _crestCyanSpr, new Vector2(0.045f, 0.20f), new Vector2(0.145f, 0.82f), Color.white);
        prevBtnGo.raycastTarget = true;
        var prevBtn = prevBtnGo.gameObject.AddComponent<Button>();
        prevBtn.onClick.AddListener(() => CyclePreviewSector(-1));
        PixelUIKit.CreatePixelLabel(prevBtnGo.transform, "Lbl", "<", new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f), 20, ColAbyssNavy, TextAnchor.MiddleCenter, false);

        var nextBtnGo = CreateSpriteImage(sectorCard.transform, "NextSectorBtn", _crestCyanSpr, new Vector2(0.855f, 0.20f), new Vector2(0.955f, 0.82f), Color.white);
        nextBtnGo.raycastTarget = true;
        var nextBtn = nextBtnGo.gameObject.AddComponent<Button>();
        nextBtn.onClick.AddListener(() => CyclePreviewSector(1));
        PixelUIKit.CreatePixelLabel(nextBtnGo.transform, "Lbl", ">", new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f), 20, ColAbyssNavy, TextAnchor.MiddleCenter, false);

        RefreshSectorPreviewCard();
    }

    private void CyclePreviewSector(int delta)
    {
        var gm = Application.isPlaying ? GameManager.Ensure() : GameManager.Instance;
        // Total selectable entries: -1 (World 00 Tutorial) + 0..(WorldRules.Count - 1)
        int totalSlots = WorldRules.Count + 1;
        int currentSlot = _defaultWorldIndex + 1; // maps -1 -> 0, 0 -> 1, ...

        for (int step = 1; step <= totalSlots; step++)
        {
            int nextSlot = (currentSlot + delta * step + totalSlots * 4) % totalSlots;
            int candidateWorld = nextSlot - 1;
            if (candidateWorld < 0 || IsWorldUnlocked(gm, candidateWorld))
            {
                _defaultWorldIndex = candidateWorld;
                if (Application.isPlaying) AudioManager.PlayUIClick();
                RefreshSectorPreviewCard();
                return;
            }
        }
    }

    private void RefreshSectorPreviewCard()
    {
        if (_defaultWorldIndex < 0)
        {
            if (_sectorPreviewTitleText != null)
            {
                _sectorPreviewTitleText.text = "WORLD 00 // SIMULASI TUTORIAL";
            }
            if (_sectorPreviewMetaText != null)
            {
                _sectorPreviewMetaText.text = "WORLD KHUSUS AWAL   |   4 ZONA LATIHAN   |   KELULUSAN -> WORLD 1";
            }
            if (_blades.Count > 0 && _blades[0].TitleText != null)
            {
                string t = "MULAI TUTORIAL  [W0]";
                _blades[0].TitleText.text = t;
                if (_blades[0].TitleShadowText != null) _blades[0].TitleShadowText.text = t;
                _blades[0].SubText.text = "WORLD KHUSUS AWAL // PROLOG + 4 ZONA LATIHAN -> WORLD 1";
            }
            return;
        }

        string wTitle = WorldRules.Title(_defaultWorldIndex).ToUpperInvariant();
        int segs = WorldRules.Segments(_defaultWorldIndex);
        float ttl = WorldRules.TTL(_defaultWorldIndex);

        if (_sectorPreviewTitleText != null)
        {
            _sectorPreviewTitleText.text = $"WORLD 0{_defaultWorldIndex + 1} // {wTitle}";
        }
        if (_sectorPreviewMetaText != null)
        {
            _sectorPreviewMetaText.text = $"{segs} STAGE   |   BATAS WAKTU TTL: {ttl:0} DETIK   |   STATUS: SIAP";
        }
        if (_blades.Count > 0 && _blades[0].TitleText != null)
        {
            string t = $"MULAI MISI  [W{_defaultWorldIndex + 1}]";
            _blades[0].TitleText.text = t;
            if (_blades[0].TitleShadowText != null) _blades[0].TitleShadowText.text = t;
            _blades[0].SubText.text = $"SEKTOR: {wTitle} // {segs} STAGE (TTL {ttl:0}s)";
        }
    }

    private void LaunchSelectedWorld()
    {
        if (_defaultWorldIndex < 0)
        {
            TutorialWorldBuilder.LaunchTutorialWorld();
            return;
        }
        TutorialWorldBuilder.IsTutorialWorldActive = false;
        TutorialWorldBuilder.HasGraduatedTutorialWorld = true;
        CyberOpeningAndTutorialUI.HasCompletedOnboardingThisSession = true;
        GameManager.Ensure().StartRun(_defaultWorldIndex);
    }

    private void LaunchTutorialOnboarding()
    {
        TutorialWorldBuilder.LaunchTutorialWorld();
    }

    private static bool IsWorldUnlocked(GameManager gm, int world)
    {
        if (gm == null) return world == 0;
        bool unlockedFlag = gm.worldsUnlocked != null && world < gm.worldsUnlocked.Length && gm.worldsUnlocked[world];
        if (!unlockedFlag && world == 0) unlockedFlag = true;
        return unlockedFlag && (!Application.isPlaying || GameManager.WorldAvailable(world));
    }

    // =========================================================================
    // 5. P3R DIAGONAL SUB-MODALS (WORLD SELECT & CONTROLS)
    // =========================================================================
    private void BuildWorldSelectPanel()
    {
        _worldSelectPanel = new GameObject("WorldSelect_Panel", typeof(RectTransform));
        _worldSelectPanel.transform.SetParent(_canvas.transform, false);
        var rt = _worldSelectPanel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var bg = CreateRectPanel(_worldSelectPanel.transform, "BG", Vector2.zero, Vector2.one, new Color(0.015f, 0.03f, 0.07f, 0.94f));
        bg.raycastTarget = true;

        CreateRotatedSlash(_worldSelectPanel.transform, "ModalCyanShadow", new Vector2(0.175f, 0.065f), new Vector2(0.835f, 0.925f), -1.5f, ColP3Cyan).raycastTarget = false;

        var modal = PixelUIKit.CreateSteppedPanel(
            _worldSelectPanel.transform,
            "RouteModalCard",
            new Vector2(0.17f, 0.07f),
            new Vector2(0.83f, 0.93f),
            new Color32(8, 16, 32, 252),
            new Color32(248, 252, 255, 255),
            new Color32(0, 234, 255, 255),
            4);

        var hdr = CreateSpriteImage(modal.transform, "WhiteHeaderBar", _bladeSelectedWhiteSpr, new Vector2(0.04f, 0.895f), new Vector2(0.96f, 0.970f), Color.white);
        PixelUIKit.CreatePixelLabel(hdr.transform, "ModalTitle", "PILIH SEKTOR EKSPEDISI  //  WORLD 0 - 5", new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.92f), 22, ColAbyssNavy, TextAnchor.MiddleCenter, false);

        // World 00: Dedicated Initial Tutorial World
        PixelUIKit.CreateTactileButton(
            modal.transform,
            "terminal",
            "00 // SIMULASI TUTORIAL [WORLD AWAL]",
            "ARENA LATIHAN 4 ZONA: DOUBLE JUMP, PLATFORM BERGERAK, DASH & PAKET -> WORLD 1",
            "W0",
            new Vector2(0.06f, 0.785f),
            new Vector2(0.94f, 0.875f),
            PixelUIKit.ButtonTheme.PrimaryCyan,
            () => TutorialWorldBuilder.LaunchTutorialWorld());

        var gm = Application.isPlaying ? GameManager.Ensure() : GameManager.Instance;
        for (int world = 0; world < WorldRules.Count; world++)
        {
            int selected = world;
            bool unlocked = IsWorldUnlocked(gm, world);
            bool isBoss = world == 4;
            float top = 0.770f - world * 0.100f;
            float bot = top - 0.088f;

            string title = unlocked
                ? $"0{world + 1} // {WorldRules.Title(world).ToUpperInvariant()}" + (isBoss ? " [FINAL BOSS]" : "")
                : $"0{world + 1} // {WorldRules.Title(world).ToUpperInvariant()} [TERKUNCI]";
            string sub = $"{WorldRules.Segments(world)} STAGE | BATAS WAKTU TTL: {WorldRules.TTL(world):0} DETIK";

            PixelUIKit.CreateTactileButton(
                modal.transform,
                isBoss ? "skull" : (unlocked ? "route" : "lock"),
                title,
                sub,
                unlocked ? $"W{world + 1}" : "LOCKED",
                new Vector2(0.06f, bot),
                new Vector2(0.94f, top),
                isBoss ? PixelUIKit.ButtonTheme.DangerCrimson : (unlocked ? PixelUIKit.ButtonTheme.PrimaryCyan : PixelUIKit.ButtonTheme.SecondarySlate),
                () =>
                {
                    if (unlocked)
                    {
                        TutorialWorldBuilder.IsTutorialWorldActive = false;
                        TutorialWorldBuilder.HasGraduatedTutorialWorld = true;
                        CyberOpeningAndTutorialUI.HasCompletedOnboardingThisSession = true;
                        GameManager.Ensure().StartRun(selected);
                    }
                });
        }

        PixelUIKit.CreateTactileButton(
            modal.transform,
            "skull",
            "BOSS RUSH GAUNTLET // TANTANGAN 3 BOSS",
            "LAWAN THE CENSOR -> SOLARIS -> ROOTKIT CORE SECARA BERUNTUN",
            "RUSH",
            new Vector2(0.06f, 0.165f),
            new Vector2(0.94f, 0.253f),
            PixelUIKit.ButtonTheme.DangerCrimson,
            () => BossRushManager.StartBossRush());

        PixelUIKit.CreateTactileButton(
            modal.transform,
            "back",
            "KEMBALI KE MENU UTAMA",
            "TUTUP PETA EKSPEDISI",
            "ESC",
            new Vector2(0.24f, 0.030f),
            new Vector2(0.76f, 0.115f),
            PixelUIKit.ButtonTheme.SecondarySlate,
            ClosePanels);

        _worldSelectPanel.SetActive(false);
    }

    private void BuildSettingsPanel()
    {
        if (_settingsPanel != null)
        {
            if (Application.isPlaying) Destroy(_settingsPanel);
            else DestroyImmediate(_settingsPanel);
            _settingsPanel = null;
        }

        _settingsPanel = new GameObject("Settings_Panel", typeof(RectTransform));
        _settingsPanel.transform.SetParent(_canvas.transform, false);
        var rt = _settingsPanel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var bg = CreateRectPanel(_settingsPanel.transform, "BG", Vector2.zero, Vector2.one, new Color(0.015f, 0.03f, 0.07f, 0.94f));
        bg.raycastTarget = true;

        CreateRotatedSlash(_settingsPanel.transform, "ModalCyanShadow", new Vector2(0.165f, 0.075f), new Vector2(0.845f, 0.925f), -1.5f, ColP3Cyan).raycastTarget = false;

        var modal = PixelUIKit.CreateSteppedPanel(
            _settingsPanel.transform,
            "ControlsModalCard",
            new Vector2(0.16f, 0.08f),
            new Vector2(0.84f, 0.93f),
            new Color32(8, 16, 32, 252),
            new Color32(248, 252, 255, 255),
            new Color32(0, 234, 255, 255),
            4);

        var hdr = CreateSpriteImage(modal.transform, "WhiteHeaderBar", _bladeSelectedWhiteSpr, new Vector2(0.04f, 0.87f), new Vector2(0.96f, 0.96f), Color.white);
        PixelUIKit.CreatePixelLabel(hdr.transform, "Title", "KONTROL & PENGATURAN SISTEM", new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.92f), 22, ColAbyssNavy, TextAnchor.MiddleCenter, false);

        string controlsText =
            "GERAK KIRI / KANAN:   [A / D] atau [Panah Kiri / Kanan]\n\n" +
            "LOMPAT / DOUBLE JUMP: [Space] (Tekan 2x di udara)\n\n" +
            "PHASE DASH (KEBAL):   [Left Shift] (Menembus serangan & bahaya)\n\n" +
            "WALL CLING (PANJAT):  Tahan arah ke dinding saat di udara\n\n" +
            "SERANGAN & PARRY:     [Klik Kiri / J] Tebas  |  [Q / Klik Kanan] Nano Parry\n\n" +
            "INTERAKSI & TUTORIAL: [E] Bicara NPC / Ambil Paket  |  [F1] Ulang Tutorial";

        var label = PixelUIKit.CreatePixelLabel(modal.transform, "ControlsBody", controlsText, new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.84f), 15, ColStarkWhite, TextAnchor.MiddleLeft, true);
        label.lineSpacing = 1.15f;

        string touchState = TouchControlsUI.IsTouchEnabled ? "[AKTIF]" : "[NONAKTIF]";
        PixelUIKit.CreateTactileButton(
            modal.transform,
            "mobile",
            "TOUCH CONTROLS: " + touchState,
            "TOMBOL VIRTUAL LAYAR SENTUH UNTUK MOBILE",
            "TOGGLE",
            new Vector2(0.06f, 0.21f),
            new Vector2(0.48f, 0.33f),
            TouchControlsUI.IsTouchEnabled ? PixelUIKit.ButtonTheme.PrimaryCyan : PixelUIKit.ButtonTheme.SecondarySlate,
            () =>
            {
                TouchControlsUI.IsTouchEnabled = !TouchControlsUI.IsTouchEnabled;
                BuildSettingsPanel();
                _settingsPanel.SetActive(true);
            });

        string rumbleState = GamepadControllerSupport.EnableVibration ? "[AKTIF]" : "[NONAKTIF]";
        PixelUIKit.CreateTactileButton(
            modal.transform,
            "vibrate",
            "GETAR GAMEPAD: " + rumbleState,
            "EFEK GETAR HAPTIC PADA CONTROLLER",
            "TOGGLE",
            new Vector2(0.52f, 0.21f),
            new Vector2(0.94f, 0.33f),
            GamepadControllerSupport.EnableVibration ? PixelUIKit.ButtonTheme.PrimaryCyan : PixelUIKit.ButtonTheme.SecondarySlate,
            () =>
            {
                GamepadControllerSupport.EnableVibration = !GamepadControllerSupport.EnableVibration;
                BuildSettingsPanel();
                _settingsPanel.SetActive(true);
            });

        PixelUIKit.CreateTactileButton(
            modal.transform,
            "back",
            "KEMBALI KE MENU UTAMA",
            "SIMPAN & TUTUP PENGATURAN",
            "ESC",
            new Vector2(0.24f, 0.05f),
            new Vector2(0.76f, 0.16f),
            PixelUIKit.ButtonTheme.SecondarySlate,
            ClosePanels);

        _settingsPanel.SetActive(false);
    }

    private void OpenWorldSelect()
    {
        if (_mainPanel) _mainPanel.SetActive(false);
        if (_worldSelectPanel) _worldSelectPanel.SetActive(true);
        if (_settingsPanel) _settingsPanel.SetActive(false);
    }

    private void OpenSettings()
    {
        if (_mainPanel) _mainPanel.SetActive(false);
        if (_worldSelectPanel) _worldSelectPanel.SetActive(false);
        if (_settingsPanel) _settingsPanel.SetActive(true);
    }

    private void ClosePanels()
    {
        if (_mainPanel) _mainPanel.SetActive(true);
        if (_worldSelectPanel) _worldSelectPanel.SetActive(false);
        if (_settingsPanel) _settingsPanel.SetActive(false);
    }

    // =========================================================================
    // PROCEDURAL 3D PARALLELOGRAM BLADE & CREST SPRITE GENERATOR
    // =========================================================================
    private static void EnsureBladeSpritesGenerated()
    {
        if (_bladeIdleSpr != null && _bladeSelectedWhiteSpr != null && _crestCyanSpr != null) return;

        // 1. Idle Blade: Deep Cobalt-Obsidian body, Bright Electric Cyan 3px frame, 7px 3D Dark Bottom Lip, subtle diagonal stripes
        _bladeIdleSpr = Build3DParallelogramSprite(
            w: 360, h: 76, slantPx: 22, borderPx: 3, lipPx: 7,
            topFill: new Color32(14, 28, 56, 252),
            botFill: new Color32(7, 15, 32, 252),
            borderCol: new Color32(0, 234, 255, 255),
            topHighlight: new Color32(90, 225, 255, 180),
            lipCol: new Color32(0, 118, 158, 255),
            stripeCol: new Color32(0, 210, 255, 18));

        // 2. Selected Blade (P3R Signature): Stark White body, Deep Ice-Blue subtle gradient, Electric Cyan 3px frame + 7px 3D Royal Lip
        _bladeSelectedWhiteSpr = Build3DParallelogramSprite(
            w: 360, h: 76, slantPx: 22, borderPx: 3, lipPx: 7,
            topFill: new Color32(252, 254, 255, 255),
            botFill: new Color32(218, 242, 255, 255),
            borderCol: new Color32(0, 234, 255, 255),
            topHighlight: new Color32(255, 255, 255, 255),
            lipCol: new Color32(12, 52, 112, 255),
            stripeCol: new Color32(0, 180, 240, 26));

        // 3. Danger Idle Blade (05 Exit)
        _bladeDangerIdleSpr = Build3DParallelogramSprite(
            w: 360, h: 76, slantPx: 22, borderPx: 3, lipPx: 7,
            topFill: new Color32(38, 12, 28, 252),
            botFill: new Color32(20, 7, 16, 252),
            borderCol: new Color32(255, 36, 87, 255),
            topHighlight: new Color32(255, 120, 150, 170),
            lipCol: new Color32(130, 16, 48, 255),
            stripeCol: new Color32(255, 36, 87, 20));

        // 4. Danger Selected Blade
        _bladeDangerSelectedSpr = Build3DParallelogramSprite(
            w: 360, h: 76, slantPx: 22, borderPx: 3, lipPx: 7,
            topFill: new Color32(255, 42, 92, 255),
            botFill: new Color32(205, 18, 64, 255),
            borderCol: new Color32(255, 245, 250, 255),
            topHighlight: new Color32(255, 190, 205, 255),
            lipCol: new Color32(92, 8, 30, 255),
            stripeCol: new Color32(255, 255, 255, 28));

        // 5. Offset Wing Plates (Cyan & Crimson) & Deep Shadow Plate
        _bladeWingCyanSpr = Build3DParallelogramSprite(
            w: 360, h: 76, slantPx: 22, borderPx: 2, lipPx: 4,
            topFill: new Color32(0, 234, 255, 255),
            botFill: new Color32(0, 175, 220, 255),
            borderCol: new Color32(180, 252, 255, 255),
            topHighlight: new Color32(255, 255, 255, 255),
            lipCol: new Color32(0, 105, 148, 255),
            stripeCol: new Color32(255, 255, 255, 0));

        _bladeWingCrimsonSpr = Build3DParallelogramSprite(
            w: 360, h: 76, slantPx: 22, borderPx: 2, lipPx: 4,
            topFill: new Color32(255, 36, 87, 255),
            botFill: new Color32(195, 16, 62, 255),
            borderCol: new Color32(255, 175, 195, 255),
            topHighlight: new Color32(255, 220, 230, 255),
            lipCol: new Color32(105, 8, 34, 255),
            stripeCol: new Color32(255, 255, 255, 0));

        _bladeDeepShadowSpr = Build3DParallelogramSprite(
            w: 360, h: 76, slantPx: 22, borderPx: 0, lipPx: 0,
            topFill: new Color32(2, 5, 14, 230),
            botFill: new Color32(2, 5, 14, 230),
            borderCol: new Color32(2, 5, 14, 230),
            topHighlight: new Color32(2, 5, 14, 230),
            lipCol: new Color32(2, 5, 14, 230),
            stripeCol: new Color32(0, 0, 0, 0));

        // 6. Protruding 3D Crest & Keycap Badges
        _crestCyanSpr = Build3DParallelogramSprite(
            w: 112, h: 56, slantPx: 12, borderPx: 2, lipPx: 5,
            topFill: new Color32(20, 242, 255, 255),
            botFill: new Color32(0, 195, 230, 255),
            borderCol: new Color32(220, 254, 255, 255),
            topHighlight: new Color32(255, 255, 255, 255),
            lipCol: new Color32(0, 108, 148, 255),
            stripeCol: new Color32(255, 255, 255, 25));

        _crestCrimsonSpr = Build3DParallelogramSprite(
            w: 112, h: 56, slantPx: 12, borderPx: 2, lipPx: 5,
            topFill: new Color32(255, 42, 92, 255),
            botFill: new Color32(210, 18, 66, 255),
            borderCol: new Color32(255, 210, 225, 255),
            topHighlight: new Color32(255, 255, 255, 255),
            lipCol: new Color32(115, 8, 38, 255),
            stripeCol: new Color32(255, 255, 255, 25));

        _crestNavySpr = Build3DParallelogramSprite(
            w: 112, h: 56, slantPx: 12, borderPx: 2, lipPx: 5,
            topFill: new Color32(14, 28, 56, 255),
            botFill: new Color32(8, 16, 34, 255),
            borderCol: new Color32(0, 224, 255, 255),
            topHighlight: new Color32(70, 190, 255, 180),
            lipCol: new Color32(3, 7, 16, 255),
            stripeCol: new Color32(0, 200, 255, 20));
    }

    private static Sprite Build3DParallelogramSprite(
        int w,
        int h,
        int slantPx,
        int borderPx,
        int lipPx,
        Color32 topFill,
        Color32 botFill,
        Color32 borderCol,
        Color32 topHighlight,
        Color32 lipCol,
        Color32 stripeCol)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color32[w * h];
        Color32 clear = new Color32(0, 0, 0, 0);

        for (int y = 0; y < h; y++)
        {
            float ny = y / (float)Mathf.Max(1, h - 1);
            // Slanted left & right edges (parallelogram lean to the right as y increases)
            int xLeft = Mathf.RoundToInt((1f - ny) * 0f + ny * slantPx);
            int xRight = (w - 1) - Mathf.RoundToInt((1f - ny) * slantPx);

            // Gradient base color for this scanline
            Color32 rowBase = Color32.Lerp(botFill, topFill, ny);

            for (int x = 0; x < w; x++)
            {
                if (x < xLeft || x > xRight)
                {
                    pixels[y * w + x] = clear;
                    continue;
                }

                int distLeft = x - xLeft;
                int distRight = xRight - x;

                // 3D Bottom & Right Bevel Lip
                if (lipPx > 0 && (y < lipPx || distRight < lipPx))
                {
                    pixels[y * w + x] = lipCol;
                    continue;
                }

                // Outer Perimeter Frame above Lip
                if (borderPx > 0 && (y >= h - borderPx || y < lipPx + borderPx || distLeft < borderPx || distRight < lipPx + borderPx))
                {
                    pixels[y * w + x] = borderCol;
                    continue;
                }

                // Inner Top Specular Highlight Line (gives crisp 3D glass/acrylic edge)
                if (borderPx > 0 && y >= h - borderPx - 2)
                {
                    pixels[y * w + x] = topHighlight;
                    continue;
                }

                // Subtle Diagonal Cyber Halftone Stripes inside Body
                Color32 c = rowBase;
                if (stripeCol.a > 0 && ((x + y * 2) % 16 < 5))
                {
                    float a = stripeCol.a / 255f;
                    c = new Color32(
                        (byte)Mathf.Clamp(c.r + stripeCol.r * a, 0, 255),
                        (byte)Mathf.Clamp(c.g + stripeCol.g * a, 0, 255),
                        (byte)Mathf.Clamp(c.b + stripeCol.b * a, 0, 255),
                        c.a);
                }

                pixels[y * w + x] = c;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    // =========================================================================
    // HELPER BUILDERS & PROCEDURAL WATERMARK
    // =========================================================================
    private static Canvas CreateCanvas()
    {
        var go = new GameObject("Menu_Canvas");
        go.hideFlags = HideFlags.DontSave;
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    public static void MarkDontSaveRecursively(GameObject go)
    {
        if (go == null) return;
        go.hideFlags = HideFlags.DontSave;
        var components = go.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null) components[i].hideFlags = HideFlags.DontSave;
        }
        for (int i = 0; i < go.transform.childCount; i++)
        {
            MarkDontSaveRecursively(go.transform.GetChild(i).gameObject);
        }
    }

    private static Image CreateRectPanel(Transform parent, string name, Vector2 aMin, Vector2 aMax, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    private static Image CreateRotatedSlash(Transform parent, string name, Vector2 aMin, Vector2 aMax, float angleDeg, Color color)
    {
        var img = CreateRectPanel(parent, name, aMin, aMax, color);
        img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angleDeg);
        return img;
    }

    private static Image CreateSpriteImage(Transform parent, string name, Sprite sprite, Vector2 aMin, Vector2 aMax, Color tint)
    {
        var img = CreateRectPanel(parent, name, aMin, aMax, tint);
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    private static Sprite GetWatermarkRingSprite()
    {
        if (_cachedRingWatermark != null) return _cachedRingWatermark;

        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                bool outerRing = Mathf.Abs(r - 0.92f) < 0.025f;
                bool innerRing = Mathf.Abs(r - 0.64f) < 0.018f;
                bool coreRing = Mathf.Abs(r - 0.28f) < 0.022f;
                bool crossHair = (Mathf.Abs(dx) < 0.015f || Mathf.Abs(dy) < 0.015f || Mathf.Abs(Mathf.Abs(dx) - Mathf.Abs(dy)) < 0.012f) && r < 0.95f && r > 0.18f;
                float a = (outerRing || innerRing || coreRing) ? 1f : (crossHair ? 0.55f : 0f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        _cachedRingWatermark = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        return _cachedRingWatermark;
    }
}

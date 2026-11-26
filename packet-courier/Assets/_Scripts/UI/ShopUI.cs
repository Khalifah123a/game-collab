using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Tactical Cyberpunk Shop UI with handcrafted pixel-art visuals.
/// Redesigned to match the FigCraft kiosk layout with Unit-74 vendor avatar,
/// transmission feed, live chassis telemetry, and 6 tactile tuning modules.
/// </summary>
public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }
    public bool IsOpen => _isOpen;
    public static bool IsShopOpen => Instance != null && Instance._isOpen;

    private bool _isOpen;
    private object _owner;
    private PlayerController _held;

    private Canvas _canvas;
    private CanvasGroup _canvasGroup;
    private RectTransform _panelRt;
    private Text _balanceText;
    private Text _footerHint;

    // Live Telemetry text fields
    private Text _telemTierText;
    private Text _telemDmgText;
    private Text _telemSpdText;
    private Text _telemRunText;
    private Text _telemClingText;

    private readonly List<Button> _buttons = new List<Button>();
    private readonly List<Text> _titleTexts = new List<Text>();
    private readonly List<Text> _levelTexts = new List<Text>();
    private readonly List<Text> _descTexts = new List<Text>();
    private readonly List<Text> _statPillTexts = new List<Text>();
    private readonly List<Text> _priceTexts = new List<Text>();
    private readonly List<Image> _priceBadges = new List<Image>();

    private readonly GameManagerShopWallet _wallet = new GameManagerShopWallet();
    private Coroutine _animCoroutine;

    private static readonly string[] ShopTitles = {
        "WALL CLING BOOSTER",
        "WEAPON DAMAGE MODULE",
        "WEAPON TIER UPGRADE",
        "BLADE REACH EXTENDER",
        "ATTACK SPEED ACTUATOR",
        "OVERCLOCK RUN SERVOS"
    };

    private static readonly string[] ShopCategories = {
        "GERAK",
        "SERANGAN",
        "SENJATA",
        "SERANGAN",
        "SERANGAN",
        "GERAK"
    };

    private static readonly Color[] ShopColors = {
        new Color(0.21f, 0.96f, 0.61f), // 0: Mint
        new Color(0.94f, 0.48f, 0.16f), // 1: Orange
        new Color(0.00f, 0.94f, 1.00f), // 2: Cyan
        new Color(0.21f, 0.96f, 0.61f), // 3: Mint
        new Color(0.94f, 0.48f, 0.16f), // 4: Orange
        new Color(0.21f, 0.96f, 0.61f)  // 5: Mint
    };

    public static ShopUI Ensure()
    {
        if (Instance != null) return Instance;
        return new GameObject("Shop_Canvas").AddComponent<ShopUI>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildCanvas();
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Open(object owner = null)
    {
        if (_isOpen) return;
        if (DialogUI.IsOpen) return;

        EnsureEventSystem();
        _owner = owner;
        _isOpen = true;
        gameObject.SetActive(true);
        Freeze();
        Refresh();
        AudioManager.PlayUIOpen();
        AudioManager.SetMuffled(true);

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateOpen());
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        if (FindAnyObjectByType<EventSystem>() != null) return;
        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<StandaloneInputModule>();
    }

    public void SwitchToOverclockTerminal()
    {
        object savedOwner = _owner;
        if (!_isOpen) return;
        _isOpen = false;
        _owner = null;
        Release();
        if (_animCoroutine != null)
        {
            StopCoroutine(_animCoroutine);
            _animCoroutine = null;
        }
        gameObject.SetActive(false);
        OverclockUI.Ensure().Open(savedOwner);
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;
        _owner = null;
        Release();
        AudioManager.SetMuffled(false);

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateClose());
    }

    public void CloseIfOwner(object owner)
    {
        if (_isOpen && _owner == owner) Close();
    }

    private IEnumerator AnimateOpen()
    {
        float duration = 0.20f;
        float elapsed = 0f;
        _canvasGroup.alpha = 0f;
        _panelRt.localScale = new Vector3(0.94f, 0.94f, 1f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            _canvasGroup.alpha = ease;
            _panelRt.localScale = Vector3.LerpUnclamped(new Vector3(0.94f, 0.94f, 1f), Vector3.one, ease);
            yield return null;
        }

        _canvasGroup.alpha = 1f;
        _panelRt.localScale = Vector3.one;
        _animCoroutine = null;
    }

    private IEnumerator AnimateClose()
    {
        float duration = 0.12f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _canvasGroup.alpha = 1f - t;
            _panelRt.localScale = Vector3.Lerp(Vector3.one, new Vector3(0.95f, 0.95f, 1f), t);
            yield return null;
        }

        gameObject.SetActive(false);
        _animCoroutine = null;
    }

    public void TryBuy(int index)
    {
        if (index < 0 || index >= ShopCatalog.Entries.Length) return;
        var entry = ShopCatalog.Entries[index];
        var result = ShopRules.Buy(_wallet, entry);

        if (result == PurchaseResult.Success)
        {
            AudioManager.PlayUIClick();
            AudioManager.Play("sfx_collect", 0.9f, 1.2f);
            FindAnyObjectByType<PlayerController>()?.RefreshUpgradedStats();
            Refresh();
        }
        else if (result == PurchaseResult.InsufficientFunds)
        {
            AudioManager.PlayUIClick();
            StartCoroutine(FlashBalanceRed());
        }
    }

    private IEnumerator FlashBalanceRed()
    {
        if (_balanceText == null) yield break;
        var origColor = PixelUIKit.Mint;
        _balanceText.color = PixelUIKit.Crimson;
        yield return new WaitForSecondsRealtime(0.25f);
        if (_balanceText != null) _balanceText.color = origColor;
    }

    public void Refresh()
    {
        var gm = GameManager.Instance;
        int balance = gm != null ? gm.AvailableDataBits : 0;
        if (_balanceText != null)
            _balanceText.text = $"{balance:N0} DATA BITS";

        // Update live telemetry
        int wpnLv = _wallet.GetLevel(2);
        int wpnDmg = _wallet.GetLevel(1);
        int atkSpd = _wallet.GetLevel(4);
        int runSpd = _wallet.GetLevel(5);
        int cling = _wallet.GetLevel(0);

        if (_telemTierText != null) _telemTierText.text = $"SENJATA:     MK-{(wpnLv + 1)}";
        if (_telemDmgText != null) _telemDmgText.text = $"DAMAGE:      +{(wpnDmg + 1)} DMG";
        if (_telemSpdText != null) _telemSpdText.text = $"SERANG:      {(100 + atkSpd * 10)}%";
        if (_telemRunText != null) _telemRunText.text = $"LARI:        {(6.00f + runSpd * 0.25f):F2} m/s";
        if (_telemClingText != null) _telemClingText.text = $"PANJAT:      {(0.50f + cling * 0.15f):F2} dtk";

        for (int i = 0; i < ShopCatalog.Entries.Length && i < _buttons.Count; i++)
        {
            var entry = ShopCatalog.Entries[i];
            int level = _wallet.GetLevel(i);
            int max = _wallet.GetMaxLevel(i);
            int cost = ShopRules.NextCost(_wallet, entry);
            bool maxed = level >= max;
            bool afford = !maxed && _wallet.CanAfford(cost);

            if (i < _titleTexts.Count && _titleTexts[i] != null)
                _titleTexts[i].text = i < ShopTitles.Length ? ShopTitles[i] : entry.Label.ToUpperInvariant();

            if (i < _levelTexts.Count && _levelTexts[i] != null)
            {
                string pips = BuildPips(level, max);
                if (maxed)
                {
                    _levelTexts[i].text = $"TIER {level}/{max}  {pips}  [MAXED]";
                    _levelTexts[i].color = PixelUIKit.Cyan;
                }
                else
                {
                    _levelTexts[i].text = $"TIER {level}/{max}  {pips}";
                    _levelTexts[i].color = afford ? PixelUIKit.Mint : PixelUIKit.TextMuted;
                }
            }

            if (i < _descTexts.Count && _descTexts[i] != null)
            {
                _descTexts[i].text = entry.Effect;
            }

            if (i < _statPillTexts.Count && _statPillTexts[i] != null)
            {
                _statPillTexts[i].text = GetStatDeltaPreview(i, level, max);
            }

            if (i < _priceTexts.Count && _priceTexts[i] != null)
            {
                if (maxed)
                {
                    _priceTexts[i].text = "MAXED";
                    _priceTexts[i].color = PixelUIKit.Cyan;
                }
                else
                {
                    _priceTexts[i].text = $"{cost} DB";
                    _priceTexts[i].color = afford ? PixelUIKit.Mint : PixelUIKit.Crimson;
                }
            }

            if (i < _priceBadges.Count && _priceBadges[i] != null)
            {
                if (maxed)
                    _priceBadges[i].color = new Color(0.04f, 0.16f, 0.24f, 0.95f);
                else if (afford)
                    _priceBadges[i].color = new Color(0.04f, 0.20f, 0.14f, 0.95f);
                else
                    _priceBadges[i].color = new Color(0.20f, 0.08f, 0.08f, 0.95f);
            }
        }
    }

    private static string GetStatDeltaPreview(int index, int level, int max)
    {
        switch (index)
        {
            case 0: // Wall Cling
                float curCling = 0.50f + level * 0.15f;
                float nxtCling = 0.50f + Mathf.Min(max, level + 1) * 0.15f;
                return $"HOLD: {curCling:F2}s → {nxtCling:F2}s (+0.15s)";
            case 1: // Weapon Damage
                int curDmg = level + 1;
                int nxtDmg = Mathf.Min(max, level + 1) + 1;
                return $"DMG: {curDmg} → {nxtDmg} (+1 Melee DMG)";
            case 2: // Weapon Level
                return $"TIER: MK-{level + 1} → MK-{Mathf.Min(max, level + 1) + 1} (Neon Blade)";
            case 3: // Weapon Range
                float curReach = 1.00f + level * 0.25f;
                float nxtReach = 1.00f + Mathf.Min(max, level + 1) * 0.25f;
                return $"REACH: {curReach:F2}x → {nxtReach:F2}x (+0.25m)";
            case 4: // Attack Speed
                int curSpd = 100 + level * 10;
                int nxtSpd = 100 + Mathf.Min(max, level + 1) * 10;
                return $"SWING: {curSpd}% → {nxtSpd}% (+10% SPD)";
            case 5: // Run Speed
                float curRun = 6.00f + level * 0.25f;
                float nxtRun = 6.00f + Mathf.Min(max, level + 1) * 0.25f;
                return $"SPEED: {curRun:F2} → {nxtRun:F2} (+0.25 m/s)";
            default:
                return "UPGRADE READY";
        }
    }

    private static string BuildPips(int current, int max)
    {
        var sb = new System.Text.StringBuilder("[");
        for (int i = 0; i < max; i++)
        {
            sb.Append(i < current ? "■" : "□");
        }
        sb.Append("]");
        return sb.ToString();
    }

    private void Update()
    {
        if (!_isOpen) return;

        var input = InputManager.Instance;
        if (input != null)
        {
            input.ConsumeInteract();
            input.ConsumeAttack();
        }

        // Numeric shortcuts [1-6]
        for (int i = 0; i < ShopCatalog.Entries.Length && i < 6; i++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
            {
                TryBuy(i);
                return;
            }
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            AudioManager.PlayUIClick();
            SwitchToOverclockTerminal();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Alpha0))
        {
            Close();
        }
    }

    private void Freeze()
    {
        _held = FindAnyObjectByType<PlayerController>();
        if (_held != null)
        {
            _held.isFrozen = true;
            var body = _held.GetComponent<Rigidbody2D>();
            if (body != null) body.linearVelocity = Vector2.zero;
        }
        InputManager.Instance?.ConsumeAttack();
    }

    private void Release()
    {
        if (_held != null) _held.isFrozen = false;
        _held = null;
        InputManager.Instance?.ConsumeAttack();
    }

    private void BuildCanvas()
    {
        EnsureEventSystem();
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 82;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();
        _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // 1. Semi-transparent Backdrop Dimmer
        var dimmerGo = new GameObject("Dimmer", typeof(RectTransform), typeof(Image), typeof(Button));
        dimmerGo.transform.SetParent(transform, false);
        var dimmerRt = dimmerGo.GetComponent<RectTransform>();
        dimmerRt.anchorMin = Vector2.zero; dimmerRt.anchorMax = Vector2.one;
        dimmerRt.offsetMin = Vector2.zero; dimmerRt.offsetMax = Vector2.zero;
        var dimmerImg = dimmerGo.GetComponent<Image>();
        dimmerImg.color = new Color(0.02f, 0.035f, 0.06f, 0.88f);
        var dimmerBtn = dimmerGo.GetComponent<Button>();
        dimmerBtn.onClick.AddListener(Close);

        // 2. Main Shop Panel Frame (1180 x 640)
        var panelGo = new GameObject("ShopPanel", typeof(RectTransform));
        panelGo.transform.SetParent(transform, false);
        _panelRt = panelGo.GetComponent<RectTransform>();
        _panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        _panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        _panelRt.sizeDelta = new Vector2(1180f, 640f);
        _panelRt.anchoredPosition = Vector2.zero;

        PixelUIKit.CreateSteppedPanel(
            panelGo.transform,
            "PanelBacking",
            Vector2.zero,
            Vector2.one,
            PixelUIKit.CardDark,
            PixelUIKit.BorderSlate,
            PixelUIKit.Mint,
            5);

        // 3. Left Sidebar: Unit-74 Vendor Kiosk & Telemetry (0.00 to 0.28 width)
        BuildSidebar(panelGo.transform);

        // 4. Right Content Area: 6 Module Cards (0.28 to 1.00 width)
        BuildMainContent(panelGo.transform);
    }

    private void BuildSidebar(Transform parent)
    {
        var sideGo = new GameObject("LeftSidebar", typeof(RectTransform), typeof(Image));
        sideGo.transform.SetParent(parent, false);
        var sideRt = sideGo.GetComponent<RectTransform>();
        sideRt.anchorMin = new Vector2(0f, 0f);
        sideRt.anchorMax = new Vector2(0.28f, 1f);
        sideRt.offsetMin = Vector2.zero; sideRt.offsetMax = Vector2.zero;
        var sideImg = sideGo.GetComponent<Image>();
        sideImg.color = new Color(0.025f, 0.04f, 0.07f, 0.95f);

        // Vertical Mint Divider
        var divGo = new GameObject("SideDivider", typeof(RectTransform), typeof(Image));
        divGo.transform.SetParent(parent, false);
        var divRt = divGo.GetComponent<RectTransform>();
        divRt.anchorMin = new Vector2(0.28f, 0f);
        divRt.anchorMax = new Vector2(0.282f, 1f);
        divRt.offsetMin = Vector2.zero; divRt.offsetMax = Vector2.zero;
        var divImg = divGo.GetComponent<Image>();
        divImg.color = PixelUIKit.Mint;

        // 1. Vendor Avatar & Identity Card
        var venCard = new GameObject("VendorHeaderCard", typeof(RectTransform), typeof(Image));
        venCard.transform.SetParent(sideGo.transform, false);
        var venRt = venCard.GetComponent<RectTransform>();
        venRt.anchorMin = new Vector2(0.05f, 0.81f);
        venRt.anchorMax = new Vector2(0.95f, 0.97f);
        venRt.offsetMin = Vector2.zero; venRt.offsetMax = Vector2.zero;
        var venImg = venCard.GetComponent<Image>();
        venImg.sprite = PixelUIKit.GetSteppedSprite("ven_card_bg", 48, 24, new Color32(8, 18, 32, 240), PixelUIKit.BorderSlate, new Color32(0, 0, 0, 0), 2);
        venImg.type = Image.Type.Sliced;

        // U-74 Avatar Box
        var avGo = new GameObject("AvatarBox", typeof(RectTransform), typeof(Image));
        avGo.transform.SetParent(venCard.transform, false);
        var avRt = avGo.GetComponent<RectTransform>();
        avRt.anchorMin = new Vector2(0.05f, 0.18f);
        avRt.anchorMax = new Vector2(0.24f, 0.82f);
        avRt.offsetMin = Vector2.zero; avRt.offsetMax = Vector2.zero;
        var avImg = avGo.GetComponent<Image>();
        avImg.color = new Color(0.05f, 0.16f, 0.12f, 0.95f);

        PixelUIKit.CreatePixelLabel(
            avGo.transform,
            "AvText",
            "U-74",
            Vector2.zero,
            Vector2.one,
            12,
            PixelUIKit.Mint,
            TextAnchor.MiddleCenter,
            true,
            FontStyle.Bold);

        PixelUIKit.CreatePixelLabel(
            venCard.transform,
            "VenTitle",
            "MEKANIK U-74",
            new Vector2(0.28f, 0.55f),
            new Vector2(0.95f, 0.90f),
            12,
            PixelUIKit.TextWhite,
            TextAnchor.MiddleLeft,
            false,
            FontStyle.Bold);

        PixelUIKit.CreatePixelLabel(
            venCard.transform,
            "VenSub",
            "TOKO SAFE ZONE",
            new Vector2(0.28f, 0.28f),
            new Vector2(0.95f, 0.55f),
            8,
            PixelUIKit.Mint,
            TextAnchor.MiddleLeft,
            true,
            FontStyle.Normal);

        PixelUIKit.CreatePixelLabel(
            venCard.transform,
            "VenId",
            "Siap pasang upgrade",
            new Vector2(0.28f, 0.05f),
            new Vector2(0.95f, 0.28f),
            8,
            PixelUIKit.TextDim,
            TextAnchor.MiddleLeft,
            true,
            FontStyle.Normal);

        // 2. Vendor Transmission Feed
        var transCard = new GameObject("VendorTransCard", typeof(RectTransform), typeof(Image));
        transCard.transform.SetParent(sideGo.transform, false);
        var transRt = transCard.GetComponent<RectTransform>();
        transRt.anchorMin = new Vector2(0.05f, 0.60f);
        transRt.anchorMax = new Vector2(0.95f, 0.79f);
        transRt.offsetMin = Vector2.zero; transRt.offsetMax = Vector2.zero;
        var transImg = transCard.GetComponent<Image>();
        transImg.sprite = PixelUIKit.GetSteppedSprite("ven_trans_bg", 48, 24, new Color32(10, 20, 36, 240), PixelUIKit.BorderSlate, new Color32(0, 0, 0, 0), 2);
        transImg.type = Image.Type.Sliced;

        PixelUIKit.CreatePixelLabel(
            transCard.transform,
            "TransLabel",
            "CATATAN MEKANIK",
            new Vector2(0.06f, 0.78f),
            new Vector2(0.94f, 0.95f),
            8,
            PixelUIKit.Mint,
            TextAnchor.MiddleLeft,
            true,
            FontStyle.Bold);

        PixelUIKit.CreatePixelLabel(
            transCard.transform,
            "TransBody",
            "Pilih upgrade untuk memperkuat senjata dan gerakmu sebelum keluar Safe Zone.",
            new Vector2(0.06f, 0.05f),
            new Vector2(0.94f, 0.75f),
            8,
            PixelUIKit.TextMuted,
            TextAnchor.UpperLeft,
            true,
            FontStyle.Normal);

        // 3. Courier Chassis Telemetry Card
        var telemCard = new GameObject("CourierTelemCard", typeof(RectTransform), typeof(Image));
        telemCard.transform.SetParent(sideGo.transform, false);
        var telemRt = telemCard.GetComponent<RectTransform>();
        telemRt.anchorMin = new Vector2(0.05f, 0.21f);
        telemRt.anchorMax = new Vector2(0.95f, 0.58f);
        telemRt.offsetMin = Vector2.zero; telemRt.offsetMax = Vector2.zero;
        var telemImg = telemCard.GetComponent<Image>();
        telemImg.sprite = PixelUIKit.GetSteppedSprite("ven_telem_bg", 48, 24, new Color32(7, 16, 28, 240), PixelUIKit.BorderSlate, new Color32(0, 0, 0, 0), 2);
        telemImg.type = Image.Type.Sliced;

        PixelUIKit.CreatePixelLabel(
            telemCard.transform,
            "TelemHeader",
            "STATUS KURIR",
            new Vector2(0.06f, 0.85f),
            new Vector2(0.94f, 0.98f),
            8,
            PixelUIKit.Cyan,
            TextAnchor.MiddleLeft,
            true,
            FontStyle.Bold);

        _telemTierText = PixelUIKit.CreatePixelLabel(
            telemCard.transform,
            "TelemTier",
            "WEAPON TIER:     MK-1 BASE",
            new Vector2(0.06f, 0.68f),
            new Vector2(0.94f, 0.82f),
            9,
            PixelUIKit.TextMuted,
            TextAnchor.MiddleLeft,
            true,
            FontStyle.Normal);

        _telemDmgText = PixelUIKit.CreatePixelLabel(
            telemCard.transform,
            "TelemDmg",
            "MELEE DAMAGE:    +0 BASE",
            new Vector2(0.06f, 0.52f),
            new Vector2(0.94f, 0.66f),
            9,
            PixelUIKit.TextMuted,
            TextAnchor.MiddleLeft,
            true,
            FontStyle.Normal);

        _telemSpdText = PixelUIKit.CreatePixelLabel(
            telemCard.transform,
            "TelemSpd",
            "ATTACK SPEED:    100% NOMINAL",
            new Vector2(0.06f, 0.36f),
            new Vector2(0.94f, 0.50f),
            9,
            PixelUIKit.TextMuted,
            TextAnchor.MiddleLeft,
            true,
            FontStyle.Normal);

        _telemRunText = PixelUIKit.CreatePixelLabel(
            telemCard.transform,
            "TelemRun",
            "RUN SPEED:       100% NOMINAL",
            new Vector2(0.06f, 0.20f),
            new Vector2(0.94f, 0.34f),
            9,
            PixelUIKit.TextMuted,
            TextAnchor.MiddleLeft,
            true,
            FontStyle.Normal);

        _telemClingText = PixelUIKit.CreatePixelLabel(
            telemCard.transform,
            "TelemCling",
            "WALL CLING:      0.50s BASE",
            new Vector2(0.06f, 0.04f),
            new Vector2(0.94f, 0.18f),
            9,
            PixelUIKit.TextMuted,
            TextAnchor.MiddleLeft,
            true,
            FontStyle.Normal);

        // 4. Available Credits Badge at Bottom
        var balCard = new GameObject("SideBalanceCard", typeof(RectTransform), typeof(Image));
        balCard.transform.SetParent(sideGo.transform, false);
        var balRt = balCard.GetComponent<RectTransform>();
        balRt.anchorMin = new Vector2(0.05f, 0.05f);
        balRt.anchorMax = new Vector2(0.95f, 0.18f);
        balRt.offsetMin = Vector2.zero; balRt.offsetMax = Vector2.zero;
        var balImg = balCard.GetComponent<Image>();
        balImg.sprite = PixelUIKit.GetSteppedSprite("side_bal_shop", 48, 24, new Color32(6, 26, 20, 240), PixelUIKit.BorderSlate, new Color32(0, 0, 0, 0), 2);
        balImg.type = Image.Type.Sliced;

        PixelUIKit.CreatePixelLabel(
            balCard.transform,
            "BalLabel",
            "SALDO DATA BITS",
            new Vector2(0.08f, 0.58f),
            new Vector2(0.92f, 0.90f),
            9,
            PixelUIKit.Mint,
            TextAnchor.MiddleLeft,
            true,
            FontStyle.Normal);

        _balanceText = PixelUIKit.CreatePixelLabel(
            balCard.transform,
            "BalanceValue",
            "0 DATA BITS",
            new Vector2(0.08f, 0.10f),
            new Vector2(0.92f, 0.58f),
            15,
            PixelUIKit.Mint,
            TextAnchor.MiddleLeft,
            true,
            FontStyle.Bold);
    }

    private void BuildMainContent(Transform parent)
    {
        var mainGo = new GameObject("MainContent", typeof(RectTransform));
        mainGo.transform.SetParent(parent, false);
        var mainRt = mainGo.GetComponent<RectTransform>();
        mainRt.anchorMin = new Vector2(0.283f, 0f);
        mainRt.anchorMax = new Vector2(1f, 1f);
        mainRt.offsetMin = Vector2.zero; mainRt.offsetMax = Vector2.zero;

        // Top Header
        PixelUIKit.CreatePixelLabel(
            mainGo.transform,
            "ContentHeader",
            "TOKO UPGRADE",
            new Vector2(0.03f, 0.915f),
            new Vector2(0.54f, 0.972f),
            18,
            PixelUIKit.Mint,
            TextAnchor.MiddleLeft,
            false,
            FontStyle.Bold);

        PixelUIKit.CreatePixelLabel(
            mainGo.transform,
            "ContentSub",
            "Tingkatkan senjata & gerak  •  Klik atau tekan [1-6]",
            new Vector2(0.03f, 0.873f),
            new Vector2(0.54f, 0.913f),
            10,
            PixelUIKit.TextMuted,
            TextAnchor.MiddleLeft,
            true,
            FontStyle.Normal);

        // Switch to Overclock Terminal Button (single-line crisp title)
        PixelUIKit.CreateTactileButton(
            mainGo.transform,
            "bolt",
            "CHIP SKILL",
            "",
            "TAB",
            new Vector2(0.555f, 0.885f),
            new Vector2(0.80f, 0.965f),
            PixelUIKit.ButtonTheme.PrimaryCyan,
            SwitchToOverclockTerminal);

        // Close Button (wide enough so ESC stays on one crisp line)
        PixelUIKit.CreateTactileButton(
            mainGo.transform,
            "back",
            "TUTUP",
            "",
            "ESC",
            new Vector2(0.815f, 0.885f),
            new Vector2(0.97f, 0.965f),
            PixelUIKit.ButtonTheme.SecondarySlate,
            Close);

        // Header Divider
        var divGo = new GameObject("ContentSep", typeof(RectTransform), typeof(Image));
        divGo.transform.SetParent(mainGo.transform, false);
        var divRt = divGo.GetComponent<RectTransform>();
        divRt.anchorMin = new Vector2(0.03f, 0.865f);
        divRt.anchorMax = new Vector2(0.97f, 0.868f);
        divRt.offsetMin = Vector2.zero; divRt.offsetMax = Vector2.zero;
        var divImg = divGo.GetComponent<Image>();
        divImg.color = PixelUIKit.BorderSlate;

        // 6 Tuning Modules Grid (2 Columns x 3 Rows)
        BuildUpgradeGrid(mainGo.transform);

        // Footer Bar
        BuildFooter(mainGo.transform);
    }

    private void BuildUpgradeGrid(Transform parent)
    {
        float[] yTops = { 0.84f, 0.58f, 0.32f };
        float[] yBots = { 0.60f, 0.34f, 0.08f };

        int total = ShopCatalog.Entries.Length;

        for (int i = 0; i < total; i++)
        {
            int col = i % 2; // 0 or 1
            int row = i / 2; // 0, 1, 2

            float xMin = col == 0 ? 0.03f : 0.515f;
            float xMax = col == 0 ? 0.485f : 0.97f;
            float yMin = yBots[row];
            float yMax = yTops[row];

            BuildShopCard(parent, i, new Vector2(xMin, yMin), new Vector2(xMax, yMax));
        }
    }

    private void BuildShopCard(Transform parent, int index, Vector2 aMin, Vector2 aMax)
    {
        var entry = ShopCatalog.Entries[index];
        var cardGo = new GameObject("ShopCard_" + index, typeof(RectTransform), typeof(Image), typeof(Button));
        cardGo.transform.SetParent(parent, false);
        var cardRt = cardGo.GetComponent<RectTransform>();
        cardRt.anchorMin = aMin; cardRt.anchorMax = aMax;
        cardRt.offsetMin = Vector2.zero; cardRt.offsetMax = Vector2.zero;

        var bgImg = cardGo.GetComponent<Image>();
        var normalSpr = PixelUIKit.GetSteppedSprite("card_mod_" + index, 48, 48, new Color32(12, 22, 38, 245), PixelUIKit.BorderSlate, new Color32(6, 12, 22, 255), 3);
        bgImg.sprite = normalSpr;
        bgImg.type = Image.Type.Sliced;

        var btn = cardGo.GetComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = bgImg;
        int captured = index;
        btn.onClick.AddListener(() => TryBuy(captured));
        _buttons.Add(btn);

        var contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(cardGo.transform, false);
        var contentRt = contentGo.GetComponent<RectTransform>();
        contentRt.anchorMin = Vector2.zero; contentRt.anchorMax = Vector2.one;
        contentRt.offsetMin = Vector2.zero; contentRt.offsetMax = Vector2.zero;

        Color themeColor = index < ShopColors.Length ? ShopColors[index] : PixelUIKit.Mint;

        // Top Accent Stripe
        var topStripe = new GameObject("TopStripe", typeof(RectTransform), typeof(Image));
        topStripe.transform.SetParent(contentGo.transform, false);
        var stripeRt = topStripe.GetComponent<RectTransform>();
        stripeRt.anchorMin = new Vector2(0f, 0.97f);
        stripeRt.anchorMax = new Vector2(1f, 1f);
        stripeRt.offsetMin = Vector2.zero; stripeRt.offsetMax = Vector2.zero;
        var stripeImg = topStripe.GetComponent<Image>();
        stripeImg.color = themeColor;
        stripeImg.raycastTarget = false;

        // Keycap Badge [1..6]
        var keycapGo = new GameObject("Keycap", typeof(RectTransform), typeof(Image));
        keycapGo.transform.SetParent(contentGo.transform, false);
        var keycapRt = keycapGo.GetComponent<RectTransform>();
        keycapRt.anchorMin = new Vector2(0.03f, 0.68f);
        keycapRt.anchorMax = new Vector2(0.12f, 0.90f);
        keycapRt.offsetMin = Vector2.zero; keycapRt.offsetMax = Vector2.zero;
        var keycapImg = keycapGo.GetComponent<Image>();
        keycapImg.color = new Color(0.04f, 0.08f, 0.16f, 0.95f);
        keycapImg.raycastTarget = false;

        PixelUIKit.CreatePixelLabel(
            keycapGo.transform,
            "KeyText",
            (index + 1).ToString(),
            Vector2.zero,
            Vector2.one,
            11,
            themeColor,
            TextAnchor.MiddleCenter,
            true,
            FontStyle.Bold);

        // Title Label
        string titleStr = index < ShopTitles.Length ? ShopTitles[index] : entry.Label.ToUpperInvariant();
        var titleText = PixelUIKit.CreatePixelLabel(
            contentGo.transform,
            "Title",
            titleStr,
            new Vector2(0.14f, 0.68f),
            new Vector2(0.70f, 0.92f),
            12,
            PixelUIKit.TextWhite,
            TextAnchor.MiddleLeft,
            false,
            FontStyle.Bold);
        _titleTexts.Add(titleText);

        // Category Tag
        string catStr = index < ShopCategories.Length ? ShopCategories[index] : "HARDWARE TUNING";
        PixelUIKit.CreatePixelLabel(
            contentGo.transform,
            "Category",
            catStr,
            new Vector2(0.14f, 0.54f),
            new Vector2(0.70f, 0.68f),
            8,
            PixelUIKit.TextDim,
            TextAnchor.MiddleLeft,
            true,
            FontStyle.Normal);

        // Description / Effect
        var descText = PixelUIKit.CreatePixelLabel(
            contentGo.transform,
            "Desc",
            entry.Effect,
            new Vector2(0.04f, 0.38f),
            new Vector2(0.70f, 0.54f),
            8,
            PixelUIKit.TextMuted,
            TextAnchor.MiddleLeft,
            true,
            FontStyle.Normal);
        _descTexts.Add(descText);

        // Stat Delta Preview Pill
        var pillGo = new GameObject("StatPill", typeof(RectTransform), typeof(Image));
        pillGo.transform.SetParent(contentGo.transform, false);
        var pillRt = pillGo.GetComponent<RectTransform>();
        pillRt.anchorMin = new Vector2(0.04f, 0.18f);
        pillRt.anchorMax = new Vector2(0.68f, 0.36f);
        pillRt.offsetMin = Vector2.zero; pillRt.offsetMax = Vector2.zero;
        var pillImg = pillGo.GetComponent<Image>();
        pillImg.color = new Color(0.05f, 0.10f, 0.18f, 0.95f);
        pillImg.raycastTarget = false;

        var pillText = PixelUIKit.CreatePixelLabel(
            pillGo.transform,
            "PillText",
            GetStatDeltaPreview(index, 0, _wallet.GetMaxLevel(index)),
            new Vector2(0.04f, 0f),
            new Vector2(0.96f, 1f),
            8,
            themeColor,
            TextAnchor.MiddleLeft,
            true,
            FontStyle.Normal);
        _statPillTexts.Add(pillText);

        // Tier Pips Label
        var levelText = PixelUIKit.CreatePixelLabel(
            contentGo.transform,
            "LevelPips",
            $"TIER 0/{_wallet.GetMaxLevel(index)}  [□□]",
            new Vector2(0.04f, 0.04f),
            new Vector2(0.68f, 0.18f),
            8,
            themeColor,
            TextAnchor.MiddleLeft,
            true,
            FontStyle.Bold);
        _levelTexts.Add(levelText);

        // Buy Button / Price Tag on Right (directly clickable!)
        var priceBadgeGo = new GameObject("PriceBadge", typeof(RectTransform), typeof(Image), typeof(Button));
        priceBadgeGo.transform.SetParent(contentGo.transform, false);
        var priceBadgeRt = priceBadgeGo.GetComponent<RectTransform>();
        priceBadgeRt.anchorMin = new Vector2(0.72f, 0.18f);
        priceBadgeRt.anchorMax = new Vector2(0.97f, 0.82f);
        priceBadgeRt.offsetMin = Vector2.zero; priceBadgeRt.offsetMax = Vector2.zero;
        var priceBadgeImg = priceBadgeGo.GetComponent<Image>();
        priceBadgeImg.sprite = PixelUIKit.GetSteppedSprite("price_badge_shop_" + index, 40, 24, new Color32(10, 24, 28, 240), PixelUIKit.BorderSlate, new Color32(0, 0, 0, 0), 2);
        priceBadgeImg.type = Image.Type.Sliced;
        var priceBadgeBtn = priceBadgeGo.GetComponent<Button>();
        priceBadgeBtn.transition = Selectable.Transition.None;
        priceBadgeBtn.targetGraphic = priceBadgeImg;
        priceBadgeBtn.onClick.AddListener(() => TryBuy(captured));
        _priceBadges.Add(priceBadgeImg);

        var priceText = PixelUIKit.CreatePixelLabel(
            priceBadgeGo.transform,
            "PriceText",
            "0 DB",
            new Vector2(0f, 0.35f),
            new Vector2(1f, 0.90f),
            12,
            PixelUIKit.Mint,
            TextAnchor.MiddleCenter,
            true,
            FontStyle.Bold);
        _priceTexts.Add(priceText);

        PixelUIKit.CreatePixelLabel(
            priceBadgeGo.transform,
            "ActionLabel",
            "BELI",
            new Vector2(0f, 0.10f),
            new Vector2(1f, 0.35f),
            8,
            PixelUIKit.TextDim,
            TextAnchor.MiddleCenter,
            true,
            FontStyle.Normal);

        // Tactile Component for press offset & sound
        if (Application.isPlaying)
        {
            var tactile = cardGo.AddComponent<TactilePixelButton>();
            tactile.hideFlags = HideFlags.DontSave;
            tactile.bgImage = bgImg;
            tactile.contentRt = contentRt;
            tactile.normalSprite = normalSpr;
            tactile.hoverSprite = PixelUIKit.GetSteppedSprite("card_mod_h_" + index, 48, 48, new Color32(18, 36, 52, 250), themeColor, new Color32(8, 20, 30, 255), 3);
            tactile.pressedSprite = PixelUIKit.GetSteppedSprite("card_mod_p_" + index, 48, 48, new Color32(8, 18, 26, 255), themeColor, new Color32(4, 10, 16, 255), 3);
            tactile.titleText = titleText;
            tactile.subText = levelText;
            tactile.normalTitleColor = PixelUIKit.TextWhite;
            tactile.hoverTitleColor = themeColor;
            tactile.pressedTitleColor = PixelUIKit.Mint;
        }
    }

    private void BuildFooter(Transform parent)
    {
        var footerGo = new GameObject("FooterBar", typeof(RectTransform), typeof(Image));
        footerGo.transform.SetParent(parent, false);
        var footerRt = footerGo.GetComponent<RectTransform>();
        footerRt.anchorMin = new Vector2(0.03f, 0.015f);
        footerRt.anchorMax = new Vector2(0.97f, 0.065f);
        footerRt.offsetMin = Vector2.zero; footerRt.offsetMax = Vector2.zero;
        var footerImg = footerGo.GetComponent<Image>();
        footerImg.sprite = PixelUIKit.GetSteppedSprite("footer_strip_shop", 32, 24, new Color32(10, 16, 28, 230), PixelUIKit.BorderSlate, new Color32(0, 0, 0, 0), 2);
        footerImg.type = Image.Type.Sliced;

        _footerHint = PixelUIKit.CreatePixelLabel(
            footerGo.transform,
            "FooterHint",
            "[1-6] Beli Upgrade  •  [TAB] Chip Skill  •  [ESC] Tutup",
            Vector2.zero,
            Vector2.one,
            10,
            PixelUIKit.TextDim,
            TextAnchor.MiddleCenter,
            true,
            FontStyle.Bold);
    }

    public int GetLevel(int upgradeIndex) => _wallet.GetLevel(upgradeIndex);
    public int GetMaxLevel(int upgradeIndex) => _wallet.GetMaxLevel(upgradeIndex);
    public int GetCost(int upgradeIndex, int nextLevel) => _wallet.GetCost(upgradeIndex, nextLevel);
    public bool CanAfford(int amount) => _wallet.CanAfford(amount);
    public PurchaseResult Buy(int upgradeIndex) => _wallet.Buy(upgradeIndex);
    public bool TrySpend(int amount) => _wallet.TrySpend(amount);

    private sealed class GameManagerShopWallet : IUpgradeWallet
    {
        private static readonly int[] MaxLevels = { 2, 5, 2, 3, 3, 3 };
        private static readonly int[][] Costs = {
            new[] { 100, 250 },                       // 0: Wall Cling
            new[] { 150, 300, 500, 750, 1000 },       // 1: Weapon Damage
            new[] { 200, 450 },                       // 2: Weapon Level
            new[] { 120, 250, 450 },                  // 3: Weapon Range
            new[] { 150, 300, 500 },                  // 4: Attack Speed
            new[] { 150, 300, 500 }                   // 5: Run Speed
        };

        public int GetLevel(int upgradeIndex)
        {
            var gm = GameManager.Instance;
            if (gm == null || upgradeIndex < 0 || upgradeIndex >= ShopCatalog.Entries.Length) return 0;
            if (upgradeIndex == 0) return gm.GetUpgradeLevel(GameManager.UpgradeType.WallClingTime);
            return gm.shopLevels[upgradeIndex];
        }

        public int GetMaxLevel(int upgradeIndex)
        {
            if (upgradeIndex < 0 || upgradeIndex >= MaxLevels.Length) return 1;
            return MaxLevels[upgradeIndex];
        }

        public int GetCost(int upgradeIndex, int nextLevel)
        {
            if (upgradeIndex < 0 || upgradeIndex >= Costs.Length) return 9999;
            var list = Costs[upgradeIndex];
            int idx = nextLevel - 1;
            if (idx >= 0 && idx < list.Length) return list[idx];
            return 9999;
        }

        public bool CanAfford(int amount)
        {
            var gm = GameManager.Instance;
            return gm != null && gm.AvailableDataBits >= amount;
        }

        public PurchaseResult Buy(int upgradeIndex)
        {
            if (upgradeIndex < 0 || upgradeIndex >= ShopCatalog.Entries.Length) return PurchaseResult.InsufficientFunds;
            var gm = GameManager.Instance;
            if (gm == null) return PurchaseResult.InsufficientFunds;

            int currentLevel = GetLevel(upgradeIndex);
            if (currentLevel >= GetMaxLevel(upgradeIndex)) return PurchaseResult.MaxLevel;

            int cost = GetCost(upgradeIndex, currentLevel + 1);
            if (!gm.SpendDataBits(cost)) return PurchaseResult.InsufficientFunds;

            if (upgradeIndex == 0)
            {
                gm.upgradeLevels[(int)GameManager.UpgradeType.WallClingTime]++;
            }
            else
            {
                gm.shopLevels[upgradeIndex]++;
            }
            return PurchaseResult.Success;
        }

        public bool TrySpend(int amount)
        {
            var gm = GameManager.Instance;
            return gm != null && gm.SpendDataBits(amount);
        }
    }
}

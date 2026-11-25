using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Tactical Cyberpunk Overclock Terminal UI with handcrafted pixel-art visuals.
/// Redesigned to match the FigCraft terminal layout with category sidebar,
/// prominent numeric chip sockets, visual progress bars, and tactile buy actions.
/// </summary>
public class OverclockUI : MonoBehaviour
{
    public static OverclockUI Instance { get; private set; }
    public bool IsOpen => _isOpen;
    public static bool IsOverclockOpen => Instance != null && Instance._isOpen;

    private bool _isOpen;
    private object _owner;
    private PlayerController _held;

    private Canvas _canvas;
    private CanvasGroup _canvasGroup;
    private RectTransform _panelRt;
    private Text _balanceText;
    private Text _footerHint;

    private readonly List<Button> _buttons = new List<Button>();
    private readonly List<Text> _titleTexts = new List<Text>();
    private readonly List<Text> _levelTexts = new List<Text>();
    private readonly List<Text> _descTexts = new List<Text>();
    private readonly List<Text> _priceTexts = new List<Text>();
    private readonly List<Image> _priceBadges = new List<Image>();
    private readonly List<RectTransform> _barFills = new List<RectTransform>();

    private readonly GameManagerOverclockWallet _wallet = new GameManagerOverclockWallet();
    private Coroutine _animCoroutine;

    private static readonly string[] CardIcons = {
        "play",      // 0: Triple Jump
        "bolt",      // 1: Cyber Blaster
        "shield",    // 2: Nano Parry
        "skull",     // 3: Core Damage
        "restart",   // 4: Phase Dash
        "power",     // 5: Magnetic Pull
        "retry",     // 6: Turbo Battery
        "terminal"   // 7: Emergency Buffer
    };

    private static readonly Color[] CardColors = {
        new Color(0.00f, 0.94f, 1.00f), // 0: Cyan (Mobility)
        new Color(0.94f, 0.48f, 0.16f), // 1: Orange (Combat)
        new Color(0.94f, 0.48f, 0.16f), // 2: Orange (Combat)
        new Color(0.94f, 0.48f, 0.16f), // 3: Orange (Combat)
        new Color(0.00f, 0.94f, 1.00f), // 4: Cyan (Mobility)
        new Color(0.21f, 0.96f, 0.61f), // 5: Mint (Utility)
        new Color(0.21f, 0.96f, 0.61f), // 6: Mint (Utility)
        new Color(0.21f, 0.96f, 0.61f)  // 7: Mint (Utility)
    };

    public static OverclockUI Ensure()
    {
        if (Instance != null) return Instance;
        return new GameObject("Overclock_Canvas").AddComponent<OverclockUI>();
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

    public void SwitchToHardwareShop()
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
        ShopUI.Ensure().Open(savedOwner);
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
        if (index < 0 || index >= OverclockCatalog.Entries.Length) return;
        var entry = OverclockCatalog.Entries[index];
        var result = OverclockCatalog.Buy(_wallet, entry);

        if (result == PurchaseResult.Success)
        {
            AudioManager.PlayUIClick();
            AudioManager.Play("sfx_collect", 0.9f, 1.25f);
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
            _balanceText.text = $"{balance:N0} DB";

        for (int i = 0; i < OverclockCatalog.Entries.Length && i < _buttons.Count; i++)
        {
            var entry = OverclockCatalog.Entries[i];
            int level = _wallet.GetLevel(i);
            int max = entry.MaxLevel;
            int cost = OverclockCatalog.NextCost(_wallet, entry);
            bool maxed = level >= max;
            bool afford = !maxed && _wallet.CanAfford(cost);

            if (i < _titleTexts.Count && _titleTexts[i] != null)
                _titleTexts[i].text = entry.Name.ToUpperInvariant();

            if (i < _levelTexts.Count && _levelTexts[i] != null)
            {
                if (maxed)
                {
                    _levelTexts[i].text = $"LV {level}/{max}  [MAX INSTALLED]";
                    _levelTexts[i].color = PixelUIKit.Cyan;
                }
                else if (afford)
                {
                    _levelTexts[i].text = level > 0 ? $"LV {level}/{max}  — UPGRADE READY" : $"LV {level}/{max}  — NOT INSTALLED";
                    _levelTexts[i].color = level > 0 ? PixelUIKit.Mint : PixelUIKit.TextMuted;
                }
                else
                {
                    _levelTexts[i].text = $"LV {level}/{max}  — LOCKED (NEED {cost} DB)";
                    _levelTexts[i].color = PixelUIKit.Crimson;
                }
            }

            if (i < _barFills.Count && _barFills[i] != null)
            {
                float frac = max > 0 ? Mathf.Clamp01((float)level / max) : 0f;
                _barFills[i].anchorMax = new Vector2(frac, 1f);
            }

            if (i < _descTexts.Count && _descTexts[i] != null)
            {
                _descTexts[i].text = $"{entry.Effect} // {entry.ControlsHint}";
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

    private void Update()
    {
        if (!_isOpen) return;

        var input = InputManager.Instance;
        if (input != null)
        {
            input.ConsumeInteract();
            input.ConsumeAttack();
        }

        // Numeric shortcuts [1-8]
        for (int i = 0; i < OverclockCatalog.Entries.Length && i < 8; i++)
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
            SwitchToHardwareShop();
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

        // 2. Main Terminal Panel Frame (1180 x 640)
        var panelGo = new GameObject("OverclockPanel", typeof(RectTransform));
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
            PixelUIKit.CyanDark,
            5);

        // 3. Left Sidebar (0.00 to 0.20 width)
        BuildSidebar(panelGo.transform);

        // 4. Right Content Area (0.20 to 1.00 width)
        BuildMainContent(panelGo.transform);
    }

    private void BuildSidebar(Transform parent)
    {
        var sideGo = new GameObject("LeftSidebar", typeof(RectTransform), typeof(Image));
        sideGo.transform.SetParent(parent, false);
        var sideRt = sideGo.GetComponent<RectTransform>();
        sideRt.anchorMin = new Vector2(0f, 0f);
        sideRt.anchorMax = new Vector2(0.20f, 1f);
        sideRt.offsetMin = Vector2.zero; sideRt.offsetMax = Vector2.zero;
        var sideImg = sideGo.GetComponent<Image>();
        sideImg.color = new Color(0.025f, 0.04f, 0.07f, 0.95f);

        // Vertical Accent Divider
        var divGo = new GameObject("SideDivider", typeof(RectTransform), typeof(Image));
        divGo.transform.SetParent(parent, false);
        var divRt = divGo.GetComponent<RectTransform>();
        divRt.anchorMin = new Vector2(0.20f, 0f);
        divRt.anchorMax = new Vector2(0.202f, 1f);
        divRt.offsetMin = Vector2.zero; divRt.offsetMax = Vector2.zero;
        var divImg = divGo.GetComponent<Image>();
        divImg.color = PixelUIKit.Cyan;

        // Overclock Title in Sidebar
        PixelUIKit.CreatePixelLabel(
            sideGo.transform,
            "SideTitle",
            "OVERCLOCK",
            new Vector2(0.08f, 0.92f),
            new Vector2(0.92f, 0.97f),
            16,
            PixelUIKit.Cyan,
            TextAnchor.MiddleLeft,
            false,
            FontStyle.Bold);

        PixelUIKit.CreatePixelLabel(
            sideGo.transform,
            "SideSub",
            "TERMINAL // v2.4",
            new Vector2(0.08f, 0.88f),
            new Vector2(0.92f, 0.92f),
            10,
            PixelUIKit.Mint,
            TextAnchor.MiddleLeft,
            true,
            FontStyle.Bold);

        // Category Tag
        PixelUIKit.CreatePixelLabel(
            sideGo.transform,
            "CategoryHeader",
            "CATEGORY ARCHITECTURE",
            new Vector2(0.08f, 0.78f),
            new Vector2(0.92f, 0.82f),
            9,
            PixelUIKit.TextDim,
            TextAnchor.MiddleLeft,
            true,
            FontStyle.Normal);

        // Tab: Mobility
        BuildSidebarTab(sideGo.transform, "MOBILITY", "CHIPS [1, 5]", 0.70f, PixelUIKit.Cyan, true);
        // Tab: Combat
        BuildSidebarTab(sideGo.transform, "COMBAT", "CHIPS [2, 3, 4]", 0.58f, PixelUIKit.OrangeLight, false);
        // Tab: Utility
        BuildSidebarTab(sideGo.transform, "UTILITY", "CHIPS [6, 7, 8]", 0.46f, PixelUIKit.Mint, false);

        // Live Balance Card at Sidebar Bottom
        var balCard = new GameObject("SideBalanceCard", typeof(RectTransform), typeof(Image));
        balCard.transform.SetParent(sideGo.transform, false);
        var balRt = balCard.GetComponent<RectTransform>();
        balRt.anchorMin = new Vector2(0.06f, 0.05f);
        balRt.anchorMax = new Vector2(0.94f, 0.16f);
        balRt.offsetMin = Vector2.zero; balRt.offsetMax = Vector2.zero;
        var balImg = balCard.GetComponent<Image>();
        balImg.sprite = PixelUIKit.GetSteppedSprite("side_bal_bg", 48, 24, new Color32(6, 26, 20, 240), PixelUIKit.BorderSlate, new Color32(0, 0, 0, 0), 2);
        balImg.type = Image.Type.Sliced;

        PixelUIKit.CreatePixelLabel(
            balCard.transform,
            "BalLabel",
            "DATA BITS AVAILABLE",
            new Vector2(0.08f, 0.55f),
            new Vector2(0.92f, 0.90f),
            9,
            PixelUIKit.Mint,
            TextAnchor.MiddleLeft,
            true,
            FontStyle.Normal);

        _balanceText = PixelUIKit.CreatePixelLabel(
            balCard.transform,
            "BalanceValue",
            "0 DB",
            new Vector2(0.08f, 0.10f),
            new Vector2(0.92f, 0.55f),
            16,
            PixelUIKit.Mint,
            TextAnchor.MiddleLeft,
            true,
            FontStyle.Bold);
    }

    private void BuildSidebarTab(Transform parent, string title, string sub, float topY, Color color, bool active)
    {
        var tabGo = new GameObject("Tab_" + title, typeof(RectTransform), typeof(Image));
        tabGo.transform.SetParent(parent, false);
        var tabRt = tabGo.GetComponent<RectTransform>();
        tabRt.anchorMin = new Vector2(0.05f, topY - 0.09f);
        tabRt.anchorMax = new Vector2(0.95f, topY);
        tabRt.offsetMin = Vector2.zero; tabRt.offsetMax = Vector2.zero;
        var tabImg = tabGo.GetComponent<Image>();
        tabImg.color = active ? new Color(0.06f, 0.14f, 0.24f, 0.85f) : new Color(0.03f, 0.06f, 0.10f, 0.5f);

        if (active)
        {
            var stripGo = new GameObject("ActiveStrip", typeof(RectTransform), typeof(Image));
            stripGo.transform.SetParent(tabGo.transform, false);
            var stripRt = stripGo.GetComponent<RectTransform>();
            stripRt.anchorMin = new Vector2(0f, 0f);
            stripRt.anchorMax = new Vector2(0.04f, 1f);
            stripRt.offsetMin = Vector2.zero; stripRt.offsetMax = Vector2.zero;
            var stripImg = stripGo.GetComponent<Image>();
            stripImg.color = color;
        }

        PixelUIKit.CreatePixelLabel(
            tabGo.transform,
            "TabTitle",
            title,
            new Vector2(0.10f, 0.48f),
            new Vector2(0.95f, 0.90f),
            11,
            color,
            TextAnchor.MiddleLeft,
            true,
            FontStyle.Bold);

        PixelUIKit.CreatePixelLabel(
            tabGo.transform,
            "TabSub",
            sub,
            new Vector2(0.10f, 0.12f),
            new Vector2(0.95f, 0.48f),
            9,
            PixelUIKit.TextDim,
            TextAnchor.MiddleLeft,
            true,
            FontStyle.Normal);
    }

    private void BuildMainContent(Transform parent)
    {
        var mainGo = new GameObject("MainContent", typeof(RectTransform));
        mainGo.transform.SetParent(parent, false);
        var mainRt = mainGo.GetComponent<RectTransform>();
        mainRt.anchorMin = new Vector2(0.203f, 0f);
        mainRt.anchorMax = new Vector2(1f, 1f);
        mainRt.offsetMin = Vector2.zero; mainRt.offsetMax = Vector2.zero;

        // Top Header
        PixelUIKit.CreatePixelLabel(
            mainGo.transform,
            "ContentHeader",
            "OVERCLOCK TERMINAL",
            new Vector2(0.03f, 0.915f),
            new Vector2(0.55f, 0.972f),
            18,
            PixelUIKit.Cyan,
            TextAnchor.MiddleLeft,
            false,
            FontStyle.Bold);

        PixelUIKit.CreatePixelLabel(
            mainGo.transform,
            "ContentSub",
            "PASANG CHIP KEMAMPUAN PERMANEN // KLIK CHIP ATAU TEKAN [1-3]",
            new Vector2(0.03f, 0.873f),
            new Vector2(0.55f, 0.913f),
            10,
            PixelUIKit.TextMuted,
            TextAnchor.MiddleLeft,
            true,
            FontStyle.Normal);

        // Switch to Hardware Tech Shop Button (single-line crisp title)
        PixelUIKit.CreateTactileButton(
            mainGo.transform,
            "shield",
            "TECH SHOP",
            "",
            "TAB",
            new Vector2(0.57f, 0.885f),
            new Vector2(0.825f, 0.965f),
            PixelUIKit.ButtonTheme.PrimaryCyan,
            SwitchToHardwareShop);

        // Close Button
        PixelUIKit.CreateTactileButton(
            mainGo.transform,
            "back",
            "TUTUP",
            "",
            "ESC",
            new Vector2(0.84f, 0.885f),
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

        // 8 Chip Cards Grid (2 Columns x 4 Rows)
        BuildUpgradeGrid(mainGo.transform);

        // Footer Bar
        BuildFooter(mainGo.transform);
    }

    private void BuildUpgradeGrid(Transform parent)
    {
        float[] yTops = { 0.84f, 0.655f, 0.47f, 0.285f };
        float[] yBots = { 0.675f, 0.49f, 0.305f, 0.12f };

        int total = OverclockCatalog.Entries.Length;

        for (int i = 0; i < total; i++)
        {
            int col = i / 4; // 0 or 1
            int row = i % 4; // 0..3

            float xMin = col == 0 ? 0.03f : 0.515f;
            float xMax = col == 0 ? 0.485f : 0.97f;
            float yMin = yBots[row];
            float yMax = yTops[row];

            BuildUpgradeCard(parent, i, new Vector2(xMin, yMin), new Vector2(xMax, yMax));
        }
    }

    private void BuildUpgradeCard(Transform parent, int index, Vector2 aMin, Vector2 aMax)
    {
        var entry = OverclockCatalog.Entries[index];
        var cardGo = new GameObject("OverclockCard_" + index, typeof(RectTransform), typeof(Image), typeof(Button));
        cardGo.transform.SetParent(parent, false);
        var cardRt = cardGo.GetComponent<RectTransform>();
        cardRt.anchorMin = aMin; cardRt.anchorMax = aMax;
        cardRt.offsetMin = Vector2.zero; cardRt.offsetMax = Vector2.zero;

        var bgImg = cardGo.GetComponent<Image>();
        var normalSpr = PixelUIKit.GetSteppedSprite("card_chip_" + index, 48, 48, new Color32(12, 22, 38, 245), PixelUIKit.BorderSlate, new Color32(6, 12, 22, 255), 3);
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

        Color themeColor = index < CardColors.Length ? CardColors[index] : PixelUIKit.Cyan;

        // 1. Large Numeric Block on Left
        var numBlock = new GameObject("NumBlock", typeof(RectTransform), typeof(Image));
        numBlock.transform.SetParent(contentGo.transform, false);
        var numRt = numBlock.GetComponent<RectTransform>();
        numRt.anchorMin = new Vector2(0.02f, 0.12f);
        numRt.anchorMax = new Vector2(0.14f, 0.88f);
        numRt.offsetMin = Vector2.zero; numRt.offsetMax = Vector2.zero;
        var numImg = numBlock.GetComponent<Image>();
        numImg.color = new Color(0.04f, 0.08f, 0.16f, 0.95f);
        numImg.raycastTarget = false;

        // Top Accent Line on NumBlock
        var numAccent = new GameObject("NumAccent", typeof(RectTransform), typeof(Image));
        numAccent.transform.SetParent(numBlock.transform, false);
        var accentRt = numAccent.GetComponent<RectTransform>();
        accentRt.anchorMin = new Vector2(0f, 0.94f);
        accentRt.anchorMax = new Vector2(1f, 1f);
        accentRt.offsetMin = Vector2.zero; accentRt.offsetMax = Vector2.zero;
        var accentImg = numAccent.GetComponent<Image>();
        accentImg.color = themeColor;
        accentImg.raycastTarget = false;

        PixelUIKit.CreatePixelLabel(
            numBlock.transform,
            "KeyNum",
            (index + 1).ToString(),
            new Vector2(0f, 0.25f),
            new Vector2(1f, 0.95f),
            18,
            themeColor,
            TextAnchor.MiddleCenter,
            true,
            FontStyle.Bold);

        PixelUIKit.CreatePixelLabel(
            numBlock.transform,
            "KeyLabel",
            "[KEY]",
            new Vector2(0f, 0.02f),
            new Vector2(1f, 0.30f),
            8,
            PixelUIKit.TextDim,
            TextAnchor.MiddleCenter,
            true,
            FontStyle.Normal);

        // 2. Title Label
        var titleText = PixelUIKit.CreatePixelLabel(
            contentGo.transform,
            "Title",
            entry.Name.ToUpperInvariant(),
            new Vector2(0.16f, 0.60f),
            new Vector2(0.68f, 0.92f),
            13,
            PixelUIKit.TextWhite,
            TextAnchor.MiddleLeft,
            false,
            FontStyle.Bold);
        _titleTexts.Add(titleText);

        // 3. Level Progress Bar Track & Fill
        var barTrack = new GameObject("LvBarTrack", typeof(RectTransform), typeof(Image));
        barTrack.transform.SetParent(contentGo.transform, false);
        var trackRt = barTrack.GetComponent<RectTransform>();
        trackRt.anchorMin = new Vector2(0.16f, 0.44f);
        trackRt.anchorMax = new Vector2(0.68f, 0.54f);
        trackRt.offsetMin = Vector2.zero; trackRt.offsetMax = Vector2.zero;
        var trackImg = barTrack.GetComponent<Image>();
        trackImg.color = new Color(0.08f, 0.14f, 0.22f, 1f);
        trackImg.raycastTarget = false;

        var barFill = new GameObject("LvBarFill", typeof(RectTransform), typeof(Image));
        barFill.transform.SetParent(barTrack.transform, false);
        var fillRt = barFill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(0f, 1f); // Updated in Refresh
        fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;
        var fillImg = barFill.GetComponent<Image>();
        fillImg.color = themeColor;
        fillImg.raycastTarget = false;
        _barFills.Add(fillRt);

        // 4. Status / Level Text
        var levelText = PixelUIKit.CreatePixelLabel(
            contentGo.transform,
            "LevelStatus",
            "LV 0/1 — NOT INSTALLED",
            new Vector2(0.16f, 0.26f),
            new Vector2(0.68f, 0.42f),
            9,
            PixelUIKit.TextMuted,
            TextAnchor.MiddleLeft,
            true,
            FontStyle.Bold);
        _levelTexts.Add(levelText);

        // 5. Description / Hint
        var descText = PixelUIKit.CreatePixelLabel(
            contentGo.transform,
            "Desc",
            $"{entry.Effect} // {entry.ControlsHint}",
            new Vector2(0.16f, 0.08f),
            new Vector2(0.68f, 0.24f),
            8,
            PixelUIKit.TextDim,
            TextAnchor.MiddleLeft,
            true,
            FontStyle.Normal);
        _descTexts.Add(descText);

        // 6. Buy Button / Price Badge on Right (directly clickable!)
        var priceBadgeGo = new GameObject("PriceBadge", typeof(RectTransform), typeof(Image), typeof(Button));
        priceBadgeGo.transform.SetParent(contentGo.transform, false);
        var priceBadgeRt = priceBadgeGo.GetComponent<RectTransform>();
        priceBadgeRt.anchorMin = new Vector2(0.70f, 0.18f);
        priceBadgeRt.anchorMax = new Vector2(0.97f, 0.82f);
        priceBadgeRt.offsetMin = Vector2.zero; priceBadgeRt.offsetMax = Vector2.zero;
        var priceBadgeImg = priceBadgeGo.GetComponent<Image>();
        priceBadgeImg.sprite = PixelUIKit.GetSteppedSprite("price_badge_bg_" + index, 40, 24, new Color32(10, 24, 38, 240), PixelUIKit.BorderSlate, new Color32(0, 0, 0, 0), 2);
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
            "PASANG / INSTALL",
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
            tactile.hoverSprite = PixelUIKit.GetSteppedSprite("card_chip_h_" + index, 48, 48, new Color32(18, 34, 58, 250), themeColor, new Color32(8, 16, 28, 255), 3);
            tactile.pressedSprite = PixelUIKit.GetSteppedSprite("card_chip_p_" + index, 48, 48, new Color32(8, 16, 28, 255), themeColor, new Color32(4, 8, 16, 255), 3);
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
        footerRt.anchorMin = new Vector2(0.03f, 0.02f);
        footerRt.anchorMax = new Vector2(0.97f, 0.08f);
        footerRt.offsetMin = Vector2.zero; footerRt.offsetMax = Vector2.zero;
        var footerImg = footerGo.GetComponent<Image>();
        footerImg.sprite = PixelUIKit.GetSteppedSprite("footer_strip_oc", 32, 24, new Color32(10, 16, 28, 230), PixelUIKit.BorderSlate, new Color32(0, 0, 0, 0), 2);
        footerImg.type = Image.Type.Sliced;

        _footerHint = PixelUIKit.CreatePixelLabel(
            footerGo.transform,
            "FooterHint",
            "[1-3] PASANG CHIP   |   [KLIK] AKTIFKAN MODUL   |   [TAB] HARDWARE TECH SHOP   |   [ESC / 0] TUTUP",
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

    private sealed class GameManagerOverclockWallet : IUpgradeWallet
    {
        public int GetLevel(int upgradeIndex)
        {
            if (upgradeIndex < 0 || upgradeIndex >= OverclockCatalog.Entries.Length) return 0;
            return OverclockManager.GetLevel((OverclockId)upgradeIndex);
        }

        public int GetMaxLevel(int upgradeIndex)
        {
            if (upgradeIndex < 0 || upgradeIndex >= OverclockCatalog.Entries.Length) return 1;
            return OverclockCatalog.Entries[upgradeIndex].MaxLevel;
        }

        public int GetCost(int upgradeIndex, int nextLevel)
        {
            if (upgradeIndex < 0 || upgradeIndex >= OverclockCatalog.Entries.Length) return 9999;
            var entry = OverclockCatalog.Entries[upgradeIndex];
            if (nextLevel - 1 < 0 || nextLevel - 1 >= entry.Costs.Length) return 9999;
            return entry.Costs[nextLevel - 1];
        }

        public bool CanAfford(int amount)
        {
            var gm = GameManager.Instance;
            return gm != null && gm.AvailableDataBits >= amount;
        }

        public PurchaseResult Buy(int upgradeIndex)
        {
            if (upgradeIndex < 0 || upgradeIndex >= OverclockCatalog.Entries.Length) return PurchaseResult.InsufficientFunds;
            var entry = OverclockCatalog.Entries[upgradeIndex];
            int currentLevel = GetLevel(upgradeIndex);
            if (currentLevel >= entry.MaxLevel) return PurchaseResult.MaxLevel;

            int cost = GetCost(upgradeIndex, currentLevel + 1);
            var gm = GameManager.Instance;
            if (gm == null || !gm.SpendDataBits(cost)) return PurchaseResult.InsufficientFunds;

            OverclockManager.SetLevel((OverclockId)upgradeIndex, currentLevel + 1);
            return PurchaseResult.Success;
        }

        public bool TrySpend(int amount)
        {
            var gm = GameManager.Instance;
            return gm != null && gm.SpendDataBits(amount);
        }
    }
}

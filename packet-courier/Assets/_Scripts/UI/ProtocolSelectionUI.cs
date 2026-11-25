using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Modal UI for selecting packet transport protocols and network frame modifiers.
/// Displays speed, jump, and reward mechanics along with authentic networking concepts.
/// </summary>
public class ProtocolSelectionUI : MonoBehaviour
{
    public static ProtocolSelectionUI Instance { get; private set; }

    private Canvas _canvas;
    private GameObject _panel;
    private Text _statusText;

    public static ProtocolSelectionUI Ensure()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("ProtocolSelectionUI");
        return go.AddComponent<ProtocolSelectionUI>();
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
        BuildUI();
    }

    private void BuildUI()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 940;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        gameObject.AddComponent<GraphicRaycaster>();

        var font = PixelUIKit.GetMonoFont();

        // Dimmer backdrop
        var dimmer = new GameObject("Dimmer", typeof(RectTransform), typeof(Image));
        dimmer.transform.SetParent(transform, false);
        var dimImg = dimmer.GetComponent<Image>();
        dimImg.color = new Color(0.02f, 0.04f, 0.08f, 0.85f);
        var dimRt = dimmer.GetComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;

        // Central Card Panel
        _panel = new GameObject("CardPanel", typeof(RectTransform), typeof(Image));
        _panel.transform.SetParent(dimmer.transform, false);
        var panelImg = _panel.GetComponent<Image>();
        panelImg.color = new Color(0.04f, 0.10f, 0.16f, 0.98f);
        var pRt = _panel.GetComponent<RectTransform>();
        pRt.anchorMin = new Vector2(0.5f, 0.5f);
        pRt.anchorMax = new Vector2(0.5f, 0.5f);
        pRt.pivot = new Vector2(0.5f, 0.5f);
        pRt.sizeDelta = new Vector2(880f, 620f);

        // Header Title
        CreateLabel(_panel.transform, "< NETWORK INTERFACE // PROTOCOL MODIFIERS >", new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.97f), 22, new Color(0.1f, 0.95f, 1f), font, true);
        CreateLabel(_panel.transform, "Pilih spesifikasi transmisi paket untuk ekspedisi pengiriman ini:", new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.89f), 14, new Color(0.75f, 0.9f, 1f), font, false);

        // 4 Protocol Cards
        CreateProtocolRow(0, PacketModifierType.StandardIPv4, "STANDARD TCP/IPv4 [MTU 1500]", 
            "Kecepatan: 100% | Lompatan: 100% | Proteksi Error: Standar\nFormat paket seimbang tanpa penalti atau kompensasi.", 
            0.69f, new Color(0.2f, 0.7f, 1f), font);

        CreateProtocolRow(1, PacketModifierType.FragileUDP, "FAST BITSTREAM [UDP UNRELIABLE]", 
            "Kecepatan: +20% | Lompatan: 100% | Penalti Tabrakan: 2x Lipat\nTransmisi kilat tanpa handshake. Bonus: +60 DataBits.", 
            0.53f, new Color(1f, 0.8f, 0.2f), font);

        CreateProtocolRow(2, PacketModifierType.JumboFrame, "JUMBO FRAME [ETHERNET MTU 9000]", 
            "Kecepatan: 100% | Lompatan: -12% | Muatan: Sangat Berat\nBeban payload masif memperpendek lompatan. Bonus: +120 DataBits.", 
            0.37f, new Color(0.8f, 0.4f, 1f), font);

        CreateProtocolRow(3, PacketModifierType.VolatileICMP, "VOLATILE ECHO [ICMP EXPEDITED]", 
            "Kecepatan: 100% | Lompatan: 100% | Integritas Bocor: 0.8%/detik\nPaket memudar di luar Safe Zone. Bonus: +100 DataBits (S-Rank).", 
            0.21f, new Color(1f, 0.3f, 0.4f), font);

        // Status text
        _statusText = CreateLabel(_panel.transform, "Active Protocol: Standard IPv4", new Vector2(0.08f, 0.12f), new Vector2(0.60f, 0.18f), 14, Color.green, font, false);
        _statusText.alignment = TextAnchor.MiddleLeft;

        // Close Button
        var closeBtn = CreateButton(_panel.transform, "CloseBtn", new Vector2(0.68f, 0.10f), new Vector2(0.92f, 0.18f), new Color(0.12f, 0.35f, 0.5f), () => Hide());
        var closeTxt = CreateLabel(closeBtn.transform, "TUTUP [ESC]", Vector2.zero, Vector2.one, 16, Color.white, font, true);
        closeTxt.alignment = TextAnchor.MiddleCenter;

        _canvas.enabled = false;
    }

    private void CreateProtocolRow(int index, PacketModifierType type, string title, string desc, float anchorY, Color themeColor, Font font)
    {
        var row = new GameObject($"Row_{type}", typeof(RectTransform), typeof(Image));
        row.transform.SetParent(_panel.transform, false);
        var rRt = row.GetComponent<RectTransform>();
        rRt.anchorMin = new Vector2(0.05f, anchorY - 0.065f);
        rRt.anchorMax = new Vector2(0.95f, anchorY + 0.065f);
        rRt.offsetMin = Vector2.zero;
        rRt.offsetMax = Vector2.zero;
        row.GetComponent<Image>().color = new Color(themeColor.r * 0.15f, themeColor.g * 0.15f, themeColor.b * 0.15f, 0.9f);

        // Accent strip
        var strip = new GameObject("Strip", typeof(RectTransform), typeof(Image));
        strip.transform.SetParent(row.transform, false);
        var sRt = strip.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0f, 0f);
        sRt.anchorMax = new Vector2(0.015f, 1f);
        sRt.offsetMin = Vector2.zero;
        sRt.offsetMax = Vector2.zero;
        strip.GetComponent<Image>().color = themeColor;

        // Title & Description
        var t = CreateLabel(row.transform, title, new Vector2(0.03f, 0.52f), new Vector2(0.72f, 0.95f), 15, themeColor, font, true);
        t.alignment = TextAnchor.MiddleLeft;
        var d = CreateLabel(row.transform, desc, new Vector2(0.03f, 0.05f), new Vector2(0.72f, 0.52f), 12, new Color(0.8f, 0.9f, 1f), font, false);
        d.alignment = TextAnchor.MiddleLeft;

        // Select button
        var btn = CreateButton(row.transform, "SelectBtn", new Vector2(0.75f, 0.15f), new Vector2(0.97f, 0.85f), themeColor * 0.7f, () =>
        {
            SelectModifier(type);
        });
        var bTxt = CreateLabel(btn.transform, "PILIH", Vector2.zero, Vector2.one, 14, Color.white, font, true);
        bTxt.alignment = TextAnchor.MiddleCenter;
    }

    private void SelectModifier(PacketModifierType type)
    {
        var pim = PacketIntegrityManager.Ensure();
        pim.SetModifier(type);
        RefreshStatus();
        AudioManager.PlayCollect();
        Hide();
    }

    private void RefreshStatus()
    {
        var pim = PacketIntegrityManager.Instance;
        if (pim != null && pim.ActiveModifier != null && _statusText != null)
        {
            _statusText.text = $"PROT: {pim.ActiveModifier.protocolName} | {pim.ActiveModifier.payloadName}";
        }
    }

    public static void Show()
    {
        var ui = Ensure();
        ui._canvas.enabled = true;
        ui.RefreshStatus();
        Time.timeScale = 0f;
    }

    public static void Hide()
    {
        if (Instance != null)
        {
            Instance._canvas.enabled = false;
            Time.timeScale = 1f;
        }
    }

    private void Update()
    {
        if (_canvas != null && _canvas.enabled)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Hide();
            }
        }
    }

    private static Text CreateLabel(Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color, Font font, bool bold)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var txt = go.GetComponent<Text>();
        txt.text = text;
        txt.fontSize = fontSize;
        txt.color = color;
        txt.font = font;
        txt.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        return txt;
    }

    private static GameObject CreateButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color col, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = col;
        var btn = go.GetComponent<Button>();
        btn.onClick.AddListener(onClick);
        return go;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Transmission Report & Delivery Scorecard modal UI.
/// Displays delivery evaluation (Grade S/A/B/C, Checksum status, bit rewards)
/// and seamlessly integrates authentic Computer Network Engineering educational bytes.
/// </summary>
public class TransmissionScorecardUI : MonoBehaviour
{
    public static TransmissionScorecardUI Instance { get; private set; }

    private Canvas _canvas;
    private GameObject _panel;
    private Text _headerText;
    private Text _gradeText;
    private Text _detailsText;
    private Text _eduFactText;
    private Button _confirmButton;

    private static readonly string[] NetworkFacts = new string[]
    {
        "KONSEP JARINGAN // TCP 3-WAY HANDSHAKE:\nSebelum data dikirim, host melakukan sinkronisasi: Client mengirim SYN, Server membalas SYN-ACK, dan Client mengonfirmasi dengan ACK untuk menjamin koneksi andal.",
        "KONSEP JARINGAN // TIME-TO-LIVE (TTL):\nField 8-bit pada header IPv4 yang berkurang 1 setiap melewati router (hop). Jika TTL mencapai 0, paket dibuang untuk mencegah looping tanpa akhir di internet.",
        "KONSEP JARINGAN // CHECKSUM & ERROR DETECTION:\nNilai hash matematis (CRC32/Internet Checksum) yang dihitung pengirim dan diverifikasi penerima untuk mendeteksi apakah ada bit data yang korup di kabel/gelombang radio.",
        "KONSEP JARINGAN // MTU & FRAGMENTASI:\nMTU (Maximum Transmission Unit) standar Ethernet adalah 1500 byte. Jika paket lebih besar dari MTU link transmisi, router harus memecahnya (fragmentation).",
        "KONSEP JARINGAN // TCP vs UDP:\nTCP berorientasi koneksi dan mengirim ulang paket hilang (reliable). UDP tanpa jabat tangan (connectionless) dan sangat cepat, ideal untuk streaming dan game real-time.",
        "KONSEP JARINGAN // DEFAULT GATEWAY:\nRouter pintu keluar (seperti Portal Gateway Server B) yang menjadi perantara transmisi ketika alamat IP tujuan berada di luar subnet lokal (LAN) asal."
    };

    public static TransmissionScorecardUI Ensure()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("TransmissionScorecardUI");
        return go.AddComponent<TransmissionScorecardUI>();
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
        BuildScorecardCanvas();
    }

    private void BuildScorecardCanvas()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 950;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        gameObject.AddComponent<GraphicRaycaster>();

        var font = PixelUIKit.GetMonoFont();
        var displayFont = PixelUIKit.GetDisplayFont();

        // Background Dimmer
        var dimmer = new GameObject("Dimmer");
        dimmer.transform.SetParent(transform, false);
        var dimImg = dimmer.AddComponent<Image>();
        dimImg.color = new Color(0.02f, 0.05f, 0.1f, 0.88f);
        var dimRt = dimmer.GetComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;

        // Card Panel
        _panel = new GameObject("ScorecardPanel");
        _panel.transform.SetParent(dimmer.transform, false);
        var panelImg = _panel.AddComponent<Image>();
        panelImg.sprite = PixelUIKit.GetSteppedSprite("scorecard_bg", 64, 64, new Color32(14, 22, 38, 248), new Color32(0, 240, 255, 255), new Color32(6, 10, 18, 255), 4, 12);
        panelImg.type = Image.Type.Sliced;
        panelImg.color = Color.white;
        var panelRt = _panel.GetComponent<RectTransform>();
        panelRt.sizeDelta = new Vector2(860, 620);
        panelRt.anchoredPosition = Vector2.zero;

        // Header
        var headerGO = new GameObject("Header");
        headerGO.transform.SetParent(_panel.transform, false);
        _headerText = headerGO.AddComponent<Text>();
        _headerText.font = displayFont;
        _headerText.fontSize = 26;
        _headerText.alignment = TextAnchor.MiddleCenter;
        _headerText.color = new Color(0f, 0.95f, 1f, 1f);
        _headerText.text = "[ TRANSMISSION REPORT // PACKET DELIVERED ]";
        var hRt = headerGO.GetComponent<RectTransform>();
        hRt.anchoredPosition = new Vector2(0, 240);
        hRt.sizeDelta = new Vector2(800, 60);

        // Grade Text
        var gradeGO = new GameObject("GradeText");
        gradeGO.transform.SetParent(_panel.transform, false);
        _gradeText = gradeGO.AddComponent<Text>();
        _gradeText.font = displayFont;
        _gradeText.fontSize = 38;
        _gradeText.alignment = TextAnchor.MiddleCenter;
        _gradeText.color = Color.white;
        _gradeText.text = "GRADE S [PRISTINE]";
        var gRt = gradeGO.GetComponent<RectTransform>();
        gRt.anchoredPosition = new Vector2(0, 160);
        gRt.sizeDelta = new Vector2(800, 70);

        // Details Text
        var detailsGO = new GameObject("DetailsText");
        detailsGO.transform.SetParent(_panel.transform, false);
        _detailsText = detailsGO.AddComponent<Text>();
        _detailsText.font = font;
        _detailsText.fontSize = 20;
        _detailsText.alignment = TextAnchor.MiddleLeft;
        _detailsText.color = new Color(0.85f, 0.95f, 1f, 0.95f);
        _detailsText.lineSpacing = 1.3f;
        var dRt = detailsGO.GetComponent<RectTransform>();
        dRt.anchoredPosition = new Vector2(0, 45);
        dRt.sizeDelta = new Vector2(740, 130);

        // Educational Knowledge Box (Network Education)
        var eduBox = new GameObject("EduBox");
        eduBox.transform.SetParent(_panel.transform, false);
        var eduImg = eduBox.AddComponent<Image>();
        eduImg.color = new Color(0.02f, 0.18f, 0.25f, 0.85f);
        var eRt = eduBox.GetComponent<RectTransform>();
        eRt.anchoredPosition = new Vector2(0, -95);
        eRt.sizeDelta = new Vector2(760, 130);

        var eduTextGO = new GameObject("EduFactText");
        eduTextGO.transform.SetParent(eduBox.transform, false);
        _eduFactText = eduTextGO.AddComponent<Text>();
        _eduFactText.font = font;
        _eduFactText.fontSize = 17;
        _eduFactText.alignment = TextAnchor.MiddleLeft;
        _eduFactText.color = new Color(0.35f, 1f, 0.85f, 1f);
        _eduFactText.lineSpacing = 1.25f;
        var etRt = eduTextGO.GetComponent<RectTransform>();
        etRt.anchoredPosition = Vector2.zero;
        etRt.sizeDelta = new Vector2(720, 110);

        // Confirm Button
        var btnGO = new GameObject("ConfirmButton");
        btnGO.transform.SetParent(_panel.transform, false);
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0f, 0.65f, 0.85f, 1f);
        _confirmButton = btnGO.AddComponent<Button>();
        var bRt = btnGO.GetComponent<RectTransform>();
        bRt.anchoredPosition = new Vector2(0, -225);
        bRt.sizeDelta = new Vector2(380, 55);

        var btnTextGO = new GameObject("BtnText");
        btnTextGO.transform.SetParent(btnGO.transform, false);
        var btnText = btnTextGO.AddComponent<Text>();
        btnText.font = font;
        btnText.fontSize = 20;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = Color.black;
        btnText.text = "[ AKSEPTASI & LANJUTKAN ]";
        var btRt = btnTextGO.GetComponent<RectTransform>();
        btRt.anchoredPosition = Vector2.zero;
        btRt.sizeDelta = new Vector2(360, 50);

        _confirmButton.onClick.AddListener(Close);
        _panel.transform.parent.gameObject.SetActive(false);
    }

    public static void Show(float integrity, DeliveryGrade grade, int bonusBits, PacketModifier modifier = null)
    {
        var ui = Ensure();
        if (ui == null) return;
        ui.DisplayInternal(integrity, grade, bonusBits, modifier);
    }

    private void DisplayInternal(float integrity, DeliveryGrade grade, int bonusBits, PacketModifier modifier)
    {
        if (_panel == null) BuildScorecardCanvas();

        _panel.transform.parent.gameObject.SetActive(true);

        _gradeText.text = PacketIntegrityRules.GetGradeString(grade);
        _gradeText.color = PacketIntegrityManager.GetGradeColor(grade);

        string proto = modifier != null ? modifier.protocolName : "IPv4 / TCP";
        string payload = modifier != null ? modifier.payloadName : "STANDARD DATAGRAM [MTU 1500]";
        int modBonus = modifier != null ? modifier.bonusBitsReward : 0;
        int totalReward = bonusBits + (grade == DeliveryGrade.S ? modBonus : 0);

        _detailsText.text = $"• PROTOKOL: {proto}\n" +
                            $"• PAYLOAD: {payload}\n" +
                            $"• INTEGRITAS BIT: {integrity:F1}% [CRC32 CHECKSUM VERIFIED]\n" +
                            $"• REWARD: +{totalReward} DATA BITS (Bonus Grade & Protocol Multiplier)";

        // Random or deterministic network education fact
        int factIndex = Random.Range(0, NetworkFacts.Length);
        _eduFactText.text = NetworkFacts[factIndex];

        AudioManager.Play("sfx_ui_open", 0.9f, 1f);
        AudioManager.SetMuffled(true);
    }

    public void Close()
    {
        if (_panel != null && _panel.transform.parent != null)
        {
            _panel.transform.parent.gameObject.SetActive(false);
        }
        AudioManager.Play("sfx_ui_click", 0.8f, 1f);
        AudioManager.SetMuffled(false);
    }
}

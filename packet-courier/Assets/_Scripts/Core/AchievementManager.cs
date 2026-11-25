using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Protocol Badges and Achievement system.
/// Infuses real-world networking engineering milestones and awards players with cyber badges.
/// Displays on-screen non-blocking cyber toast banners upon unlocking.
/// </summary>
public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    [System.Serializable]
    public class Achievement
    {
        public string id;
        public string title;
        public string description;
        public string networkLore;

        public Achievement(string id, string title, string description, string networkLore)
        {
            this.id = id;
            this.title = title;
            this.description = description;
            this.networkLore = networkLore;
        }
    }

    private static readonly Dictionary<string, Achievement> _catalog = new Dictionary<string, Achievement>()
    {
        { "ACH_RFC791", new Achievement("ACH_RFC791", "IPv4 PIONEER", "Antar paket data pertama sampai ke terminal tujuan.", "RFC 791 mendefinisikan protokol IPv4 yang menjadi fondasi internet global.") },
        { "ACH_ZERO_LOSS", new Achievement("ACH_ZERO_LOSS", "ZERO PACKET LOSS", "Selesaikan pengantaran dengan Grade S [Pristine] 100% utuh.", "Zero packet loss adalah standar emas transmisi data kritis dan finansial.") },
        { "ACH_FIREWALL_BYPASS", new Achievement("ACH_FIREWALL_BYPASS", "FIREWALL BYPASS", "Kalahkan The Censor (World 2) tanpa merusak integritas paket.", "Firewall menyaring paket berbahaya berdasarkan header dan signature pola serangan.") },
        { "ACH_ROOTKIT_PURGE", new Achievement("ACH_ROOTKIT_PURGE", "ROOTKIT EXORCIST", "Netralkan Rootkit Core pada World 5.", "Rootkit adalah malware stealth tingkat kernel yang menyusup ke level terendah OS.") },
        { "ACH_KERNEL_OVERCLOCK", new Achievement("ACH_KERNEL_OVERCLOCK", "KERNEL OVERCLOCK", "Tingkatkan 4 modul hardware Overclock.", "Overclocking meningkatkan clock rate CPU/chipset di atas spesifikasi pabrik.") },
        { "ACH_SYN_ACK", new Achievement("ACH_SYN_ACK", "TCP HANDSHAKE VERIFIED", "Pecahkan Software Network Quiz Terminal.", "TCP 3-way handshake menjamin kedua ujung komunikasi telah siap bertukar byte.") },
        { "ACH_BOSS_RUSH", new Achievement("ACH_BOSS_RUSH", "GLITCH PURGER", "Taklukkan Boss Rush Mode dari The Censor hingga Rootkit Core.", "Menyelesaikan stress-test sistem menyeluruh tanpa kegagalan koneksi.") }
    };

    private Canvas _toastCanvas;
    private GameObject _toastPanel;
    private Text _toastTitle;
    private Text _toastDesc;
    private Coroutine _toastCoroutine;

    public static AchievementManager Ensure()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("AchievementManager");
        return go.AddComponent<AchievementManager>();
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
        BuildToastCanvas();
    }

    private void BuildToastCanvas()
    {
        _toastCanvas = gameObject.AddComponent<Canvas>();
        _toastCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _toastCanvas.sortingOrder = 990;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        _toastPanel = new GameObject("ToastPanel");
        _toastPanel.transform.SetParent(transform, false);
        var bg = _toastPanel.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.12f, 0.20f, 0.92f);
        var rt = _toastPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, 100); // starts hidden above screen
        rt.sizeDelta = new Vector2(580, 80);

        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(_toastPanel.transform, false);
        _toastTitle = titleGO.AddComponent<Text>();
        _toastTitle.font = font;
        _toastTitle.fontSize = 18;
        _toastTitle.color = new Color(0.2f, 0.95f, 1f, 1f);
        _toastTitle.alignment = TextAnchor.MiddleLeft;
        var tRt = titleGO.GetComponent<RectTransform>();
        tRt.anchoredPosition = new Vector2(20, 15);
        tRt.sizeDelta = new Vector2(540, 26);

        var descGO = new GameObject("Desc");
        descGO.transform.SetParent(_toastPanel.transform, false);
        _toastDesc = descGO.AddComponent<Text>();
        _toastDesc.font = font;
        _toastDesc.fontSize = 14;
        _toastDesc.color = new Color(0.85f, 0.9f, 0.95f, 0.85f);
        _toastDesc.alignment = TextAnchor.MiddleLeft;
        var dRt = descGO.GetComponent<RectTransform>();
        dRt.anchoredPosition = new Vector2(20, -15);
        dRt.sizeDelta = new Vector2(540, 26);

        _toastPanel.SetActive(false);
    }

    public static bool IsUnlocked(string id)
    {
        return PlayerPrefs.GetInt("PC_Ach_" + id, 0) == 1;
    }

    public static void Unlock(string id)
    {
        if (IsUnlocked(id)) return;

        PlayerPrefs.SetInt("PC_Ach_" + id, 1);
        PlayerPrefs.Save();

        var mgr = Ensure();
        if (mgr != null && _catalog.TryGetValue(id, out var ach))
        {
            mgr.ShowToast(ach);
        }
    }

    private void ShowToast(Achievement ach)
    {
        if (_toastPanel == null) BuildToastCanvas();

        _toastTitle.text = $"[ PROTOCOL BADGE UNLOCKED // {ach.title} ]";
        _toastDesc.text = ach.description;

        AudioManager.Play("sfx_collect", 0.9f, 1.2f);

        if (_toastCoroutine != null) StopCoroutine(_toastCoroutine);
        _toastCoroutine = StartCoroutine(ToastSlideRoutine());
    }

    private IEnumerator ToastSlideRoutine()
    {
        _toastPanel.SetActive(true);
        var rt = _toastPanel.GetComponent<RectTransform>();

        // Slide down
        float elapsed = 0f;
        while (elapsed < 0.35f)
        {
            elapsed += Time.unscaledDeltaTime;
            float y = Mathf.Lerp(100f, -25f, elapsed / 0.35f);
            rt.anchoredPosition = new Vector2(0, y);
            yield return null;
        }
        rt.anchoredPosition = new Vector2(0, -25f);

        yield return new WaitForSecondsRealtime(3.0f);

        // Slide up
        elapsed = 0f;
        while (elapsed < 0.35f)
        {
            elapsed += Time.unscaledDeltaTime;
            float y = Mathf.Lerp(-25f, 100f, elapsed / 0.35f);
            rt.anchoredPosition = new Vector2(0, y);
            yield return null;
        }

        _toastPanel.SetActive(false);
        _toastCoroutine = null;
    }
}

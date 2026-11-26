using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tactical Pixel Relay Hub UI ("RUN COMPLETE").
/// Uses PixelUIKit stepped-chamfer cards, custom parcel emblem, Silkscreen + JetBrains Mono fonts,
/// and tactile multi-state buttons.
/// </summary>
[ExecuteAlways]
public class HubUI : MonoBehaviour
{
    private Canvas _canvas;

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
            CleanupExistingCanvas();
    }

    private void OnDestroy()
    {
        CleanupExistingCanvas();
    }

    private void Start()
    {
        if (Application.isPlaying)
        {
            AudioManager.PlayStageOpeningBGM(AudioManager.BGMTrack.SafeZone, 2.0f);
        }
    }

    public void RebuildUI()
    {
        CleanupExistingCanvas();
        _canvas = CreateCanvas();

        // Deep Cyber Backdrop
        var bg = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(_canvas.transform, false);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = PixelUIKit.BgDeep;

        var card = PixelUIKit.CreateSteppedPanel(
            _canvas.transform,
            "HubCard",
            new Vector2(0.22f, 0.18f),
            new Vector2(0.78f, 0.82f),
            new Color32(14, 22, 38, 248),
            new Color32(54, 245, 155, 255),
            new Color32(6, 10, 18, 255),
            4);

        // Parcel Emblem
        var logoGo = new GameObject("ParcelLogo", typeof(RectTransform), typeof(Image));
        logoGo.transform.SetParent(card.transform, false);
        var logoRt = logoGo.GetComponent<RectTransform>();
        logoRt.anchorMin = new Vector2(0.43f, 0.74f);
        logoRt.anchorMax = new Vector2(0.57f, 0.93f);
        logoRt.offsetMin = Vector2.zero;
        logoRt.offsetMax = Vector2.zero;
        var logoImg = logoGo.GetComponent<Image>();
        logoImg.sprite = PixelUIKit.GetParcelLogo();
        logoImg.preserveAspect = true;

        PixelUIKit.CreatePixelLabel(card.transform, "Title", "WORLD SELESAI!", new Vector2(0.06f, 0.60f), new Vector2(0.94f, 0.72f), 28, PixelUIKit.Mint, TextAnchor.MiddleCenter, false);
        PixelUIKit.CreatePixelLabel(card.transform, "Subtitle", HubSubtitle(), new Vector2(0.08f, 0.48f), new Vector2(0.92f, 0.58f), 16, PixelUIKit.TextWhite, TextAnchor.MiddleCenter, true);

        var gm = Application.isPlaying ? GameManager.Ensure() : GameManager.Instance;
        int next = gm != null ? gm.currentWorldIndex + 1 : 1;
        if (next < WorldRules.Count && (!Application.isPlaying || GameManager.WorldAvailable(next)))
        {
            PixelUIKit.CreateTactileButton(
                card.transform,
                "play",
                $"LANJUT: WORLD {next + 1} — " + WorldRules.Title(next).ToUpperInvariant(),
                $"{WorldRules.Segments(next)} Stage  •  TTL {WorldRules.TTL(next):0} Detik",
                "ENTER",
                new Vector2(0.14f, 0.27f),
                new Vector2(0.86f, 0.42f),
                PixelUIKit.ButtonTheme.PrimaryCyan,
                () => GameManager.Ensure().StartRun(next));
        }
        else
        {
            PixelUIKit.CreateTactileButton(
                card.transform,
                "skull",
                "BOSS RUSH",
                "Lawan 3 Boss Ground berturut-turut",
                "RUSH",
                new Vector2(0.14f, 0.27f),
                new Vector2(0.86f, 0.42f),
                PixelUIKit.ButtonTheme.DangerCrimson,
                () => BossRushManager.StartBossRush());
        }

        PixelUIKit.CreateTactileButton(
            card.transform,
            "home",
            "MENU UTAMA",
            "Kembali ke layar awal",
            "ESC",
            new Vector2(0.14f, 0.09f),
            new Vector2(0.86f, 0.23f),
            PixelUIKit.ButtonTheme.SecondarySlate,
            () => GameManager.Instance?.ReturnToMenu());

        MenuUI.MarkDontSaveRecursively(_canvas.gameObject);
    }

    private void CleanupExistingCanvas()
    {
        if (_canvas != null)
        {
            if (Application.isPlaying) Destroy(_canvas.gameObject);
            else DestroyImmediate(_canvas.gameObject);
            _canvas = null;
        }

        var stale = GameObject.Find("Hub_Canvas");
        while (stale != null)
        {
            if (Application.isPlaying)
            {
                stale.name = "Hub_Canvas_Destroyed";
                Destroy(stale);
                break;
            }
            else
            {
                DestroyImmediate(stale);
                stale = GameObject.Find("Hub_Canvas");
            }
        }
    }

    private static string HubSubtitle()
    {
        var gm = GameManager.Instance;
        if (gm == null) return "PARCEL DELIVERED TO DESTINATION SERVER. ALL CHECKSUMS VALID.";

        int cleared = gm.currentWorldIndex + 1;
        if (cleared < WorldRules.Count && (!Application.isPlaying || GameManager.WorldAvailable(cleared)))
            return $"WORLD {cleared} CLEARED  |  WORLD {cleared + 1} ({WorldRules.Title(cleared).ToUpperInvariant()}) UNLOCKED  |  BANKED: {gm.totalDataBits} DB";
        return $"WORLD {cleared} TRAVERSAL COMPLETE  |  TOTAL BANKED BITS: {gm.totalDataBits} DB";
    }

    private static Canvas CreateCanvas()
    {
        var go = new GameObject("Hub_Canvas");
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
}

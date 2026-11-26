using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tactical Pixel Pop-up Death Screen ("Die Scene").
/// Freezes gameplay while keeping the background location where the player died in view.
/// Matches the FigCraft Die Scene specification 1:1 with custom pixel emblems,
/// fallen courier viewport, diagnostic black-box log, debrief cards, and tactile buttons.
/// Supports both Edit Mode scene preview (in GameOver.unity) and Play Mode overlay.
/// </summary>
[ExecuteAlways]
public class GameOverUI : MonoBehaviour
{
    private static GameObject activeOverlay;

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            Show();
        }
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
        {
            DestroyOverlay();
        }
    }

    private void Start()
    {
        if (Application.isPlaying && activeOverlay == null)
        {
            Show();
        }
    }

    private void OnDestroy()
    {
        DestroyOverlay();
    }

    public void RebuildUI()
    {
        DestroyOverlay();
        Show();
    }

    public static void Show()
    {
        if (activeOverlay != null) return;

        var stale = GameObject.Find("DeathOverlay");
        while (stale != null)
        {
            if (Application.isPlaying)
            {
                stale.name = "DeathOverlay_Destroyed";
                Object.Destroy(stale);
                break;
            }
            else
            {
                Object.DestroyImmediate(stale);
                stale = GameObject.Find("DeathOverlay");
            }
        }

        if (Application.isPlaying)
        {
            AudioManager.SetMuffled(true);
        }

        activeOverlay = new GameObject("DeathOverlay");
        activeOverlay.hideFlags = HideFlags.DontSave;
        var canvas = activeOverlay.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = activeOverlay.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        activeOverlay.AddComponent<GraphicRaycaster>();
        DeathOverlayController controller = null;
        if (Application.isPlaying)
        {
            controller = activeOverlay.AddComponent<DeathOverlayController>();
            controller.hideFlags = HideFlags.DontSave;
        }

        // 1. Semi-translucent dark crimson-tinted vignette backdrop keeping frozen gameplay visible
        var shadeGo = CreateFlatRect(
            activeOverlay.transform,
            "DeathSceneShade",
            Vector2.zero,
            Vector2.one,
            new Color(0.02f, 0.03f, 0.07f, 0.76f)
        );
        shadeGo.raycastTarget = true;

        // Top Exception Strip
        var topStrip = PixelUIKit.CreateSteppedPanel(
            activeOverlay.transform,
            "TopExceptionStrip",
            new Vector2(0.04f, 0.92f),
            new Vector2(0.96f, 0.97f),
            new Color32(28, 10, 18, 240),
            new Color32(122, 24, 48, 255),
            new Color32(10, 4, 8, 255),
            3
        );
        CreateSpriteBox(topStrip.transform, "StripSkull", new Vector2(0.015f, 0.20f), new Vector2(0.035f, 0.80f), PixelUIKit.GetIcon("skull"), PixelUIKit.Crimson);
        PixelUIKit.CreatePixelLabel(topStrip.transform, "StripLeft", "SINYAL TERPUTUS", new Vector2(0.042f, 0f), new Vector2(0.55f, 1f), 15, PixelUIKit.Crimson, TextAnchor.MiddleLeft, false);
        PixelUIKit.CreatePixelLabel(topStrip.transform, "StripRight", "Coba lagi dari checkpoint atau awal stage", new Vector2(0.55f, 0f), new Vector2(0.985f, 1f), 13, PixelUIKit.TextMuted, TextAnchor.MiddleRight, true);

        // 2. Central Animated Pop-up Card (1180x660 reference proportion)
        var cardGo = new GameObject("DeathCard", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        cardGo.transform.SetParent(activeOverlay.transform, false);
        var cardRt = cardGo.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.sizeDelta = new Vector2(1180f, 660f);

        var cardImg = cardGo.GetComponent<Image>();
        var cardSprite = Resources.Load<Sprite>("GeneratedArt/death_card_bg")
                         ?? PixelUIKit.GetSteppedSprite("death_card_fallback", 64, 64, new Color32(14, 10, 20, 248), new Color32(255, 42, 85, 255), new Color32(6, 4, 10, 255), 4, 12);
        cardImg.sprite = cardSprite;
        cardImg.type = Image.Type.Sliced;
        cardImg.color = Color.white;

        if (controller != null)
        {
            controller.cardRt = cardRt;
            controller.canvasGroup = cardGo.GetComponent<CanvasGroup>();
        }

        // Header Banner inside DeathCard
        var headerBanner = PixelUIKit.CreateSteppedPanel(
            cardGo.transform,
            "HeaderBanner",
            new Vector2(0.03f, 0.77f),
            new Vector2(0.97f, 0.95f),
            new Color32(28, 10, 18, 245),
            new Color32(92, 20, 42, 255),
            new Color32(10, 4, 8, 255),
            3
        );

        // Multi-color Corrupted Skull Emblem
        var skullSprite = PixelUIKit.GetCorruptLogo();
        CreateSpriteBox(
            headerBanner.transform,
            "CorruptIcon",
            new Vector2(0.02f, 0.12f),
            new Vector2(0.11f, 0.88f),
            skullSprite,
            Color.white
        );

        PixelUIKit.CreatePixelLabel(
            headerBanner.transform,
            "SignalTerminated",
            "SINYAL TERPUTUS",
            new Vector2(0.125f, 0.44f),
            new Vector2(0.74f, 0.90f),
            34,
            PixelUIKit.Crimson,
            TextAnchor.MiddleLeft,
            false
        );

        PixelUIKit.CreatePixelLabel(
            headerBanner.transform,
            "SubHeader",
            "Paket gagal terkirim • Kurir K-01 jatuh",
            new Vector2(0.125f, 0.12f),
            new Vector2(0.74f, 0.44f),
            15,
            PixelUIKit.Cyan,
            TextAnchor.MiddleLeft,
            false
        );

        var errBadge = PixelUIKit.CreateSteppedPanel(
            headerBanner.transform,
            "ErrBadge",
            new Vector2(0.75f, 0.26f),
            new Vector2(0.975f, 0.74f),
            new Color32(42, 12, 24, 255),
            new Color32(255, 42, 85, 255),
            new Color32(0, 0, 0, 0),
            2
        );
        PixelUIKit.CreatePixelLabel(errBadge.transform, "ErrCode", "TTL HABIS", Vector2.zero, Vector2.one, 12, PixelUIKit.CrimsonSoft, TextAnchor.MiddleCenter, false);

        // Divider Line
        CreateFlatRect(cardGo.transform, "Divider", new Vector2(0.03f, 0.745f), new Vector2(0.97f, 0.750f), new Color(1f, 0.23f, 0.36f, 0.45f));

        // Gather live telemetry from GameManager
        var gm = GameManager.Instance;
        int world = gm != null ? gm.currentWorldIndex + 1 : 1;
        int segment = gm != null ? gm.currentSegmentIndex + 1 : 1;
        int bits = gm != null ? gm.dataBitsCollectedThisRun : 0;
        bool hasCp = gm != null && gm.hasCheckpoint;
        string worldName = WorldRules.Title(world - 1).ToUpperInvariant();

        // LEFT COLUMN: Fallen Courier Showcase + Diagnostic Black-Box Log
        var showcaseBox = PixelUIKit.CreateSteppedPanel(
            cardGo.transform,
            "FallenCourierBox",
            new Vector2(0.03f, 0.31f),
            new Vector2(0.39f, 0.72f),
            new Color32(10, 7, 15, 250),
            new Color32(74, 21, 37, 255),
            new Color32(4, 2, 6, 255),
            3
        );
        CreateFlatRect(showcaseBox.transform, "FloorLine", new Vector2(0.06f, 0.22f), new Vector2(0.94f, 0.24f), PixelUIKit.Crimson);

        var dead1 = Resources.Load<Sprite>("PlayerSprites/death1");
        var dead2 = Resources.Load<Sprite>("PlayerSprites/death2");
        var deadImg = CreateSpriteBox(
            showcaseBox.transform,
            "DeadCourierSprite",
            new Vector2(0.14f, 0.28f),
            new Vector2(0.52f, 0.88f),
            dead2 != null ? dead2 : (dead1 ?? skullSprite),
            Color.white
        );
        if (dead1 != null && dead2 != null && Application.isPlaying)
        {
            var anim = deadImg.gameObject.AddComponent<PixelSpriteAnimator>();
            anim.hideFlags = HideFlags.DontSave;
            anim.targetImage = deadImg;
            anim.frames = new[] { dead1, dead2 };
            anim.framesPerSecond = 2.5f;
        }

        CreateSpriteBox(
            showcaseBox.transform,
            "SmallCorruptBadge",
            new Vector2(0.58f, 0.36f),
            new Vector2(0.86f, 0.78f),
            skullSprite,
            Color.white
        );
        PixelUIKit.CreatePixelLabel(showcaseBox.transform, "Cap", "KURIR K-01 • OFFLINE", new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.20f), 13, PixelUIKit.CrimsonSoft, TextAnchor.MiddleCenter, false);

        var logBox = PixelUIKit.CreateSteppedPanel(
            cardGo.transform,
            "DiagnosticLogBox",
            new Vector2(0.03f, 0.05f),
            new Vector2(0.39f, 0.28f),
            new Color32(10, 8, 16, 248),
            new Color32(58, 21, 36, 255),
            new Color32(0, 0, 0, 0),
            3
        );
        PixelUIKit.CreatePixelLabel(logBox.transform, "LogTitle", "CATATAN PERJALANAN", new Vector2(0.05f, 0.70f), new Vector2(0.95f, 0.94f), 12, PixelUIKit.TextMuted, TextAnchor.MiddleLeft, false);
        PixelUIKit.CreatePixelLabel(logBox.transform, "Log1", $"> Lokasi: World {world} Stage {segment} ({worldName})", new Vector2(0.05f, 0.46f), new Vector2(0.95f, 0.68f), 12, PixelUIKit.CrimsonSoft, TextAnchor.MiddleLeft, true);
        PixelUIKit.CreatePixelLabel(logBox.transform, "Log2", "> Data Bits & upgrade tetap tersimpan", new Vector2(0.05f, 0.25f), new Vector2(0.95f, 0.46f), 12, PixelUIKit.OrangeLight, TextAnchor.MiddleLeft, true);
        PixelUIKit.CreatePixelLabel(logBox.transform, "Log3", $"> Checkpoint: {(hasCp ? "Siap ulang di checkpoint" : "Mulai dari awal stage")}", new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.25f), 12, PixelUIKit.Cyan, TextAnchor.MiddleLeft, true);

        // RIGHT COLUMN: Structured Debrief Rows + Tactile Recovery Actions
        PixelUIKit.CreatePixelLabel(cardGo.transform, "DebriefHdr", "RINCIAN MISI", new Vector2(0.42f, 0.675f), new Vector2(0.97f, 0.725f), 14, PixelUIKit.TextMuted, TextAnchor.MiddleLeft, false);

        CreateDebriefRow(
            cardGo.transform,
            "RowLocation",
            new Vector2(0.42f, 0.585f),
            new Vector2(0.97f, 0.665f),
            PixelUIKit.GetIcon("route"),
            PixelUIKit.Crimson,
            "LOCATION",
            $"WORLD {world} ({worldName}) - SEGMENT {segment}",
            PixelUIKit.TextWhite
        );

        var dbSprite = Resources.Load<Sprite>("GeneratedArt/databit") ?? PixelUIKit.GetIcon("terminal");
        CreateDebriefRow(
            cardGo.transform,
            "RowDataBits",
            new Vector2(0.42f, 0.495f),
            new Vector2(0.97f, 0.575f),
            dbSprite,
            Color.white,
            "RECOVERABLE DATA BITS",
            $"{bits} DB BUFFERED",
            PixelUIKit.Cyan
        );

        CreateDebriefRow(
            cardGo.transform,
            "RowCheckpoint",
            new Vector2(0.42f, 0.405f),
            new Vector2(0.97f, 0.485f),
            PixelUIKit.GetIcon("check"),
            hasCp ? PixelUIKit.Mint : PixelUIKit.OrangeLight,
            "CHECKPOINT STATUS",
            hasCp ? "TCP NODE LINKED" : "INITIAL SPAWN",
            hasCp ? PixelUIKit.Mint : PixelUIKit.OrangeLight
        );

        PixelUIKit.CreatePixelLabel(cardGo.transform, "RecoveryHdr", "SELECT RECOVERY PROTOCOL", new Vector2(0.42f, 0.345f), new Vector2(0.97f, 0.390f), 14, PixelUIKit.TextMuted, TextAnchor.MiddleLeft, false);

        // 3 Tactile Recovery Buttons with hover/press states and custom pixel icons
        PixelUIKit.CreateTactileButton(
            cardGo.transform,
            "retry",
            "RETRY CHECKPOINT",
            "RESPAWN AT LAST VERIFIED TCP RELAY NODE",
            "R / SPACE",
            new Vector2(0.42f, 0.245f),
            new Vector2(0.97f, 0.335f),
            PixelUIKit.ButtonTheme.PrimaryCyan,
            Retry
        );

        PixelUIKit.CreateTactileButton(
            cardGo.transform,
            "restart",
            "RESTART SEGMENT",
            "CLEAR CHECKPOINT & REBOOT SEGMENT FROM START",
            "KEY T",
            new Vector2(0.42f, 0.145f),
            new Vector2(0.97f, 0.235f),
            PixelUIKit.ButtonTheme.SecondarySlate,
            Restart
        );

        PixelUIKit.CreateTactileButton(
            cardGo.transform,
            "home",
            "RETURN TO MENU",
            "ABORT ACTIVE RUN & DISCONNECT TO RELAY HUB",
            "ESC",
            new Vector2(0.42f, 0.045f),
            new Vector2(0.97f, 0.135f),
            PixelUIKit.ButtonTheme.DangerCrimson,
            ReturnToMenu
        );

        // Play SFX in Play Mode
        if (Application.isPlaying)
        {
            AudioManager.PlayDeath();
            AudioManager.PlayUIOpen();
        }

        MenuUI.MarkDontSaveRecursively(activeOverlay);
    }

    private static Image CreateFlatRect(Transform parent, string name, Vector2 aMin, Vector2 aMax, Color color)
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
        img.raycastTarget = false;
        return img;
    }

    private static Image CreateSpriteBox(Transform parent, string name, Vector2 aMin, Vector2 aMax, Sprite sprite, Color tint)
    {
        var img = CreateFlatRect(parent, name, aMin, aMax, tint);
        img.sprite = sprite;
        img.preserveAspect = true;
        return img;
    }

    private static void CreateDebriefRow(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Sprite iconSprite,
        Color iconTint,
        string keyText,
        string valText,
        Color valColor)
    {
        var row = PixelUIKit.CreateSteppedPanel(
            parent,
            name,
            anchorMin,
            anchorMax,
            new Color32(15, 20, 34, 245),
            new Color32(36, 54, 88, 255),
            new Color32(0, 0, 0, 0),
            3
        );
        CreateSpriteBox(row.transform, "Icon", new Vector2(0.025f, 0.20f), new Vector2(0.075f, 0.80f), iconSprite, iconTint);
        PixelUIKit.CreatePixelLabel(row.transform, "Key", keyText, new Vector2(0.095f, 0f), new Vector2(0.48f, 1f), 14, PixelUIKit.TextMuted, TextAnchor.MiddleLeft, true);
        PixelUIKit.CreatePixelLabel(row.transform, "Val", valText, new Vector2(0.48f, 0f), new Vector2(0.97f, 1f), 16, valColor, TextAnchor.MiddleRight, false);
    }

    public static void Retry()
    {
        if (Application.isPlaying) AudioManager.PlayUIClick();
        DestroyOverlay();
        Time.timeScale = 1;
        GameManager.Instance?.RetrySegment();
    }

    public static void Restart()
    {
        if (Application.isPlaying) AudioManager.PlayUIClick();
        DestroyOverlay();
        Time.timeScale = 1;
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.hasCheckpoint = false;
            gm.RetrySegment();
        }
    }

    public static void ReturnToMenu()
    {
        if (Application.isPlaying) AudioManager.PlayUIClick();
        DestroyOverlay();
        Time.timeScale = 1;
        GameManager.Instance?.ReturnToMenu();
    }

    public static void DestroyOverlay()
    {
        if (activeOverlay != null)
        {
            if (Application.isPlaying) Object.Destroy(activeOverlay);
            else Object.DestroyImmediate(activeOverlay);
        }
        activeOverlay = null;
        if (Application.isPlaying)
        {
            AudioManager.SetMuffled(false);
        }
    }
}

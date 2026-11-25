using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In-Game Cyberpunk Pause Menu.
/// Freezes gameplay (Time.timeScale = 0), applies muffled audio filter,
/// and allows adjusting Audio Volume, Screen Shake toggle, inspecting
/// Overclock status, and restarting or returning to menu.
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    private static GameObject _activeOverlay;
    public static bool IsPaused => _activeOverlay != null;

    public static void TogglePause()
    {
        if (IsPaused) Resume();
        else Show();
    }

    public static void Show()
    {
        if (_activeOverlay != null) return;

        var gm = GameManager.Instance;
        if (gm != null)
        {
            if (gm.state == GameManager.GameState.GameOver) return;
            gm.state = GameManager.GameState.Paused;
        }

        Time.timeScale = 0;
        AudioManager.SetMuffled(true);

        _activeOverlay = new GameObject("PauseOverlay");
        var canvas = _activeOverlay.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 95; // Just beneath death screen and loading screen

        var scaler = _activeOverlay.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        _activeOverlay.AddComponent<GraphicRaycaster>();
        _activeOverlay.AddComponent<PauseMenuController>();
    }

    public static void Resume()
    {
        if (_activeOverlay != null)
        {
            Destroy(_activeOverlay);
            _activeOverlay = null;
        }

        var gm = GameManager.Instance;
        if (gm != null && gm.state == GameManager.GameState.Paused)
        {
            gm.state = GameManager.GameState.Run;
        }

        Time.timeScale = 1;
        AudioManager.SetMuffled(false);
    }

    public static void RetryCheckpoint()
    {
        Resume();
        GameManager.Instance?.RetrySegment();
    }

    public static void RestartSegment()
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.hasCheckpoint = false;
        }
        Resume();
        gm?.RetrySegment();
    }

    public static void ReturnToMenu()
    {
        Resume();
        GameManager.Instance?.ReturnToMenu();
    }

    public class PauseMenuController : MonoBehaviour
    {
        private RectTransform _cardRt;
        private CanvasGroup _canvasGroup;

        private Text _masterVolText;
        private Text _bgmVolText;
        private Text _sfxVolText;
        private Text _shakeToggleText;
        private Text _touchToggleText;
        private Text _rumbleToggleText;
        private Text _overclockSummaryText;

        private void Awake()
        {
            // 1. Semi-translucent dark vignette backdrop
            var shade = CreatePanel(transform, "Shade", Vector2.zero, Vector2.one, new Color(0.02f, 0.04f, 0.08f, 0.75f));
            shade.raycastTarget = true;

            // 2. Central Animated Card
            var cardGo = new GameObject("PauseCard", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            cardGo.transform.SetParent(transform, false);
            _cardRt = cardGo.GetComponent<RectTransform>();
            _cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            _cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            _cardRt.pivot = new Vector2(0.5f, 0.5f);
            _cardRt.sizeDelta = new Vector2(820f, 620f);

            var cardImg = cardGo.GetComponent<Image>();
            var bgSprite = Resources.Load<Sprite>("GeneratedArt/death_card_bg");
            if (bgSprite != null)
            {
                cardImg.sprite = bgSprite;
                cardImg.type = Image.Type.Sliced;
                cardImg.color = new Color(0.12f, 0.2f, 0.3f, 0.98f);
            }
            else
            {
                cardImg.color = new Color(0.06f, 0.1f, 0.18f, 0.98f);
            }

            _canvasGroup = cardGo.GetComponent<CanvasGroup>();

            // Header
            var header = CreateLabel(cardGo.transform, "< SYSTEM PAUSED // TERMINAL >", new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.96f), 24, new Color(0.2f, 0.95f, 1f, 1f));
            header.fontStyle = FontStyle.Bold;

            // Section 1: Settings
            CreateLabel(cardGo.transform, "[ SYSTEM SETTINGS ]", new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.84f), 16, new Color(0f, 1f, 0.8f, 0.9f));

            // Volume controls
            BuildVolumeRow(cardGo.transform, "Master Volume", 0.70f, ref _masterVolText,
                () => AdjustVolume(ref AudioManager.MasterVolume, -0.1f),
                () => AdjustVolume(ref AudioManager.MasterVolume, 0.1f));

            BuildVolumeRow(cardGo.transform, "Music (BGM)", 0.62f, ref _bgmVolText,
                () => AdjustVolume(ref AudioManager.BGMVolume, -0.1f),
                () => AdjustVolume(ref AudioManager.BGMVolume, 0.1f));

            BuildVolumeRow(cardGo.transform, "Effects (SFX)", 0.54f, ref _sfxVolText,
                () => AdjustVolume(ref AudioManager.SFXVolume, -0.1f),
                () => AdjustVolume(ref AudioManager.SFXVolume, 0.1f));

            // Settings toggles (Screen Shake, Touch HUD, Gamepad Rumble)
            var shakeBtn = CreateButton(cardGo.transform, "ScreenShakeBtn", new Vector2(0.08f, 0.44f), new Vector2(0.36f, 0.50f), new Color(0.12f, 0.25f, 0.4f, 1f), ToggleShake);
            _shakeToggleText = shakeBtn.GetComponentInChildren<Text>();
            UpdateShakeText();

            var touchBtn = CreateButton(cardGo.transform, "TouchToggleBtn", new Vector2(0.39f, 0.44f), new Vector2(0.67f, 0.50f), new Color(0.12f, 0.35f, 0.35f, 1f), ToggleTouchControls);
            _touchToggleText = touchBtn.GetComponentInChildren<Text>();
            UpdateTouchText();

            var rumbleBtn = CreateButton(cardGo.transform, "GamepadRumbleBtn", new Vector2(0.70f, 0.44f), new Vector2(0.92f, 0.50f), new Color(0.25f, 0.2f, 0.45f, 1f), ToggleRumble);
            _rumbleToggleText = rumbleBtn.GetComponentInChildren<Text>();
            UpdateRumbleText();

            // Section 2: Overclock Hardware Status
            CreateLabel(cardGo.transform, "[ HARDWARE MODS INSTALLED ]", new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.40f), 16, new Color(1f, 0.85f, 0.2f, 0.9f));
            _overclockSummaryText = CreateLabel(cardGo.transform, "Loading mods...", new Vector2(0.08f, 0.24f), new Vector2(0.92f, 0.34f), 14, new Color(0.75f, 0.9f, 1f, 0.95f));
            _overclockSummaryText.alignment = TextAnchor.MiddleCenter;
            RefreshOverclockSummary();

            // Section 3: Navigation Buttons
            CreateButton(cardGo.transform, "[ESC] RESUME", new Vector2(0.08f, 0.08f), new Vector2(0.36f, 0.17f), new Color(0.1f, 0.55f, 0.4f, 1f), Resume);
            CreateButton(cardGo.transform, "[R] RETRY", new Vector2(0.39f, 0.08f), new Vector2(0.61f, 0.17f), new Color(0.2f, 0.4f, 0.65f, 1f), RetryCheckpoint);
            CreateButton(cardGo.transform, "MENU", new Vector2(0.64f, 0.08f), new Vector2(0.92f, 0.17f), new Color(0.55f, 0.2f, 0.25f, 1f), ReturnToMenu);

            RefreshVolumeTexts();
        }

        private void Start()
        {
            StartCoroutine(AnimateOpen());
        }

        private IEnumerator AnimateOpen()
        {
            float duration = 0.22f;
            float elapsed = 0f;
            _canvasGroup.alpha = 0f;
            _cardRt.localScale = new Vector3(0.7f, 0.7f, 1f);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float c1 = 1.70158f;
                float c3 = c1 + 1f;
                float scale = 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);

                _canvasGroup.alpha = Mathf.Lerp(0f, 1f, t * 1.5f);
                _cardRt.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            _canvasGroup.alpha = 1f;
            _cardRt.localScale = Vector3.one;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Resume();
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                RetryCheckpoint();
            }
        }

        private void BuildVolumeRow(Transform parent, string label, float anchorY, ref Text valText, UnityEngine.Events.UnityAction onMinus, UnityEngine.Events.UnityAction onPlus)
        {
            CreateLabel(parent, label, new Vector2(0.10f, anchorY - 0.03f), new Vector2(0.40f, anchorY + 0.03f), 15, Color.white);
            CreateButton(parent, "[-]", new Vector2(0.44f, anchorY - 0.03f), new Vector2(0.52f, anchorY + 0.03f), new Color(0.2f, 0.35f, 0.5f, 1f), onMinus);
            valText = CreateLabel(parent, "100%", new Vector2(0.54f, anchorY - 0.03f), new Vector2(0.66f, anchorY + 0.03f), 15, new Color(0.3f, 1f, 0.9f, 1f));
            valText.alignment = TextAnchor.MiddleCenter;
            CreateButton(parent, "[+]", new Vector2(0.68f, anchorY - 0.03f), new Vector2(0.76f, anchorY + 0.03f), new Color(0.2f, 0.35f, 0.5f, 1f), onPlus);
        }

        private void AdjustVolume(ref float vol, float delta)
        {
            vol = Mathf.Clamp01(vol + delta);
            AudioManager.SetVolumes(AudioManager.MasterVolume, AudioManager.BGMVolume, AudioManager.SFXVolume);
            RefreshVolumeTexts();
            AudioManager.PlayUIClick();
        }

        private void RefreshVolumeTexts()
        {
            if (_masterVolText != null) _masterVolText.text = string.Format("{0}%", Mathf.RoundToInt(AudioManager.MasterVolume * 100f));
            if (_bgmVolText != null) _bgmVolText.text = string.Format("{0}%", Mathf.RoundToInt(AudioManager.BGMVolume * 100f));
            if (_sfxVolText != null) _sfxVolText.text = string.Format("{0}%", Mathf.RoundToInt(AudioManager.SFXVolume * 100f));
        }

        private void ToggleShake()
        {
            JuiceManager.EnableScreenShake = !JuiceManager.EnableScreenShake;
            UpdateShakeText();
            AudioManager.PlayUIClick();
        }

        private void UpdateShakeText()
        {
            if (_shakeToggleText != null)
            {
                _shakeToggleText.text = string.Format("SHAKE: {0}", JuiceManager.EnableScreenShake ? "[ON]" : "[OFF]");
            }
        }

        private void ToggleTouchControls()
        {
            TouchControlsUI.IsTouchEnabled = !TouchControlsUI.IsTouchEnabled;
            UpdateTouchText();
            AudioManager.PlayUIClick();
        }

        private void UpdateTouchText()
        {
            if (_touchToggleText != null)
            {
                _touchToggleText.text = string.Format("TOUCH: {0}", TouchControlsUI.IsTouchEnabled ? "[ON]" : "[OFF]");
            }
        }

        private void ToggleRumble()
        {
            GamepadControllerSupport.EnableVibration = !GamepadControllerSupport.EnableVibration;
            UpdateRumbleText();
            AudioManager.PlayUIClick();
        }

        private void UpdateRumbleText()
        {
            if (_rumbleToggleText != null)
            {
                _rumbleToggleText.text = string.Format("RUMBLE: {0}", GamepadControllerSupport.EnableVibration ? "[ON]" : "[OFF]");
            }
        }

        private void RefreshOverclockSummary()
        {
            if (_overclockSummaryText == null) return;
            var list = new List<string>();

            if (OverclockManager.HasTripleJump) list.Add("TRIPLE JUMP");
            if (OverclockManager.HasBlaster) list.Add(string.Format("BLASTER LV.{0}", OverclockManager.BlasterLevel));
            if (OverclockManager.HasParry) list.Add(string.Format("PARRY LV.{0}", OverclockManager.ParryLevel));
            if (OverclockManager.BonusDamage > 0) list.Add(string.Format("DMG +{0}", OverclockManager.BonusDamage));
            if (OverclockManager.HasPhaseDash) list.Add("PHASE DASH");
            if (OverclockManager.MagnetLevel > 0) list.Add(string.Format("MAGNET {0}m", OverclockManager.MagnetLevel == 1 ? 6 : 10));
            if (OverclockManager.BatteryLevel > 0) list.Add("TURBO BATTERY");
            if (OverclockManager.HasBufferRevive) list.Add("REVIVAL BUFFER");

            if (list.Count == 0)
            {
                _overclockSummaryText.text = "NO MODS INSTALLED. VISIT OVERCLOCK TERMINAL IN SAFE ZONE.";
            }
            else
            {
                _overclockSummaryText.text = string.Join("  |  ", list.ToArray());
            }
        }

        private static Image CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        private static Text CreateLabel(Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color)
        {
            var go = new GameObject("Label_" + text.Replace(" ", "_"), typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var label = go.GetComponent<Text>();
            label.text = text;
            label.font = fontSize >= 16 ? PixelUIKit.GetDisplayFont() : PixelUIKit.GetMonoFont();
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        private static Button CreateButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Color bgColor, UnityEngine.Events.UnityAction onClick)
        {
            Color32 fill = bgColor;
            Color32 border = new Color32(
                (byte)Mathf.Clamp((int)(bgColor.r * 340f), 40, 255),
                (byte)Mathf.Clamp((int)(bgColor.g * 340f), 60, 255),
                (byte)Mathf.Clamp((int)(bgColor.b * 340f), 90, 255),
                255);
            var img = PixelUIKit.CreateSteppedPanel(parent, "Btn_" + label.Replace(" ", "_"), anchorMin, anchorMax, fill, border, new Color32(6, 10, 18, 255), 3);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
            var txt = CreateLabel(img.transform, label, Vector2.zero, Vector2.one, 15, Color.white);
            txt.fontStyle = FontStyle.Bold;
            return btn;
        }
    }
}

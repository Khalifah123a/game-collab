using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Tactical Cyberpunk Conversation & Quest Dialogue UI with handcrafted pixel-art visuals.
/// Sibling systems (quests, shop, safe zones) attach their behaviour to the per-choice
/// callback handed to <see cref="Show"/>; this panel never opens gameplay UI on its own.
/// Adheres strictly to the zero-emoji anti-slop hardware aesthetic.
/// </summary>
[DefaultExecutionOrder(200)]
public class DialogUI : MonoBehaviour
{
    public static DialogUI Instance { get; private set; }

    /// <summary>Static guard so only one interaction is ever active at a time.</summary>
    public static bool IsOpen => Instance != null && Instance._isOpen;

    [Header("Style")]
    public int speakerFontSize = 18;
    public int bodyFontSize = 14;
    public int choiceFontSize = 12;
    [Range(1, 6)] public int maxChoices = 4;

    public int ChoiceCount => _isOpen ? _choiceCount : 0;
    public string SpeakerText => _speaker != null ? _speaker.text : string.Empty;
    public string BodyText => _body != null ? _body.text : string.Empty;
    public object Owner => _owner;

    private readonly List<Button> _buttons = new List<Button>();
    private readonly List<Text> _buttonLabels = new List<Text>();
    private readonly List<Image> _buttonImgs = new List<Image>();
    private RectTransform _panel;
    private Image _portraitImg;
    private Text _speaker;
    private Text _body;
    private Text _statusTag;
    private Text _dismissHint;
    private bool _isOpen;
    private int _choiceCount;
    private int _selected = -1;
    private Action<int> _onChoose;
    private object _owner;
    private PlayerController _held;

    public static DialogUI Ensure()
    {
        if (Instance != null) return Instance;
        return new GameObject("Dialog_Canvas").AddComponent<DialogUI>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildCanvas();
        _panel.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Opens the panel. Returns false when another dialog already holds the screen.</summary>
    public bool Show(string speaker, string[] lines, string[] labels, Action<int> onChoice, object owner = null)
    {
        if (IsOpen) return false;
        _onChoose = onChoice;
        _owner = owner;
        _selected = -1;
        _speaker.text = string.IsNullOrEmpty(speaker) ? "UNKNOWN SOURCE" : speaker.ToUpperInvariant();
        _body.text = string.Join("\n", lines ?? new string[0]);
        _choiceCount = Mathf.Min(labels != null ? labels.Length : 0, maxChoices);

        for (int i = 0; i < _choiceCount; i++)
            _buttonLabels[i].text = $"{i + 1}.  {labels[i]}";

        if (_portraitImg != null)
        {
            string up = _speaker.text;
            bool isTerm = up.Contains("TERMINAL") || up.Contains("BOARD") || up.Contains("QUIZ") || up.Contains("SYSTEM");
            _portraitImg.sprite = isTerm
                ? PixelUIKit.GetIcon("terminal")
                : CyberOpeningAndTutorialUI.GetOperatorPortraitSprite();
        }

        if (_dismissHint != null)
        {
            _dismissHint.gameObject.SetActive(_choiceCount == 0);
        }

        LayoutChoices();
        _panel.gameObject.SetActive(true);
        _isOpen = true;
        Freeze();
        AudioManager.PlayNPCTalk();
        AudioManager.SetMuffled(true);
        return true;
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;
        _choiceCount = 0;
        _selected = -1;
        _onChoose = null;
        _owner = null;
        _panel.gameObject.SetActive(false);
        Release();
        AudioManager.SetMuffled(false);
    }

    public void CloseIfOwner(object owner)
    {
        if (_isOpen && _owner == owner) Close();
    }

    public string ChoiceLabel(int index) => index >= 0 && index < _choiceCount ? _buttonLabels[index].text : string.Empty;

    /// <summary>Highlights a choice without committing it (keyboard navigation).</summary>
    public void SelectChoice(int index)
    {
        if (!_isOpen || index < 0 || index >= _choiceCount) return;
        _selected = index;
        for (int i = 0; i < _buttons.Count; i++) TintChoice(i, i == index);
    }

    /// <summary>Submit/Enter: commits the highlighted choice, or dismisses a choice-less line.</summary>
    public void CommitSelected()
    {
        if (!_isOpen) return;
        if (_choiceCount == 0) { Close(); return; }
        Choose(_selected >= 0 ? _selected : 0);
    }

    /// <summary>Commits a choice by index. Shared by the keyboard and the choice buttons.</summary>
    public void Choose(int index)
    {
        if (!_isOpen || index < 0) return;
        AudioManager.PlayUIClick();
        var callback = _onChoose;
        Close();
        callback?.Invoke(index);
    }

    /// <summary>Mouse / touch entry point: routes through the real Button so uGUI handles the pointer.</summary>
    public void ClickChoice(int index)
    {
        if (!_isOpen || index < 0 || index >= _choiceCount) return;
        _buttons[index].onClick.Invoke();
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
        for (int i = 0; i < _choiceCount && i < 9; i++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
            {
                SelectChoice(i);
                Choose(i);
                return;
            }
        }
        if (_choiceCount > 0)
        {
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                SelectChoice((_selected + 1) % _choiceCount);
                AudioManager.PlayUIClick();
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                SelectChoice((_selected <= 0 ? _choiceCount - 1 : _selected - 1));
                AudioManager.PlayUIClick();
            }
        }
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
        {
            CommitSelected();
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

    private void LayoutChoices()
    {
        if (_body != null)
        {
            var bodyRt = _body.GetComponent<RectTransform>();
            if (bodyRt != null)
            {
                bodyRt.anchorMin = new Vector2(0.17f, _choiceCount > 0 ? 0.45f : 0.20f);
                bodyRt.anchorMax = new Vector2(0.955f, 0.77f);
                bodyRt.offsetMin = Vector2.zero;
                bodyRt.offsetMax = Vector2.zero;
            }
        }

        int count = Mathf.Max(1, _choiceCount);
        float slot = 1f / count;
        for (int i = 0; i < _buttons.Count; i++)
        {
            var rt = _buttons[i].GetComponent<RectTransform>();
            int orderFromTop = i;
            float yTop = 1f - orderFromTop * slot;
            float yBot = 1f - (orderFromTop + 1) * slot;

            rt.anchorMin = new Vector2(0f, yBot);
            rt.anchorMax = new Vector2(1f, yTop);
            rt.offsetMin = new Vector2(0f, 3f);
            rt.offsetMax = new Vector2(0f, -3f);

            _buttons[i].gameObject.SetActive(i < _choiceCount);
            TintChoice(i, i == _selected);
        }
    }

    private void TintChoice(int index, bool selected)
    {
        if (index < 0 || index >= _buttonImgs.Count) return;
        var img = _buttonImgs[index];
        if (img == null) return;

        if (selected)
        {
            img.sprite = PixelUIKit.GetSteppedSprite("btn_dialog_hover", 48, 32, new Color32(20, 36, 56, 250), PixelUIKit.Cyan, new Color32(10, 20, 32, 255), 2);
            if (index < _buttonLabels.Count && _buttonLabels[index] != null)
                _buttonLabels[index].color = PixelUIKit.Cyan;
        }
        else
        {
            img.sprite = PixelUIKit.GetSteppedSprite("btn_dialog_norm", 48, 32, new Color32(14, 22, 36, 245), PixelUIKit.BorderSlate, new Color32(8, 12, 20, 255), 2);
            if (index < _buttonLabels.Count && _buttonLabels[index] != null)
                _buttonLabels[index].color = PixelUIKit.TextWhite;
        }
    }

    private void BuildCanvas()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        // Dialogue Box: lower third of screen with hard RectMask2D so text never overflows outside
        _panel = Stretch("DialogPanel", transform, new Vector2(0.10f, 0.05f), new Vector2(0.90f, 0.48f));
        _panel.gameObject.AddComponent<RectMask2D>();
        PixelUIKit.CreateSteppedPanel(
            _panel,
            "PanelBackdrop",
            Vector2.zero,
            Vector2.one,
            PixelUIKit.CardDark,
            PixelUIKit.BorderSlate,
            PixelUIKit.CyanDark,
            5);

        // Header Tab with Speaker & Transmission Status
        var headerTab = Stretch("HeaderTab", _panel, new Vector2(0.03f, 0.82f), new Vector2(0.97f, 0.96f));
        var iconBox = Stretch("SpeakerIcon", headerTab, new Vector2(0f, 0.15f), new Vector2(0.04f, 0.85f));
        var iconImg = iconBox.gameObject.AddComponent<Image>();
        iconImg.sprite = PixelUIKit.GetIcon("terminal");
        iconImg.color = PixelUIKit.Cyan;
        iconImg.preserveAspect = true;

        _speaker = Label("Speaker", headerTab, new Vector2(0.05f, 0f), new Vector2(0.65f, 1f), speakerFontSize, TextAnchor.MiddleLeft, false);
        _speaker.fontStyle = FontStyle.Bold;
        _speaker.color = PixelUIKit.Cyan;

        _statusTag = Label("StatusTag", headerTab, new Vector2(0.65f, 0f), new Vector2(0.98f, 1f), 10, TextAnchor.MiddleRight, true);
        _statusTag.text = "// COMMS LINK [ONLINE]";
        _statusTag.color = PixelUIKit.TextDim;

        // Thin Separator Line
        var sepGo = new GameObject("Sep", typeof(RectTransform), typeof(Image));
        sepGo.transform.SetParent(_panel, false);
        var sepRt = sepGo.GetComponent<RectTransform>();
        sepRt.anchorMin = new Vector2(0.03f, 0.80f);
        sepRt.anchorMax = new Vector2(0.97f, 0.805f);
        sepRt.offsetMin = Vector2.zero; sepRt.offsetMax = Vector2.zero;
        var sepImg = sepGo.GetComponent<Image>();
        sepImg.color = PixelUIKit.BorderSlate;

        // Speaker Portrait Frame on Left
        var portBox = Stretch("PortraitFrame", _panel, new Vector2(0.03f, 0.45f), new Vector2(0.15f, 0.77f));
        PixelUIKit.CreateSteppedPanel(
            portBox,
            "PortraitBg",
            Vector2.zero,
            Vector2.one,
            new Color32(16, 28, 48, 255),
            PixelUIKit.Cyan,
            new Color32(8, 14, 24, 255),
            3);
        var portArt = Stretch("PortraitArt", portBox, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f));
        _portraitImg = portArt.gameObject.AddComponent<Image>();
        _portraitImg.sprite = CyberOpeningAndTutorialUI.GetOperatorPortraitSprite();
        _portraitImg.preserveAspect = true;

        // Dialogue Body
        _body = Label("Body", _panel, new Vector2(0.17f, 0.46f), new Vector2(0.96f, 0.77f), bodyFontSize, TextAnchor.UpperLeft, true);
        _body.color = PixelUIKit.TextWhite;

        _dismissHint = Label("DismissHint", _panel, new Vector2(0.55f, 0.08f), new Vector2(0.96f, 0.22f), 12, TextAnchor.MiddleRight, false);
        _dismissHint.text = "[SPACE / ENTER] LANJUT >>";
        _dismissHint.color = PixelUIKit.Mint;
        _dismissHint.gameObject.SetActive(false);

        // Choices Container aligned to right of Portrait Box
        var choices = Stretch("Choices", _panel, new Vector2(0.17f, 0.06f), new Vector2(0.955f, 0.42f));
        for (int i = 0; i < maxChoices; i++)
        {
            _buttons.Add(BuildChoiceButton(i, choices));
        }
    }

    private Button BuildChoiceButton(int index, Transform parent)
    {
        var rt = Stretch("Choice_" + (index + 1), parent, Vector2.zero, Vector2.one);
        var image = rt.gameObject.AddComponent<Image>();
        image.sprite = PixelUIKit.GetSteppedSprite("btn_dialog_norm", 48, 32, new Color32(14, 22, 36, 245), PixelUIKit.BorderSlate, new Color32(8, 12, 20, 255), 2);
        image.type = Image.Type.Sliced;
        _buttonImgs.Add(image);

        var button = rt.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = image;
        int captured = index;
        button.onClick.AddListener(() => Choose(captured));

        var label = Label("Label", rt, new Vector2(0.03f, 0f), new Vector2(0.97f, 1f), choiceFontSize, TextAnchor.MiddleLeft, true);
        label.color = PixelUIKit.TextWhite;
        _buttonLabels.Add(label);

        if (Application.isPlaying)
        {
            var tactile = rt.gameObject.AddComponent<TactilePixelButton>();
            tactile.hideFlags = HideFlags.DontSave;
            tactile.bgImage = image;
            tactile.normalSprite = image.sprite;
            tactile.hoverSprite = PixelUIKit.GetSteppedSprite("btn_dialog_hover", 48, 32, new Color32(20, 36, 56, 250), PixelUIKit.Cyan, new Color32(10, 20, 32, 255), 2);
            tactile.pressedSprite = PixelUIKit.GetSteppedSprite("btn_dialog_press", 48, 32, new Color32(10, 16, 26, 255), PixelUIKit.Mint, new Color32(6, 10, 16, 255), 2);
            tactile.titleText = label;
            tactile.normalTitleColor = PixelUIKit.TextWhite;
            tactile.hoverTitleColor = PixelUIKit.Cyan;
            tactile.pressedTitleColor = PixelUIKit.Mint;
        }

        return button;
    }

    private static RectTransform Stretch(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return rt;
    }

    private static Text Label(string name, Transform parent, Vector2 min, Vector2 max, int fontSize, TextAnchor anchor, bool mono)
    {
        var text = Stretch(name, parent, min, max).gameObject.AddComponent<Text>();
        text.font = mono ? PixelUIKit.GetMonoFont() : PixelUIKit.GetDisplayFont();
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = Color.white;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Max(9, fontSize - 5);
        text.resizeTextMaxSize = fontSize;
        return text;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        go.hideFlags = HideFlags.DontSave;
    }
}

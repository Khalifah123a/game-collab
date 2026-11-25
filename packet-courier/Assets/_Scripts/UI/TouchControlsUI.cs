using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Modern on-screen touch controls HUD for mobile devices and accessibility testing.
/// Provides a responsive left virtual D-Pad and a right action button cluster.
/// Integrates directly into InputManager.
/// </summary>
public class TouchControlsUI : MonoBehaviour
{
    public static TouchControlsUI Instance { get; private set; }

    private Canvas _canvas;
    private bool _leftHeld;
    private bool _rightHeld;
    private bool _downHeld;

    public const string PrefsTouchKey = "PacketCourier_TouchControlsEnabled";

    public static bool IsTouchEnabled
    {
        get
        {
#if UNITY_ANDROID || UNITY_IOS
            return PlayerPrefs.GetInt(PrefsTouchKey, 1) == 1;
#else
            return PlayerPrefs.GetInt(PrefsTouchKey, 0) == 1;
#endif
        }
        set
        {
            PlayerPrefs.SetInt(PrefsTouchKey, value ? 1 : 0);
            PlayerPrefs.Save();
            if (Instance != null && Instance._canvas != null)
                Instance._canvas.enabled = value;
        }
    }

    public static TouchControlsUI Ensure()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("TouchControlsUI");
        return go.AddComponent<TouchControlsUI>();
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
        BuildTouchHUD();
    }

    private void BuildTouchHUD()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 900;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        gameObject.AddComponent<GraphicRaycaster>();

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // --- Left D-Pad Controls ---
        CreateDirectionButton("Btn_Left", new Vector2(90, 90), new Vector2(100, 100), "<", font,
            () => _leftHeld = true, () => _leftHeld = false);

        CreateDirectionButton("Btn_Right", new Vector2(210, 90), new Vector2(100, 100), ">", font,
            () => _rightHeld = true, () => _rightHeld = false);

        CreateDirectionButton("Btn_Down", new Vector2(150, 40), new Vector2(90, 80), "v", font,
            () => _downHeld = true, () => _downHeld = false);

        // --- Right Action Button Cluster ---
        CreateActionButton("Btn_Jump", new Vector2(-110, 90), new Vector2(105, 105), "JUMP", font, () =>
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.jumpPressed = true;
                InputManager.Instance.jumpHeld = true;
            }
        });

        CreateActionButton("Btn_Dash", new Vector2(-230, 90), new Vector2(95, 95), "DASH", font, () =>
        {
            if (InputManager.Instance != null) InputManager.Instance.dashPressed = true;
        });

        CreateActionButton("Btn_Attack", new Vector2(-110, 210), new Vector2(95, 95), "ATK", font, () =>
        {
            if (InputManager.Instance != null) InputManager.Instance.attackPressed = true;
        });

        CreateActionButton("Btn_Interact", new Vector2(-230, 205), new Vector2(85, 85), "[E]", font, () =>
        {
            if (InputManager.Instance != null) InputManager.Instance.interactPressed = true;
        });

        _canvas.enabled = IsTouchEnabled;
    }

    private void Update()
    {
        if (!_canvas.enabled) return;

        var input = InputManager.Instance;
        if (input == null) return;

        float h = 0f;
        if (_leftHeld) h -= 1f;
        if (_rightHeld) h += 1f;

        float v = _downHeld ? -1f : 0f;

        if (h != 0f || v != 0f)
        {
            input.moveInput = new Vector2(h, v);
        }
    }

    private void CreateDirectionButton(string name, Vector2 pos, Vector2 size, string label, Font font,
        System.Action onPointerDown, System.Action onPointerUp)
    {
        var btnGO = new GameObject(name);
        btnGO.transform.SetParent(transform, false);

        var img = btnGO.AddComponent<Image>();
        img.color = new Color(0.1f, 0.4f, 0.6f, 0.45f);

        var rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(btnGO.transform, false);
        var txt = textGO.AddComponent<Text>();
        txt.font = font;
        txt.fontSize = 32;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = new Color(0.3f, 1f, 0.9f, 0.85f);
        txt.text = label;

        var tRt = textGO.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = Vector2.zero;
        tRt.offsetMax = Vector2.zero;

        var trigger = btnGO.AddComponent<EventTrigger>();

        var entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        entryDown.callback.AddListener((data) => onPointerDown());
        trigger.triggers.Add(entryDown);

        var entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        entryUp.callback.AddListener((data) => onPointerUp());
        trigger.triggers.Add(entryUp);
    }

    private void CreateActionButton(string name, Vector2 pos, Vector2 size, string label, Font font, System.Action onClick)
    {
        var btnGO = new GameObject(name);
        btnGO.transform.SetParent(transform, false);

        var img = btnGO.AddComponent<Image>();
        img.color = new Color(0f, 0.65f, 0.9f, 0.5f);

        var rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick());

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(btnGO.transform, false);
        var txt = textGO.AddComponent<Text>();
        txt.font = font;
        txt.fontSize = 20;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.text = label;

        var tRt = textGO.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = Vector2.zero;
        tRt.offsetMax = Vector2.zero;
    }

    public static void Toggle()
    {
        IsTouchEnabled = !IsTouchEnabled;
    }
}

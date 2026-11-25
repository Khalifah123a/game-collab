using UnityEngine;

/// <summary>
/// Tactile animated keyboard [E] keycap + chamfered cyber action tag prompt (v2 HUD style).
/// Floats gently, demonstrates periodic click presses, and reacts instantly
/// to user pressing [E] with a tactile key depression and shockwave ring.
/// </summary>
public class InteractKeyPrompt : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite keyNormal;
    public Sprite keyPressed;
    public Sprite keyRing;

    [Header("Label")]
    public string actionLabel = "INTERACT";

    private SpriteRenderer _keyRenderer;
    private SpriteRenderer _ringRenderer;
    private SpriteRenderer _plateRenderer;
    private TextMesh _textMesh;
    private MeshRenderer _textRenderer;

    private Vector3 _basePos;
    private readonly Vector3 _baseScale = Vector3.one * 0.52f;
    private const float KeyRestX = -0.34f;
    private float _pressTimer;
    private float _demoTimer;
    private float _ringTimer;
    private bool _isPressed;
    private string _lastSyncedLabel;

    private void Awake()
    {
        _basePos = transform.localPosition;
        LoadDefaultSprites();
        BuildVisuals();
        SyncLabelVisuals();
    }

    private void Start()
    {
        // Capture any position or label assigned directly after AddComponent
        if (_basePos == Vector3.zero && transform.localPosition != Vector3.zero)
            _basePos = transform.localPosition;

        if (_lastSyncedLabel != actionLabel)
            SyncLabelVisuals();
    }

    public void SetBasePosition(Vector3 pos)
    {
        _basePos = pos;
        transform.localPosition = pos;
    }

    private void LoadDefaultSprites()
    {
        if (keyNormal == null)
            keyNormal = Resources.Load<Sprite>("GeneratedArt/key_e_normal");
        if (keyPressed == null)
            keyPressed = Resources.Load<Sprite>("GeneratedArt/key_e_pressed");
        if (keyRing == null)
            keyRing = Resources.Load<Sprite>("GeneratedArt/key_e_ring");
    }

    private void BuildVisuals()
    {
        // 1. Keycap SpriteRenderer
        var keyGo = new GameObject("KeycapSprite", typeof(SpriteRenderer));
        keyGo.transform.SetParent(transform, false);
        keyGo.transform.localPosition = new Vector3(KeyRestX, 0f, 0f);
        keyGo.transform.localScale = _baseScale;
        _keyRenderer = keyGo.GetComponent<SpriteRenderer>();
        _keyRenderer.sprite = keyNormal;
        _keyRenderer.sortingOrder = 42;

        // 2. Shockwave Click Ring
        var ringGo = new GameObject("ClickRing", typeof(SpriteRenderer));
        ringGo.transform.SetParent(keyGo.transform, false);
        ringGo.transform.localPosition = Vector3.zero;
        ringGo.transform.localScale = Vector3.zero;
        _ringRenderer = ringGo.GetComponent<SpriteRenderer>();
        _ringRenderer.sprite = keyRing;
        _ringRenderer.sortingOrder = 41;
        _ringRenderer.color = new Color(0.2f, 0.95f, 1f, 0f);

        // 3. Chamfered Cyber Tag Backing Plate (v2 Diegetic Prompt)
        var plateGo = new GameObject("CyberTagPlate", typeof(SpriteRenderer));
        plateGo.transform.SetParent(transform, false);
        _plateRenderer = plateGo.GetComponent<SpriteRenderer>();
        _plateRenderer.sortingOrder = 41;

        // 4. Crisp Pixel Display Text Label
        var textGo = new GameObject("ActionText", typeof(TextMesh));
        textGo.transform.SetParent(transform, false);
        textGo.transform.localPosition = new Vector3(0.11f, 0f, 0f);
        _textMesh = textGo.GetComponent<TextMesh>();
        _textRenderer = textGo.GetComponent<MeshRenderer>();

        Font displayFont = PixelUIKit.GetDisplayFont();
        if (displayFont != null)
        {
            _textMesh.font = displayFont;
            if (displayFont.material != null)
            {
                _textRenderer.sharedMaterial = displayFont.material;
                if (displayFont.material.mainTexture != null)
                    displayFont.material.mainTexture.filterMode = FilterMode.Point;
            }
        }
        else
        {
            _textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        _textMesh.fontSize = 38;
        _textMesh.characterSize = 0.042f;
        _textMesh.anchor = TextAnchor.MiddleLeft;
        _textMesh.alignment = TextAlignment.Left;
        _textMesh.color = TacticalHUDGraphics.ColNeonMint;
        _textRenderer.sortingOrder = 42;
    }

    private void SyncLabelVisuals()
    {
        string clean = string.IsNullOrEmpty(actionLabel) ? "INTERACT" : actionLabel.ToUpperInvariant();
        _lastSyncedLabel = actionLabel;

        if (_textMesh != null)
            _textMesh.text = clean;

        if (_plateRenderer == null) return;

        float textWidthUnits = (_textRenderer != null && _textRenderer.localBounds.size.x > 0.08f)
            ? _textRenderer.localBounds.size.x
            : Mathf.Max(0.55f, clean.Length * 0.145f);

        // Quantize width to 8px steps so sprite cache stays small and deterministic
        int rawPx = Mathf.Clamp(Mathf.CeilToInt((textWidthUnits + 0.24f) * 100f), 72, 340);
        int quantPx = ((rawPx + 7) / 8) * 8;
        const int plateHeightPx = 38;

        Vector2[] poly =
        {
            new Vector2(6, 0),
            new Vector2(quantPx - 8, 0),
            new Vector2(quantPx, 8),
            new Vector2(quantPx, plateHeightPx),
            new Vector2(8, plateHeightPx),
            new Vector2(0, plateHeightPx - 8),
            new Vector2(0, 6)
        };

        _plateRenderer.sprite = TacticalHUDGraphics.GetPolygonSprite(
            $"hud_world_prompt_{quantPx}",
            quantPx,
            plateHeightPx,
            poly,
            new Color32(6, 21, 36, 238),
            TacticalHUDGraphics.ColElectricCyan,
            2);

        float worldW = quantPx / 100f;
        _plateRenderer.transform.localPosition = new Vector3(worldW * 0.5f, 0f, 0f);
    }

    public static InteractKeyPrompt Attach(GameObject parent, string label = "INTERACT", Vector2 localOffset = default)
    {
        var go = new GameObject("InteractKeyPrompt");
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = (Vector3)localOffset;
        var prompt = go.AddComponent<InteractKeyPrompt>();
        prompt.SetBasePosition((Vector3)localOffset);
        prompt.SetLabel(label);
        return prompt;
    }

    public void Show()
    {
        gameObject.SetActive(true);
        if (_lastSyncedLabel != actionLabel)
            SyncLabelVisuals();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SetLabel(string label)
    {
        actionLabel = label;
        SyncLabelVisuals();
    }

    /// <summary>Triggers an animated key depression with shockwave ring.</summary>
    public void TriggerClick(bool playAudio = false)
    {
        _isPressed = true;
        _pressTimer = 0.16f;
        _ringTimer = 0.28f;
        if (playAudio)
        {
            AudioManager.PlayInteract();
        }
    }

    private void Update()
    {
        if (_lastSyncedLabel != actionLabel)
            SyncLabelVisuals();

        float time = Time.time;

        // 1. Hover bobbing
        float bob = Mathf.Sin(time * 3.8f) * 0.045f;
        transform.localPosition = _basePos + new Vector3(0f, bob, 0f);

        // 2. Periodic demo press every 1.6s if not currently active
        _demoTimer += Time.deltaTime;
        if (_demoTimer >= 1.6f)
        {
            _demoTimer = 0f;
            TriggerClick();
        }

        // 3. Handle key depression animation
        if (_isPressed)
        {
            _pressTimer -= Time.deltaTime;
            if (_keyRenderer != null)
            {
                _keyRenderer.sprite = keyPressed;
                _keyRenderer.transform.localScale = new Vector3(_baseScale.x * 1.08f, _baseScale.y * 0.78f, 1f);
                _keyRenderer.transform.localPosition = new Vector3(KeyRestX, -0.04f, 0f);
            }
            if (_plateRenderer != null)
                _plateRenderer.color = new Color(1f, 1f, 1f, 1f);

            if (_pressTimer <= 0f)
            {
                _isPressed = false;
            }
        }
        else
        {
            if (_keyRenderer != null)
            {
                _keyRenderer.sprite = keyNormal;
                _keyRenderer.transform.localScale = Vector3.Lerp(_keyRenderer.transform.localScale, _baseScale, Time.deltaTime * 18f);
                _keyRenderer.transform.localPosition = Vector3.Lerp(_keyRenderer.transform.localPosition, new Vector3(KeyRestX, 0f, 0f), Time.deltaTime * 18f);
            }
            if (_plateRenderer != null)
                _plateRenderer.color = new Color(0.95f, 0.98f, 1f, 0.96f);
        }

        // 4. Click shockwave ring expansion & alpha fade
        if (_ringTimer > 0f)
        {
            _ringTimer -= Time.deltaTime;
            float progress = 1f - Mathf.Clamp01(_ringTimer / 0.28f);
            if (_ringRenderer != null)
            {
                float ringScale = Mathf.Lerp(0.4f, 2.4f, progress);
                _ringRenderer.transform.localScale = new Vector3(ringScale, ringScale, 1f);
                float alpha = (1f - progress) * 0.85f;
                _ringRenderer.color = new Color(0.2f, 0.95f, 1f, alpha);
            }
        }
        else
        {
            if (_ringRenderer != null)
                _ringRenderer.color = new Color(0.2f, 0.95f, 1f, 0f);
        }
    }
}

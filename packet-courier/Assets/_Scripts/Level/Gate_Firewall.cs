using UnityEngine;

/// <summary>
/// Tactical Cyberpunk Firewall Security Gate.
/// Replaces the legacy flat orange pillar with a multi-layer laser grid,
/// emitter pylons, procedural scanline energy fields, and an interactive
/// holographic security sign with real-time feedback and control guidance.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Gate_Firewall : MonoBehaviour
{
    [Header("Protocol Requirement")]
    public int requiredPort = 80;
    public bool requireEncrypted = false;
    public bool isOpen;

    [Header("Visual References (Legacy or Runtime)")]
    public GameObject gateOpenVisual;
    public GameObject gateClosedVisual;
    public float wrongPortPenalty = 0;

    private Collider2D blocker;
    private Transform _playerTransform;
    private PlayerMode _playerMode;

    // Procedural Cyber Visuals
    private GameObject _cyberRoot;
    private SpriteRenderer _coreLaserSr;
    private SpriteRenderer _glowLaserSr;
    private SpriteRenderer _scanlineSr;
    private SpriteRenderer _topPylonSr;
    private SpriteRenderer _bottomPylonSr;
    private TextMesh _hologramTitle;
    private TextMesh _hologramStatus;
    private TextMesh _hologramHint;
    private SpriteRenderer _hologramBgSr;

    private float _gateHeight = 18f;
    private float _gateCenterY = 8f;
    private float _disengageTimer = -1f;
    private Color _themeColor;

    private static Sprite _laserCoreSprite;
    private static Sprite _pylonSprite;
    private static Sprite _panelSprite;

    private void Awake()
    {
        blocker = GetComponent<Collider2D>();
        if (blocker != null) blocker.isTrigger = false;

        var box = blocker as BoxCollider2D;
        if (box != null)
        {
            _gateHeight = box.size.y;
            _gateCenterY = box.offset.y;
        }

        // Hide legacy flat untextured visuals
        if (gateClosedVisual != null) gateClosedVisual.SetActive(false);
        if (gateOpenVisual != null) gateOpenVisual.SetActive(false);

        // Hide legacy raw 3D text if any
        var legacyPortText = transform.Find("Port");
        if (legacyPortText != null) legacyPortText.gameObject.SetActive(false);

        DetermineThemeColor();
        BuildProceduralCyberGate();
    }

    private void Start()
    {
        FindPlayer();
        DetermineThemeColor();
        UpdateVisualState(instant: true);
    }

    private void DetermineThemeColor()
    {
        switch (requiredPort)
        {
            case 22:
                // SSH / Encrypted: Amber Security Orange
                _themeColor = new Color(1f, 0.48f, 0.05f, 1f);
                break;
            case 443:
                // HTTPS: Cyber Ice Cyan / Teal
                _themeColor = new Color(0f, 0.95f, 1f, 1f);
                break;
            case 80:
                // HTTP: Emerald Green
                _themeColor = new Color(0.1f, 1f, 0.65f, 1f);
                break;
            case 21:
                // FTP: Golden Yellow
                _themeColor = new Color(1f, 0.82f, 0.1f, 1f);
                break;
            default:
                // Alt / Custom: Radiant Violet
                _themeColor = new Color(0.75f, 0.4f, 1f, 1f);
                break;
        }
    }

    private void BuildProceduralCyberGate()
    {
        if (_cyberRoot != null) Destroy(_cyberRoot);

        _cyberRoot = new GameObject("CyberGate_Visuals");
        _cyberRoot.transform.SetParent(transform, false);

        float halfH = _gateHeight * 0.5f;
        float baseY = _gateCenterY - halfH;
        float topY = _gateCenterY + halfH;

        // 1. Bottom Emitter Pylon
        var botPylonGo = new GameObject("Pylon_Bottom", typeof(SpriteRenderer));
        botPylonGo.transform.SetParent(_cyberRoot.transform, false);
        botPylonGo.transform.localPosition = new Vector3(0f, baseY + 0.4f, 0f);
        _bottomPylonSr = botPylonGo.GetComponent<SpriteRenderer>();
        _bottomPylonSr.sprite = GetOrCreatePylonSprite();
        _bottomPylonSr.sortingOrder = 7;
        _bottomPylonSr.color = Color.white;

        // 2. Top Emitter Pylon
        var topPylonGo = new GameObject("Pylon_Top", typeof(SpriteRenderer));
        topPylonGo.transform.SetParent(_cyberRoot.transform, false);
        topPylonGo.transform.localPosition = new Vector3(0f, topY - 0.4f, 0f);
        topPylonGo.transform.localRotation = Quaternion.Euler(0, 0, 180);
        _topPylonSr = topPylonGo.GetComponent<SpriteRenderer>();
        _topPylonSr.sprite = GetOrCreatePylonSprite();
        _topPylonSr.sortingOrder = 7;
        _topPylonSr.color = Color.white;

        // 3. Wide Outer Glow Beam
        var glowGo = new GameObject("Laser_OuterGlow", typeof(SpriteRenderer));
        glowGo.transform.SetParent(_cyberRoot.transform, false);
        glowGo.transform.localPosition = new Vector3(0f, _gateCenterY, 0f);
        _glowLaserSr = glowGo.GetComponent<SpriteRenderer>();
        _glowLaserSr.sprite = GetOrCreateLaserSprite();
        _glowLaserSr.drawMode = SpriteDrawMode.Sliced;
        _glowLaserSr.size = new Vector2(1.8f, _gateHeight - 0.8f);
        _glowLaserSr.sortingOrder = 5;
        _glowLaserSr.color = new Color(_themeColor.r, _themeColor.g, _themeColor.b, 0.35f);

        // 4. Intense Core Laser Beam
        var coreGo = new GameObject("Laser_CoreBeam", typeof(SpriteRenderer));
        coreGo.transform.SetParent(_cyberRoot.transform, false);
        coreGo.transform.localPosition = new Vector3(0f, _gateCenterY, 0f);
        _coreLaserSr = coreGo.GetComponent<SpriteRenderer>();
        _coreLaserSr.sprite = GetOrCreateLaserSprite();
        _coreLaserSr.drawMode = SpriteDrawMode.Sliced;
        _coreLaserSr.size = new Vector2(0.85f, _gateHeight - 0.8f);
        _coreLaserSr.sortingOrder = 6;
        _coreLaserSr.color = new Color(1f, 1f, 1f, 0.95f);

        // 5. Electric Scanlines overlay
        var scanGo = new GameObject("Laser_Scanlines", typeof(SpriteRenderer));
        scanGo.transform.SetParent(_cyberRoot.transform, false);
        scanGo.transform.localPosition = new Vector3(0f, _gateCenterY, 0f);
        _scanlineSr = scanGo.GetComponent<SpriteRenderer>();
        _scanlineSr.sprite = GetOrCreateLaserSprite();
        _scanlineSr.drawMode = SpriteDrawMode.Tiled;
        _scanlineSr.size = new Vector2(0.6f, _gateHeight - 0.8f);
        _scanlineSr.sortingOrder = 8;
        _scanlineSr.color = new Color(_themeColor.r, _themeColor.g, _themeColor.b, 0.85f);

        // 6. Floating Holographic Security Billboard at eye level
        float signY = Mathf.Clamp(2.4f, baseY + 1.8f, topY - 1.8f);
        var signRoot = new GameObject("Hologram_SecuritySign");
        signRoot.transform.SetParent(_cyberRoot.transform, false);
        signRoot.transform.localPosition = new Vector3(-3.2f, signY, 0f);

        // Sign Backing Panel
        var bgGo = new GameObject("Sign_Backing", typeof(SpriteRenderer));
        bgGo.transform.SetParent(signRoot.transform, false);
        _hologramBgSr = bgGo.GetComponent<SpriteRenderer>();
        _hologramBgSr.sprite = GetOrCreatePanelSprite();
        _hologramBgSr.drawMode = SpriteDrawMode.Sliced;
        _hologramBgSr.size = new Vector2(4.8f, 2.4f);
        _hologramBgSr.sortingOrder = 18;
        _hologramBgSr.color = new Color(0.04f, 0.08f, 0.14f, 0.94f);

        // Title Text
        var titleGo = new GameObject("Text_Title", typeof(TextMesh));
        titleGo.transform.SetParent(signRoot.transform, false);
        titleGo.transform.localPosition = new Vector3(-2.15f, 0.72f, 0f);
        _hologramTitle = titleGo.GetComponent<TextMesh>();
        _hologramTitle.fontSize = 28;
        _hologramTitle.characterSize = 0.065f;
        _hologramTitle.anchor = TextAnchor.UpperLeft;
        _hologramTitle.GetComponent<MeshRenderer>().sortingOrder = 22;

        // Status Text
        var statusGo = new GameObject("Text_Status", typeof(TextMesh));
        statusGo.transform.SetParent(signRoot.transform, false);
        statusGo.transform.localPosition = new Vector3(-2.15f, 0.05f, 0f);
        _hologramStatus = statusGo.GetComponent<TextMesh>();
        _hologramStatus.fontSize = 32;
        _hologramStatus.characterSize = 0.075f;
        _hologramStatus.anchor = TextAnchor.UpperLeft;
        _hologramStatus.GetComponent<MeshRenderer>().sortingOrder = 22;

        // Control Hint Text
        var hintGo = new GameObject("Text_Hint", typeof(TextMesh));
        hintGo.transform.SetParent(signRoot.transform, false);
        hintGo.transform.localPosition = new Vector3(-2.15f, -0.55f, 0f);
        _hologramHint = hintGo.GetComponent<TextMesh>();
        _hologramHint.fontSize = 22;
        _hologramHint.characterSize = 0.055f;
        _hologramHint.anchor = TextAnchor.UpperLeft;
        _hologramHint.GetComponent<MeshRenderer>().sortingOrder = 22;
    }

    private void Update()
    {
        if (isOpen)
        {
            HandleDisengageAnimation();
            return;
        }

        FindPlayer();
        if (_playerTransform == null) return;

        float dx = Mathf.Abs(_playerTransform.position.x - transform.position.x);
        float dy = _playerTransform.position.y - transform.position.y;
        float halfH = _gateHeight * 0.5f;
        float baseY = _gateCenterY - halfH;
        float topY = _gateCenterY + halfH;

        bool inVerticalSpan = dy >= (baseY - 1.2f) && dy <= (topY + 2.0f);
        bool inProximity = dx <= 3.8f && inVerticalSpan;
        bool inNoticeRange = dx <= 7.5f && inVerticalSpan;

        bool matchesPort = _playerMode != null && _playerMode.currentPort == requiredPort;
        bool matchesEncryption = !requireEncrypted || (_playerMode != null && _playerMode.isEncrypted);
        bool isAuthorized = matchesPort && matchesEncryption;

                if (inProximity && isAuthorized)
        {
            ForceOpen();
            return;
        }

        // Press [E] to Auto-Tune packet protocol when at the gate
        bool interact = Input.GetKeyDown(KeyCode.E) || (InputManager.Instance != null && InputManager.Instance.interactPressed);
        if (inProximity && !isAuthorized && interact && _playerMode != null)
        {
            if (InputManager.Instance != null) InputManager.Instance.interactPressed = false;
            _playerMode.SetPort(requiredPort);
            _playerMode.SetMode(requireEncrypted);
            ForceOpen();
            return;
        }

        // Animated laser oscillations
        float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 8f);
        if (_coreLaserSr != null)
        {
            _coreLaserSr.color = new Color(1f, 1f, 1f, 0.95f * pulse);
        }
        if (_glowLaserSr != null)
        {
            _glowLaserSr.color = new Color(_themeColor.r, _themeColor.g, _themeColor.b, 0.4f * pulse);
        }
        if (_scanlineSr != null)
        {
            // Scroll electric texture vertically
            _scanlineSr.transform.localPosition = new Vector3(0f, _gateCenterY + Mathf.Sin(Time.time * 6f) * 0.4f, 0f);
        }

        UpdateHologramUI(inNoticeRange, inProximity, isAuthorized, matchesPort, matchesEncryption);
    }

    private void UpdateHologramUI(bool inNotice, bool inProximity, bool isAuthorized, bool matchesPort, bool matchesEncryption)
    {
        if (_hologramTitle == null) return;

        string protoName = GetProtocolName(requiredPort);
        string modeReq = requireEncrypted ? "ENCRYPTED [ENC]" : "PLAINTEXT [PLN]";
        _hologramTitle.text = $"FIREWALL // PORT {requiredPort} ({protoName})";
        _hologramTitle.color = _themeColor;

        if (inProximity || inNotice)
        {
            if (isAuthorized)
            {
                _hologramStatus.text = ">> ACCESS GRANTED <<";
                _hologramStatus.color = new Color(0.1f, 1f, 0.65f, 1f);
                _hologramHint.text = "Firewall siap dinonaktifkan...";
                _hologramHint.color = Color.white;
            }
            else
            {
                _hologramStatus.text = ">> ACCESS DENIED <<";
                _hologramStatus.color = new Color(1f, 0.25f, 0.35f, 1f);

                                string hint1 = !matchesEncryption ? (requireEncrypted ? "[TAB] ENC" : "[TAB] PLN") : "";
                string hint2 = !matchesPort ? $"[Q/E / 1-5] P{requiredPort}" : "";
                string manualHint = "";
                if (!string.IsNullOrEmpty(hint1) && !string.IsNullOrEmpty(hint2))
                    manualHint = $"{hint1} & {hint2}";
                else
                    manualHint = string.IsNullOrEmpty(hint1) ? hint2 : hint1;

                _hologramHint.text = $"TEKAN [E] AUTO-SYNC PROTOKOL  ({manualHint})";
                _hologramHint.color = new Color(0.9f, 0.95f, 1f, 0.95f);
            }

            if (_hologramBgSr != null)
            {
                Color bgCol = isAuthorized
                    ? new Color(0.02f, 0.12f, 0.08f, 0.94f)
                    : new Color(0.12f, 0.04f, 0.06f, 0.94f);
                _hologramBgSr.color = bgCol;
            }
        }
        else
        {
            _hologramStatus.text = $"REQ: P{requiredPort} · {modeReq}";
            _hologramStatus.color = Color.white;
            _hologramHint.text = "Tekan [TAB] Mode | [Q/E] Port";
            _hologramHint.color = new Color(0.6f, 0.75f, 0.88f, 0.75f);
            if (_hologramBgSr != null)
            {
                _hologramBgSr.color = new Color(0.04f, 0.08f, 0.14f, 0.92f);
            }
        }
    }

    private void HandleDisengageAnimation()
    {
        if (_disengageTimer < 0f) return;
        _disengageTimer += Time.deltaTime;

        float dur = 0.5f;
        float t = Mathf.Clamp01(_disengageTimer / dur);
        float alpha = 1f - t;
        float scaleX = 1f - t;

        if (_coreLaserSr != null)
        {
            _coreLaserSr.color = new Color(1f, 1f, 1f, alpha);
            _coreLaserSr.transform.localScale = new Vector3(scaleX, 1f, 1f);
        }
        if (_glowLaserSr != null)
        {
            _glowLaserSr.color = new Color(_themeColor.r, _themeColor.g, _themeColor.b, alpha * 0.4f);
            _glowLaserSr.transform.localScale = new Vector3(scaleX, 1f, 1f);
        }
        if (_scanlineSr != null)
        {
            _scanlineSr.color = new Color(_themeColor.r, _themeColor.g, _themeColor.b, alpha);
        }

        if (_hologramStatus != null)
        {
            _hologramStatus.text = ">> DECRYPTED & OPEN <<";
            _hologramStatus.color = new Color(0.1f, 1f, 0.65f, alpha);
        }

        if (t >= 1f)
        {
            if (_coreLaserSr != null) _coreLaserSr.gameObject.SetActive(false);
            if (_glowLaserSr != null) _glowLaserSr.gameObject.SetActive(false);
            if (_scanlineSr != null) _scanlineSr.gameObject.SetActive(false);
            if (_cyberRoot != null)
            {
                var sign = _cyberRoot.transform.Find("Hologram_SecuritySign");
                if (sign != null) sign.gameObject.SetActive(false);
            }
            _disengageTimer = -1f;
        }
    }

    public void ForceOpen()
    {
        if (isOpen) return;
        isOpen = true;

        if (blocker != null) blocker.enabled = false;

        _disengageTimer = 0f;

        // Audiovisual satisfaction
        AudioManager.PlayInteract();
        JuiceManager.Hitstop(0.04f);
        JuiceManager.Shake(0.08f);

        Vector3 sparkPos = transform.position + new Vector3(0f, 2.5f, 0f);
        JuiceManager.SpawnImpactSparks(sparkPos, _themeColor);
        JuiceManager.SpawnImpactSparks(sparkPos + Vector3.up * 1.5f, Color.white);
    }

    private void UpdateVisualState(bool instant)
    {
        if (isOpen)
        {
            if (blocker != null) blocker.enabled = false;
            if (instant)
            {
                if (_coreLaserSr != null) _coreLaserSr.gameObject.SetActive(false);
                if (_glowLaserSr != null) _glowLaserSr.gameObject.SetActive(false);
                if (_scanlineSr != null) _scanlineSr.gameObject.SetActive(false);
            }
        }
    }

    private void FindPlayer()
    {
        if (_playerTransform == null)
        {
            var pc = FindAnyObjectByType<PlayerController>();
            if (pc != null) _playerTransform = pc.transform;
        }
        if (_playerMode == null)
        {
            _playerMode = PlayerMode.Instance ?? FindAnyObjectByType<PlayerMode>();
        }
    }

    private static string GetProtocolName(int port)
    {
        switch (port)
        {
            case 21: return "FTP";
            case 22: return "SSH";
            case 80: return "HTTP";
            case 443: return "HTTPS";
            case 8080: return "ALT-HTTP";
            default: return "CUSTOM";
        }
    }

    // =========================================================================
    // PROCEDURAL TEXTURE HELPERS (Zero external asset dependencies)
    // =========================================================================
    private static Sprite GetOrCreateLaserSprite()
    {
        if (_laserCoreSprite != null) return _laserCoreSprite;

        const int w = 32;
        const int h = 64;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            name = "ProceduralGateLaser",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat
        };

        var pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float normX = Mathf.Abs(x - (w * 0.5f - 0.5f)) / (w * 0.5f);
                float scanline = (y % 4 == 0) ? 0.75f : 1f;

                if (normX < 0.2f)
                {
                    pixels[y * w + x] = new Color(1f, 1f, 1f, 1f); // White Core
                }
                else if (normX < 0.65f)
                {
                    float a = Mathf.Lerp(1f, 0.4f, (normX - 0.2f) / 0.45f) * scanline;
                    pixels[y * w + x] = new Color(1f, 1f, 1f, a);
                }
                else if (normX < 0.95f)
                {
                    float a = Mathf.Lerp(0.4f, 0f, (normX - 0.65f) / 0.3f) * scanline;
                    pixels[y * w + x] = new Color(0.9f, 0.95f, 1f, a);
                }
                else
                {
                    pixels[y * w + x] = Color.clear;
                }
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();

        _laserCoreSprite = Sprite.Create(
            tex,
            new Rect(0, 0, w, h),
            new Vector2(0.5f, 0.5f),
            16f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(4, 4, 4, 4));
        return _laserCoreSprite;
    }

    private static Sprite GetOrCreatePylonSprite()
    {
        if (_pylonSprite != null) return _pylonSprite;

        const int w = 32;
        const int h = 24;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            name = "ProceduralGatePylon",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Metallic base mounting bracket
                if (y < 6)
                {
                    pixels[y * w + x] = new Color(0.12f, 0.18f, 0.25f, 1f); // Steel base
                }
                else if (y < 16 && x >= 4 && x < w - 4)
                {
                    pixels[y * w + x] = new Color(0.18f, 0.26f, 0.36f, 1f); // Housing
                }
                else if (y >= 16 && x >= 8 && x < w - 8)
                {
                    pixels[y * w + x] = new Color(0.3f, 0.95f, 1f, 1f); // Neon emitter nozzle
                }
                else
                {
                    pixels[y * w + x] = Color.clear;
                }
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();

        _pylonSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        return _pylonSprite;
    }

    private static Sprite GetOrCreatePanelSprite()
    {
        if (_panelSprite != null) return _panelSprite;

        const int w = 48;
        const int h = 24;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            name = "ProceduralHologramPanel",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool isBorder = (x <= 1 || x >= w - 2 || y <= 1 || y >= h - 2);
                if (isBorder)
                    pixels[y * w + x] = new Color(0.2f, 0.85f, 1f, 0.95f);
                else
                    pixels[y * w + x] = new Color(0.03f, 0.07f, 0.12f, 0.92f);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();

        _panelSprite = Sprite.Create(
            tex,
            new Rect(0, 0, w, h),
            new Vector2(0.5f, 0.5f),
            16f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(4, 4, 4, 4));
        return _panelSprite;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dynamic Multi-Layer Parallax Background System with Cyber Ambient Dust Particle Simulation.
/// Attaches seamlessly behind the game world (sortingOrder -50 to -30).
/// Generates world-specific cybernetic vistas:
/// - World 1 (Localhost): Silicon Motherboard Grid, Bus Lines, Green Neon Silhouettes
/// - World 2 (LAN Party): Fiber Cables, Towering Server Racks, Blue/Violet Terminal Lights
/// - World 3 (Fiber Deep): Undersea Datacenter Trenches, Glass Fiber Conduits, Aquamarine Glow
/// - World 4 (Satellite Relay): Orbital Carbon Truss, Solar Panels, Deep Space Starfield & Gold Ion Drift
/// - World 5 (Rootkit Core): Corrupted Digital Abyss, Red Glitch Monoliths, Crimson Gridlines
/// </summary>
public class ParallaxBackgroundSystem : MonoBehaviour
{
    public static ParallaxBackgroundSystem Instance { get; private set; }

    [System.Serializable]
    public class ParallaxLayer
    {
        public Transform transform;
        public float parallaxFactorX;
        public float parallaxFactorY;
        public float textureUnitSizeX;
        public Vector3 initialPos;
    }

    private Camera _cam;
    private Vector3 _lastCamPos;
    private Vector3 _camInitialPos;
    private readonly List<ParallaxLayer> _layers = new List<ParallaxLayer>();
    private int _currentWorld = -1;

    // Ambient floating dust / cyber bit simulation
    private GameObject _ambientParticleHost;

    public static ParallaxBackgroundSystem Ensure(int worldIndex)
    {
        if (Instance != null)
        {
            if (Instance._currentWorld != worldIndex)
            {
                Instance.RebuildForWorld(worldIndex);
            }
            return Instance;
        }

        var go = new GameObject("ParallaxBackgroundSystem");
        var sys = go.AddComponent<ParallaxBackgroundSystem>();
        sys.RebuildForWorld(worldIndex);
        return sys;
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
    }

    private void Start()
    {
        FindActiveCamera();
    }

    private void FindActiveCamera()
    {
        if (_cam == null || !_cam.gameObject.activeInHierarchy)
        {
            _cam = Camera.main;
            if (_cam != null)
            {
                _lastCamPos = _cam.transform.position;
            }
        }
    }

    public void RebuildForWorld(int worldIndex)
    {
        _currentWorld = Mathf.Clamp(worldIndex, 0, 4);
        _layers.Clear();

        // Clear existing children
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        FindActiveCamera();
        if (_cam != null)
        {
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = _currentWorld switch
            {
                0 => new Color(0.015f, 0.035f, 0.025f, 1f),
                1 => new Color(0.020f, 0.035f, 0.080f, 1f),
                2 => new Color(0.010f, 0.040f, 0.050f, 1f),
                3 => new Color(0.035f, 0.030f, 0.050f, 1f),
                4 => new Color(0.050f, 0.010f, 0.020f, 1f),
                _ => new Color(0.015f, 0.035f, 0.025f, 1f)
            };
        }
        Vector3 camPos = _cam != null ? _cam.transform.position : Vector3.zero;
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            camPos = new Vector3(player.transform.position.x, player.transform.position.y + 1.2f, -10f);
        }
        else if (_currentWorld == 0)
        {
            camPos = new Vector3(0f, -16f, -10f);
        }
        _camInitialPos = camPos;
        _lastCamPos = camPos;

        // Build 3 layered parallax planes: Far (-50), Mid (-40), Near (-30)
        BuildLayer(camPos, 0, 0.05f, 0.02f, -50, GetFarLayerColor(_currentWorld), "FarSkyLayer");
        BuildLayer(camPos, 1, 0.20f, 0.08f, -40, GetMidLayerColor(_currentWorld), "MidInfrastructureLayer");
        BuildLayer(camPos, 2, 0.45f, 0.15f, -30, GetNearLayerColor(_currentWorld), "NearCircuitLayer");

        // Attach ambient floating cyber particles
        SetupAmbientParticles(camPos, _currentWorld);
    }

    private void BuildLayer(Vector3 camPos, int layerIndex, float factorX, float factorY, int sortOrder, Color tint, string layerName)
    {
        var layerObj = new GameObject(layerName);
        layerObj.transform.SetParent(transform, false);

        // Generate or load procedural procedural tile for this layer & world
        Sprite layerSprite = GetOrCreateLayerSprite(_currentWorld, layerIndex);

        // We create 3 contiguous segments (-1, 0, +1) so it loops infinitely horizontally
        float unitWidth = 48f; // World units width
        float unitHeight = 36f; // World units height (covers vertical viewport fully from sky to subterranean depths)

        var rootLayer = layerObj.transform;
        rootLayer.position = new Vector3(camPos.x, camPos.y, 0f);

        for (int i = -1; i <= 1; i++)
        {
            var segment = new GameObject($"{layerName}_Segment_{i}");
            segment.transform.SetParent(rootLayer, false);
            segment.transform.localPosition = new Vector3(i * unitWidth, 0f, 0f);

            var sr = segment.AddComponent<SpriteRenderer>();
            sr.sprite = layerSprite;
            sr.color = tint;
            sr.sortingOrder = sortOrder;
            sr.drawMode = SpriteDrawMode.Simple;
            if (layerSprite != null)
            {
                float spriteW = layerSprite.rect.width / layerSprite.pixelsPerUnit;
                float spriteH = layerSprite.rect.height / layerSprite.pixelsPerUnit;
                segment.transform.localScale = new Vector3((unitWidth + 0.1f) / Mathf.Max(0.01f, spriteW), unitHeight / Mathf.Max(0.01f, spriteH), 1f);
            }
        }

        _layers.Add(new ParallaxLayer
        {
            transform = rootLayer,
            parallaxFactorX = factorX,
            parallaxFactorY = factorY,
            textureUnitSizeX = unitWidth,
            initialPos = rootLayer.position
        });
    }

    private void LateUpdate()
    {
        FindActiveCamera();
        if (_cam == null) return;

        Vector3 camPos = _cam.transform.position;
        Vector3 delta = camPos - _lastCamPos;

        for (int i = 0; i < _layers.Count; i++)
        {
            var l = _layers[i];
            if (l.transform == null) continue;

            // Move with camera offset according to parallax factor
            float newX = l.transform.position.x + delta.x * l.parallaxFactorX;
            float newY = camPos.y + (camPos.y - _camInitialPos.y) * l.parallaxFactorY;
            l.transform.position = new Vector3(newX, newY, 0f);

            // Infinite loop wrapping
            float distFromCamX = camPos.x - l.transform.position.x;
            if (Mathf.Abs(distFromCamX) >= l.textureUnitSizeX)
            {
                float offset = (distFromCamX > 0 ? 1 : -1) * l.textureUnitSizeX;
                l.transform.position += new Vector3(offset, 0f, 0f);
            }
        }

        if (_ambientParticleHost != null)
        {
            _ambientParticleHost.transform.position = new Vector3(camPos.x, camPos.y, 0f);
            UpdateAmbientMotes(camPos);
        }

        _lastCamPos = camPos;
    }

    private struct AmbientMote
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public Vector2 velocity;
        public float life;
        public float maxLife;
        public float baseAlpha;
        public float swayPhase;
        public float swaySpeed;
        public float swayAmount;
        public float baseScale;
        public float spinSpeed;
        public bool isScanline;
        public Color primaryColor;
    }

    private readonly List<AmbientMote> _ambientMotes = new List<AmbientMote>();
    private const int TOTAL_AMBIENT_MOTES = 68;

    private void SetupAmbientParticles(Vector3 camPos, int world)
    {
        if (_ambientParticleHost != null)
        {
            Destroy(_ambientParticleHost);
            _ambientMotes.Clear();
        }

        _ambientParticleHost = new GameObject("AmbientCyberAtmosphereHost");
        _ambientParticleHost.transform.SetParent(transform, false);
        _ambientParticleHost.transform.position = camPos;

        Sprite[] availableSprites = new Sprite[]
        {
            Resources.Load<Sprite>("GeneratedArt/particle_binary_0"),
            Resources.Load<Sprite>("GeneratedArt/particle_binary_1"),
            Resources.Load<Sprite>("GeneratedArt/particle_cyber_spark"),
            Resources.Load<Sprite>("GeneratedArt/particle_del_bit"),
            Resources.Load<Sprite>("GeneratedArt/particle_data_glitch"),
            Resources.Load<Sprite>("GeneratedArt/particle_loopback_ring")
        };
        Sprite fallbackSprite = Resources.Load<Sprite>("GeneratedArt/databit");

        for (int i = 0; i < TOTAL_AMBIENT_MOTES; i++)
        {
            var go = new GameObject($"CyberMote_{i}");
            go.transform.SetParent(_ambientParticleHost.transform, false);

            float rx = UnityEngine.Random.Range(-20f, 20f);
            float ry = UnityEngine.Random.Range(-12f, 12f);
            go.transform.localPosition = new Vector3(rx, ry, 0f);

            // Three depth planes: Foreground Bokeh (10), Midfield Active (42), Deep Matrix Atmosphere (16)
            int sortOrder = 15;
            float baseScale = UnityEngine.Random.Range(0.18f, 0.28f);
            float alpha = 0.90f;

            if (i < 10)
            {
                // Foreground Bokeh (drifts across camera lens for cinematic depth)
                sortOrder = 45;
                baseScale = UnityEngine.Random.Range(0.42f, 0.68f);
                alpha = 0.38f;
            }
            else if (i >= 52)
            {
                // Deep Matrix Atmosphere (subtle ambient background)
                sortOrder = -5;
                baseScale = UnityEngine.Random.Range(0.12f, 0.22f);
                alpha = 0.45f;
            }

            var sr = go.AddComponent<SpriteRenderer>();

            // Pick sprite archetype
            int spriteIdx = i % availableSprites.Length;
            Sprite s = availableSprites[spriteIdx];
            if (s == null) s = fallbackSprite;
            sr.sprite = s;

            bool isGlitch = s != null && s.name.Contains("glitch");
            bool isBinary = s != null && (s.name.Contains("binary") || s.name.Contains("0") || s.name.Contains("1"));
            bool isSpark = s != null && s.name.Contains("spark");
            bool isDel = s != null && s.name.Contains("del");

            Color pColor = GetParticleColorVariant(world, i);
            sr.color = pColor;
            sr.sortingOrder = sortOrder;

            // Archetype-specific physics
            Vector2 vel;
            float spin = 0f;
            if (isBinary)
            {
                // Cascading digital rain downwards
                vel = new Vector2(UnityEngine.Random.Range(-0.15f, 0.15f), -UnityEngine.Random.Range(0.6f, 1.4f));
            }
            else if (isDel)
            {
                // Tumbling discarded scraps
                vel = new Vector2(UnityEngine.Random.Range(-0.35f, 0.35f), UnityEngine.Random.Range(-0.4f, 0.4f));
                spin = UnityEngine.Random.Range(-110f, 110f);
            }
            else if (isSpark)
            {
                // Rising electric sparks
                vel = new Vector2(UnityEngine.Random.Range(-0.3f, 0.3f), UnityEngine.Random.Range(0.35f, 0.85f));
                spin = UnityEngine.Random.Range(-80f, 80f);
            }
            else
            {
                // Ambient gentle drift
                vel = new Vector2(UnityEngine.Random.Range(-0.4f, 0.4f), UnityEngine.Random.Range(0.2f, 0.6f));
            }

            _ambientMotes.Add(new AmbientMote
            {
                transform = go.transform,
                renderer = sr,
                velocity = vel,
                life = UnityEngine.Random.Range(0f, 5f),
                maxLife = UnityEngine.Random.Range(4.0f, 7.5f),
                baseAlpha = alpha,
                swayPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                swaySpeed = UnityEngine.Random.Range(1.4f, 3.2f),
                swayAmount = UnityEngine.Random.Range(0.35f, 1.1f),
                baseScale = baseScale,
                spinSpeed = spin,
                isScanline = isGlitch,
                primaryColor = pColor
            });
        }
    }

    private void UpdateAmbientMotes(Vector3 camPos)
    {
        if (_ambientParticleHost == null || _ambientMotes.Count == 0) return;

        float dt = Time.unscaledDeltaTime;
        for (int i = 0; i < _ambientMotes.Count; i++)
        {
            var mote = _ambientMotes[i];
            if (mote.transform == null) continue;

            mote.life += dt;
            Vector3 pos = mote.transform.localPosition;

            // Dual-harmonic sine wave turbulence for organic floating
            float sway = (Mathf.Sin(mote.life * mote.swaySpeed + mote.swayPhase) * 0.7f +
                          Mathf.Cos(mote.life * (mote.swaySpeed * 1.5f) + mote.swayPhase) * 0.3f) * mote.swayAmount;
            pos.x += (mote.velocity.x + sway) * dt;
            pos.y += mote.velocity.y * dt;

            // Micro-glitch horizontal jitter
            if (UnityEngine.Random.value < 0.012f)
            {
                pos.x += UnityEngine.Random.Range(-0.25f, 0.25f);
            }

            // Viewport boundary wrapping
            if (pos.y > 12f) pos.y = -12f;
            else if (pos.y < -12f) pos.y = 12f;

            if (pos.x > 21f) pos.x = -21f;
            else if (pos.x < -21f) pos.x = 21f;

            mote.transform.localPosition = pos;

            // Dynamic rotation oscillation / tumbling spin
            if (mote.spinSpeed != 0f)
            {
                mote.transform.localEulerAngles = new Vector3(0f, 0f, mote.life * mote.spinSpeed);
            }
            else
            {
                mote.transform.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(mote.life * 2.2f + mote.swayPhase) * 18f);
            }

            // Scale breathing & pulsating
            float scalePulse = mote.baseScale * (1f + 0.22f * Mathf.Sin(mote.life * 3.8f + mote.swayPhase));
            mote.transform.localScale = mote.isScanline
                ? new Vector3(scalePulse * 2.4f, scalePulse * 0.75f, 1f)
                : new Vector3(scalePulse, scalePulse, 1f);

            // Dynamic alpha breathing, smooth lifecycle fade & high-frequency twinkle
            if (mote.renderer != null)
            {
                float t = Mathf.PingPong(mote.life, mote.maxLife * 0.5f) / (mote.maxLife * 0.5f);
                float twinkle = 0.85f + 0.15f * Mathf.Sin(mote.life * 16f + mote.swayPhase);
                Color c = mote.primaryColor;
                c.a = mote.baseAlpha * Mathf.Lerp(0.25f, 1f, t) * twinkle;
                mote.renderer.color = c;
            }

            _ambientMotes[i] = mote;
        }
    }

    // -------------------------------------------------------------------------
    // Visual Palette & Sprite Factories
    // -------------------------------------------------------------------------
    private static Color GetFarLayerColor(int world)
    {
        switch (world)
        {
            case 0: return new Color(0.40f, 0.52f, 0.48f, 0.90f); // World 1: Localhost Far CRT Matrix Void
            case 1: return new Color(0.35f, 0.42f, 0.65f, 0.95f); // World 2: Subnet Hub Indigo Twilight
            case 2: return new Color(0.25f, 0.50f, 0.52f, 0.95f); // World 3: Darknet Trench Teal
            case 3: return new Color(0.42f, 0.38f, 0.58f, 0.95f); // World 4: Orbital Space Void
            case 4: return new Color(0.55f, 0.22f, 0.28f, 0.95f); // World 5: Rootkit Core Abyss
            default: return new Color(0.3f, 0.3f, 0.4f, 0.95f);
        }
    }

    private static Color GetMidLayerColor(int world)
    {
        switch (world)
        {
            case 0: return new Color(0.50f, 0.65f, 0.60f, 0.90f); // World 1: Recycle Bin & Memory Stacks
            case 1: return new Color(0.50f, 0.60f, 0.85f, 0.92f); // World 2: Server rack clusters
            case 2: return new Color(0.38f, 0.68f, 0.70f, 0.92f); // World 3: Classified Vault Towers
            case 3: return new Color(0.60f, 0.55f, 0.75f, 0.92f); // World 4: Solar Truss Towers & Ion Drives
            case 4: return new Color(0.70f, 0.30f, 0.36f, 0.92f); // World 5: Corrupted Monolith & BSOD windows
            default: return new Color(0.5f, 0.5f, 0.6f, 0.92f);
        }
    }

    private static Color GetNearLayerColor(int world)
    {
        switch (world)
        {
            case 0: return new Color(0.55f, 0.70f, 0.66f, 0.80f); // World 1: Ambient conduits & particles
            case 1: return new Color(0.65f, 0.75f, 0.95f, 0.88f); // World 2: Cable bundles & zip ties
            case 2: return new Color(0.48f, 0.82f, 0.82f, 0.88f); // World 3: Subsea Repeaters & glowing fiber
            case 3: return new Color(0.78f, 0.72f, 0.90f, 0.88f); // World 4: Thermal radiators & antenna arrays
            case 4: return new Color(0.85f, 0.40f, 0.46f, 0.88f); // World 5: Core veins & glitch fragments
            default: return new Color(0.7f, 0.7f, 0.8f, 0.88f);
        }
    }

    private static Color GetParticleColor(int world)
    {
        switch (world)
        {
            case 0: return new Color(0f, 1f, 0.65f, 0.75f); // Cyan-green data sparkles
            case 1: return new Color(0.35f, 0.65f, 1f, 0.75f); // Blue packets
            case 2: return new Color(0f, 0.95f, 0.95f, 0.75f); // Aquamarine photon pulses
            case 3: return new Color(1f, 0.82f, 0.25f, 0.75f); // Gold ion flecks
            case 4: return new Color(1f, 0.25f, 0.35f, 0.85f); // Red corruption artifacts
            default: return Color.white;
        }
    }

    private static Color GetParticleColorVariant(int world, int variantIndex)
    {
        switch (world)
        {
            case 0: // World 1: Localhost / Recycle Bin (Matrix Phosphor Green, Cyan, Amber, Corrupted Pink)
                int v0 = variantIndex % 4;
                if (v0 == 0) return new Color(0f, 1f, 0.45f, 0.85f); // Matrix Phosphor Green
                if (v0 == 1) return new Color(0f, 0.9f, 1f, 0.85f);  // Localhost Cyan
                if (v0 == 2) return new Color(1f, 0.78f, 0.15f, 0.85f); // Trash Amber
                return new Color(1f, 0.2f, 0.45f, 0.85f);            // Corrupted Pink / Glitch

            case 1: // World 2: Subnet Hub / Router
                int v1 = variantIndex % 3;
                if (v1 == 0) return new Color(0.2f, 0.7f, 1f, 0.85f);
                if (v1 == 1) return new Color(0.1f, 0.95f, 1f, 0.85f);
                return new Color(0.6f, 0.4f, 1f, 0.85f);

            case 2: // World 3: Darknet Alley / Subsea
                int v2 = variantIndex % 3;
                if (v2 == 0) return new Color(0f, 0.95f, 0.85f, 0.85f);
                if (v2 == 1) return new Color(0.7f, 0.2f, 1f, 0.85f);
                return new Color(0.1f, 1f, 0.6f, 0.85f);

            case 3: // World 4: Overclock Core / Thermal
                int v3 = variantIndex % 3;
                if (v3 == 0) return new Color(1f, 0.85f, 0.2f, 0.85f);
                if (v3 == 1) return new Color(1f, 0.5f, 0.1f, 0.85f);
                return new Color(1f, 0.2f, 0.15f, 0.85f);

            case 4: // World 5: Rootkit Kernel / Void
                int v4 = variantIndex % 3;
                if (v4 == 0) return new Color(1f, 0.15f, 0.25f, 0.85f);
                if (v4 == 1) return new Color(0.8f, 0f, 0.2f, 0.85f);
                return new Color(1f, 1f, 1f, 0.9f);

            default:
                return GetParticleColor(world);
        }
    }

    private static Sprite GetOrCreateLayerSprite(int world, int layerIndex)
    {
        string spriteName = $"GeneratedArt/bg_w{world + 1}_layer{layerIndex}";
        var existing = Resources.Load<Sprite>(spriteName);
        if (existing != null) return existing;

        // Fallback procedural texture if resource png not loaded - use deep cyber tone, NOT white
        int w = 64;
        int h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color baseCol = new Color(0.04f, 0.08f, 0.14f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Procedural scanlines and vertical circuit traces
                bool isTrace = (x % 16 == 0) || (y % 8 == 0 && x % 4 == 0);
                float alpha = isTrace ? 0.60f : 0.25f - (float)y / h * 0.15f;
                tex.SetPixel(x, y, new Color(baseCol.r, baseCol.g, baseCol.b, alpha));
            }
        }
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f, 0, SpriteMeshType.FullRect);
    }
}

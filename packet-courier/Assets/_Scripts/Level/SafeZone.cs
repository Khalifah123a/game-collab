using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Safe Zone: an area where the courier is untouchable. While the player stands inside
/// one, enemy aggro is dropped, hazards deal nothing and the TTL drain pauses.
/// Features a cyber holographic forcefield, boundary pylons, energy rails and data motes.
/// </summary>
/// <remarks>
/// Detection is a plain overlap counter rather than a single reference so several
/// zones can overlap without one leaving the player "unsafe" while another still
/// holds them. <see cref="IsPlayerSafe"/> is a plain static read with no scene
/// lookups, so EditMode tooling and tests can poll it safely when no zone exists.
/// Install zones with <c>Tools &gt; Packet Courier &gt; Install Safe Zones</c>.
/// </remarks>
[RequireComponent(typeof(BoxCollider2D))]
public class SafeZone : MonoBehaviour
{
    [Header("Visual (Cyber Forcefield)")]
    [Tooltip("Assign a SpriteRenderer here to replace the generated placeholder fill.")]
    public SpriteRenderer visual;
    public Color tint = new Color(0.15f, 0.95f, 0.85f, 1f);
    [Range(0f, 1f)] public float fillOpacity = 0.22f;
    public int sortingOrder = 1;

    [Header("Border Gate (Pintu Perbatasan Safe Zone)")]
    [Tooltip("Door posts at the left/right edges marking the safe-zone boundary. Visual only, no collider.")]
    public bool showBorder = true;
    public Color borderColor = new Color(0.1f, 0.85f, 1f, 1f);

    [Header("Hologram & Ambiance")]
    public bool showHoloBanner = true;
    public bool showDataMotes = true;

    /// <summary>True while the player is inside at least one safe zone.</summary>
    public static bool IsPlayerSafe => _overlaps > 0;

    private static int _overlaps;
    private static Sprite _placeholder;
    private static Sprite _beaconSprite;
    private static Sprite _moteSprite;

    private BoxCollider2D _zone;
    private bool _counted;
    private Transform _borderWest;
    private Transform _borderEast;
    private SpriteRenderer _railTop;
    private SpriteRenderer _railBottom;
    private SpriteRenderer _beaconWest;
    private SpriteRenderer _beaconEast;
    private TextMesh _bannerText;
    private Transform _bannerTransform;
    private float _bannerBaseY;

    // Data motes
    private struct DataMote
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public float speed;
        public float seed;
    }
    private readonly List<DataMote> _motes = new List<DataMote>();

    // Visual pulse & entry reaction
    private float _entryPulseTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOverlapCount()
    {
        _overlaps = 0;
    }

    private void Awake()
    {
        _zone = GetComponent<BoxCollider2D>();
        if (_zone != null) _zone.isTrigger = true;
    }

    private void Start()
    {
        EnsurePlaceholderVisual();
    }

    public void ConfigureAsInvisible()
    {
        fillOpacity = 0f;
        showBorder = false;
        showHoloBanner = false;
        showDataMotes = false;
        EnsurePlaceholderVisual();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        if (_counted) return;
        _counted = true;
        _overlaps++;
        _entryPulseTimer = 0.5f; // Flash on entry
        AudioManager.SetMuffled(true, 0.6f);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        ReleaseOverlap();
    }

    private void OnDisable()
    {
        ReleaseOverlap();
    }

    private void OnDestroy()
    {
        ReleaseOverlap();
    }

    private void ReleaseOverlap()
    {
        if (!_counted) return;
        _counted = false;
        if (_overlaps > 0) _overlaps--;
        if (_overlaps == 0)
        {
            AudioManager.SetMuffled(false, 0.6f);
        }
    }

    private static bool IsPlayer(Collider2D other)
    {
        if (other == null) return false;
        return other.GetComponentInParent<PlayerController>() != null;
    }

    private void Update()
    {
        if (visual == null) return;

        float time = Time.time;
        float pulse = 0.5f + 0.5f * Mathf.Sin(time * 2.5f);

        if (_entryPulseTimer > 0f)
            _entryPulseTimer -= Time.deltaTime;

        // Dynamic shield brightness
        float activeBoost = _counted ? 0.08f : 0f;
        float entryFlash = Mathf.Max(0f, _entryPulseTimer) * 0.25f;
        float alpha = Mathf.Clamp01(fillOpacity + pulse * 0.05f + activeBoost + entryFlash);

        Color c = tint;
        c.a = alpha;
        visual.color = c;

        // Energy rails pulse
        if (_railTop != null && _railBottom != null)
        {
            Color railCol = borderColor;
            railCol.a = Mathf.Clamp01(0.6f + pulse * 0.35f + entryFlash);
            _railTop.color = railCol;
            _railBottom.color = railCol;
        }

        // Pylon beacons pulse & bob
        if (_beaconWest != null && _beaconEast != null)
        {
            Color beaconCol = tint;
            beaconCol.a = Mathf.Clamp01(0.75f + pulse * 0.25f + entryFlash);
            _beaconWest.color = beaconCol;
            _beaconEast.color = beaconCol;
        }

        // Floating banner gentle bobbing
        if (_bannerTransform != null)
        {
            float bob = Mathf.Sin(time * 1.8f) * 0.06f;
            _bannerTransform.localPosition = new Vector3(_zone != null ? _zone.offset.x : 0f, _bannerBaseY + bob, 0f);
            if (_bannerText != null)
            {
                Color textCol = _counted ? new Color(0.3f, 1f, 0.9f, 0.95f) : new Color(0.6f, 0.95f, 1f, 0.8f);
                _bannerText.color = textCol;
            }
        }

        // Animate floating data motes
        if (_zone != null && _motes.Count > 0)
        {
            float halfW = _zone.size.x * 0.45f;
            float bottomY = _zone.offset.y - _zone.size.y * 0.45f;
            float topY = _zone.offset.y + _zone.size.y * 0.45f;

            for (int i = 0; i < _motes.Count; i++)
            {
                var mote = _motes[i];
                if (mote.transform == null) continue;

                Vector3 pos = mote.transform.localPosition;
                pos.y += mote.speed * Time.deltaTime;
                pos.x += Mathf.Sin(time * 2f + mote.seed) * 0.2f * Time.deltaTime;

                if (pos.y > topY)
                {
                    pos.y = bottomY;
                    pos.x = _zone.offset.x + Mathf.Sin(mote.seed * 3.7f) * halfW;
                }

                mote.transform.localPosition = pos;
                float heightNorm = Mathf.InverseLerp(bottomY, topY, pos.y);
                float moteAlpha = Mathf.Sin(heightNorm * Mathf.PI) * 0.65f;
                Color mc = tint;
                mc.a = moteAlpha;
                mote.renderer.color = mc;
            }
        }
    }

    public void EnsurePlaceholderVisual()
    {
        if (fillOpacity <= 0.001f && !showBorder && !showHoloBanner && !showDataMotes)
        {
            if (visual != null) { Destroy(visual.gameObject); visual = null; }
            if (_borderWest != null) { Destroy(_borderWest.gameObject); _borderWest = null; }
            if (_borderEast != null) { Destroy(_borderEast.gameObject); _borderEast = null; }
            if (_railTop != null) { Destroy(_railTop.gameObject); _railTop = null; }
            if (_railBottom != null) { Destroy(_railBottom.gameObject); _railBottom = null; }
            if (_bannerTransform != null) { Destroy(_bannerTransform.gameObject); _bannerTransform = null; }
            foreach (var m in _motes) if (m.transform != null) Destroy(m.transform.gameObject);
            _motes.Clear();
            return;
        }
        if (visual != null) return;

        var child = new GameObject("SafeZone Visual");
        child.transform.SetParent(transform, false);
        visual = child.AddComponent<SpriteRenderer>();
        visual.sprite = PlaceholderSprite();
        visual.sortingOrder = sortingOrder;
        var color = tint;
        color.a = fillOpacity;
        visual.color = color;
        if (fillOpacity <= 0.01f) visual.enabled = false;

        if (_zone != null)
        {
            child.transform.localPosition = _zone.offset;
            var scale = _zone.size / PlaceholderSprite().bounds.size;
            child.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            if (showBorder)
            {
                BuildEnergyRails();
                BuildBorderPosts();
            }
            if (showHoloBanner) BuildHoloBanner();
            if (showDataMotes) BuildDataMotes();
        }
    }

    private void BuildEnergyRails()
    {
        if (_zone == null) return;
        float halfH = _zone.size.y * 0.5f;

        // Top rail
        var top = new GameObject("Rail Top");
        top.transform.SetParent(transform, false);
        top.transform.localPosition = new Vector3(_zone.offset.x, _zone.offset.y + halfH, 0f);
        _railTop = top.AddComponent<SpriteRenderer>();
        _railTop.sprite = PlaceholderSprite();
        _railTop.sortingOrder = sortingOrder + 2;
        var unit = PlaceholderSprite().bounds.size;
        top.transform.localScale = new Vector3(_zone.size.x / unit.x, 0.12f / unit.y, 1f);

        // Bottom rail
        var bottom = new GameObject("Rail Bottom");
        bottom.transform.SetParent(transform, false);
        bottom.transform.localPosition = new Vector3(_zone.offset.x, _zone.offset.y - halfH, 0f);
        _railBottom = bottom.AddComponent<SpriteRenderer>();
        _railBottom.sprite = PlaceholderSprite();
        _railBottom.sortingOrder = sortingOrder + 2;
        bottom.transform.localScale = new Vector3(_zone.size.x / unit.x, 0.12f / unit.y, 1f);
    }

    private void BuildBorderPosts()
    {
        if (!showBorder || _zone == null) return;
        if (_borderWest != null && _borderEast != null) return;
        float halfW = _zone.size.x * 0.5f;
        float height = Mathf.Max(1f, _zone.size.y);
        _borderWest = BuildBorderPost("Border West", new Vector2(_zone.offset.x - halfW, _zone.offset.y), height, true);
        _borderEast = BuildBorderPost("Border East", new Vector2(_zone.offset.x + halfW, _zone.offset.y), height, false);
    }

    private Transform BuildBorderPost(string name, Vector2 localPos, float height, bool isWest)
    {
        var postRoot = new GameObject(name);
        postRoot.transform.SetParent(transform, false);
        postRoot.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);

        // Outer chassis post
        var frame = new GameObject("Pylon Chassis");
        frame.transform.SetParent(postRoot.transform, false);
        var sprite = frame.AddComponent<SpriteRenderer>();
        sprite.sprite = PlaceholderSprite();
        var color = borderColor;
        color.a = 0.95f;
        sprite.color = color;
        sprite.sortingOrder = sortingOrder + 2;
        var unit = PlaceholderSprite().bounds.size;
        frame.transform.localScale = new Vector3(0.35f / unit.x, height / unit.y, 1f);

        // Inner glowing laser column
        var core = new GameObject("Energy Core");
        core.transform.SetParent(postRoot.transform, false);
        var coreSprite = core.AddComponent<SpriteRenderer>();
        coreSprite.sprite = PlaceholderSprite();
        coreSprite.color = tint;
        coreSprite.sortingOrder = sortingOrder + 3;
        core.transform.localScale = new Vector3(0.12f / unit.x, (height - 0.2f) / unit.y, 1f);

        // Top emitter beacon orb
        var beacon = new GameObject("Emitter Beacon");
        beacon.transform.SetParent(postRoot.transform, false);
        beacon.transform.localPosition = new Vector3(0f, height * 0.5f + 0.15f, 0f);
        var bSprite = beacon.AddComponent<SpriteRenderer>();
        bSprite.sprite = BeaconSprite();
        bSprite.color = tint;
        bSprite.sortingOrder = sortingOrder + 4;
        beacon.transform.localScale = Vector3.one * 0.5f;

        if (isWest) _beaconWest = bSprite;
        else _beaconEast = bSprite;

        return postRoot.transform;
    }

    private void BuildHoloBanner()
    {
        if (!showHoloBanner || _zone == null) return;

        float halfH = _zone.size.y * 0.5f;
        _bannerBaseY = _zone.offset.y + halfH - 0.75f;

        var banner = new GameObject("HoloBanner");
        banner.transform.SetParent(transform, false);
        _bannerTransform = banner.transform;
        _bannerTransform.localPosition = new Vector3(_zone.offset.x, _bannerBaseY, 0f);

        _bannerText = banner.AddComponent<TextMesh>();
        _bannerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _bannerText.fontSize = 44;
        _bannerText.characterSize = 0.042f;
        _bannerText.anchor = TextAnchor.MiddleCenter;
        _bannerText.alignment = TextAlignment.Center;
        _bannerText.text = "< SAFE ZONE // BUFFER TERMINAL >\nFIREWALL SECURE \u2022 TTL PAUSED";
        _bannerText.color = new Color(0.4f, 0.95f, 1f, 0.9f);

        var mr = banner.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = sortingOrder + 5;
    }

    private void BuildDataMotes()
    {
        if (!showDataMotes || _zone == null) return;
        _motes.Clear();

        const int count = 8;
        float halfW = _zone.size.x * 0.42f;
        float halfH = _zone.size.y * 0.42f;

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject($"DataMote_{i}");
            go.transform.SetParent(transform, false);
            float startX = _zone.offset.x + Random.Range(-halfW, halfW);
            float startY = _zone.offset.y + Random.Range(-halfH, halfH);
            go.transform.localPosition = new Vector3(startX, startY, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = MoteSprite();
            sr.color = tint;
            sr.sortingOrder = sortingOrder + 1;
            go.transform.localScale = Vector3.one * Random.Range(0.1f, 0.2f);

            _motes.Add(new DataMote
            {
                transform = go.transform,
                renderer = sr,
                speed = Random.Range(0.6f, 1.4f),
                seed = Random.Range(0f, 100f)
            });
        }
    }

    private static Sprite PlaceholderSprite()
    {
        if (_placeholder != null) return _placeholder;

        var loaded = Resources.Load<Sprite>("GeneratedArt/safezone_field");
        if (loaded != null)
        {
            _placeholder = loaded;
            return _placeholder;
        }

        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "SafeZone_Placeholder",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isBorder = x == 0 || x == size - 1 || y == 0 || y == size - 1;
                bool isInnerBorder = x == 1 || x == size - 2 || y == 1 || y == size - 2;
                bool isScanline = (y % 4 == 0);
                bool isHexDiag = ((x + y) % 8 == 0) || ((x - y) % 8 == 0);

                if (isBorder)
                    pixels[y * size + x] = new Color(0.6f, 1f, 0.95f, 1f);
                else if (isInnerBorder)
                    pixels[y * size + x] = new Color(0.4f, 0.9f, 1f, 0.75f);
                else if (isHexDiag && isScanline)
                    pixels[y * size + x] = new Color(0.75f, 1f, 0.95f, 0.6f);
                else if (isScanline)
                    pixels[y * size + x] = new Color(0.65f, 0.95f, 1f, 0.42f);
                else
                    pixels[y * size + x] = new Color(0.45f, 0.85f, 1f, 0.25f);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();

        _placeholder = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        _placeholder.name = "SafeZone_Placeholder";
        _placeholder.hideFlags = HideFlags.HideAndDontSave;
        return _placeholder;
    }

    private static Sprite BeaconSprite()
    {
        if (_beaconSprite != null) return _beaconSprite;

        var loaded = Resources.Load<Sprite>("GeneratedArt/safezone_beacon");
        if (loaded != null)
        {
            _beaconSprite = loaded;
            return _beaconSprite;
        }

        const int size = 16;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "SafeZone_Beacon",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        var pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.48f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                if (dist <= radius)
                {
                    float falloff = 1f - (dist / radius);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.SmoothStep(0.2f, 1f, falloff));
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();

        _beaconSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        _beaconSprite.name = "SafeZone_Beacon";
        _beaconSprite.hideFlags = HideFlags.HideAndDontSave;
        return _beaconSprite;
    }

    private static Sprite MoteSprite()
    {
        if (_moteSprite != null) return _moteSprite;

        const int size = 8;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "SafeZone_Mote",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        var pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        texture.SetPixels(pixels);
        texture.Apply();

        _moteSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        _moteSprite.name = "SafeZone_Mote";
        _moteSprite.hideFlags = HideFlags.HideAndDontSave;
        return _moteSprite;
    }

    private void OnDrawGizmos()
    {
        DrawGizmoBox(false);
    }

    private void OnDrawGizmosSelected()
    {
        DrawGizmoBox(true);
    }

    private void DrawGizmoBox(bool selected)
    {
        var zone = _zone != null ? _zone : GetComponent<BoxCollider2D>();
        if (zone == null) return;

        var color = tint;
        color.a = selected ? 0.35f : 0.15f;
        Gizmos.color = color;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(zone.offset, zone.size);
        color.a = selected ? 1f : 0.5f;
        Gizmos.color = color;
        Gizmos.DrawWireCube(zone.offset, zone.size);
    }
}

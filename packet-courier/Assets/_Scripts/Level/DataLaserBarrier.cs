using UnityEngine;

/// <summary>
/// World 3 (Fiber Deep) laser barrier.
/// Controlled by one or more DataBlasterSwitch objects.
/// Blocks movement or damages player when closed; dissolves safely when opened.
/// Always ensures a procedural glowing laser texture so it NEVER becomes invisible.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class DataLaserBarrier : MonoBehaviour
{
    [Header("Barrier State")]
    public bool isOpen;
    public float damageOnTouch = 15f;

    private BoxCollider2D _collider;
    private SpriteRenderer _renderer;
    private float _damageCooldown;
    private static Sprite _proceduralLaserSprite;

    private void Awake()
    {
        _collider = GetComponent<BoxCollider2D>();
        _renderer = GetComponent<SpriteRenderer>();

        if (_renderer == null)
        {
            var child = transform.Find("BarrierVisual");
            if (child != null)
            {
                _renderer = child.GetComponent<SpriteRenderer>();
            }
            else
            {
                var go = new GameObject("BarrierVisual", typeof(SpriteRenderer));
                go.transform.SetParent(transform, false);
                _renderer = go.GetComponent<SpriteRenderer>();
            }
        }

        var loaded = Resources.Load<Sprite>("GeneratedArt/hazard_laser");
        _renderer.sprite = loaded != null ? loaded : GetOrCreateLaserSprite();
        _renderer.drawMode = SpriteDrawMode.Sliced;
        _renderer.size = _collider != null ? _collider.size : new Vector2(1.5f, 6f);
        _renderer.sortingOrder = 6;
        SetBarrierOpen(isOpen);
    }

    private void Update()
    {
        if (_renderer != null && !isOpen)
        {
            // Electric pulse animation
            float pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * 9f);
            _renderer.color = new Color(1f, 0.2f, 0.35f, 0.88f * pulse);
        }
    }

    public void SetBarrierOpen(bool open)
    {
        isOpen = open;
        if (_collider != null)
        {
            _collider.isTrigger = open;
            _collider.enabled = !open;
        }

        if (_renderer != null)
        {
            Color c = open ? new Color(0.1f, 1f, 0.85f, 0.12f) : new Color(1f, 0.2f, 0.35f, 0.9f);
            _renderer.color = c;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (isOpen) return;
        if (Time.time < _damageCooldown) return;

        var health = other.GetComponentInParent<PlayerHealth>();
        if (health != null && !SafeZone.IsPlayerSafe)
        {
            health.TakeDamage(damageOnTouch, PlayerHealth.DamageType.TTLDamage);
            _damageCooldown = Time.time + 0.5f;
        }
    }

    private static Sprite GetOrCreateLaserSprite()
    {
        if (_proceduralLaserSprite != null) return _proceduralLaserSprite;

        const int w = 16;
        const int h = 32;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            name = "ProceduralHazardLaser",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float distFromCenter = Mathf.Abs(x - (w * 0.5f - 0.5f)) / (w * 0.5f);
                if (distFromCenter < 0.25f)
                {
                    pixels[y * w + x] = new Color(1f, 1f, 1f, 1f); // White core
                }
                else if (distFromCenter < 0.65f)
                {
                    pixels[y * w + x] = new Color(1f, 0.25f, 0.35f, 0.95f); // Neon red beam
                }
                else if (distFromCenter < 0.95f)
                {
                    pixels[y * w + x] = new Color(0.7f, 0.05f, 0.2f, 0.6f); // Soft aura
                }
                else
                {
                    pixels[y * w + x] = Color.clear;
                }
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();

        _proceduralLaserSprite = Sprite.Create(
            tex,
            new Rect(0, 0, w, h),
            new Vector2(0.5f, 0.5f),
            16f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(2, 4, 2, 4));
        return _proceduralLaserSprite;
    }
}

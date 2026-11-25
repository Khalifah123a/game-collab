using UnityEngine;

/// <summary>
/// World 2 (LAN Party / Packet Congestion) environmental mechanic.
/// Simulates packet traffic gusts that periodically push the courier horizontally or vertically.
/// Challenges air control, jump distance, and dash timing across high-traffic server chasms.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class NetworkWindZone : MonoBehaviour
{
    [Header("Wind Direction & Force")]
    public Vector2 windForce = new Vector2(-16f, 0f);
    public float calmDuration = 2.0f;
    public float gustDuration = 2.8f;

    [Header("Visual Stream")]
    public Color streamColor = new Color(0.2f, 0.85f, 1f, 0.5f);

    public bool IsGustActive { get; private set; }

    private BoxCollider2D _collider;
    private SpriteRenderer _streamRenderer;
    private float _cycleTimer;
    private PlayerController _playerInside;

    private void Awake()
    {
        _collider = GetComponent<BoxCollider2D>();
        _collider.isTrigger = true;

        EnsureVisual();
    }

    private void EnsureVisual()
    {
        var child = transform.Find("WindVisual");
        if (child == null)
        {
            var go = new GameObject("WindVisual", typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = _collider.offset;
            _streamRenderer = go.GetComponent<SpriteRenderer>();
            _streamRenderer.sprite = Resources.Load<Sprite>("GeneratedArt/wind_zone");
            _streamRenderer.sortingOrder = 2;
            var size = _collider.size;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
        }
        else
        {
            _streamRenderer = child.GetComponent<SpriteRenderer>();
        }
    }

    private void Update()
    {
        _cycleTimer += Time.deltaTime;
        float totalCycle = calmDuration + gustDuration;
        float progress = Mathf.Repeat(_cycleTimer, totalCycle);

        IsGustActive = progress >= calmDuration;

        // Visual fade in/out during gust
        if (_streamRenderer != null)
        {
            float targetAlpha = IsGustActive ? 0.65f : 0.08f;
            Color c = streamColor;
            c.a = Mathf.Lerp(_streamRenderer.color.a, targetAlpha, Time.deltaTime * 6f);
            _streamRenderer.color = c;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var pc = other.GetComponentInParent<PlayerController>();
        if (pc != null) _playerInside = pc;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var pc = other.GetComponentInParent<PlayerController>();
        if (pc != null && pc == _playerInside) _playerInside = null;
    }

    private void FixedUpdate()
    {
        if (!IsGustActive || _playerInside == null) return;
        if (SafeZone.IsPlayerSafe) return;

        var rb = _playerInside.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Apply wind acceleration
            rb.AddForce(windForce, ForceMode2D.Force);
        }
    }
}

using UnityEngine;

/// <summary>
/// World 4 (Satellite Relay) environmental mechanic.
/// Vertical anti-gravity tractor beam that smoothly elevates the courier across
/// deep orbital chasms and restores air dashes while inside the stream.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class AntiGravityUplink : MonoBehaviour
{
    [Header("Uplink Config")]
    public float liftSpeed = 12f;
    public float upwardAcceleration = 38f;

    private BoxCollider2D _collider;
    private SpriteRenderer _renderer;
    private PlayerController _playerInside;
    private float _originalGravity;

    private void Awake()
    {
        _collider = GetComponent<BoxCollider2D>();
        _collider.isTrigger = true;

        EnsureVisual();
    }

    private void EnsureVisual()
    {
        var child = transform.Find("BeamVisual");
        if (child == null)
        {
            var go = new GameObject("BeamVisual", typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = _collider.offset;
            _renderer = go.GetComponent<SpriteRenderer>();
            _renderer.sprite = Resources.Load<Sprite>("GeneratedArt/gravity_beam");
            _renderer.sortingOrder = 2;
            var size = _collider.size;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
        }
        else
        {
            _renderer = child.GetComponent<SpriteRenderer>();
        }
    }

    private void Update()
    {
        if (_renderer != null)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 4f);
            Color c = new Color(0.2f, 0.9f, 1f, 0.45f + pulse * 0.25f);
            _renderer.color = c;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var pc = other.GetComponentInParent<PlayerController>();
        if (pc != null)
        {
            _playerInside = pc;
            _originalGravity = pc.gravity;
            pc.gravity = 0f;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var pc = other.GetComponentInParent<PlayerController>();
        if (pc != null && pc == _playerInside)
        {
            pc.gravity = _originalGravity;
            _playerInside = null;
        }
    }

    private void FixedUpdate()
    {
        if (_playerInside == null) return;

        var rb = _playerInside.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Lift smoothly upwards
            float newY = Mathf.MoveTowards(rb.linearVelocity.y, liftSpeed, upwardAcceleration * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, newY);

            // Refresh air traversal resources inside uplink beam
            _playerInside.airDashesLeft = 1;
        }
    }

    private void OnDisable()
    {
        if (_playerInside != null)
        {
            _playerInside.gravity = _originalGravity;
            _playerInside = null;
        }
    }
}

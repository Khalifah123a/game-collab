using UnityEngine;

/// <summary>
/// Silksong-style bio-cyber trampoline / pogo bouncer. Propels the player high into
/// the air, resets air dashes, and plays a squash-and-stretch energy burst.
/// Enables vertical traversal into high-level parkour paths and secret alcoves.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class CyberBouncer : MonoBehaviour
{
    [Header("Bouncer Config")]
    public float bounceForce = 26f;
    public float cooldown = 0.15f;

    private float _lastBounceTime;
    private SpriteRenderer _sr;
    private Vector3 _baseScale;
    private BoxCollider2D _col;

    private void Awake()
    {
        _col = GetComponent<BoxCollider2D>();
        _sr = GetComponent<SpriteRenderer>();
        _baseScale = transform.localScale;

        if (_sr != null && (_sr.sprite == null || _sr.sprite.name.Contains("placeholder")))
        {
            var s = Resources.Load<Sprite>("GeneratedArt/cyber_bouncer");
            if (s != null)
            {
                _sr.sprite = s;
                _sr.color = Color.white;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryBounce(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryBounce(other);
    }

    private void TryBounce(Collider2D other)
    {
        if (Time.time < _lastBounceTime + cooldown) return;
        var player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        _lastBounceTime = Time.time;

        // Propel player upwards and refresh movement resources
        Vector2 vel = rb.linearVelocity;
        vel.y = bounceForce;
        rb.linearVelocity = vel;

        // Reset air dash count
        player.airDashesLeft = 1;

        // Visual squash-and-stretch bounce feedback
        StartCoroutine(BounceSquashRoutine());
    }

    private System.Collections.IEnumerator BounceSquashRoutine()
    {
        float elapsed = 0f;
        const float duration = 0.35f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Squash wide and flat, then stretch tall, then settle
            float sy = 1f - 0.45f * Mathf.Sin(t * Mathf.PI);
            float sx = 1f + 0.35f * Mathf.Sin(t * Mathf.PI);
            transform.localScale = new Vector3(_baseScale.x * sx, _baseScale.y * sy, _baseScale.z);
            yield return null;
        }

        transform.localScale = _baseScale;
    }
}

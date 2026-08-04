using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Collectible_Snippet : MonoBehaviour
{
    [Header("Config")]
    public float ttlBonus = 15f;

    [Header("Visual")]
    public float pulseSpeed = 3f;
    public float pulseMin = 0.8f;
    public float pulseMax = 1.2f;
    public GameObject pickupEffect;

    private Collider2D _collider;
    private SpriteRenderer _sprite;
    private Vector3 _baseScale;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _sprite = GetComponent<SpriteRenderer>();
        _baseScale = transform.localScale;
    }

    private void Update()
    {
        float scale = Mathf.Lerp(pulseMin, pulseMax, (Mathf.Sin(Time.time * pulseSpeed) + 1) / 2);
        transform.localScale = _baseScale * scale;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        _collider.enabled = false;
        if (_sprite) _sprite.enabled = false;

        var health = FindObjectOfType<PlayerHealth>();
        if (health) health.AddPacketSnippet();

        if (pickupEffect)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);

        Destroy(gameObject, 0.1f);
    }
}

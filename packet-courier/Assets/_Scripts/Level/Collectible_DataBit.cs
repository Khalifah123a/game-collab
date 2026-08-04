using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Collectible_DataBit : MonoBehaviour
{
    [Header("Config")]
    public int amount = 10;
    public bool isSecret;

    [Header("Visual")]
    public float floatSpeed = 2f;
    public float floatHeight = 0.2f;
    public GameObject pickupEffect;

    private Vector2 _startPos;
    private Collider2D _collider;
    private SpriteRenderer _sprite;

    private void Awake()
    {
        _startPos = transform.position;
        _collider = GetComponent<Collider2D>();
        _sprite = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        Vector2 pos = _startPos;
        pos.y += Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = pos;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        _collider.enabled = false;
        if (_sprite) _sprite.enabled = false;

        GameManager.Instance?.AddDataBits(amount);

        if (pickupEffect)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);

        Destroy(gameObject, 0.1f);
    }
}

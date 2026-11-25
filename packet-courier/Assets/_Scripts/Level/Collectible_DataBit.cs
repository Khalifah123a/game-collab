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
    private Vector3 _startLocalPos;
    private bool _hasAnchor;
    private Collider2D _collider;
    private SpriteRenderer _sprite;
    private string Id => "bit:" + gameObject.name + ":" + _startPos.ToString("F3");
    private bool _collected;

    public void InitializePosition(Vector2 worldPos, Vector3 localPos)
    {
        _startPos = worldPos;
        _startLocalPos = localPos;
        _hasAnchor = true;
    }

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _sprite = GetComponent<SpriteRenderer>();
        if (_sprite != null)
        {
            var s = Resources.Load<Sprite>("GeneratedArt/databit");
            if (s != null) { _sprite.sprite = s; _sprite.color = Color.white; }
            if (_sprite.sortingOrder == 0) _sprite.sortingOrder = 10;
        }
    }

    private void Start()
    {
        if (!_hasAnchor)
        {
            _startPos = transform.position;
            _startLocalPos = transform.localPosition;
            _hasAnchor = true;
        }

        if (GameManager.Ensure().ledger.IsCollected(Id))
            gameObject.SetActive(false);
    }

    private void Update()
    {
        if (transform.parent != null && transform.parent.GetComponent<Platform_Moving>() != null)
        {
            Vector3 lpos = _startLocalPos;
            lpos.y += Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            transform.localPosition = lpos;
        }
        else
        {
            Vector2 pos = _startPos;
            pos.y += Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            transform.position = pos;
        }
        float pulse = 1f + 0.08f * Mathf.Sin(Time.time * 4.5f);
        transform.localScale = new Vector3(pulse, pulse, 1f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (_collected) return;
        _collected = true;

        _collider.enabled = false;
        if (_sprite) _sprite.enabled = false;

        var gm = GameManager.Ensure();
        gm.ledger.Collect(Id, amount);
        gm.dataBitsCollectedThisRun = gm.ledger.Bits;
        OptionalQuestService.ReportCollect(gm.currentWorldIndex, "bit:recycle", Id);
        AudioManager.PlayCollect();

        if (pickupEffect)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);

        Destroy(gameObject, 0.1f);
    }
}

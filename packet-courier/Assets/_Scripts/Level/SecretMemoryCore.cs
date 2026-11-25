using UnityEngine;

/// <summary>
/// Hidden golden encrypted memory core tucked away in high vertical parkour shafts.
/// Grants +50 TTL and +150 DataBits, reporting to the run ledger and optional quest tracker.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SecretMemoryCore : MonoBehaviour
{
    [Header("Reward")]
    public int dataBits = 150;
    public float ttlBonus = 50f;

    private Collider2D _col;
    private SpriteRenderer _sr;
    private bool _collected;
    private string Id => "secret_core:" + gameObject.name + ":" + transform.position.ToString("F2");

    private void Start()
    {
        if (GameManager.Ensure().ledger.IsCollected(Id))
            gameObject.SetActive(false);
    }

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;
        _sr = GetComponent<SpriteRenderer>();

        if (_sr != null && (_sr.sprite == null || _sr.sprite.name.Contains("placeholder")))
        {
            var s = Resources.Load<Sprite>("GeneratedArt/secret_memory_core");
            if (s != null)
            {
                _sr.sprite = s;
                _sr.color = Color.white;
            }
        }
    }

    private void Update()
    {
        // Floating ambient pulse
        float bob = Mathf.Sin(Time.time * 3f) * 0.15f;
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y + bob * Time.deltaTime, transform.localPosition.z);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (_collected) return;
        _collected = true;

        var gm = GameManager.Ensure();
        gm.ledger.Collect(Id, dataBits);
        gm.dataBitsCollectedThisRun = gm.ledger.Bits;

        var health = FindAnyObjectByType<PlayerHealth>();
        if (health != null) health.Heal(ttlBonus);

        OptionalQuestService.ReportCollect(gm.currentWorldIndex, "item:secret_core", Id);

        _col.enabled = false;
        if (_sr != null) _sr.enabled = false;

        Destroy(gameObject, 0.1f);
    }
}

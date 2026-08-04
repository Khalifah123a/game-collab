using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Gate_Firewall : MonoBehaviour
{
    [Header("Gate Config")]
    public int requiredPort = 80;
    public bool requireEncrypted;
    public bool isOpen;

    [Header("Visual")]
    public GameObject gateOpenVisual;
    public GameObject gateClosedVisual;

    [Header("Damage")]
    public float wrongPortPenalty = 15f;

    private Collider2D _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        UpdateGate();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        TryOpen();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || isOpen) return;
        TryOpen();
    }

    private void TryOpen()
    {
        if (isOpen) return;

        var mode = FindObjectOfType<PlayerMode>();
        if (mode == null) return;

        bool portMatch = mode.currentPort == requiredPort;
        bool modeMatch = !requireEncrypted || mode.isEncrypted;

        if (portMatch && modeMatch)
        {
            Open();
        }
        else if (portMatch && !modeMatch)
        {
            var health = FindObjectOfType<PlayerHealth>();
            if (health) health.TakeDamage(wrongPortPenalty, PlayerHealth.DamageType.TTLDamage);
        }
    }

    private void Open()
    {
        isOpen = true;
        _collider.enabled = false;
        UpdateGate();
    }

    private void UpdateGate()
    {
        if (gateOpenVisual) gateOpenVisual.SetActive(isOpen);
        if (gateClosedVisual) gateClosedVisual.SetActive(!isOpen);
    }

    public void ForceOpen()
    {
        Open();
    }
}

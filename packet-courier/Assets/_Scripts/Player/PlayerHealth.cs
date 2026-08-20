using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    [Header("TTL")]
    public float ttlMax = 100f;
    public float ttlCurrent;
    public float drainRate = 1f;
    public bool isDead;

    [Header("Upgrade Bonuses")]
    public int extraTTLPerLevel = 10;
    public float drainReductionPerLevel = 0.1f;

    [Header("Damage")]
    public float malwareDamage = 25f;
    public float pitDamage = 50f;
    public float wrongPortDamage = 15f;
    public float respawnTTLPercent = 0.5f;

    public delegate void TTLChanged(float current, float max);
    public event TTLChanged OnTTLChanged;
    public delegate void PlayerDied();
    public event PlayerDied OnPlayerDied;

    private PlayerMode _mode;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _mode = GetComponent<PlayerMode>();
        ttlMax = GetUpgradedTTLMax();
        ttlCurrent = ttlMax;
        drainRate = GetUpgradedDrainRate();
    }

    private void Update()
    {
        if (isDead) return;

        ttlCurrent -= drainRate * Time.deltaTime;
        OnTTLChanged?.Invoke(ttlCurrent, ttlMax);

        if (ttlCurrent <= 0)
            Die();
    }

    public void TakeDamage(float amount, DamageType type)
    {
        if (isDead) return;

        if (type == DamageType.InstaKill)
        {
            if (!_mode.isEncrypted)
            {
                Die();
                return;
            }
            float shieldReduc = GetUpgradeBonus(GameManager.UpgradeType.EncryptedShield) * 0.05f;
            amount = malwareDamage * (1f - shieldReduc);
        }

        ttlCurrent = Mathf.Max(0, ttlCurrent - amount);
        OnTTLChanged?.Invoke(ttlCurrent, ttlMax);

        if (ttlCurrent <= 0)
            Die();
    }

    public void Heal(float amount)
    {
        ttlCurrent = Mathf.Min(ttlMax, ttlCurrent + amount);
        OnTTLChanged?.Invoke(ttlCurrent, ttlMax);
    }

    public void AddPacketSnippet()
    {
        Heal(15f);
    }

    public void SetCheckpointBonus()
    {
        Heal(10f);
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        OnPlayerDied?.Invoke();
        GameManager.Ensure().OnPlayerDeath();
    }

    private float GetUpgradedTTLMax()
    {
        float baseTTL = ttlMax;
        int level = GameManager.Instance?.GetUpgradeLevel(GameManager.UpgradeType.TTLMax) ?? 0;
        return baseTTL + (level * extraTTLPerLevel);
    }

    private float GetUpgradedDrainRate()
    {
        float baseDrain = drainRate;
        int level = GameManager.Instance?.GetUpgradeLevel(GameManager.UpgradeType.TTLDrainRate) ?? 0;
        return Mathf.Max(0.3f, baseDrain - (level * drainReductionPerLevel));
    }

    private float GetUpgradeBonus(GameManager.UpgradeType type)
    {
        return GameManager.Instance?.GetUpgradeLevel(type) ?? 0;
    }

    public void RefreshStats()
    {
        ttlMax = GetUpgradedTTLMax();
        drainRate = GetUpgradedDrainRate();
    }

    public enum DamageType { InstaKill, TTLDamage }
}

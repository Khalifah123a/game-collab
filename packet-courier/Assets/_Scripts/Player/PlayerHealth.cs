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
    private float _invulnerableUntil;
    private float _baseTTL;
    private float _baseDrain;
    public bool IsInvulnerable => Time.time < _invulnerableUntil;

    private void Awake()
    {
        Instance = this;
        _mode = GetComponent<PlayerMode>();
    }

    private void Start()
    {
        var level = FindAnyObjectByType<LevelManager>();
        _baseTTL = level ? WorldRules.TTL(level.worldIndex) : ttlMax;
        _baseDrain = level ? level.levelDrainRate : drainRate;
        ttlMax = GetUpgradedTTLMax();
        ttlCurrent = ttlMax;
        drainRate = GetUpgradedDrainRate();
        var gm = GameManager.Instance;
        if (gm != null && gm.hasCheckpoint)
            ttlCurrent = Mathf.Clamp(Mathf.Max(gm.checkpointTTL, ttlMax * 0.7f), 0, ttlMax);
    }

    private void Update()
    {
        if (isDead || Time.timeScale == 0) return;

        // Safe Zone, Dialogue, or Quiz Modal holds TTL steady: no drain while reading or interacting.
        if (SafeZone.IsPlayerSafe || DialogUI.IsOpen || SoftwareQuizUI.IsQuizOpen)
        {
            OnTTLChanged?.Invoke(ttlCurrent, ttlMax);
            return;
        }

        ttlCurrent = Mathf.Max(0, ttlCurrent - drainRate * Time.deltaTime);
        OnTTLChanged?.Invoke(ttlCurrent, ttlMax);

        if (ttlCurrent <= 0)
            Die();
    }

    public void TakeDamage(float amount, DamageType type)
    {
        if (isDead || IsInvulnerable || amount <= 0 || DialogUI.IsOpen || SoftwareQuizUI.IsQuizOpen) return;

        // Overclock Phase Dash: complete immunity during dash
        var controller = GetComponent<PlayerController>();
        if (controller != null && controller.isDashing && OverclockManager.HasPhaseDash)
            return;

        // Overclock Nano Parry: negate damage and trigger counter-attack
        var combat = GetComponent<PlayerCombat>();
        if (combat != null && combat.IsParrying)
        {
            combat.OnParrySuccess(amount, type);
            return;
        }

        if (type == DamageType.InstaKill)
        {
            if (_mode == null || !_mode.isEncrypted)
            {
                Die();
                return;
            }
            float shieldReduc = GetUpgradeBonus(GameManager.UpgradeType.EncryptedShield) * 0.05f;
            amount = malwareDamage * (1f - shieldReduc);
        }

        if (type == DamageType.TTLDamage && _mode && _mode.isEncrypted)
            amount *= Mathf.Max(0.5f, 0.75f - GetUpgradeBonus(GameManager.UpgradeType.EncryptedShield) * 0.05f);
        ttlCurrent = Mathf.Max(0, ttlCurrent - amount);
        _invulnerableUntil = Time.time + 0.8f;
        GetComponent<PlayerAnimator>()?.NotifyHurt();
        AudioManager.PlayHurt();
        JuiceManager.Shake(0.38f);
        JuiceManager.Hitstop(0.055f);
        JuiceManager.TriggerDamageGlitch(0.14f);
        GamepadControllerSupport.TriggerRumble(0.18f);
        JuiceManager.SpawnImpactSparks(transform.position, new Color(1f, 0.25f, 0.35f, 1f));
        PacketIntegrityManager.Ensure()?.OnPlayerHit();
        OnTTLChanged?.Invoke(ttlCurrent, ttlMax);

        if (ttlCurrent <= 0)
            Die();
    }

    public void Heal(float amount)
    {
        if (isDead) return;
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

        // Overclock Emergency Buffer reviver
        if (OverclockManager.TryTriggerEmergencyBuffer())
        {
            ttlCurrent = 25f;
            _invulnerableUntil = Time.time + 1.8f;
            OnTTLChanged?.Invoke(ttlCurrent, ttlMax);
            return;
        }

        isDead = true;
        AudioManager.PlayDeath();
        OnPlayerDied?.Invoke();
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        GetComponent<PlayerAnimator>()?.NotifyDeath();
        Invoke(nameof(FinishDeath), 0.45f);
    }

    private void FinishDeath() { GameManager.Ensure().OnPlayerDeath(); }

    private float GetUpgradedTTLMax()
    {
        float baseTTL = _baseTTL;
        int level = GameManager.Instance?.GetUpgradeLevel(GameManager.UpgradeType.TTLMax) ?? 0;
        return baseTTL + (level * extraTTLPerLevel) + OverclockManager.ExtraTTL;
    }

    private float GetUpgradedDrainRate()
    {
        float baseDrain = _baseDrain;
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

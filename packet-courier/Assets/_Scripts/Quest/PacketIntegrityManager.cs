using System;
using UnityEngine;

/// <summary>
/// Runtime manager for packet integrity.
/// Tracks physical damage on the packet, emits change events, and coordinates audio.
/// Pure calculation rules are delegated to <see cref="PacketIntegrityRules"/>.
/// </summary>
public class PacketIntegrityManager : MonoBehaviour
{
    public static PacketIntegrityManager Instance { get; private set; }

    [Header("Integrity State")]
    public float currentIntegrity = 100f;

    public const float MinIntegrity = PacketIntegrityRules.MinIntegrity;
    public const float PenaltyPerHit = PacketIntegrityRules.PenaltyPerHit;

    public static event Action<float, DeliveryGrade> OnIntegrityChanged;

    public static PacketIntegrityManager Ensure()
    {
        if (Instance != null) return Instance;
        var gm = GameManager.Ensure();
        var pim = gm.GetComponent<PacketIntegrityManager>();
        if (pim == null) pim = gm.gameObject.AddComponent<PacketIntegrityManager>();
        Instance = pim;
        return pim;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    public void ResetIntegrity()
    {
        currentIntegrity = 100f;
        NotifyChange();
    }

    public void Repair(float amount)
    {
        currentIntegrity = Mathf.Min(100f, currentIntegrity + amount);
        NotifyChange();
    }

    public void RepairDamage(float amount) => Repair(amount);

    [Header("Active Modifier")]
    public PacketModifier ActiveModifier { get; private set; } = PacketModifier.Create(PacketModifierType.StandardIPv4);

    public void SetModifier(PacketModifierType type)
    {
        ActiveModifier = PacketModifier.Create(type);
        NotifyChange();
    }

    private void Update()
    {
        if (ActiveModifier == null || ActiveModifier.integrityDrainPerSecond <= 0f) return;
        if (SafeZone.IsPlayerSafe || Time.timeScale == 0f) return;

        var gm = GameManager.Instance;
        if (gm == null || !gm.isInRun) return;

        var quests = QuestService.Instance;
        if (quests == null || quests.GetPackageState(gm.currentWorldIndex) != PackageState.Active)
            return;

        currentIntegrity = Mathf.Max(MinIntegrity, currentIntegrity - ActiveModifier.integrityDrainPerSecond * Time.deltaTime);
        NotifyChange();
    }

    public void OnPlayerHit()
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.isInRun) return;

        var quests = QuestService.Instance;
        if (quests == null || quests.GetPackageState(gm.currentWorldIndex) != PackageState.Active)
            return;

        float penalty = PenaltyPerHit * (ActiveModifier != null ? ActiveModifier.damagePenaltyMultiplier : 1.0f);
        currentIntegrity = Mathf.Max(MinIntegrity, currentIntegrity - penalty);
        NotifyChange();

        // Audio glitch feedback
        AudioManager.Play("sfx_hurt", 0.6f, 1.4f);
    }

    private void NotifyChange()
    {
        OnIntegrityChanged?.Invoke(currentIntegrity, CalculateGrade(currentIntegrity));
    }

    public static DeliveryGrade CalculateGrade(float integrity)
    {
        return PacketIntegrityRules.CalculateGrade(integrity);
    }

    public static int GetBonusBits(DeliveryGrade grade)
    {
        return PacketIntegrityRules.GetBonusBits(grade);
    }

    public static string GetGradeString(DeliveryGrade grade)
    {
        return PacketIntegrityRules.GetGradeString(grade);
    }

    public static Color GetGradeColor(DeliveryGrade grade)
    {
        switch (grade)
        {
            case DeliveryGrade.S: return new Color(0.2f, 1f, 0.85f, 1f);
            case DeliveryGrade.A: return new Color(0.3f, 0.9f, 1f, 1f);
            case DeliveryGrade.B: return new Color(1f, 0.85f, 0.2f, 1f);
            default: return new Color(1f, 0.35f, 0.35f, 1f);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime manager for courier Overclock hardware upgrades.
/// Persists via GameManager across scenes and retries.
/// </summary>
public class OverclockManager : MonoBehaviour
{
    public static OverclockManager Instance { get; private set; }

    private static readonly int[] FallbackLevels = new int[8];
    private static bool _reviverUsedThisSegment;

    private PlayerController _player;

    public static int GetLevel(OverclockId id)
    {
        int idx = (int)id;
        var gm = GameManager.Instance;
        if (gm != null && gm.overclockLevels != null && idx >= 0 && idx < gm.overclockLevels.Length)
            return gm.overclockLevels[idx];
        if (idx >= 0 && idx < FallbackLevels.Length)
            return FallbackLevels[idx];
        return 0;
    }

    public static void SetLevel(OverclockId id, int level)
    {
        int idx = (int)id;
        int prevLevel = GetLevel(id);
        var gm = GameManager.Instance;
        if (gm != null && gm.overclockLevels != null && idx >= 0 && idx < gm.overclockLevels.Length)
            gm.overclockLevels[idx] = level;
        if (idx >= 0 && idx < FallbackLevels.Length)
            FallbackLevels[idx] = level;

        // Immediately sync upgraded stats onto live PlayerHealth and PlayerController
        var ph = PlayerHealth.Instance;
        if (ph != null)
        {
            float oldMax = ph.ttlMax;
            ph.RefreshStats();
            if (id == OverclockId.EmergencyBuffer && level > prevLevel)
            {
                float gained = Mathf.Max(20f * (level - prevLevel), ph.ttlMax - oldMax);
                ph.ttlCurrent = Mathf.Min(ph.ttlMax, ph.ttlCurrent + gained);
            }
        }
        var pc = FindAnyObjectByType<PlayerController>();
        if (pc != null)
        {
            pc.RefreshUpgradedStats();
        }

        int activeCount = 0;
        for (int i = 0; i < 8; i++) if (GetLevel((OverclockId)i) > 0) activeCount++;
        if (activeCount >= 4) AchievementManager.Unlock("ACH_KERNEL_OVERCLOCK");
    }

    public static bool HasTripleJump => GetLevel(OverclockId.TripleJump) >= 1;
    public static bool HasBlaster => GetLevel(OverclockId.CyberBlaster) >= 1;
    public static int BlasterLevel => GetLevel(OverclockId.CyberBlaster);
    public static bool HasParry => GetLevel(OverclockId.NanoParry) >= 1;
    public static int ParryLevel => GetLevel(OverclockId.NanoParry);
    public static int BonusDamage => GetLevel(OverclockId.CoreDamage);
    public static bool HasPhaseDash => GetLevel(OverclockId.PhaseDash) >= 1;
    public static bool HasBuffActive
    {
        get
        {
            for (int i = 0; i < 8; i++)
            {
                if (GetLevel((OverclockId)i) > 0) return true;
            }
            return false;
        }
    }
    public static int MagnetLevel => GetLevel(OverclockId.MagneticPull);
    public static int BatteryLevel => GetLevel(OverclockId.TurboBattery);
    public static bool HasBufferRevive => GetLevel(OverclockId.EmergencyBuffer) >= 1;
    public static bool IsBufferAvailableThisSegment => HasBufferRevive && !_reviverUsedThisSegment;
    public static float MagnetRadius
    {
        get
        {
            int lv = GetLevel(OverclockId.MagneticPull);
            if (lv == 1) return 6f;
            if (lv >= 2) return 10f;
            return 0f;
        }
    }
    public static float StaminaCostMult => GetLevel(OverclockId.TurboBattery) >= 1 ? 0.7f : 1f;
    public static float StaminaRegenMult => GetLevel(OverclockId.TurboBattery) >= 1 ? 1.5f : 1f;
    public static float ExtraTTL => GetLevel(OverclockId.EmergencyBuffer) * 20f;

    public static bool TryTriggerEmergencyBuffer()
    {
        if (_reviverUsedThisSegment) return false;
        if (GetLevel(OverclockId.EmergencyBuffer) < 1) return false;
        _reviverUsedThisSegment = true;
        return true;
    }

    public static void ResetSegmentState()
    {
        _reviverUsedThisSegment = false;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _player = GetComponent<PlayerController>();
        ResetSegmentState();
    }

    private void Update()
    {
        if (_player == null) _player = FindAnyObjectByType<PlayerController>();
        if (_player == null || _player.isFrozen) return;

        UpdateMagneticVacuum();
    }

    private void UpdateMagneticVacuum()
    {
        float radius = MagnetRadius;
        if (radius <= 0f) return;

        Vector2 playerPos = _player.transform.position;
        float pullSpeed = 12f;

        // Pull nearby DataBits
        var bits = FindObjectsByType<Collectible_DataBit>();
        for (int i = 0; i < bits.Length; i++)
        {
            if (bits[i] == null) continue;
            float dist = Vector2.Distance(playerPos, bits[i].transform.position);
            if (dist <= radius && dist > 0.1f)
            {
                bits[i].transform.position = Vector2.MoveTowards(
                    bits[i].transform.position, playerPos, pullSpeed * Time.deltaTime);
            }
        }

        // Pull nearby Snippets
        var snippets = FindObjectsByType<Collectible_Snippet>();
        for (int i = 0; i < snippets.Length; i++)
        {
            if (snippets[i] == null) continue;
            float dist = Vector2.Distance(playerPos, snippets[i].transform.position);
            if (dist <= radius && dist > 0.1f)
            {
                snippets[i].transform.position = Vector2.MoveTowards(
                    snippets[i].transform.position, playerPos, pullSpeed * Time.deltaTime);
            }
        }
    }
}

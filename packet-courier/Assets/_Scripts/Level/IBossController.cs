using UnityEngine;

/// <summary>
/// Common contract for all bosses and mini-bosses in Packet Courier.
/// Integrates with GameplayHUD and LevelManager for health bars, telegraphs, and goal locking.
/// </summary>
public interface IBossController : IEnemyDamageable
{
    string BossName { get; }
    int HitPoints { get; }
    int MaxHitPoints { get; }
    float HealthPercent { get; }
}

/// <summary>
/// Global registry tracking currently engaged boss/mini-boss in the active segment.
/// </summary>
public static class BossTracker
{
    public static IBossController ActiveBoss { get; private set; }

    public static void Register(IBossController boss)
    {
        ActiveBoss = boss;
    }

    public static void Unregister(IBossController boss)
    {
        if (ActiveBoss == boss)
        {
            ActiveBoss = null;
        }
        BossRushManager.Instance?.NotifyBossDefeated(boss);
    }

    public static void ClearIfSame(IBossController boss)
    {
        if (ActiveBoss == boss)
        {
            ActiveBoss = null;
        }
    }
}

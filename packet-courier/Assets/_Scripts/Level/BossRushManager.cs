using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boss Rush Mode manager.
/// Allows players to fight all 3 bosses back-to-back:
/// 1. The Censor (World 2)
/// 2. Solaris (World 4)
/// 3. Rootkit Core (World 5 Boss Akhir)
/// Tracks continuous speedrun time, health retention, and records personal bests.
/// </summary>
public class BossRushManager : MonoBehaviour
{
    public static BossRushManager Instance { get; private set; }

    public enum BossRushPhase
    {
        Inactive,
        TheCensor,
        Solaris,
        RootkitCore,
        Victory
    }

    public static bool IsBossRushActive { get; private set; }
    public BossRushPhase CurrentPhase { get; private set; } = BossRushPhase.Inactive;
    public float ElapsedTimer { get; private set; }
    public int BossesDefeated { get; private set; }

    public const string PrefsBestTimeKey = "PacketCourier_BossRushBestTime";
    public const string PrefsCompletedKey = "PacketCourier_BossRushCompleted";

    public static event Action<BossRushPhase, float> OnBossRushPhaseChanged;
    public static event Action<float> OnBossRushCompleted;

    public static BossRushManager Ensure()
    {
        if (Instance != null) return Instance;
        var gm = GameManager.Ensure();
        var brm = gm.GetComponent<BossRushManager>();
        if (brm == null) brm = gm.gameObject.AddComponent<BossRushManager>();
        Instance = brm;
        return brm;
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

    public static void StartBossRush()
    {
        var mgr = Ensure();
        mgr.BeginRushInternal();
    }

    private void BeginRushInternal()
    {
        IsBossRushActive = true;
        CurrentPhase = BossRushPhase.TheCensor;
        ElapsedTimer = 0f;
        BossesDefeated = 0;

        OnBossRushPhaseChanged?.Invoke(CurrentPhase, ElapsedTimer);

        // Load World 2 Segment 4 (The Censor arena)
        UnityEngine.SceneManagement.SceneManager.LoadScene("World2_Segment4");
    }

    private void Update()
    {
        if (!IsBossRushActive || CurrentPhase == BossRushPhase.Victory || Time.timeScale == 0f) return;
        ElapsedTimer += Time.deltaTime;
    }

    /// <summary>
    /// Invoked whenever any boss controller dies in the scene.
    /// Progresses the Boss Rush run to the next boss arena.
    /// </summary>
    public void NotifyBossDefeated(IBossController boss)
    {
        if (!IsBossRushActive) return;

        BossesDefeated++;

        if (CurrentPhase == BossRushPhase.TheCensor)
        {
            CurrentPhase = BossRushPhase.Solaris;
            OnBossRushPhaseChanged?.Invoke(CurrentPhase, ElapsedTimer);
            StartCoroutine(TransitionToNextBoss("World4_Segment5", 1.8f));
        }
        else if (CurrentPhase == BossRushPhase.Solaris)
        {
            CurrentPhase = BossRushPhase.RootkitCore;
            OnBossRushPhaseChanged?.Invoke(CurrentPhase, ElapsedTimer);
            StartCoroutine(TransitionToNextBoss("World5_Segment1", 1.8f));
        }
        else if (CurrentPhase == BossRushPhase.RootkitCore)
        {
            CurrentPhase = BossRushPhase.Victory;
            IsBossRushActive = false;

            float prevBest = PlayerPrefs.GetFloat(PrefsBestTimeKey, float.MaxValue);
            if (ElapsedTimer < prevBest)
            {
                PlayerPrefs.SetFloat(PrefsBestTimeKey, ElapsedTimer);
            }
            PlayerPrefs.SetInt(PrefsCompletedKey, 1);
            PlayerPrefs.Save();

            // Reward player with 1000 Data Bits
            GameManager.Instance?.AddDataBits(1000);
            AchievementManager.Unlock("ACH_BOSS_RUSH");

            OnBossRushCompleted?.Invoke(ElapsedTimer);
            AudioManager.Play("sfx_collect", 1f, 1f);
        }
    }

    private IEnumerator TransitionToNextBoss(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    public static float GetBestTime()
    {
        return PlayerPrefs.GetFloat(PrefsBestTimeKey, 0f);
    }

    public static bool HasCompletedBossRush()
    {
        return PlayerPrefs.GetInt(PrefsCompletedKey, 0) == 1;
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public GameState state = GameState.Menu;
    public int currentWorldIndex;
    public int currentSegmentIndex;
    public int totalDataBits;
    public int dataBitsCollectedThisRun;
    public readonly RunLedger ledger = new RunLedger();
    public bool hasCheckpoint;
    public Vector3 checkpointPosition;
    public float checkpointTTL;
    public string checkpointId;

    [Header("Run State")]
    public bool isInRun;
    public float runTime;
    /// <summary>Seeded once per run; retries and segment changes must never re-roll it.</summary>
    public int runSeed;

    [Header("Unlocks (persistent)")]
    public bool[] worldsUnlocked = new bool[7];
    public int[] upgradeLevels = new int[System.Enum.GetValues(typeof(UpgradeType)).Length];

    [Header("Shop Levels (persistent, ShopCatalog 0..5)")]
    [Tooltip("Generic shop levels for weapon/run stats. Index matches ShopCatalog UpgradeIndex. 0=WallCling unused (uses UpgradeType), 1=Damage, 2=Level, 3=Range, 4=AttackSpeed, 5=RunSpeed.")]
    public int[] shopLevels = new int[6];

    [Header("Overclock Levels (persistent, OverclockCatalog 0..7)")]
    [Tooltip("Overclock hardware abilities. 0=TripleJump, 1=Blaster, 2=Parry, 3=CoreDamage, 4=PhaseDash, 5=Magnet, 6=Battery, 7=Buffer.")]
    public int[] overclockLevels = new int[8];

    [Header("Quest State (persistent)")]
    /// <summary>Mandatory package quest, one entry per world. Survives scene loads and retries.</summary>
    public PackageState[] packageStates = new PackageState[WorldRules.Count];
    public bool[] packageRewardsPaid = new bool[WorldRules.Count];
    public QuestService quests;

    private int _runSequence;

    public enum GameState { Menu, Run, Paused, Hub, GameOver }
    public enum UpgradeType
    {
        DashDistance, BaseSpeed, AirControl, DashCooldown,
        TTLMax, TTLDrainRate, EncryptedShield, RespawnTTL,
        SwitchCooldown, WalljumpHeight, WallClingTime, PowerUpDuration,
        VisionRadius, PacketRadar, DangerOutline, SecretIndicator, AirJump
    }

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        quests = QuestService.Attach(this);
        EnsureQuestArrays();
        EnsureShopLevels();
        EnsureOverclockLevels();

        worldsUnlocked[0] = true;
        for (int i = 0; i < upgradeLevels.Length; i++)
            upgradeLevels[i] = 0;

        if (totalDataBits == 0) totalDataBits = 500;
    }

    /// <summary>Quest service on this persistent object, attached on first use if needed.</summary>
    public QuestService Quests
    {
        get { return quests != null ? quests : (quests = QuestService.Attach(this)); }
    }

    private void EnsureQuestArrays()
    {
        if (packageStates == null || packageStates.Length != WorldRules.Count)
            packageStates = new PackageState[WorldRules.Count];
        if (packageRewardsPaid == null || packageRewardsPaid.Length != WorldRules.Count)
            packageRewardsPaid = new bool[WorldRules.Count];
    }

    private void EnsureShopLevels()
    {
        if (shopLevels == null || shopLevels.Length != 6)
        {
            var next = new int[6];
            if (shopLevels != null)
                for (int i = 0; i < next.Length && i < shopLevels.Length; i++) next[i] = shopLevels[i];
            shopLevels = next;
        }
    }

    public void EnsureOverclockLevels()
    {
        if (overclockLevels == null || overclockLevels.Length != 8)
        {
            var next = new int[8];
            if (overclockLevels != null)
                for (int i = 0; i < next.Length && i < overclockLevels.Length; i++) next[i] = overclockLevels[i];
            overclockLevels = next;
        }
    }

    public void StartRun(int worldIndex)
    {
        if (worldIndex < 0 || worldIndex >= WorldRules.Count || !worldsUnlocked[worldIndex] || !WorldAvailable(worldIndex)) return;

        currentWorldIndex = worldIndex;
        currentSegmentIndex = 0;
        dataBitsCollectedThisRun = 0;
        ledger.Clear();
        hasCheckpoint = false;
        ledger.BeginSegment();
        Time.timeScale = 1;
        runTime = 0;
        isInRun = true;
        state = GameState.Run;
        if (runSeed <= 0) runSeed = PackageQuestRules.DefaultRunSeed(worldIndex, ++_runSequence);
        OptionalQuestService.PayoutSink = AddDataBits;
        OptionalQuestService.BeginRun(runSeed);

        LoadSegment(currentSegmentIndex);
    }

    /// <summary>Overrides the derived seed for reproducible runs. Non-positive seeds are ignored.</summary>
    public void SetRunSeed(int seed)
    {
        if (seed > 0) runSeed = seed;
    }

    public static GameManager Ensure()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("GameManager");
        return go.AddComponent<GameManager>();
    }

    public void RetrySegment()
    {
        ledger.Retry();
        dataBitsCollectedThisRun = ledger.Bits;
        Time.timeScale = 1;
        isInRun = true;
        state = GameState.Run;
        LoadSegment(currentSegmentIndex);
    }

    public void ReturnToMenu()
    {
        TutorialWorldBuilder.IsTutorialWorldActive = false;
        isInRun = false;
        ledger.Clear();
        hasCheckpoint = false;
        Time.timeScale = 1;
        state = GameState.Menu;
        LoadOrReload("Menu");
    }

    public void LoadSegment(int segmentIndex)
    {
        string sceneName = $"World{currentWorldIndex + 1}_Segment{segmentIndex + 1}";
        LoadOrReload(sceneName);
    }

    public void OnSegmentComplete()
    {
        if (!isInRun) return;
        hasCheckpoint = false;

        // If completing the dedicated initial Tutorial World (World 00), bank tutorial DataBits and graduate directly to World 1 Stage 1!
        if (TutorialWorldBuilder.IsTutorialWorldActive)
        {
            TutorialWorldBuilder.IsTutorialWorldActive = false;
            TutorialWorldBuilder.HasGraduatedTutorialWorld = true;
            CyberOpeningAndTutorialUI.HasCompletedOnboardingThisSession = true;
            int earnedBits = ledger.Bank();
            dataBitsCollectedThisRun = 0;
            totalDataBits += earnedBits;
            StartRun(0);
            return;
        }

        ledger.BeginSegment();
        currentSegmentIndex++;
        int maxSegments = GetSegmentCount(currentWorldIndex);

        if (currentSegmentIndex >= maxSegments)
            OnRunComplete();
        else
            LoadSegment(currentSegmentIndex);
    }

    private int GetSegmentCount(int worldIndex)
    {
        return WorldRules.Segments(worldIndex);
    }

    private void OnRunComplete()
    {
        isInRun = false;
        int bitsToAdd = ledger.Bank();
        dataBitsCollectedThisRun = 0;
        totalDataBits += bitsToAdd;

        if (currentWorldIndex + 1 < WorldRules.Count && WorldAvailable(currentWorldIndex + 1))
            worldsUnlocked[currentWorldIndex + 1] = true;

        state = GameState.Hub;
        LoadOrReload("Hub");
    }

    public void OnPlayerDeath()
    {
        if (state == GameState.GameOver) return;

        isInRun = false;
        state = GameState.GameOver;
        Time.timeScale = 0;
        GameOverUI.Show();
    }

    private void LoadOrReload(string sceneName)
    {
        if (SceneExists(sceneName))
            PacketCourierLoadingScreen.LoadScene(sceneName);
        else
            Debug.LogError("Scene is not available in Build Settings: " + sceneName);
    }

    private static bool SceneExists(string name)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (path.EndsWith("/" + name + ".unity", System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public void AddDataBits(int amount)
    {
        if (isInRun)
            dataBitsCollectedThisRun += amount;
    }

    public static bool WorldAvailable(int world)
    {
        for (int segment = 1; segment <= WorldRules.Segments(world); segment++)
            if (!SceneExists($"World{world + 1}_Segment{segment}")) return false;
        return true;
    }

    public void SaveCheckpoint(string id, Vector3 position, float ttl)
    {
        checkpointId = id; checkpointPosition = position; checkpointTTL = ttl;
        hasCheckpoint = true;
        ledger.SaveCheckpoint();
    }

    public int AvailableDataBits => totalDataBits + (isInRun ? dataBitsCollectedThisRun : 0);

    public bool SpendDataBits(int amount)
    {
        if (amount <= 0) return true;
        if (totalDataBits >= amount)
        {
            totalDataBits -= amount;
            return true;
        }
        if (isInRun && (totalDataBits + dataBitsCollectedThisRun >= amount))
        {
            int remainder = amount - totalDataBits;
            totalDataBits = 0;
            dataBitsCollectedThisRun -= remainder;
            ledger.Spend(remainder);
            return true;
        }
        return false;
    }

    public int GetUpgradeLevel(UpgradeType type)
    {
        return upgradeLevels[(int)type];
    }

    public int GetUpgradeMaxLevel(UpgradeType type)
    {
        int[] maxLevels = { 3, 3, 2, 2, 5, 3, 2, 2, 3, 2, 2, 2, 2, 1, 1, 1, 2 };
        return maxLevels[(int)type];
    }

    public bool Upgrade(UpgradeType type)
    {
        int idx = (int)type;
        if (upgradeLevels[idx] >= GetUpgradeMaxLevel(type))
            return false;

        int cost = GetUpgradeCost(type, upgradeLevels[idx] + 1);
        if (SpendDataBits(cost))
        {
            upgradeLevels[idx]++;
            return true;
        }
        return false;
    }

    public int GetUpgradeCost(UpgradeType type, int level)
    {
        int[][] costs = {
            new[]{100, 250, 500},           // DashDistance
            new[]{150, 300, 600},           // BaseSpeed
            new[]{200, 400},                // AirControl
            new[]{150, 350},                // DashCooldown
            new[]{100, 200, 350, 500, 700}, // TTLMax
            new[]{200, 400, 650},           // TTLDrainRate
            new[]{250, 500},                // EncryptedShield
            new[]{150, 300},                // RespawnTTL
            new[]{100, 200, 400},           // SwitchCooldown
            new[]{150, 350},                // WalljumpHeight
            new[]{100, 250},                // WallClingTime
            new[]{200, 450},                // PowerUpDuration
            new[]{150, 350},                // VisionRadius
            new[]{500},                      // PacketRadar
            new[]{300},                      // DangerOutline
            new[]{400},                      // SecretIndicator
            new[]{300, 500}                  // AirJump
        };
        int upgradeIdx = (int)type;
        var list = costs[upgradeIdx];
        if (level - 1 < list.Length) return list[level - 1];
        return 9999;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && (state == GameState.Run || state == GameState.Paused))
        {
            if (state == GameState.GameOver) return;
            if (DialogUI.IsOpen) return;
            var overclock = OverclockUI.Instance;
            if (overclock != null && overclock.IsOpen) return;
            var shop = ShopUI.Instance;
            if (shop != null && shop.IsOpen) return;

            PauseMenuUI.TogglePause();
        }
        if (isInRun && state == GameState.Run)
            runTime += Time.deltaTime;
    }
}

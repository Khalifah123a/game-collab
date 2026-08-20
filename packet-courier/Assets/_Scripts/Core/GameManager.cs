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

    [Header("Run State")]
    public bool isInRun;
    public float runTime;

    [Header("Unlocks (persistent)")]
    public bool[] worldsUnlocked = new bool[7];
    public int[] upgradeLevels = new int[System.Enum.GetValues(typeof(UpgradeType)).Length];

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

        worldsUnlocked[0] = true;
        for (int i = 0; i < upgradeLevels.Length; i++)
            upgradeLevels[i] = 0;
    }

    public void StartRun(int worldIndex)
    {
        if (!worldsUnlocked[worldIndex]) return;

        currentWorldIndex = worldIndex;
        currentSegmentIndex = 0;
        dataBitsCollectedThisRun = 0;
        runTime = 0;
        isInRun = true;
        state = GameState.Run;

        LoadSegment(currentSegmentIndex);
    }

    public static GameManager Ensure()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("GameManager");
        return go.AddComponent<GameManager>();
    }

    public void RetrySegment()
    {
        dataBitsCollectedThisRun = 0;
        isInRun = true;
        state = GameState.Run;
        LoadSegment(currentSegmentIndex);
    }

    public void ReturnToMenu()
    {
        isInRun = false;
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
        currentSegmentIndex++;
        int maxSegments = GetSegmentCount(currentWorldIndex);

        if (currentSegmentIndex >= maxSegments)
            OnRunComplete();
        else
            LoadSegment(currentSegmentIndex);
    }

    private int GetSegmentCount(int worldIndex)
    {
        int[] counts = { 3, 4, 5, 5, 6, 6, 3 };
        return counts[Mathf.Clamp(worldIndex, 0, counts.Length - 1)];
    }

    private void OnRunComplete()
    {
        isInRun = false;
        int bitsToAdd = dataBitsCollectedThisRun;
        totalDataBits += bitsToAdd;

        if (currentWorldIndex + 1 < worldsUnlocked.Length)
            worldsUnlocked[currentWorldIndex + 1] = true;

        state = GameState.Hub;
        LoadOrReload("Hub");
    }

    public void OnPlayerDeath()
    {
        int loss = Mathf.FloorToInt(dataBitsCollectedThisRun * Random.Range(0.25f, 0.5f));
        dataBitsCollectedThisRun -= loss;
        totalDataBits += Mathf.Max(0, dataBitsCollectedThisRun);
        dataBitsCollectedThisRun = 0;

        isInRun = false;
        state = GameState.GameOver;
        LoadOrReload("GameOver");
    }

    private void LoadOrReload(string sceneName)
    {
        if (SceneExists(sceneName))
            SceneManager.LoadScene(sceneName);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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

    public bool SpendDataBits(int amount)
    {
        if (totalDataBits >= amount)
        {
            totalDataBits -= amount;
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
        if (isInRun)
            runTime += Time.deltaTime;
    }
}

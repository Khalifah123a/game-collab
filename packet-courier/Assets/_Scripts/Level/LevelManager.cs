using UnityEngine;

public class LevelManager : MonoBehaviour

{

    [Header("Level Config")]

    public int worldIndex;

    public int segmentIndex;

    public Transform playerSpawn;

    public Transform serverBPoint;

    [Header("TTL Config")]

    public float levelDrainRate = 1f;

    [Header("Quest Gate")]

    [Tooltip("World-space quest label shown while the mandatory package quest blocks the goal.")]

    public QuestGateHint questGateHint;

    [Header("State")]

    public bool isComplete;

    /// <summary>Quest hint currently published by the gate, for HUD and dialog layers.</summary>

    public string LastGateHint { get; private set; } = "";

    private PlayerHealth _health;

    private QuestService _quests;

    /// <summary>True on the last segment of the world, the only place the gate applies.</summary>

    public bool IsFinalSegment

    {

        get { return segmentIndex >= WorldRules.Segments(worldIndex) - 1; }

    }

    /// <summary>True when the mandatory package quest or active boss still blocks world progress.</summary>

    public bool IsGoalLocked

    {

        get

        {

            if (BossTracker.ActiveBoss != null && !BossTracker.ActiveBoss.IsDead) return true;

            if (Boss_Rootkit.Instance != null && !Boss_Rootkit.Instance.IsDead) return true;

            if (BossArenaController.HasUnfinishedArena) return true;

            return !PackageDelivered();

        }

    }

    private void Awake()

    {

        var gm = GameManager.Ensure();

        if (gm.currentWorldIndex != worldIndex || gm.currentSegmentIndex != segmentIndex)

        { gm.hasCheckpoint = false; gm.ledger.BeginSegment(); }

        gm.currentWorldIndex = worldIndex;

        gm.currentSegmentIndex = segmentIndex;

        gm.isInRun = true;

        gm.state = GameManager.GameState.Run;

        _quests = QuestService.Ensure();

    }

    private void Start()

    {

        EnsureSafeZones();

        ExtendAllGroundsToBedrock();

        WorldThemeApplier.ApplyTheme(worldIndex);

        SnapAllEnemiesToGround();

        var gm = GameManager.Ensure();

        _health = FindAnyObjectByType<PlayerHealth>();

        if (playerSpawn)

        {

            var player = FindAnyObjectByType<PlayerController>();

            if (player)

            {

                player.transform.position = gm.hasCheckpoint ? gm.checkpointPosition : playerSpawn.position;

                SnapPlayerToGround(player);

            }

        }

        _quests = QuestService.Ensure();

        Vector2 startAnchor = playerSpawn != null ? (Vector2)playerSpawn.position : (Vector2)transform.position;
        var startChamber = GameObject.Find("SafeChamber_Start");
        if (startChamber != null)
        {
            // Chamber floor slab center is at floorCenterY; top walkable surface is floorCenterY + 0.75f
            startAnchor = new Vector2(startChamber.transform.position.x, startChamber.transform.position.y + 0.75f);
        }
        if (worldIndex == 0)
        {
            startAnchor = new Vector2(startChamber != null ? startChamber.transform.position.x : -8.0f, -17.0f);
        }

        Vector2 endAnchor = EndSafeZone();
        var endChamber = GameObject.Find("SafeChamber_End");
        if (endChamber != null)
        {
            endAnchor = new Vector2(endChamber.transform.position.x, endChamber.transform.position.y + 0.75f);
        }
        if (worldIndex == 0)
        {
            endAnchor = new Vector2(endAnchor.x, -17.0f);
        }

        _quests.BeginWorldSegment(

            worldIndex,

            segmentIndex,

            WorldRules.Segments(worldIndex),

            startAnchor,

            endAnchor);

        if (questGateHint == null) questGateHint = QuestGateHint.Create(EndSafeZone(), LastGateHint);

        // Remove any obsolete CyberBouncers from scene

        foreach (var bouncer in Object.FindObjectsByType<CyberBouncer>())

        {

            if (bouncer != null) Destroy(bouncer.gameObject);

        }

        if (TutorialWorldBuilder.IsTutorialWorldActive)
        {
            // Dedicated World 00 Tutorial: skip World 1 bosses, quiz boards, secret cores & parkour hazards
            TutorialWorldBuilder.BuildCleanTutorialCourse(this);
        }
        else
        {
            EnsureSegmentBoss();

            EnsureSilksongFeatures();

            PopulateStageHazardsAndPlatforms();
        }

        bool inSafeZone = SafeZone.IsPlayerSafe;

        bool bossActive = (BossTracker.ActiveBoss != null && !BossTracker.ActiveBoss.IsDead) ||

                          (Boss_Rootkit.Instance != null && !Boss_Rootkit.Instance.IsDead);

        AudioManager.PlayWorldBGM(worldIndex, inSafeZone, bossActive, 2.5f);

        // Spawn directional arrow guide: Stage start -> PackageGiverNPC ("WAJIB: AMBIL PAKET"), Post-pickup -> PackageReceiverNPC / ServerB
        PackageQuestArrowGuide.EnsureForStage(this);

        // Launch Opening Prologue + Interactive Field Tutorial in Tutorial World (or if explicitly armed)
        if (worldIndex == 0 && segmentIndex == 0 && !gm.hasCheckpoint)
        {
            if (TutorialWorldBuilder.IsTutorialWorldActive || CyberOpeningAndTutorialUI.PendingOnboarding)
            {
                CyberOpeningAndTutorialUI.StartSequence(includeOpeningPrologue: true);
            }
            else if (!TutorialWorldBuilder.HasGraduatedTutorialWorld && !CyberOpeningAndTutorialUI.HasCompletedOnboardingThisSession)
            {
                StartCoroutine(AutoOnboardingCheckRoutine());
            }
        }
    }

    private System.Collections.IEnumerator AutoOnboardingCheckRoutine()
    {
        yield return new WaitForSeconds(0.45f);
        if (CyberOpeningAndTutorialUI.HasCompletedOnboardingThisSession || CyberOpeningAndTutorialUI.IsActive)
            yield break;
        if (DialogUI.IsOpen || PauseMenuUI.IsPaused)
            yield break;

        var input = InputManager.Instance;
        if (input != null && !input.enabled)
            yield break; // Automated smoke test has taken control

        var pc = FindAnyObjectByType<PlayerController>();
        if (pc != null && (!pc.enabled || Mathf.Abs(pc.transform.position.x) > 250f))
            yield break; // Automated smoke test teleported or froze player

        CyberOpeningAndTutorialUI.StartSequence(includeOpeningPrologue: true);
    }

    private void EnsureSilksongFeatures()

    {

        if (playerSpawn == null) return;

        // 1. Secret Memory Core (High Parkour Collectible)
        if (GameObject.Find("SecretCore") == null)
        {
            var core = new GameObject("SecretCore", typeof(CircleCollider2D), typeof(SpriteRenderer), typeof(SecretMemoryCore));
            float coreX;
            float coreY;
            if (worldIndex == 0)
            {
                coreX = segmentIndex == 0 ? 33f : segmentIndex == 1 ? 68f : 175f;
                coreY = segmentIndex == 0 ? -7.0f : segmentIndex == 1 ? -2.5f : 1.5f;
            }
            else
            {
                coreX = playerSpawn.position.x + 22f;
                coreY = playerSpawn.position.y + 6.5f;
            }
            core.transform.position = new Vector3(coreX, coreY, 0f);
        }

        // 2. Interactive Software Engineering Quiz Billboard (1 per stage)
        if (GameObject.Find("SoftwareQuizBoard") == null)
        {
            var qGo = new GameObject("SoftwareQuizBoard");
            float quizX;
            if (worldIndex == 0)
            {
                quizX = segmentIndex == 0 ? 82f : segmentIndex == 1 ? 154f : 162f;
            }
            else
            {
                float quizOffset = segmentIndex == 0 ? 32f : segmentIndex == 1 ? 52f : 65f;
                quizX = playerSpawn.position.x + quizOffset;
            }
            qGo.transform.position = new Vector3(quizX, -9.5f, 0f);
            var qb = qGo.AddComponent<SoftwareQuizBoard>();
            qb.quizBoardId = $"w{worldIndex}_s{segmentIndex}_quiz";
            qb.SnapToGround();
        }

        // 3. Cipher Sequence Puzzle in Segment 2 (Puzzle Plaza)
        if (segmentIndex == 1 && GameObject.Find("TerminalPuzzle") == null)
        {
            var pzGo = new GameObject("TerminalPuzzle", typeof(SpriteRenderer), typeof(TerminalPuzzle));
            float pzX = worldIndex == 0 ? 152f : playerSpawn.position.x + 35f;
            float pzY = worldIndex == 0 ? -12.5f : playerSpawn.position.y;
            pzGo.transform.position = new Vector3(pzX, pzY, 0f);
            SnapObjectToGround(pzGo);
            var pz = pzGo.GetComponent<TerminalPuzzle>();
            pz.puzzleId = $"w{worldIndex}_s{segmentIndex}_cipher";

            var relays = new PuzzleRelay[3];
            Vector3[] relayPositions;
            if (worldIndex == 0)
            {
                relayPositions = new Vector3[]
                {
                    new Vector3(147f, -10.0f, 0f), // Platform_Relay_0
                    new Vector3(158f, -10.0f, 0f), // Platform_Relay_1
                    new Vector3(167f, -5.5f, 0f)   // Pillar_Relay_2
                };
            }
            else
            {
                relayPositions = new Vector3[]
                {
                    new Vector3(playerSpawn.position.x + 31f, playerSpawn.position.y, 0f),
                    new Vector3(playerSpawn.position.x + 35f, playerSpawn.position.y, 0f),
                    new Vector3(playerSpawn.position.x + 39f, playerSpawn.position.y, 0f)
                };
            }

            for (int r = 0; r < 3; r++)
            {
                var rGo = new GameObject($"PuzzleRelay_{r}", typeof(BoxCollider2D), typeof(SpriteRenderer), typeof(PuzzleRelay));
                rGo.transform.position = relayPositions[r];
                SnapObjectToGround(rGo);
                relays[r] = rGo.GetComponent<PuzzleRelay>();
            }
            pz.relays = relays;
        }

        // 4. Hidden Hermit NPC on high vantage point in Segment 3
        if (segmentIndex == 2 && GameObject.Find("HiddenHermit") == null)
        {
            var hermitGo = new GameObject("HiddenHermit");
            float hermitX = worldIndex == 0 ? 175f : playerSpawn.position.x + 48f;
            float hermitY = worldIndex == 0 ? -1.0f : playerSpawn.position.y + 7.5f;
            hermitGo.transform.position = new Vector3(hermitX, hermitY, 0f);
            SnapObjectToGround(hermitGo);
            var hermit = hermitGo.AddComponent<HiddenHermitNPC>();
            hermit.worldIndex = worldIndex;
        }

        

                // Clean up any stray gimmicks that obstruct progression with invisible walls / force fields
        var strayBarrier = GameObject.Find("DataLaserBarrier");
        if (strayBarrier != null) Destroy(strayBarrier);
        var straySwitch = GameObject.Find("DataBlasterSwitch");
        if (straySwitch != null) Destroy(straySwitch);
        var strayUplink = GameObject.Find("AntiGravityUplink");
        if (strayUplink != null) Destroy(strayUplink);
        var strayWind = GameObject.Find("NetworkWindZone");
        if (strayWind != null) Destroy(strayWind);

        // 8. Combat Enrichment: Firewall Sentry Turret & Glitch Phantom encounters
        EnsureSpecialCombatEncounter();
    }

    private void EnsureSpecialCombatEncounter()
    {
        if (playerSpawn == null) return;

        string turretName = $"FirewallTurret_W{worldIndex}_S{segmentIndex}";
        string phantomName = $"GlitchPhantom_W{worldIndex}_S{segmentIndex}";

        // Placement matrix for Firewall Turret
        bool spawnTurret = (worldIndex == 0 && segmentIndex == 2) ||
                           (worldIndex == 1 && segmentIndex == 2) ||
                           (worldIndex == 2 && segmentIndex == 1) ||
                           (worldIndex == 3 && segmentIndex == 2);

        if (spawnTurret && GameObject.Find(turretName) == null)
        {
            float turretOffset = worldIndex == 0 ? 115f : 65f;
            var tGo = new GameObject(turretName, typeof(BoxCollider2D), typeof(Enemy_FirewallTurret));
            tGo.transform.position = new Vector3(playerSpawn.position.x + turretOffset, playerSpawn.position.y, 0f);
            SnapObjectToGround(tGo);
            var turret = tGo.GetComponent<Enemy_FirewallTurret>();
            if (turret != null) turret.facingDirection = -1;
        }

        // Placement matrix for Glitch Phantom
        bool spawnPhantom = (worldIndex == 0 && segmentIndex == 2) ||
                            (worldIndex == 1 && segmentIndex == 1) ||
                            (worldIndex == 2 && segmentIndex == 2) ||
                            (worldIndex == 3 && segmentIndex == 3);

        if (spawnPhantom && GameObject.Find(phantomName) == null)
        {
            float phantomOffset = worldIndex == 0 ? 135f : 85f;
            var pGo = new GameObject(phantomName, typeof(CircleCollider2D), typeof(Rigidbody2D), typeof(Enemy_GlitchPhantom));
            pGo.transform.position = new Vector3(playerSpawn.position.x + phantomOffset, playerSpawn.position.y + 1.2f, 0f);
        }
    }

    private static void SnapObjectToGround(GameObject go)
    {
        if (go == null) return;

        var qb = go.GetComponent<SoftwareQuizBoard>();
        if (qb != null)
        {
            qb.SnapToGround();
            return;
        }

        var ot = go.GetComponent<OverclockTerminal>();
        if (ot != null)
        {
            ot.SnapToGround();
            return;
        }

        Vector2 origin = (Vector2)go.transform.position + Vector2.up * 2.5f;
        foreach (var hit in Physics2D.RaycastAll(origin, Vector2.down, 12f))
        {
            if (hit.collider == null || hit.collider.isTrigger) continue;
            if (hit.collider.gameObject == go) continue;
            if (hit.collider.transform.IsChildOf(go.transform)) continue;
            if (hit.normal.y < 0.5f) continue;

            float bottomOffset = 0f;
            var sr = go.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                bottomOffset = sr.sprite.pivot.y / sr.sprite.pixelsPerUnit;
            }
            else
            {
                var col = go.GetComponentInChildren<Collider2D>();
                if (col != null)
                {
                    bottomOffset = col.bounds.extents.y;
                }
            }
            go.transform.position = new Vector3(go.transform.position.x, hit.point.y + bottomOffset, go.transform.position.z);
            return;
        }
    }

    private void EnsureSegmentBoss()
    {
        if (!IsFinalSegment) return;

        Vector3 spawnPos = serverBPoint != null
            ? serverBPoint.position + Vector3.left * 16f
            : transform.position + Vector3.right * 20f;

        if (worldIndex == 0) // World 1 Stage 3: Grounded 3-Phase Boss Arena
        {
            if (FindAnyObjectByType<Boss_Grounded>() == null)
            {
                var endPad = GameObject.Find("Ground_S3_EndPad");
                float floorY = -17.0f;
                float minX = 276.5f;
                float maxX = 300.5f;
                if (endPad != null)
                {
                    var col = endPad.GetComponent<Collider2D>();
                    if (col != null)
                    {
                        floorY = col.bounds.max.y;
                        minX = col.bounds.min.x + 0.5f;
                        maxX = col.bounds.max.x - 0.5f;
                    }
                }

                SpawnGrounded3PhaseBoss(
                    "Boss_Grounded_Sentinel",
                    "BossArena_W1_S3",
                    "SENTINEL_01 // CORE WARDEN",
                    minX,
                    maxX,
                    floorY,
                    hpPerPhase: 8,
                    rewardBits: 250,
                    ledgerId: "boss:w1_sentinel:kill",
                    achievementId: "",
                    visualScale: 1.05f,
                    cleaveDmg: 20f,
                    dashDmg: 22f,
                    slamDmg: 24f,
                    triggerOffset: 6.2f);
            }
        }
        else if (worldIndex == 1) // World 2 Stage 4: Grounded 3-Phase The Censor
        {
            foreach (var oldCensor in Object.FindObjectsByType<Boss_TheCensor>())
                if (oldCensor != null) Destroy(oldCensor.gameObject);

            if (FindAnyObjectByType<Boss_Grounded>() == null)
            {
                float floorY = DetectBossArenaFloorY(spawnPos.x, -17.0f);
                float minX = spawnPos.x - 14f;
                float maxX = spawnPos.x + 14f;
                EnsureBossArenaFloor("Ground_W2_BossArenaFloor", minX - 1f, maxX + 1f, floorY, worldIndex);

                SpawnGrounded3PhaseBoss(
                    "Boss_Grounded_TheCensor",
                    "BossArena_W2_Censor",
                    "THE CENSOR // GROUND WARDEN",
                    minX,
                    maxX,
                    floorY,
                    hpPerPhase: 8,
                    rewardBits: 200,
                    ledgerId: "boss:the_censor:kill",
                    achievementId: "",
                    visualScale: 1.12f,
                    cleaveDmg: 22f,
                    dashDmg: 24f,
                    slamDmg: 25f,
                    triggerOffset: 6.5f);
            }
        }
        else if (worldIndex == 3) // World 4 Stage 5: Grounded 3-Phase Solaris
        {
            foreach (var oldSolaris in Object.FindObjectsByType<Boss_Solaris>())
                if (oldSolaris != null) Destroy(oldSolaris.gameObject);

            if (FindAnyObjectByType<Boss_Grounded>() == null)
            {
                float floorY = DetectBossArenaFloorY(spawnPos.x, -17.0f);
                float minX = spawnPos.x - 15f;
                float maxX = spawnPos.x + 15f;
                EnsureBossArenaFloor("Ground_W4_BossArenaFloor", minX - 1f, maxX + 1f, floorY, worldIndex);

                SpawnGrounded3PhaseBoss(
                    "Boss_Grounded_Solaris",
                    "BossArena_W4_Solaris",
                    "SOLARIS // THERMAL WARDEN",
                    minX,
                    maxX,
                    floorY,
                    hpPerPhase: 9,
                    rewardBits: 300,
                    ledgerId: "boss:solaris:kill",
                    achievementId: "",
                    visualScale: 1.18f,
                    cleaveDmg: 23f,
                    dashDmg: 25f,
                    slamDmg: 27f,
                    triggerOffset: 6.5f);
            }
        }
        else if (worldIndex == 4) // World 5 (World Boss): Grounded 3-Phase Final Boss (Boss Akhir)
        {
            // Remove legacy floating/aerial Boss_Final / Boss_Rootkit so the World Boss is strictly GROUNDED
            foreach (var oldRootkit in Object.FindObjectsByType<Boss_Rootkit>())
                if (oldRootkit != null) Destroy(oldRootkit.gameObject);

            if (FindAnyObjectByType<Boss_Grounded>() == null)
            {
                float minX = 402.5f;
                float maxX = 447.5f;
                float floorY = -17.5f;

                // Open clean ground-level entry & exit archways on the World 5 arena walls (gates handled by BossArenaController)
                foreach (var wallName in new[] { "Ground_401_15", "Ground_401_16", "Ground_401_17", "Ground_449_15", "Ground_449_16", "Ground_449_17" })
                {
                    var wallBlock = GameObject.Find(wallName);
                    if (wallBlock != null) wallBlock.SetActive(false);
                }

                // Build a continuous solid Boss Arena Floor across x = 401.5..448.5 so grounded Boss & Player never drop into gaps
                EnsureBossArenaFloor("Ground_W5_BossArenaFloor", 401.0f, 449.0f, floorY, worldIndex);

                SpawnGrounded3PhaseBoss(
                    "Boss_Grounded_Final",
                    "BossArena_W5_Final",
                    "BOSS AKHIR // ROOTKIT WARDEN",
                    minX,
                    maxX,
                    floorY,
                    hpPerPhase: 10,
                    rewardBits: 500,
                    ledgerId: "boss:rootkit:kill",
                    achievementId: "ACH_ROOTKIT_PURGE",
                    visualScale: 1.26f,
                    cleaveDmg: 24f,
                    dashDmg: 26f,
                    slamDmg: 28f,
                    triggerOffset: 6.5f);
            }
        }
    }

    private void SpawnGrounded3PhaseBoss(
        string bossObjName,
        string arenaObjName,
        string displayName,
        float minX,
        float maxX,
        float floorY,
        int hpPerPhase,
        int rewardBits,
        string ledgerId,
        string achievementId,
        float visualScale,
        float cleaveDmg,
        float dashDmg,
        float slamDmg,
        float triggerOffset)
    {
        var bossGo = new GameObject(bossObjName);
        bossGo.transform.position = new Vector3(Mathf.Lerp(minX, maxX, 0.74f), floorY + 0.05f, 0f);
        var boss = bossGo.AddComponent<Boss_Grounded>();
        boss.bossName = displayName;
        boss.maxHitPoints = hpPerPhase;
        boss.totalPhases = 3;
        boss.refillHealthEachPhase = true;
        boss.rewardBits = rewardBits;
        boss.rewardLedgerId = ledgerId;
        boss.unlockAchievementId = achievementId;
        boss.visualScale = visualScale;
        boss.cleaveDamage = cleaveDmg;
        boss.dashDamage = dashDmg;
        boss.slamDamage = slamDmg;

        BossArenaController.CreateArena(
            null,
            arenaObjName,
            minX,
            maxX,
            floorY,
            worldIndex,
            boss,
            triggerOffset);
    }

    private float DetectBossArenaFloorY(float centerX, float fallbackY)
    {
        float bestY = fallbackY;
        float bestDist = float.MaxValue;
        foreach (var col in Object.FindObjectsByType<Collider2D>())
        {
            if (col == null || col.isTrigger) continue;
            var go = col.gameObject;
            if (!go.name.Contains("Ground") && !go.name.Contains("Platform")) continue;
            if (go.name.StartsWith("SafeChamber_")) continue;
            if (col.bounds.min.x <= centerX + 12f && col.bounds.max.x >= centerX - 12f)
            {
                float d = Mathf.Abs(col.bounds.center.x - centerX);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestY = col.bounds.max.y;
                }
            }
        }
        return bestY;
    }

    private void EnsureBossArenaFloor(string objName, float minX, float maxX, float floorSurfaceY, int wIdx)
    {
        if (GameObject.Find(objName) != null) return;
        float width = Mathf.Max(8f, maxX - minX);
        float centerX = (minX + maxX) * 0.5f;
        const float thickness = 2.0f;

        var floorGo = new GameObject(objName);
        floorGo.transform.position = new Vector3(centerX, floorSurfaceY - thickness * 0.5f, 0f);
        floorGo.layer = DetectGroundLayer();

        var col = floorGo.AddComponent<BoxCollider2D>();
        col.size = new Vector2(width, thickness);

        var sr = floorGo.AddComponent<SpriteRenderer>();
        int wNum = Mathf.Clamp(wIdx + 1, 1, 5);
        sr.sprite = Resources.Load<Sprite>($"GeneratedArt/ground_w{wNum}") ?? Resources.Load<Sprite>("GeneratedArt/ground");
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(width, thickness);
        sr.sortingOrder = 2;
        Physics2D.SyncTransforms();
    }

    private void EnsureSafeZones()
    {
        if (TutorialWorldBuilder.IsTutorialWorldActive)
        {
            // Wipe ALL World 1 Stage 1 static geometry and lay down clean Tutorial World 00 endpoints BEFORE building chambers!
            TutorialWorldBuilder.PrepareCleanTutorialCanvas(this);
        }
        else
        {
            // Build World 1 multi-tier vertical parkour layout
            StageParkourGenerator.BuildParkourStage(this, worldIndex, segmentIndex);
        }
        Physics2D.SyncTransforms();

        float roomWidth = 24.0f;
        float roomHeight = 5.5f;
        float floorThick = 2.0f;

        // Clean out legacy scene SafeZone visuals so they don't duplicate or obstruct the bunker room
        foreach (var name in new[] { "SafeZones", "SafeZone_Arrival", "SafeZone_Departure", "SafePad_Start", "SafePad_End", "SafeChamber_Start", "SafeChamber_End", "BridgeConnector_Start", "BridgeConnector_End" })
        {
            var oldObj = GameObject.Find(name);
            if (oldObj != null) Destroy(oldObj);
        }

        foreach (var szExisting in Object.FindObjectsByType<SafeZone>())
        {
            if (szExisting != null && (szExisting.transform.parent == null || !szExisting.transform.parent.name.StartsWith("SafeChamber_")))
            {
                Destroy(szExisting.gameObject);
            }
        }

        // 1. Ensure Arrival Safe Zone Chamber (enclosed bunker placed to the left of level start)
        var arrivalChamber = GameObject.Find("SafeChamber_Start");
        if (arrivalChamber == null && playerSpawn != null)
        {
            float doorX = playerSpawn.position.x;
            float floorSurfaceY = playerSpawn.position.y;

            // Direct check for start ground platform
            var startPadGo = GameObject.Find("Ground_StartPad") ?? 
                             GameObject.Find("Ground_S2_Start") ?? 
                             GameObject.Find("Ground_S3_Start");
            if (startPadGo != null)
            {
                var col = startPadGo.GetComponent<Collider2D>();
                if (col != null) floorSurfaceY = col.bounds.max.y;
            }
            else
            {
                // Fallback scan
                Collider2D firstGround = null;
                float closestDist = float.MaxValue;
                foreach (var col in Object.FindObjectsByType<Collider2D>())
                {
                    if (col == null || col.isTrigger) continue;
                    var go = col.gameObject;
                    if (go.GetComponentInParent<PlayerController>() != null) continue;
                    if (go.GetComponentInParent<NPC_Base>() != null) continue;
                    if (go.name.StartsWith("SafeChamber_") || go.name.StartsWith("BridgeConnector_")) continue;
                    if (!go.name.Contains("Ground") && !go.name.Contains("Platform")) continue;

                    if (col.bounds.max.x >= doorX - 1.0f)
                    {
                        float dist = Mathf.Max(0f, col.bounds.min.x - doorX);
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            firstGround = col;
                        }
                    }
                }
                if (firstGround != null) floorSurfaceY = firstGround.bounds.max.y;
            }

            if (worldIndex == 0)
            {
                floorSurfaceY = -17.0f; // Deterministic anchor for World 1
            }

            float floorCenterY = floorSurfaceY - floorThick * 0.5f;
            float floorCenterX = doorX - roomWidth * 0.5f;
            Vector3 padCenter = new Vector3(floorCenterX, floorCenterY, 0f);

            BuildChamberRoom("SafeChamber_Start", padCenter, roomWidth, floorThick, roomHeight, doorOnRight: true);

            // Connect chamber doorway to first stage ground platform with a continuous bridge if needed
            float chamberFloorRightEdge = floorCenterX + roomWidth * 0.5f;
            var startPadCol = startPadGo != null ? startPadGo.GetComponent<Collider2D>() : null;
            if (startPadCol != null && startPadCol.bounds.min.x > chamberFloorRightEdge - 0.05f)
            {
                float startX = chamberFloorRightEdge - 0.1f;
                float endX = startPadCol.bounds.min.x + 0.5f;
                float bridgeWidth = endX - startX;
                float bridgeCenterX = startX + bridgeWidth * 0.5f;
                Vector3 bridgeCenter = new Vector3(bridgeCenterX, floorCenterY, 0f);
                var chamber = GameObject.Find("SafeChamber_Start");
                Transform parentTr = chamber != null ? chamber.transform : transform;
                BuildChamberPad(parentTr, "BridgeConnector_Start", bridgeCenter, bridgeWidth, floorThick, DetectGroundLayer());
            }

            // Relocate player spawn safely inside the chamber in front of arrival portal (-9.2)
            playerSpawn.position = new Vector3(floorCenterX - 7.2f, floorSurfaceY + 0.5f, playerSpawn.position.z);
        }

        // 2. Ensure Departure Safe Zone Chamber around serverBPoint / Goal
        var departureChamber = GameObject.Find("SafeChamber_End");
        Vector3 goalPos = EndSafeZone();
        if (departureChamber == null && goalPos != Vector3.zero)
        {
            float doorX = goalPos.x - 1.5f;
            float floorSurfaceY = goalPos.y;

            // Direct check for end ground platform
            var endPadGo = GameObject.Find("Ground_EndPad") ?? 
                           GameObject.Find("Ground_S2_EndPad") ?? 
                           GameObject.Find("Ground_S3_EndPad");
            if (endPadGo != null)
            {
                var col = endPadGo.GetComponent<Collider2D>();
                if (col != null) floorSurfaceY = col.bounds.max.y;
            }
            else
            {
                // Fallback scan
                Collider2D lastGround = null;
                float closestEndDist = float.MaxValue;
                foreach (var col in Object.FindObjectsByType<Collider2D>())
                {
                    if (col == null || col.isTrigger) continue;
                    var go = col.gameObject;
                    if (go.GetComponentInParent<PlayerController>() != null) continue;
                    if (go.GetComponentInParent<NPC_Base>() != null) continue;
                    if (go.name.StartsWith("SafeChamber_") || go.name.StartsWith("BridgeConnector_")) continue;
                    if (!go.name.Contains("Ground") && !go.name.Contains("Platform")) continue;

                    if (col.bounds.min.x <= doorX + 1.0f)
                    {
                        float dist = Mathf.Max(0f, doorX - col.bounds.max.x);
                        if (dist < closestEndDist)
                        {
                            closestEndDist = dist;
                            lastGround = col;
                        }
                    }
                }
                if (lastGround != null) floorSurfaceY = lastGround.bounds.max.y;
            }

            if (worldIndex == 0)
            {
                floorSurfaceY = -17.0f; // Deterministic anchor for World 1
            }

            float floorCenterY = floorSurfaceY - floorThick * 0.5f;
            float floorCenterX = doorX + roomWidth * 0.5f;
            Vector3 padCenter = new Vector3(floorCenterX, floorCenterY, 0f);

            BuildChamberRoom("SafeChamber_End", padCenter, roomWidth, floorThick, roomHeight, doorOnRight: false);

            // Connect last stage ground platform to departure chamber doorway
            float chamberFloorLeftEdge = floorCenterX - roomWidth * 0.5f;
            var endPadCol = endPadGo != null ? endPadGo.GetComponent<Collider2D>() : null;
            if (endPadCol != null && endPadCol.bounds.max.x < chamberFloorLeftEdge + 0.05f)
            {
                float startX = endPadCol.bounds.max.x - 0.5f;
                float endX = chamberFloorLeftEdge + 0.1f;
                float bridgeWidth = endX - startX;
                float bridgeCenterX = startX + bridgeWidth * 0.5f;
                Vector3 bridgeCenter = new Vector3(bridgeCenterX, floorCenterY, 0f);
                var chamber = GameObject.Find("SafeChamber_End");
                Transform parentTr = chamber != null ? chamber.transform : transform;
                BuildChamberPad(parentTr, "BridgeConnector_End", bridgeCenter, bridgeWidth, floorThick, DetectGroundLayer());
            }

            float portalX = floorCenterX + (roomWidth * 0.5f - 2.8f);
            if (serverBPoint != null)
                serverBPoint.position = new Vector3(portalX, floorSurfaceY + 1.2f, serverBPoint.position.z);

            var goal = FindAnyObjectByType<ServerB_Goal>();
            if (goal == null)
            {
                var goalGo = serverBPoint != null ? serverBPoint.gameObject : new GameObject("ServerB_Goal");
                goalGo.transform.position = new Vector3(portalX, floorSurfaceY + 1.2f, 0f);
                goal = goalGo.AddComponent<ServerB_Goal>();
                goal.levelManager = this;
                var col = goalGo.GetComponent<BoxCollider2D>();
                if (col == null) col = goalGo.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(2.4f, 3.2f);
            }
            else
            {
                goal.transform.position = new Vector3(portalX, floorSurfaceY + 1.2f, goal.transform.position.z);
                goal.levelManager = this;
            }
        }

        Physics2D.SyncTransforms();
    }

    private void BuildChamberRoom(string prefix, Vector3 floorCenter, float width, float floorThick, float roomHeight, bool doorOnRight)

    {

        var root = new GameObject(prefix);

        root.transform.position = floorCenter;

        int layer = DetectGroundLayer();

        // 1. Solid Floor

        BuildChamberPad(root.transform, $"{prefix}_Floor", floorCenter, width, floorThick, layer);

        float padTop = floorCenter.y + floorThick * 0.5f;

        float ceilingY = padTop + roomHeight + floorThick * 0.5f;

        var ceilingCenter = new Vector3(floorCenter.x, ceilingY, floorCenter.z);

        // 2. Solid Ceiling

        BuildChamberPad(root.transform, $"{prefix}_Ceiling", ceilingCenter, width, floorThick, layer);

        // 3. Solid Back Wall (encloses the safe room)

        float wallThick = 1.4f;

        float wallX = doorOnRight ? (floorCenter.x - width * 0.5f + wallThick * 0.5f) : (floorCenter.x + width * 0.5f - wallThick * 0.5f);

        float wallY = padTop + roomHeight * 0.5f;

        var wallCenter = new Vector3(wallX, wallY, floorCenter.z);

        BuildChamberPad(root.transform, $"{prefix}_BackWall", wallCenter, wallThick, roomHeight, layer);

        // 4. Chamber Dark Cyber Backdrop

        var backdrop = new GameObject($"{prefix}_Backdrop");

        backdrop.transform.SetParent(root.transform, false);

        backdrop.transform.position = new Vector3(floorCenter.x, wallY, 0.2f);

        var bgSr = backdrop.AddComponent<SpriteRenderer>();

        bgSr.sprite = WorldThemeApplier.GetSafePadSprite(subsurface: true) ?? GetPadSprite();

        bgSr.drawMode = SpriteDrawMode.Tiled;

        bgSr.tileMode = SpriteTileMode.Continuous;

        bgSr.size = new Vector2(width - wallThick, roomHeight);

        bgSr.color = new Color(0.26f, 0.32f, 0.42f, 0.96f);

        bgSr.sortingOrder = -5;

        // 5. Ceiling Neon Light Strip

        var lightBar = new GameObject($"{prefix}_CeilingLight");

        lightBar.transform.SetParent(root.transform, false);

        lightBar.transform.position = new Vector3(floorCenter.x, padTop + roomHeight - 0.1f, 0f);

        var lightSr = lightBar.AddComponent<SpriteRenderer>();

        var boxSpr = NPC_Base.BoxSprite();

        lightSr.sprite = boxSpr;

        lightSr.color = new Color(0.1f, 0.95f, 1f, 0.85f);

        lightSr.sortingOrder = 2;

        var unit = boxSpr.bounds.size;

        lightBar.transform.localScale = new Vector3((width - 3.5f) / unit.x, 0.12f / unit.y, 1f);

        // 6. Corner Boundary Door dividing Safe Zone and outside world
        float doorX = doorOnRight ? (floorCenter.x + width * 0.5f) : (floorCenter.x - width * 0.5f);
        var doorPos = new Vector3(doorX, padTop, 0f);

        var doorGo = new GameObject("WorldDoor");

        doorGo.transform.SetParent(root.transform, false);

        doorGo.transform.position = doorPos;

        var doorComp = doorGo.AddComponent<WorldDoor>();

        doorComp.doorSize = new Vector2(2.0f, roomHeight * 0.65f);

        // 7. Arrival Portal if start chamber (door on right)

        if (doorOnRight)

        {

            var portalPos = new Vector3(floorCenter.x - width * 0.5f + 2.8f, padTop + 1.2f, 0f);

            SafeZoneArrivalPortal.Spawn(portalPos, root.transform);

        }

        // 8. SafeZone Trigger Collider (invisible logic volume without giant cyan boxes)

        string zoneName = prefix.Contains("Start") ? "SafeZone_Arrival" : "SafeZone_Departure";

        var zoneGo = new GameObject(zoneName);

        zoneGo.transform.SetParent(root.transform, false);

        zoneGo.transform.position = new Vector3(floorCenter.x, wallY, 0f);

        var col = zoneGo.AddComponent<BoxCollider2D>();

        col.isTrigger = true;

        col.size = new Vector2(width - 2f, roomHeight + 1f);

        var sz = zoneGo.AddComponent<SafeZone>();

        sz.ConfigureAsInvisible();

        // 9. Clean up any level geometry or entities intruding into this safe chamber

        CleanupIntrudersInChamber(floorCenter, width, roomHeight, doorOnRight);

    }

    private void CleanupIntrudersInChamber(Vector3 floorCenter, float width, float roomHeight, bool doorOnRight)

    {

        float minX = floorCenter.x - width * 0.5f;

        float maxX = floorCenter.x + width * 0.5f;

        float minY = floorCenter.y;

        float maxY = floorCenter.y + roomHeight + 3f;

        var allColliders = Object.FindObjectsByType<Collider2D>();

        foreach (var c in allColliders)

        {

            if (c == null || c.gameObject == null) continue;

            var go = c.gameObject;

            if (go.name.StartsWith("SafeChamber_") || go.name.StartsWith("WorldDoor") || go.name.Contains("Portal") || go.name.Contains("Goal")) continue;

            if (go.GetComponentInParent<PlayerController>() != null) continue;

            if (go.GetComponentInParent<NPC_Base>() != null) continue;

            if (go.GetComponentInParent<SafeZone>() != null) continue;

            if (go.GetComponentInParent<OverclockTerminal>() != null) continue;

            if (go.GetComponentInParent<SafeZonePortal>() != null) continue;

            if (go.GetComponentInParent<SafeZoneArrivalPortal>() != null) continue;

            if (go.GetComponentInParent<ServerB_Goal>() != null) continue;

            Vector3 p = go.transform.position;

            if (p.x >= minX && p.x <= maxX && p.y >= minY && p.y <= maxY)

            {

                Destroy(go);

            }

        }

    }

    private static void BuildChamberPad(Transform parent, string name, Vector3 center, float width, float thick, int layer)
    {
        var pad = new GameObject(name);
        pad.transform.SetParent(parent, false);
        pad.layer = layer;
        pad.transform.localScale = Vector3.one;

        float topY = center.y + thick * 0.5f;
        float finalThick = thick;
        Vector3 finalCenter = center;

        // Extend chamber floor and bridge foundations down to deep bedrock (-30) like Mario
        if (name.EndsWith("_Floor") || name.StartsWith("BridgeConnector_"))
        {
            float groundBottom = -30.0f;
            if (topY > groundBottom)
            {
                finalThick = topY - groundBottom;
                finalCenter = new Vector3(center.x, topY - finalThick * 0.5f, center.z);
            }
        }

        pad.transform.position = finalCenter;

        var padBox = pad.AddComponent<BoxCollider2D>();
        padBox.size = new Vector2(width, finalThick);
        padBox.offset = Vector2.zero;
        padBox.isTrigger = false;

        var visual = pad.AddComponent<SpriteRenderer>();
        bool isBackWall = name.EndsWith("_BackWall");
        bool isBridge = name.StartsWith("BridgeConnector_");
        visual.sprite = isBackWall
            ? (WorldThemeApplier.GetSafePadSprite(subsurface: true) ?? GetPadSprite())
            : (isBridge ? (WorldThemeApplier.GetGroundSprite(0, subsurface: false) ?? GetPadSprite()) : (WorldThemeApplier.GetSafePadSprite(subsurface: false) ?? GetPadSprite()));
        visual.drawMode = SpriteDrawMode.Tiled;
        visual.tileMode = SpriteTileMode.Continuous;
        visual.size = new Vector2(width, finalThick);
        visual.color = Color.white;
        visual.sortingOrder = 0;

        if (!isBackWall && !isBridge && finalThick > 2.1f)
        {
            WorldThemeApplier.ApplySegmentedGround(
                visual,
                WorldThemeApplier.GetSafePadSprite(subsurface: false) ?? GetPadSprite(),
                WorldThemeApplier.GetSafePadSprite(subsurface: true) ?? GetPadSprite(),
                2.0f);
        }
    }

    private static int DetectGroundLayer()

    {

        foreach (var col in Object.FindObjectsByType<Collider2D>())

        {

            if (col == null || col.isTrigger) continue;

            var go = col.gameObject;

            if (go.GetComponentInParent<PlayerController>() != null) continue;

            if (go.GetComponentInParent<SafeZone>() != null) continue;

            if (go.name.Contains("Ground") || go.name.Contains("Platform")) return go.layer;

        }

        return 0;

    }

    /// <summary>
    /// Mario-style ground foundation: pulls all solid ground terrain foundations down into deep bedrock (y = -30),
    /// providing sturdy architectural foundations while strictly preserving the exact top walkable surface.
    /// Platforms (one-way catwalks, moving platforms), hazards, and pillars remain floating as intended.
    /// </summary>
    public void ExtendAllGroundsToBedrock()
    {
        float bedrockY = -30.0f;
        foreach (var col in Object.FindObjectsByType<Collider2D>())
        {
            if (col == null || col.isTrigger) continue;
            var go = col.gameObject;
            if (go.GetComponentInParent<PlayerController>() != null) continue;
            if (go.GetComponentInParent<NPC_Base>() != null) continue;
            if (go.GetComponentInParent<Enemy_Patrol>() != null) continue;
            if (go.GetComponentInParent<Enemy_Flying>() != null) continue;

            string name = go.name;
            // Exclude moving platforms, one-way ledges, spikes, pillars, or observatory deck (has its own tunnel + pillars)
            if (name.Contains("Platform") || name.Contains("OneWay") || name.Contains("Moving") || 
                name.Contains("Spike") || name.Contains("Hazard") || name.Contains("Pillar") || 
                name == "Ground_QuizObservatory")
            {
                continue;
            }

            // Target solid grounds, chamber floors, bridge connectors, and ground slabs
            bool isGround = name.Contains("Ground") || name.EndsWith("_Floor") || 
                            name.StartsWith("BridgeConnector_") || name.StartsWith("SafePad_");
            if (!isGround) continue;

            var box = col as BoxCollider2D;
            if (box == null) continue;

            float topY = box.bounds.max.y;
            float targetBedrockY = bedrockY;

            // Check if there is another solid ground slab directly underneath this one
            Vector2 castOrigin = new Vector2(box.bounds.center.x, box.bounds.min.y - 0.05f);
            foreach (var hit in Physics2D.RaycastAll(castOrigin, Vector2.down, Mathf.Abs(box.bounds.min.y - bedrockY)))
            {
                if (hit.collider != null && !hit.collider.isTrigger && hit.collider != col && hit.normal.y > 0.5f)
                {
                    string hName = hit.collider.gameObject.name;
                    if (hName.Contains("Ground") || hName.EndsWith("_Floor") || hName.StartsWith("BridgeConnector_"))
                    {
                        targetBedrockY = Mathf.Max(targetBedrockY, hit.point.y);
                        break;
                    }
                }
            }

            if (topY <= targetBedrockY + 1.0f) continue; // Already at or below target bedrock

            float totalHeight = topY - targetBedrockY;
            float currentCenterY = box.bounds.center.y;
            float targetCenterY = topY - totalHeight * 0.5f;
            float dy = targetCenterY - currentCenterY;

            // Only adjust if not already deep enough
            if (box.size.y < totalHeight - 0.2f)
            {
                go.transform.position = new Vector3(go.transform.position.x, go.transform.position.y + dy, go.transform.position.z);
                box.size = new Vector2(box.size.x, totalHeight);
                box.offset = Vector2.zero;

                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.size = new Vector2(sr.size.x, totalHeight);
                    sr.drawMode = SpriteDrawMode.Tiled;
                    sr.tileMode = SpriteTileMode.Continuous;
                    WorldThemeApplier.SyncSurfaceCapPosition(go);
                }
            }
        }
        Physics2D.SyncTransforms();
    }

    /// <summary>
    /// Snaps all patrolling ground enemies squarely onto the walkable terrain surface,
    /// eliminating any vertical embedding, clipping, or hovering.
    /// </summary>
    public void SnapAllEnemiesToGround()
    {
        foreach (var enemy in Object.FindObjectsByType<Enemy_Patrol>())
        {
            if (enemy != null) enemy.SnapToGroundSurface();
        }
        Physics2D.SyncTransforms();
    }

    private static Sprite _cachedPadSprite;

    private static Sprite GetPadSprite()

    {

        if (_cachedPadSprite != null) return _cachedPadSprite;

        _cachedPadSprite = Resources.Load<Sprite>("GeneratedArt/safepad");

        if (_cachedPadSprite != null) return _cachedPadSprite;

        const int w = 32;

        const int h = 16;

        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)

        {

            name = "SafePad_Runtime",

            filterMode = FilterMode.Point,

            wrapMode = TextureWrapMode.Repeat

        };

        var pixels = new Color[w * h];

        for (int y = 0; y < h; y++)

        {

            for (int x = 0; x < w; x++)

            {

                if (y >= h - 2)

                    pixels[y * w + x] = new Color(0.15f, 0.95f, 0.85f, 1f);

                else if (y >= h - 4)

                    pixels[y * w + x] = new Color(0.08f, 0.28f, 0.35f, 1f);

                else

                    pixels[y * w + x] = new Color(0.05f, 0.12f, 0.18f, 1f);

            }

        }

        texture.SetPixels(pixels);

        texture.Apply();

        _cachedPadSprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f, 0, SpriteMeshType.FullRect);

        return _cachedPadSprite;

    }

    private void Update()

    {

        if (_health && !_health.isDead && _health.transform.position.y < -25f) _health.Die();

    }

    public void CompleteSegment()

    {

        if (isComplete) return;

        if (IsGoalLocked)

        {

            ShowGateHint();

            return;

        }

        isComplete = true;

        if (questGateHint != null) questGateHint.Hide();

        GameManager.Instance?.OnSegmentComplete();

    }

    /// <summary>Publishes the locked-goal quest hint. Cheap to call every frame while blocked.</summary>

    public void ShowGateHint()

    {

        var activeBoss = BossTracker.ActiveBoss;

        if (activeBoss != null && !activeBoss.IsDead)

        {

            LastGateHint = $"Kalahkan {activeBoss.BossName} untuk membuka Server B!";

            if (questGateHint != null) questGateHint.Show(LastGateHint);

            return;

        }

        if (Boss_Rootkit.Instance != null && !Boss_Rootkit.Instance.IsDead)

        {

            LastGateHint = $"Kalahkan {Boss_Rootkit.Instance.bossName} untuk membuka Server B!";

            if (questGateHint != null) questGateHint.Show(LastGateHint);

            return;

        }

        if (BossArenaController.HasUnfinishedArena)

        {

            LastGateHint = "Masuki Arena dan kalahkan Sentinel Boss untuk membuka Server B!";

            if (questGateHint != null) questGateHint.Show(LastGateHint);

            return;

        }

        if (_quests == null) _quests = QuestService.Ensure();

        var hint = _quests != null

            ? _quests.GetLockedHint(worldIndex)

            : PackageQuestRules.LockedHint(PackageState.None);

        if (hint != LastGateHint)

        {

            LastGateHint = hint;

            Debug.Log("[" + gameObject.name + "] " + hint, this);

        }

        if (questGateHint != null) questGateHint.Show(LastGateHint);

    }

    /// <summary>The single gate condition; optional quests must not change it.</summary>

    public bool PackageDelivered()

    {

        if (_quests == null) _quests = QuestService.Ensure();

        return _quests != null && _quests.IsPackageDelivered(worldIndex);

    }

    /// <summary>

    /// Drops the player onto solid ground below the spawn so the run never starts

    /// hovering, however the spawn was authored. No ground in reach: unchanged.

    /// </summary>

    private static void SnapPlayerToGround(PlayerController player)
    {
        if (player == null) return;
        Physics2D.SyncTransforms();
        var col = player.GetComponent<BoxCollider2D>();
        float halfH = col != null ? col.size.y * 0.5f * player.transform.localScale.y : 0.5f;
        float colOffsetY = col != null ? col.offset.y * player.transform.localScale.y : 0f;

        Vector2 origin = (Vector2)player.transform.position + Vector2.up * 2.0f;
        Collider2D hitGround = null;
        float highestY = float.MinValue;

        foreach (var hit in Physics2D.RaycastAll(origin, Vector2.down, 40f))
        {
            if (hit.collider == null || hit.collider.isTrigger) continue;
            if (hit.collider.GetComponentInParent<PlayerController>() != null) continue;
            if (hit.collider.GetComponentInParent<NPC_Base>() != null) continue;
            if (hit.collider.GetComponentInParent(typeof(IEnemyDamageable)) as IEnemyDamageable != null) continue;
            if (hit.normal.y < 0.4f) continue;

            if (hit.point.y > highestY)
            {
                highestY = hit.point.y;
                hitGround = hit.collider;
            }
        }

        if (hitGround != null)
        {
            player.transform.position = new Vector3(
                player.transform.position.x, highestY + halfH - colOffsetY, player.transform.position.z);
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null) body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            return;
        }

        var lm = Object.FindAnyObjectByType<LevelManager>();
        if (lm != null && lm.worldIndex == 0)
        {
            player.transform.position = new Vector3(player.transform.position.x, -17.0f + halfH - colOffsetY, player.transform.position.z);
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null) body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
        }
    }

    private Vector3 EndSafeZone()

    {

        if (serverBPoint != null) return serverBPoint.position;

        var goal = FindAnyObjectByType<ServerB_Goal>();

        if (goal != null) return goal.transform.position + Vector3.left * 2.4f;

        return transform.position;

    }

    private void OnDrawGizmos()

    {

        if (playerSpawn)

        {

            Gizmos.color = Color.cyan;

            Gizmos.DrawWireCube(playerSpawn.position, Vector3.one * 0.5f);

        }

        if (serverBPoint)

        {

            Gizmos.color = Color.yellow;

            Gizmos.DrawWireCube(serverBPoint.position, Vector3.one * 0.5f);

        }

    }

    private void PopulateStageHazardsAndPlatforms()

    {

        if (playerSpawn == null) return;

        if (GameObject.Find("GeneratedHazardsRoot") != null) return;

        // If the level already has enemies or moving platforms (e.g. handcrafted layout), skip procedural injection

        if (Object.FindObjectsByType<Enemy_Patrol>().Length > 0 || Object.FindObjectsByType<Platform_Moving>().Length > 0)

            return;

        var hazardsRoot = new GameObject("GeneratedHazardsRoot");

        int groundLayer = DetectGroundLayer();

        var movingPlatSpr = Resources.Load<Sprite>("GeneratedArt/platform_moving");

        var onewayPlatSpr = Resources.Load<Sprite>("GeneratedArt/platform_oneway");

        var enemySpr = Resources.Load<Sprite>("GeneratedArt/enemy");

        var droneSpr = Resources.Load<Sprite>("GeneratedArt/enemy_flying");

        // 1. Collect all solid ground colliders sorted by X

        var groundColliders = new System.Collections.Generic.List<Collider2D>();

        foreach (var col in Object.FindObjectsByType<Collider2D>())

        {

            if (col == null || col.isTrigger) continue;

            var go = col.gameObject;

            if (go.GetComponentInParent<PlayerController>() != null) continue;

            if (go.GetComponentInParent<NPC_Base>() != null) continue;

            if (go.name.StartsWith("SafeChamber_") || go.name.StartsWith("BridgeConnector_")) continue;

            if (go.name.Contains("Ground") || go.name.Contains("Platform"))

                groundColliders.Add(col);

        }

        groundColliders.Sort((a, b) => a.bounds.min.x.CompareTo(b.bounds.min.x));

        // 2. Identify chasms / gaps between adjacent platforms to place moving platforms and parkour

        for (int i = 0; i < groundColliders.Count - 1; i++)

        {

            var left = groundColliders[i];

            var right = groundColliders[i + 1];

            float gap = right.bounds.min.x - left.bounds.max.x;

            if (gap >= 5.0f && gap <= 26.0f)

            {

                float midX = (left.bounds.max.x + right.bounds.min.x) * 0.5f;

                float midY = (left.bounds.max.y + right.bounds.max.y) * 0.5f;

                // Spawn a moving platform in the chasm

                var platGo = new GameObject($"Platform_Moving_Chasm_{i}");

                platGo.transform.SetParent(hazardsRoot.transform, false);

                platGo.transform.position = new Vector3(midX, midY, 0f);

                platGo.layer = groundLayer;

                var platBox = platGo.AddComponent<BoxCollider2D>();

                platBox.size = new Vector2(3.5f, 0.6f);

                var platSr = platGo.AddComponent<SpriteRenderer>();

                platSr.sprite = movingPlatSpr != null ? movingPlatSpr : GetPadSprite();

                platSr.drawMode = SpriteDrawMode.Tiled;

                platSr.tileMode = SpriteTileMode.Continuous;

                platSr.size = new Vector2(3.5f, 0.6f);

                platSr.color = Color.white;

                platSr.sortingOrder = 1;

                var platRb = platGo.AddComponent<Rigidbody2D>();

                platRb.bodyType = RigidbodyType2D.Kinematic;

                platRb.interpolation = RigidbodyInterpolation2D.Interpolate;

                var mover = platGo.AddComponent<Platform_Moving>();

                mover.pointA = new Vector2(-Mathf.Min(3.5f, gap * 0.25f), 0f);

                mover.pointB = new Vector2(Mathf.Min(3.5f, gap * 0.25f), 0f);

                mover.speed = 2.4f;

                // Above moving platform, spawn an Enemy_Flying drone patrolling the skies

                if (i % 2 == 0)

                {

                    var droneGo = new GameObject($"Enemy_Flying_Gap_{i}");

                    droneGo.transform.SetParent(hazardsRoot.transform, false);

                    droneGo.transform.position = new Vector3(midX, midY + 3.6f, 0f);

                    var dSr = droneGo.AddComponent<SpriteRenderer>();

                    dSr.sprite = droneSpr != null ? droneSpr : NPC_Base.BoxSprite();

                    dSr.color = Color.white;

                    dSr.sortingOrder = 4;

                    var dCol = droneGo.AddComponent<CircleCollider2D>();

                    dCol.isTrigger = true;

                    dCol.radius = 0.5f;

                    var dRb = droneGo.AddComponent<Rigidbody2D>();

                    dRb.bodyType = RigidbodyType2D.Kinematic;

                    var flying = droneGo.AddComponent<Enemy_Flying>();

                    flying.SetHome(droneGo.transform.position);

                    flying.patrolRadius = Mathf.Min(3.0f, gap * 0.2f);

                    flying.damageAmount = 12f;

                }

                // Add elevated parkour one-way platform

                if (gap >= 8.5f)

                {

                    var parkourGo = new GameObject($"Platform_Parkour_{i}");

                    parkourGo.transform.SetParent(hazardsRoot.transform, false);

                    parkourGo.transform.position = new Vector3(midX, midY + 3.8f, 0f);

                    parkourGo.layer = groundLayer;

                    var pBox = parkourGo.AddComponent<BoxCollider2D>();

                    pBox.size = new Vector2(3.0f, 0.45f);

                    pBox.usedByEffector = true;

                    var pSr = parkourGo.AddComponent<SpriteRenderer>();

                    pSr.sprite = onewayPlatSpr != null ? onewayPlatSpr : GetPadSprite();

                    pSr.drawMode = SpriteDrawMode.Tiled;

                    pSr.tileMode = SpriteTileMode.Continuous;

                    pSr.size = new Vector2(3.0f, 0.45f);

                    pSr.color = Color.white;

                    pSr.sortingOrder = 1;

                    parkourGo.AddComponent<PlatformEffector2D>();

                }

            }

        }

        // 3. Populate patrol monsters on wide ground platforms (width >= 6.5)

        for (int i = 0; i < groundColliders.Count; i++)

        {

            var col = groundColliders[i];

            float width = col.bounds.size.x;

            float centerX = col.bounds.center.x;

            float topY = col.bounds.max.y;

            // Keep away from player spawn and goal safe zones

            if (playerSpawn != null && Mathf.Abs(centerX - playerSpawn.position.x) < 18f) continue;

            if (serverBPoint != null && Mathf.Abs(centerX - serverBPoint.position.x) < 18f) continue;

            // Keep away from checkpoints

            bool nearCp = false;

            foreach (var cp in Object.FindObjectsByType<Checkpoint_TCP>())

            {

                if (cp != null && Mathf.Abs(centerX - cp.transform.position.x) < 8.0f) { nearCp = true; break; }

            }

            if (nearCp) continue;

            if (width >= 6.5f)

            {

                var patrolGo = new GameObject($"Enemy_Patrol_Ground_{i}");

                patrolGo.transform.SetParent(hazardsRoot.transform, false);

                patrolGo.transform.position = new Vector3(centerX, topY + 0.65f, 0f);

                var pSr = patrolGo.AddComponent<SpriteRenderer>();

                pSr.sprite = enemySpr != null ? enemySpr : NPC_Base.BoxSprite();

                pSr.color = Color.white;

                pSr.sortingOrder = 4;

                var pCol = patrolGo.AddComponent<BoxCollider2D>();

                pCol.isTrigger = true;

                pCol.size = new Vector2(0.85f, 0.85f);

                var pRb = patrolGo.AddComponent<Rigidbody2D>();

                pRb.bodyType = RigidbodyType2D.Kinematic;

                var patrol = patrolGo.AddComponent<Enemy_Patrol>();

                patrol.SetHome(patrolGo.transform.position);

                float patrolSpan = Mathf.Clamp(width * 0.35f, 1.5f, 5.0f);

                patrol.pointA = new Vector2(-patrolSpan, 0f);

                patrol.pointB = new Vector2(patrolSpan, 0f);

                patrol.patrolSpeed = 2.4f;

                patrol.damageAmount = 15f;

            }

        }

    }

}


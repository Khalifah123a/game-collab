using System.Collections.Generic;

using UnityEngine;

/// <summary>

/// World 1 ("Localhost") Parkour and Multi-Elevation Stage Generator.

/// Transforms previously flat, monotonous straight-line stages into rhythmic,

/// multi-tiered vertical parkour courses featuring:

/// - Varied elevation terrain islands (y = -18, -15, -11, -7, -4, -23)

/// - Stepping one-way platforms forming stairways and aerial ledges

/// - Horizontal and Vertical moving platforms (elevators) bridging deep chasms

/// - Spike hazard pits beneath all parkour chasms

/// - Strategic enemy stations: Ground Crawlers and Flying Drones guarding airspace

/// - Parabolic DataBit trails and Code Snippet secrets on high perches

/// - Seamless alignment with Arrival and Departure Safe Zones (y = -18)

/// </summary>

public static class StageParkourGenerator

{

    public static void BuildParkourStage(LevelManager levelManager, int worldIndex, int segmentIndex)

    {

        if (worldIndex != 0) return; // Dedicated to World 1: Localhost (Stages 1 - 3)

        string layoutName = $"World1_ParkourLayout_S{segmentIndex + 1}";

        if (GameObject.Find(layoutName) != null) return; // Already generated

        // 1. Deactivate legacy flat straight-line ground slabs so they don't bypass parkour

        DeactivateLegacyFlatGround();

        // 2. Create clean hierarchy container

        var root = new GameObject(layoutName);

        var groundGroup = new GameObject("GroundPlatforms");

        groundGroup.transform.SetParent(root.transform, false);

        var onewayGroup = new GameObject("OneWayPlatforms");

        onewayGroup.transform.SetParent(root.transform, false);

        var movingGroup = new GameObject("MovingPlatforms");

        movingGroup.transform.SetParent(root.transform, false);

        var hazardGroup = new GameObject("Hazards");

        hazardGroup.transform.SetParent(root.transform, false);

        var enemyGroup = new GameObject("Enemies");

        enemyGroup.transform.SetParent(root.transform, false);

        var itemGroup = new GameObject("Collectibles");

        itemGroup.transform.SetParent(root.transform, false);

        int groundLayer = DetectGroundLayer();

        switch (segmentIndex)

        {

            case 0:

                BuildStage1(groundGroup.transform, onewayGroup.transform, movingGroup.transform, hazardGroup.transform, enemyGroup.transform, itemGroup.transform, groundLayer);

                break;

            case 1:

                BuildStage2(groundGroup.transform, onewayGroup.transform, movingGroup.transform, hazardGroup.transform, enemyGroup.transform, itemGroup.transform, groundLayer);

                break;

            case 2:

                BuildStage3(groundGroup.transform, onewayGroup.transform, movingGroup.transform, hazardGroup.transform, enemyGroup.transform, itemGroup.transform, groundLayer);

                break;

        }

        Physics2D.SyncTransforms();
        Debug.Log($"[StageParkourGenerator] Built dynamic parkour layout for World 1 Stage {segmentIndex + 1}.");

    }

    private static void DeactivateLegacyFlatGround()

    {

        // Deactivate old flat slabs and legacy objects in scene, preserving Player and ServerB Goal

        var lc = GameObject.Find("LevelContent");

        if (lc != null)

        {

            foreach (Transform child in lc.transform)

            {

                if (child.name.StartsWith("Player") || child.name.StartsWith("ServerB"))

                    continue;

                child.gameObject.SetActive(false);

            }

        }

        foreach (var name in new[] { "Ground_0_18", "Ground_89_18", "Ground_219_18", "Ground_202_18", "Ground_104_18", "Ground_271_18", "Ground_74_18" })

        {

            var g = GameObject.Find(name);

            if (g != null) g.SetActive(false);

        }

    }

    // =====================================================================================

    // STAGE 1: SUBNET BOOTSTRAP (x: 0 to 185m)

    // Multi-tier terraces, horizontal moving platform over spike canyon, vertical elevator,

    // observatory quiz deck, and midway TCP checkpoint.

    // =====================================================================================

    private static void BuildStage1(Transform ground, Transform oneway, Transform moving, Transform hazards, Transform enemies, Transform items, int layer)

    {

        // --- Section 1: Entrance Runway & First Ascent (x: 0 - 38) ---

        // Entrance pad connects directly to SafeChamber_Start doorway at y = -18

        CreateSolidGround(ground, "Ground_StartPad", new Vector3(11.0f, -18.0f, 0f), 14f, 2.0f, layer);

        CreateDataBit(items, new Vector3(7f, -16.2f, 0f));

        CreateDataBit(items, new Vector3(10f, -16.2f, 0f));

        CreateDataBit(items, new Vector3(13f, -16.2f, 0f));

        // Stepped one-way ledges going UP: -15.5m -> -13.5m -> -12.2m

        CreateOneWayPlatform(oneway, "Platform_Step_1", new Vector3(16f, -15.5f, 0f), 4.0f);

        CreateDataBit(items, new Vector3(16f, -14.5f, 0f));

        // Smoke Test Launch platform (x < middle.x, size >= 4.0)

        CreateOneWayPlatform(oneway, "Platform_Launch_Smoke", new Vector3(21f, -13.5f, 0f), 4.5f);

        CreateDataBit(items, new Vector3(21f, -12.5f, 0f));

        // Smoke Test Middle platform (x > 20, size == 3.0) with collectible DataBit directly above

        CreateOneWayPlatform(oneway, "Platform_Middle_Smoke", new Vector3(26.5f, -12.2f, 0f), 3.0f);

        CreateDataBit(items, new Vector3(26.5f, -11.2f, 0f));

        // Elevated Terrace 1 (+7.5m higher than base at y = -10.5m)

        CreateSolidGround(ground, "Ground_HighTerrace_1", new Vector3(33f, -10.5f, 0f), 10f, 2.0f, layer);

        CreateSnippet(items, "Snippet_HighTerrace_1", new Vector3(33f, -8.2f, 0f));

        CreateDataBit(items, new Vector3(30f, -8.7f, 0f));

        CreateDataBit(items, new Vector3(36f, -8.7f, 0f));

        // --- Section 2: First Deep Canyon & Horizontal Moving Platform (x: 38 - 72) ---

        // Deep pit underneath canyon from x=38 to 58 (depth y = -24)

        CreateSpikeHazard(hazards, "Spike_Canyon_1", new Vector3(48f, -23.0f, 0f), 16f);

        // Horizontal moving platform bridging the canyon: moves back and forth between x=41 and x=55

        var mp1 = CreateMovingPlatform(moving, "MovingPlat_Canyon_1", new Vector3(48f, -11.5f, 0f), new Vector2(-6f, 0f), new Vector2(6f, 0f), 2.4f, 4.0f);

        CreateDataBit(mp1.transform, new Vector3(48f, -10.6f, 0f));

        // Stepping ledge on far side of canyon

        CreateOneWayPlatform(oneway, "Platform_Step_3", new Vector3(58f, -13.0f, 0f), 4.2f);

        CreateDataBit(items, new Vector3(58f, -12.0f, 0f));

        // Mid Terrace

        CreateSolidGround(ground, "Ground_MidTerrace", new Vector3(67f, -14.5f, 0f), 8.5f, 2.0f, layer);

        CreateDataBit(items, new Vector3(65f, -12.7f, 0f));

        CreateDataBit(items, new Vector3(68f, -12.7f, 0f));

        // --- Section 3: Quiz Observatory & Checkpoint TCP 1 (x: 72 - 108) ---

        // Stepped ascent to upper observatory

        CreateOneWayPlatform(oneway, "Platform_Step_4", new Vector3(73f, -13.0f, 0f), 4.0f);

        CreateDataBit(items, new Vector3(73f, -12.0f, 0f));

        // Upper Observatory Deck at y = -10.5m (Software Quiz Board placed here!)

        CreateSolidGround(ground, "Ground_QuizObservatory", new Vector3(82f, -10.5f, 0f), 12f, 2.0f, layer);

        CreateDataBit(items, new Vector3(78f, -8.7f, 0f));

        CreateDataBit(items, new Vector3(86f, -8.7f, 0f));

        // Lower combat tunnel under the observatory at y = -18.0m

        CreateSolidGround(ground, "Ground_ObservatoryTrench", new Vector3(82f, -18.0f, 0f), 14f, 2.0f, layer);

        CreateEnemyPatrol(enemies, "Enemy_Patrol_Trench", new Vector3(82f, -17.0f, 0f), new Vector2(-4f, 0f), new Vector2(4f, 0f));

        CreateDataBit(items, new Vector3(80f, -16.2f, 0f));

        CreateDataBit(items, new Vector3(84f, -16.2f, 0f));

        // Stepping stone down towards Checkpoint

        CreateOneWayPlatform(oneway, "Platform_Step_5", new Vector3(92f, -12.5f, 0f), 4.0f);

        CreateDataBit(items, new Vector3(92f, -11.7f, 0f));

        // Checkpoint TCP Island (size >= 7.0 for smoke tests)

        var cpPlatform = CreateOneWayPlatform(oneway, "Platform_Checkpoint_1", new Vector3(103f, -14.5f, 0f), 9.0f);

        CreateCheckpointTCP(items, "Checkpoint_TCP_S1", new Vector3(103f, -13.5f, 0f), cpPlatform);

        CreateDataBit(items, new Vector3(100f, -13.7f, 0f));

        CreateDataBit(items, new Vector3(106f, -13.7f, 0f));

        // --- Section 4: Giant Canyon & Vertical Elevator (x: 108 - 152) ---

        // Deep Canyon with Spikes at bottom

        CreateSpikeHazard(hazards, "Spike_Canyon_2", new Vector3(123f, -23.0f, 0f), 22f);

        // Vertical Moving Platform (Elevator! Moves vertically between y = -19.5m and y = -10.5m)

        var elevator1 = CreateMovingPlatform(moving, "MovingPlat_Elevator_1", new Vector3(112f, -15.0f, 0f), new Vector2(0f, -4.5f), new Vector2(0f, 4.5f), 2.2f, 3.8f);

        CreateDataBit(elevator1.transform, new Vector3(112f, -14.1f, 0f));

        // High Sky Stepping Stones

        CreateOneWayPlatform(oneway, "Platform_Sky_1", new Vector3(121f, -10.0f, 0f), 4.0f);

        CreateDataBit(items, new Vector3(121f, -9.2f, 0f));

        CreateOneWayPlatform(oneway, "Platform_Sky_2", new Vector3(129f, -11.5f, 0f), 4.0f);

        CreateDataBit(items, new Vector3(129f, -10.7f, 0f));

        // Flying Drone guarding the canyon airspace!

        CreateEnemyFlying(enemies, "Enemy_Flying_Canyon_1", new Vector3(125f, -8.0f, 0f), 3.0f);

        // Landing Plateau on the other side

        CreateSolidGround(ground, "Ground_LandingPlateau", new Vector3(143f, -13.5f, 0f), 13f, 2.0f, layer);

        CreateEnemyPatrol(enemies, "Enemy_Patrol_Landing", new Vector3(143f, -12.5f, 0f), new Vector2(-3.5f, 0f), new Vector2(3.5f, 0f));

        CreateDataBit(items, new Vector3(140f, -11.7f, 0f));

        CreateDataBit(items, new Vector3(146f, -11.7f, 0f));

        // --- Section 5: Stepped Descent & Spike Dash into Safe Zone 2 (x: 152 - 185) ---

        CreateSolidGround(ground, "Ground_Descent_1", new Vector3(156f, -15.5f, 0f), 7.0f, 2.0f, layer);

        CreateDataBit(items, new Vector3(156f, -13.7f, 0f));

        // Spike gap between x=160 and 165 requiring jump/dash!

        CreateSpikeHazard(hazards, "Spike_FinalGap", new Vector3(163f, -19.0f, 0f), 5.5f);

        CreateOneWayPlatform(oneway, "Platform_Over_Spike", new Vector3(163f, -13.5f, 0f), 3.5f); // High bypass ledge

        CreateDataBit(items, new Vector3(163f, -12.7f, 0f));

        // Final exit runway connecting smoothly into SafeChamber_End at y = -18

        CreateSolidGround(ground, "Ground_EndPad", new Vector3(176f, -18.0f, 0f), 16f, 2.0f, layer);

        CreateDataBit(items, new Vector3(171f, -16.2f, 0f));

        CreateDataBit(items, new Vector3(174f, -16.2f, 0f));

        CreateDataBit(items, new Vector3(177f, -16.2f, 0f));

    }

    // =====================================================================================

    // STAGE 2: VERTICAL ROUTING & CIPHER BYPASS (x: 0 to 245m)

    // Tower zig-zag ascent, Split Paths (Upper Rooftop Parkour vs Lower Combat Sewer),

    // Cipher Puzzle plaza, dual counter-phase moving platforms.

    // =====================================================================================

    private static void BuildStage2(Transform ground, Transform oneway, Transform moving, Transform hazards, Transform enemies, Transform items, int layer)

    {

        // --- Section 1: Entrance & Vertical Fortress Ascent (x: 0 - 52) ---

        CreateSolidGround(ground, "Ground_S2_Start", new Vector3(10.0f, -18.0f, 0f), 12f, 2.0f, layer);

        CreateDataBit(items, new Vector3(7f, -16.2f, 0f));

        CreateDataBit(items, new Vector3(10f, -16.2f, 0f));

        // Zig-Zag ascending one-way stairs: -15m -> -12m -> -9m

        CreateOneWayPlatform(oneway, "Platform_S2_Stairs_1", new Vector3(15f, -15.0f, 0f), 4.5f);

        CreateDataBit(items, new Vector3(15f, -14.2f, 0f));

        CreateOneWayPlatform(oneway, "Platform_S2_Stairs_2", new Vector3(21f, -12.0f, 0f), 4.5f);

        CreateDataBit(items, new Vector3(21f, -11.2f, 0f));

        CreateOneWayPlatform(oneway, "Platform_S2_Stairs_3", new Vector3(27f, -9.0f, 0f), 4.5f);

        CreateDataBit(items, new Vector3(27f, -8.2f, 0f));

        // High Peak Fortress (y = -6.5m! 11.5m above baseline!)

        CreateSolidGround(ground, "Ground_HighPeak_S2", new Vector3(38f, -6.5f, 0f), 13f, 2.0f, layer);

        CreateSnippet(items, "Snippet_S2_Peak", new Vector3(38f, -4.2f, 0f));

        CreateDataBit(items, new Vector3(34f, -4.7f, 0f));

        CreateDataBit(items, new Vector3(42f, -4.7f, 0f));

        // --- Section 2: Split Path (x: 52 - 120) ---

        // === RUTE ATAS: High-Altitude Rooftop Parkour (y = -6.5m to -3.5m) ===

        var s2Sky1 = CreateMovingPlatform(moving, "MovingPlat_Sky_1", new Vector3(56f, -5.5f, 0f), new Vector2(-4.5f, 0f), new Vector2(4.5f, 0f), 2.2f, 3.8f);

        CreateDataBit(s2Sky1.transform, new Vector3(56f, -4.6f, 0f));

        CreateOneWayPlatform(oneway, "Platform_Sky_Roof_1", new Vector3(68f, -4.5f, 0f), 5.5f);

        CreateSnippet(items, "Snippet_Sky_Roof", new Vector3(68f, -2.5f, 0f));

        CreateDataBit(items, new Vector3(66f, -3.7f, 0f));

        CreateDataBit(items, new Vector3(70f, -3.7f, 0f));

        // High vertical elevator in sky: oscillates y = -7.5m to -2.5m

        var s2Elevator = CreateMovingPlatform(moving, "MovingPlat_Sky_Elevator", new Vector3(80f, -5.0f, 0f), new Vector2(0f, -2.5f), new Vector2(0f, 2.5f), 2.0f, 3.6f);

        CreateDataBit(s2Elevator.transform, new Vector3(80f, -4.1f, 0f));

        var s2Sky2 = CreateMovingPlatform(moving, "MovingPlat_Sky_2", new Vector3(94f, -5.0f, 0f), new Vector2(-4.5f, 0f), new Vector2(4.5f, 0f), 2.5f, 3.8f);

        CreateDataBit(s2Sky2.transform, new Vector3(94f, -4.1f, 0f));

        CreateOneWayPlatform(oneway, "Platform_Sky_Roof_2", new Vector3(106f, -6.5f, 0f), 5.5f);

        CreateDataBit(items, new Vector3(106f, -5.7f, 0f));

        // Aerial Drone guarding skyway

        CreateEnemyFlying(enemies, "Enemy_Flying_S2_1", new Vector3(75f, -2.5f, 0f), 3.0f);

        // === RUTE BAWAH: Lower Combat Trench (y = -18.0m) ===

        CreateSolidGround(ground, "Ground_S2_Trench_1", new Vector3(68f, -18.0f, 0f), 18f, 2.0f, layer);

        CreateEnemyPatrol(enemies, "Enemy_Patrol_Trench_1", new Vector3(68f, -17.0f, 0f), new Vector2(-5f, 0f), new Vector2(5f, 0f));

        CreateDataBit(items, new Vector3(63f, -16.2f, 0f));

        CreateDataBit(items, new Vector3(73f, -16.2f, 0f));

        CreateSpikeHazard(hazards, "Spike_S2_TrenchGap", new Vector3(84f, -21.0f, 0f), 7.0f);

        CreateSolidGround(ground, "Ground_S2_Trench_2", new Vector3(100f, -18.0f, 0f), 18f, 2.0f, layer);

        CreateEnemyPatrol(enemies, "Enemy_Patrol_Trench_2", new Vector3(100f, -17.0f, 0f), new Vector2(-5f, 0f), new Vector2(5f, 0f));

        CreateDataBit(items, new Vector3(95f, -16.2f, 0f));

        CreateDataBit(items, new Vector3(105f, -16.2f, 0f));

        // --- Section 3: Checkpoint TCP 2 & Cipher Puzzle Plaza (x: 120 - 176) ---

        // Both paths converge onto Mid Terrace

        CreateSolidGround(ground, "Ground_S2_Convergence", new Vector3(124f, -14.0f, 0f), 12f, 2.0f, layer);

        var cpPlatform2 = CreateOneWayPlatform(oneway, "Platform_Checkpoint_2", new Vector3(124f, -13.5f, 0f), 8.0f);

        CreateCheckpointTCP(items, "Checkpoint_TCP_S2", new Vector3(124f, -12.5f, 0f), cpPlatform2);

        CreateDataBit(items, new Vector3(121f, -12.7f, 0f));

        CreateDataBit(items, new Vector3(127f, -12.7f, 0f));

        CreateOneWayPlatform(oneway, "Platform_S2_Step_Mid", new Vector3(135f, -12.0f, 0f), 4.5f);

        CreateDataBit(items, new Vector3(135f, -11.2f, 0f));

        // Puzzle Plaza (Software Quiz Board & Terminal Puzzle situated here)

        CreateSolidGround(ground, "Ground_PuzzlePlaza", new Vector3(154f, -13.5f, 0f), 26f, 2.0f, layer);

        CreateDataBit(items, new Vector3(149f, -11.7f, 0f));

        CreateDataBit(items, new Vector3(160f, -11.7f, 0f));

        // Elevated Relay pylon perches (Cipher Relay puzzle)

        CreateOneWayPlatform(oneway, "Platform_Relay_0", new Vector3(147f, -10.5f, 0f), 4.0f);

        CreateDataBit(items, new Vector3(147f, -9.7f, 0f));

        CreateOneWayPlatform(oneway, "Platform_Relay_1", new Vector3(158f, -10.5f, 0f), 4.0f);

        CreateDataBit(items, new Vector3(158f, -9.7f, 0f));

        CreateSolidGround(ground, "Pillar_Relay_2", new Vector3(167f, -8.5f, 0f), 4.0f, 5.0f, layer); // High pillar

        CreateDataBit(items, new Vector3(167f, -5.2f, 0f));

        // --- Section 4: Double Chasm & Synchronized Counter-Phase Moving Platforms (x: 176 - 222) ---

        CreateSpikeHazard(hazards, "Spike_DoubleChasm_1", new Vector3(188f, -23.5f, 0f), 14f);

        CreateSpikeHazard(hazards, "Spike_DoubleChasm_2", new Vector3(208f, -23.5f, 0f), 14f);

        // Moving platform 1 (moves left while 2 moves right)

        var sync1 = CreateMovingPlatform(moving, "MovingPlat_Sync_1", new Vector3(188f, -12.5f, 0f), new Vector2(-4.5f, 0f), new Vector2(4.5f, 0f), 2.5f, 3.8f);

        CreateDataBit(sync1.transform, new Vector3(188f, -11.6f, 0f));

        // Center island between chasms

        CreateSolidGround(ground, "Ground_CenterIsland_S2", new Vector3(198f, -13.5f, 0f), 6.0f, 2.0f, layer);

        CreateDataBit(items, new Vector3(198f, -11.7f, 0f));

        // Moving platform 2

        var sync2 = CreateMovingPlatform(moving, "MovingPlat_Sync_2", new Vector3(208f, -12.5f, 0f), new Vector2(4.5f, 0f), new Vector2(-4.5f, 0f), 2.5f, 3.8f);

        CreateDataBit(sync2.transform, new Vector3(208f, -11.6f, 0f));

        CreateEnemyFlying(enemies, "Enemy_Flying_S2_2", new Vector3(198f, -9.0f, 0f), 3.0f);

        // --- Section 5: Final Stepped Descent to Safe Zone 2 (x: 222 - 245) ---

        CreateSolidGround(ground, "Ground_S2_Descent", new Vector3(224f, -15.5f, 0f), 7.0f, 2.0f, layer);

        CreateDataBit(items, new Vector3(224f, -13.7f, 0f));

        CreateSolidGround(ground, "Ground_S2_EndPad", new Vector3(238f, -18.0f, 0f), 15f, 2.0f, layer);

        CreateDataBit(items, new Vector3(234f, -16.2f, 0f));

        CreateDataBit(items, new Vector3(238f, -16.2f, 0f));

        CreateDataBit(items, new Vector3(242f, -16.2f, 0f));

    }

    // =====================================================================================

    // STAGE 3: THE LOCALHOST GAUNTLET (x: 0 to 310m)

    // Extreme verticality, triple chained moving platforms, crumbling ledges,

    // laser obstacles, and peak hermit observation deck.

    // =====================================================================================

    private static void BuildStage3(Transform ground, Transform oneway, Transform moving, Transform hazards, Transform enemies, Transform items, int layer)

    {

        // --- Section 1: Steep Multi-Tier Mountain Ramps (x: 0 - 68) ---

        CreateSolidGround(ground, "Ground_S3_Start", new Vector3(10.0f, -18.0f, 0f), 12f, 2.0f, layer);

        CreateDataBit(items, new Vector3(7f, -16.2f, 0f));

        CreateDataBit(items, new Vector3(10f, -16.2f, 0f));

        CreateOneWayPlatform(oneway, "Platform_S3_Ramp_1", new Vector3(15f, -15.0f, 0f), 4.5f);

        CreateDataBit(items, new Vector3(15f, -14.2f, 0f));

        CreateSolidGround(ground, "Ground_S3_Terrace_1", new Vector3(25f, -12.5f, 0f), 9.0f, 2.0f, layer);

        CreateDataBit(items, new Vector3(23f, -10.7f, 0f));

        CreateDataBit(items, new Vector3(27f, -10.7f, 0f));

        CreateOneWayPlatform(oneway, "Platform_S3_Ramp_2", new Vector3(35f, -10.0f, 0f), 4.5f);

        CreateDataBit(items, new Vector3(35f, -9.2f, 0f));

        CreateSolidGround(ground, "Ground_S3_Terrace_2", new Vector3(46f, -7.5f, 0f), 10.0f, 2.0f, layer);

        CreateDataBit(items, new Vector3(44f, -5.7f, 0f));

        CreateDataBit(items, new Vector3(48f, -5.7f, 0f));

        CreateEnemyPatrol(enemies, "Enemy_Patrol_S3_1", new Vector3(46f, -6.5f, 0f), new Vector2(-3.5f, 0f), new Vector2(3.5f, 0f));

        CreateEnemyFlying(enemies, "Enemy_Flying_S3_Valley", new Vector3(35f, -5.0f, 0f), 3.0f);

        // --- Section 2: High-Altitude Moving Platform Gauntlet (x: 68 - 148) ---

        CreateSpikeHazard(hazards, "Spike_S3_Abyss_1", new Vector3(105f, -24.0f, 0f), 45f);

        // Platform 1 (Horizontal fast)

        var s3Plat1 = CreateMovingPlatform(moving, "MovingPlat_Gauntlet_1", new Vector3(68f, -6.5f, 0f), new Vector2(-4f, 0f), new Vector2(4f, 0f), 2.8f, 3.6f);

        CreateDataBit(s3Plat1.transform, new Vector3(68f, -5.6f, 0f));

        // Mid sky stepping pillar

        CreateSolidGround(ground, "Pillar_Gauntlet_1", new Vector3(82f, -8.0f, 0f), 4.5f, 6.0f, layer);

        CreateDataBit(items, new Vector3(82f, -4.2f, 0f));

        // Platform 2 (Vertical elevator: y = -13m to -4m)

        var s3Elevator = CreateMovingPlatform(moving, "MovingPlat_Gauntlet_Elevator", new Vector3(95f, -8.5f, 0f), new Vector2(0f, -4.5f), new Vector2(0f, 4.5f), 2.4f, 3.6f);

        CreateDataBit(s3Elevator.transform, new Vector3(95f, -7.6f, 0f));

        // Mid sky stepping pillar 2

        CreateSolidGround(ground, "Pillar_Gauntlet_2", new Vector3(108f, -6.5f, 0f), 4.5f, 7.0f, layer);

        CreateDataBit(items, new Vector3(108f, -2.2f, 0f));

        // Platform 3 (Horizontal fast)

        var s3Plat2 = CreateMovingPlatform(moving, "MovingPlat_Gauntlet_2", new Vector3(122f, -5.5f, 0f), new Vector2(-4.5f, 0f), new Vector2(4.5f, 0f), 2.8f, 3.6f);

        CreateDataBit(s3Plat2.transform, new Vector3(122f, -4.6f, 0f));

        CreateOneWayPlatform(oneway, "Platform_Gauntlet_Step", new Vector3(135f, -7.5f, 0f), 4.5f);

        CreateDataBit(items, new Vector3(135f, -6.7f, 0f));

        CreateEnemyFlying(enemies, "Enemy_Flying_S3_Air", new Vector3(100f, -3.5f, 0f), 4.0f);

        // --- Section 3: Mid-Stage Peak, Quiz Deck & Checkpoint TCP 3 (x: 148 - 200) ---

        // Massive mountain peak plateau

        CreateSolidGround(ground, "Ground_S3_PeakPlateau", new Vector3(162f, -9.5f, 0f), 24f, 3.0f, layer);

        CreateDataBit(items, new Vector3(158f, -7.2f, 0f));

        CreateDataBit(items, new Vector3(166f, -7.2f, 0f));

        var cpPlatform3 = CreateOneWayPlatform(oneway, "Platform_Checkpoint_3", new Vector3(154f, -9.0f, 0f), 8.0f);

        CreateCheckpointTCP(items, "Checkpoint_TCP_S3", new Vector3(154f, -8.0f, 0f), cpPlatform3);

        CreateDataBit(items, new Vector3(151f, -8.2f, 0f));

        // High lookout tower (Hidden Hermit perches here)

        CreateSolidGround(ground, "Tower_HermitLookout", new Vector3(175f, -5.0f, 0f), 5.0f, 8.0f, layer);

        CreateDataBit(items, new Vector3(175f, -0.2f, 0f));

        // --- Section 4: Stepped Pits & Combat Gauntlet (x: 200 - 275) ---

        CreateSolidGround(ground, "Ground_S3_Terrace_3", new Vector3(190f, -12.0f, 0f), 8.0f, 2.0f, layer);

        CreateDataBit(items, new Vector3(188f, -10.2f, 0f));

        CreateDataBit(items, new Vector3(192f, -10.2f, 0f));

        CreateSpikeHazard(hazards, "Spike_S3_Pit_1", new Vector3(204f, -20.0f, 0f), 12f);

        var pitPlat = CreateMovingPlatform(moving, "MovingPlat_S3_Pit", new Vector3(204f, -11.0f, 0f), new Vector2(-4f, 0f), new Vector2(4f, 0f), 2.6f, 3.8f);

        CreateDataBit(pitPlat.transform, new Vector3(204f, -10.1f, 0f));

        CreateSolidGround(ground, "Ground_S3_Island_Mid", new Vector3(218f, -13.0f, 0f), 9.0f, 2.0f, layer);

        CreateEnemyPatrol(enemies, "Enemy_Patrol_S3_2", new Vector3(218f, -12.0f, 0f), new Vector2(-3f, 0f), new Vector2(3f, 0f));

        CreateDataBit(items, new Vector3(215f, -11.2f, 0f));

        CreateDataBit(items, new Vector3(221f, -11.2f, 0f));

        CreateSpikeHazard(hazards, "Spike_S3_Pit_2", new Vector3(232f, -20.0f, 0f), 10f);

        CreateOneWayPlatform(oneway, "Platform_S3_Hurdle", new Vector3(232f, -11.5f, 0f), 4.5f);

        CreateDataBit(items, new Vector3(232f, -10.7f, 0f));

        CreateSolidGround(ground, "Ground_S3_Terrace_4", new Vector3(246f, -14.5f, 0f), 11.0f, 2.0f, layer);

        CreateEnemyPatrol(enemies, "Enemy_Patrol_S3_3", new Vector3(246f, -13.5f, 0f), new Vector2(-4f, 0f), new Vector2(4f, 0f));

        CreateDataBit(items, new Vector3(242f, -12.7f, 0f));

        CreateDataBit(items, new Vector3(250f, -12.7f, 0f));

        var descentPad = CreateSolidGround(ground, "Ground_S3_Descent", new Vector3(264f, -16.5f, 0f), 10.0f, 2.0f, layer);

        CreateCheckpointTCP(items, "Checkpoint_TCP_PreBoss", new Vector3(264f, -14.3f, 0f), descentPad);

        CreateDataBit(items, new Vector3(262f, -14.7f, 0f));

        CreateDataBit(items, new Vector3(266f, -14.7f, 0f));

        // --- Section 5: Pre-Arena Gauntlet Step & Locked Boss Arena (x: 271 - 310) ---

        CreateSpikeHazard(hazards, "Spike_S3_ArenaApproachPit", new Vector3(272.5f, -20.0f, 0f), 7.5f);

        CreateOneWayPlatform(oneway, "Platform_S3_ArenaApproach", new Vector3(272.5f, -15.2f, 0f), 2.8f);

        CreateSolidGround(ground, "Ground_S3_EndPad", new Vector3(288.5f, -18.0f, 0f), 25f, 2.0f, layer);

        // Tactical one-way ledges inside Arena for dodging Jump Slam shockwaves
        CreateOneWayPlatform(oneway, "Platform_Arena_Left", new Vector3(282.5f, -14.1f, 0f), 4.2f);

        CreateOneWayPlatform(oneway, "Platform_Arena_Right", new Vector3(294.5f, -14.1f, 0f), 4.2f);

        CreateDataBit(items, new Vector3(272.5f, -14.3f, 0f));

        CreateDataBit(items, new Vector3(282.5f, -13.3f, 0f));

        CreateDataBit(items, new Vector3(294.5f, -13.3f, 0f));

        CreateDataBit(items, new Vector3(298f, -16.2f, 0f));

    }

    // =====================================================================================

    // HELPER BUILDERS: PRECISE PIXEL ART PLATFORMS, ONE-WAY, MOVING & HAZARDS

    // =====================================================================================

    public static GameObject CreateSolidGround(Transform parent, string name, Vector3 center, float width, float thick, int layer)
    {
        float topY = center.y + thick * 0.5f;

        // Ground_QuizObservatory in Section 3 is an elevated deck with a combat tunnel running underneath
        if (name == "Ground_QuizObservatory")
        {
            var deck = new GameObject(name);
            deck.transform.SetParent(parent, false);
            deck.transform.position = center;
            deck.layer = layer;

            var dBox = deck.AddComponent<BoxCollider2D>();
            dBox.size = new Vector2(width, thick);

            var dSr = deck.AddComponent<SpriteRenderer>();
            var deckTop = WorldThemeApplier.GetGroundSprite(0, false) ?? Resources.Load<Sprite>("GeneratedArt/ground_w1");
            var deckSub = WorldThemeApplier.GetGroundSprite(0, true) ?? Resources.Load<Sprite>("GeneratedArt/ground_w1_sub");
            dSr.sprite = deckTop;
            dSr.drawMode = SpriteDrawMode.Tiled;
            dSr.tileMode = SpriteTileMode.Continuous;
            dSr.size = new Vector2(width, thick);
            dSr.sortingOrder = 5;
            WorldThemeApplier.ApplySegmentedGround(dSr, deckTop, deckSub, 1.0f);

            // Sturdy architectural support pillars connecting the deck down to the trench floor (y = -17)
            float pillarTop = center.y - thick * 0.5f; // -11.5f
            float pillarBottom = -17.0f;
            float pillarH = pillarTop - pillarBottom; // 5.5f
            float pillarCenterY = pillarBottom + pillarH * 0.5f; // -14.25f

            CreatePillarSupport(parent, "Observatory_Support_Left", new Vector3(center.x - width * 0.5f + 0.6f, pillarCenterY, 0f), 1.2f, pillarH, layer);
            CreatePillarSupport(parent, "Observatory_Support_Right", new Vector3(center.x + width * 0.5f - 0.6f, pillarCenterY, 0f), 1.2f, pillarH, layer);

            return deck;
        }

        // Mario-style Ground: solid terrain foundation pulled all the way down into deep bedrock (-30)
        float groundBottom = -30.0f;
        float totalHeight = Mathf.Max(thick, topY - groundBottom);
        float newCenterY = topY - totalHeight * 0.5f;

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(center.x, newCenterY, center.z);
        go.layer = layer;

        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(width, totalHeight);

        var sr = go.AddComponent<SpriteRenderer>();
        var groundTop = WorldThemeApplier.GetGroundSprite(0, false) ?? Resources.Load<Sprite>("GeneratedArt/ground_w1");
        var groundSub = WorldThemeApplier.GetGroundSprite(0, true) ?? Resources.Load<Sprite>("GeneratedArt/ground_w1_sub");
        sr.sprite = groundTop;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = new Vector2(width, totalHeight);
        sr.sortingOrder = 5;
        WorldThemeApplier.ApplySegmentedGround(sr, groundTop, groundSub, 1.0f);

        return go;
    }

    private static GameObject CreatePillarSupport(Transform parent, string name, Vector3 center, float width, float height, int layer)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        go.layer = layer;

        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(width, height);

        var sr = go.AddComponent<SpriteRenderer>();
        var supportSub = WorldThemeApplier.GetGroundSprite(0, true) ?? Resources.Load<Sprite>("GeneratedArt/ground_w1_sub");
        sr.sprite = supportSub != null ? supportSub : Resources.Load<Sprite>("GeneratedArt/ground_w1");
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = new Vector2(width, height);
        sr.sortingOrder = 4;

        return go;
    }

    public static GameObject CreateOneWayPlatform(Transform parent, string name, Vector3 center, float width)

    {

        var go = new GameObject(name);

        go.transform.SetParent(parent, false);

        go.transform.position = center;

        go.layer = 0; // Default layer

        var col = go.AddComponent<BoxCollider2D>();

        col.size = new Vector2(width, 0.45f);

        col.usedByEffector = true;

        var effector = go.AddComponent<PlatformEffector2D>();

        effector.useOneWay = true;

        effector.useOneWayGrouping = true;

        effector.surfaceArc = 180f;

        var sr = go.AddComponent<SpriteRenderer>();

        var s = Resources.Load<Sprite>("GeneratedArt/platform_oneway_w1");

        if (s == null) s = Resources.Load<Sprite>("GeneratedArt/platform_oneway");

        sr.sprite = s;

        sr.drawMode = SpriteDrawMode.Sliced;

        sr.size = new Vector2(width, 0.45f);

        sr.sortingOrder = 6;

        return go;

    }

    public static GameObject CreateMovingPlatform(Transform parent, string name, Vector3 startPos, Vector2 pointA, Vector2 pointB, float speed, float width)

    {

        var go = new GameObject(name);

        go.transform.SetParent(parent, false);

        go.transform.position = startPos;

        go.layer = 0;

        var rb = go.AddComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;

        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = go.AddComponent<BoxCollider2D>();

        col.size = new Vector2(width, 0.6f);

        var sr = go.AddComponent<SpriteRenderer>();

        var s = Resources.Load<Sprite>("GeneratedArt/platform_moving_w1");

        if (s == null) s = Resources.Load<Sprite>("GeneratedArt/platform_moving");

        sr.sprite = s;

        sr.drawMode = SpriteDrawMode.Sliced;

        sr.size = new Vector2(width, 0.6f);

        sr.sortingOrder = 6;

        var mp = go.AddComponent<Platform_Moving>();

        mp.pointA = pointA;

        mp.pointB = pointB;

        mp.speed = speed;

        return go;

    }

    public static GameObject CreateSpikeHazard(Transform parent, string name, Vector3 center, float width)

    {

        var go = new GameObject(name);

        go.transform.SetParent(parent, false);

        go.transform.position = center;

        var col = go.AddComponent<BoxCollider2D>();

        col.isTrigger = true;

        col.size = new Vector2(width, 0.8f);

        var sr = go.AddComponent<SpriteRenderer>();

        var s = Resources.Load<Sprite>("GeneratedArt/hazard_spikes_w1");

        if (s == null) s = Resources.Load<Sprite>("GeneratedArt/hazard_spikes");

        sr.sprite = s;

        sr.drawMode = SpriteDrawMode.Tiled;

        sr.size = new Vector2(width, 0.8f);

        sr.sortingOrder = 7;

        var spike = go.AddComponent<Hazard_Spike>();

        spike.ttlDamage = 35f;

        spike.damageType = PlayerHealth.DamageType.TTLDamage;

        return go;

    }

    public static GameObject CreateEnemyPatrol(Transform parent, string name, Vector3 pos, Vector2 pointA, Vector2 pointB)

    {

        var go = new GameObject(name, typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer), typeof(Enemy_Patrol));

        go.transform.SetParent(parent, false);

        go.transform.position = pos;

        var ep = go.GetComponent<Enemy_Patrol>();

        ep.pointA = pointA;

        ep.pointB = pointB;

        ep.patrolSpeed = 2.2f;

        return go;

    }

    public static GameObject CreateEnemyFlying(Transform parent, string name, Vector3 pos, float radius)

    {

        var go = new GameObject(name, typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer), typeof(Enemy_Flying));

        go.transform.SetParent(parent, false);

        go.transform.position = pos;

        var ef = go.GetComponent<Enemy_Flying>();

        ef.patrolRadius = radius;

        ef.patrolSpeed = 1.6f;

        return go;

    }

    public static void CreateCheckpointTCP(Transform parent, string name, Vector3 pos, GameObject supportPlatform)

    {

        if (GameObject.Find(name) != null) return;

        var go = new GameObject(name, typeof(BoxCollider2D), typeof(Checkpoint_TCP));

        go.transform.SetParent(parent, false);

        go.transform.position = pos;

        var col = go.GetComponent<BoxCollider2D>();

        col.isTrigger = true;

        col.size = new Vector2(1.5f, 2.5f);

    }

    public static GameObject CreateDataBit(Transform parent, Vector3 pos, int amount = 10)

    {

        var go = new GameObject($"DataBit_{pos.x:F0}_{pos.y:F0}", typeof(CircleCollider2D), typeof(SpriteRenderer), typeof(Collectible_DataBit));

        go.transform.position = pos;

        if (parent != null)

        {

            go.transform.SetParent(parent, true);

            // If parent is a moving platform, elevate cleanly above the deck (+0.95m) so it hovers in air

            if (parent.GetComponent<Platform_Moving>() != null)

            {

                go.transform.localPosition = new Vector3(0f, 0.95f, 0f);

            }

        }

        var col = go.GetComponent<CircleCollider2D>();

        col.isTrigger = true;

        col.radius = 0.5f;

        var sr = go.GetComponent<SpriteRenderer>();

        if (sr != null)

        {

            var s = Resources.Load<Sprite>("GeneratedArt/databit");

            if (s != null) sr.sprite = s;

            sr.sortingOrder = 10;

        }

        var bit = go.GetComponent<Collectible_DataBit>();

        bit.amount = amount;

        bit.InitializePosition(go.transform.position, go.transform.localPosition);

        return go;

    }

    public static void CreateDataBitArc(Transform parent, Vector2 start, Vector2 end, float archHeight, int count)

    {

        for (int i = 0; i < count; i++)

        {

            float t = (i + 1f) / (count + 1f);

            float x = Mathf.Lerp(start.x, end.x, t);

            float y = Mathf.Lerp(start.y, end.y, t) + Mathf.Sin(t * Mathf.PI) * archHeight;

            var go = new GameObject($"DataBit_{x:F0}_{y:F0}", typeof(CircleCollider2D), typeof(SpriteRenderer), typeof(Collectible_DataBit));

            go.transform.position = new Vector3(x, y, 0f);

            if (parent != null)

                go.transform.SetParent(parent, true);

            var col = go.GetComponent<CircleCollider2D>();

            col.isTrigger = true;

            col.radius = 0.5f;

            var sr = go.GetComponent<SpriteRenderer>();

            if (sr != null)

            {

                var s = Resources.Load<Sprite>("GeneratedArt/databit");

                if (s != null) sr.sprite = s;

                sr.sortingOrder = 10;

            }

            var bit = go.GetComponent<Collectible_DataBit>();

            bit.amount = 10;

            bit.InitializePosition(go.transform.position, go.transform.localPosition);

        }

    }

    public static void CreateSnippet(Transform parent, string name, Vector3 pos)

    {

        var go = new GameObject(name, typeof(CircleCollider2D), typeof(SpriteRenderer), typeof(Collectible_DataBit));

        go.transform.position = pos;

        if (parent != null)

            go.transform.SetParent(parent, true);

        var col = go.GetComponent<CircleCollider2D>();

        col.isTrigger = true;

        col.radius = 0.7f;

        var sr = go.GetComponent<SpriteRenderer>();

        var s = Resources.Load<Sprite>("GeneratedArt/snippet");

        if (s != null) sr.sprite = s;

        sr.sortingOrder = 12;

        var bit = go.GetComponent<Collectible_DataBit>();

        bit.amount = 50;

        bit.isSecret = true;

        bit.InitializePosition(go.transform.position, go.transform.localPosition);

    }

    private static int DetectGroundLayer()

    {

        int layer = LayerMask.NameToLayer("Ground");

        if (layer < 0) layer = 0;

        return layer;

    }

}


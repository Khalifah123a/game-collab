using UnityEngine;

/// <summary>
/// Dedicated Initial Tutorial World ("WORLD 00 // SIMULASI TUTORIAL").
/// Runs BEFORE EnsureSafeZones() to clear ALL World 1 Stage 1 geometry/props so there are
/// ZERO overlapping platforms, walls, or hazards, then builds a crisp, generously spaced
/// 64-meter 3-Zone Cyber Boot Camp between SafeChamber_Start (x = -24..0) and SafeChamber_End (x = 64..88):
///   - Start Safe Zone (x = -24..0): Opening Prologue + Live Player Animation + PackageGiverNPC + Tech Shop/Overclock
///   - Zone 01 (x = 7..19): Double Jump stepped platforms + DataBits arc
///   - Zone 02 (x = 22..41): Moving Platform (horizontal + vertical) + Double Jump upper deck
///   - Zone 03 (x = 45..59): Phase Dash & Cyber Blade against a genuine Enemy_Patrol monster (GeneratedArt/enemy)
///   - End Safe Zone (x = 64..88): PackageReceiverNPC + Graduation Portal -> launches World 1: Localhost.
/// </summary>
public static class TutorialWorldBuilder
{
    public static bool IsTutorialWorldActive { get; set; }
    public static bool HasGraduatedTutorialWorld { get; set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticFlags()
    {
        IsTutorialWorldActive = false;
        HasGraduatedTutorialWorld = false;
    }

    public static void LaunchTutorialWorld()
    {
        IsTutorialWorldActive = true;
        CyberOpeningAndTutorialUI.HasCompletedOnboardingThisSession = false;
        CyberOpeningAndTutorialUI.ArmForNextWorld1Entry();
        GameManager.Ensure().StartRun(0);
    }

    public static void GraduateToWorld1()
    {
        IsTutorialWorldActive = false;
        HasGraduatedTutorialWorld = true;
        CyberOpeningAndTutorialUI.HasCompletedOnboardingThisSession = true;
        GameManager.Ensure().StartRun(0);
    }

    /// <summary>
    /// Called inside LevelManager.EnsureSafeZones() INSTEAD of StageParkourGenerator.BuildParkourStage().
    /// Completely wipes World 1 Stage 1 static geometry so SafeChamber_Start (-24..0),
    /// Ground_StartPad (0..64), and SafeChamber_End (64..88) sit flush with ZERO collisions/overlaps.
    /// </summary>
    public static void PrepareCleanTutorialCanvas(LevelManager lm)
    {
        // 1. Rescue essential scene actors before wiping LevelContent
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null && player.transform.parent != null)
            player.transform.SetParent(null, true);

        var goal = Object.FindAnyObjectByType<ServerB_Goal>();
        if (goal != null && goal.transform.parent != null)
            goal.transform.SetParent(null, true);

        if (lm.playerSpawn != null && lm.playerSpawn.parent != null)
            lm.playerSpawn.SetParent(null, true);

        if (lm.serverBPoint != null && lm.serverBPoint.parent != null)
            lm.serverBPoint.SetParent(null, true);

        // 2. Wipe EVERY World 1 Stage 1 level object (grounds, platforms, spikes, databits, enemies, checkpoints, snippets)
        var levelContent = GameObject.Find("LevelContent");
        if (levelContent != null)
        {
            for (int i = levelContent.transform.childCount - 1; i >= 0; i--)
            {
                var child = levelContent.transform.GetChild(i).gameObject;
                if (child == null) continue;
                if (player != null && child == player.gameObject) continue;
                if (goal != null && child == goal.gameObject) continue;
                child.SetActive(false);
                Object.Destroy(child);
            }
        }

        var flyingEnc = GameObject.Find("FlyingEncounters");
        if (flyingEnc != null)
        {
            flyingEnc.SetActive(false);
            Object.Destroy(flyingEnc);
        }

        // Sweep any stray root enemies/hazards/collectibles/platforms from scene
        foreach (var e in Object.FindObjectsByType<Enemy_Patrol>()) if (e != null) { e.gameObject.SetActive(false); Object.Destroy(e.gameObject); }
        foreach (var f in Object.FindObjectsByType<Enemy_Flying>()) if (f != null) { f.gameObject.SetActive(false); Object.Destroy(f.gameObject); }
        foreach (var s in Object.FindObjectsByType<Hazard_Spike>()) if (s != null) { s.gameObject.SetActive(false); Object.Destroy(s.gameObject); }
        foreach (var d in Object.FindObjectsByType<Collectible_DataBit>()) if (d != null) { d.gameObject.SetActive(false); Object.Destroy(d.gameObject); }
        foreach (var sn in Object.FindObjectsByType<Collectible_Snippet>()) if (sn != null) { sn.gameObject.SetActive(false); Object.Destroy(sn.gameObject); }
        foreach (var cp in Object.FindObjectsByType<Checkpoint_TCP>()) if (cp != null) { cp.gameObject.SetActive(false); Object.Destroy(cp.gameObject); }
        foreach (var mp in Object.FindObjectsByType<Platform_Moving>()) if (mp != null) { mp.gameObject.SetActive(false); Object.Destroy(mp.gameObject); }

        // 3. Position Start Doorway at x = 0.0 and End Doorway at x = 64.0 (goalPos.x - 1.5 = 64.0 -> goalPos.x = 65.5)
        if (lm.playerSpawn == null)
        {
            var spGo = new GameObject("Tutorial_PlayerSpawn");
            lm.playerSpawn = spGo.transform;
        }
        lm.playerSpawn.position = new Vector3(0.0f, -17.0f, 0f);

        if (lm.serverBPoint == null)
        {
            var sbGo = new GameObject("Tutorial_ServerBPoint");
            lm.serverBPoint = sbGo.transform;
        }
        lm.serverBPoint.position = new Vector3(65.5f, -17.0f, 0f);

        if (goal != null)
        {
            goal.transform.position = new Vector3(65.5f, -17.0f, 0f);
        }

        // 4. Build ONE flush, continuous Ground Floor from x = 0.0 to x = 64.0 (top surface at y = -17.0f)
        // Named "Ground_StartPad" so EnsureSafeZones() locks floorSurfaceY = -17.0f and ExtendAllGroundsToBedrock() fills underneath cleanly.
        Transform floorParent = levelContent != null ? levelContent.transform : null;
        var floorGo = new GameObject("Ground_StartPad");
        if (floorParent != null) floorGo.transform.SetParent(floorParent, false);
        floorGo.transform.position = new Vector3(32.0f, -18.0f, 0f);
        floorGo.layer = 0;

        var floorCol = floorGo.AddComponent<BoxCollider2D>();
        floorCol.size = new Vector2(64.4f, 2.0f);

        var floorSr = floorGo.AddComponent<SpriteRenderer>();
        floorSr.sprite = Resources.Load<Sprite>("GeneratedArt/ground_w1") ?? Resources.Load<Sprite>("GeneratedArt/ground");
        floorSr.drawMode = SpriteDrawMode.Tiled;
        floorSr.size = new Vector2(64.4f, 2.0f);
        floorSr.sortingOrder = 2;
    }

    /// <summary>
    /// Called inside LevelManager.Start() AFTER SafeChambers, Bedrock, Theme, and Quest NPCs are placed.
    /// Builds the 3 generously-spaced, zero-overlap outdoor training stations inside x = 0..64.
    /// </summary>
    public static void BuildCleanTutorialCourse(LevelManager lm)
    {
        // 1. Grant 150 DataBits training allowance if player starts at 0 so they can test Shop / Overclock upgrades immediately
        var gm = GameManager.Instance;
        if (gm != null && gm.AvailableDataBits < 150)
        {
            gm.AddDataBits(150 - gm.AvailableDataBits);
        }

        var health = PlayerHealth.Instance != null ? PlayerHealth.Instance : Object.FindAnyObjectByType<PlayerHealth>();
        if (health != null)
        {
            health.ttlMax = Mathf.Max(health.ttlMax, 180f);
            health.ttlCurrent = health.ttlMax;
        }

        var root = new GameObject("TutorialWorld00_Course").transform;

        // ---------------------------------------------------------------------
        // ZONA 01 (x = 7..19): LOMPAT & DOUBLE JUMP
        // ---------------------------------------------------------------------
        CreateFloatingHoloLabel(
            root,
            new Vector3(12.5f, -9.6f, 0f),
            "ZONA 01 // DOUBLE JUMP",
            "Tekan [SPACE] 2x di udara untuk melompat ke platform atas",
            new Color(0.22f, 0.98f, 0.65f, 1f));

        CreateTrainingPlatform(root, "TutPlat_Z1_Low", new Vector2(9.5f, -15.2f), new Vector2(3.6f, 0.55f));
        CreateTrainingPlatform(root, "TutPlat_Z1_High", new Vector2(15.5f, -13.0f), new Vector2(4.2f, 0.55f));
        SpawnTrainingDataBit(root, new Vector2(9.5f, -14.1f));
        SpawnTrainingDataBit(root, new Vector2(12.5f, -12.3f));
        SpawnTrainingDataBit(root, new Vector2(15.5f, -11.9f));

        // ---------------------------------------------------------------------
        // ZONA 02 (x = 22..41): PLATFORM BERGERAK + DOUBLE JUMP
        // ---------------------------------------------------------------------
        CreateFloatingHoloLabel(
            root,
            new Vector3(32.0f, -8.8f, 0f),
            "ZONA 02 // PLATFORM BERGERAK",
            "Naiki platform bergerak lalu tekan [SPACE 2x] ke dek atas",
            new Color(0f, 0.94f, 1f, 1f));

        CreateMovingTrainingPlatform(
            root,
            "TutPlat_Z2_Moving",
            new Vector2(28.0f, -14.7f),
            new Vector2(3.8f, 0.55f),
            new Vector2(-2.6f, -0.5f),
            new Vector2(2.6f, 0.9f),
            2.0f);

        CreateTrainingPlatform(root, "TutPlat_Z2_UpperDeck", new Vector2(36.5f, -12.2f), new Vector2(4.4f, 0.55f));
        SpawnTrainingDataBit(root, new Vector2(35.3f, -11.1f));
        SpawnTrainingDataBit(root, new Vector2(36.5f, -11.1f));
        SpawnTrainingDataBit(root, new Vector2(37.7f, -11.1f));

        // ---------------------------------------------------------------------
        // ZONA 03 (x = 45..59): PHASE DASH & CYBER BLADE COMBAT
        // ---------------------------------------------------------------------
        CreateFloatingHoloLabel(
            root,
            new Vector3(52.5f, -11.4f, 0f),
            "ZONA 03 // PHASE DASH & COMBAT",
            "[LEFT SHIFT] Dash (Kebal)  |  [KLIK KIRI / J] Tebas Musuh  |  [Q] Parry",
            new Color(1f, 0.72f, 0.22f, 1f));

        SpawnGenuinePatrolMonster(root, new Vector2(52.5f, -16.4f));

        // ---------------------------------------------------------------------
        // ZONA 04 (Right outside SafeChamber_End entrance at x = 64): GRADUATION GUIDE
        // ---------------------------------------------------------------------
        CreateFloatingHoloLabel(
            root,
            new Vector3(64.5f, -10.2f, 0f),
            "ZONA 04 // KELULUSAN -> WORLD 1",
            "Masuk Safe Zone -> Serahkan Paket [E] -> Portal Server B",
            new Color(0.22f, 0.98f, 0.65f, 1f));
    }

    private static GameObject CreateTrainingPlatform(Transform parent, string name, Vector2 center, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        go.layer = 0;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = size;
        col.usedByEffector = true;

        var eff = go.AddComponent<PlatformEffector2D>();
        eff.useOneWay = true;
        eff.surfaceArc = 160f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("GeneratedArt/platform_oneway_w1") ?? Resources.Load<Sprite>("GeneratedArt/platform_oneway");
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = size;
        sr.sortingOrder = 4;
        return go;
    }

    private static GameObject CreateMovingTrainingPlatform(
        Transform parent,
        string name,
        Vector2 center,
        Vector2 size,
        Vector2 pointA,
        Vector2 pointB,
        float speed)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        go.layer = 0;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = size;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("GeneratedArt/platform_moving_w1") ?? Resources.Load<Sprite>("GeneratedArt/platform_moving");
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = size;
        sr.sortingOrder = 4;

        var mover = go.AddComponent<Platform_Moving>();
        mover.pointA = pointA;
        mover.pointB = pointB;
        mover.speed = speed;
        return go;
    }

    private static void SpawnTrainingDataBit(Transform parent, Vector2 pos)
    {
        var go = new GameObject("Tutorial_DataBit", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(Collectible_DataBit));
        go.transform.SetParent(parent, false);
        go.transform.position = pos;

        var col = go.GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.45f;

        var sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("GeneratedArt/databit");
        sr.color = Color.white;
        sr.sortingOrder = 6;
    }

    private static void SpawnGenuinePatrolMonster(Transform parent, Vector2 pos)
    {
        // Use the genuine Enemy_Patrol monster sprite ("GeneratedArt/enemy"), NEVER the death screen icon!
        var go = new GameObject("Tutorial_PatrolBot");
        go.transform.SetParent(parent, false);
        go.transform.position = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("GeneratedArt/enemy");
        sr.color = Color.white;
        sr.sortingOrder = 10;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.85f, 0.85f);
        col.isTrigger = true;

        var enemy = go.AddComponent<Enemy_Patrol>();
        enemy.SetHome(pos);
        enemy.pointA = new Vector2(-3.2f, 0f);
        enemy.pointB = new Vector2(3.2f, 0f);
        enemy.patrolSpeed = 1.4f;
        enemy.chaseSpeed = 2.4f;
        enemy.detectionRadius = 6.5f;
        enemy.chaseRadius = 9.0f;
        enemy.damageAmount = 5f;
        enemy.hitPoints = 2;
        enemy.SnapToGroundSurface();
    }

    private static void CreateFloatingHoloLabel(Transform parent, Vector3 pos, string title, string subtitle, Color accent)
    {
        // Clean floating sky hologram text (NO oversized solid sprite box that could clash with platforms!)
        var root = new GameObject("HoloLabel_" + title);
        root.transform.SetParent(parent, false);
        root.transform.position = pos;

        var titleGo = new GameObject("TitleText");
        titleGo.transform.SetParent(root.transform, false);
        titleGo.transform.localPosition = new Vector3(0f, 0.34f, 0f);
        var tmTitle = titleGo.AddComponent<TextMesh>();
        tmTitle.font = PixelUIKit.GetDisplayFont();
        tmTitle.fontSize = 36;
        tmTitle.characterSize = 0.068f;
        tmTitle.anchor = TextAnchor.MiddleCenter;
        tmTitle.alignment = TextAlignment.Center;
        tmTitle.color = accent;
        tmTitle.text = title;
        var mr1 = titleGo.GetComponent<MeshRenderer>();
        if (mr1 != null)
        {
            if (tmTitle.font != null && tmTitle.font.material != null)
                mr1.sharedMaterial = tmTitle.font.material;
            mr1.sortingOrder = 15;
        }

        var subGo = new GameObject("SubText");
        subGo.transform.SetParent(root.transform, false);
        subGo.transform.localPosition = new Vector3(0f, -0.22f, 0f);
        var tmSub = subGo.AddComponent<TextMesh>();
        tmSub.font = PixelUIKit.GetMonoFont();
        tmSub.fontSize = 26;
        tmSub.characterSize = 0.052f;
        tmSub.anchor = TextAnchor.MiddleCenter;
        tmSub.alignment = TextAlignment.Center;
        tmSub.color = new Color(0.90f, 0.97f, 1f, 0.95f);
        tmSub.text = subtitle;
        var mr2 = subGo.GetComponent<MeshRenderer>();
        if (mr2 != null)
        {
            if (tmSub.font != null && tmSub.font.material != null)
                mr2.sharedMaterial = tmSub.font.material;
            mr2.sortingOrder = 15;
        }
    }
}

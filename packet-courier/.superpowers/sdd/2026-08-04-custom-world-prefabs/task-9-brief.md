### Task 9: Generator — prefab player, ground, platform, spike, enemy, laser

**Files:**
- Modify: `Assets/_Scripts/Editor/EditorGenerator.cs` (tambahkan `using UnityEditor.Animations;`, `using UnityEngine.SceneManagement;` bila belum ada)

**Interfaces:**
- Consumes: helper Task 8, `PlayerAnimator`, `Enemy_Patrol`, `Laser_Beam`, `Platform_Moving`, `Hazard_Spike` (semua sudah ada).
- Produces: prefab `Player`, `Ground`, `Platform_OneWay`, `Spike`, `Enemy_Patrol`, `Laser_Beam`, `Platform_Moving` di `Assets/_Prefabs/`.

- [ ] **Step 1: Buat AnimatorController player**

Di dalam class (sebelum `GeneratePrefabs` isi akhir), tambahkan method:

```csharp
    private static void CreatePlayerAnimatorController()
    {
        string path = PrefabRoot + "/PlayerAnimator.controller";
        if (AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(path) == null)
        {
            var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(path);
            UnityEditor.Animations.AnimatorControllerLayer layer = controller.layers[0];

            controller.AddParameter("isMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("speedMultiplier", AnimatorControllerParameterType.Float);
            controller.AddParameter("facing", AnimatorControllerParameterType.Float);

            AnimationClip idleClip = new AnimationClip { frameRate = 10f, name = "PlayerIdle" };
            SetSpriteCurve(idleClip, Sprite[] { LoadWalkSprite(0) ?? LoadOrCreateSprite("player_placeholder", 16, 16, Color.cyan, 32f) });
            SetLoop(idleClip);

            Sprite[] walkSprites = new Sprite[5];
            for (int i = 0; i < 5; i++)
                walkSprites[i] = LoadWalkSprite(i) ?? LoadOrCreateSprite("player_placeholder", 16, 16, Color.cyan, 32f);
            AnimationClip runClip = new AnimationClip { frameRate = 10f, name = "PlayerRun" };
            SetSpriteCurve(runClip, walkSprites);
            SetLoop(runClip);

            var idle = layer.stateMachine.AddState("Idle");
            idle.motion = idleClip;
            var run = layer.stateMachine.AddState("Run");
            run.motion = runClip;

            var idleToRun = idle.AddTransition(run);
            idleToRun.hasExitTime = false;
            idleToRun.duration = 0f;
            idleToRun.AddCondition(AnimatorConditionMode.If, 0f, "isMoving");

            var runToIdle = run.AddTransition(idle);
            runToIdle.hasExitTime = false;
            runToIdle.duration = 0f;
            runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "isMoving");

            AssetDatabase.SaveAssets();
        }
    }

    private static void SetSpriteCurve(AnimationClip clip, params Sprite[] frames)
    {
        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe { time = i / clip.frameRate, value = frames[i] };
        }
        keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / clip.frameRate, value = frames[0] };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
    }

    private static void SetLoop(AnimationClip clip)
    {
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
    }
```

- [ ] **Step 2: Tambah method pembuat prefab player + ground + one-way**

```csharp
    private static void CreatePlayerPrefab()
    {
        string path = PrefabRoot + "/Player.prefab";
        GameObject go = new GameObject("Player");
        go.tag = "Player";
        go.layer = 0;

        go.transform.localScale = new Vector3(1f, 1f, 1f);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.5f, 1f);

        go.AddComponent<PlayerHealth>();
        go.AddComponent<PlayerMode>();
        go.AddComponent<PlayerController>();
        go.AddComponent<PlayerAnimator>();

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadWalkSprite(0) ?? LoadOrCreateSprite("player_placeholder", 16, 16, Color.cyan, 32f);
        sr.sortingOrder = 10;

        var anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(
            PrefabRoot + "/PlayerAnimator.controller");

        SavePrefab(go, path);
    }

    private static void CreateGroundPrefab()
    {
        string path = PrefabRoot + "/Ground.prefab";
        GameObject go = new GameObject("Ground");
        go.layer = 0;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadOrCreateSprite("ground", 1, 1, new Color(0.35f, 0.35f, 0.55f), 16f);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(1f, 1f);
        sr.sortingOrder = 0;

        SavePrefab(go, path);
    }

    private static void CreatePlatformOneWayPrefab()
    {
        string path = PrefabRoot + "/Platform_OneWay.prefab";
        GameObject go = new GameObject("Platform_OneWay");
        go.layer = 0;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 0.5f);
        col.usedByEffector = true;

        var eff = go.AddComponent<PlatformEffector2D>();
        eff.useOneWay = true;
        eff.surfaceArc = 160f;
        eff.useColliderMask = false;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadOrCreateSprite("platform_oneway", 1, 1, new Color(0.3f, 0.6f, 1f), 16f);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(1f, 0.5f);
        sr.sortingOrder = 5;

        SavePrefab(go, path);
    }
```

- [ ] **Step 3: Tambah method pembuat prefab spike + enemy + laser + platform moving**

```csharp
    private static void CreateSpikePrefab()
    {
        string path = PrefabRoot + "/Spike.prefab";
        GameObject go = new GameObject("Spike");
        go.layer = 0;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.7f, 0.7f);
        col.isTrigger = true;

        go.AddComponent<Hazard_Spike>();

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadOrCreateSprite("spike", 7, 7, new Color(0.9f, 0.25f, 0.25f), 32f);
        sr.sortingOrder = 4;

        SavePrefab(go, path);
    }

    private static void CreateEnemyPrefab()
    {
        string path = PrefabRoot + "/Enemy_Patrol.prefab";
        GameObject go = new GameObject("Enemy_Patrol");
        go.layer = 0;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 0.8f);

        go.AddComponent<Enemy_Patrol>();

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadOrCreateSprite("enemy", 8, 8, Color.red, 32f);
        sr.sortingOrder = 6;

        SavePrefab(go, path);
    }

    private static void CreateLaserPrefab()
    {
        string path = PrefabRoot + "/Laser_Beam.prefab";
        GameObject go = new GameObject("Laser_Beam");
        go.layer = 0;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.3f, 3f);
        col.isTrigger = true;

        go.AddComponent<Laser_Beam>();

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadOrCreateSprite("laser", 3, 30, new Color(1f, 0.2f, 0.2f), 32f);
        sr.sortingOrder = 4;

        SavePrefab(go, path);
    }

    private static void CreateMovingPlatformPrefab()
    {
        string path = PrefabRoot + "/Platform_Moving.prefab";
        GameObject go = new GameObject("Platform_Moving");
        go.layer = 0;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2f, 0.5f);

        go.AddComponent<Platform_Moving>();

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadOrCreateSprite("platform_moving", 2, 1, new Color(0.2f, 0.9f, 0.3f), 32f);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(2f, 0.5f);
        sr.sortingOrder = 5;

        SavePrefab(go, path);
    }
```

- [ ] **Step 4: Helper SavePrefab**

```csharp
    private static void SavePrefab(GameObject go, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }
```

- [ ] **Step 5: Panggil method di GeneratePrefabs**

Di `GeneratePrefabs()` (sebelum penutup), tambahkan:

```csharp
        CreatePlayerAnimatorController();
        CreatePlayerPrefab();
        CreateGroundPrefab();
        CreatePlatformOneWayPrefab();
        CreateSpikePrefab();
        CreateEnemyPrefab();
        CreateLaserPrefab();
        CreateMovingPlatformPrefab();

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog(
            "Packet Courier",
            "Prefab inti selesai dibuat di Assets/_Prefabs/.\nSilakan klik Generate lagi setelah prefab lainnya tersedia.",
            "OK"
        );
```

- [ ] **Step 6: Verify compile + generate**

1. Buka Unity → Console: tidak ada error.
2. Klik menu `Tools > Packet Courier > Generate Prefabs`.
3. Expected: folder `Assets/_Prefabs/` berisi `PlayerAnimator.controller` + prefab `Player`, `Ground`, `Platform_OneWay`, `Spike`, `Enemy_Patrol`, `Laser_Beam`, `Platform_Moving` + `_GeneratedArt/`.
4. Klik lagi → idempotent, tidak ada error.

---


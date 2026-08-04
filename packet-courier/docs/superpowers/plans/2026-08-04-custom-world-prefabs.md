# Custom World + Prefab System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Mengganti level yang dibuat 100% kode (`PrototypeBootstrapper`) menjadi world manual yang dibangun user di scene Unity via prefab, plus fitur baru (sprint, enemy patroli, laser, platform bergerak, animasi player kurir).

**Architecture:** Satu editor script (`Assets/_Scripts/Editor/EditorGenerator.cs`) dengan menu `Tools > Packet Courier > Generate Prefabs` yang membuat semua prefab + AnimatorController + sprite placeholder ke `Assets/_Prefabs/`, idempotent. Menu `Setup Scene` menambahkan InputManager, kamera + CameraFollow, LevelManager, dan HUD ke scene aktif supaya scene manual langsung bisa dimainkan. Bootstrapper dihapus setelah generator siap.

**Tech Stack:** Unity 2D (URP tidak digunakan; default built-in pipeline), C#, UnityEditor API (`PrefabUtility`, `AnimationUtility`, `AnimatorController`), `UnityEngine.UI` untuk HUD.

## Global Constraints

- Bukan git repo — TIDAK ada `git commit`. Setiap task diakhiri verifikasi: buka Unity, cek Console tidak ada error kompilasi, lalu playtest sesuai langkah.
- Dilarang menambah dependency / package baru. Hanya pakai API Unity built-in.
- Semua asset yang dibuat generator wajib **idempotent**: klik ulang aman, hasil konsisten.
- Nama class/file harus persis seperti di design doc yang disetujui: `PlayerAnimator.cs`, `Enemy_Patrol.cs`, `Laser_Beam.cs`, `Platform_Moving.cs`, `EditorGenerator.cs`.
- Semua komponen baru guard null (player/hambatan hilang → diam, tidak crash).
- File di bawah 500 baris. Jangan menambah komentar kecuali `[Header]` agar konsisten dengan kode existing.
- Menu item wajib: `Tools > Packet Courier > Generate Prefabs`. Tambahan: `Tools > Packet Courier > Setup Scene`.
- Player prefab tag = `Player`. Komponen player: `PlayerController` + `PlayerMode` + `PlayerHealth` + `PlayerAnimator` + `Rigidbody2D` + `BoxCollider2D` + `SpriteRenderer` + `Animator`.
- Animator player: state `Idle` → `Run`, param `isMoving` (bool), `speedMultiplier` (float), `facing` (float).

---

### Task 1: Tambah `sprintHeld` di InputManager

**Files:**
- Modify: `Assets/_Scripts/Core/InputManager.cs`

**Interfaces:**
- Produces: `InputManager.sprintHeld` (public bool, true selama Left Shift ditekan). Dibaca Task 3.

- [ ] **Step 1: Tambah field `sprintHeld`**

Di `InputManager.cs`, di blok `[Header("Buttons")]` (setelah baris `public bool powerUpPressed;`):

```csharp
    public bool sprintHeld;
```

- [ ] **Step 2: Set `sprintHeld` di keyboard handler**

Di `HandleKeyboardInput()`, tambahkan paling atas (sebelum `moveInput`):

```csharp
        sprintHeld = Input.GetKey(KeyCode.LeftShift);
```

Catatan: `sprintHeld` TIDAK dibersihkan di `ClearPressedFlags()` karena ini state "sedang tahan", bukan "baru ditekan".

- [ ] **Step 3: Verify compile**

Buka Unity → Console. Expected: tidak ada error kompilasi.

---

### Task 2: Buat `PlayerAnimator.cs`

**Files:**
- Create: `Assets/_Scripts/Player/PlayerAnimator.cs`

**Interfaces:**
- Consumes: `Animator` + `SpriteRenderer` (dari GameObject yang sama).
- Produces: `PlayerAnimator.UpdateState(bool isMoving, float speedMultiplier, float facing)` — dipanggil Task 3. Men-set param animator `isMoving` / `speedMultiplier` / `facing`, set `animator.speed = speedMultiplier`, dan `spriteRenderer.flipX = facing < 0`.

- [ ] **Step 1: Tulis file baru**

```csharp
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    private bool _warned;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void UpdateState(bool isMoving, float speedMultiplier, float facing)
    {
        if (animator == null)
        {
            if (!_warned)
            {
                Debug.LogWarning("[PlayerAnimator] Animator belum di-set. Skip animasi.", this);
                _warned = true;
            }
            return;
        }

        animator.speed = speedMultiplier;
        animator.SetBool("isMoving", isMoving);
        animator.SetFloat("speedMultiplier", speedMultiplier);
        animator.SetFloat("facing", facing);

        if (spriteRenderer != null)
            spriteRenderer.flipX = facing < 0;
    }
}
```

- [ ] **Step 2: Verify compile**

Buka Unity → Console. Expected: tidak ada error kompilasi.

---

### Task 3: Sprint + wiring animator di PlayerController

**Files:**
- Modify: `Assets/_Scripts/Player/PlayerController.cs`

**Interfaces:**
- Consumes: `_input.sprintHeld` (Task 1), `PlayerAnimator.UpdateState` (Task 2).
- Produces: `PlayerController.isSprinting` (public bool). Kecepatan lari = `GetSpeed() * sprintSpeedMultiplier` (8 → 12.8), animator `speedMultiplier` = `sprintAnimMultiplier` (1.8) saat sprint.

- [ ] **Step 1: Tambah header + field Sprint**

Setelah blok `[Header("Dash")]` (setelah `public LayerMask dashBlockMask;`):

```csharp
    [Header("Sprint")]
    public float sprintSpeedMultiplier = 1.6f;
    public float sprintAnimMultiplier = 1.8f;
    public bool isSprinting;
```

- [ ] **Step 2: Tambah field `_animator`**

Di antara field private (setelah `private PlayerHealth _health;`):

```csharp
    private PlayerAnimator _animator;
```

- [ ] **Step 3: Assign `_animator` di Awake**

Di `Awake()` (setelah `_health = GetComponent<PlayerHealth>();`):

```csharp
        _animator = GetComponent<PlayerAnimator>();
```

- [ ] **Step 4: Null-guard `_input` di HandleJumpInput & HandleDashInput**

Di awal `HandleJumpInput()`:

```csharp
        if (_input == null) return;
```

Di awal `HandleDashInput()`:

```csharp
        if (_input == null) return;
```

- [ ] **Step 5: Panggil HandleSprintInput di Update**

Di `Update()`, paling akhir (setelah `UpdateFacing();`):

```csharp
        HandleSprintInput();
```

- [ ] **Step 6: Terapkan multiplier sprint di FixedMove**

Ubah baris di `FixedMove()`:

```csharp
        float targetSpeed = _mode.GetSpeed() * _moveInput.x;
```

menjadi:

```csharp
        float targetSpeed = _mode.GetSpeed() * (isSprinting ? sprintSpeedMultiplier : 1f) * _moveInput.x;
```

- [ ] **Step 7: Tambah metode HandleSprintInput**

Tempatkan sebelum `UpdateFacing()`:

```csharp
    private void HandleSprintInput()
    {
        isSprinting = _input != null && _input.sprintHeld && Mathf.Abs(_moveInput.x) > 0.01f && isGrounded;

        if (_animator == null) return;
        float mult = isSprinting ? sprintAnimMultiplier : 1f;
        _animator.UpdateState(Mathf.Abs(_rb.velocity.x) > 0.1f, mult, FacingDir);
    }
```

- [ ] **Step 8: Verify compile + playtest**

1. Buka Unity → Console, expected tidak ada error.
2. Play scene yang sudah ada `InputManager` → gerak kiri/kanan, tahan Left Shift → player lari lebih cepat.

---

### Task 4: Buat `Enemy_Patrol.cs`

**Files:**
- Create: `Assets/_Scripts/Level/Enemy_Patrol.cs`

**Interfaces:**
- Consumes: `PlayerHealth.TakeDamage(amount, DamageType.TTLDamage)` (sudah ada), tag `Player`.
- Produces: `Enemy_Patrol` — patroli antara `_startPos + pointA` ↔ `_startPos + pointB`; player dalam `detectionRadius` → state Chase (kejar); kena = `TakeDamage(40, TTLDamage)` + cooldown 1 detik. Butuh `Rigidbody2D` (gravityScale 0) + `Collider2D` solid di GameObject yang sama.

- [ ] **Step 1: Tulis file baru**

```csharp
using UnityEngine;

public class Enemy_Patrol : MonoBehaviour
{
    [Header("Patrol")]
    public Vector2 pointA = new Vector2(-2, 0);
    public Vector2 pointB = new Vector2(2, 0);
    public float patrolSpeed = 2f;

    [Header("Chase")]
    public float detectionRadius = 5f;
    public float chaseSpeed = 4f;

    [Header("Damage")]
    public float damageAmount = 40f;
    public float damageCooldown = 1f;

    public enum State { Patrol, Chase }
    [Header("State")]
    public State currentState = State.Patrol;

    private Vector2 _startPos;
    private Vector2 _target;
    private Rigidbody2D _rb;
    private Transform _player;
    private float _damageTimer;
    private SpriteRenderer _sprite;

    private void Awake()
    {
        _startPos = transform.position;
        _target = _startPos + pointA;
        _rb = GetComponent<Rigidbody2D>();
        _sprite = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        _player = FindObjectOfType<PlayerController>()?.transform;
    }

    private void FixedUpdate()
    {
        if (_rb == null) return;

        if (_player == null)
        {
            _player = FindObjectOfType<PlayerController>()?.transform;
            if (_player == null) return;
        }

        float dist = Vector2.Distance(transform.position, _player.position);
        currentState = dist <= detectionRadius ? State.Chase : State.Patrol;

        Vector2 moveDir;
        if (currentState == State.Chase)
        {
            moveDir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
            _rb.velocity = moveDir * chaseSpeed;
        }
        else
        {
            if (Vector2.Distance(transform.position, _target) < 0.2f)
                _target = Vector2.Distance(_target, _startPos + pointA) < 0.1f
                    ? _startPos + pointB
                    : _startPos + pointA;

            moveDir = ((Vector2)_target - (Vector2)transform.position).normalized;
            _rb.velocity = moveDir * patrolSpeed;
        }

        if (_sprite)
            _sprite.flipX = _rb.velocity.x < 0;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamage(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDamage(collision.collider);
    }

    private void TryDamage(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time < _damageTimer) return;

        var health = other.GetComponent<PlayerHealth>();
        if (health)
        {
            health.TakeDamage(damageAmount, PlayerHealth.DamageType.TTLDamage);
            _damageTimer = Time.time + damageCooldown;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            _startPos = transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(_startPos + pointA, Vector3.one * 0.4f);
        Gizmos.DrawWireCube(_startPos + pointB, Vector3.one * 0.4f);
        Gizmos.DrawLine(_startPos + pointA, _startPos + pointB);
    }
}
```

- [ ] **Step 2: Verify compile**

Buka Unity → Console. Expected: tidak ada error kompilasi.

---

### Task 5: Buat `Laser_Beam.cs`

**Files:**
- Create: `Assets/_Scripts/Level/Laser_Beam.cs`

**Interfaces:**
- Consumes: `PlayerHealth.TakeDamage(amount, DamageType.TTLDamage)`, tag `Player`.
- Produces: `Laser_Beam` — nyala `onDuration` / mati `offDuration` (loop), `phaseOffset` detik; collider + sprite cuma aktif saat nyala; kena = `TakeDamage(30, TTLDamage)` + cooldown 1 detik.

- [ ] **Step 1: Tulis file baru**

```csharp
using UnityEngine;

public class Laser_Beam : MonoBehaviour
{
    [Header("Timing")]
    public float onDuration = 2f;
    public float offDuration = 1.5f;
    public float phaseOffset = 0f;

    [Header("Damage")]
    public float damageAmount = 30f;
    public float damageCooldown = 1f;

    [Header("Visual")]
    public SpriteRenderer beamSprite;

    public bool IsOn { get; private set; }

    private Collider2D _collider;
    private float _timer;
    private float _damageTimer;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        if (beamSprite == null) beamSprite = GetComponent<SpriteRenderer>();
        _timer = -phaseOffset;
    }

    private void Update()
    {
        float cycle = onDuration + offDuration;
        if (cycle <= 0) return;

        _timer += Time.deltaTime;
        SetOn(Mathf.Repeat(_timer, cycle) < onDuration);
    }

    private void SetOn(bool on)
    {
        IsOn = on;
        if (_collider) _collider.enabled = on;
        if (beamSprite) beamSprite.enabled = on;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!IsOn) return;
        if (!other.CompareTag("Player")) return;
        if (Time.time < _damageTimer) return;

        var health = other.GetComponent<PlayerHealth>();
        if (health)
        {
            health.TakeDamage(damageAmount, PlayerHealth.DamageType.TTLDamage);
            _damageTimer = Time.time + damageCooldown;
        }
    }
}
```

- [ ] **Step 2: Verify compile**

Buka Unity → Console. Expected: tidak ada error kompilasi.

---

### Task 6: Buat `Platform_Moving.cs`

**Files:**
- Create: `Assets/_Scripts/Level/Platform_Moving.cs`

**Interfaces:**
- Produces: `Platform_Moving` — bergerak horizontal antara `_startPos + pointA` ↔ `_startPos + pointB` memakai `Rigidbody2D` kinematic + `MovePosition`; player di atas ikut terbawa. Butuh `Rigidbody2D` bodyType=Kinematic di GameObject yang sama.

- [ ] **Step 1: Tulis file baru**

```csharp
using UnityEngine;

public class Platform_Moving : MonoBehaviour
{
    [Header("Path")]
    public Vector2 pointA = new Vector2(-3, 0);
    public Vector2 pointB = new Vector2(3, 0);
    public float speed = 2f;

    private Vector2 _startPos;
    private Vector2 _target;
    private Rigidbody2D _rb;

    private void Awake()
    {
        _startPos = transform.position;
        _target = _startPos + pointA;
        _rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (_rb == null) return;

        if (Vector2.Distance(transform.position, _target) < 0.05f)
            _target = Vector2.Distance(_target, _startPos + pointA) < 0.1f
                ? _startPos + pointB
                : _startPos + pointA;

        Vector2 dir = ((Vector2)_target - (Vector2)transform.position).normalized;
        _rb.MovePosition((Vector2)transform.position + dir * speed * Time.fixedDeltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            _startPos = transform.position;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(_startPos + pointA, Vector3.one * 0.4f);
        Gizmos.DrawWireCube(_startPos + pointB, Vector3.one * 0.4f);
        Gizmos.DrawLine(_startPos + pointA, _startPos + pointB);
    }
}
```

- [ ] **Step 2: Verify compile**

Buka Unity → Console. Expected: tidak ada error kompilasi.

---

### Task 7: Pindahkan `CameraFollow` ke file sendiri

**Files:**
- Create: `Assets/_Scripts/Core/CameraFollow.cs`
- Modify: `Assets/_Scripts/PrototypeBootstrapper.cs` (hapus class `CameraFollow`, baris 391–409)

**Interfaces:**
- Produces: `CameraFollow` (public class, API tidak berubah) — dipakai Task 9 (Setup Scene). Setelah Task 10 menghapus bootstrapper, camera tetap tersedia.

- [ ] **Step 1: Buat `CameraFollow.cs`**

```csharp
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(4, 2, -10);
    public float smoothSpeed = 5f;

    private void LateUpdate()
    {
        if (target == null)
        {
            var player = FindObjectOfType<PlayerController>();
            if (player) target = player.transform;
            else return;
        }

        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
    }
}
```

- [ ] **Step 2: Hapus class `CameraFollow` dari `PrototypeBootstrapper.cs`**

Hapus seluruh blok mulai `public class CameraFollow : MonoBehaviour` sampai akhir file (baris 391–409). File bootstrapper berakhir setelah `SetupTiledSprite` method (kurung tutup `}` baris 389).

- [ ] **Step 3: Verify compile**

Buka Unity → Console. Expected: tidak ada error kompilasi (class `CameraFollow` kini satu-satunya di file barunya).

---

### Task 8: Buat `EditorGenerator.cs` — kerangka + sprite + AnimatorController

**Files:**
- Create: `Assets/_Scripts/Editor/EditorGenerator.cs`
- Create (dihasilkan): `Assets/_Prefabs/_GeneratedArt/*.png` + `Assets/_Prefabs/PlayerAnimator.controller`

**Interfaces:**
- Produces (dipakai Task 9 & 10):
  - `EditorGenerator.GeneratePrefabs()` — menu item, titik masuk utama.
  - `EditorGenerator.SetupScene()` — menu item (diimplementasikan di Task 10).
  - Helper `EnsureFolder(string path)`, `LoadOrCreateSprite(string name, int w, int h, Color color, float ppu)`, `LoadWalkSprite(int index)`, `CreateSolidSpriteAsset(...)`, `CreatePlayerAnimatorController()`.
- Path konstanta: `PrefabRoot = "Assets/_Prefabs"`, `ArtRoot = PrefabRoot + "/_GeneratedArt"`, `WalkSpriteDir = "Assets/walk"`.

- [ ] **Step 1: Tulis kerangka generator + helper sprite**

```csharp
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class EditorGenerator
{
    private const string PrefabRoot = "Assets/_Prefabs";
    private const string ArtRoot = PrefabRoot + "/_GeneratedArt";
    private const string WalkSpriteDir = "Assets/walk";

    [MenuItem("Tools/Packet Courier/Generate Prefabs")]
    public static void GeneratePrefabs()
    {
        EnsureFolder(PrefabRoot);
        EnsureFolder(ArtRoot);
        EnsureWalkSpriteImportSettings();

        // Implementasi pembuatan prefab ada di Task 9 & 10 (GeneratePlayer + teman-temannya dipanggil di sini).
    }

    private static void EnsureFolder(string assetPath)
    {
        string[] parts = assetPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static void EnsureWalkSpriteImportSettings()
    {
        if (!Directory.Exists(WalkSpriteDir)) return;

        foreach (string path in Directory.GetFiles(WalkSpriteDir, "*.png"))
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = 32f;
            importer.SaveAndReimport();
        }
    }

    private static Sprite LoadWalkSprite(int index)
    {
        string name = index == 0 ? "walk" : $"walk{index}";
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{WalkSpriteDir}/{name}.png");
    }

    private static Sprite LoadOrCreateSprite(string name, int w, int h, Color color, float ppu)
    {
        string path = $"{ArtRoot}/{name}.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                tex.SetPixel(x, y, color);
        tex.filterMode = FilterMode.Point;
        tex.Apply();

        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);

        if (AssetImporter.GetAtPath(path) is TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.spritePixelsPerUnit = ppu;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
#endif
```

- [ ] **Step 2: Verify compile**

Buka Unity → Console. Expected: tidak ada error. Menu `Tools > Packet Courier > Generate Prefabs` muncul (belum melakukan apa-apa selain bikin folder + import settings).

---

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

### Task 10: Generator — prefab rintangan/lain + Setup Scene

**Files:**
- Modify: `Assets/_Scripts/Editor/EditorGenerator.cs`

**Interfaces:**
- Consumes: `Gate_Firewall`, `Checkpoint_TCP`, `Collectible_DataBit`, `Collectible_Snippet`, `ServerB_Goal`, `LevelManager`, `CameraFollow` (Task 7), HUD scripts.
- Produces: prefab `Firewall_Gate`, `Checkpoint_TCP`, `DataBit`, `Snippet`, `ServerB_Goal`; menu `Tools > Packet Courier > Setup Scene` (menambah InputManager, kamera+CameraFollow, LevelManager, HUD ke scene aktif).

- [ ] **Step 1: Tambah prefab Firewall_Gate + Checkpoint_TCP**

```csharp
    private static void CreateFirewallGatePrefab()
    {
        string path = PrefabRoot + "/Firewall_Gate.prefab";
        GameObject go = new GameObject("Firewall_Gate");
        go.layer = 0;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 2f);
        col.isTrigger = true;

        var gate = go.AddComponent<Gate_Firewall>();
        gate.requiredPort = 80;
        gate.requireEncrypted = false;

        var closed = new GameObject("Visual_Closed");
        closed.transform.SetParent(go.transform);
        var srClosed = closed.AddComponent<SpriteRenderer>();
        srClosed.sprite = LoadOrCreateSprite("gate_closed", 10, 20, new Color(1f, 0.5f, 0f), 32f);
        srClosed.sortingOrder = 6;

        var open = new GameObject("Visual_Open");
        open.transform.SetParent(go.transform);
        var srOpen = open.AddComponent<SpriteRenderer>();
        srOpen.sprite = LoadOrCreateSprite("gate_open", 10, 20, new Color(0.4f, 1f, 0.4f), 32f);
        srOpen.sortingOrder = 6;

        gate.gateOpenVisual = open;
        gate.gateClosedVisual = closed;
        open.SetActive(false);

        SavePrefab(go, path);
    }

    private static void CreateCheckpointPrefab()
    {
        string path = PrefabRoot + "/Checkpoint_TCP.prefab";
        GameObject go = new GameObject("Checkpoint_TCP");
        go.layer = 0;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1.5f);
        col.isTrigger = true;

        var cp = go.AddComponent<Checkpoint_TCP>();

        var inactive = new GameObject("Visual_Inactive");
        inactive.transform.SetParent(go.transform);
        var srInactive = inactive.AddComponent<SpriteRenderer>();
        srInactive.sprite = LoadOrCreateSprite("checkpoint_off", 8, 12, Color.gray, 32f);
        srInactive.sortingOrder = 6;

        var active = new GameObject("Visual_Active");
        active.transform.SetParent(go.transform);
        var srActive = active.AddComponent<SpriteRenderer>();
        srActive.sprite = LoadOrCreateSprite("checkpoint_on", 8, 12, new Color(0f, 1f, 0.6f), 32f);
        srActive.sortingOrder = 6;

        cp.visualActive = active;
        cp.visualInactive = inactive;
        active.SetActive(false);

        SavePrefab(go, path);
    }
```

- [ ] **Step 2: Tambah prefab DataBit + Snippet + ServerB_Goal**

```csharp
    private static void CreateDataBitPrefab()
    {
        string path = PrefabRoot + "/DataBit.prefab";
        GameObject go = new GameObject("DataBit");
        go.layer = 0;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.5f, 0.5f);
        col.isTrigger = true;

        go.AddComponent<Collectible_DataBit>();

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadOrCreateSprite("databit", 8, 8, new Color(0f, 1f, 0.3f), 32f);
        sr.sortingOrder = 7;

        SavePrefab(go, path);
    }

    private static void CreateSnippetPrefab()
    {
        string path = PrefabRoot + "/Snippet.prefab";
        GameObject go = new GameObject("Snippet");
        go.layer = 0;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.6f, 0.6f);
        col.isTrigger = true;

        go.AddComponent<Collectible_Snippet>();

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadOrCreateSprite("snippet", 10, 10, Color.magenta, 32f);
        sr.sortingOrder = 7;

        SavePrefab(go, path);
    }

    private static void CreateServerBGoalPrefab()
    {
        string path = PrefabRoot + "/ServerB_Goal.prefab";
        GameObject go = new GameObject("ServerB_Goal");
        go.layer = 0;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2f, 4f);
        col.isTrigger = true;

        go.AddComponent<ServerB_Goal>();

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadOrCreateSprite("serverb_goal", 20, 40, Color.yellow, 32f);
        sr.sortingOrder = 8;

        SavePrefab(go, path);
    }
```

- [ ] **Step 3: Lengkapi GeneratePrefabs**

Di `GeneratePrefabs()`, setelah pemanggilan Task 9, tambahkan:

```csharp
        CreateFirewallGatePrefab();
        CreateCheckpointPrefab();
        CreateDataBitPrefab();
        CreateSnippetPrefab();
        CreateServerBGoalPrefab();
```

Ganti dialog akhir menjadi:

```csharp
        EditorUtility.DisplayDialog(
            "Packet Courier",
            "Semua prefab selesai dibuat di Assets/_Prefabs/.\nBuka Tools > Packet Courier > Setup Scene untuk menyiapkan scene kosong.",
            "OK"
        );
```

- [ ] **Step 4: Menu Setup Scene**

Tambah method + menu item baru:

```csharp
    [MenuItem("Tools/Packet Courier/Setup Scene")]
    public static void SetupScene()
    {
        EnsureInputManager();
        EnsureCamera();
        EnsureLevelManager();
        EnsureHUD();

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog(
            "Packet Courier",
            "Scene disiapkan: InputManager, Main Camera (CameraFollow), LevelManager, dan HUD.\nDrag prefab dari Assets/_Prefabs/ untuk membangun level.",
            "OK"
        );
    }

    private static void EnsureInputManager()
    {
        if (Object.FindObjectOfType<InputManager>() != null) return;

        var go = new GameObject("InputManager");
        go.AddComponent<InputManager>();
    }

    private static void EnsureCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }

        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.15f);
        cam.transform.position = new Vector3(0, 0, -10);

        if (cam.GetComponent<CameraFollow>() == null)
            cam.gameObject.AddComponent<CameraFollow>();
    }

    private static void EnsureLevelManager()
    {
        if (Object.FindObjectOfType<LevelManager>() != null) return;

        var go = new GameObject("LevelManager");
        var lm = go.AddComponent<LevelManager>();
        lm.worldIndex = 0;
        lm.segmentIndex = 0;
        lm.levelDrainRate = 1f;
    }

    private static void EnsureHUD()
    {
        if (Object.FindObjectOfType<HUD_TTL>() != null) return;

        var canvasObj = new GameObject("HUD_Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var hudTTL = new GameObject("HUD_TTL");
        hudTTL.transform.SetParent(canvasObj.transform);
        var ttlCanvas = hudTTL.AddComponent<HUD_TTL>();

        var ttlBar = new GameObject("TTL_Bar");
        ttlBar.transform.SetParent(hudTTL.transform);
        var img = ttlBar.AddComponent<UnityEngine.UI.Image>();
        ttlCanvas.ttlFillBar = img;
        var rt = ttlBar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.01f, 0.94f);
        rt.anchorMax = new Vector2(0.35f, 0.98f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        img.color = Color.green;
        img.type = UnityEngine.UI.Image.Type.Filled;
        img.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;

        var ttlTxt = new GameObject("TTL_Text");
        ttlTxt.transform.SetParent(hudTTL.transform);
        var tmp = ttlTxt.AddComponent<UnityEngine.UI.Text>();
        ttlCanvas.ttlText = tmp;
        var txtRt = ttlTxt.GetComponent<RectTransform>();
        txtRt.anchorMin = new Vector2(0.01f, 0.89f);
        txtRt.anchorMax = new Vector2(0.35f, 0.94f);
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;
        tmp.text = "TTL: 100/100";
        tmp.fontSize = 14;
        tmp.alignment = TextAnchor.MiddleLeft;
        tmp.color = Color.white;
        tmp.font = font;

        var hudModeObj = new GameObject("HUD_Mode");
        hudModeObj.transform.SetParent(canvasObj.transform);
        var modeComp = hudModeObj.AddComponent<HUD_Mode>();

        var modeText = new GameObject("Mode_Label");
        modeText.transform.SetParent(hudModeObj.transform);
        var mTmp = modeText.AddComponent<UnityEngine.UI.Text>();
        modeComp.modeText = mTmp;
        var modeRt = modeText.GetComponent<RectTransform>();
        modeRt.anchorMin = new Vector2(0.75f, 0.94f);
        modeRt.anchorMax = new Vector2(0.99f, 0.98f);
        modeRt.offsetMin = Vector2.zero;
        modeRt.offsetMax = Vector2.zero;
        mTmp.text = "PLAINTEXT";
        mTmp.fontSize = 14;
        mTmp.alignment = TextAnchor.MiddleRight;
        mTmp.color = Color.green;
        mTmp.font = font;

        var bitsText = new GameObject("Bits_Label");
        bitsText.transform.SetParent(hudModeObj.transform);
        var bTmp = bitsText.AddComponent<UnityEngine.UI.Text>();
        modeComp.bitsText = bTmp;
        var bitsRt = bitsText.GetComponent<RectTransform>();
        bitsRt.anchorMin = new Vector2(0.55f, 0.94f);
        bitsRt.anchorMax = new Vector2(0.74f, 0.98f);
        bitsRt.offsetMin = Vector2.zero;
        bitsRt.offsetMax = Vector2.zero;
        bTmp.text = "BITS \u00A5 0";
        bTmp.fontSize = 12;
        bTmp.alignment = TextAnchor.MiddleRight;
        bTmp.color = Color.yellow;
        bTmp.font = font;
    }
```

- [ ] **Step 5: Verify compile + generate + setup**

1. Buka Unity → Console: tidak ada error.
2. Klik `Generate Prefabs` → semua 12 prefab ada di `Assets/_Prefabs/`.
3. Buka scene kosong → klik `Tools > Packet Courier > Setup Scene`.
4. Expected: muncul `InputManager`, `Main Camera` (dengan CameraFollow), `LevelManager`, `HUD_Canvas`.
5. Klik Setup Scene lagi → idempotent, tidak dobel.

---

### Task 11: Hapus PrototypeBootstrapper + verifikasi akhir

**Files:**
- Delete: `Assets/_Scripts/PrototypeBootstrapper.cs`

**Interfaces:**
- Semua class yang dibutuhkan runtime sudah tersedia: `CameraFollow` (Task 7), `InputManager` (Setup Scene), HUD & LevelManager (Setup Scene), prefab (Task 9–10). Tidak ada referensi ke `PrototypeBootstrapper` dari file lain.

- [ ] **Step 1: Hapus file**

Hapus `Assets/_Scripts/PrototypeBootstrapper.cs` lewat editor Unity (kanan-klik → Delete) atau lewat file manager, lalu Refresh AssetDatabase.

- [ ] **Step 2: Verify compile global**

1. Buka Unity → biarkan kompilasi selesai → Console kosong dari error.
2. Cari sisa referensi: `grep -r "PrototypeBootstrapper" Assets --include=*.cs` → tidak ada hasil (kecuali di docs/).

- [ ] **Step 3: Playtest end-to-end (langkah manual user)**

Di scene kosong:
1. `Tools > Packet Courier > Generate Prefabs` (sekali).
2. `Tools > Packet Courier > Setup Scene`.
3. Drag `Player` prefab ke scene (posisi mis. (0, 2)).
4. Bangun level sederhana: drag `Ground` beberapa kali (scale lebar), `Platform_OneWay`, `Spike`, `Enemy_Patrol`, `Laser_Beam`, `Platform_Moving`, `DataBit`, `Snippet`, `ServerB_Goal`.
5. Play:
   - Gerak kiri/kanan → animasi Run; berhenti → Idle.
   - Tahan Left Shift → lari lebih cepat, animasi lebih cepat.
   - Kena spike/enemy/laser → TTL turun (HUD_TTL berkurang), tidak crash.
   - Naik `Platform_Moving` → ikut terbawa.
   - Sentuh `ServerB_Goal` → segmen selesai (pesan dari LevelManager/GameManager).
6. Kalau `GameManager` belum ada di scene, tambahkan via Add Component → `GameManager` (DontDestroyOnLoad) supaya `AddDataBits`/`OnSegmentComplete` tidak null. Catatan: tanpa GameManager game tetap jalan, collectible hanya tidak tercatat.

- [ ] **Step 4: Update design doc status**

Di `docs/superpowers/specs/2026-08-04-custom-world-prefabs-design.md`, ubah baris status menjadi:

```markdown
Status: Approved (bagian 1–4 disetujui user) — Implemented 2026-08-04
```

---

## Self-Review

**1. Spec coverage:**
- World manual di scene → Task 9–11 (prefab + Setup Scene + hapus bootstrapper). ✓
- Player sprite kurir + animasi Idle/Run → Task 2, 8, 9 (controller + clip + prefab). ✓
- Sprint Left Shift ×1.6, animasi ×1.8 → Task 1, 3. ✓
- Spikes pakai `Hazard_Spike` → Task 9 (`CreateSpikePrefab`). ✓
- Enemy patroli + chase + damage 40 cooldown 1s → Task 4, 9. ✓
- Laser nyala-mati + damage 30 → Task 5, 9. ✓
- Platform moving horizontal → Task 6, 9. ✓
- ServerB_Goal finish → Task 10. ✓
- Semua jadi prefab → Task 9, 10 (12 prefab). ✓
- `EditorGenerator.cs` + menu `Tools > Packet Courier > Generate Prefabs` → Task 8–10. ✓
- Idempotent → `LoadOrCreateSprite` skip jika ada; `SaveAsPrefabAsset` overwrite; Setup Scene `Ensure*` guard. ✓
- Null-guard semua komponen baru → Task 4, 5, 2 (warn sekali), 3 (`_input == null`). ✓
- Bootstrapper dihapus → Task 7 (CameraFollow dipindah dulu), Task 11. ✓
- NRE `InputManager.Instance` di scene manual → Task 3 (null-guard `_input`) + Task 10 (Setup Scene otomatis bikin InputManager). ✓

**2. Placeholder scan:** Semua step berisi kode konkret; tidak ada "TBD"/"implement later". Metode yang dipakai antar task konsisten (`LoadWalkSprite`, `LoadOrCreateSprite`, `SavePrefab`, `EnsureFolder`).

**3. Type consistency:**
- `UpdateState(bool, float, float)` di Task 2 ↔ dipanggil Task 3 Step 7. ✓
- `sprintHeld` Task 1 ↔ Task 3 Step 7. ✓
- `LoadWalkSprite(int)`, `LoadOrCreateSprite(name,w,h,color,ppu)` Task 8 ↔ Task 9–10. ✓
- `CreatePlayerAnimatorController()` Task 9 Step 1 ↔ dipanggil Task 9 Step 5. ✓
- `SetupScene()` Task 10 Step 4 ↔ menu item Task 10 Step 4. ✓
- `SavePrefab(go, path)` Task 9 Step 4 ↔ semua method Task 9–10. ✓

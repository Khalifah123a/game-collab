# Packet Courier — Sprint 2 Bug Fixes

## Masalah yang Diperbaiki

### 1. Enemy tidak bisa nyerang
**Root cause:** `Enemy_Patrol` pakai `OnCollisionEnter2D` dengan `BoxCollider2D` (solid). Player juga solid collider — dua solid collider tidak saling trigger callback damage.

**Fix:** 
- Ganti ke `CircleCollider2D` + `isTrigger = true`
- Ganti ke `OnTriggerStay2D` untuk damage
- Rigidbody tetap kinematic (tidak terdorong saat kena player)

### 2. Laser tembus player tanpa damage
**Root cause:** `OnTriggerStay2D` tidak konsisten karena collider di-disable saat laser mati. Kalau player sudah di dalam trigger saat laser mati, callback terhenti.

**Fix:**
- Tambah `OnTriggerEnter2D` / `OnTriggerExit2D` untuk track `_playerInside`
- Damage check pindah ke `FixedUpdate` — hanya aktif kalau `IsOn && _playerInside`
- `damageCooldown` dipercepat (0.25s) supaya terasa saat laser menyala

### 3. "no cameras rendering"
**Root cause:** Kamera scene tidak di-set `clearFlags = SolidColor` saat setup, atau ada konflik render.

**Fix:**
- Tambah `cam.clearFlags = CameraClearFlags.SolidColor` di `EnsureCamera()`
- Hapus Library cache supaya scene di-reimport bersih

### 4. Double-jump belum terimplementasi
**Root cause:** Plan `docs/superpowers/plans/2026-08-11-double-jump-plan.md` mencentang semua task, tapi tidak ada kode air jump sama sekali (grep `airjump` kosong) — dokumen ditulis, kode tidak.

**Fix:**
- `PlayerController.cs`: tambah `airJumpsAvailable = 1` + `airJumpsLeft`, reset di `Start()` & `CheckGrounded()`, branch air jump setelah coyote & sebelum walljump (`DoJump(Vector2.up, jumpForce)`, decrement, `_coyoteTimer = 0`), diblokir saat dash (`!isDashing`) & wall cling (`!isOnWall`). Jumlah naik via `GetAirJumps()` = `airJumpsAvailable + upgrade AirJump`.
- `GameManager.cs`: tambah `UpgradeType.AirJump` (di akhir enum agar indeks aman), `maxLevels` 2, `costs` 300/500.
- `CameraFix.cs` dihapus — dead code murni (tak terpasang di scene, `EnsureCamera()` sudah menangani clearFlags + background).

---

## Langkah Regenerasi di Unity

Setelah semua script di-update, jalankan **urutan ini** di Unity Editor:

### Step 1: Clear Cache
```
Edit > Preferences > Project > Press "Clear Library Cache"
atau manual: tutup Unity, hapus folder Library/, buka ulang
```

### Step 2: Regenerate Prefabs
```
Tools > Packet Courier > Generate Prefabs
```
Ini akan membuat ulang SEMUA prefab termasuk:
- `Enemy_Patrol.prefab` — sekarang pakai CircleCollider2D trigger
- `Laser_Beam.prefab` — BoxCollider2D trigger (sudah benar)
- `Fan_Vortex.prefab` — baru dibuat
- `Ground`, `Platform`, `Spike`, dll

### Step 3: Regenerate Level Scenes
```
Tools > Packet Courier > Generate Level Scenes
```
Ini akan rebuild semua scene World1_Segment1-3 dari file layout.

### Step 4: Setup Scene (kalau perlu)
```
Tools > Packet Courier > Setup Scene
```

### Step 5: Test
Buka **World1_Segment1** → Press Play

---

## Kontrol (Sprint 2)

| Key | Aksi |
|---|---|
| A/D atau ←/→ | Gerak kiri/kanan |
| Space / W / ↑ | Lompat (walljump di dinding) |
| Left Ctrl / Mouse0 | Dash 8 arah |
| Left Alt / Mouse1 | Toggle Plaintext ↔ Encrypted |
| Q | Port sebelumnya |
| E | Port selanjutnya |
| Enter | Interact (hold 0.5s di checkpoint) |
| Left Shift | Sprint (saat bergerak di ground) |

---

## Verification Checklist

- [ ] Player bisa gerak normal (WASD/Arrow)
- [ ] Player bisa dash (Ctrl) ke 8 arah
- [ ] Player bisa walljump (tempel dinding + jump)
- [ ] Player kena damage saat sentuh enemy (tidak perlu jump di atas)
- [ ] Player kena damage saat laser ON (red beam)
- [ ] Player TIDAK kena damage saat laser OFF (invisible)
- [ ] Firewall gate buka kalau port + mode sesuai
- [ ] Checkpoint aktif setelah hold Enter 0.5s
- [ ] TTL bar berkurang setiap detik
- [ ] Tidak ada error "no cameras rendering" saat Play
- [ ] Game over muncul kalau TTL habis
- [ ] Segment complete kalau sentuh Server B (kuning)

---

## Struktur File Setelah Fix

```
Assets/_Scripts/
  Core/
    CameraFollow.cs      (camera follow)
    GameManager.cs       (game state, upgrades)
    InputManager.cs      (keyboard + touch input)
  Player/
    PlayerController.cs  (movement, dash, walljump, air jump) ← FIXED
    PlayerMode.cs        (plain/enc toggle, port cycle)
    PlayerHealth.cs      (TTL system)
    PlayerAnimator.cs    (anim state)
  Level/
    Enemy_Patrol.cs      ( patrol + trigger damage) ← FIXED
    Laser_Beam.cs        ( timing + trigger damage) ← FIXED
    Fan_Vortex.cs        (push field enemy) ← NEW
    Gate_Firewall.cs     (port matching)
    Checkpoint_TCP.cs    (hold-to-activate) ← FIXED
    Hazard_Spike.cs
    Platform_Moving.cs
    ServerB_Goal.cs
    Collectible_*.cs
  UI/
    HUD_TTL.cs
    HUD_Cooldown.cs     ← fixed text
    HUD_Mode.cs
    MenuUI.cs
    HubUI.cs
    GameOverUI.cs
  Editor/
    EditorGenerator.cs   ← fixed enemy prefab
    EditorGeneratorSetup.cs ← fixed camera clear flags
    EditorLevelGenerator.cs

Assets/_Prefabs/        ← regenerating via Tools menu
Assets/_Scenes/         ← regenerating via Tools menu
Assets/_Levels/         ← layout files (text-based)
  World1_Segment1.txt
  World1_Segment2.txt
  World1_Segment3.txt
```

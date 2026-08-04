# Custom World + Prefab System — Design

Tanggal: 2026-08-04
Status: Approved (bagian 1–4 disetujui user) - Implemented 2026-08-04

## Tujuan

Mengubah game `packet-courier` dari level yang 100% dibuat kode (`PrototypeBootstrapper`)
menjadi level yang **dibangun manual oleh user langsung di scene Unity** lewat prefab.
Sekaligus menambah fitur baru: sprint, player sprite kurir baru dengan animasi walk,
enemy patroli yang mengejar, laser nyala-mati, dan platform bergerak horizontal.

## Keputusan Desain (hasil brainstorming)

| Bagian | Keputusan |
|---|---|
| World | Level campuran — beberapa segmen horizontal naik-turun, dibangun manual di scene |
| Player | Sprite kurir baru (32×32, 5 frame walk), Animator standar, animasi lain menyusul |
| Sprint | Fitur baru, tombol Left Shift, animasi walk diputar lebih cepat |
| Spikes | Pakai `Hazard_Spike` yang sudah ada |
| Enemy | Patroli antar 2 titik; kalau player masuk detection radius langsung chase & serang; kena = TTL -40 + cooldown damage |
| Laser | Sinar nyala-mati periodik (on/off duration), harus timing lewat; kena = TTL damage + cooldown |
| Platform bergerak | Maju-mundur horizontal (pointA ↔ pointB), kinematic, membawa player |
| Goal | `ServerB_Goal` tetap jadi garis finish |
| Prefab | Semua jadi prefab lengkap (player, ground, platform one-way, spikes, enemy, laser, platform moving, firewall gate, checkpoint, data bit, snippet, ServerB_Goal) |

## Arsitektur

**File baru:**

```
Assets/_Prefabs/                        ← hasil generate menu item
Assets/_Scripts/Editor/EditorGenerator.cs  ← Tools > Packet Courier > Generate Prefabs
Assets/_Scripts/Player/PlayerAnimator.cs   ← ngatur Animator (moving, speedMultiplier, facing)
Assets/_Scripts/Level/Enemy_Patrol.cs      ← enemy patroli + deteksi + serang
Assets/_Scripts/Level/Laser_Beam.cs        ← laser nyala-mati
Assets/_Scripts/Level/Platform_Moving.cs   ← platform maju-mundur
Assets/_Animation/PlayerAnimator.controller  ← AnimatorController (generate via script)
Assets/Sprites/Player/walk.png..walk4.png   ← sprite player (di-copy user)
```

**File diubah:**
- `InputManager.cs` → tambah `sprintHeld` (Left Shift)
- `PlayerController.cs` → tambah sprint (speed multiplier, refill timer, update animator)

**File dihapus:**
- `PrototypeBootstrapper.cs`

## Komponen & Perilaku

### Player (prefab `Player`)
- Sprite kurir 32×32, PPU 32, `Animator` state `Idle` → `Run`
- Komponen: `PlayerController` + `PlayerMode` + `PlayerHealth` + `PlayerAnimator` + `Rigidbody2D` + `BoxCollider2D` + `SpriteRenderer`
- Sprint: tahan Left Shift → kecepatan ×1.6 (plaintext 8 → 12.8), animator `speedMultiplier` ~1.8
- Animator parameter: `isMoving` (bool), `speedMultiplier` (float), `facing` (float)
- Idle pakai frame walk pertama sebagai placeholder

### Enemy_Patrol
- Patroli antara `pointA` ↔ `pointB` (Vector2 offset dari posisi awal)
- `detectionRadius` (public, default 5) — player masuk radius → state Chase (kejar dengan kecepatan lebih tinggi)
- Kena player = `PlayerHealth.TakeDamage(40, TTL)` + damage cooldown 1 detik
- Sprite placeholder kotak merah (bisa diganti)

### Laser_Beam
- Sinar balok tipis (SpriteRenderer stretch), `onDuration` / `offDuration` public, `phaseOffset` public
- Toggle nyala-mati via timer; collider aktif hanya saat nyala
- Kena sinar = TTL damage 30 + cooldown

### Platform_Moving
- Gerak horizontal antara `pointA` ↔ `pointB`, `Rigidbody2D` kinematic, pakai `MovePosition`/lerp
- Collider solid → player di atasnya ikut terbawa

### Semua rintangan
- Spikes: trigger collider (`Hazard_Spike` existing)
- Enemy: solid collider + damage saat collide
- Laser: trigger collider
- Platform: solid / one-way (PlatformEffector2D) — di-generate dua prefab (Ground solid, Platform_OneWay)

## Prefab Generator

Menu: **Tools > Packet Courier > Generate Prefabs**

Sekali klik menghasilkan prefab berikut ke `Assets/_Prefabs/`:

| Prefab | Isi |
|---|---|
| `Player` | Sprite kurir + Animator (Idle/Run) + PlayerController + PlayerMode + PlayerHealth + PlayerAnimator + RB + BoxCollider |
| `Ground` | BoxCollider solid + sprite tiled abu |
| `Platform_OneWay` | BoxCollider + PlatformEffector2D (one-way, drop-through) |
| `Spike` | `Hazard_Spike` + trigger collider + sprite duri |
| `Enemy_Patrol` | `Enemy_Patrol` + RB + collider + sprite placeholder |
| `Laser_Beam` | `Laser_Beam` + trigger collider + sprite merah |
| `Platform_Moving` | `Platform_Moving` + RB kinematic + collider + sprite |
| `Firewall_Gate` | `Gate_Firewall` + collider |
| `Checkpoint_TCP` | `Checkpoint_TCP` + trigger + sprite |
| `DataBit` | `Collectible_DataBit` + trigger + sprite |
| `Snippet` | `Collectible_Snippet` + trigger + sprite |
| `ServerB_Goal` | `ServerB_Goal` + trigger + sprite |

Generator **idempotent** — diklik ulang aman, prefab lama ditimpa konsisten.
Kalau sprite player belum ada di `Assets/Sprites/Player/`, generator menampilkan dialog
peringatan dan player memakai placeholder kotak cyan sampai sprite tersedia.

### Setup sprite walk (langkah user)
1. Copy `walk.png`–`walk4.png` ke `Assets/Sprites/Player/`
2. Tiap sprite: Pixels Per Unit = 32, Filter Mode = Point
3. Klik Generate → animasi Run otomatis memakai 5 frame itu

## Data Flow

- `InputManager` → `sprintHeld` → `PlayerController.HandleSprintInput()` → set multiplier + `PlayerAnimator.SetSpeedMultiplier()`
- `Enemy_Patrol` cek jarak player tiap `FixedUpdate` → state Patrol/Chase → gerak via velocity → collide damage
- `Laser_Beam` timer → toggle `SpriteRenderer.enabled` + collider → overlap damage
- `Platform_Moving` lerp posisi antar 2 titik di `FixedUpdate`, kinematic → player ikut terbawa

## Error Handling

- Semua komponen baru guard null (player hilang → enemy/laser diam, tidak crash)
- `PlayerAnimator`: kalau `Animator` belum di-set → warning sekali, skip
- Generator: sprite belum ada → dialog + placeholder
- Generator idempotent

## Testing

- Jalankan kompilasi (Unity batch mode bila memungkinkan; minimal cek sintaks semua file baru)
- Tes manual user di Unity:
  1. Generate prefabs
  2. Bangun level sederhana (ground, platform, 1 spike, 1 enemy, 1 laser, 1 moving platform, goal)
  3. Play: tes sprint (kecepatan + animasi cepat), tes tiap rintangan, tes finish Server B

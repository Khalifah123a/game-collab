# Packet Courier — Prototype Sprint 1

## Persiapan Unity

1. Buka **Unity Hub** → **Add** → pilih folder `packet-courier/`
2. Buka project (Unity 2022.3.x atau lebih baru)
3. Jika ada dialog upgrade, klik **OK**

## Cara Test Prototype

### Opsi 1: Auto-Bootstrap (langsung Play)

1. Di **Project window**, cari `_Scripts/PrototypeBootstrapper.cs`
2. Drag ke **Hierarchy** (ke scene apa saja, atau scene kosong)
3. **Save scene** sebagai `_Scenes/World1_Segment1`
4. **File** → **Build Settings** → **Add Open Scenes**
5. Tekan **Play**

Prototype akan otomatis spawn:
- Player (kotak cyan) dengan Rigidbody2D + collider
- Ground (garis biru gelap) + 8 platform
- 5 Data Bits (hijau) dan 1 Packet Snippet (magenta)
- Server B (kuning) — sentuh untuk complete segment
- HUD lengkap: TTL bar (atas), Dash cooldown (kiri), Mode + Port (kanan)

### Opsi 2: Manual Build Scene

1. Buat **GameObject** kosong di scene → beri nama `Bootstrapper`
2. Add Component → **PrototypeBootstrapper**
3. Atur `Spawn Player At Start = true`
4. Drag `_Scripts/Player` components langsung ke GameObject player jika ingin kustom

## Kontrol

| Aksi | Keyboard | Touch |
|---|---|---|
| Move | WASD / Arrow keys | Joystick kiri |
| Jump | Space | Tombol kanan bawah |
| Dash | Left Click / Z | Tombol dash |
| Toggle Mode | Right Click / X | Tombol mode |
| Port Next | E | Panah kanan port |
| Port Prev | Q | Panah kiri port |
| Interact | Enter / E | Hold di TCP node |
| Power-Up | Left Shift | Tombol power |

## Struktur Script

```
Assets/_Scripts/
├── Core/
│   ├── GameManager.cs       — State game, upgrade, run, death
│   └── InputManager.cs      — Keyboard + touch input
├── Player/
│   ├── PlayerController.cs  — Movement, dash, walljump
│   ├── PlayerMode.cs        — Mode Plain/Enc, port toggle
│   └── PlayerHealth.cs      — TTL system, damage
├── Level/
│   ├── LevelManager.cs      — Config per segment
│   ├── Checkpoint_TCP.cs    — Hold-to-activate checkpoint
│   ├── Gate_Firewall.cs     — Port + mode matching gate
│   ├── Collectible_DataBit.cs    — +10 bits
│   ├── Collectible_Snippet.cs    — +15 TTL
│   ├── Hazard_Spike.cs      — Insta-kill / TTL damage
│   └── ServerB_Goal.cs      — Trigger segment complete
├── UI/
│   ├── HUD_TTL.cs           — TTL progress bar (top)
│   ├── HUD_Cooldown.cs      — Dash cooldown (vertical kiri)
│   └── HUD_Mode.cs          — Mode, port, bits (kanan)
└── PrototypeBootstrapper.cs — Auto-build scene saat Play
```

## Sprint 1 Scope

- [x] Player movement (move, jump, dash 8-dir, walljump)
- [x] Mode toggle (Plaintext/Encrypted + stat switching)
- [x] TTL system (drain, damage, heal)
- [x] Port cycle (21→22→80→443→8080)
- [x] HUD (TTL bar, dash cooldown vertical, mode/port/bits)
- [x] Collectibles (Data Bits + Packet Snippet)
- [x] Goal trigger (Server B → segment complete)
- [ ] Gate Firewall (port matching)
- [ ] TCP Checkpoint (hold to activate)
- [ ] Enemy (Cursor, Process Zombie)
- [ ] Hazard (Spike)

**Next:** Sprint 2 — Enemy + full World 1 (3 segment)

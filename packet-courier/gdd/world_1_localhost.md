# WORLD 1: LOCALHOST — Blueprint

## Overview
| Aspek | Detail |
|---|---|
| **Theme** | Desktop PC — file folder, shortcut, taskbar |
| **Biome Visual** | Palet biru gelap `#0a0a2e` × hijau terminal `#00ff41`. Platform dari folder, ground dari taskbar, background motherboard texture |
| **New Mechanic** | Tutorial dasar — move, jump, dash, walljump, plaintext only |
| **TTL Budget** | 60 unit (drain 1/s → ~60s clear) |
| **Run Duration** | ~5 menit |
| **Total Segmen** | 3 |
| **Boss?** | ❌ |

---

## Segment Blueprint

```
├── Server A (Desktop)
├── CP1 ─── [Segmen 1: Recycle Bin]
├── CP2 ─── [Segmen 2: File Explorer]
├── CP3 ─── [Segmen 3: Network Tray]
└── Server B (Network Adapter)
```

### Segmen 1: Recycle Bin — CP1 → CP2
- **Panjang**: 2000px horizontal, 2 vertical climb
- **Platforming**: 3 walljump section sempit, 2 gap selebar 80px
- **Gate puzzle**: None (tutorial)
- **Enemy**: Cursor ×2 (static, patrol horizontal), Process Zombie ×1 (static, blocking)
- **Bits**: 10 ×3 = 30
- **Snippet**: +1 (posisi aman di tengah)
- **Layout**:
```
                                CP2 █
                            ▄▄▄▄▄▄▄▄
                          │Z│
  █ CP1            ┌──────┘ │   ┌───
  ████  ██  ████   │ C1     │ C2│
  █  █──█──█──█────┘        └───█───▄▄▄ Server B
  █  █  █  █  █                █
```

### Segmen 2: File Explorer — CP2 → CP3
- **Panjang**: 2500px, 1 vertical climb 400px (walljump sequence)
- **Platforming**: Vertical wall-climb section, platform sempit naik ke atas
- **Gate puzzle**: None (tutorial)
- **Enemy**: Cursor ×1 (static), Process Zombie ×2 (static, blocking)
- **Bits**: 10 ×4 = 40
- **Snippet**: +1 (di tengah vertical climb)

### Segmen 3: Network Tray — CP3 → Server B
- **Panjang**: 1500px horizontal
- **Platforming**: Dash timing — gap 120px (butuh dash), 2 platform kecil
- **Gate puzzle**: None
- **Enemy**: Cursor ×1 (static), Fan Vortex ×1 (environmental push)
- **Bits**: 10 ×2 = 20
- **Snippet**: +1 (sebelum Server B)

---

## Enemy Spawn Table

| Enemy | Segmen | Position (x,y) | Type | Behavior Notes |
|---|---|---|---|---|
| Cursor 1 | 1 | (600, 300) | Static | Patrol horizontal 200px |
| Cursor 2 | 1 | (1400, 250) | Static | Patrol horizontal 150px |
| Process Zombie 1 | 1 | (1000, 350) | Static | Diam, blocking narrow path |
| Cursor 3 | 2 | (800, 200) | Static | Patrol vertical 100px |
| Process Zombie 2 | 2 | (1200, 400) | Static | Diam di platform sempit |
| Process Zombie 3 | 2 | (1800, 300) | Static | Diam, blocking dash landing |
| Cursor 4 | 3 | (700, 320) | Static | Patrol horizontal cepat |
| Fan Vortex 1 | 3 | (1100, 280) | Static | Push up, timing jump |

---

## Port Gate Sequence
None — pure tutorial world.

---

## Collectible Map

| Item | Segmen | Position | Reward |
|---|---|---|---|
| Data Bits ×3 | 1 | (300,320), (900,280), (1600,350) | +30 |
| Packet Snippet | 1 | (1200, 300) | +15 TTL |
| Data Bits ×4 | 2 | (400,200), (700,150), (1500,350), (2000,400) | +40 |
| Packet Snippet | 2 | (1000, 250) | +15 TTL |
| Data Bits ×2 | 3 | (500, 310), (1000, 350) | +20 |
| Packet Snippet | 3 | (1300, 320) | +15 TTL |

---

## Secret Path
- **Entrance location**: Segmen 2, vertical climb area — platform tersembunyi di balik tile yang bisa di-dash
- **How to trigger**: Dash ke kanan di tengah vertical climb (koordinat 1100, 300) — tile pecah
- **Path layout**: Koridor pendek 500px, 1 gap sempit, 1 Cursor
- **Reward**: Data Bits +30, Packet Snippet +1

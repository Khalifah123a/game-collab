# WORLD 7: SERVER CORE — Blueprint

## Overview
| Aspek | Detail |
|---|---|
| **Theme** | Server core — jantung infrastruktur, final stand |
| **Biome Visual** | Palet putih bersih `#ffffff` × emas `#ffd700`. Server blade, cooling pipe, hologram biru tipis. Clean aesthetic — kontras dengan world sebelumnya |
| **New Mechanic** | All mechanics combined — final exam |
| **TTL Budget** | 120 unit (drain 2.0/s → ~60s clear) — paling cepat drain |
| **Run Duration** | ~25 menit |
| **Total Segmen** | 3 (boss gauntlet) |
| **Boss?** | ✅ Sundown Virus (Final) |

---

## Segment Blueprint

```
├── Server A (Core Gateway)
├── CP1 ─── [Segmen 1: Sentinel Row]
├── CP2 ─── [Segmen 2: Processor Core]
├── CP3 ─── [Segmen 3: The Heart]
└── Server B (Central Processor) → Boss Arena
```

### Segmen 1: Sentinel Row — CP1 → CP2
- **Panjang**: 3000px
- **Platforming**: Mix semua — walljump, dash timing, Lag Prediction, zero grav section, tile instability
- **Puzzle**: Gate Port 80 → 22 → 443 → 8080 sequential (4 gate beruntun)
- **Enemy**: Core Sentinel ×3 (static), Sundown Spawn ×2 (static)
- **Bits**: 50 ×4 = 200
- **Snippet**: +1

### Segmen 2: Processor Core — CP2 → CP3
- **Panjang**: 3500px — segmen terpanjang di seluruh game
- **Platforming**: Extreme — kombinasi DPI beam, solar flare timing, glitch tile, Packet Loss pit
- **Puzzle**: TOR routing + Port matching + Enc mode switching (all at once)
- **Enemy**: Core Sentinel ×4, Sundown Spawn ×3
- **Bits**: 50 ×5 = 250
- **Snippet**: +1

### Segmen 3: The Heart — CP3 → Server B
- **Panjang**: 2000px
- **Platforming**: Sprint intense — semua hazard overlapping, arena shrink progressif
- **Puzzle**: All gate types mixed — player harus switching port & mode cepat
- **Enemy**: Core Sentinel ×2, Sundown Spawn ×4
- **Bits**: 50 ×3 = 150
- **Snippet**: +1 (sebelum Server B)

---

## Enemy Spawn Table

| Enemy | Segmen | Position | Type | Behavior |
|---|---|---|---|---|
| Core Sentinel ×3 | 1 | (600,300), (1500,200), (2400,250) | Static | Charge line attack |
| Sundown Spawn ×2 | 1 | (1000,200), (2000,300) | Static | Mini virus — all patterns |
| Core Sentinel ×4 | 2 | (500,300), (1200,200), (2200,250), (3000,300) | Static | |
| Sundown Spawn ×3 | 2 | (800,200), (1800,300), (2800,250) | Static | |
| Core Sentinel ×2 | 3 | (600,200), (1600,300) | Static | Final guards |
| Sundown Spawn ×4 | 3 | (400,300), (1000,200), (1400,350), (1800,250) | Static | |

---

## Port Gate Sequence

| Segmen | Port | Mode | Position | Note |
|---|---|---|---|---|
| 1 | 80→22→443→8080 | Mixed | (600,200)→(1200,250)→(1800,300)→(2400,200) | 4 gate sequential |
| 2 | 22 / 443 / 80 / 8080 | Mixed | (700,200)(1400,250)(2100,300)(2800,200) | TOR routing + port |
| 3 | Semua random | Mixed | (400,200)→(900,250)→(1400,300)→(1800,200) | Final random sequence |

---

## Collectible Map

| Item | Segmen | Position | Reward |
|---|---|---|---|
| Bits ×4 | 1 | (400,300), (1000,200), (1800,350), (2600,250) | +200 |
| Snippet | 1 | (2000, 200) | +15 TTL |
| Bits ×5 | 2 | (400,300), (1000,200), (1600,350), (2500,250), (3200,300) | +250 |
| Snippet | 2 | (2800, 200) | +15 TTL |
| Bits ×3 | 3 | (300,200), (1000,350), (1600,250) | +150 |
| Snippet | 3 | (1200, 300) | +15 TTL |

---

## Secret Path
- **Entrance**: Segmen 2 — di area solar flare simulation, dash ke cooling pipe di (3000, 50)
- **Trigger**: Saat flare off window 1s, dash diagonal atas kanan
- **Path**: 300px tunnel — no enemy, pure reward
- **Reward**: Data Bits +200, Packet Snippet ×3 (TTL burst)

---

## Boss Arena: Sundown Virus (Final Boss)

| Aspek | Detail |
|---|---|
| **Lokasi** | Central Processor — ruang putih + fractal corruption hitam-merah |
| **Arena** | 3 platform server rack vertikal + rail dash line |
| **Phase 1** | **Corruption Zone** — Sundown spawn area merah di platform (3s cycle). Hindari + shoot virus orb (homing). Dash antar platform tiap 2s. |
| **Phase 2** | **Shadow Split** — Sundown split jadi 3 shadow. 1 asli (glow merah), 2 palsu (glow abu). Shadow dash attack pattern. Cari yang asli sebelum kena. Hit yang asli 3×. |
| **Phase 3** | **The Collapse** — Semua pattern digabung. Arena shrink perlahan (platform hilang 1/1 tiap 10s). TTL drain ×3. Sundown exposed di tengah tiap 3s — window 0.5s untuk dash ke core. |
| **Kill Condition** | Phase 3: dash ke Sundown core saat exposed (4 hits) |
| **After Defeat** | Sundown melt → Server Core reboot. Cutscene: jaringan pulih, Packet-01 terkirim. |
| **Credits Reward** | New Game+ unlocked. Speedrun mode unlocked. |

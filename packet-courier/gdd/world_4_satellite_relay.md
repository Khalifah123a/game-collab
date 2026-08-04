# WORLD 4: SATELLITE RELAY — Blueprint

## Overview
| Aspek | Detail |
|---|---|
| **Theme** | Luar angkasa — stasiun satelit, orbital path, gravitasi rendah |
| **Biome Visual** | Palet putih neon `#ffffff` × biru es `#0066ff`. Panel surya sebagai platform, bintang latar, antena sebagai obstacle |
| **New Mechanic** | Lag Prediction — platform masa depan terlihat sekilas (ghost), lompat sebelum platform muncul |
| **TTL Budget** | 90 unit (drain 1.4/s → ~64s clear) |
| **Run Duration** | ~15 menit |
| **Total Segmen** | 5 |
| **Boss?** | ✅ Solaris |

---

## Segment Blueprint

```
├── Server A (Launch Station)
├── CP1 ─── [Segmen 1: Low Orbit]
├── CP2 ─── [Segmen 2: Relay Chain]
├── CP3 ─── [Segmen 3: Solar Field]
├── CP4 ─── [Segmen 4: Debris Ring]
├── CP5 ─── [Segmen 5: Geostationary]
└── Server B (Main Satellite) → Boss Arena
```

### Segmen 1: Low Orbit — CP1 → CP2
- **Panjang**: 2000px
- **Platforming**: Low gravity (gravitasi 60%), platform lebar — pengenalan Lag Prediction (ghost muncul 0.5s sebelum platform)
- **Puzzle**: Gate Port 80 (Plain)
- **Enemy**: Orbital Drone ×1 (static), Lag Ghost ×2 (respawn 10s)
- **Bits**: 30 ×2 = 60
- **Snippet**: +1

### Segmen 2: Relay Chain — CP2 → CP3
- **Panjang**: 2500px
- **Platforming**: Platform berjejer — butuh dash chain antar platform (gap 100-120px)
- **Puzzle**: Gate Port 22 (Enc)
- **Enemy**: Orbital Drone ×2, Lag Ghost ×3 (respawn)
- **Bits**: 30 ×3 = 90
- **Snippet**: +1

### Segmen 3: Solar Field — CP3 → CP4
- **Panjang**: 2800px
- **Platforming**: Solar flare periodik (area panas 3s cycle) — timing lari antar shelter
- **Puzzle**: Gate Port 443 (Enc)
- **Enemy**: Solar Sentry ×2 (static), Orbital Drone ×1
- **Bits**: 30 ×4 = 120
- **Snippet**: +1

### Segmen 4: Debris Ring — CP4 → CP5
- **Panjang**: 3000px
- **Platforming**: Platform bergerak — debris satelit hancur sebagai stepping stone
- **Puzzle**: Gate Port 80 → 22 sequence
- **Enemy**: Orbital Drone ×3, Lag Ghost ×2 (respawn)
- **Bits**: 30 ×5 = 150
- **Snippet**: +1

### Segmen 5: Geostationary — CP5 → Server B
- **Panjang**: 1500px
- **Platforming**: Zero gravity section — dash sebagai movement utama, platform mengambang
- **Puzzle**: Gate Port 443 (Enc)
- **Enemy**: Solar Sentry ×1, Lag Ghost ×3 (respawn)
- **Bits**: 30 ×3 = 90
- **Snippet**: +1

---

## Enemy Spawn Table

| Enemy | Segmen | Position | Type | Behavior |
|---|---|---|---|---|
| Orbital Drone | 1 | (1500, 250) | Static | Orbit player, shoot laser |
| Lag Ghost ×2 | 1 | (800,200), (1600,300) | Respawn 10s | Mirror player delayed |
| Orbital Drone ×2 | 2 | (1000,300), (2000,200) | Static | |
| Lag Ghost ×3 | 2 | (500,200), (1500,250), (2200,300) | Respawn 10s | |
| Solar Sentry ×2 | 3 | (1000,200), (2200,250) | Static | Scan beam vertikal |
| Orbital Drone | 3 | (1800, 300) | Static | |
| Orbital Drone ×3 | 4 | (600,300), (1600,200), (2600,250) | Static | |
| Lag Ghost ×2 | 4 | (1200,300), (2400,200) | Respawn 10s | |
| Solar Sentry | 5 | (1000, 250) | Static | |
| Lag Ghost ×3 | 5 | (400,200), (800,300), (1200,250) | Respawn 10s | |

---

## Port Gate Sequence

| Segmen | Port | Mode | Position | Note |
|---|---|---|---|---|
| 1 | 80 | Plain | (1800, 250) | |
| 2 | 22 | Enc | (2200, 300) | |
| 3 | 443 | Enc | (2500, 300) | |
| 4 | 80→22 | Plain→Enc | (1000,200)→(2500,300) | Sequential |
| 5 | 443 | Enc | (1200, 250) | Final gate |

---

## Collectible Map

| Item | Segmen | Position | Reward |
|---|---|---|---|
| Bits ×2 | 1 | (600,300), (1400,250) | +60 |
| Snippet | 1 | (1000, 200) | +15 TTL |
| Bits ×3 | 2 | (400,200), (1200,350), (2000,250) | +90 |
| Snippet | 2 | (1600, 300) | +15 TTL |
| Bits ×4 | 3 | (500,300), (1200,200), (1800,350), (2400,250) | +120 |
| Snippet | 3 | (2000, 200) | +15 TTL |
| Bits ×5 | 4 | (400,300), (1000,200), (1600,350), (2200,250), (2800,300) | +150 |
| Snippet | 4 | (2400, 200) | +15 TTL |
| Bits ×3 | 5 | (300,200), (700,300), (1200,250) | +90 |
| Snippet | 5 | (900, 200) | +15 TTL |

---

## Secret Path
- **Entrance**: Segmen 3 — saat solar flare red, dash ke pojok kanan atas (2400, 100)
- **Trigger**: Dash ke tile khusus saat flare off — window 1.5s
- **Path**: 400px zero-gravity tunnel, 1 Orbital Drone
- **Reward**: Data Bits +100, Packet Snippet ×2

---

## Boss Arena: Solaris

| Aspek | Detail |
|---|---|
| **Lokasi** | Setelah Server B — outer space arena terbuka |
| **Arena** | Zero gravity, 4 platform mengambang di 4 sudut |
| **Phase 1** | 4 panel surya rotate beam. Hindari + dash antar platform. Panel mati 1/1 kena dash |
| **Phase 2** | Panel lepas jadi drone mini (×4). Body core shoot homing laser. Drone harus dihindari sambil dash ke core |
| **Kill Condition** | Hancurkan 4 panel → dash ke core exposed (2×) |
| **Setelah Mati** | Core meledak — drop Lag Prediction module upgrade |

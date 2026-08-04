# WORLD 2: LAN PARTY — Blueprint

## Overview
| Aspek | Detail |
|---|---|
| **Theme** | Router / office network — LED indicator, heatsink, ethernet cable |
| **Biome Visual** | Palet amber `#ffb000` × merah `#ff3333`. PCB track sebagai platform, port ethernet sebagai gate, heatsink sebagai obstacle |
| **New Mechanic** | Encrypted Mode toggle + Port Gate (Port 80 = HTTP, lewat tanpa Enc mode) |
| **TTL Budget** | 70 unit (drain 1.2/s → ~58s clear) |
| **Run Duration** | ~8 menit |
| **Total Segmen** | 4 |
| **Boss?** | ✅ The Censor |

---

## Segment Blueprint

```
├── Server A (ISP Gateway)
├── CP1 ─── [Segmen 1: Switch Hub]
├── CP2 ─── [Segmen 2: Routing Table]
├── CP3 ─── [Segmen 3: Bandwidth Choke]
├── CP4 ─── [Segmen 4: Firewall Perimeter]
└── Server B (Core Router) → Boss Arena
```

### Segmen 1: Switch Hub — CP1 → CP2
- **Panjang**: 2000px horizontal, 1 vertical climb
- **Platforming**: Gap selebar 100px, walljump ×3 sequence
- **Puzzle**: Gate Port 80 (HTTP) pertama — mode Plaintext cukup
- **Enemy**: Data Minion ×2 (respawn 10s — lemah), Firewall Turret ×1 (static)
- **Bits**: 15 ×3 = 45
- **Snippet**: +1
- **Layout ASCII**:
```
               CP2 █
          ▄▄▄▄▄▄▄▄▄▄
         │ DM1  │      DM2
  █ CP1  └──────┘ ┌────┴───
  ████      ██    │   ██       ██
  █ ██──────██────┘───██───G80──██──── Server B
  █        ██           ██     ██
```

### Segmen 2: Routing Table — CP2 → CP3
- **Panjang**: 2500px
- **Platforming**: Platform diagonal naik + turun, 2 dash timing section
- **Puzzle**: Gate Port 22 (SSH) — butuh Encrypted mode + Port 22
- **Enemy**: Ping Spike ×2 (respawn 10s), Firewall Turret ×1 (static)
- **Bits**: 15 ×4 = 60
- **Snippet**: +1

### Segmen 3: Bandwidth Choke — CP3 → CP4
- **Panjang**: 3000px — segmen terpanjang di world ini
- **Platforming**: Koridor sempit (80px lebar), walljump alternating, 3 gap sequential
- **Puzzle**: Gate Port 443 (HTTPS) — butuh Encrypted mode + Port 443
- **Enemy**: Data Minion ×3 (respawn 10s), Fan Vortex ×2 (static)
- **Bits**: 15 ×5 = 75
- **Snippet**: +1

### Segmen 4: Firewall Perimeter — CP4 → Server B
- **Panjang**: 1500px
- **Platforming**: Sprint pendek — multiple gate sequence cepat
- **Puzzle**: Gate Port 80 → 22 → 443 dalam 3 gate berurutan (timer puzzle)
- **Enemy**: Firewall Turret ×2 (static), Ping Spike ×1 (respawn)
- **Bits**: 15 ×3 = 45
- **Snippet**: +1

---

## Enemy Spawn Table

| Enemy | Segmen | Position (x,y) | Type | Behavior Notes |
|---|---|---|---|---|
| Data Minion ×2 | 1 | (700,300), (1300,250) | Respawn 10s | Worm lemah, kejar player lambat |
| Firewall Turret | 1 | (1700, 200) | Static | Rotasi 90°, tembak beam horizontal |
| Ping Spike ×2 | 2 | (900,350), (1800,200) | Respawn 10s | Teleport random Y axis |
| Firewall Turret | 2 | (2100, 300) | Static | Rotasi 90° |
| Data Minion ×3 | 3 | (500,300), (1500,200), (2500,350) | Respawn 10s | Swarm |
| Fan Vortex ×2 | 3 | (1000,250), (2000,300) | Static | Push horizontal |
| Firewall Turret ×2 | 4 | (600,200), (1100,300) | Static | Gate area — timing |
| Ping Spike | 4 | (800, 250) | Respawn 10s | | 

---

## Port Gate Sequence

| Segmen | Port | Mode Required | Position | Note |
|---|---|---|---|---|
| 1 | 80 | Plain (atau Enc) | (1800, 250) | Gate termudah — pengenalan puzzle |
| 2 | 22 | Enc + Port 22 | (2200, 300) | Kombinasi pertama |
| 3 | 443 | Enc + Port 443 | (2800, 320) | Gate standar |
| 4 | 80 → 22 → 443 | Mixed | (400,200)→(800,250)→(1200,300) | Sequential — timing puzzle |

---

## Collectible Map

| Item | Segmen | Position | Reward |
|---|---|---|---|
| Data Bits ×3 | 1 | (400,320), (1000,280), (1600,350) | +45 |
| Packet Snippet | 1 | (1400, 300) | +15 TTL |
| Data Bits ×4 | 2 | (300,200), (800,150), (1500,350), (2100,400) | +60 |
| Packet Snippet | 2 | (1800, 250) | +15 TTL |
| Data Bits ×5 | 3 | (500,310), (1000,350), (1500,200), (2000,300), (2600,350) | +75 |
| Packet Snippet | 3 | (2200, 300) | +15 TTL |
| Data Bits ×3 | 4 | (300,310), (700,350), (1200,300) | +45 |
| Packet Snippet | 4 | (1000, 320) | +15 TTL |

---

## Secret Path
- **Entrance location**: Segmen 3 — di tengah koridor sempit, ada tile berbeda warna
- **How to trigger**: Dash ke tile berbeda di (1200, 250) saat mode Encrypted
- **Path layout**: Labirin kecil 800px, 3 gate port 80→22→443 berurutan (latihan untuk segmen 4)
- **Reward**: Data Bits +60, Packet Snippet ×2

---

## Boss Arena: The Censor

| Aspek | Detail |
|---|---|
| **Lokasi** | Setelah Server B — pintu terbuka ke arena boss |
| **Arena** | Platform sempit vertikal 600×800px, 2 dinding untuk walljump |
| **Phase 1** | Turret arms rotate 180°, tembak beam bergantian (2s interval). Hindari dengan timing Enc mode |
| **Phase 2** | Core terbuka — spawn Firewall Minion ×1 tiap 5s + beam lebih cepat (1.2s interval) |
| **Phase 3** | Rush — beam konstan, core exposed 0.5s tiap 3s. Dash ke core 3× |
| **Kill Condition** | Dash ke port terbuka di core saat exposed (3 hits) |
| **Setelah Mati** | Core meledak — drop Routing Token (progression ke World 3) |

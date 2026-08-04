# WORLD 5: GREAT FIREWALL — Blueprint

## Overview
| Aspek | Detail |
|---|---|
| **Theme** | Tembok firewall raksasa — sensor, censorship, blokade |
| **Biome Visual** | Palet merah `#cc0000` × abu `#4a4a4a`. Dinding beton digital, pagar data berduri, daerah dicoret merah |
| **New Mechanic** | Port Forwarding Puzzle — cocokkan port dengan gate yang benar (80→HTTP, 22→SSH, 443→HTTPS, 8080→Proxy) |
| **TTL Budget** | 100 unit (drain 1.5/s → ~66s clear) |
| **Run Duration** | ~18 menit |
| **Total Segmen** | 6 |
| **Boss?** | ❌ |

---

## Segment Blueprint

```
├── Server A (DMZ)
├── CP1 ─── [Segmen 1: Packet Inspection]
├── CP2 ─── [Segmen 2: Access Control]
├── CP3 ─── [Segmen 3: Deep Inspection]
├── CP4 ─── [Segmen 4: Proxy Tunnel]
├── CP5 ─── [Segmen 5: Censorship Wall]
├── CP6 ─── [Segmen 6: Core Firewall]
└── Server B (Gateway Out)
```

### Segmen 1: Packet Inspection — CP1 → CP2
- **Panjang**: 2000px
- **Platforming**: Gate berjajar — trial & error port matching
- **Puzzle**: 3 gate, cari port yang benar (80, 22, 443) — salah port kena pushback
- **Enemy**: DPI Scanner ×1 (static), Censor Bot ×1 (static)
- **Bits**: 25 ×2 = 50
- **Snippet**: +1

### Segmen 2: Access Control — CP2 → CP3
- **Panjang**: 2400px
- **Platforming**: Platform sempit vertikal — access control list (ACL) sebagai blocker
- **Puzzle**: Gate Port 80 → 22 sequence, timing ketat
- **Enemy**: Rate Limiter ×2 (static), Censor Bot ×2 (static)
- **Bits**: 25 ×3 = 75
- **Snippet**: +1

### Segmen 3: Deep Inspection — CP3 → CP4
- **Panjang**: 2800px
- **Platforming**: DPI beam sweep — hindari sinar biru yang scan area
- **Puzzle**: Gate Port 443 (Enc) + 8080 (Plain)
- **Enemy**: DPI Scanner ×2, Rate Limiter ×1
- **Bits**: 25 ×4 = 100
- **Snippet**: +1

### Segmen 4: Proxy Tunnel — CP4 → CP5
- **Panjang**: 2600px
- **Platforming**: Terowongan sempit — butuh dash & walljump beruntun
- **Puzzle**: Gate Port 8080 (Proxy) — mode Plaintext cukup
- **Enemy**: Censor Bot ×3, DPI Scanner ×1
- **Bits**: 25 ×4 = 100
- **Snippet**: +1

### Segmen 5: Censorship Wall — CP5 → CP6
- **Panjang**: 3000px
- **Platforming**: Walljump intensive — dinding tinggi dengan spike di dasar
- **Puzzle**: Gate Port 22 → 443 → 80 sequence — 3 gate berurutan
- **Enemy**: Rate Limiter ×2, Censor Bot ×2
- **Bits**: 25 ×5 = 125
- **Snippet**: +1

### Segmen 6: Core Firewall — CP6 → Server B
- **Panjang**: 2000px
- **Platforming**: All mechanics — dash, walljump, Lag Prediction, gravitasi normal
- **Puzzle**: Gate Port 443 (Enc) + DPI avoidance
- **Enemy**: DPI Scanner ×2, Rate Limiter ×2
- **Bits**: 25 ×4 = 100
- **Snippet**: +1

---

## Enemy Spawn Table

| Enemy | Segmen | Position | Type | Behavior |
|---|---|---|---|---|
| DPI Scanner | 1 | (1500, 200) | Static | Full-width scan naik-turun |
| Censor Bot | 1 | (800, 300) | Static | Block path, hancur kena dash |
| Rate Limiter ×2 | 2 | (1000,200), (1800,300) | Static | Traffic light rotasi |
| Censor Bot ×2 | 2 | (600,350), (2000,250) | Static | |
| DPI Scanner ×2 | 3 | (1000,200), (2200,250) | Static | Beam sweep |
| Rate Limiter | 3 | (1800, 300) | Static | |
| Censor Bot ×3 | 4 | (500,300), (1400,200), (2200,350) | Static | |
| DPI Scanner | 4 | (1800, 250) | Static | |
| Rate Limiter ×2 | 5 | (1000,200), (2400,300) | Static | |
| Censor Bot ×2 | 5 | (600,250), (2000,350) | Static | |
| DPI Scanner ×2 | 6 | (800,200), (1600,250) | Static | Final push |
| Rate Limiter ×2 | 6 | (400,300), (1200,200) | Static | |

---

## Port Gate Sequence

| Segmen | Port | Mode | Position | Note |
|---|---|---|---|---|
| 1 | 80 / 22 / 443 | Mixed | (600,300)(1200,250)(1800,200) | Trial & error — cari benar |
| 2 | 80 → 22 | Plain→Enc | (800,200)→(2000,300) | Sequential |
| 3 | 443 + 8080 | Enc→Plain | (1200,250)→(2400,300) | Dual gate |
| 4 | 8080 | Plain | (2200, 300) | |
| 5 | 22→443→80 | Enc→Enc→Plain | (800,200)→(1600,250)→(2600,300) | Triple sequence |
| 6 | 443 | Enc | (1600, 250) | Final gate |

---

## Collectible Map

| Item | Segmen | Position | Reward |
|---|---|---|---|
| Bits ×2 | 1 | (500,300), (1500,250) | +50 |
| Snippet | 1 | (1000, 200) | +15 TTL |
| Bits ×3 | 2 | (400,200), (1200,350), (2000,250) | +75 |
| Snippet | 2 | (1600, 300) | +15 TTL |
| Bits ×4 | 3 | (500,300), (1000,200), (1800,350), (2400,250) | +100 |
| Snippet | 3 | (2000, 200) | +15 TTL |
| Bits ×4 | 4 | (400,300), (1000,200), (1800,350), (2400,250) | +100 |
| Snippet | 4 | (2000, 200) | +15 TTL |
| Bits ×5 | 5 | (500,300), (1000,200), (1600,350), (2200,250), (2800,300) | +125 |
| Snippet | 5 | (2400, 200) | +15 TTL |
| Bits ×4 | 6 | (400,300), (1000,200), (1600,350), (1800,250) | +100 |
| Snippet | 6 | (1200, 300) | +15 TTL |

---

## Secret Path
- **Entrance**: Segmen 3 — saat DPI beam di posisi tertinggi, dash ke celah sempit di (2600, 100)
- **Trigger**: Timing — beam naik, dash 45° ke atas kanan dalam 0.5s window
- **Path**: 500px tunnel dengan 2 Censor Bot + 1 gate Port 8080
- **Reward**: Data Bits +120, Packet Snippet ×2

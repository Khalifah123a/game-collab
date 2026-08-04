# WORLD 3: FIBER DEEP — Blueprint

## Overview
| Aspek | Detail |
|---|---|
| **Theme** | Fiber optic cable bawah laut — gelap, tekanan air, data leak |
| **Biome Visual** | Palet cyan `#00ffff` × ungu `#2d004d`. Kabel fiber bercahaya sebagai jalur, gelembung data, reruntuhan kabel usang |
| **New Mechanic** | TCP Handshake checkpoint — berhenti 0.5s di node untuk aktivasi, bonus +10 TTL |
| **TTL Budget** | 80 unit (drain 1.3/s → ~61s clear) |
| **Run Duration** | ~12 menit |
| **Total Segmen** | 5 |
| **Boss?** | ❌ |

---

## Segment Blueprint

```
├── Server A (Shore Station)
├── CP1 ─── [Segmen 1: Cable Surface]
├── CP2 ─── [Segmen 2: Coral Data]
├── CP3 ─── [Segmen 3: Pressure Zone]
├── CP4 ─── [Segmen 4: Deep Trench]
├── CP5 ─── [Segmen 5: The Abyss]
└── Server B (Optic Node)
```

### Segmen 1: Cable Surface — CP1 → CP2
- **Panjang**: 1800px
- **Platforming**: Platform stabil, pengenalan fiber cable sebagai rail
- **Puzzle**: None — tutorial TCP Handshake (node di tengah)
- **Enemy**: Packet Jelly ×1 (static), Data Leech ×1 (static — intro)
- **Bits**: 20 ×2 = 40
- **Snippet**: +1

### Segmen 2: Coral Data — CP2 → CP3
- **Panjang**: 2200px
- **Platforming**: Platform tidak stabil — bergerak naik/turun oleh arus, 2 walljump section
- **Puzzle**: TCP Handshake ×1 node
- **Enemy**: Packet Jelly ×2 (static), Kelp Cable ×2 (environmental)
- **Bits**: 20 ×3 = 60
- **Snippet**: +1

### Segmen 3: Pressure Zone — CP3 → CP4
- **Panjang**: 2800px
- **Platforming**: Gravity shift ringan, platform pecah periodik (Packet Loss pit)
- **Puzzle**: TCP Handshake ×1 node + Gate Port 80 (Plain)
- **Enemy**: Data Leech ×2 (static), Packet Jelly ×3 (static)
- **Bits**: 20 ×4 = 80
- **Snippet**: +1

### Segmen 4: Deep Trench — CP4 → CP5
- **Panjang**: 3000px
- **Platforming**: Sempit, gelap — visibility rendah, platform muncul-hilang
- **Puzzle**: TCP Handshake ×2 node + Gate Port 22 (Enc)
- **Enemy**: Data Leech ×3, Packet Jelly ×2, Kelp Cable ×1
- **Bits**: 20 ×5 = 100
- **Snippet**: +1

### Segmen 5: The Abyss — CP5 → Server B
- **Panjang**: 2000px
- **Platforming**: Sprint final — Packet Loss pit dominan, 3 dash timing section
- **Puzzle**: Gate Port 443 (Enc)
- **Enemy**: Data Leech ×1, Kelp Cable ×2
- **Bits**: 20 ×3 = 60
- **Snippet**: +1

---

## Enemy Spawn Table

| Enemy | Segmen | Position (x,y) | Type | Behavior |
|---|---|---|---|---|
| Packet Jelly | 1 | (900, 200) | Static | Floating, expand 3s cycle |
| Data Leech | 1 | (1400, 300) | Static | Chase player lambat |
| Packet Jelly ×2 | 2 | (600,150), (1600,250) | Static | |
| Kelp Cable ×2 | 2 | (1000,350), (1800,200) | Static | Swing periodic block |
| Data Leech ×2 | 3 | (800,300), (2000,250) | Static | |
| Packet Jelly ×3 | 3 | (400,100), (1400,300), (2400,150) | Static | |
| Data Leech ×3 | 4 | (500,300), (1500,200), (2500,350) | Static | |
| Packet Jelly ×2 | 4 | (1000,150), (2200,250) | Static | |
| Kelp Cable | 4 | (1800, 300) | Static | |
| Data Leech | 5 | (1500, 250) | Static | Final chase |
| Kelp Cable ×2 | 5 | (800,200), (1600,350) | Static | |

---

## Port Gate Sequence

| Segmen | Port | Mode | Position | Note |
|---|---|---|---|---|
| 3 | 80 | Plain | (2000, 300) | Gate pertama di world ini |
| 4 | 22 | Enc | (1500, 250) | Dark area — susah lihat gate |
| 5 | 443 | Enc | (1600, 300) | Final gate |

---

## Collectible Map

| Item | Segmen | Position | Reward |
|---|---|---|---|
| Bits ×2 | 1 | (400,300), (1200,350) | +40 |
| Snippet | 1 | (1000, 250) | +15 TTL |
| Bits ×3 | 2 | (400,200), (1200,300), (1800,250) | +60 |
| Snippet | 2 | (1500, 200) | +15 TTL |
| Bits ×4 | 3 | (400,300), (1000,200), (1800,350), (2400,250) | +80 |
| Snippet | 3 | (2000, 300) | +15 TTL |
| Bits ×5 | 4 | (500,300), (1000,200), (1500,350), (2000,250), (2600,300) | +100 |
| Snippet | 4 | (2200, 200) | +15 TTL |
| Bits ×3 | 5 | (400,300), (1000,250), (1600,350) | +60 |
| Snippet | 5 | (1200, 300) | +15 TTL |

---

## Secret Path
- **Entrance**: Segmen 4 — di platform paling gelap, dasbor ke kiri saat mode Encrypted
- **Trigger**: Encrypted dash di (700, 350) — tembus tile palsu
- **Path**: Lorong 600px dengan 2 Packet Jelly + 1 TCP node
- **Reward**: Data Bits +80, Packet Snippet ×2

# WORLD 6: DARK WEB — Blueprint

## Overview
| Aspek | Detail |
|---|---|
| **Theme** | Dark web — anonim, glitch, distorsi realitas |
| **Biome Visual** | Palet hitam `#000000` × ungu neon `#ff00ff`. Glitchcore — tile bergerak sendiri, error artifact, scrambled texture |
| **New Mechanic** | TOR Routing — minimap terbatas (hanya 1 hop ke depan), jalur muncul saat dekat, backtrack rawan |
| **TTL Budget** | 110 unit (drain 1.6/s → ~68s clear) |
| **Run Duration** | ~20 menit |
| **Total Segmen** | 6 |
| **Boss?** | ✅ Phantom Root |

---

## Segment Blueprint

```
├── Server A (TOR Entry Node)
├── CP1 ─── [Segmen 1: Onion Layer 1]
├── CP2 ─── [Segmen 2: Onion Layer 2]
├── CP3 ─── [Segmen 3: Onion Layer 3]
├── CP4 ─── [Segmen 4: Hidden Wiki]
├── CP5 ─── [Segmen 5: Silk Route]
├── CP6 ─── [Segmen 6: The Core]
└── Server B (Exit Node) → Boss Arena
```

### Segmen 1: Onion Layer 1 — CP1 → CP2
- **Panjang**: 2000px
- **Platforming**: Platform dasar, visibility 60% — area gelap berselang
- **Puzzle**: Gate Port 80 (Plain) — TOR map hanya tunjuk 1 hop
- **Enemy**: Glitch Worm ×2 (respawn 10s), Silhouette ×1 (static)
- **Bits**: 40 ×2 = 80
- **Snippet**: +1

### Segmen 2: Onion Layer 2 — CP2 → CP3
- **Panjang**: 2500px
- **Platforming**: Tile berubah bentuk periodik — platform berganti tiap 4s
- **Puzzle**: Gate Port 22 (Enc) — 2 hop di TOR map
- **Enemy**: Glitch Worm ×3 (respawn), Silhouette ×2
- **Bits**: 40 ×3 = 120
- **Snippet**: +1

### Segmen 3: Onion Layer 3 — CP3 → CP4
- **Panjang**: 2800px
- **Platforming**: Glitch heavy — tile kadang hilang total, kadang solid
- **Puzzle**: Gate Port 443 (Enc) — 3 hop map
- **Enemy**: Ransomware Core ×1 (static — lock gate), Glitch Worm ×2
- **Bits**: 40 ×4 = 160
- **Snippet**: +1

### Segmen 4: Hidden Wiki — CP4 → CP5
- **Panjang**: 2600px
- **Platforming**: Maze-like — banyak cabang, hanya 1 benar
- **Puzzle**: TOR routing — pilih jalur yang benar (3 cabang, 2 dead end)
- **Enemy**: Silhouette ×3, Glitch Worm ×3 (respawn)
- **Bits**: 40 ×4 = 160
- **Snippet**: +1

### Segmen 5: Silk Route — CP5 → CP6
- **Panjang**: 3000px
- **Platforming**: Sprint panjang dengan fake platform (tile yang tampak solid tapi palsu)
- **Puzzle**: Gate Port 8080 (Plain) + 22 (Enc) sequence
- **Enemy**: Ransomware Core ×2 (static), Glitch Worm ×4 (respawn)
- **Bits**: 40 ×5 = 200
- **Snippet**: +1

### Segmen 6: The Core — CP6 → Server B
- **Panjang**: 2000px
- **Platforming**: All glitch — tile berganti tiap 2s, fake platform dominan
- **Puzzle**: Gate Port 443 (Enc) — final push
- **Enemy**: Silhouette ×2, Glitch Worm ×3 (respawn)
- **Bits**: 40 ×3 = 120
- **Snippet**: +1

---

## Enemy Spawn Table

| Enemy | Segmen | Position | Type | Behavior |
|---|---|---|---|---|
| Glitch Worm ×2 | 1 | (600,300), (1400,250) | Respawn 10s | Gerak erratic, split |
| Silhouette | 1 | (1200, 200) | Static | Mirror player, touch = swap |
| Glitch Worm ×3 | 2 | (400,300), (1200,200), (2000,250) | Respawn 10s | |
| Silhouette ×2 | 2 | (800,200), (1800,300) | Static | |
| Ransomware Core | 3 | (2000, 300) | Static | Lock gate, spawn minion |
| Glitch Worm ×2 | 3 | (800,250), (1600,300) | Respawn 10s | |
| Silhouette ×3 | 4 | (500,200), (1400,300), (2200,250) | Static | |
| Glitch Worm ×3 | 4 | (800,300), (1600,200), (2400,250) | Respawn 10s | |
| Ransomware Core ×2 | 5 | (1000,200), (2500,300) | Static | |
| Glitch Worm ×4 | 5 | (400,300), (1200,200), (2000,250), (2800,300) | Respawn 10s | |
| Silhouette ×2 | 6 | (600,200), (1600,300) | Static | |
| Glitch Worm ×3 | 6 | (400,300), (1000,250), (1500,200) | Respawn 10s | |

---

## Port Gate Sequence

| Segmen | Port | Mode | Position | Note |
|---|---|---|---|---|
| 1 | 80 | Plain | (1600, 250) | 1 hop |
| 2 | 22 | Enc | (2000, 300) | 2 hop |
| 3 | 443 | Enc | (2400, 300) | 3 hop |
| 4 | 80 / 22 / 443 | Mixed | (600,200)(1400,250)(2200,300) | Pilih 1 dari 3 cabang |
| 5 | 8080→22 | Plain→Enc | (1600,200)→(2600,300) | Sequence |
| 6 | 443 | Enc | (1600, 250) | Final |

---

## Collectible Map

| Item | Segmen | Position | Reward |
|---|---|---|---|
| Bits ×2 | 1 | (500,300), (1500,250) | +80 |
| Snippet | 1 | (1000, 200) | +15 TTL |
| Bits ×3 | 2 | (400,200), (1200,350), (2000,250) | +120 |
| Snippet | 2 | (1600, 300) | +15 TTL |
| Bits ×4 | 3 | (500,300), (1000,200), (1800,350), (2400,250) | +160 |
| Snippet | 3 | (2000, 200) | +15 TTL |
| Bits ×4 | 4 | (400,300), (1000,200), (1800,350), (2400,250) | +160 |
| Snippet | 4 | (2000, 200) | +15 TTL |
| Bits ×5 | 5 | (500,300), (1000,200), (1600,350), (2200,250), (2800,300) | +200 |
| Snippet | 5 | (2400, 200) | +15 TTL |
| Bits ×3 | 6 | (400,300), (1000,250), (1600,200) | +120 |
| Snippet | 6 | (1200, 300) | +15 TTL |

---

## Secret Path
- **Entrance**: Segmen 4 — di cabang dead end, ada tile yang bisa di-dash (1600, 350)
- **Trigger**: Dash ke tile di dead end kanan — tembus ke jalur rahasia
- **Path**: 600px tunnel gelap, 1 Ransomware Core (lock gate), 1 Silhouette
- **Reward**: Data Bits +150, Packet Snippet ×2

---

## Boss Arena: Phantom Root

| Aspek | Detail |
|---|---|
| **Lokasi** | Setelah Server B — glitch dimension |
| **Arena** | Arena melayang — 5 platform acak, berubah tiap 5s |
| **Phase 1** | Phantom spawn Glitch Worm swarm (4 ekor tiap 6s). Hindari + cari core yang muncul sekilas |
| **Phase 2** | Arena collapse — platform menyempit, Phantom chase (insta-kill). Dash ke core di antara spawn. TOR map hilang — butuh hafal arena |
| **Kill Condition** | 3 hit ke core di antara spawn worm |
| **Setelah Mati** | Drop Private Key fragment (progression ke World 7) |

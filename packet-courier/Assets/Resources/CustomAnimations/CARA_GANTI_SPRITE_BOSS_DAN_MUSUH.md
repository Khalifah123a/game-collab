# Panduan Ganti Sprite & Animasi Boss / Musuh (Tanpa Coding)

Folder ini berisi template `SpriteAnimationProfile` yang otomatis dibaca oleh game saat runtime.

## Cara 1 — Paling Cepat (Otomatis untuk SEMUA Stage di Game)
1. Import gambar PNG / Sprite Sheet boss atau musuh ke Unity (set `Texture Type = Sprite (2D and UI)`, `Filter Mode = Point` jika pixel art, dan untuk Boss ground disarankan `Pivot = Bottom`).
2. Di Unity Project Window, buka folder:
   `Assets/Resources/CustomAnimations/`
3. Klik file profile yang ingin diganti:
   - **Musuh Darat (Patrol):** `Enemy_Patrol`
   - **Musuh Terbang (Drone):** `Enemy_Flying`
   - **Musuh Glitch Phantom:** `Enemy_GlitchPhantom`
   - **Musuh Turret Firewall:** `Enemy_FirewallTurret`
   - **Boss Default (semua boss jika spesifik belum diisi):** `Boss_Default`
   - **Boss World 1:** `Boss_World1_Sentinel`
   - **Boss World 2:** `Boss_World2_Censor`
   - **Boss World 4:** `Boss_World4_Solaris`
   - **Boss World 5 (Final Boss):** `Boss_World5_Final`
4. Di Inspector:
   - **Jika baru punya 1 gambar diam:** drag langsung ke kolom **Sprite Diam (Default)**.
   - **Jika punya frame animasi:** buka state (`Idle`, `Walk`, `Attack`, `Slash`, `Dash`, `Jump`, `Slam`, `PhaseShift`, `Hurt`, `Death`), lalu **drag & drop banyak sprite sekaligus (atau 1 Sprite Sheet)** ke kotak drop `"Drag & Drop Banyak Sprite / Sprite Sheet ke Sini"`.
   - Tidak wajib mengisi semua state! State yang kosong otomatis fallback ke `Idle` / `Walk` / `Attack`.
   - Jika arah hadap terbalik, centang **Balik Arah (Invert Facing)**.
   - Jika ukuran terlalu besar/kecil atau kaki melayang, atur **Skala Gambar** dan **Offset Posisi (X, Y)**.
5. Tekan **Play** — semua Boss & Musuh di game langsung memakai sprite & animasi barumu!

## Cara 2 — Pasang Sendiri Component di GameObject / Prefab
1. Pilih prefab siap pakai di `Assets/_Prefabs/`:
   - `Enemy_Patrol.prefab`
   - `Enemy_Flying.prefab`
   - `Boss_Grounded.prefab`
   Atau buat GameObject sendiri lalu **Add Component**:
   - `PacketCourier/Animation/Modular Sprite Animator`
   - `PacketCourier/Animation/Enemy Animation Bridge` (untuk musuh) atau `Boss Animation Bridge` (untuk boss)
2. Klik tombol **+ Preset Musuh** atau **+ Preset Boss 3-Fase** di Inspector `ModularSpriteAnimator`, lalu drag sprite langsung di komponen itu!

## Bonus: Animasi Berbeda per Fase Boss (Fase 1, 2, 3)
Untuk Boss 3-Fase (`Boss_Grounded`), kamu bisa menambah state dengan akhiran `_P2` atau `_P3` (contoh: `Idle_P2`, `Slash_P2`, `Idle_P3`, `Slash_P3`). Jika tidak dibuat, Boss otomatis memakai state utama (`Idle`, `Slash`, dst.).

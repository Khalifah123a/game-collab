### Task 11: Hapus PrototypeBootstrapper + verifikasi akhir

**Files:**
- Delete: `Assets/_Scripts/PrototypeBootstrapper.cs`

**Interfaces:**
- Semua class yang dibutuhkan runtime sudah tersedia: `CameraFollow` (Task 7), `InputManager` (Setup Scene), HUD & LevelManager (Setup Scene), prefab (Task 9–10). Tidak ada referensi ke `PrototypeBootstrapper` dari file lain.

- [ ] **Step 1: Hapus file**

Hapus `Assets/_Scripts/PrototypeBootstrapper.cs` lewat editor Unity (kanan-klik → Delete) atau lewat file manager, lalu Refresh AssetDatabase.

- [ ] **Step 2: Verify compile global**

1. Buka Unity → biarkan kompilasi selesai → Console kosong dari error.
2. Cari sisa referensi: `grep -r "PrototypeBootstrapper" Assets --include=*.cs` → tidak ada hasil (kecuali di docs/).

- [ ] **Step 3: Playtest end-to-end (langkah manual user)**

Di scene kosong:
1. `Tools > Packet Courier > Generate Prefabs` (sekali).
2. `Tools > Packet Courier > Setup Scene`.
3. Drag `Player` prefab ke scene (posisi mis. (0, 2)).
4. Bangun level sederhana: drag `Ground` beberapa kali (scale lebar), `Platform_OneWay`, `Spike`, `Enemy_Patrol`, `Laser_Beam`, `Platform_Moving`, `DataBit`, `Snippet`, `ServerB_Goal`.
5. Play:
   - Gerak kiri/kanan → animasi Run; berhenti → Idle.
   - Tahan Left Shift → lari lebih cepat, animasi lebih cepat.
   - Kena spike/enemy/laser → TTL turun (HUD_TTL berkurang), tidak crash.
   - Naik `Platform_Moving` → ikut terbawa.
   - Sentuh `ServerB_Goal` → segmen selesai (pesan dari LevelManager/GameManager).
6. Kalau `GameManager` belum ada di scene, tambahkan via Add Component → `GameManager` (DontDestroyOnLoad) supaya `AddDataBits`/`OnSegmentComplete` tidak null. Catatan: tanpa GameManager game tetap jalan, collectible hanya tidak tercatat.

- [ ] **Step 4: Update design doc status**

Di `docs/superpowers/specs/2026-08-04-custom-world-prefabs-design.md`, ubah baris status menjadi:

```markdown
Status: Approved (bagian 1–4 disetujui user) — Implemented 2026-08-04
```

---

## Self-Review

**1. Spec coverage:**
- World manual di scene → Task 9–11 (prefab + Setup Scene + hapus bootstrapper). ✓
- Player sprite kurir + animasi Idle/Run → Task 2, 8, 9 (controller + clip + prefab). ✓
- Sprint Left Shift ×1.6, animasi ×1.8 → Task 1, 3. ✓
- Spikes pakai `Hazard_Spike` → Task 9 (`CreateSpikePrefab`). ✓
- Enemy patroli + chase + damage 40 cooldown 1s → Task 4, 9. ✓
- Laser nyala-mati + damage 30 → Task 5, 9. ✓
- Platform moving horizontal → Task 6, 9. ✓
- ServerB_Goal finish → Task 10. ✓
- Semua jadi prefab → Task 9, 10 (12 prefab). ✓
- `EditorGenerator.cs` + menu `Tools > Packet Courier > Generate Prefabs` → Task 8–10. ✓
- Idempotent → `LoadOrCreateSprite` skip jika ada; `SaveAsPrefabAsset` overwrite; Setup Scene `Ensure*` guard. ✓
- Null-guard semua komponen baru → Task 4, 5, 2 (warn sekali), 3 (`_input == null`). ✓
- Bootstrapper dihapus → Task 7 (CameraFollow dipindah dulu), Task 11. ✓
- NRE `InputManager.Instance` di scene manual → Task 3 (null-guard `_input`) + Task 10 (Setup Scene otomatis bikin InputManager). ✓

**2. Placeholder scan:** Semua step berisi kode konkret; tidak ada "TBD"/"implement later". Metode yang dipakai antar task konsisten (`LoadWalkSprite`, `LoadOrCreateSprite`, `SavePrefab`, `EnsureFolder`).

**3. Type consistency:**
- `UpdateState(bool, float, float)` di Task 2 ↔ dipanggil Task 3 Step 7. ✓
- `sprintHeld` Task 1 ↔ Task 3 Step 7. ✓
- `LoadWalkSprite(int)`, `LoadOrCreateSprite(name,w,h,color,ppu)` Task 8 ↔ Task 9–10. ✓
- `CreatePlayerAnimatorController()` Task 9 Step 1 ↔ dipanggil Task 9 Step 5. ✓
- `SetupScene()` Task 10 Step 4 ↔ menu item Task 10 Step 4. ✓
- `SavePrefab(go, path)` Task 9 Step 4 ↔ semua method Task 9–10. ✓

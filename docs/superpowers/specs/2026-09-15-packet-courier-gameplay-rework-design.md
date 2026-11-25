# Packet Courier: Rancangan Perbaikan Gameplay

Tanggal: 2026-09-15
Status: Disetujui pengguna untuk implementasi setelah didokumentasikan di Obsidian. Implementasi dasar tersedia; angka balancing dan rincian lintasan tetap memerlukan playtest. Hasil aktual dicatat dalam laporan implementasi 2026-09-16.

## 1. Tujuan

Membuat World 1-4 menjadi pengalaman platformer yang utuh dengan durasi dan kesulitan yang meningkat: gerakan terbaca lewat animasi, tantangan membutuhkan kombinasi gerakan, sumber daya terlihat, dan serangan musuh dapat dipahami serta dihindari. Aset final sedang dibuat; fondasi gameplay dan pemasangan animasi dapat dikerjakan dengan aset sementara.

Lingkup rancangan mencakup World 1-4: 3, 4, 5, dan 5 segmen, total 17 segmen. Implementasi kelak dimulai dari fondasi bersama dan World 1, kemudian World 2, 3, dan 4 sebagai bagian dari lingkup yang sama. World 5-7 berada di luar revisi ini.

## 2. Bukti Dari Proyek

| Area | Kondisi yang ditemukan | Implikasi |
| --- | --- | --- |
| Animasi | `PlayerAnimator.controller` memiliki Idle/Run dengan motion kosong. Penggerak animasi hanya menerima gerak, kecepatan, dan arah. | Perlu klip terpasang serta state udara dan dash. |
| Level | Tiga layout World 1 memiliki lintasan utama sekitar 46-48 unit dan banyak tanah datar. | Konten terlalu singkat; menambah dekorasi saja tidak menambah tantangan. |
| Dash | `PlayerController` mematikan collider ketika dash. Default kecepatan kode 700, sedangkan prefab 45 dengan durasi 0.15 detik. | Penghalang dapat ditembus dan nilai gerakan tidak memiliki acuan yang konsisten. Angka default kode bukan bukti kecepatan aktual di scene. |
| TTL | `PlayerHealth` mengurangi TTL terhadap waktu dan damage. Prefab memakai maksimum 100 dan drain 1/detik. | Sistem nyawa/waktu sudah ada, tetapi perlu integrasi dan feedback yang jelas. |
| HUD | Wadah HUD_TTL memakai Transform biasa, bukan RectTransform yang membentang pada canvas. | Kandidat masalah posisi HUD; perlu pembuktian di Play Mode. |
| Stamina | Belum ditemukan sistem stamina. | Penambahannya merupakan perubahan desain. |
| Musuh | Patroli/mengejar dengan badan padat, trigger damage 40, cooldown 1 detik. Tidak ada state serangan atau jeda. | Musuh terus menekan player dan damage terasa seperti akibat bersentuhan. |
| Checkpoint | Aktivasi memberi TTL, tetapi tidak mencatat posisi respawn. | Belum berfungsi sebagai checkpoint pemulihan progres. |
| Progres | Generator dan build yang diperiksa memuat tiga segmen World 1. Pengguna melaporkan World 2-4 juga terlalu pendek dan mudah. | Laporan pengguna menjadi kebutuhan desain World 2-4. Lokasi/versi level yang dimainkan perlu dicocokkan sebelum mengedit layout; durasi dan ukuran aktualnya belum terverifikasi. |

Temuan berasal dari pembacaan kode, prefab, scene, dan dokumen. Belum ada validasi perilaku langsung di Play Mode.

## 3. Pilihan Sistem Sumber Daya

| Pilihan | Aturan | Konsekuensi |
| --- | --- | --- |
| A: TTL + stamina, direkomendasikan | TTL adalah nyawa/waktu. Stamina membiayai dash dan sprint. | Mempertahankan identitas awal sambil memberi batas penggunaan gerakan kuat. |
| B: TTL + cooldown saja | TTL adalah nyawa/waktu. Dash dibatasi cooldown dan jatah di udara. | Lebih sederhana dan lebih bebas; stamina tidak diperlukan. |
| C: HP + TTL + stamina | HP menerima serangan, TTL mengatur batas waktu, stamina membiayai aksi. | Tiga penyebab kegagalan/batas aksi harus diajarkan dan diseimbangkan; menyimpang lebih jauh dari konsep awal. |

Rancangan berikut memakai pilihan A sebagai usulan. Penyebutan health dalam UI dijelaskan sebagai TTL, bukan menambahkan bar HP kedua. Jika pilihan C dipilih, aturan damage, healing, kematian, dan hadiah harus dirancang ulang sebelum implementasi.

Semua angka berikut adalah titik awal playtest, bukan hasil pengukuran atau keseimbangan final.

## 4. Gerakan, Dash, dan Stamina

### Aturan gerakan

- Pertahankan jalan, sprint, jump, double jump, walljump, coyote time, dan jump buffer yang sudah ada.
- Jump, double jump, dan walljump tidak memakai stamina agar rute platforming dasar tetap tersedia.
- Pertahankan satu dash di udara, pulih ketika mendarat. Pemulihan stamina di udara tidak memberikan dash udara tambahan.
- Dash berhenti sebelum dinding atau gate tertutup. Collider tubuh tetap aktif; pemeriksaan lintasan dash mencegah menembus penghalang tipis.
- Dash tidak otomatis kebal damage. Badan musuh tidak menghalangi gerak, tetapi hitbox serangan aktif tetap mengenai player saat dash. Kebal sementara berasal dari aturan setelah terkena serangan.
- Tile rahasia yang dapat dihancurkan harus punya aturan eksplisit; dinding biasa tetap menghalangi.
- Jadikan jarak dan durasi dash satu acuan: kecepatan dihitung dari jarak/durasi. Inspector, prefab, generator, dan upgrade memakai acuan yang sama.
- Usulan awal jarak plaintext 7 unit, encrypted 5 unit, durasi 0.15 detik. Validasi skala collider dan jarak lompatan sebelum membangun gap.

### Stamina usulan

| Parameter | Nilai awal |
| --- | --- |
| Kapasitas | 100 |
| Biaya dash | 25 per aktivasi berhasil |
| Biaya sprint | 15/detik, hanya ketika grounded dan benar-benar bergerak |
| Regenerasi | 35/detik setelah 0.75 detik tanpa pemakaian |
| Cooldown dash | Plaintext 0.4 detik; encrypted 0.55 detik |

Dash ditolak tanpa biaya jika stamina kurang, cooldown aktif, atau jatah udara habis. Saat sprint kehabisan stamina, player kembali ke kecepatan jalan; sprint perlu dilepas lalu ditekan lagi agar tidak berulang hidup-mati ketika stamina mulai pulih. Regenerasi berjalan saat berjalan biasa atau melompat, tetapi berhenti ketika game dijeda atau player mati. Percobaan dash yang ditolak tidak mereset jeda regenerasi.

Tujuannya memungkinkan beberapa dash beruntun dan keputusan kapan sprint. Panjang level tetap harus bermakna tanpa bergantung pada stamina untuk memperlambat player.

## 5. TTL, Damage, dan Tampilan

- TTL awal mengikuti world: 100/150/170/190 untuk World 1/2/3/4, drain awal 1/detik selama gameplay aktif. Ini usulan untuk durasi baru; mengganti budget/drain lama pada empat GDD. TTL tidak berkurang di menu, pause, transisi, atau setelah mati.
- TTL 0 memicu kematian satu kali. Damage mengurangi TTL, bukan langsung mengganti scene tanpa feedback.
- Packet Snippet memberi +15 TTL, dibatasi maksimum. Checkpoint memberi +10 hanya pada aktivasi pertama dalam percobaan segmen tersebut.
- Serangan musuh dasar memberi 15 TTL damage, diikuti kebal serangan selama 0.8 detik. Drain waktu tetap berjalan saat kebal.
- Knockback berupa impuls singkat dengan kendali terbatas maksimal 0.15 detik; player kemudian bisa menjauh. Kontak badan tidak boleh mengunci player ke dinding.
- Bahaya biasa di World 1 memberi damage terukur. Jatuh keluar level tetap mengakhiri percobaan dan menggunakan checkpoint/retry.
- Aturan spike plaintext mati seketika yang ada saat ini perlu diubah untuk tutorial World 1. Bahaya fatal harus berbeda secara visual dari bahaya biasa.
- Di world berikutnya, mode encrypted mengurangi damage sesuai aturan shield; tidak meniadakan drain waktu atau kematian karena jatuh keluar level.

HUD: TTL di kiri atas, stamina tepat di bawahnya, status dash berupa ikon yang membedakan siap/cooldown/jatah udara habis. Port dan encrypted baru ditampilkan saat mekaniknya dibuka. TTL rendah ditandai warna serta angka/ikon agar tidak bergantung pada warna saja. Jangan menampilkan tiga bar dengan fungsi yang tumpang tindih.

Kriteria: pada 1280x720, 1920x1080, dan 2560x1080, bar serta teks terlihat, tidak terpotong, dan tidak bertumpuk. Jika input sentuh menjadi lingkup berikutnya, layout mobile diuji tersendiri.

## 6. Animasi dan Penyerahan Aset

| State | Pemicu | Pemutaran awal |
| --- | --- | --- |
| Idle | Grounded, tidak bergerak | Loop, 4-6 frame, 6-8 fps |
| Walk/Run | Grounded, bergerak | Loop, 6-8 frame, 10-12 fps; sprint menaikkan tempo |
| Jump | Lompatan tanah atau walljump | 2-4 frame, sekali, lanjut Fall saat turun |
| DoubleJump | Lompatan udara diterima | 4-6 frame, sekali; kemudian pose udara |
| Fall | Di udara dengan kecepatan vertikal negatif | 2-4 frame, loop/pose tahan |
| Dash | Dash aktif | 3-5 frame dalam durasi dash |
| Land | Baru menyentuh tanah | 2-3 frame singkat, dapat dipotong gerakan baru |
| Hurt | Damage diterima | Flash/reaksi singkat; tidak menahan input sepanjang klip |
| Death | TTL habis atau jatuh fatal | Sekali, lalu Game Over |

Prioritas visual: Death > Hurt singkat > Dash > DoubleJump > Jump/Fall > Land > Walk > Idle. Selesainya animasi tidak menentukan fisika gerak. Event double jump hanya dipicu pada lompatan yang benar-benar diterima, bukan setiap tombol ditekan.

Untuk pembuat aset: gunakan kanvas frame konsisten, pivot kaki konsisten, arah hadap kanan sebagai standar, latar transparan, dan ukuran piksel yang disepakati mengikuti sprite player final. Pisahkan klip per aksi, sertakan urutan frame, dan jaga posisi kaki agar sprite tidak bergeser. Hitbox gameplay tidak mengikuti perubahan pose tiap frame. Efek double jump dan trail dash boleh terpisah dari tubuh.

Sebelum aset final tersedia, gunakan pose/frame sementara yang jelas berbeda dan beri status placeholder dalam catatan produksi. Idle/Walk yang tidak punya klip tidak boleh dianggap selesai hanya karena state Animator sudah ada.

## 7. Desain World 1

### Target durasi dan pengenalan mekanik

Target awal adalah 60-90 detik per segmen untuk pemain yang baru memahami kontrol, atau 3-4.5 menit gerak aktif untuk seluruh World 1. Dengan orientasi dan percobaan ulang, sasaran pengalaman awal mendekati 5 menit. Pemain terampil boleh lebih cepat; tidak ada kewajiban menunggu timer untuk membuka goal.

Dokumen lama menyebut 30-60 detik per segmen, sementara GDD World 1 menyebut sekitar 5 menit dan TTL 60. Rancangan ini mengusulkan target durasi baru dan TTL 100 per segmen untuk menyelaraskan keduanya. Jumlah unit level ditentukan setelah pengukuran kecepatan aktual; angka piksel di GDD tidak langsung dianggap unit Unity.

| Segmen | Susunan tantangan | Sumber daya/rute |
| --- | --- | --- |
| 1: Recycle Bin | Area gerak aman, jump pendek, dua kombinasi platform, dash melintasi gap, satu patroli yang mudah dibaca, goal | Dua zona aman, satu checkpoint otomatis, dua snippet; jalur utama tanpa walljump sulit |
| 2: File Explorer | Double jump, kenaikan vertikal dengan walljump, platform sempit, dua pertemuan musuh bergantian, turun menuju goal | Checkpoint sebelum pendakian sulit, dua snippet; cabang opsional berisi bits |
| 3: Network Tray | Moving platform, dash timing, fan vortex, kombinasi gerak dan musuh, rangkaian pendek menuju server | Checkpoint sebelum kombinasi akhir, dua snippet; hindari hazard di titik spawn/landing wajib |

Setiap segmen memiliki 4-6 kelompok tantangan dengan tempat mendarat yang aman di antaranya. Perubahan ketinggian, dinding, dan jalur berbelok membuat player memakai lebih dari dash horizontal. Tidak ada lantai lurus alternatif yang melewati seluruh tantangan. Snippet pada rute utama harus bisa dicapai tanpa gerakan lanjutan.

World 1 mengikuti GDD sebagai tutorial plaintext. Port gate dan kewajiban encrypted dipindahkan ke World 2. Checkpoint otomatis World 1 memulihkan progres; mekanik menahan tombol 0.5 detik untuk TCP Handshake diperkenalkan di World 3. Ini membedakan fasilitas retry dasar dari tantangan protokol.

### Generation

Pertahankan generator berbasis layout teks sebagai alat penyusun scene. Tahap ini memakai level yang dirancang tetap, agar jarak jump, jalur, dan ritme bisa diuji ulang secara konsisten. Generator ini belum merupakan pembangkit dunia acak.

Generator perlu memeriksa satu spawn, satu goal, simbol yang dikenal, referensi prefab, serta jumlah segmen yang cocok dengan progres/build. Pemeriksaan otomatis tidak membuktikan semua rute bisa dilalui; lintasan tetap harus diuji bermain.

Setelah World 1 berhasil, variasi dapat memakai potongan level teruji dengan titik masuk/keluar dan syarat kemampuan yang jelas. Pembangkit acak penuh menjadi pekerjaan terpisah karena perlu menjamin keterjangkauan dan anggaran TTL.

Untuk World 2-4, generator harus membaca daftar world/segmen yang dikonfigurasi, bukan hanya pola `World1_Segment*.txt`. Metadata per world mencakup urutan segmen, TTL, kemampuan yang dibuka, dan konfigurasi gravitasi. Jangan menganggap pergantian nama atau warna scene sebagai world baru yang selesai. Sebelum menambah file, cocokkan terlebih dahulu versi level yang dilaporkan pengguna; perbaiki level tersebut jika tersedia.

### Anggaran TTL

Pada segmen 90 detik, TTL 100 menyisakan 10 tanpa damage. Dua snippet (+30) dan checkpoint (+10) menyediakan total 50 TTL tersisa sebelum damage. Dua hit musuh @15 menyisakan 20. Gunakan contoh ini sebagai batas desain awal dan ukur hasil nyata; penempatan reward lebih menentukan daripada jumlahnya saja.

### Perluasan: Durasi dan Kesulitan World 2-4

Target berikut mengukur percobaan berhasil pada rute utama, tanpa loading, pause, dan retry. Waktu belajar/retry dicatat terpisah. Semua angka merupakan usulan playtest; pemain terampil boleh lebih cepat tanpa melewati seluruh isi level.

| World | Segmen | Target per segmen | Total traversal | Tantangan utama |
| --- | --- | --- | --- | --- |
| 1: Localhost | 3 | 60-90 detik | 3-4.5 menit | Dasar gerak dan membaca serangan |
| 2: LAN Party | 4 | 90-120 detik | 6-8 menit | Memilih mode/port sambil mengatur gerakan dan timing |
| 3: Fiber Deep | 5 | 100-130 detik | 8 menit 20 detik-10 menit 50 detik | Platform bergerak, arus, dan waktu aman untuk TCP Handshake |
| 4: Satellite Relay | 5 | 110-140 detik | 9 menit 10 detik-11 menit 40 detik | Prediksi platform, momentum gravitasi rendah, dan kombinasi mekanik |

GDD menyebut boss The Censor pada World 2 dan Solaris pada World 4. Waktu boss dipisahkan dari traversal: usulan 60-90 detik untuk The Censor dan 90-120 detik untuk Solaris. Keduanya tetap item rancangan boss tersendiri yang perlu diturunkan dari GDD sebelum implementasi; angka ini tidak menyatakan boss sudah tersedia. Dengan boss, target clear World 2 sekitar 7-9.5 menit dan World 4 sekitar 10 menit 40 detik-13 menit 40 detik. Sasaran lama 8/12/15 menit untuk World 2/3/4 menjadi referensi pengalaman awal termasuk belajar, bukan kewajiban menambah waktu tunggu.

### World 2: LAN Party

| Segmen | Rangkaian yang dirancang | Bukti kemampuan yang diuji |
| --- | --- | --- |
| Switch Hub | Platforming pendek, gate 80 di area aman, pengenalan encrypted dengan contoh visual, patroli dan satu turret bergantian | Pemain memahami perbedaan mode sebelum mendapat tekanan |
| Routing Table | Gate 22, naik/turun vertikal, laser berkala, pergantian kembali ke plaintext untuk gap | Memilih port dan mode di tempat aman lalu mengeksekusi lintasan |
| Bandwidth Choke | Koridor bertingkat, rangkaian jump-dash dengan pendaratan, fan dan gate 443 | Mengatur stamina serta kecepatan tanpa terjebak di mode lambat |
| Firewall Perimeter | Urutan 80 -> 22 -> 443, dua rangkaian turret/gate, area pemulihan sebelum boss | Menggabungkan aturan yang sudah dipelajari dengan tempo lebih rapat |

Target 5-7 kelompok tantangan per segmen. Pada pengenalan port, tidak ada serangan saat pemain pertama membaca gate. Pada ujian akhir, gate bisa dibaca dari shelter sebelum masuk jendela aman. Mode encrypted tidak menjadi kebutuhan untuk melintasi gap yang hanya bisa dicapai plaintext; berikan area pergantian setelah gate. Gate salah tetap tertutup dan menunjukkan alasan, tanpa membunuh player hanya karena salah memilih.

### World 3: Fiber Deep

| Segmen | Rangkaian yang dirancang | Bukti kemampuan yang diuji |
| --- | --- | --- |
| Cable Surface | Pengenalan TCP Handshake 0.5 detik di zona aman, platform stabil, satu musuh berdenyut | Memahami aktivasi checkpoint yang kini membutuhkan interaksi |
| Coral Data | Platform vertikal bergerak, arus ringan, pendakian dua bagian, node setelah tantangan | Menyesuaikan titik lepas landas dan mendarat |
| Pressure Zone | Platform runtuh bertanda, lorong arus, gate 80, jalur aman alternatif lebih panjang | Memilih rute berdasarkan TTL dan kemampuan gerak |
| Deep Trench | Platform muncul-hilang, gate 22, dua node, kombinasi musuh berdenyut dan arus | Membaca siklus lalu bergerak menuju tempat berlindung berikutnya |
| The Abyss | Tiga rangkaian dash dengan pendaratan, gate 443, kombinasi arus dan platform runtuh | Konsistensi menjalankan mekanik; bukan satu dash lurus ke goal |

Target 6-8 kelompok tantangan per segmen. Handshake selalu punya jendela aman yang terlihat, tidak meminta player menerima damage. Visibilitas rendah tidak boleh menyembunyikan tempat mendarat wajib atau bahaya di luar kamera. Platform runtuh memberi tanda sebelum hilang dan dapat pulih ketika percobaan diulang.

### World 4: Satellite Relay

| Segmen | Rangkaian yang dirancang | Bukti kemampuan yang diuji |
| --- | --- | --- |
| Low Orbit | Area latihan gravitasi 60%, ghost platform 0.5 detik sebelum solid, gate 80 | Menyesuaikan momentum dan memahami tanda platform mendatang |
| Relay Chain | Rangkaian ghost platform bertingkat dengan pendaratan, gate 22, drone dengan jalur tembak terbaca | Menggabungkan prediksi waktu dan satu dash per lompatan |
| Solar Field | Solar flare berkala, shelter yang terlihat, perpindahan mode dan gate 443 | Memilih waktu keluar shelter sambil menyisakan stamina |
| Debris Ring | Debris bergerak, ghost platform, urutan gate 80 -> 22, drone bergantian | Menggabungkan dua tekanan yang sudah dikenal tanpa serangan tak terhindarkan |
| Geostationary | Bagian zero gravity dengan titik docking, rangkaian timing akhir, pemulihan sebelum Solaris | Menjaga arah dan sumber daya hingga ujian akhir |

Target 6-8 kelompok tantangan per segmen. Zero gravity membutuhkan pengecualian eksplisit: titik docking solid mengisi ulang satu jatah dash udara, menghentikan drift saat menempel, dan menyediakan tempat memulihkan stamina. Jarak antar docking harus dapat dicapai dengan satu dash dan kemampuan gerak yang tersedia pada mode tersebut. Jangan mewajibkan beberapa dash udara tanpa pendaratan/refill. Keadaan awal setiap siklus platform harus menjamin rute tersedia dari spawn/checkpoint.

### Aturan Kesulitan Bersama

- World baru mengikuti urutan pengenalan aman -> latihan -> kombinasi -> ujian. Tambahkan satu aturan baru pada satu kelompok tantangan.
- World 1 umumnya satu ancaman aktif; World 2 mulai dua ancaman bergantian; World 3-4 boleh menggabungkan dua ancaman yang sudah diajarkan dan tetap menyediakan jendela lewat tanpa damage.
- Kesulitan berasal dari pilihan rute, titik pendaratan, timing, dan pergantian mode. Kenaikan damage musuh, jarak kosong, serta waktu tunggu wajib tidak dijadikan sumber utama durasi.
- Jalur utama selesai tanpa upgrade wajib atau mengambil seluruh collectible. Jalur opsional menawarkan tantangan lebih sulit dan reward, dengan jalan kembali yang jelas.
- Uji jalur utama memakai profil tercepat yang legal dan upgrade mobilitas tertinggi yang mungkin dibawa ke world tersebut. Shortcut terampil boleh memotong satu kelompok, tetapi tidak seluruh segmen atau gate wajib.
- Setiap checkpoint berjarak sekitar 30-60 detik traversal dari yang sebelumnya. Percobaan gagal mengulang bagian tantangan yang relevan, bukan seluruh world.
- Regenerasi stamina pada node/shelter tidak boleh menjadi waktu tunggu rutin. Sisipkan gerakan ringan yang tetap memungkinkan regenerasi di antara rangkaian dash.

### TTL untuk Level yang Lebih Panjang

Usulan kapasitas per segmen World 1/2/3/4 adalah 100/150/170/190 dengan drain 1/detik. World 2-4 menempatkan 2-3 snippet di jalur utama dan checkpoint sesuai jarak traversal, lalu menyesuaikan berdasarkan TTL tersisa pada playtest. Snippet sebelum TTL berkurang akan terbuang karena batas maksimum; anggaran harus dihitung per bagian lintasan, bukan hanya dijumlahkan.

Contoh World 4: 140 detik menghabiskan 140 TTL dari 190, menyisakan 50. Dua hit @15 menyisakan 20 sebelum snippet/checkpoint. Ini memberi toleransi awal untuk belajar; kalau terlalu mudah, tingkatkan kombinasi tantangan lebih dulu sebelum mengurangi toleransi damage. Uji juga sisa rute setelah setiap checkpoint dengan budget retry, agar checkpoint akhir tidak menciptakan keadaan mustahil.

Masuk arena boss yang kelak diimplementasikan menggunakan checkpoint khusus dan refill TTL/stamina. Durasi boss, serangan, serta suplai TTL harus diuji sebagai anggaran terpisah; pemain tidak dipaksa mengulang traversal hanya untuk mendapat TTL cukup bagi boss.

## 8. Musuh yang Bisa Dibaca

Musuh dasar memakai urutan berikut:

`Patrol -> Detect/Chase -> Windup -> Attack -> Recovery -> Chase/Patrol`

- Patrol dibatasi area rumah; musuh memeriksa tepi platform dan dinding.
- Chase hanya dimulai dalam jangkauan dan garis pandang. Musuh darat tidak mengejar ke jurang atau menembus dinding.
- Pada jarak serang, musuh berhenti. Windup sekitar 0.4 detik memberi pose/warna yang jelas.
- Attack memakai arah yang dikunci ketika windup dimulai; jendela aktif sekitar 0.15 detik. Player bisa menghindar ke belakang atau melompat.
- Recovery sekitar 0.8 detik memberi kesempatan lewat. Satu serangan mengenai player maksimal sekali meskipun player punya beberapa collider.
- Kehilangan target membuat musuh kembali ke area patroli. Musuh tidak mengikuti player sepanjang level.
- Badan musuh tidak menabrak badan player secara fisik; damage berasal dari hitbox serangan. Tabrakan musuh terhadap lingkungan tetap aktif.
- Fan Vortex tetap bahaya lingkungan yang mendorong, dengan batas kekuatan dan rute keluar yang bisa dilalui.

Varian pertama: Cursor berpatroli lalu menyerang jarak dekat. Process Zombie menjaga tempat dan memberi serangan berkala dengan jeda. Gunakan satu varian dulu untuk menguji fondasi, baru tambah variasi.

Player bertugas menghindari musuh; menyerang, stomp, senjata, HP musuh, dan hadiah membunuh tidak termasuk usulan awal. Penambahan combat aktif akan membutuhkan rancangan tersendiri.

## 9. Kematian, Checkpoint, dan Progres

- Checkpoint menyimpan ID scene, posisi aman, dan snapshot bits serta collectible yang sudah diambil dalam segmen.
- Setelah mati, tampilkan feedback singkat, lalu Game Over. Tombol Retry kembali ke checkpoint aktif atau spawn awal jika belum ada checkpoint.
- Retry checkpoint memakai TTL minimal 70% kapasitas segmen dan stamina penuh. Gunakan nilai terbesar antara snapshot TTL saat checkpoint dan 70% kapasitas, dibatasi maksimum. Timer dan damage tidak berjalan selama transisi.
- Hazard, musuh, moving platform, dan cooldown kembali ke keadaan awal percobaan. Posisi spawn harus aman terhadap pola reset tersebut.
- Restore snapshot collectible: reward sebelum checkpoint tetap sudah terambil; reward sesudah checkpoint dikembalikan dan bits sesudah checkpoint dibatalkan. Bonus checkpoint tidak bisa dipanen ulang.
- Usulan untuk World 1-4: hilangkan pengurangan bits acak saat mati. Bits masuk saldo permanen satu kali ketika World selesai; kembali ke menu membuang bits run yang belum diselesaikan.
- Pergantian segmen mengisi TTL/stamina dan menghapus checkpoint segmen lama, sambil mempertahankan bits run. Jangan menghapus collectible segmen aktif secara parsial tanpa memulihkan snapshot yang sama.
- Hub hanya menawarkan world yang scene-nya tersedia. World terkunci/masih dirancang tidak boleh memuat ulang scene saat ini seolah berhasil dimulai.

Aturan snapshot dan ekonomi ini mengganti perilaku sekarang yang langsung menyimpan sebagian bits saat mati. Perubahan tersebut harus diterapkan bersama agar retry tidak menggandakan hadiah.

## 10. Urutan Pengerjaan yang Diusulkan

| Tahap | Hasil yang dapat ditinjau | Area file utama |
| --- | --- | --- |
| 1. Fondasi gerak | Dash tertahan dinding/gate, nilai jarak konsisten, lompatan tetap nyaman | PlayerController.cs, PlayerMode.cs, Player.prefab, EditorGenerator.cs |
| 2. TTL dan stamina | HUD terbaca, biaya/regenerasi bekerja, damage punya feedback dan kebal singkat | PlayerHealth.cs, calon PlayerStamina.cs, HUD_TTL.cs, calon HUD_Stamina.cs, EditorGeneratorSetup.cs |
| 3. Animasi sementara | Semua state gerakan dapat dilihat dan mudah diganti klip final | PlayerAnimator.cs, PlayerAnimator.controller, generator animasi |
| 4. Musuh | Satu musuh dengan serangan terbaca dan tanpa dorongan terus-menerus | Enemy_Patrol.cs, Enemy_Patrol.prefab, pengaturan tabrakan |
| 5. Checkpoint/progres | Retry benar, collectible konsisten, tidak ada duplikasi saldo | Checkpoint_TCP.cs, LevelManager.cs, GameManager.cs, GameOverUI.cs |
| 6. World 1 | Tiga segmen dengan ritme dan anggaran TTL teruji | World1_Segment1/2/3.txt, EditorLevelGenerator.cs, scene hasil generator |
| 7. World 2 | Empat segmen, mode/port dan tekanan turret teruji | Layout World2_Segment1-4, Gate_Firewall.cs, metadata world, generator, scene; lokasi layout dicocokkan dulu |
| 8. World 3 | Lima segmen, handshake dan platform/arus teruji | Layout World3_Segment1-5, Checkpoint_TCP.cs, prefab platform/hazard, metadata world dan scene |
| 9. World 4 | Lima segmen, gravitasi rendah, ghost platform dan docking teruji | Layout World4_Segment1-5, konfigurasi gerak/gravitasi, prefab platform/docking, metadata world dan scene |
| 10. Aset dan tuning World 1-4 | Klip final terpasang, durasi serta peningkatan kesulitan diperbaiki dari playtest | Aset player, controller, prefab, parameter 17 segmen |

Boss The Censor dan Solaris memerlukan rincian encounter dari GDD dan tahap implementasi tersendiri. Penyelesaian 17 segmen traversal tidak boleh dilaporkan sebagai penyelesaian boss atau semua isi world menurut GDD.

Semua nama file pada tabel mengacu ke proyek `packet-courier/Assets`; script berada di `_Scripts` sesuai folder yang sudah ada. Pisahkan tanggung jawab stamina dari gerakan; gerakan meminta biaya aksi, HUD membaca nilai, dan pengelola level mengatur reset. Gunakan pola proyek yang ada tanpa mengganti seluruh arsitektur.

Generator harus menghasilkan konfigurasi yang sama dengan prefab/scene yang diuji. Sebelum regenerasi, periksa perubahan scene manual agar tidak tertimpa. Perbaikan tidak dianggap selesai jika hanya bekerja sampai generator dijalankan lagi.

## 11. Kriteria Penerimaan

1. Idle, Walk, Jump, DoubleJump, Fall, Dash, Land, Hurt, dan Death terlihat pada pemicu yang tepat. Pergantian cepat tidak meninggalkan state yang salah.
2. Dash menuju dinding/gate tipis pada 30, 60, dan 120 fps tidak menembusnya. Uji juga sudut, langit-langit, dan dash saat berdempetan dengan penghalang.
3. TTL dan stamina terlihat pada seluruh resolusi target. Pause membekukan drain, regen, musuh, dan timer aksi; resume tidak memberi lonjakan waktu.
4. Biaya dash hanya satu kali per aktivasi; input yang ditolak tidak mengurangi stamina. Mendarat memulihkan jatah udara, tetapi tidak langsung mengisi stamina.
5. Player dapat keluar dari pertemuan musuh dengan membaca windup/recovery. Tidak ada dorongan tanpa henti, damage berulang dalam satu swing, atau musuh berjalan keluar platform.
6. Retry mengembalikan player ke posisi aman, dengan TTL/stamina sesuai aturan. Mengulang checkpoint dan collectible tidak menambah bits permanen berulang kali.
7. Kedua jalur utama dan opsional di setiap segmen dapat dilalui dengan kemampuan yang sudah diperkenalkan. Tidak ada goal yang dapat dicapai dengan dua dash dari spawn.
8. Catat waktu selesai, lokasi mati, TTL tersisa, stamina habis, dan aksi yang membingungkan pada sedikitnya tiga sesi pemain yang belum hafal layout untuk tiap world. Bandingkan traversal berhasil dengan rentang per-world pada bagian 7; waktu boss dan retry dicatat terpisah.
9. Regenerasi prefab dan scene mempertahankan wiring HUD, Animator, stamina, checkpoint, dan musuh. Console bebas error yang terkait perubahan.
10. Uji integrasi Menu -> seluruh segmen world -> Hub untuk World 1-4 (3/4/5/5 segmen), mati -> Retry, kembali ke menu, dan unlock world berikutnya. Penyelesaian world menambah saldo satu kali. Boss yang belum diimplementasikan dicatat terpisah, tidak dianggap lolos pengujian.
11. Semua 17 segmen lolos percobaan mencari jalan pintas dengan dash dan upgrade mobilitas yang tersedia. Dua dash dari spawn tidak cukup mencapai goal; tidak ada jalan lurus yang menghindari seluruh kelompok tantangan atau gate wajib.
12. Mode encrypted tidak menyebabkan gap wajib mustahil. Handshake mempunyai jendela aman. Ghost platform memiliki tanda yang konsisten dan dapat dicapai. Zero gravity tidak menjebak player tanpa jatah dash atau titik pemulihan.

Pengujian otomatis difokuskan pada biaya/regen stamina, kebal/damage, snapshot checkpoint, dan saldo. Pengujian Play Mode memeriksa tabrakan, urutan animasi, UI, serta perilaku musuh. Durasi dan kenyamanan memerlukan playtest manusia.

## 12. Keputusan yang Perlu Ditinjau

Usulan paling berpengaruh adalah pilihan A (TTL sebagai health + stamina), target durasi dan budget TTL yang meningkat untuk World 1-4, pengenalan encrypted/port mulai World 2, handshake di World 3, prediksi platform di World 4, dan retry checkpoint tanpa potongan bits acak. Lingkup revisi adalah 17 segmen traversal. Semua sudah diberi aturan konkret agar dapat dinilai sebelum pekerjaan implementasi dimulai.

Rujukan: `game.md` bagian konsep TTL = nyawa; `packet-courier/gdd/world_1_localhost.md`, `world_2_lan_party.md`, `world_3_fiber_deep.md`, dan `world_4_satellite_relay.md`; `docs/superpowers/specs/2026-08-05-packet-courier-vertical-slice-design.md`; serta script/prefab/scene yang dicatat pada bagian bukti. Laporan pengguna memperluas kebutuhan perbaikan panjang dan kesulitan hingga World 4.

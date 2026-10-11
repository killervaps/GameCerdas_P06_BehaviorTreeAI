# GameCerdas_P06_BehaviorTreeAI

## Identitas

| Keterangan | Isi |
|---|---|
| Nama Kelompok | Xx_Sigm4_M4le_67_xX |
| Anggota 1 | Muhammad Farrel Fathin Wibowo / 5025231233 |
| Anggota 2 | Danny Rachmadian Yusuf Satryatama / 5025231240 |
| Anggota 3 | Valensio Arvin Putra Setiawan / 5025231273 |

## Praktikum: Enemy AI dengan Behavior Tree (BT)

### Tujuan Praktikum

Pada praktikum ini mahasiswa membuat sebuah NPC enemy yang mampu memilih perilaku berdasarkan kondisi lingkungan menggunakan Behavior Tree (BT).

NPC memiliki empat perilaku utama dengan prioritas:

```plaintext
Flee > Attack > Chase > Patrol
```

Perilaku yang dibuat:

1. Enemy melakukan **Patrol** ketika player tidak terlihat.
2. Enemy melakukan **Chase** ketika player terlihat tetapi masih jauh.
3. Enemy melakukan **Attack** ketika player terlihat dan berada dalam jarak serang.
4. Enemy melakukan **Flee** ketika health enemy rendah.

Setelah menyelesaikan praktikum, mahasiswa diharapkan mampu memahami konsep Behavior Tree, membuat node dasarnya (`Selector`, `Sequence`, `Condition`, `Action`, `Decorator`), menghubungkannya dengan `NavMeshAgent` dan perception, membandingkannya dengan FSM, memahami dasar Utility-Based AI, serta melakukan debugging terhadap perilaku NPC.

| Capaian pembelajaran | Dibahas pada bagian |
|---|---|
| Konsep BT, `Success` / `Failure` / `Running` | [Konsep Behavior Tree](#konsep-behavior-tree) |
| Base node, `Selector`, `Sequence`, `Condition`, `Action` | [Konsep Behavior Tree](#konsep-behavior-tree), [Penjelasan Logika Kode](#penjelasan-logika-kode) (bagian 1) |
| `Decorator` | `CooldownDecorator` pada [Penjelasan Logika Kode](#penjelasan-logika-kode) (bagian 1) |
| Blackboard | [Blackboard](#blackboard) |
| BT dengan `NavMeshAgent`, perception ke decision making | [Penjelasan Logika Kode](#penjelasan-logika-kode) (bagian 5) |
| FSM vs BT, dasar Utility-Based AI | [Perbandingan FSM, BT, dan Utility AI](#perbandingan-fsm-bt-dan-utility-ai) |
| Debugging perilaku NPC | [Debugging](#debugging) |

### Fitur

Seluruh praktikum berada pada satu scene: `Assets/Scenes/P06_BehaviorTree.unity`.

**Inti praktikum**

- Framework Behavior Tree: `BTNode`, `SelectorNode`, `SequenceNode`, `ConditionNode`, `ActionNode`, dan `CooldownDecorator`.
- `EnemyBTController` dengan perilaku Patrol, Chase, Attack, Flee, serta Dead.
- Persepsi enemy berdasarkan jarak, sudut pandang, dan raycast line of sight.
- Pergerakan enemy memakai `NavMeshAgent` pada NavMesh yang di-bake dari scene.
- Sistem health untuk enemy dan player, serta serangan enemy dengan damage dan cooldown.
- Player digerakkan dengan keyboard dan dapat menyerang enemy dengan tombol Space.

**Bonus pengembangan yang dikerjakan**

- **Search Last Seen Position:** enemy yang kehilangan player berjalan ke posisi terakhir player terlihat, berhenti, lalu menoleh kiri-kanan sebelum kembali patroli.
- **Health Bar:** slider health di atas kepala enemy, beserta teks state aktif.
- **Animator Integration:** `EnemyAnimation` mengirim aksi enemy ke Animator, termasuk animasi idle saat enemy menoleh di fase Search.
- **Full Utility AI Controller:** `EnemyUtilityController`, enemy kedua dengan aksi dan executor yang sama, tetapi memilih aksi berdasarkan skor, bukan urutan pohon.
- **Personality Enemy:** aset `EnemyPersonality` (Default, Aggressive, Coward) yang dibaca oleh BT maupun Utility AI.

**Improvisasi dari tim**

- Kontrak `IEnemyBrain` sehingga UI dan Animation bekerja untuk enemy BT maupun Utility tanpa mengetahui jenis otaknya.
- Label otak dan personality pada teks state (misalnya `[BT/Aggressive] Chase`) agar kedua enemy bisa dibedakan saat tampil berdampingan.
- Player memukul enemy hidup **terdekat**, sehingga dua enemy bisa berada di scene yang sama.
- Skor tiap aksi Utility AI ditampilkan di Inspector untuk debugging.
- HUD player (slider dan total HP), animasi player (Idle, Run, Attack, Death), dan penalti ketika player mati, dibawa dari praktikum 5.

> Keterangan di atas hanya preview singkat pencapaian praktikum. Penjelasan lengkap ada di bagian berikutnya.

---

## Setup Scene

- **Ground**, **Environment**, dan **Obstacles** (`Wall1`–`Wall12`) membentuk area permainan. Semuanya ikut di-bake pada **Navigation**.
- **PatrolPoints** berisi waypoint (`W1`–`W4`), dan **SafePoint** adalah titik tujuan enemy saat kabur.
- **Enemy_BT** memakai `NavMeshAgent`, `EnemyHealth`, `EnemyBTController`, dan `EnemyAnimation`, dengan model beranimasi `TripleT+Rigs`. Patrol melewati keempat waypoint.
- **Enemy_Utility** memakai `NavMeshAgent`, `EnemyHealth`, `EnemyUtilityController`, dan `EnemyAnimation`, dengan model dan Animator yang sama. Patrol melewati dua waypoint.
- **Player** memakai `CharacterController`, `PlayerHealth`, `SimplePlayerController`, `PlayerAttackTest`, dan `PlayerAnimation`, dengan model beranimasi `Wok`.
- **EnemyUI** (satu per enemy) adalah Canvas *World Space* di atas kepala enemy yang berisi **Text_Enemy_State** dan **Slider_Enemy_Health**.
- **PlayerHUD** adalah Canvas *Screen Space* yang berisi **Slider_HP_Player** dan **Text_HP**.
- Aset personality berada di `Assets/Personalities/`. Pada scene saat ini, `Enemy_BT` memakai `AggressivePersonality` dan `Enemy_Utility` memakai `CowardPersonality`.

### Personality

Slot `Personality` pada kedua controller bersifat opsional. Jika diisi, nilai di aset **menimpa** field Persepsi, Combat, Flee, Search, dan Movement di Inspector saat game dimulai. Jika kosong, nilai Inspector dipakai.

| Sifat | Default | Aggressive | Coward |
|---|---|---|---|
| `fleeStartHealthRatio` | 0,30 | 0,15 | 0,60 |
| `attackDamage` | 10 | 15 | 6 |
| `attackCooldown` | 1,5 | 1,0 | 2,0 |
| `attackRange` / `attackExitRange` | 2 / 2,75 | 2,2 / 3,0 | 1,8 / 2,5 |
| `visionRange` / `visionAngle` | 10 / 90° | 12 / 100° | 8 / 80° |
| `safeDistance` | 12 | 8 | 16 |
| `searchScanDuration` | 4 detik | 6 detik | 2 detik |
| `patrolSpeed` / `chaseSpeed` / `fleeSpeed` | 2 / 4 / 5 | 2,5 / 4,5 / 4 | 1,5 / 3,5 / 6 |

`fleeStartHealthRatio` diterjemahkan berbeda oleh tiap otak:

| Otak | Terjemahan |
|---|---|
| BT | `lowHealthThreshold = fleeStartHealthRatio × MaxHealth` (ambang tetap) |
| Utility | Kurva `fleeUrgencyByHealth`: urgensi penuh di bawah rasio itu, turun ke nol 0,2 di atasnya |

### Parameter Lain

| Komponen | Parameter | Nilai |
|---|---|---|
| `EnemyBTController` (Search) | `searchSpeed` / `searchScanAngle` | 3 / 70° |
| `EnemyBTController` (Search) | `searchTimeout` / `searchSampleDistance` | 15 detik / 2 |
| `EnemyUtilityController` (Search) | `searchSpeed` / `searchScanAngle` / `searchPatience` | 3 / 70° / 12 detik |
| `EnemyUtilityController` (Search) | `searchTimeout` / `searchSampleDistance` | 15 detik / 2 |
| `EnemyUtilityController` (Keputusan) | `decisionInterval` / `inertiaBonus` | 0,25 detik / 0,1 |
| `EnemyUtilityController` (Flee) | `fleeCooldown` | 10 detik |
| `EnemyUtilityController` (Plafon skor) | `fleeWeight` / `attackWeight` / `chaseWeight` / `searchWeight` / `patrolWeight` | 1 / 0,9 / 0,7 / 0,5 / 0,2 |
| `EnemyHealth`, `PlayerHealth` | `maxHealth` | 100 |
| `SimplePlayerController` | `moveSpeed` | 7 |
| `PlayerAttackTest` | `attackDistance` / `damage` | 3 / 20 |

### Diagram Behavior Tree

Prioritas praktikum `Flee > Attack > Chase > Patrol` diperluas dengan **Dead** di paling atas dan **Search** di antara Chase dan Patrol.

```plaintext
Selector (root)                      ← cabang pertama yang tidak Failure menang
├─ Sequence [Dead]
│    ├─ Condition  IsDead
│    └─ Action     Die
├─ Sequence [Flee]
│    ├─ Condition  ShouldFlee        (health ≤ ambang dan belum pernah kabur)
│    └─ Action     Flee
├─ Sequence [Attack]
│    ├─ Condition  CanSeePlayer
│    ├─ Condition  IsPlayerInAttackRange
│    ├─ Action     EngageTarget      (berhenti dan menghadap player)
│    └─ Cooldown ── Action StrikePlayer
├─ Sequence [Chase]
│    ├─ Condition  CanSeePlayer
│    └─ Action     ChasePlayer
├─ Sequence [Search]
│    ├─ Condition  HasLastKnownPosition
│    └─ Action     SearchLastKnownPosition
└─ Action [Patrol]                   ← pilihan terakhir, selalu bisa dijalankan
```

### Diagram Utility AI

Pada Utility AI tidak ada urutan: tiap aksi menghitung skor 0–1 dan aksi dengan skor tertinggi dijalankan.

| Aksi | Skor | Plafon |
|---|---|---|
| **Dead** | gerbang: jika `IsDead`, langsung Dead tanpa penilaian | – |
| **Flee** | `urgensi(HP) × ancaman` | 1,0 |
| **Attack** | `terlihat × dalamJangkauan × kemauanSerang(HP)` | 0,9 |
| **Chase** | `terlihat × (1 − dalamJangkauan) × (1 − urgensi)` | 0,7 |
| **Search** | `punyaLKP × (1 − terlihat) × (1 − lamaHilang) × (1 − urgensi)` | 0,5 |
| **Patrol** | konstanta (baseline) | 0,2 |

`ancaman = 1 − clamp01(jarakPlayer / safeDistance)`. `urgensi(HP)` dan `kemauanSerang(HP)` adalah `AnimationCurve` yang bisa disetel di Inspector. Setelah Flee selesai, urgensi bernilai 0 selama `fleeCooldown`.

### Arti Pengaturan Animasi

**Enemy** (`TripleT+Rigs.controller`) memakai parameter `State` (Int) yang nilainya mengikuti enum `EnemyAction`:

| Nilai `State` | Aksi | Clip |
|---|---|---|
| 0 | Patrol (dan Search saat berjalan) | `EnemyWalking` |
| 1 | Chase | `Fast Run` |
| 2 | Attack | `Melee Attack` |
| 3 | Flee | `Fast Run` |
| 4 | Dead | `Death` |
| 5 | Search (saat menoleh) | `Idle` |

Semua transisi berasal dari **Any State** dengan satu kondisi `State == n`. *Has Exit Time* dan *Can Transition To Self* dimatikan agar animasi tidak diulang dari awal selama aksi yang sama masih aktif.

Search punya dua tampilan: saat berjalan ke posisi terakhir player enemy memakai animasi jalan (`State = 0`), dan saat tiba lalu menoleh memakai `Idle` (`State = 5`).

**Player** (`Wok.controller`) memakai tiga parameter:

| Parameter | Tipe | Fungsi |
|---|---|---|
| `Speed` | Float | `idleAnim` ↔ `FastRun` (ambang 0,1) |
| `Attack` | Trigger | Any State → `punchCombo`, lalu kembali ke `idleAnim` setelah clip selesai |
| `IsDead` | Bool | Any State → `Death` |

---

## Konsep Behavior Tree

Behavior Tree adalah pohon keputusan yang dievaluasi ulang setiap frame dari akarnya. Tiap node mengembalikan salah satu dari tiga status:

| Status | Arti | Contoh pada proyek ini |
|---|---|---|
| `Success` | Tugas selesai atau syarat terpenuhi | `CanSeePlayer` bernilai true; `StrikePlayer` selesai memukul |
| `Failure` | Tugas gagal atau syarat tidak terpenuhi | `IsDead` bernilai false; `safePoint` tidak ada |
| `Running` | Tugas masih berjalan, belum selesai | `ChasePlayer` terus mengejar; `Patrol` terus berjalan |

Ada dua jenis node komposit yang dipakai:

| Node | Cara kerja | Berhenti saat | Hasil |
|---|---|---|---|
| **Selector** (fallback) | Mencoba anak satu per satu | Anak pertama `Success` atau `Running` | `Failure` hanya jika semua anak `Failure` |
| **Sequence** | Menjalankan anak satu per satu | Anak pertama `Failure` atau `Running` | `Success` hanya jika semua anak `Success` |

Root adalah **Selector**, sehingga urutan anaknya menjadi urutan prioritas. Tiap cabang prioritas adalah **Sequence** yang berbentuk "syarat dulu, baru aksi": Condition berfungsi sebagai gerbang, dan jika gagal, Selector turun ke cabang berikutnya.

Pohon tidak punya memori antar frame. Selector selalu mulai dari anak pertama, sehingga cabang berprioritas tinggi bisa menginterupsi cabang yang sedang `Running`. Contohnya, enemy yang sedang Chase langsung beralih ke Flee saat health rendah, tanpa transisi eksplisit.

### Blackboard

Blackboard adalah penyimpanan data bersama yang dibaca dan ditulis oleh node-node pohon, misalnya target, posisi terakhir target, dan status internal.

Proyek ini **tidak memakai kelas Blackboard terpisah**. Data bersama disimpan sebagai field di `EnemyBTController`, dan node mengaksesnya lewat delegate (`Func<bool>` dan `Func<NodeState>`) yang menunjuk ke method controller. Fungsinya setara blackboard implisit:

| Data | Field | Dibaca oleh |
|---|---|---|
| Target | `player`, `playerHealth` | `CanSeePlayer`, `ChasePlayer`, `StrikePlayer` |
| Posisi terakhir player | `lastKnownPosition`, `hasLastKnownPosition` | `HasLastKnownPosition`, `SearchLastKnownPosition` |
| Progres Search | `searchPhase`, `scanStartTime` | `SearchLastKnownPosition`, `IsScanning` |
| Status kabur | `fleeCompleted` | `ShouldFlee` |
| Aksi aktif | `activeAction` | `IsPlayerInAttackRange` (histeresis), UI, Animation |

---

## Penjelasan Logika Kode

Penjelasan mengikuti urutan: framework BT, kontrak bersama (`EnemyAction`, `IEnemyBrain`), `EnemyHealth`, `EnemyPersonality`, `EnemyBTController`, framework Utility AI dan `EnemyUtilityController`, `EnemyAnimation`, `EnemyUI`, lalu script player.

#### 1. `Assets/Scripts/BehaviorTree/` (framework)

**`BTNode.cs`**
Berisi enum `NodeState` (`Success`, `Failure`, `Running`) dan kelas dasar abstrak `BTNode` dengan satu method `Tick()`. Semua node pohon mewarisi kelas ini.

**`SelectorNode.cs`**
Menyimpan daftar anak. `Tick()` memanggil anak berurutan: jika ada yang `Success` atau `Running`, nilainya langsung dikembalikan dan anak sisanya tidak dievaluasi. Jika semua `Failure`, hasilnya `Failure`.

**`SequenceNode.cs`**
Kebalikan Selector. `Tick()` memanggil anak berurutan: jika ada yang `Failure` atau `Running`, nilainya langsung dikembalikan. Jika semua `Success`, hasilnya `Success`.

**`ConditionNode.cs`**
Membungkus `Func<bool>`. `Tick()` mengembalikan `Success` jika fungsi bernilai true, `Failure` jika false. Node ini tidak pernah `Running` dan tidak mengubah apa pun di dunia game.

**`ActionNode.cs`**
Membungkus `Func<NodeState>`. `Tick()` menjalankan fungsi dan mengembalikan statusnya apa adanya. Di sinilah perilaku nyata dijalankan (bergerak, menyerang, kabur).

**`CooldownDecorator.cs`**
Decorator yang membungkus satu node anak dan membatasi seberapa sering anak itu boleh dijalankan.
- Jika `Time.time < nextAllowedTime`, mengembalikan `Running` tanpa menyentuh anak.
- Jika tidak, `Tick()` anak dipanggil. Jika hasilnya `Success`, `nextAllowedTime` dijadwalkan `cooldown` detik ke depan.

Pada proyek ini decorator membungkus `StrikePlayer`, sehingga serangan dibatasi `attackCooldown` tanpa menambah variabel timer di controller.

#### 2. `Assets/Scripts/AI/EnemyAction.cs` dan `IEnemyBrain.cs`

**`EnemyAction`**
Enum berisi enam aksi: `Patrol = 0`, `Chase = 1`, `Attack = 2`, `Flee = 3`, `Dead = 4`, dan `Search = 5`. Nilai numeriknya **harus** sama dengan parameter `State` di Animator.

**`IEnemyBrain`**
Kontrak yang dibaca UI dan Animation: `BrainName`, `CurrentAction`, dan `IsScanning`. Dengan kontrak ini, `EnemyUI` dan `EnemyAnimation` bekerja untuk `EnemyBTController` maupun `EnemyUtilityController`.

#### 3. `Assets/Scripts/AI/EnemyHealth.cs`

**Field.** `maxHealth` dan properti `CurrentHealth` (hanya bisa diubah dari dalam script ini). `MaxHealth` dan `IsDead` (health ≤ 0) dibaca oleh script lain. `Active` adalah daftar statis semua enemy yang sedang aktif.

**`Awake()`**
Health diisi penuh saat game mulai.

**`OnEnable()` / `OnDisable()`**
Enemy dimasukkan ke dan dikeluarkan dari daftar `Active`, sehingga enemy yang di-`SetActive(false)` otomatis tidak menjadi target player.

**`TakeDamage(damage)`**
Jika enemy sudah mati, fungsi berhenti. Jika belum, health dikurangi `damage` lalu dibatasi antara 0 dan `maxHealth`, dan nilainya ditulis ke Console.

Dua menu konteks tersedia untuk pengujian: **Test Damage 25** dan **Reset Health**.

#### 4. `Assets/Scripts/AI/EnemyPersonality.cs`

`ScriptableObject` (menu **Create ▸ Enemy AI ▸ Personality**) berisi sifat-sifat yang tidak terikat pada otak tertentu: persepsi, combat, flee, search, dan kecepatan. Semua field hanya-baca lewat properti publik. `DisplayName` dipakai untuk label di UI, dan memakai nama aset jika dikosongkan.

Kedua controller membaca aset yang sama pada `Awake()` lewat `ApplyPersonality()`, sehingga "Aggressive" di BT dan "Aggressive" di Utility AI berasal dari satu definisi.

#### 5. `Assets/Scripts/AI/EnemyBTController.cs`

**Field.** Referensi (`player`, `patrolPoints`, `safePoint`, `EnemyHealth`, `personality`), parameter tiap perilaku (lihat tabel pengaturan), serta `activeAction` yang terlihat di Inspector untuk debugging. Properti publik `CurrentAction`, `IsScanning`, dan `BrainName` dipakai UI dan Animation.

**`Awake()`**
Mengambil `NavMeshAgent` dan `EnemyHealth` (ditambahkan otomatis jika belum ada), mengambil `PlayerHealth` dari player, lalu menerapkan personality.

**`Start()`**
Membangun pohon lewat `BuildBehaviorTree()`.

**`Update()`**
Memanggil `rootNode.Tick()` setiap frame.

**`BuildBehaviorTree()`**
Menyusun pohon seperti pada diagram: Selector dengan anak berurutan Dead, Flee, Attack, Chase, Search, dan Patrol. Method-method controller dihubungkan ke node sebagai delegate.

**Conditions**
- `IsDead()`: health enemy ≤ 0.
- `ShouldFlee()`: Flee belum pernah selesai dan health ≤ `lowHealthThreshold`.
- `HasLastKnownPosition()`: enemy masih menyimpan posisi terakhir player.
- `IsPlayerInAttackRange()`: jarak ke player ≤ `attackRange`, atau ≤ `attackExitRange` bila aksi aktif sudah Attack (histeresis, agar tidak berkedip di perbatasan).
- `CanSeePlayer()`: perception, yaitu persepsi yang menghubungkan dunia game ke pohon. Mengembalikan `true` hanya jika keempat pemeriksaan lolos:
  1. **Player masih hidup.**
  2. **Jarak.** Dari mata enemy ke dada player tidak melebihi `visionRange`.
  3. **Sudut pandang.** Sudut antara arah depan enemy dan arah ke player tidak melebihi setengah `visionAngle`.
  4. **Line of sight.** Ray dari mata enemy ke player tidak mengenai objek pada `obstacleMask`.

**Actions**
- `Patrol()`: agent berjalan ke waypoint saat ini dengan `patrolSpeed`. Jika sudah dekat (≤ 0,5), indeks waypoint naik (kembali ke 0 setelah yang terakhir). Selalu `Running`; `Failure` bila tidak ada waypoint.
- `ChasePlayer()`: agent berjalan ke posisi player dengan `chaseSpeed`, dan `stoppingDistance` diatur 80% dari `attackRange`. Posisi player dicatat sebagai last known position. `Running`.
- `EngageTarget()`: agent dihentikan, enemy menoleh ke player (`FacePlayer()`), dan posisi player dicatat. Dijalankan setiap tick selama player dalam jangkauan, sehingga enemy tetap menghadap player saat menunggu cooldown. `Success`.
- `StrikePlayer()`: memberi `attackDamage` ke `PlayerHealth`. Hanya dipanggil saat `CooldownDecorator` mengizinkan.
- `Flee()`: agent berjalan ke `safePoint` dengan `fleeSpeed`, dan last known position dihapus agar enemy tidak berbalik mencari player. Selesai (`Success`) jika jarak ke player ≥ `safeDistance` atau jarak ke `safePoint` ≤ 1, dan saat itu `fleeCompleted` diset true sehingga Flee hanya terjadi sekali. Selama belum selesai `Running`.
- `Die()`: agent dihentikan dan path-nya dibersihkan sekali.
- `SearchLastKnownPosition()`: dijelaskan di bawah.

**`SearchLastKnownPosition()`**
Karena pohon tidak punya memori, progres Search disimpan di field `searchPhase` (`None`, `Move`, `Scan`).
1. **Player mati:** last known position dihapus, hasil `Failure`.
2. **Mulai (`None`):** posisi disambungkan ke titik NavMesh terdekat (`NavMesh.SamplePosition`, jarak maks `searchSampleDistance`). Jika tidak ada titik, pencarian dibatalkan. Jika ada, fase menjadi `Move` dan tujuan agent diatur sekali.
3. **`Move`:** agent berjalan dengan `searchSpeed`. Jika sudah tiba (≤ 0,5), fase menjadi `Scan`. Jika `searchTimeout` terlampaui, pencarian dihentikan.
4. **`Scan`:** agent berhenti dan badan berayun satu siklus kanan, tengah, kiri, tengah (`Mathf.Sin`, amplitudo `searchScanAngle`) selama `searchScanDuration`. Timer dihitung sejak enemy **tiba**, bukan sejak player hilang. Setelah durasi habis, last known position dihapus dan hasilnya `Success`.

Fase di-reset ke `None` setiap kali Chase atau Attack berjalan, sehingga jika player ditemukan lagi, pencarian berikutnya mulai dari awal.

**`SetAction(action)`**
Mengganti `activeAction` dan menulis perubahannya ke Console (misalnya `Enemy_BT → State: Chase`).

**`OnDrawGizmosSelected()`**
Menggambar `visionRange` (kuning), dua batas sudut pandang, `attackRange` (merah), `attackExitRange` (magenta), dan last known position (biru) di Scene view.

#### 6. `Assets/Scripts/UtilityAI/` dan `EnemyUtilityController.cs`

**`UtilityAction.cs`**
Satu kandidat aksi: `EnemyAction` sebagai identitas, fungsi skor `Func<float>`, dan fungsi eksekusi `Action`. `Evaluate()` menghitung skor dan membatasinya ke 0–1. Pasangan `ActionNode` pada BT.

**`UtilitySelector.cs`**
Pasangan `SelectorNode`, tetapi memilih berdasarkan skor, bukan urutan.
- `Tick()` dipanggil setiap frame: keputusan baru dibuat setiap `decisionInterval` (0,25 detik), sedangkan aksi terpilih dijalankan **setiap frame**.
- `Decide()` menilai semua aksi dan memilih skor tertinggi. Aksi yang sedang berjalan mendapat bonus `inertiaBonus` agar keputusan tidak berkedip di sekitar titik silang skor.
- `RequestDecision()` memaksa keputusan baru pada frame berikutnya (dipakai saat Search atau Flee selesai).

**`EnemyUtilityController.cs`**
Aksi dan executor-nya sama dengan `EnemyBTController`; hanya cara memilihnya yang berbeda.

*`Update()`*
1. Jika enemy mati, `Die()` dijalankan dan fungsi berhenti. Dead adalah gerbang, bukan hasil penilaian.
2. `Sense()` memperbarui persepsi dan memori Search setiap frame.
3. `selector.Tick()` memilih dan menjalankan aksi.

*`Sense()`*
`canSee` diperbarui. Jika player terlihat, posisinya dicatat sebagai last known position. Jika player hilang dan kesabaran habis (`lamaHilang ≥ 1`), posisi dihapus.

*Skor*
- `ScoreFlee()`: `urgensi(HP) × ancaman`. Urgensi diambil dari kurva `fleeUrgencyByHealth` dan bernilai 0 selama `fleeCooldown`.
- `ScoreAttack()`: nol jika player tidak terlihat atau di luar jangkauan; selain itu `kemauanSerang(HP) × attackWeight`.
- `ScoreChase()`: nol jika player tidak terlihat atau sudah dalam jangkauan; selain itu `chaseWeight × (1 − urgensi)`. Faktor `(1 − urgensi)` mencegah enemy yang ingin kabur justru mengejar.
- `ScoreSearch()`: nol jika tidak punya last known position atau player terlihat; selain itu `searchWeight × (1 − lamaHilang) × (1 − urgensi)`. Skor meluruh sampai di bawah Patrol (0,2), saat itu enemy menyerah.
- `ScorePatrol()`: konstanta `patrolWeight`.

*Executor*
`ExecutePatrol`, `ExecuteChase`, `ExecuteAttack`, `ExecuteFlee`, dan `ExecuteSearch` identik secara perilaku dengan action BT, ditambah dua hal:
- `ExecuteAttack` memeriksa ulang jangkauan sebelum memukul, karena keputusan hanya diperbarui tiap 0,25 detik.
- Setelah Flee selesai, `fleeCooldown` dimulai. Selama itu Flee tidak dipertimbangkan, sehingga enemy tidak bolak-balik di safe point.

Selama fase Scan, `lamaHilang` dibekukan sehingga enemy menuntaskan scan seperti pada BT. Kesabaran hanya membatasi perjalanan ke last known position.

*`ApplyPersonality()`*
Selain menyalin nilai umum, rasio `fleeStartHealthRatio` dibangun menjadi kurva `fleeUrgencyByHealth`.

#### 7. `Assets/Scripts/Animation/EnemyAnimation.cs`

Menghubungkan otak enemy dengan Animator tanpa mengubah controller.

**`Awake()`**
Mencari `IEnemyBrain` dari objek itu atau parent-nya, dan `Animator` dari child.

**`Update()`**
Hanya saat nilainya berubah, aksi diubah menjadi angka dan dikirim ke parameter Int `State`. Khusus Search: jika enemy belum menoleh (`IsScanning` false), nilai yang dikirim adalah `Patrol` (animasi jalan); saat menoleh, nilai `Search` (5) dikirim sehingga animasi Idle diputar.

#### 8. `Assets/Scripts/UI/EnemyUI.cs`

Dipasang di Canvas World Space di atas enemy.

**`Awake()`**
Menyimpan kamera utama dan mencari `IEnemyBrain` serta `EnemyHealth` dari parent.

**`LateUpdate()`**
Setiap frame:
1. Canvas dibuat menghadap searah kamera (billboard).
2. Teks diisi `[nama otak/personality] aksi` dan diwarnai: hijau (Patrol), kuning (Chase), merah (Attack), cyan (Flee), oranye (Search), abu-abu (Dead).
3. Slider health diisi dengan `CurrentHealth / MaxHealth`.

#### 9. Script player

Script player dibawa dari praktikum 5 dengan satu perubahan di `PlayerAttackTest`.

**`PlayerHealth.cs`**
Menyimpan `CurrentHealth`, `MaxHealth`, `IsDead`, dan event `Died`. `TakeDamage(damage)` diabaikan jika player sudah mati; saat health mencapai 0, event `Died` dipanggil.

**`SimplePlayerController.cs`**
Bergerak dengan WASD atau panah lewat `CharacterController`, dengan gravitasi dan arah hadap mengikuti gerak. Tidak bergerak jika player mati.

**`PlayerAttackTest.cs`**
Saat Space ditekan, trigger `Attack` dikirim ke Animator, lalu `FindNearestEnemy()` memilih `EnemyHealth` hidup **terdekat** dalam `attackDistance` dari daftar `EnemyHealth.Active`, dan target itu diberi `damage`. Berbeda dari praktikum 5 yang hanya punya satu referensi enemy.

**`PlayerAnimation.cs`**
Mengirim kecepatan horizontal (dihitung dari perubahan posisi per detik) ke `Speed` dan `IsDead` ke Animator.

**`PlayerHUD.cs`**
Mengisi slider dengan `CurrentHealth / MaxHealth` dan teks `HP sekarang/maksimum`.

---

## Perbandingan FSM, BT, dan Utility AI

Ketiganya diimplementasikan pada proyek yang sama dengan aksi dan executor yang sama, sehingga perbedaan hanya terletak pada **cara memilih aksi**.

| Aspek | FSM (praktikum 5) | Behavior Tree | Utility AI |
|---|---|---|---|
| Cara memilih | State aktif + transisi eksplisit antar state | Urutan prioritas tetap (Selector), dievaluasi ulang tiap frame | Skor tertinggi, dihitung ulang tiap keputusan |
| Prioritas | Dikodekan di urutan `if` pada `Update()` | Urutan anak di Selector | Plafon skor dan bentuk kurva |
| Eksklusi Attack dan Chase | Perpindahan state | Urutan Selector | Dikodekan di skor (`1 − dalamJangkauan`) |
| Flee | Ambang tetap, sekali (`fleeTriggered`) | Ambang tetap, sekali (`fleeCompleted`) | Trade-off kontinu; bisa berulang dengan `fleeCooldown` |
| Menyerah mencari | `lostPlayerDelay` (timer eksplisit) | `searchTimeout` dan durasi scan | Skor Search meluruh sampai di bawah Patrol |
| Stabilitas | Stabil, state mengunci | Stabil, urutan menjaga konsistensi | Perlu `inertiaBonus`, histeresis, dan `decisionInterval` |
| Menambah perilaku | Tambah state dan semua transisinya | Tambah satu cabang di Selector | Tambah satu fungsi skor |
| Debugging | Lihat `currentState` | Lihat aksi aktif dan status node | Lihat tabel skor |

Catatan perilaku yang muncul dari perbedaan ini:

- **Titik kabur.** Pada BT, enemy kabur tepat di ambang HP. Pada Utility, titiknya muncul dari trade-off antara skor Flee dan Attack, sehingga sedikit di atas ambang (misalnya sekitar 37% untuk personality Default, dibanding 30% pada BT).
- **Berkedip.** BT tidak mengalami keputusan berkedip karena urutan prioritas dan status `Running`. Utility membutuhkan `inertiaBonus`, histeresis jarak serang, dan interval keputusan untuk mencapai kestabilan serupa.
- **Personality.** Pada BT, sifat hanya bisa diwujudkan lewat ambang dan angka karena struktur pohon tetap. Pada Utility, sifat juga bisa mengubah bentuk kurva keinginan.

### Prinsip Dasar Utility-Based AI

1. **Setiap aksi punya fungsi utilitas** yang memetakan kondisi dunia (HP, jarak, terlihat atau tidak) menjadi skor 0–1.
2. **Aksi dengan skor tertinggi dipilih.** Tidak ada aturan "jika A maka B"; prioritas muncul dari skor.
3. **Response curve** mengubah input mentah (misalnya rasio HP) menjadi skor yang bermakna. Pada proyek ini berupa `AnimationCurve` yang bisa disetel di Inspector.
4. **Bobot atau plafon** menentukan prioritas kasar antar aksi.
5. **Stabilisasi.** Karena skor dihitung ulang, keputusan bisa berkedip, sehingga dipakai bonus inertia, histeresis, dan interval keputusan.

---

## Analisis

### 1. Apa perbedaan Selector dan Sequence?

**Selector** mencoba anaknya satu per satu dan berhenti pada anak pertama yang `Success` atau `Running`; ia baru `Failure` jika semua anak `Failure` (logika "atau", fallback). **Sequence** menjalankan anaknya satu per satu dan berhenti pada anak pertama yang `Failure` atau `Running`; ia baru `Success` jika semua anak `Success` (logika "dan"). Pada proyek ini root adalah Selector dan tiap cabang perilaku adalah Sequence berbentuk "syarat dulu, baru aksi".

### 2. Apa arti Success, Failure, dan Running?

- **`Success`**: syarat terpenuhi atau aksi sudah selesai, misalnya `CanSeePlayer` bernilai true.
- **`Failure`**: syarat tidak terpenuhi atau aksi tidak bisa dijalankan, misalnya `IsDead` bernilai false.
- **`Running`**: aksi belum selesai dan harus dilanjutkan pada frame berikutnya, misalnya `ChasePlayer` yang terus mengejar.

### 3. Mengapa urutan child pada Selector penting?

Karena Selector berhenti pada anak pertama yang tidak `Failure`, sehingga urutan anak menjadi urutan prioritas. Anak yang berada di bawah hanya dievaluasi jika semua anak di atasnya gagal. Jika `Patrol` diletakkan paling atas, enemy akan selalu patroli karena Patrol selalu bisa dijalankan, dan cabang lain tidak pernah dievaluasi.

### 4. Mengapa Flee ditempatkan sebelum Attack?

Saat health rendah, player biasanya masih terlihat dan berada dalam jarak serang, sehingga syarat Attack ikut terpenuhi. Jika Attack berada di depan, Selector berhenti di Attack dan Flee tidak pernah dievaluasi, sehingga enemy terus bertarung sampai mati. Dengan Flee di depan, kondisi bahaya (health rendah) mengalahkan keinginan menyerang.

### 5. Apa fungsi Cooldown Decorator?

`CooldownDecorator` membungkus `StrikePlayer` dan membatasi seberapa sering serangan boleh dijalankan. Selama waktu tunggu belum lewat ia mengembalikan `Running` tanpa menjalankan anaknya; setelah anak `Success`, serangan berikutnya dijadwalkan `attackCooldown` detik ke depan. Tanpanya enemy memukul setiap frame dan player kehilangan seluruh health dalam sekejap. Logika cooldown juga terpisah dari logika memukul.

### 6. Apa perbedaan utama FSM, Behavior Tree, dan Utility AI?

| | FSM | Behavior Tree | Utility AI |
|---|---|---|---|
| Cara memilih perilaku | State aktif dan transisi eksplisit antar state | Urutan prioritas pohon, dievaluasi ulang dari akar setiap frame | Skor tiap aksi dihitung, skor tertinggi dipilih |
| Menambah perilaku | Tambah state dan semua transisinya | Tambah satu cabang | Tambah satu fungsi skor |
| Sifat keputusan | Tegas, mengunci pada satu state | Tegas, berdasarkan prioritas | Kontinu, hasil trade-off antar kondisi |

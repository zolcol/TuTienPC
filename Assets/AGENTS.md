# 🧭 AGENTS.MD - AI SYSTEM MAP & ARCHITECTURE GUIDE

> **Mục tiêu:** Cung cấp bản đồ kiến trúc, nguyên tắc kỹ thuật bất biến và bảng chỉ mục tác vụ (Task-to-File Mapping). Khi nhận yêu cầu mới, AI **chỉ cần đọc file này** để định vị chính xác 1–3 file liên quan, tránh đọc toàn bộ codebase, tiết kiệm token và triệt tiêu nguy cơ ảo giác logic.

---

## 1. TỔNG QUAN HỆ THỐNG & TECH STACK

* **Thể loại:** Top-Down Action RPG 3D (phong cách Kiếm Hiệp Võ Học, kế thừa quy chuẩn dữ liệu võ lâm JX/Kingsoft).
* **Engine & Input:** Unity (C# .NET), New Input System (`PlayerInputReader`), Cinemachine (`CinemachineDragInputProvider`).
* **Kiến trúc cốt lõi:** Data-Driven (CSV/INI), Finite State Machine (FSM), Object Pooling (VFX, Projectiles, Audio, Floating Text).
* **Logic FPS Tiêu Chuẩn:** **15 FPS** ($\Delta t = \frac{1}{15} \approx 0.0667\text{s}$) đồng bộ cho Animation, Action Event, Frame Timing, Bullet Lifetime và Cooldown.
* **Quy tắc bộ nhớ:** **Zero GC Allocation** trong chu trình chiến đấu (`Update`/`FixedUpdate`). Cấm `new`, `GetComponent`, `FindObjectOfType`, `Physics.OverlapSphere` (không dùng bộ đệm) hoặc cấp phát mảng tạm thời trong game loop.

---

## 2. NGUYÊN TẮC BẤT BIẾN (HARD RULES)

1. **Chuẩn Frame Timing & Tốc Đánh (15 FPS):**
   * Đổi Frame sang Giây: $t = \frac{\text{frame}}{15.0}$.
   * Công thức Tốc Đánh (`AttackSpeed`):
     $$\text{SpeedReduction} = \frac{\lfloor \text{AttackSpeed} / 10 \rfloor}{20}$$
     $$\text{FinalFrame} = \text{Clamp}(\text{Round}(\text{OriginalFrame} \times (1.0 - \text{SpeedReduction})), 9, 100)$$
     $$\text{SpeedFactor} = \frac{\text{OriginalFrame}}{\text{FinalFrame}}$$
2. **Animation Thế Hệ Cũ (Legacy Animation Component):**
   * Mỗi nhân vật gồm 2 GameObject con riêng biệt: `Body` và `Head`, đều gắn component `UnityEngine.Animation`.
   * **Tuyệt đối không dùng** Animator Controller / Mecanim. Mọi hoạt ảnh được điều khiển qua `LegacyAnimationController.cs`.
   * Luôn hỗ trợ cơ chế tự động Fallback clip qua `ResolveClipName`: `st` $\leftrightarrow$ `sta`, `at01`/`at02` $\rightarrow$ `at`, `die` $\leftrightarrow$ `jfd`, `bat` (Hurt) $\leftrightarrow$ `jt`.
3. **Data-Driven (CSV/INI Source of Truth):**
   * Đọc dữ liệu từ `Assets/Settings/GameData/**/*.csv` qua `GameDataPaths.cs`. Không hardcode chỉ số vào component.
   * Tra cứu schema chi tiết: xem `Settings/GameData/Docs/DATA_CONVENTIONS_V2.md` và thư mục `Conventions/` (từ `01` đến `06`).
4. **Không Hardcode Định Danh (GameConstants):**
   * Mọi Tag, Layer, LayerMask, tên Animation Clip chuẩn và đường dẫn Resources phải lấy từ `GameConstants.cs` (`Tags`, `Layers`, `AnimClips`, `ResourcePaths`).
5. **VFX Khớp Xương (Bone Slots) & Ground Snapping:**
   * Quản lý qua `PartSlotDatabase` và `VfxLockRotation` với 4 chế độ xoay:
     * `FollowBoneFull`: Bám $100\%$ theo xương (Vũ khí, cánh).
     * `UprightBody`: Khóa Pitch & Roll, chỉ xoay mặt phẳng ngang $OXZ$ (Ngực, đai lưng).
     * `FlatGround`: Khóa góc phẳng $OXZ$, bám mặt đất dưới chân.
     * `FixedWorld`: Khóa cố định cả 3 trục thế giới (Hiệu ứng đỉnh đầu, Stun).
   * **Khử Z-Fighting:** Mọi VFX tiếp đất phải dùng `CombatFormula.SnapToGround(pos, GROUND_VFX_Y_OFFSET)` (offset cố định $0.02\text{m}$).
6. **Âm Thanh (Sound Mapping Rules):**
   * Quản lý qua `SoundManager.cs` (Pool 20 kênh AudioSource).
   * Tự động cắt tiền tố `Play_` từ Wwise Event để tìm AudioClip trong `Resources/Audio/{Bank}/{CleanEvent}`.
   * Hỗ trợ cấu trúc Random Container: Hit (`{Phái}_{Chiêu}_Hit01`..`09`) và Voice (`{Phái}_Vo_{ID}a`..`e`).
7. **Quản lý Domain Reload (Editor Play Mode):**
   * Mọi class chứa biến `static` (Database cache, Pool, Singleton, Delegate) bắt buộc phải có hàm reset gắn cờ:
     ```csharp
     [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
     private static void ResetStaticData() { /* Clear cache / instances */ }
     ```
8. **Quy trình Kiểm Tra Biên Dịch Tự Động (Compilation Verification):**
   * Sau khi tạo mới hoặc chỉnh sửa code C# (`.cs`), Agent chính phải gọi Subagent `unity_compiler_agent` (Model `flash`) để build bằng MSBuild:
     `& "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" "..\test1.sln" -nologo -clp:ErrorsOnly`
   * `unity_compiler_agent` tự sửa các lỗi cú pháp cơ bản. Nếu gặp lỗi logic phức tạp, Subagent sẽ báo cáo chi tiết để Agent chính xử lý.

---

## 3. BẢN ĐỒ MÃ NGUỒN (CODE MAP)

```
Assets/Scripts/
├── CameraFollow.cs                     # Camera bám theo Player mượt mà (SmoothDamp)
├── LegacyAnimationController.cs       # ĐIỀU KHIỂN HOẠT ẢNH: Play/CrossFade Body + Head, scale tốc đánh 15 FPS
│
├── Audio/
│   ├── SoundData.cs / SoundDatabase.cs# Tra cứu Sound.csv, bóc tách CleanEventName, hỗ trợ ICsvTable
│   └── SoundManager.cs                # AudioSource Pool (20 kênh), Wwise Random Container, Pitch variation, CrossFade BGM
│
├── Camera/
│   └── CinemachineDragInputProvider.cs # Nhận diện kéo chuột phải / cần Gamepad xoay camera Cinemachine
│
├── Combat/
│   ├── CombatEnums.cs                 # Toàn bộ Enums: NpcKind, NpcCamp, SkillTypeDef, HitboxShape, MissileMoveKind, v.v.
│   ├── CombatFormula.cs               # STATIC MATH: Đổi Frame/Giây, Ground Snapping (offset 0.02m), Damage, Mitigate, Crit, Heal
│   ├── DummyTarget.cs                 # Bia tập bắn test sát thương (IDamageable, World Canvas Health Bar)
│   ├── EffectManager.cs               # VFX POOLING: SpawnEffect, SpawnEffectAtSlot, SpawnEffectFollowTargetGround, RecycleEffect
│   ├── GameConstants.cs               # HẰNG SỐ TẬP TRUNG: Tags, Layers, LayerMasks, AnimClips, ResourcePaths
│   ├── IDamageable.cs                 # Interface nhận sát thương: TakeDamage(amount, hitPoint, hitDirection)
│   └── VfxLockRotation.cs             # Khóa trục xoay VFX theo 4 chế độ (FollowBoneFull, UprightBody, FlatGround, FixedWorld)
│
├── Data/
│   ├── CsvParserHelper.cs             # Parse dòng CSV, xử lý ngoặc kép, nội suy tuyến tính chuỗi cặp {Level, Value}
│   ├── EffectDatabase.cs              # Nạp EffectRes.csv (đường dẫn prefab VFX, cờ lockRotate)
│   ├── ExpRuleDatabase.cs             # Nạp ExpRule.csv (ma trận % EXP Player vs Monster), CalculateExpReward
│   ├── FlyCharData.cs / FlyCharDatabase.cs # Nạp FlyChar.csv (AnimationCurve nảy số, Alpha, Scale, Visual Style chuẩn JX)
│   ├── GameDatabase.cs                # Entry-point nạp toàn bộ Database theo đúng Dependency Order
│   ├── GameDataPaths.cs               # Quản lý đường dẫn tập trung file CSV/INI trong Settings/GameData với Fallback
│   ├── ICsvTable.cs                   # Interface nạp/xóa bảng dữ liệu chuẩn
│   ├── MissileDatabase.cs             # Nạp Missile.csv (tốc độ đạn, tầm va chạm, hitbox shape, prefab vfx fly/hit)
│   ├── NpcAiData.cs / NpcAiDatabase.cs# Nạp AI INI profiles (CommonActive, CommonPassive, BreathTick, StrikeBack)
│   ├── NpcAttributeDatabase.cs        # Nạp NpcAttribute.csv (máu, công vật lý/ngũ hành scale theo Level)
│   ├── NpcResData.cs / NpcResDatabase.cs # Nạp NpcRes.csv (kích thước quái, ActionFrames, ActionCrossFades, Model Prefab)
│   ├── NpcStatDatabase.cs             # Nạp NpcStats.csv (chỉ số mở rộng HP, MP, Công, Giáp, Kháng, Crit, Hồi phục)
│   ├── PartSlotData.cs / PartSlotDatabase.cs # Nạp PartSlot.csv (tìm Transform xương theo ID: B_RH, head, chân...)
│   ├── PlayerLevelData.cs / PlayerLevelDatabase.cs # Nạp PlayerLevel.csv (EXP lên cấp, BaseAwardExp, chỉ số cơ bản)
│   └── StateEffectDatabase.cs         # Nạp StateEffect.csv (hiệu ứng buff ngực/đầu theo thời gian)
│
├── Enemy/
│   ├── EnemyBrain.cs                  # Quản lý Cooldown skill quái, chọn skill sẵn sàng, gọi SkillDamageResolver
│   ├── EnemyController.cs             # FSM Runner quái vật, CharacterController move, đồng bộ template
│   ├── EnemyHealthBar.cs              # Billboard World Space Health Bar runtime cho từng quái vật
│   ├── EnemyPerception.cs             # Nhận diện mục tiêu: AI Breath Tick, tầm nhìn, TargetLock, agro, StrikeBack
│   ├── EnemySpawnPoint.cs             # BÃI QUÁI: Quản lý sinh quái tự động, hồi sinh, bán kính tuần tra
│   ├── EnemyStats.cs                  # Máu quái (kế thừa EntityStats), MonsterLevel, tính thưởng EXP khi chết
│   └── States/
│       ├── EnemyBaseState.cs          # State cơ sở quái vật
│       ├── EnemyIdleState.cs          # Đứng chờ / Tản bộ ngẫu nhiên (RandmonMove) / AI Breath Tick
│       ├── EnemyChaseState.cs         # Đuổi theo Player trong Vision & ActiveRadius, hỗ trợ ForbitMove
│       ├── EnemyAttackState.cs        # Xoay về mục tiêu, kích hoạt anim, áp sát thương tại mốc CastSkill
│       ├── EnemyReturnState.cs        # Leash quay về điểm Spawn, bật Invulnerable, hồi đầy máu khi về đích
│       ├── EnemyHurtState.cs          # Phát anim bat (bị thương), khựng nhẹ
│       └── EnemyDeadState.cs          # Phát anim die, tắt collider, thưởng EXP cho Player, dọn dẹp sau delay
│
├── Input/
│   └── PlayerInputReader.cs           # Wrapper New Input System (Move, Attack, Skill Q/E/R, Gamepad flag)
│
├── NPC/
│   ├── NpcTemplateCsvParser.cs        # Parser NpcTemplate.csv và Character.csv
│   ├── NpcTemplateData.cs             # DTO cấu hình quái: ID, ResID, AttribID, skills, tầm nhìn, tốc chạy
│   └── NpcTemplateDatabase.cs         # Tra cứu template quái vật theo ID
│
├── Player/
│   ├── CharacterMovement.cs           # Di chuyển camera-relative, xoay hướng nhân vật, áp dụng trọng lực
│   ├── PlayerAiming.cs                # Smartcast ngắm chiêu, raycast chuột/gamepad, điều khiển Indicator VFX
│   ├── PlayerCombat.cs                # Quản lý Slot kỹ năng (Q-E-R, Đánh thường), Cooldown, gọi SkillDamageResolver
│   ├── PlayerController.cs            # FSM Runner người chơi, facade kết nối Movement-Combat-Aiming
│   └── States/
│       ├── PlayerBaseState.cs         # State cơ sở người chơi
│       ├── PlayerIdleState.cs         # Đứng thủ thế (sta), lắng nghe Input di chuyển / đánh
│       ├── PlayerMoveState.cs         # Chạy bộ (run), có thể hủy (cancel) để tung chiêu ngay lập tức
│       ├── PlayerAttackState.cs       # ĐIỀU KHIỂN XUẤT CHIÊU: MovePos lướt tới, xoay tức thì, Combo window, Cancel
│       └── PlayerHurtState.cs         # Phát anim bat (bị thương), cho phép hủy bằng Skill / Move
│
├── Skills/
│   ├── ActionEventParser.cs           # Parse ActionEvent.csv (CastSkill, CanDoSkill, CanDoRun, MovePos...)
│   ├── CastActionID.cs                # Enum & Helper ánh xạ CastActionID (16=at01, 21=jn01...) sang tên clip
│   ├── LegacySkillCsvParser.cs        # Parser dự phòng cho cấu trúc Skills.csv cũ
│   ├── PlayerSkillManager.cs          # Quản lý cấp độ kỹ năng RPG, điểm SkillPoints, nâng cấp chiêu theo MaxLevel
│   ├── ProjectileController.cs        # Quỹ đạo đạn, Homing bám mục tiêu, Chain-bounce, DoT interval, SphereCast, Damage theo SkillLevel
│   ├── ProjectilePool.cs              # Object Pool đạn đạo tái sử dụng 100% (0 GC Alloc)
│   ├── SkillCsvParser.cs              # Parse Skill.csv + ActionEvent.csv + CustomSkill.csv (Hỗ trợ Level Scaling đa mốc)
│   ├── SkillDamageResolver.cs         # THUẬT TOÁN GÂY SÁT THƯƠNG: BoxCast, Sector, Circle, Projectile, Heal (Scale theo SkillLevel)
│   ├── SkillData.cs                   # DTO kỹ năng: chỉ số sát thương, hitbox, frame mốc, sprite icon, trích xuất Level Scaling
│   ├── SkillDatabase.cs               # Tra cứu SkillData: GetSkill (Custom), GetBaseSkill (Gốc), GetSubSkill
│   └── SkillType.cs / VfxStartPosType.cs # Enums hình thái hitbox và vị trí xuất phát chiêu
│
├── StateMachine/
│   ├── IState.cs                      # Interface trạng thái FSM (Enter, Update, PhysicsUpdate, Exit)
│   └── StateMachine.cs                # Bộ điều khiển chuyển đổi State FSM cơ sở
│
├── Stats/
│   ├── EntityStats.cs                 # Base stats: Máu, Mana, Công vật lý/phép, Tốc đánh/chạy, Giáp/Kháng, Crit, IDamageable
│   ├── PlayerStats.cs                 # Mở rộng cho Player: Level, Exp, SkillPoints, tăng cấp nhận thưởng
│   └── ResourceStat.cs                # Quản lý giá trị Hiện tại/Tối đa, tự hồi phục, phát sự kiện OnChanged
│
├── UI/
│   ├── CharacterStatsUI.cs            # BẢNG THUỘC TÍNH (C): Chi tiết Level, EXP, HP/MP Regen, Công, Thủ, Kháng, Crit
│   ├── FloatingTextItem.cs            # Item số nhảy 3D: Billboard, scale nảy, fade out theo đường cong FlyChar
│   ├── FloatingTextManager.cs         # POOLING FLOATING TEXT: SpawnDamage, SpawnHeal, SpawnExp, SpawnMiss
│   ├── PlayerHUD.cs                   # HUD CHÍNH: Máu (Lerp + Ghost Bar vàng), Mana, Level/EXP, 3 ô chiêu thức
│   ├── SkillBookItemUI.cs             # Item trong Bảng Võ Học: Icon, Tên, Cấp, Nút nâng cấp, Nút gán Hotbar
│   ├── SkillBookUI.cs                 # BẢNG VÕ HỌC (K): Danh sách chiêu thức, cộng điểm kỹ năng, gán Hotbar slot
│   ├── SkillSlotUI.cs                 # Ô skill HUD đơn lẻ: Icon, Overlay xoay 360°, đếm giây Cooldown, Mana cost
│   └── UIModalManager.cs              # QUẢN LÝ CỬA SỔ MODAL: Stack mở/đóng UI, ESC key, chặn Input chiến đấu
│
└── Editor/
    ├── CharacterStatsUIBuilder.cs     # Menu Tool tự động dựng Bảng thuộc tính (Phím C) vào Scene
    ├── ComboTimingAnalyzer.cs         # Tool Editor phân tích xương tìm điểm khớp Animation Combo tối ưu
    ├── EnemySpawnPointEditor.cs       # Custom Inspector & Menu tạo bãi quái EnemySpawnPoint
    ├── NpcSpawnerBuilder.cs           # Tool dựng NPC/Quái ra Scene trực tiếp từ NpcTemplate.csv
    ├── PlayerAnimationPopulator.cs    # Tool quét thư mục nạp clips vào component Animation Body/Head
    ├── PlayerHUDBuilder.cs            # Tool tạo tự động Canvas UI HUD chuẩn vào Scene
    ├── SkillBookUIBuilder.cs          # Tool tự động tạo Canvas Bảng Võ Học (Phím K) vào Scene
    ├── SkillEffectVerifier.cs         # Tool Unit Test kiểm tra tính toàn vẹn của Skill trong Console
    ├── SmartPackageImporter.cs        # Tool nhập .unitypackage tự động khử trùng Shader & C# GUID
    └── VietnameseFontAutoSetup.cs     # Tool tự động cấu hình Fallback Font tiếng Việt cho TextMeshPro
```

---

## 4. BẢNG CHỈ MỤC TÁC VỤ (AI TASK ROUTER)

| Tác vụ cần xử lý | File TRỌNG TÂM cần đọc & sửa | File phụ (chỉ đọc khi thiếu ngữ cảnh) |
| :--- | :--- | :--- |
| **Di chuyển / Xoay người / Trọng lực** | `Player/CharacterMovement.cs`<br>`Player/States/PlayerMoveState.cs` | `Player/PlayerController.cs`<br>`Combat/GameConstants.cs` |
| **Ngắm chiêu / Smartcast / Indicator VFX** | `Player/PlayerAiming.cs` | `Combat/CombatEnums.cs`<br>`Combat/CombatFormula.cs` |
| **Tung chiêu / Combo / Hủy chiêu (Cancel)** | `Player/States/PlayerAttackState.cs`<br>`Player/PlayerCombat.cs` | `Skills/SkillData.cs`<br>`Combat/CombatFormula.cs` |
| **Bị thương (Hit Reaction / Flinch)** | `Player/States/PlayerHurtState.cs`<br>`Enemy/States/EnemyHurtState.cs` | `LegacyAnimationController.cs`<br>`Stats/EntityStats.cs` |
| **Hitbox / Tính Damage / Hồi máu** | `Skills/SkillDamageResolver.cs` | `Combat/CombatFormula.cs`<br>`Stats/EntityStats.cs` |
| **Đạn bay / Bám đuổi / Đạn nảy (Missile)** | `Skills/ProjectileController.cs`<br>`Skills/ProjectilePool.cs` | `Data/MissileDatabase.cs`<br>`Combat/EffectManager.cs` |
| **Cấu hình Kỹ năng & ActionEvent từ CSV** | `Skills/SkillData.cs`<br>`Skills/SkillCsvParser.cs`<br>`Skills/ActionEventParser.cs`<br>`Settings/GameData/Combat/CustomSkill.csv` | `Skills/SkillDatabase.cs`<br>`Data/GameDatabase.cs` |
| **Bảng Võ Học / Nâng Cấp & Gán Phím (K)** | `Skills/PlayerSkillManager.cs`<br>`UI/SkillBookUI.cs`<br>`UI/SkillBookItemUI.cs` | `Player/PlayerCombat.cs`<br>`Editor/SkillBookUIBuilder.cs` |
| **AI / Hành vi Quái vật / Boss** | `Enemy/EnemyBrain.cs`<br>`Enemy/EnemyPerception.cs`<br>`Enemy/EnemyController.cs`<br>`Enemy/States/EnemyIdleState.cs`<br>`Enemy/States/EnemyChaseState.cs`<br>`Enemy/States/EnemyAttackState.cs`<br>`Enemy/States/EnemyReturnState.cs` | `Data/NpcAiDatabase.cs`<br>`NPC/NpcTemplateDatabase.cs` |
| **Hoạt ảnh / Khớp xương / Đồng bộ Body & Head** | `LegacyAnimationController.cs`<br>`Combat/VfxLockRotation.cs`<br>`Data/PartSlotDatabase.cs` | `Skills/CastActionID.cs`<br>`Combat/GameConstants.cs` |
| **Chỉ số, Cấp độ, EXP, Giáp, Kháng, Crit** | `Stats/EntityStats.cs`<br>`Stats/PlayerStats.cs`<br>`Combat/CombatFormula.cs`<br>`Data/ExpRuleDatabase.cs`<br>`Data/PlayerLevelDatabase.cs` | `Data/NpcStatDatabase.cs`<br>`Stats/ResourceStat.cs` |
| **Giao diện HUD / Thanh máu Lerp / Ô chiêu** | `UI/PlayerHUD.cs`<br>`UI/SkillSlotUI.cs` | `Editor/PlayerHUDBuilder.cs` |
| **Bảng Thuộc Tính Nhân Vật (C)** | `UI/CharacterStatsUI.cs` | `Editor/CharacterStatsUIBuilder.cs`<br>`Stats/PlayerStats.cs` |
| **Chữ số nhảy chiến đấu (Floating Text)** | `UI/FloatingTextManager.cs`<br>`UI/FloatingTextItem.cs`<br>`Data/FlyCharDatabase.cs` | `Data/FlyCharData.cs`<br>`Settings/GameData/Combat/FlyChar.csv` |
| **Âm thanh kỹ năng, Hit sound, Voice** | `Audio/SoundManager.cs`<br>`Audio/SoundDatabase.cs` | `Audio/SoundData.cs`<br>`Settings/GameData/Feedback/Sound.csv` |
| **Bãi quái (Spawner) / Sinh quái Editor** | `Enemy/EnemySpawnPoint.cs`<br>`Editor/EnemySpawnPointEditor.cs`<br>`Editor/NpcSpawnerBuilder.cs` | `NPC/NpcTemplateDatabase.cs`<br>`Enemy/EnemyController.cs` |

---

## 5. THỨ TỰ NẠP DỮ LIỆU BẮT BUỘC (DEPENDENCY ORDER)

Trong `GameDatabase.EnsureLoaded()`, dữ liệu **phải** được khởi tạo đúng thứ tự sau để tránh `NullReferenceException`:
1. `NpcAi` (`AI/*.ini`)
2. `Sounds` (`Feedback/Sound.csv`)
3. `FlyChars` (`Combat/FlyChar.csv`)
4. `PlayerLevels` (`Progression/PlayerLevel.csv`)
5. `ExpRules` (`Progression/ExpRule.csv`)
6. `Effects` (`VFX_Slots/EffectRes.csv`)
7. `Missiles` (`Combat/Missile.csv`) $\rightarrow$ *phụ thuộc `EffectDatabase`*
8. `StateEffects` (`VFX_Slots/StateEffect.csv`) $\rightarrow$ *phụ thuộc `EffectDatabase`*
9. `PartSlots` (`VFX_Slots/PartSlot.csv`)
10. `NpcRes` (`NPC/NpcRes.csv`)
11. `NpcAttributes` (`NPC/NpcAttribute.csv`)
12. `NpcStats` (`NPC/NpcStats.csv`)
13. `NpcTemplates` (`NPC/NpcTemplate.csv`, `Character.csv`) $\rightarrow$ *phụ thuộc `NpcRes`, `NpcAttribute`, `NpcAi`*
14. `Skills` (`Combat/Skill.csv`, `ActionEvent.csv`, `CustomSkill.csv`) $\rightarrow$ *phụ thuộc toàn bộ bảng trên*

---

## 6. CÁC LỖI TỐI KỴ CẦN TRÁNH (AI PITFALLS)

* ❌ **Cấm dùng Animator Mecanim:** Không gọi `Animator.Play` hay `animator.SetTrigger`. Bắt buộc gọi qua `LegacyAnimationController.PlayAction(...)`.
* ❌ **Cấm đổi FPS:** Không đổi hệ số chia thời gian sang 30 FPS hoặc 60 FPS. Toàn bộ animation keyframe, action frame và cooldown đều chuẩn hóa ở **15 FPS**.
* ❌ **Cấm cấp phát mới lặp đi lặp lại:** Không dùng `Instantiate` cho đạn hoặc VFX. Bắt buộc dùng `ProjectilePool.Instance.Get()` và `EffectManager.Instance.SpawnEffect(...)`.
* ❌ **Cấm dùng Physics Overlap không đệm:** Dùng `Physics.OverlapSphereNonAlloc` với bộ đệm tĩnh (`hitBuffer`) thay vì `Physics.OverlapSphere` để tránh rác GC.
* ❌ **Cấm quên gán Layer đệ quy cho Quái:** Khi tạo quái vật, root và toàn bộ xương/mesh con phải được gán Layer `Enemy` (Layer 6) qua đệ quy. Nếu sót, hệ thống ngắm (`PlayerAiming`) và quét đòn (`SkillDamageResolver`) sẽ bỏ qua mục tiêu.
* ❌ **Cấm đặt VFX sát sàn phẳng:** Mọi hiệu ứng mặt đất phải gọi `CombatFormula.SnapToGround(..., GROUND_VFX_Y_OFFSET)` (offset $0.02\text{m}$) để tránh lỗi nhấp nháy xuyên mặt sàn (Z-Fighting).
* ❌ **Cấm hardcode chuỗi định danh:** Không viết tay `"Player"`, `"Enemy"`, `"st"`, `"at01"`, `"Players/Npcs/Prefabs/"`. Sử dụng hằng số từ `GameConstants`.
* ❌ **Cấm quên Reset Static:** Mọi Singleton, Cache Dictionary hoặc Event Static phải có hàm dọn dẹp gắn cờ `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` để đảm bảo tương thích hoàn hảo khi bật *Enter Play Mode Options* trong Unity Editor.
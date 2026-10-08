# ⚔️ QUY CHUẨN DỮ LIỆU KỸ NĂNG, ĐẠN ĐẠO & ĐỊNH HƯỚNG CHIÊU THỨC

> **Tài liệu thành phần:** Nằm trong bộ quy chuẩn dữ liệu [DATA_CONVENTIONS_V2.md](../DATA_CONVENTIONS_V2.md).
> **Phạm vi:** Cấu hình kỹ năng (Skill.csv, AutoSkill.csv, SkillLevelUp.csv), đạn đạo (Missile.csv), khung hoạt ảnh sự kiện (ActionEvent.csv), cơ chế Selector định hướng.

---

## 1. BẢNG TÙY BIẾN KỸ NĂNG `CustomSkill.csv` (CUSTOM GAMEPLAY LAYER)

Bảng dành riêng cho thiết kế game mới: Tự do tùy biến Sát thương, Hồi máu, Hồi chiêu, Tiêu hao, Trần cấp độ và Tên/Mô tả trong khi kế thừa 100% Animation, Timeline ActionEvent và VFX từ `BaseSkillId`.

| Tên Cột | Kiểu | Ý nghĩa & Nguyên tắc vận hành |
| :--- | :---: | :--- |
| **`SkillId`** | `int` | ID kỹ năng mới của người chơi (Khóa chính). |
| **`BaseSkillId`** | `int` | ID chiêu gốc trong `Skill.csv` để clone toàn bộ Animation, ActionEvent, Missile, VFX, Hitbox. |
| **`SkillName`** | `string` | Tên chiêu thức tùy chỉnh hiển thị trong game. |
| **`Description`** | `string` | Mô tả hiệu ứng, sát thương chiêu thức. |
| **`IconPath`** | `string` | Đường dẫn Sprite icon mới (Nếu để trống `""` $\rightarrow$ tự lấy icon của `BaseSkillId`). |
| **`MaxLevel`** | `int` | Cấp độ tối đa của kỹ năng (Mặc định `20` cho đòn thường, `10` cho chiêu thức). |
| **`Cooldown`** | `float` / `string` | Thời gian hồi chiêu (giây). Hỗ trợ số đơn (`10`) hoặc chuỗi mốc (`"{1,10},{5,8},{10,5}"`). |
| **`ManaCost`** | `float` / `string` | Lượng MP tiêu hao. Hỗ trợ số đơn (`50`) hoặc chuỗi mốc (`"{1,50},{10,80}"`). |
| **`BaseDamage`** | `float` / `string` | Sát thương cơ bản. Hỗ trợ chuỗi mốc nội suy (`"{1,10},{5,25},{10,60},{20,150}"`). |
| **`PhysScale`** | `float` / `string` | Hệ số sát thương Công vật lý (`1.0` = 100%). Hỗ trợ chuỗi mốc (`"{1,1.0},{10,1.5},{20,2.0}"`). |
| **`MagicScale`** | `float` / `string` | Hệ số sát thương Công phép / Nội công. |
| **`BaseHeal`** | `float` / `string` | Lượng máu hồi cố định mỗi nhịp (chiêu hồi máu). Hỗ trợ chuỗi mốc (`"{1,20},{5,60},{10,140}"`). |
| **`HealScale`** | `float` / `string` | Hệ số hồi máu theo Công phép: $\text{Heal} = \text{BaseHeal} + (\text{Công Phép} \times \text{HealScale})$. |
| **`SelectorType`** | `int` | Kiểu hiển thị vòng ngắm Smartcast (`0`: None, `1`: Sector, `2`: Arrow, `3`: Circle, `4`: Lock). |
| **`SelectorRange`** | `float` | Tầm xa hiển thị vòng ngắm (Mét). |

> 💡 **Quy tắc nội suy Level Scaling:** Mọi cột chỉ số hỗ trợ format chuỗi `"{1,Val1},{5,Val2},{10,Val3}"`. Engine tự động nội suy tuyến tính (Linear Interpolation) cho các cấp trung gian qua `CsvParserHelper.ParseLevelValue`.

---

## 2. CHI TIẾT BẢNG `Skill.csv` (GỐC `Skill.tab`)

| Tên Cột | Kiểu | Ý nghĩa & Nguyên lý vận hành | Bằng chứng Source Code |
| :--- | :---: | :--- | :--- |
| **`SkillId`** | `int` | ID duy nhất của kỹ năng (Khóa chính). | `Skill.tab` |
| **`SkillName`** | `string` | Tên hiển thị của kỹ năng (Tiếng Việt). | `Skill.tab` |
| **`Property`** | `string` | Phân loại ngữ cảnh (`Kỹ năng môn phái`, `Kỹ năng Nộ Khí`, `Buff`...). | `Skill.tab` |
| **`SkillType`** | `int` | **Phân loại cơ chế xuất chiêu:**<br>• `0`: Vô hiệu / None.<br>• `1`: **Áp sát cận chiến / khinh công** (`skill_type_melee`).<br>• `2`: **Tức thì đơn thể** (`skill_type_inst_single`).<br>• `3`: **Bị động / Trạng thái nội tại** (`skill_type_passivity`).<br>• `4`: **Đạn tức thì / Không delay** (`skill_type_inst_missile`).<br>• `5`: **Đạn có quỹ đạo bay** (`skill_type_missile`). | `CommonScript/Skill/Define.lua` (`FightSkill.SkillTypeDef`) |
| **`MeleeForm`** | `int` | Kiểu đánh cận chiến: `0`/rỗng = Bình thường, `1` = Lướt áp sát nhanh, `2` = Xoay vòng quanh người. | `Skill.tab` |
| **`StartPosType`** | `int` | **Vị trí tâm xuất phát chiêu/đạn/sub-skill:**<br>• `1`: Xuất phát từ **Caster** (Bản thân người ra chiêu).<br>• `2`: Xuất phát tại **Target** (Vị trí mục tiêu đang khóa hoặc vị trí chạm ngắm).<br>• `3`: Xuất phát tại **Điểm va chạm (HitPoint)**. | `Skill.tab` |
| **`StartDirType`** | `int` | Hướng ngắm ban đầu: `0` = Hướng mặt nhân vật, `1` = Hướng vector chỉ tới mục tiêu. | `Skill.tab` |
| **`WaitTime`** | `int` | **Thời gian trễ xuất chiêu (Frames chuẩn 15 FPS):** Dùng tạo nhịp phân tầng cho Sub-skill (vd: Sub-skill 348..353 delay 1,2,3,4,5 frames để tạo sóng cọc băng tỏa dần). | `Skill.tab` |
| **`ChildID`** | `int` | ID Missile hoặc Sub-skill sinh ra khi thi triển (Trỏ sang `Missile.csv` hoặc `Skill.csv`). | `Skill.tab` |
| **`ChildCount`** | `int` | Số lượng đạn/tia sinh ra trong 1 lần xuất chiêu. | `Skill.tab` |
| **`MissileForm`** | `int` | **Dạng đạn đạo / Hình thái Missile:**<br>• `1`: Đạn bay thẳng bình thường (Linear Straight).<br>• `2`: Đạn bắn chùm hình quạt (Spread Fan).<br>• `3`: **Vòng tròn tỏa ra xung quanh Tâm Spawn (`StartPosType`)** (Nếu `StartPosType=2` là tỏa quanh Target, `1` là quanh Caster).<br>• `4`: **Đạn nảy bật liên hoàn (Chain / Bouncing)** giữa các mục tiêu (vd: Bạch Lộ Ngưng Sương - Skill 308).<br>• `5`: Rơi từ trên trời xuống (Sky Drop / Meteor).<br>• `6`: Vòng tròn AOE tĩnh.<br>• `7`: **Chùm đa đạn đồng loạt / Sóng tỏa** (vd: Giang Hải Ngưng Ba - Skill 310). | `Skill.tab` |
| **`Relation`** | `string` | **Quy tắc quan hệ lọc mục tiêu:**<br>• `+` (Bắt buộc phải có), `-` (Bắt buộc KHÔNG được có), không tiền tố (Có là hợp lệ).<br>• Ví dụ `recover=+assist,self,team,partner,teampartner,-enemy,-dead`. | `SkillSetting.ini` (`[RelationSet]`) |
| **`TimePerCast`** | `int` | Thời gian hồi chiêu (Cooldown) tính theo frame chuẩn 15 FPS (`Cooldown = TimePerCast / 15.0f` giây). | `Skill.tab` |
| **`Series`** | `int` | Ngũ hành kỹ năng: `0`=Vô, `1`=Kim, `2`=Mộc, `3`=Thủy, `4`=Hỏa, `5`=Thổ. | `NpcDefine.lua` (`Npc.Series`) |
| **`CastActionId`** | `int` | ID hoạt ảnh ra đòn (Trỏ sang `ActionName.csv`: `16..19`=at01..04, `21..25`=jn01..05). | `Skill.tab` |
| **`ActionEventID`** | `int` | ID dòng thời gian sự kiện hoạt ảnh (Trỏ sang `ActionEvent.csv`). | `Skill.tab` |
| **`ActionFrontPer`** / **`ActionBackPer`** | `int` | Tỉ lệ phần trăm tiền lắc / hậu lắc của hoạt ảnh. | `Skill.tab` |
| **`StateEffectId`** | `int` | ID hiệu ứng trạng thái buff/debuff gắn kèm (Trỏ sang `StateEffect.csv`). | `Skill.tab` |
| **`SkillStyle`** | `string` | Phong cách chiêu: `normal_melee`, `skill_melee`, `normal_remote`, `skill_remote`, `skill_trigger`, `poison`, `control`, `heal`, `jump`, `buff_attack`, `buff_ex`, `buff_sp`, `curse`, `aura`, `logic`, `show`... | `SkillSetting.ini` (`[SkillStyleDef]`) |
| **`StartSkillID`** | `int` | **ID Sub-skill tự động gọi ngay ở Frame 0** (Kế thừa toàn bộ Target và vị trí `StartPosType` từ chiêu mẹ). | `Skill.tab` |
| **`FlySkillId`** | `int` | **ID Sub-skill kích hoạt theo nhịp** khi đạn/vùng đang bay hoặc duy trì DoT. | `Skill.tab` |
| **`FlyEventInterval`**| `int` | Nhịp thời gian gọi `FlySkillId` (Frames, `15` frames = 1 giây/lần). | `Skill.tab` |
| **`HitSkillID` / `HitSkillID2`**| `int` | **ID Sub-skill kích hoạt khi đạn/chiêu đánh trúng mục tiêu** (gây hiệu ứng debuff, giảm kháng, sát thương nổ phụ). | `Skill.tab` |
| **`VanishedSkillId`**| `int` | ID Sub-skill kích hoạt khi viên đạn hết thời gian sống (`LifeTime`) hoặc tự biến mất. | `Skill.tab` |
| **`CollisionSkillId`**| `int` | ID Sub-skill kích hoạt khi viên đạn va chạm chướng ngại vật/địa hình. | `Skill.tab` |
| **`AttackRadius`** | `int` | Tầm thi triển tối đa tính bằng Centimet (`AttackRadius / 100.0f = Mét`). | `Skill.tab` |
| **`ClassName`** | `string` | Tên hàm xử lý script Lua môn phái (vd: `em_bphlj`, `em_chpd`, `em_blns`). | `CommonScript/Skill/FightSkill.lua` |
| **`MaxUsePoint`** | `int` | Số điểm nạp đạn tối đa (Charge Skill / Hệ thống tích trữ số lần dùng chiêu, vd: 2 lần). | `Skill.tab` |
| **`UsePointRecover`**| `int` | Thời gian hồi 1 điểm sạc tích lũy (Frames chuẩn 15 FPS). | `Skill.tab` |
| **`CostType`** | `int` | Loại tài nguyên tiêu hao: `0` = Không tốn, `1` = Mana/Nội lực, `2` / `3` = Nộ khí (Anger, max `1000`). | `SkillSetting.ini` (`FullAnger=1000`) |
| **`CostValue`** | `int` | Lượng tài nguyên tiêu hao tương ứng với `CostType`. | `Skill.tab` |
| **`TargetSelf`** | `int (0/1)`| `1` = Chiêu thức tự động nhắm vào bản thân người ra chiêu. | `Skill.tab` |
| **`NotChangeActFrame`**| `int (0/1)`| `1` = Khóa cứng frame hoạt ảnh, KHÔNG bị tăng tốc bởi `AttackSpeed`. | `Skill.tab` |
| **`DeathActEventID`**| `int` | ID ActionEvent đặc biệt chạy khi người chơi tử vong trong lúc đang cast chiêu này. | `Skill.tab` |

---

## 3. GIẢI MÃ THAM SỐ ĐA BIẾN TRONG `Skill.csv`

### 🔄 1. Bảng Tra Cứu `Param1..Param6` Theo `MissileForm`:

| `MissileForm` | Ý nghĩa `Param1` | Ý nghĩa `Param2` | Ý nghĩa `Param3` | Ý nghĩa `Param4` | Ý nghĩa `Param5` |
| :---: | :--- | :--- | :--- | :--- | :--- |
| **`1` (Bay thẳng)** | Vị trí đạn ban đầu (Start Offset) | Góc giữa các viên đạn | — | — | — |
| **`2` (Bắn chùm hình quạt)** | Cự ly đến tâm vòng tròn (cm) | Góc chia giữa các tia ($64\text{ units} = 360^\circ$) | Số tia đạn tỏa | — | — |
| **`3` (Vòng tròn quanh Tâm Spawn)** | Bán kính vòng tròn ban đầu | Góc phân bố các tia đạn ($64\text{ units} = 360^\circ$) | — | — | — |
| **`4` (Nảy bật liên hoàn - Chain)** | **Số lần nảy tối đa** (vd: 5 lần) | **Tự động tìm mục tiêu** (`1` = Bật) | **Tầm quét tìm mục tiêu nảy** (cm, vd: 1000cm) | **Số lần nảy lặp lại trên cùng 1 người** (`0` = không giới hạn) | Người đầu tiên trúng đòn có cho nảy tiếp không (`0/1`) |
| **`6` (AOE Vùng tròn tĩnh)** | Bán kính hình tròn (vd: `100` = $1.0\text{m}$) | — | — | — | — |
| **`7` (Chùm đa đạn đồng loạt / Sóng tỏa)** | Khoảng cách cự ly giữa các tia đạn (cm) | — | — | — | — |
| **Trống / Khinh công (Lướt/Dash)** | Tốc độ gia tốc lướt về trước | Frame tối thiểu của động tác | Cự ly lướt tối đa (cm) | Độ dài phá vỡ phòng ngự | Số lần đánh tối đa trên cùng 1 người |

---

### 🏃 2. Cơ Chế Lướt & Khinh Công (`AcceSpeedInfo1..3`):
* **Định dạng chuỗi:** `GiaTốc | TốcĐộKhởiĐiểm | VậnTốcTốiĐa` (Ví dụ: `20|1|100`, `0|1|82`, `-1|20|90`, `-10|50|90`).
  - **`Gia tốc` (Số 1):** 
    - Nếu giá trị **dương** (vd: `20`): Lướt tăng tốc dần (Dash Acceleration).
    - Nếu giá trị **âm** (vd: `-1`, `-10`): Lướt giảm tốc dần (De-acceleration).
    - Nếu giá trị bằng **`0`**: Lướt với vận tốc đều cố định.
  - **`Tốc độ khởi điểm` (Số 2):** Vận tốc ban đầu ngay lúc bắt đầu động tác.
  - **`Vận tốc tối đa` (Số 3):** Ngưỡng tốc độ kẹp trần khi lướt.

---

### ⏳ 3. Cơ Chế Sát Thương Đa Đợt, Nhịp Sinh Đạn & Bãi Đất DoT (`MSGenerate`, `MSGenerateParam`, `ChildCount`, `FlySkillId`):

#### 🅰️ Cơ Chế 1: Chiêu thức sinh đạn liên hoàn theo chu kỳ (`MSGenerate` trong `Skill.csv`)
* **`MSGenerate` (Kiểu sinh đạn):**
  - `0`: Sinh tức thời 1 lần duy nhất (`Instant Single Spawning`).
  - `1`: Sinh đạn theo nhịp dọc theo đường di chuyển (`Trail Spawning`).
  - `2`: **Duy trì bãi sát thương tại chỗ (Area DoT / Hazard Zone Spawning)**. Sinh ra `ChildCount` đợt đạn `ChildID`, mỗi đợt cách nhau `MSGenerateParam` frames (vd: Thiên Vũ Bảo Luân 312: `ChildCount = 12`, `MSGenerateParam = 7` $\rightarrow 12$ đợt sát thương, cách nhau $7/15\text{s} \approx 0.46\text{s}$, duy trì $5.6\text{s}$).
  - `3`: **Mưa rơi liên hoàn ngẫu nhiên từ trên trời xuống (Meteor / Sky Drop Rain)** (vd: Vạn Kiếm Phong Thiên Quyết, Phấn Tinh Lạc Vũ).
  - `4`: **Bẫy hẹn giờ phát nổ định kỳ (Timed Trap Multi-Explosion)**.
  - `5`: **Tụ lực / Dồn tia tăng dần (Rapid Fire / Charge Spawning)**.

$$\text{Tổng thời gian duy trì bãi sát thương} = \frac{\text{ChildCount} \times \text{MSGenerateParam}}{15.0f} \text{ (giây)}$$

#### 🅱️ Cơ Chế 2: Viên đạn/vùng nổ tự lặp lại tác dụng (`DmgInterval` trong `Missile.csv`)
* **`DmgInterval`:** Giãn cách giữa 2 lần gây sát thương/hồi máu của cùng một viên đạn/vùng nổ (Frames).
* **`CanRepeatDmg = 1`:** Bắt buộc phải bật để cho phép viên đạn tác động nhiều lần lên cùng một mục tiêu.
* **`LifeTime`:** Thời gian tồn tại tối đa của viên đạn/vùng nổ (Frames).

$$\text{Số nhịp tác động thực tế của 1 viên đạn} = \left\lfloor \frac{\text{LifeTime}}{\text{DmgInterval}} \right\rfloor$$

#### 🆎 Cơ Chế 3: Sub-skill kích hoạt theo nhịp đạn bay (`FlySkillId` & `FlyEventInterval`)
* Khi viên đạn chính đang bay hoặc duy trì bãi đất, cứ mỗi `FlyEventInterval` frames ($t = \text{FlyEventInterval} / 15.0\text{s}$), hệ thống tự động gọi Sub-skill `FlySkillId` (vd: Từ Hàng Phổ Độ 306 gọi 307 mỗi 15 frames; Vạn Kiếm Quyết 631 gọi 632 mỗi 5 frames).

---

## 4. CHI TIẾT BẢNG `Missile.csv` (GỐC `Missile.tab`)

| Tên Cột | Kiểu | Ý nghĩa & Công thức vận hành trong Unity |
| :--- | :---: | :--- |
| **`MissileId`** | `int` | ID duy nhất của viên đạn/hitbox (Khóa chính). |
| **`MoveKind`** | `int` | **Cơ chế chuyển động:**<br>• `0`: Cố định tại chỗ (Static AOE / Bẫy / Trận pháp).<br>• `1`: Bay thẳng theo hướng bắn (Linear Directional).<br>• `2`: **Đạn tự bám đuổi / uốn lượn theo mục tiêu (Homing / Tracking)**.<br>• `3`: Di chuyển gắn liền theo thân người Caster (Dash Hitbox).<br>• `4`: Đạn/Bẫy cắm cọc tĩnh đặc biệt (Static Trap Tower).<br>• `5`: Đạn bay uốn cong / quay trở lại (Boomerang / Curved).<br>• `6`: Đạn bay xoay vòng quanh thân Caster. |
| **`MissileParam1..3`**| `int/string`| **Tham số chuyển động đặc biệt:**<br>• Khi `MoveKind = 5` (Boomerang): `Param1` = Độ cong quỹ đạo Bezier.<br>• Khi `MoveKind = 6` (Đạn xoay vòng): `Param1 = 1` (Khi quay về sẽ tự động bám theo người chơi), `Param2` = Bán kính quỹ đạo xoay. |
| **`Speed`** | `int` | Vận tốc bay (`Velocity = Speed / 10.0f` m/s). |
| **`AcceSpeed`** | `int` | Gia tốc tăng tốc khi bay (`Acceleration = AcceSpeed / 10.0f` $m/s^2$). |
| **`DmgRangeType`** | `int` | `0`: Đơn mục tiêu (Single Target), `1`: Vùng tròn / Khối cầu (AOE Sphere). |
| **`DmgRange`** | `int` | Bán kính Hitbox ngang (`Radius = DmgRange / 10.0f` mét). |
| **`DmgRangeY`** | `int` | Chiều cao vùng sát thương Hitbox 3D (`Height = DmgRangeY / 10.0f` mét). |
| **`IgnoreDmgRange`** | `int` | Bán kính vùng mù an toàn sát thân Caster không bị dính đạn (`IgnoreRadius / 10.0f` mét). |
| **`DmgRangeSpeed`** | `int` | Tốc độ mở rộng bán kính nổ theo thời gian. |
| **`LifeTime`** | `int` | Thời gian sống tối đa của đạn (`LifeTime / 15.0f` giây). |
| **`DmgInterval`** | `int` | **Nhịp giãn cách giữa các lần gây sát thương/hồi máu lặp lại** (`Interval = DmgInterval / 15.0f` giây). |
| **`DelayDeleteFrame`**| `int` | Thời gian trễ trước khi hủy GameObject đạn (Frames, để VFX fade out). |
| **`IsDmgVanish`** | `int (0/1)` | `1` = Đạn chạm trúng mục tiêu là hủy ngay lập tức (Single Hit Projectile). |
| **`CanRepeatDmg`** | `int (0/1)` | `1` = Được phép gây sát thương/hồi máu nhiều lần (kết hợp với `DmgInterval`). |
| **`IsDrag`** | `int (0/1)` | `1` = Đạn cuốn và kéo theo kẻ địch trúng đòn dọc đường bay. |
| **`MissileResID`** | `int` | ID Prefab 3D của đạn khi bay (Trỏ sang `EffectRes.csv`). |
| **`CollResID`** | `int` | ID Prefab 3D nổ khi va chạm (Hit Impact VFX, trỏ `EffectRes.csv`). |
| **`VanishResID`** | `int` | ID Prefab 3D khi đạn tự tiêu biến mà không va chạm. |
| **`EnemyResID`** | `int` | ID Prefab 3D hiển thị riêng cho kẻ địch nhìn thấy (để nhận biết vùng nguy hiểm đỏ). |
| **`PosOffsetLenght`**| `int` | Độ lệch khoảng cách spawn từ Tâm Spawn (`Offset = PosOffsetLenght / 100.0f` m). |
| **`MissileHeight`** / **`HeightSpeed`** | `int` | Độ cao xuất hiện ban đầu và tốc độ rơi/bay theo trục đứng. |
| **`CollSoundID` / `FlySoundID` / `VanishSoundID`** | `int` | ID âm thanh Wwise lúc nổ va chạm, bay tuần hoàn và biến mất (Trỏ sang `Sound.csv`). |
| **`ColFollowTarget`**| `int (0/1)`| `1` = Hiệu ứng va chạm đính chặt và di chuyển bám theo mục tiêu. |
| **`CollBrigth`** | `int` | Cường độ chớp sáng màn hình (`Alpha = CollBrigth / 1000.0f`). |
| **`CollBrigthFrame`**| `int` | Số frame duy trì chớp sáng màn hình. |
| **`IsIgnoreBarrier`**| `int (0/1)`| `1` = Bỏ qua chướng ngại vật/vật cản địa hình. |
| **`IsHitFloat`** | `int (0/1)`| `1` = Đạn đánh trúng hất tung mục tiêu lên không trung. |

---

## 5. CHI TIẾT BẢNG `ActionEvent.csv` & `ActionEventDes.tab` (TOÀN BỘ SỰ KIỆN TIMELINE)

> **📌 Quy ước Frame & EventType:**
> * `EventType`: `1` = Sự kiện khởi tạo ban đầu, `2` = Sự kiện theo Frame ($t = \text{Frame} / 15.0\text{s}$), `3` = Sự kiện khi kết thúc hoạt ảnh.
> * Cột `Frame` tính theo chuẩn **15 FPS**. Riêng `InstantDir` tính theo chuẩn **45 FPS** ($1/45\text{s}$).

| Tên Sự Kiện (`EventName`) | `EventParam1` | `EventParam2` | `EventParam3` | `EventParam4` | `EventParam5` | Ý nghĩa thực tế & Bằng chứng mã nguồn (`ActionEventDes.tab`) |
| :--- | :---: | :---: | :---: | :---: | :---: | :--- |
| **`CastSkill`** | `SkillId` | `SkillLevel` | Đồng bộ Act | — | — | **Thời điểm chính xác sinh đạn / nổ sát thương** của chiêu. |
| **`PlayEffect`** | `ResID` (VFX) | Tuần hoàn (`0/1`) | Không scale theo tốc đánh (`0/1`) | **`SlotId`** (Khớp xương) | `Duration` (-1 là vô hạn) | Sinh Prefab VFX `ResID` gắn vào khớp `SlotId`. |
| **`PlayEffectNoClear`**| `ResID` | Tuần hoàn (`0/1`) | Không scale tốc đánh | `SlotId` | `Duration` | Phát VFX không bị xóa bởi lệnh `ClearEffect` sau đó. |
| **`UnbindEffect`** | `ResID` | — | — | — | — | Tháo gỡ Prefab VFX không cho bám theo NPC nữa. |
| **`ClearEffect`** | `ResID` | Xóa Npc liên quan | — | — | — | Xóa sạch hiệu ứng chỉ định trên nhân vật. |
| **`PlaySound`** | `SoundID` | — | — | — | — | Phát âm thanh từ Wwise (`Sound.csv`). |
| **`StopSound`** | `SoundID` | Delay frame lưu giữ | — | — | — | Dừng âm thanh khi bị ngắt chiêu. |
| **`PlayShake`** | Dao động (1=1cm) | Tốc độ rung | Số lần rung | Trước sau? | Bắt buộc? | Rung màn hình Camera Shake. |
| **`StopShake`** | Bắt buộc (`0/1`) | — | — | — | — | Ngừng rung camera. |
| **`InstantDir`** | Góc quay / Frame | — | — | — | — | Tốc độ xoay mặt về mục tiêu (Độ/Frame, **chuẩn 45 FPS**). |
| **`CrossFade`** | Tỉ lệ vượt mức ($10000/1000=1$) | Vượt mức / ($Frame \times Frame$) | Vượt mức tối đa | — | — | Hòa trộn mượt chuyển tiếp hoạt ảnh. |
| **`MovePos`** | Quãng đường (cm) | Tốc độ (cm/frame) | Khoảng cách dừng (cm) | Gia tốc | — | Ép nhân vật lướt tới trước. |
| **`MoveBack`** | Quãng đường (cm) | Tốc độ (cm/frame) | Gia tốc | — | — | Ép nhân vật lùi về sau. |
| **`CanDoSkill`** | Cờ so sánh Priority | — | — | — | — | `0` hoặc rỗng: Priority mới > Priority hiện tại thì được cast; `1`: Priority mới $\ge$ Priority hiện tại. |
| **`SetCanDoSkillFrame`**| Thời gian (frames) | Cấp ưu tiên | — | — | — | **Cửa sổ hủy chiêu / GCD**: Thiết lập thời gian tối thiểu trước khi được phép dùng chiêu kế tiếp. |
| **`CanDoRun`** | — | — | — | — | — | **Cho phép ngắt động tác thừa sớm** nếu bấm di chuyển (Animation Cancel). |
| **`LinkSkillInit`** | Frame hiệu lực cuối | `SkillId_1` | `SkillId_2` | `SkillId_3` | — | Khởi tạo thời gian chờ combo liên hoàn theo độ ưu tiên chiêu. |
| **`CastLinkSkill`**| Di chuyển ngắt chiêu (`0/1`)| — | — | — | — | Chuyển tiếp sang hoạt ảnh chiêu kế tiếp trong combo. |
| **`NpcChangeSize`**| Kích thước (%) | Tốc độ (%) | — | — | — | Phóng to/thu nhỏ Model nhân vật. |
| **`OpenSceneGray` / `CloseSceneGray`**| Delay frame đóng | — | — | — | — | Phủ tối/xám màn hình khi ra tuyệt chiêu Nộ khí. |
| **`ChangeBright` / `ClearBright`**| Tỉ lệ độ sáng ($100=1.0$) | — | — | — | — | Chớp sáng toàn thân nhân vật khi xuất chiêu. |
| **`Protected`** | Bật bảo vệ (`0/1`)| — | — | — | — | Miễn nhiễm sát thương trong lúc cast chiêu. |
| **`HeadUIVisable` / `BodyVisable`**| `0/1` | — | — | — | — | Ẩn/Hiện thanh máu trên đầu hoặc thân thể nhân vật. |
| **`ShadowActive`** | Bật bóng (`0/1`) | — | — | — | — | Bật/Tắt bóng đổ nhân vật. |
| **`ModelEffectVisible`**| `0/1` | — | — | — | — | Ẩn/Hiện Mesh Renderer nhân vật. |
| **`IgnoreSpeState`**| `0/1` | — | — | — | — | Bỏ qua hiệu ứng khống chế trong thời gian cast. |
| **`RemoveBuff`** | `SkillId` | — | — | — | — | Xóa buff chỉ định trên người. |
| **`SetSkillCD`** | `SkillId` | — | — | — | — | Thiết lập bắt đầu tính hồi chiêu ngay tại frame này. |
| **`ReceiveDmgBreakAct`**| — | — | — | — | — | Cho phép bị ngắt chiêu nếu trúng đòn sát thương. |
| **`ChangeAct`** | `ActId` | Frame động tác | Player `ActId` | — | — | Đổi sang hoạt ảnh khác giữa chừng. |
| **`UseLastAct`** | Dùng act cuối? | Dùng frame hiện tại? | Dùng frame cuối? | — | — | Giữ nguyên tư thế frame cuối cùng của hoạt ảnh trước. |
| **`DoCallScript`** | Mã lệnh script | — | — | — | — | Gọi hàm Lua kịch bản đặc biệt. |
| **`ControlPuppet`**| ID Skill Rối | ID Đạn Rối | — | — | — | Điều khiển Rối/Phân thân đồng thời tung chiêu. |
| **`ForceSetWorldPos`**| $X \times 1000$ | $Y \times 1000$ | $Z \times 1000$ | $RotY \times 1000$ | — | Ép dịch chuyển tức thời đến tọa độ thế giới chính xác. |
| **`ForceSetWorldPosCancel`**| — | — | — | — | — | Hủy ép tọa độ thế giới. |

---

## 6. HỆ THỐNG ĐỊNH HƯỚNG, CHỈ ĐỊNH MỤC TIÊU & CẤU HÌNH SELECTOR

Hệ thống Target & Aiming Reticle được điều khiển phối hợp qua các bảng dữ liệu và quy tắc suy luận tự động:

### 🎯 1. Bảng `AttackSkill.csv` (Cơ chế phân loại tấn công & Tự đánh):
* **`AttackType = 1` (`Normal`)**: Chiêu thường / AOE quanh thân (Không ép hướng).
* **`AttackType = 2` (`Direction`)**: Chiêu **định hướng tự do** (Linear Skillshot).
* **`AttackType = 3` (`Target`)**: Chiêu **bắt buộc khóa mục tiêu** (Target-Locked).
* **`AttackType = 4` (`Line`)**: Chiêu đâm đường thẳng xuyên thấu (vd: Huyền Băng Xuyên Vân 6410).
* **`AutoFightTarget`**: `1` = AI tự đánh bắt buộc phải tìm thấy mục tiêu mới xuất chiêu.

### 📐 2. Phân loại `SkillSelectorType` & Quy tắc Tự Động Suy Luận (Auto-Inference):
Khi `SelectorType` trong CSV để trống (`0`):
* **`None (0)`**: Chiêu buff bản thân (`targetSelf = 1` hoặc `Relation == self`) hoặc chiêu đánh thường cơ bản.
* **`SmartcastCircleAOE (1)`**: Vòng tròn chọn vùng đất (Dành cho bãi nổ tĩnh `StaticCircle` / `StaticTrap`).
* **`DirectionalArrow (2)`**: Mũi tên định hướng Skillshot (`MoveKind == Linear` hoặc `MissileForm == StraightLinear / SpreadFan / MultiMissileWave`).
* **`TargetLock (3)`**: Vòng tròn khóa mục tiêu đơn thể (Tự động gán cho: `StartPosType == Target (2)`, `MoveKind == HomingTracking (2)`, `MissileForm == ChainBouncing (4)` như chiêu 308, `Relation == recover/friend`, `SkillType == InstSingle (2)`).

Tầm ngắm `SelectorRange` nếu trống sẽ tự động lấy từ `AttackRadius / 100`, hoặc tầm đạn bay (`Speed * LifeTime / 15 / 100`), hoặc `rangeInMeters`.

### ✏️ 3. Ghi đè tùy biến trong `CustomSkill.csv`:
Hỗ trợ 2 cột **`SelectorType`** và **`SelectorRange`**. Nếu để trống sẽ sử dụng giá trị mặc định / suy luận tự động.

### 🔍 4. Bảng `SkillSelector.csv` (Bộ lọc ưu tiên mục tiêu thông minh):
* **`hurt_maxhp`**: Tự động ưu tiên đồng minh/bản thân bị mất nhiều % máu nhất (Dùng cho Hồi máu Từ Hàng Phổ Độ 306, Bàn Băng Phi Sương 6414).
* **`flag_npc`**: Ưu tiên nhắm vào cờ hoặc NPC mục tiêu nhiệm vụ.

---

## 7. CHI TIẾT BẢNG `AutoSkill.csv` (GỐC `AutoSkill.tab`)

Bảng quy định cơ chế tự động kích hoạt chiêu thức / kỹ năng bị động phản đòn:

| Tên Cột | Kiểu | Ý nghĩa logic |
| :--- | :---: | :--- |
| **`AutoId`** | `int` | ID quy tắc tự động (Khóa chính). |
| **`AutoName`** | `string` | Tên quy tắc / Kỹ năng kích hoạt. |
| **`AutoType`** | `int` | **Loại điều kiện kích hoạt (1..31):**<br>• `1`: Bị tấn công phản kích (`Đồng Hành-Phản Kích`, `La Hán Trận`).<br>• `2`: Đánh thường/Kỹ năng trúng đích kích hoạt thêm hiệu ứng.<br>• `3`: Sinh lực xuống thấp dưới ngưỡng kích hoạt hộ mạng (`Tọa Vọng Vô Ngã`, `Kim Cang Nộ Mục`).<br>• `4`: Sinh lực thấp kích hoạt bất tử / hồi sinh lực (`Phá Phủ Trầm Châu`, `Miên Lý Tàng Châm`).<br>• `5`: Kích hoạt khi tử vong (`Quyết Biệt`).<br>• `6`: Kích hoạt toàn màn hình.<br>• `8`: Khiên vỡ kích hoạt hiệu ứng.<br>• `13`: Bị tấn công phản kích gây khống chế (`La Hán Kim Thân`).<br>• `14`: Tự động hồi phục định kỳ (`Khô Mộc Phùng Xuân`, `Niết Bàn Trùng Sinh`).<br>• `15`: Phá tàng hình kích hoạt Buff (`Huyết Nguyệt Ảnh`).<br>• `16`: Hào quang Hộ Chủ Đồng Hành.<br>• `17`: Kích hoạt theo bước di chuyển (`Phi Sa Bộ Pháp`).<br>• `18`: Dưới 30% HP tăng sát thương.<br>• `20`: Kích hoạt theo sự kiện môi trường / kịch bản.<br>• `22`: Kết thúc đóng băng gây nổ sát thương.<br>• `24`: Đổi vũ khí / chuyển thế nhận Buff (`Dương Môn Thương - Cung`).<br>• `25`: Né tránh đòn thành công kích hoạt Buff (`Bất Diệt Quang`).<br>• `26`: Nổ hộ thuẫn gây sát thương.<br>• `27`: Trạng thái Say Rượu (`Hàng Long Hữu Hối`).<br>• `31`: Máu dưới 75% miễn dịch khống chế (`Chân Nguyên`). |
| **`CastPercent`** | `int` hoặc `string` | Tỉ lệ phần trăm kích hoạt thành công (`int` cố định hoặc chuỗi `{Lv,Percent}`). |
| **`CastSkillId`** | `int` | ID Sub-skill được tự động gọi ra (Trỏ sang `Skill.csv`). |
| **`CastSkillLevel`**| `int` | Cấp độ Sub-skill (`-1` = Bằng cấp chiêu cha, `-2` = Cố định). |
| **`ParentSkillId`** | `string` | Danh sách ID kỹ năng nguồn được phép kích hoạt rule này. |
| **`RelativeSkillStyle`**| `string`| Lọc phong cách chiêu thức gây sát thương để kích hoạt phản đòn (`normal_melee,skill_melee...`). |
| **`TargetStateStyle`**| `string`| Lọc trạng thái mục tiêu để kích hoạt. |
| **`MinPerCastTime`**| `string` | Thời gian hồi nội bộ giữa 2 lần kích hoạt (`15*15` = 225 frames = 15 giây). |
| **`MaxCastCount`** | `int` | Số lần kích hoạt tối đa (`-1` = Vô hạn, `>0` = Giới hạn số lần). |

---

## 8. CHI TIẾT BẢNG `SkillLevelUp.csv` & `SkillSlot.csv`

* **`SkillLevelUp.csv`**: Bảng chi phí và điều kiện nâng cấp kỹ năng:
  - `GroupId`: Nhóm tiến cấp kỹ năng.
  - `SkillLevel`: Cấp độ kỹ năng đích.
  - `ReqLevel`: Cấp độ nhân vật yêu cầu tối thiểu.
  - `Coin`: Lượng Bạc tiêu hao.
  - `Exp`: Lượng Tu vi / Kinh nghiệm tiêu hao.
  - `SkillPoint`: Số điểm kỹ năng cần tiêu hao (Mặc định `1` điểm).
  - `FightPower`: Lực chiến cộng thêm cho nhân vật.
* **`SkillSlot.csv`**: Cấu hình gán kỹ năng vào các ô phím bấm:
  - `SkillID`: ID kỹ năng.
  - `IconAltlas` & `Icon`: Sprite icon hiển thị trên HUD.
  - `BtnName1`: Nút phím được phép gán kỹ năng này vào (`Attack`, `Skill1`..`Skill4`).

---

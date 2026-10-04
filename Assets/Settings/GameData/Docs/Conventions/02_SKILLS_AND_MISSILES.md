# ⚔️ QUY CHUẨN DỮ LIỆU KỸ NĂNG, ĐẠN ĐẠO & ĐỊNH HƯỚNG CHIÊU THỨC

> **Tài liệu thành phần:** Nằm trong bộ quy chuẩn dữ liệu [DATA_CONVENTIONS_V2.md](../DATA_CONVENTIONS_V2.md).
> **Phạm vi:** Cấu hình kỹ năng (Skill.csv, AutoSkill.csv, SkillLevelUp.csv), đạn đạo (Missile.csv), khung hoạt ảnh sự kiện (ActionEvent.csv), cơ chế Selector định hướng & Môn phái (FactionSkill.csv).

---

## 2. CHI TIẾT BẢNG `Skill.csv`

Bảng định nghĩa thuộc tính cốt lõi của mọi chiêu thức.

| Tên Cột | Kiểu | Ý nghĩa & Nguyên lý vận hành | Bằng chứng Source Code |
| :--- | :---: | :--- | :--- |
| **`SkillId`** | `int` | ID duy nhất của kỹ năng (Khóa chính). | `Skill.tab` |
| **`SkillName`** | `string` | Tên hiển thị của kỹ năng (Tiếng Việt). | `Skill.tab` |
| **`Property`** | `string` | Phân loại ngữ cảnh (`Kỹ năng môn phái`, `Thử nghiệm`, `Buff`...). | `Skill.tab` |
| **`SkillType`** | `int` | **Phân loại cơ chế xuất chiêu:**<br>• `0`: Vô hiệu / None.<br>• `1`: **Kỹ năng áp sát cận chiến / khinh công** (`skill_type_melee`).<br>• `2`: **Tác dụng tức thì đơn thể** (`skill_type_inst_single`).<br>• `3`: **Bị động / Trạng thái nội tại** (`skill_type_passivity`).<br>• `4`: **Đạn tức thì / Không delay** (`skill_type_inst_missile`).<br>• `5`: **Đạn có quỹ đạo bay** (`skill_type_missile`). | `CommonScript/Skill/Define.lua` (`FightSkill.SkillTypeDef`) |
| **`MeleeForm`** | `int` | Kiểu đánh cận chiến: `0`/rỗng = Bình thường, `1` = Lướt áp sát nhanh, `2` = Xoay vòng quanh người. | `Skill.tab` |
| **`StartPosType`** | `int` | **Vị trí xuất phát chiêu/đạn:**<br>• `1`: Xuất phát từ **Caster** (Bản thân người ra chiêu).<br>• `2`: Xuất phát hướng về / tại **Target** (Mục tiêu đang khóa).<br>• `3`: Xuất phát tại **Điểm va chạm (HitPoint)**. | `Skill.tab` |
| **`StartDirType`** | `int` | Hướng ngắm ban đầu: `0` = Hướng mặt nhân vật, `1` = Hướng vector chỉ tới mục tiêu. | `Skill.tab` |
| **`ChildID`** | `int` | ID Missile hoặc Sub-skill sinh ra khi thi triển (Trỏ sang `Missile.csv` hoặc `Skill.csv`). | `Skill.tab` |
| **`ChildCount`** | `int` | Số lượng đạn/tia sinh ra trong 1 lần xuất chiêu. | `Skill.tab` |
| **`MissileForm`** | `int` | **Dạng đạn đạo / Hình thái Missile:**<br>• `1`: Đạn bay thẳng bình thường (Linear Straight).<br>• `2`: Đạn bắn chùm hình quạt (Spread Fan).<br>• `3`: Vòng tròn tỏa ra xung quanh Caster (Circular Ring).<br>• `4`: **Đạn nảy bật liên hoàn (Chain / Bouncing)** giữa các mục tiêu (vd: Bạch Lộ Ngưng Sương - Skill 308).<br>• `5`: Rơi từ trên trời xuống (Sky Drop / Meteor).<br>• `6`: Vòng tròn AOE tĩnh.<br>• **`7`**: **Chùm đa đạn đồng loạt / Sóng tỏa** (vd: Nga Mi Kiếm Pháp 4 - Skill 305, Giang Hải Ngưng Ba - Skill 310). | `Skill.tab` |
| **`Relation`** | `string` | **Quy tắc quan hệ lọc mục tiêu:**<br>• Cú pháp logic: `+` (Bắt buộc phải có), `-` (Bắt buộc KHÔNG được có), không tiền tố (Có là hợp lệ).<br>• Ví dụ `recover=+assist,self,team,partner,teampartner,-enemy,-dead`. | `SkillSetting.ini` (`[RelationSet]`) |
| **`TimePerCast`** | `int` | Thời gian hồi chiêu (Cooldown) tính theo frame chuẩn 15 FPS (`Cooldown = TimePerCast / 15.0f` giây). | `Skill.tab` |
| **`Series`** | `int` | Ngũ hành kỹ năng: `0`=Vô, `1`=Kim, `2`=Mộc, `3`=Thủy, `4`=Hỏa, `5`=Thổ. | `NpcDefine.lua` (`Npc.Series`) |
| **`CastActionId`** | `int` | ID hoạt ảnh ra đòn (Trỏ sang `ActionName.csv`). | `Skill.tab` |
| **`ActionEventID`** | `int` | ID dòng thời gian sự kiện hoạt ảnh (Trỏ sang `ActionEvent.csv`). | `Skill.tab` |
| **`StateEffectId`** | `int` | ID hiệu ứng trạng thái buff/debuff gắn kèm (Trỏ sang `StateEffect.csv`). | `Skill.tab` |
| **`SkillStyle`** | `string` | Phong cách chiêu: `normal_melee`, `skill_melee`, `normal_remote`, `skill_remote`, `skill_trigger`, `poison`, `control`, `heal`, `jump`, `buff_attack`, `buff_ex`, `buff_sp`, `curse`, `aura`, `logic`, `show`... | `SkillSetting.ini` (`[SkillStyleDef]`) |
| **`FlySkillId`** | `int` | **ID Sub-skill kích hoạt theo nhịp** khi đạn/vùng đang tồn tại (DoT / HoT). | `Skill.tab` |
| **`FlyEventInterval`**| `int` | Nhịp thời gian gọi `FlySkillId` (Frames, `15` frames = 1 giây/lần). | `Skill.tab` |
| **`AttackRadius`** | `int` | Tầm thi triển tối đa tính bằng Centimet (`AttackRadius / 100.0f = Mét`). | `Skill.tab` |
| **`ClassName`** | `string` | Tên hàm xử lý script Lua môn phái (vd: `em_pg1`, `em_chpd`, `em_blns`). | `CommonScript/Skill/FightSkill.lua` |
| **`CostType`** | `int` | Loại tài nguyên tiêu hao: `0` = Không tốn, `1` = Mana/Nội lực, `2` = Nộ khí (Anger, max `1000`). | `SkillSetting.ini` (`FullAnger=1000`) |
| **`NotChangeActFrame`**| `int (0/1)`| `1` = Khóa cứng frame hoạt ảnh, KHÔNG bị tăng tốc bởi `AttackSpeed`. | `Skill.tab` |

---

---

## 3. GIẢI MÃ THAM SỐ ĐA BIẾN TRONG `Skill.csv`

### 🔄 1. Bảng Tra Cứu `Param1..Param6` Theo `MissileForm`:

| `MissileForm` | Ý nghĩa `Param1` | Ý nghĩa `Param2` | Ý nghĩa `Param3` | Ý nghĩa `Param4` | Ý nghĩa `Param5` |
| :---: | :--- | :--- | :--- | :--- | :--- |
| **`1` (Bay thẳng)** | Vị trí đạn ban đầu (Start Offset) | Góc giữa các viên đạn | — | — | — |
| **`2` (Bắn chùm hình quạt)** | Cự ly đến tâm vòng tròn (cm) | Góc chia giữa các tia ($64^\circ = 360^\circ$) | Số tia đạn tỏa | — | — |
| **`3` (Vòng tròn quanh người)** | Bán kính vòng tròn ban đầu | Góc phân bố các tia đạn ($64^\circ$) | — | — | — |
| **`4` (Nảy bật liên hoàn - Chain)** | **Số lần nảy tối đa** (vd: 5 lần) | **Tự động tìm mục tiêu** (`1` = Bật) | **Tầm quét tìm mục tiêu nảy** (cm, vd: 1000cm) | **Số lần nảy lặp lại trên cùng 1 người** (`0` = không giới hạn) | Người đầu tiên trúng đòn có cho nảy tiếp không (`0/1`) |
| **`6` (AOE Vùng tròn tĩnh)** | Bán kính hình tròn (vd: `100` = $1.0\text{m}$) | — | — | — | — |
| **`7` (Chùm đa đạn đồng loạt / Sóng tỏa)** | Khoảng cách cự ly giữa các tia đạn (cm) | — | — | — | — |
| **Trống / Khinh công (Lướt/Dash)** | Tốc độ gia tốc lướt về trước | Frame tối thiểu của động tác | Cự ly lướt tối đa (cm) | Độ dài phá vỡ phòng ngự | Số lần đánh tối đa trên cùng 1 người |

---

### 🏃 2. Cơ Chế Lướt & Khinh Công (`AcceSpeedInfo1..3`):
Trong các chiêu thức lướt (Nhất Kích Sát Thần, Tiên Nhân Chỉ Lộ, Bôn Lang Thương) và Khinh công:
* **Định dạng chuỗi:** `GiaTốc | TốcĐộKhởiĐiểm | VậnTốcTốiĐa` (Ví dụ: `20|1|100`, `0|1|82`, `-1|20|90`, `-10|50|90`).
  - **`Gia tốc` (Số 1):** 
    - Nếu giá trị **dương** (vd: `20`): Lướt tăng tốc dần (Dash Acceleration).
    - Nếu giá trị **âm** (vd: `-1`, `-10`): Lướt giảm tốc dần (De-acceleration).
    - Nếu giá trị bằng **`0`**: Lướt với vận tốc đều cố định.
  - **`Tốc độ khởi điểm` (Số 2):** Vận tốc ban đầu ngay lúc bắt đầu động tác.
  - **`Vận tốc tối đa` (Số 3):** Ngưỡng tốc độ kẹp trần khi lướt.

---

### ⏳ 3. Cơ Chế Sát Thương Đa Đợt, Nhịp Sinh Đạn & Bãi Đất DoT (`MSGenerate`, `MSGenerateParam`, `ChildCount`, `FlySkillId`):
Game hỗ trợ 3 cơ chế gây sát thương / hồi máu đa đợt khác nhau, phối hợp chặt chẽ giữa `Skill.csv` và `Missile.csv`:

#### 🅰️ Cơ Chế 1: Chiêu thức sinh đạn liên hoàn theo chu kỳ (`MSGenerate` trong `Skill.csv`)
* **`MSGenerate` (Kiểu sinh đạn):**
  - `0`: Sinh tức thời 1 lần duy nhất (`Instant Single Spawning`).
  - `1`: Sinh đạn theo nhịp dọc theo đường di chuyển (`Trail Spawning`).
  - `2`: **Duy trì bãi sát thương tại chỗ (Area DoT / Hazard Zone Spawning)**. Sinh ra `ChildCount` đợt đạn `ChildID`, mỗi đợt cách nhau `MSGenerateParam` frames (vd: Thiên Vũ Bảo Luân 312: `ChildCount = 12`, `MSGenerateParam = 7` $\rightarrow 12$ đợt sát thương, cách nhau $7/15\text{s} \approx 0.46\text{s}$, duy trì $5.6\text{s}$).
  - `3`: **Mưa rơi liên hoàn ngẫu nhiên từ trên trời xuống (Meteor / Sky Drop Rain)** (vd: Vạn Kiếm Phong Thiên Quyết, Phấn Tinh Lạc Vũ).
  - `4`: **Bẫy hẹn giờ phát nổ định kỳ (Timed Trap Multi-Explosion)**.
  - `5`: **Tụ lực / Dồn tia tăng dần (Rapid Fire / Charge Spawning)**.
* **`ChildCount`:** Tổng số đợt đạn/nhịp nổ sinh ra trong toàn bộ thời gian tồn tại của chiêu.
* **`MSGenerateParam`:** Nhịp thời gian giữa 2 lần sinh đạn liên tiếp tính bằng **Frames** ($t = \text{MSGenerateParam} / 15.0\text{s}$).

$$\text{Tổng thời gian duy trì bãi sát thương} = \frac{\text{ChildCount} \times \text{MSGenerateParam}}{15.0f} \text{ (giây)}$$

#### 🅱️ Cơ Chế 2: Viên đạn/vùng nổ tự lặp lại tác dụng (`DmgInterval` trong `Missile.csv`)
* **`DmgInterval`:** Giãn cách giữa 2 lần gây sát thương/hồi máu của cùng một viên đạn/vùng nổ (Frames).
* **`CanRepeatDmg = 1`:** Bắt buộc phải bật để cho phép viên đạn tác động nhiều lần lên cùng một mục tiêu.
* **`LifeTime`:** Thời gian tồn tại tối đa của viên đạn/vùng nổ (Frames).

$$\text{Số nhịp tác động thực tế của 1 viên đạn} = \left\lfloor \frac{\text{LifeTime}}{\text{DmgInterval}} \right\rfloor$$
*(Ví dụ: Vùng hoa sen hồi máu 313: `LifeTime = 90`, `DmgInterval = 15`, `CanRepeatDmg = 1` $\rightarrow 90/15 = 6$ nhịp hồi máu, mỗi giây hồi 1 lần trong 6 giây).*

#### 🆎 Cơ Chế 3: Sub-skill kích hoạt theo nhịp đạn bay (`FlySkillId` & `FlyEventInterval`)
* Khi viên đạn chính đang bay hoặc duy trì bãi đất, cứ mỗi `FlyEventInterval` frames ($t = \text{FlyEventInterval} / 15.0\text{s}$), hệ thống tự động gọi Sub-skill `FlySkillId` (vd: Từ Hàng Phổ Độ 306 gọi 307 mỗi 15 frames; Vạn Kiếm Quyết 631 gọi 632 mỗi 5 frames).

---

---

## 4. CHI TIẾT BẢNG `Missile.csv`

| Tên Cột | Kiểu | Ý nghĩa & Công thức vận hành trong Unity |
| :--- | :---: | :--- |
| **`MissileId`** | `int` | ID duy nhất của viên đạn/hitbox (Khóa chính). |
| **`MoveKind`** | `int` | **Cơ chế chuyển động:**<br>• `0`: Cố định tại chỗ (Static AOE / Bẫy / Trận pháp).<br>• `1`: Bay thẳng theo hướng bắn (Linear Directional).<br>• `2`: **Đạn tự bám đuổi / uốn lượn theo mục tiêu (Homing / Tracking)**.<br>• `3`: Di chuyển gắn liền theo thân người Caster (Dash Hitbox).<br>• `5`: Đạn bay uốn cong / quay trở lại (Boomerang / Curved).<br>• `6`: Đạn bay xoay vòng quanh thân Caster. |
| **`MissileParam1..3`**| `int/string`| **Tham số chuyển động đặc biệt:**<br>• Khi `MoveKind = 5` (Boomerang): `Param1` = Độ cong quỹ đạo Bezier.<br>• Khi `MoveKind = 6` (Đạn xoay vòng): `Param1 = 1` (Khi quay về sẽ tự động bám theo người chơi), `Param2` = Bán kính quỹ đạo xoay. |
| **`Speed`** | `int` | Vận tốc bay (`Velocity = Speed / 10.0f` m/s). |
| **`AcceSpeed`** | `int` | Gia tốc tăng tốc khi bay (`Acceleration = AcceSpeed / 10.0f` $m/s^2$). |
| **`DmgRangeType`** | `int` | `0`: Đơn mục tiêu (Single Target), `1`: Vùng tròn / Khối cầu (AOE Sphere). |
| **`DmgRange`** | `int` | Bán kính Hitbox (`Radius = DmgRange / 10.0f` mét). |
| **`LifeTime`** | `int` | Thời gian sống tối đa của đạn (`LifeTime / 15.0f` giây). |
| **`DmgInterval`** | `int` | **Nhịp giãn cách giữa các lần gây sát thương/hồi máu lặp lại** (`Interval = DmgInterval / 15.0f` giây). |
| **`DelayDeleteFrame`**| `int` | Thời gian trễ trước khi hủy GameObject đạn (Frames, để VFX fade out). |
| **`IsDmgVanish`** | `int (0/1)` | `1` = Đạn chạm trúng mục tiêu là hủy ngay lập tức (Single Hit Projectile). |
| **`CanRepeatDmg`** | `int (0/1)` | `1` = Được phép gây sát thương/hồi máu nhiều lần (kết hợp với `DmgInterval`). |
| **`MissileResID`** | `int` | ID Prefab 3D của đạn khi bay (Trỏ sang `EffectRes.csv`). |
| **`CollResID`** | `int` | ID Prefab 3D nổ khi va chạm (Hit Impact VFX, trỏ `EffectRes.csv`). |
| **`PosOffsetLenght`**| `int` | Độ lệch spawn về phía trước Caster (`Offset = PosOffsetLenght / 100.0f` m). |
| **`CollBrigth`** | `int` | Cường độ chớp sáng màn hình (`Alpha = CollBrigth / 1000.0f`). |
| **`CollBrigthFrame`**| `int` | Số frame duy trì chớp sáng màn hình. |
| **`IsIgnoreBarrier`**| `int (0/1)`| `1` = Bỏ qua chướng ngại vật/vật cản địa hình. |

---

---

## 5. CHI TIẾT BẢNG `ActionEvent.csv` & `ActionEventDes.csv`

Bảng quy định Timeline chính xác của hoạt ảnh nhân vật.

> **📌 Quy ước Frame & EventType:**
> * `EventType`: `1` = Sự kiện khởi tạo ban đầu, `2` = Sự kiện theo Frame ($t = \text{Frame} / 15.0\text{s}$), `3` = Sự kiện khi kết thúc hoạt ảnh.
> * Cột `Frame` tính theo chuẩn **15 FPS**. Riêng `InstantDir` tính theo chuẩn **45 FPS** ($1/45\text{s}$).

| Tên Sự Kiện (`EventName`) | `EventParam1` | `EventParam2` | `EventParam3` | `EventParam4` | `EventParam5` | Ý nghĩa thực tế & Bằng chứng mã nguồn (`ActionEventDes.tab`) |
| :--- | :---: | :---: | :---: | :---: | :---: | :--- |
| **`CrossFade`** | Tỉ lệ vượt mức ($10000/1000=1$) | Vượt mức / ($Frame \times Frame$) | Vượt mức tối đa | — | — | Hòa trộn mượt chuyển tiếp hoạt ảnh. |
| **`InstantDir`** | Góc quay / Frame | — | — | — | — | Tốc độ xoay mặt về mục tiêu (Độ/Frame, **chuẩn 45 FPS**). |
| **`PlayEffect`** | `ResID` (VFX) | Tuần hoàn (`0/1`) | Không scale theo tốc đánh (`0/1`) | **`SlotId`** (Khớp xương) | `Duration` (-1 là vô hạn) | Sinh Prefab VFX `ResID` gắn vào khớp `SlotId`. |
| **`PlayEffectNoClear`**| `ResID` | Tuần hoàn (`0/1`) | Không scale tốc đánh | `SlotId` | `Duration` | Phát VFX không bị xóa bởi lệnh `ClearEffect` sau đó. |
| **`UnbindEffect`** | `ResID` | — | — | — | — | Tháo gỡ Prefab VFX không cho bám theo NPC nữa. |
| **`ClearEffect`** | `ResID` | Xóa Npc liên quan | — | — | — | Xóa sạch hiệu ứng chỉ định trên nhân vật. |
| **`PlaySound`** | `SoundID` | — | — | — | — | Phát âm thanh từ Wwise (`Sound.csv`). |
| **`StopSound`** | `SoundID` | Delay frame lưu giữ | — | — | — | Dừng âm thanh khi bị ngắt chiêu. |
| **`CastSkill`** | `SkillId` | `SkillLevel` | Đồng bộ Act | — | — | **Thời điểm chính xác sinh đạn / nổ sát thương** của chiêu. |
| **`CanDoSkill`** | Cờ so sánh Priority | — | — | — | — | `0` hoặc rỗng: Priority mới > Priority hiện tại thì được cast; `1`: Priority mới $\ge$ Priority hiện tại. |
| **`CanDoRun`** | — | — | — | — | — | **Cho phép ngắt động tác thừa sớm** nếu bấm di chuyển (Animation Cancel). |
| **`LinkSkillInit`** | Frame hiệu lực cuối | `SkillId_1` | `SkillId_2` | `SkillId_3` | — | Khởi tạo thời gian chờ combo liên hoàn theo độ ưu tiên chiêu. |
| **`CastLinkSkill`**| Di chuyển ngắt chiêu (`0/1`)| — | — | — | — | Chuyển tiếp sang hoạt ảnh chiêu kế tiếp trong combo. |
| **`MovePos`** | Quãng đường (cm) | Tốc độ (cm/frame) | Khoảng cách dừng (cm) | Gia tốc | — | Ép nhân vật lướt tới trước. |
| **`PlayShake`** | Dao động (1=1cm) | Tốc độ rung | Số lần rung | Trước sau? | Bắt buộc? | Rung màn hình Camera Shake. |
| **`NpcChangeSize`**| Kích thước (%) | Tốc độ (%) | — | — | — | Phóng to/thu nhỏ Model nhân vật. |
| **`ModelEffectVisible`**| `0/1` | — | — | — | — | Ẩn/Hiện Mesh Renderer nhân vật. |

---

---

## 7. HỆ THỐNG ĐỊNH HƯỚNG, CHỈ ĐỊNH MỤC TIÊU & CẤU HÌNH SELECTOR

Hệ thống Target & Aiming Reticle được điều khiển phối hợp qua 3 bảng:

### 🎯 1. Bảng `AttackSkill.csv` (Cơ chế phân loại tấn công & Tự đánh):
* **`AttackType = 1` (`Normal`)**: Chiêu thường / AOE quanh thân (Không ép hướng).
* **`AttackType = 2` (`Direction`)**: Chiêu **định hướng tự do** (Linear Skillshot).
* **`AttackType = 3` (`Target`)**: Chiêu **bắt buộc khóa mục tiêu** (Target-Locked).
* **`AttackType = 4` (`Line`)**: Chiêu đâm đường thẳng xuyên thấu.
* **`AutoFightTarget`**: `1` = AI tự đánh bắt buộc phải tìm thấy mục tiêu mới xuất chiêu.

### 📐 2. Bảng `PreciseCastSkill.csv` (Kích thước hiển thị Selector khi vuốt Joystick):
* **`CastType`**: `direction` (Mũi tên chỉ hướng) hoặc `target` (Vòng tròn khóa chân / Điểm rơi).
* **`CastRadius`**: Chiều dài mũi tên / Bán kính tầm với tối đa (`CastRadius / 100.0f` mét).
* **`DamageRadius`**: Bề rộng vùng sát thương / Bán kính vòng tròn nổ (`DamageRadius / 10.0f` mét).

### 🔍 3. Bảng `SkillSelector.csv` (Bộ lọc ưu tiên mục tiêu thông minh):
* **`hurt_maxhp`**: Tự động ưu tiên đồng minh/bản thân bị mất nhiều % máu nhất (Dùng cho Hồi máu Từ Hàng Phổ Độ 306, Bàn Băng Phi Sương 6414).
* **`flag_npc`**: Ưu tiên nhắm vào cờ hoặc NPC mục tiêu nhiệm vụ.

---

---

## 10. CHI TIẾT BẢNG `AutoSkill.csv`

Bảng quy định cơ chế tự động kích hoạt chiêu thức / kỹ năng bị động phản đòn:

| Tên Cột | Kiểu | Ý nghĩa logic |
| :--- | :---: | :--- |
| **`AutoId`** | `int` | ID quy tắc tự động (Khóa chính). |
| **`AutoName`** | `string` | Tên quy tắc / Kỹ năng kích hoạt. |
| **`AutoType`** | `int` | **Loại điều kiện kích hoạt:**<br>• `1`: Bị tấn công phản kích (`Đồng Hành-Phản Kích`).<br>• `2`: Đánh thường/Kỹ năng trúng đích kích hoạt thêm hiệu ứng.<br>• `3`: Sinh lực xuống thấp dưới ngưỡng kích hoạt hộ mạng (Tọa Vọng Vô Ngã, Hộ thuẫn).<br>• `4`: Sinh lực thấp kích hoạt bất tử / hồi sinh lực (Phá Phủ Trầm Châu).<br>• `5`: Kích hoạt khi tử vong (Quyết Biệt).<br>• `6`: Kích hoạt toàn màn hình.<br>• `8`: Kích hoạt theo điều kiện đặc biệt nội tại.<br>• `13`: Bị tấn công phản kích gây khống chế (La Hán Kim Thân - Định Thân, Cửu Âm - Đẩy Lùi).<br>• `14`: Tự động hồi phục định kỳ (Khô Mộc Phùng Xuân, Niết Bàn Trùng Sinh).<br>• `15`: Phá tàng hình kích hoạt Buff (Huyết Nguyệt Ảnh).<br>• `16`: Hào quang Hộ Chủ Đồng Hành.<br>• `17`: Kích hoạt theo trạng thái di chuyển đặc biệt.<br>• `18`: Kích hoạt theo điều kiện combat nâng cao.<br>• `20`: Kích hoạt theo sự kiện môi trường / kịch bản.<br>• `24`: Chuyển đổi trạng thái/vũ khí nhận Buff (Dương Môn).<br>• `25`: Né tránh đòn thành công kích hoạt Buff (Cái Bang Du Long Quyết).<br>• `26`: Hộ thuẫn bị vỡ kích hoạt nổ sát thương/hồi máu. |
| **`CastPercent`** | `int` hoặc `string` | Tỉ lệ phần trăm kích hoạt thành công. Có 2 dạng:<br>• Số đơn (`int`): vd `40`, `100` — áp dụng cố định.<br>• Đa cấp (`string`): vd `{1,100},{10,100}` — tỉ lệ thay đổi theo cấp độ kỹ năng (`{SkillLevel, Percent}`). |
| **`CastSkillId`** | `int` | ID Sub-skill được tự động gọi ra (Trỏ sang `Skill.csv`). |
| **`MinPerCastTime`**| `string` | Thời gian hồi nội bộ giữa 2 lần kích hoạt. Có 2 dạng:<br>• Phép nhân (`string`): vd `15*15` = 225 frames = 15 giây, `15*10` = 150 frames = 10 giây.<br>• Số đơn (`int`): vd `30` = 30 frames = 2 giây.<br>• Rỗng: Không giới hạn thời gian hồi. |

---

---

## 11. CHI TIẾT BẢNG `SkillLevelUp.csv` & `SkillSlot.csv`

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
  - `BtnName1`: Nút phím được phép gán kỹ năng này vào.

---

---

## 8. CHI TIẾT BẢNG FactionSkill.csv (MÔN PHÁI & PHÂN BỔ KỸ NĂNG)

* **`FactionSkill.csv`**: Định vị vị trí nút bấm trên HUD UI (`Attack`, `Skill1`, `Skill2`, `Skill3`, `Skill4`, `Skill_Dodge`), cờ `IsAnger = 1` cho Tuyệt kỹ Nộ (Ulti).

### 🏛️ Bảng Mã Môn Phái Chuẩn & Môn Phái Thức Tỉnh (`Faction` ID):
Game phân chia hệ thống môn phái thành 2 ID riêng biệt: **Môn Phái Tiêu Chuẩn** ($1 \sim 19$) và **Môn Phái Thức Tỉnh / Phân Nhánh** ($26 \sim 42$):

| Tên Môn Phái | Hệ Ngũ Hành | Faction ID (Chuẩn) | Faction ID (Thức Tỉnh) | Bộ Skill Chủ Đạo Thức Tỉnh |
| :--- | :---: | :---: | :---: | :--- |
| **Thiên Vương** | Kim | `1` | `38` | `8401` (Công), `8410` (Skill1), `8406` (Skill2), `8413` (Skill3), `8408` (Skill4) |
| **Nga Mi (EM)** | Thủy | `2` | `28` | `6901` (Công), `6908` (Thiên Vũ Bảo Luân), `6906` (Giang Hải), `6910` (Bạch Lộ), `6916` (Cửu Âm Bạch Cốt Trảo) |
| **Đào Hoa** | Hỏa | `3` | `39` | `8601` (Công), `8610` (Skill1), `8607` (Skill2), `8606` (Skill3), `8612` (Skill4) |
| **Tiêu Dao** | Mộc | `4` | `29` | `7001` (Công), `7008` (Skill1), `7006` (Skill2), `7013` (Skill3), `7015` (Skill4) |
| **Võ Đang** | Thổ | `5` | `26` | `6701` (Công), `6710` (Skill1), `6706` (Skill2), `6712` (Skill3), `6713` (Skill4) |
| **Thiên Nhẫn** | Hỏa | `6` | `31` | `7301` (Công), `7305` (Skill1), `7308` (Skill2), `7311` (Skill3), `7322` (Skill4) |
| **Thúy Yên** | Thủy | `8` | `41` | `9001` (Công), `9010` (Skill1), `9006` (Skill2), `9015` (Skill3), `9028` (Skill4) |
| **Đường Môn** | Mộc | `9` | `36` | `8201` (Công), `8208` (Skill1), `8206` (Skill2), `8210` (Skill3), `8212` (Skill4) |
| **Côn Lôn** | Thổ | `10` | `35` | `7901` (Công), `7910` (Skill1), `7906` (Skill2), `7908` (Skill3), `7912` (Skill4) |
| **Trường Ca** | Thổ | `12` | `42` | `9201` (Công), `9207` (Skill1), `9205` (Skill2), `9211` (Skill3), `9225` (Skill4) |

* **`Sound.csv`**: Tra cứu gói SoundBank Wwise và Event âm thanh xuất chiêu / va chạm.

---

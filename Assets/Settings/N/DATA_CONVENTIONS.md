# 📘 TÀI LIỆU QUY ƯỚC & NGUYÊN LÝ DỮ LIỆU GAME (DATA CONVENTIONS & SYSTEM ARCHITECTURE)

Tài liệu này tổng hợp toàn bộ quy ước, định nghĩa biến, công thức chuyển đổi sang Unity, bảng mã Enum và nguyên lý vận hành của hệ thống Combat / NPC / VFX / Audio trích xuất từ dữ liệu game Kiếm Hiệp (Seasun / Kingsoft).

---

## 📑 MỤC LỤC
1. [Hệ Thống Đơn Vị Đo Lường & Công Thức Quy Đổi Unity](#1-hệ-thống-đơn-vị-đo-lường--công-thức-quy-đổi-unity)
2. [Chi Tiết Bảng `Skill.csv` (Kỹ Năng & Logic Xuất Chiêu)](#2-chi-tiết-bảng-skillcsv)
3. [Chi Tiết Bảng `Missile.csv` (Đạn Đạo, Hitbox & Va Chạm)](#3-chi-tiết-bảng-missilecsv)
4. [Chi Tiết Bảng `ActionEvent.csv` (Dòng Thời Gian Từng Frame Sự Kiện)](#4-chi-tiết-bảng-actioneventcsv)
5. [Chi Tiết Bảng `NpcRes.csv` (Kích Thước 3D, Collider & Frame Hoạt Ảnh)](#5-chi-tiết-bảng-npcrescsv)
6. [Chi Tiết Bảng `ActionName.csv` (Từ Điển Tên Hoạt Ảnh Chuẩn)](#6-chi-tiết-bảng-actionnamecsv)
7. [Chi Tiết Bảng `NpcTemplate.csv` & `Field_HeaderBoss.csv` (Dữ Liệu Quái & Boss)](#7-chi-tiết-bảng-npctemplatecsv--field_headerbosscsv)
8. [Chi Tiết Bảng `Character.csv` (Nhân Vật Người Chơi)](#8-chi-tiết-bảng-charactercsv)
9. [Chi Tiết Bảng `NpcAttribute.csv` (Chỉ Số Chiến Đấu & Thuộc Tính Ngũ Hành)](#9-chi-tiết-bảng-npcattributecsv)
10. [Chi Tiết Bảng `EffectRes.csv` (Tài Nguyên Prefab VFX)](#10-chi-tiết-bảng-effectrescsv)
11. [Chi Tiết Bảng `StateEffect.csv` & `PartSlot.csv` (Buff/Debuff & Khớp Gắn Xương)](#11-chi-tiết-bảng-stateeffectcsv--partslotcsv)
12. [Chi Tiết Bảng `FactionSkill.csv` & `AutoAiSkill.csv` (Cây Chiêu Thức & AI Tự Đánh)](#12-chi-tiết-bảng-factionskillcsv--autoaiskillcsv)
13. [Chi Tiết Bảng `Sound.csv` (Âm Thanh SFX / Wwise Bank)](#13-chi-tiết-bảng-soundcsv)
14. [Tổng Hợp Toàn Bộ Bảng Mã Enum Chuẩn C# Cho Unity](#14-tổng-hợp-toàn-bộ-bảng-mã-enum-chuẩn-c-cho-unity)

---

## 1. HỆ THỐNG ĐƠN VỊ ĐO LƯỜNG & CÔNG THỨC QUY ĐỔI UNITY

| Đại lượng trong CSV | Đơn vị gốc | Tỉ lệ quy đổi | Đơn vị Unity C# | Công thức tính trong Unity |
| :--- | :--- | :---: | :--- | :--- |
| **Thời gian Frame (ActionEvent / LifeTime / CanDoSkill...)** | 1 frame (chuẩn **15 FPS**) | $\div 15$ | Giây (Seconds) | `float timeSec = frame / 15.0f;` |
| **Thời gian (TimePerCast / Cooldown)** | 1 frame (chuẩn **15 FPS**) | $\div 15$ | Giây (Seconds) | `float cdSec = timePerCast / 15.0f;` |
| **Khoảng cách / Bán kính** | Centimet (cm) | $\div 100$ | Mét (Meters) | `float rangeMeter = attackRadius / 100.0f;` |
| **Vận tốc bay (`Speed`)** | Game Speed Unit | $\div 10$ | Mét/giây (m/s) | `float velocity = speed / 10.0f;` |
| **Gia tốc (`AcceSpeed`)** | Game Acce Unit | $\div 10$ | $m/s^2$ | `float accel = acceSpeed / 10.0f;` |
| **Góc quay (`InstantDir`)** | Độ/giây (°/s) | $\times 1$ | Độ/giây (°/s) | `transform.rotation = Quaternion.RotateTowards(...);` |
| **Tỉ lệ phần trăm (`%`)** | $0 \sim 100$ | $\div 100$ | $0.0f \sim 1.0f$ | `float percent = val / 100.0f;` |

> **⚠️ Lưu ý quan trọng (đã kiểm chứng bằng vật lý):** Toàn bộ cột `Frame` trong `ActionEvent.csv`, `LifeTime` trong `Missile.csv`, `TimePerCast` trong `Skill.csv` đều dùng **15 FPS** — không phải 30 FPS. Bằng chứng: Missile 301 có `LifeTime=10 frames`, `Speed=120` (12 m/s), `AttackRadius=550` (5.5m). Tại 15 FPS: 10÷15 × 12 = **8m** (đủ tầm 5.5m ✅). Tại 30 FPS: 10÷30 × 12 = **4m** (không đủ ❌).

---

## 2. CHI TIẾT BẢNG `Skill.csv`

Bảng định nghĩa thuộc tính cốt lõi của mọi chiêu thức (Người chơi, Quái vật, Boss, Đệ tử).

| Tên Cột | Kiểu dữ liệu | Ý nghĩa & Nguyên lý vận hành |
| :--- | :---: | :--- |
| **`SkillId`** | `int` | ID duy nhất của kỹ năng (Khóa chính). |
| **`SkillName`** | `string` | Tên hiển thị của kỹ năng (Tiếng Việt). |
| **`Property`** | `string` | Phân loại ngữ cảnh (Kỹ năng môn phái, Kỹ năng quái, Buff...). |
| **`SkillType`** | `int` | Loại kỹ năng: `1` = Bị động (Passive), `2` = Tấn công thường, `3` = Buff hỗ trợ, `4` = Trận pháp (Aura), `5` = Kỹ năng chủ động điều khiển. |
| **`MeleeForm`** | `int` | Kiểu đánh cận chiến: `0`/rỗng = Bình thường, `1` = Áp sát nhanh, `2` = Xoay vòng quanh người. |
| **`StartPosType`** | `int` | **Gốc tọa độ tham chiếu của Kỹ năng:**<br>• `1` / Trống (**Caster-Based**): Lấy **Người ra chiêu** làm gốc. Chiêu thức tự do vung chém / phóng ra theo hướng mặt mà không cần mục tiêu.<br>• `2` (**Target-Based / Target-Lock**): Lấy **Mục tiêu đang khóa** làm gốc tham chiếu đích đến. Bắt buộc phải có Target trong tầm `AttackRadius` để thi triển.<br>&nbsp;&nbsp;↳ *Nếu `Missile.MoveKind = 0` (Bẫy / Sét)*: Nổ ngay tại vị trí chân Target.<br>&nbsp;&nbsp;↳ *Nếu `Missile.MoveKind = 2` (Đạn bay / Cầu nước)*: Sinh ra phía trước mặt Caster (`PosOffsetLenght`) và bay lao tới Target.<br>• `3` (**Ground Point**): Lấy điểm chạm đất / con trỏ chỉ định làm gốc. |
| **`StartDirType`** | `int` | **Hướng ngắm ban đầu:**<br>• `0` / Trống: Hướng mặt nhân vật (theo 64 hướng logic).<br>• `1`: Hướng vector thẳng tới tâm Target.<br>• `2`: Hướng kéo Joystick của người chơi. |
| **`SelectorType`** | `int` | **Loại vòng ngắm / Indicator giao diện:**<br>• `1`: Vòng tròn chọn vùng đất (AOE Circle Indicator).<br>• `2`: Mũi tên định hướng (Directional Arrow / Sector Indicator). |
| **`SelectorRange`**| `int` | Bán kính tối đa cho phép kéo vòng chọn/ngắm trên màn hình (`SelectorRange / 100` mét). |
| **`TargetSelf`** | `int (0/1)` | `1` = Chiêu tự động / Khóa mục tiêu là chính bản thân (Self Buff, Khiên, Hào quang). |
| **`Icon`** | `string` | Tên Sprite Icon kỹ năng trong Sprite Atlas. |
| **`IconAtlas`** | `string` | Đường dẫn prefab UI chứa Sprite Atlas (vd: `UI/Atlas/SkillIcon/EM_Skill.prefab`). |
| **`WaitTime`** | `int` | Thời gian chờ tích lực trước khi ra chiêu (Frames). |
| **`ChildID`** | `int` | ID Missile hoặc Sub-skill sinh ra khi thi triển (Trỏ sang `Missile.csv` hoặc `Skill.csv`). |
| **`ChildCount`** | `int` | Số lượng đạn/tia sinh ra trong 1 lần xuất chiêu. |
| **`MissileForm`** | `int` | **Dạng đạn đạo / Hình thái Missile:**<br>• `1`: Đạn bay thẳng bình thường (Linear Straight).<br>• `2`: Đạn bay hình quạt / chùm nhiều tia (Spread Shot).<br>• `3`: Vòng tròn tỏa ra xung quanh Caster (Circular Ring).<br>• `4`: **Đạn nảy bật liên hoàn (Chain / Bouncing)** giữa các mục tiêu.<br>• `5`: Rơi từ trên trời xuống (Sky Drop / Meteor). |
| **`MSGenerate`** | `int` | Kiểu sinh đạn đa tia: `1` = Đồng loạt, `2` = Bắn tuần tự cách quãng, `3` = Bắn xoay tròn. |
| **`MSGenerateParam`**| `string` | Tham số đi kèm `MSGenerate` (vd: góc lệch giữa các tia đạn). |
| **`Relation`** | `string` | Đối tượng tác dụng: `enemy` (Kẻ địch), `ally` (Đồng minh), `self` (Bản thân). |
| **`TimePerCast`** | `int` | Thời gian hồi chiêu (Cooldown) tính theo frame chuẩn 15 FPS (`Cooldown = TimePerCast / 15.0s`). |
| **`IsUseAR`** | `int (0/1)` | `1` = Tự động áp dụng tầm đánh `AttackRadius` khi tìm mục tiêu. |
| **`Series`** | `int` | Ngũ hành kỹ năng: `0`=Vô, `1`=Kim, `2`=Mộc, `3`=Thủy, `4`=Hỏa, `5`=Thổ. |
| **`CastActionId`** | `int` | ID hoạt ảnh ra đòn (Trỏ sang `ActionName.csv`, vd: `16` = `at01`, `22` = `jn02`). |
| **`ActionEventID`** | `int` | ID dòng thời gian sự kiện hoạt ảnh (Trỏ sang `ActionEvent.csv`). |
| **`ActionFrontPer`** | `int` | Tỉ lệ phần trăm thời gian vung đòn trước khi sát thương nổ (Front swing %). |
| **`ActionBackPer`** | `int` | Tỉ lệ phần trăm thu hồi đòn sau khi sát thương nổ (Back swing %). |
| **`StateEffectId`** | `int` | ID hiệu ứng trạng thái buff/debuff gắn kèm (Trỏ sang `StateEffect.csv`). |
| **`SkillStyle`** | `string` | Phong cách chiêu: `normal_melee`, `normal_remote`, `skill_melee`, `skill_remote`. |
| **`IsMelee`** | `int (0/1)` | `1` = Chiêu đánh áp sát cận chiến, `0` = Đánh tầm xa. |
| **`StatePriority`** | `int` | Độ ưu tiên của trạng thái (Số lớn đè số nhỏ). |
| **`StateType`** | `int` | Loại hiệu ứng: Choáng, Chậm, Đóng băng, Hút máu, Hộ thuẫn... |
| **`StateTime`** | `int` | Thời gian duy trì hiệu ứng trạng thái (Frames). |
| **`AttackRadius`** | `int` | Tầm thi triển tối đa tính bằng Centimet (`AttackRadius / 100 = Mét`). |
| **`ClassName`** | `string` | Tên hàm xử lý công thức sát thương trong file Lua môn phái (vd: `em_pg1`, `em_blns`). |
| **`MaxUsePoint`** | `int` | Số điểm nạp trữ chiêu tối đa (Charges/Stock, vd: tích tối đa 3 lần lướt). |
| **`UsePointRecover`**| `int` | Thời gian hồi 1 điểm nạp trữ (Frames). |
| **`Param1..Param6`** | `string` | Các tham số logic mở rộng tùy theo `MissileForm`:<br>• Khi `MissileForm = 4` (Nảy bật):<br>&nbsp;&nbsp;- `Param1`: Số lần nảy tối đa.<br>&nbsp;&nbsp;- `Param2`: `1` = Bật tự động tìm mục tiêu kế tiếp.<br>&nbsp;&nbsp;- `Param3`: Phạm vi quét tìm mục tiêu nảy (cm).<br>&nbsp;&nbsp;- `Param4`: Số lần lặp lại trên cùng 1 người (`0` = không giới hạn). |
| **`Param1Des..`** | `string` | Chú thích tiếng Việt cho từng tham số `Param1..Param6`. |
| **`CostType`** | `int` | Loại tài nguyên tiêu hao: `0` = Không tốn, `1` = Mana/Nội lực, `2` = Nộ khí (Anger). |
| **`CostValue`** | `int` | Lượng tài nguyên tiêu hao mỗi lần xuất chiêu. |
| **`FactionLimit`** | `int` | Môn phái sở hữu: `1`=Thiên Vương, `2`=Nga Mi, `3`=Đào Hoa, `4`=Tiêu Dao, `5`=Võ Đang... |
| **`ReqLevel`** | `int` | Cấp độ nhân vật yêu cầu tối thiểu để học chiêu. |
| **`IsLinkSubSkill`**| `int (0/1)` | `1` = Chiêu thức tự động kích hoạt kỹ năng phụ đi kèm. |
| **`IsAura`** | `int (0/1)` | `1` = Trận pháp hào quang tỏa ra liên tục quanh bản thân. |
| **`Desc`** | `string` | Mô tả chi tiết kỹ năng (Hiển thị Tooltip trong game). |
| **`AttackAnger`** | `int` | Điểm nộ khí tích lũy được khi đánh trúng địch. |
| **`CastSoundID`** | `int` | ID âm thanh phát ra khi bắt đầu niệm chiêu (Trỏ sang `Sound.csv`). |
| **`CanDoSkillPri`** | `int` | Cấp độ ưu tiên ngắt chiêu (Interrupt Priority). |
| **`UseLimitType`** | `int` | Giới hạn dùng: `0`=Tự do, `1`=Chỉ khi đứng yên, `2`=Chỉ trong chiến đấu. |
| **`IsPeaceCanUse`** | `int (0/1)` | `1` = Cho phép dùng trong khu vực an toàn (Thành chính / Tân thủ thôn). |

---

### 🎯 Hệ Thống Vòng Ngắm & Chỉ Thị Mục Tiêu (Skill Selector & Indicators):

Toàn bộ hệ thống kỹ năng trong game sử dụng **bộ Selector chung chuẩn hóa**, được tự động phân loại theo cấu hình trong `Skill.csv`:

| Nhóm Cơ Chế Ngắm | Dấu hiệu nhận diện trong `Skill.csv` | Tên Prefab VFX Indicator | ResID (`EffectRes.csv`) | Cơ chế hoạt động (`EffectMoveType`) |
| :--- | :--- | :--- | :---: | :--- |
| **1. Mũi tên định hướng** (Directional / Dash) | `SelectorType = 2` | `xingdongfangxianjiantou_G2.prefab` | **`7`** | **`Rotate (0)`**: Mũi tên cắm dưới chân Caster, xoay $360^\circ$ theo góc cần Joystick. |
| **2. Vùng chọn thông minh** (Smartcast AOE) | `SelectorType = 1` | `xuanzhong.prefab` | **`9`** | **`Move (1)`**: Vòng tròn kéo tự do trên mặt đất, giới hạn bởi `SelectorRange`. |
| **3. Vòng chọn hỗ trợ** (Ally / Healing AOE) | `SelectorType = 1` & `Relation = recover/ally` | `xuanzhong_LV.prefab` | **`11`** | **`Move (1)`**: Vòng tròn xanh lá kéo chọn vùng hồi máu / buff đồng đội. |
| **4. Khóa mục tiêu địch** (Target Lock Enemy) | `StartPosType = 2` & `Relation = enemy` | `xuanzhong.prefab` / `xuanzhong_tubiao.prefab` | **`9` / `10`** | **Khóa chân Target**: Vòng đỏ và icon mũi tên trên đầu mục tiêu đang chọn. |
| **5. Chiêu tự thân / Buff** (Self Cast / Aura) | `TargetSelf = 1` hoặc `Relation = self` | *(Không hiển thị)* | — | Không hiện selector, nhấn nút là nổ ngay tại vị trí Caster. |

> **Công thức tính kích thước & tầm ngắm:**
> - **Kích thước vòng tròn:** Lấy theo `AttackRadius / 100.0f` hoặc `DmgRange / 10.0f` (m).
> - **Phạm vi kéo tối đa:** Lấy theo `SelectorRange / 100.0f` (m).

---

## 3. CHI TIẾT BẢNG `Missile.csv`

Bảng quy định toàn bộ cơ chế vật lý của Đạn Đạo, Hitbox, Tốc độ, Vùng sát thương và Hiệu ứng va chạm.

| Tên Cột | Kiểu | Ý nghĩa & Công thức vận hành trong Unity |
| :--- | :---: | :--- |
| **`MissileId`** | `int` | ID duy nhất của viên đạn/hitbox (Khóa chính). |
| **`MissileName`** | `string` | Tên định danh của viên đạn. |
| **`MoveKind`** | `int` | **Cơ chế chuyển động:**<br>• `0`: Cố định tại chỗ (AOE bẫy / Vòng sáng dưới đất).<br>• `1`: Bay thẳng theo hướng lúc bắn (Linear Directional).<br>• `2`: **Đạn tự bám đuổi / uốn lượn theo mục tiêu (Homing / Tracking)**.<br>• `3`: Lướt theo thân người Caster (Dash Hitbox). |
| **`Speed`** | `int` | Vận tốc bay (`Velocity = Speed / 10.0f` m/s). |
| **`AcceSpeed`** | `int` | Gia tốc tăng tốc khi bay (`Acceleration = AcceSpeed / 10.0f` $m/s^2$). |
| **`DmgRangeType`** | `int` | **Hình dạng Hitbox sát thương:**<br>• `0`: Đơn mục tiêu (Single Target Raycast).<br>• `1`: Hình tròn / Khối cầu (`DmgRange` = Bán kính mét).<br>• `2`: Hình cánh quạt (`DmgRange` = Tầm xa, `DmgRangeY` = Góc mở độ).<br>• `3`: Hình hộp chữ nhật đâm tới (`DmgRange` = Chiều dài, `DmgRangeY` = Bề rộng).<br>• `4`: Vòng xuyến / Trụ rỗng (Donut/Ring AOE). |
| **`DmgRange`** | `int` | Kích thước bán kính hoặc chiều dài Hitbox (`DmgRange / 10.0f` m). |
| **`DmgRangeY`** | `int` | Góc mở hình quạt (Độ) hoặc bề rộng hình hộp chữ nhật (m). |
| **`DmgInterval`** | `int` | Khoảng cách nhịp giữa 2 lần gây dame liên tục (Frames). |
| **`LifeTime`** | `int` | Thời gian sống tối đa của đạn (`LifeTime / 15.0f` giây). |
| **`IsDmgVanish`** | `int (0/1)` | `1` = Đạn chạm trúng 1 mục tiêu là nổ và biến mất ngay lập tức. |
| **`CanRepeatDmg`** | `int (0/1)` | `1` = Đạn được phép gây sát thương nhiều lần (Dùng cho đạn xuyên thấu / đạn nảy). |
| **`MissileResID`** | `int` | ID Prefab 3D của viên đạn khi đang bay (Trỏ sang `EffectRes.csv`). |
| **`CollResID`** | `int` | ID Prefab 3D nổ khi va chạm trúng đích (Hit Impact VFX, trỏ `EffectRes.csv`). |
| **`VanishResID`** | `int` | ID Prefab 3D khi đạn hết thời gian bay mà không trúng ai (Fade Out VFX). |
| **`PosOffsetLenght`**| `int` | Khoảng cách spawn lệch về phía trước mặt Caster (`Offset = PosOffsetLenght / 100.0f` m). |
| **`ColFollowTarget`**| `int (0/1)` | `1` = Điểm va chạm dính chặt theo xương mục tiêu. |
| **`IsIgnoreBarrier`**| `int (0/1)` | `1` = Đạn bay xuyên qua chướng ngại vật địa hình thấp. |
| **`CollSoundID`** | `int` | ID âm thanh khi bắn trúng đích (Trỏ sang `Sound.csv`). |
| **`FlySoundID`** | `int` | ID âm thanh phát ra liên tục khi đạn đang bay (Looping Audio). |
| **`CollBrigth`** | `int` | Độ chớp sáng màn hình khi trúng đòn (Screen Flash Intensity). |
| **`CollBrigthFrame`**| `int` | Số frame duy trì chớp sáng (Flash Duration). |
| **`IsHitFloat`** | `int (0/1)` | `1` = Trúng đạn làm mục tiêu bị hất nổi bồng bềnh trên không. |

---

## 4. CHI TIẾT BẢNG `ActionEvent.csv`

Bảng quy định **Timeline chính xác từng frame** của hoạt ảnh nhân vật khi tung đòn, hòa trộn animation, phát âm thanh, tạo vệt VFX và thời điểm nổ sát thương.

| Tên Cột | Kiểu | Ý nghĩa |
| :--- | :---: | :--- |
| **`ActEventID`** | `int` | ID của chuỗi sự kiện hoạt ảnh (Khớp với `ActionEventID` trong `Skill.csv`). |
| **`Frame`** | `int` | Frame mốc kích hoạt sự kiện (Chuẩn **15 FPS**, $t = \text{Frame} / 15.0s$). |
| **`EventName`** | `string` | **Tên sự kiện logic:** |
| ↳ `CrossFade` | — | Hòa trộn mượt hoạt ảnh mới (`EventParam1` = Thời gian blend ms). |
| ↳ `InstantDir` | — | Tốc độ tự động xoay mặt về hướng mục tiêu (`EventParam1` = 1000°/s). |
| ↳ `PlayEffect` | — | Sinh hiệu ứng Prefab tại người Caster (`EventParam1` = `ResID` trong `EffectRes.csv`). |
| ↳ `PlaySound` | — | Phát âm thanh (`EventParam1` = `SoundID` trong `Sound.csv`). |
| ↳ `CastSkill` | — | **Thời điểm chính xác sinh đạn / nổ sát thương** (`EventParam1` = `SkillId`). |
| ↳ `CanDoSkill` | — | Cho phép người chơi bấm trước phím combo tiếp theo (Input Buffer window). |
| ↳ `CanDoRun` | — | Cho phép hủy động tác thừa sớm bằng cách bấm nút di chuyển (Animation Cancel). |
| ↳ `LinkSkillInit` | — | Bắt đầu đếm thời gian chờ combo (`Param1` = Frame timeout, `Param2` = Chiêu sau). |
| ↳ `CastLinkSkill`| — | Thời điểm chuyển tiếp sang hoạt ảnh của chiêu kế trong chuỗi combo. |
| ↳ `MovePos` | — | Lướt người tới trước (`Param1` = Quãng đường cm, `Param2` = Tốc độ). |
| ↳ `PlayShake` | — | Rung màn hình (`Param1` = Biên độ, `Param2` = Tần số, `Param3` = Thời lượng). |

---

## 5. CHI TIẾT BẢNG `NpcRes.csv`

Bảng định nghĩa Model 3D, Kích thước Collider vật lý, Âm thanh di chuyển/chết và **Tổng số Frame + Thời gian hòa trộn (CrossFade)** của từng clip hoạt ảnh.

| Tên Cột | Kiểu | Ý nghĩa trong Unity 3D |
| :--- | :---: | :--- |
| **`NpcResId`** | `int` | ID định danh tài nguyên Model 3D (Khóa chính). |
| **`NpcResFile`** | `string` | Đường dẫn file Model Prefab Unity (vd: `Model/Npc/npc_002/npc_002.prefab`). |
| **`Height`** | `float` | Chiều cao của nhân vật / quái $\rightarrow$ Gán cho `CapsuleCollider.height` (m). |
| **`Width`** | `float` | Bán kính thân của nhân vật / quái $\rightarrow$ Gán cho `CapsuleCollider.radius` (m). |
| **`HitSoundID`** | `int` | Âm thanh khi bị trúng đòn đau (Trỏ `Sound.csv`). |
| **`DeathSoundID`**| `int` | Âm thanh khi chết (Trỏ `Sound.csv`). |
| **`RunSoundID`** | `int` | Âm thanh bước chân khi chạy (Footstep Sound). |
| **`<Action>_frame`**| `int` | **Tổng số Frame chuẩn của clip đó** (vd: `run_frame = 12`, `at01_frame = 16`). |
| **`<Action>_cross`**| `float` | **Thời gian Blend hòa trộn khi chuyển sang clip đó** (vd: `0.1s`). |

---

## 6. CHI TIẾT BẢNG `ActionName.csv`

Từ điển mã hóa tên viết tắt các Clip hoạt ảnh chuẩn của game:

| Tên viết tắt | Mã ID | Tên đầy đủ trong Animator | Ý nghĩa chuyển động & Hit Reaction |
| :--- | :---: | :--- | :--- |
| **`st`** | `1` | `Stand / Idle` | Đứng yên phi chiến đấu (Thư giãn). |
| **`run`** | `2` | `Run` | Di chuyển / Chạy bộ. |
| **`die`** | `3` | `Death` | Chết gục tại chỗ (Normal Death). |
| **`jt`** | `4` | `Knockback` | Bị trúng đòn nặng đẩy lùi ra sau (Trượt chân). |
| **`sta`** | `7` | `Combat Ready` | Thủ thế sẵn sàng chiến đấu. |
| **`bat`** | `9` | **`Hit Flinch`** | **Bị thương nhẹ giật mình tại chỗ (0.15s, chân đứng nguyên).** |
| **`at01` .. `at04`**| `16..19`| `Attack 1..4` | Đòn đánh thường chuỗi combo từ 1 đến 4. |
| **`jfd`** | `20` | `Knockdown Death` | Bị đánh văng lên đập đất nằm chết luôn. |
| **`jn01` .. `jn05`**| `21..25`| `Skill 1..5` | Kỹ năng phái 1, 2, 3, 4 và Tuyệt kỹ Nộ (Q, W, E, R, Ulti). |
| **`jf`** | `26` | **`Knockup & Getup`** | **Bị hất tung lên trời $\rightarrow$ Rơi xuống đập đất $\rightarrow$ Đứng dậy.** |
| **`dz`** | `28` | `Sit / Meditate` | Ngồi thiền / Đả tọa hồi phục sinh lực. |
| **`wlk`** | — | `Walk` | Đi bộ chậm rãi. |
| **`qg` / `qg01..05`**| — | `Qinggong` | Khinh công bay lượn / Né tránh (Dodge). |

---

## 7. CHI TIẾT BẢNG `NpcTemplate.csv` & `Field_HeaderBoss.csv`

Bảng dữ liệu định cấu hình cho toàn bộ Quái thường, Quái Tinh Anh, Thủ Lĩnh và Boss Thế Giới.

| Tên Cột | Kiểu | Ý nghĩa |
| :--- | :---: | :--- |
| **`TemplateID`** | `int` | ID mẫu quái/Boss (Khóa chính). |
| **`Name`** | `string` | Tên hiển thị trên đầu quái (Tiếng Việt). |
| **`Kind`** | `int` | Phân loại: `0` = Quái vật thường, `1` = Người chơi/Clone, `2` = NPC đàm thoại, `3` = Đồng hành, `4` = Cơ quan/Cổng, `5` = Rương/Khoáng sản, `6` = Bẫy. |
| **`Camp`** | `int` | Phe phái chiến đấu: `0`=Player, `1`=Monster (Thù địch), `2`=Trung lập, `3`=Tống, `4`=Kim. |
| **`NpcResID`** | `int` | ID ngoại hình 3D (Trỏ sang `NpcRes.csv` để lấy Model & Collider). |
| **`NpcAttribID`** | `int` | ID chỉ số máu, công, thủ, kháng (Trỏ sang `NpcAttribute.csv`). |
| **`NormalSkill1..3`**| `int` | ID các chiêu thức tấn công quái sở hữu (Trỏ sang `Skill.csv`). |
| **`NormalSkillLevel1..3`**| `int` | Cấp độ của từng chiêu thức tương ứng. |
| **`VisionRadius`** | `int` | Tầm nhìn phát hiện kẻ địch (`VisionRadius / 100` mét). |
| **`ActiveRadius`** | `int` | Tầm truy đuổi tối đa trước khi tự động quay về điểm xuất phát (Leash Range). |
| **`AiFile`** | `string` | File kịch bản AI điều khiển hành vi (vd: `Normal_Monster.lua`, `Boss_Ai.lua`). |
| **`FightMode`** | `int` | Trạng thái chiến đấu: `0` = Hòa bình (Không thể bị đánh), `1` = Chiến đấu. |
| **`RunSpeed`** | `int` | Tốc độ chạy di chuyển (`RunSpeed / 100.0f` m/s, vd: `450` = `4.5 m/s`). |
| **`WalkSpeed`** | `int` | Tốc độ đi bộ (`WalkSpeed / 100.0f` m/s). |
| **`ReviveFrame`** | `int` | Thời gian hồi sinh sau khi chết (`ReviveFrame / 15.0f` giây chuẩn 15 FPS). |
| **`BloodStyle`** | `int` | Kiểu thanh máu trên đầu: `1` = Thanh máu nhỏ (Quái thường), `2` = Thanh máu nhiều lớp nhiều màu trên màn hình (Boss). |
| **`AuraSkillId`** | `int` | Kỹ năng hào quang nội tại tự động phát ra liên tục. |
| **`ForbitMove`** | `int (0/1)` | `1` = Quái đứng cố định tại chỗ (vd: Trụ thủ thành, Bẫy gắp). |
| **`DropFile`** | `string` | File cấu hình bảng rơi đồ khi quái chết. |
| **`DropType`** | `int` | Quy tắc nhặt đồ: `0` = Rơi tự do ai cũng nhặt được, `1` = Thuộc về người gây sát thương nhiều nhất (Top Damager). |

---

### 🐺 Cơ Chế Tấn Công Thường & Chọn Mục Tiêu Của Quái Vật (Monster AI Combat Loop):

Trong engine, **đòn cắn thường / đánh thường của quái thực chất vẫn là một Skill ID** (định nghĩa tại cột `NormalSkill1` như Skill `21`, `22`...):

1. **Phát hiện mục tiêu (Aggro Detection):**
   - Quái liên tục quét tìm đối tượng khác phe (`Camp`) trong phạm vi **`VisionRadius`** (vd: `560` = 5.6m).
   - Khi phát hiện người chơi, AI đặt `TargetID = PlayerID` và bắt đầu bám đuổi.
   - Nếu người chơi chạy vượt quá **`ActiveRadius`** (vd: `980` = 9.8m), quái **hủy mục tiêu, miễn nhiễm sát thương và tự động quay về điểm xuất phát (Reset Leash / Full HP)**.

2. **Cơ chế ra đòn cắn thường (`NormalSkill1`):**
   - Khi khoảng cách tới mục tiêu $\le$ **`AttackRadius`** của skill (vd: `150` = 1.5m):
   - **Xoay mặt khóa hướng:** Quái tự động xoay góc Euler trục $Y$ hướng $100\%$ về phía tâm người chơi (`LookAt / Dir64`).
   - **Phát động tác cắn:** Kích hoạt hoạt ảnh `at01` (`CastActionId = 16`).
   - **Nổ sát thương theo Frame:** Khi chạy đến mốc `HitFrame` trong `ActionEvent.csv` (vd: Frame 4), quái quét Hitbox hình quạt/tròn 1.5m trước mặt để trừ máu mục tiêu.
   - **Né đòn (Dodge):** Nếu người chơi dùng Khinh công / Lướt né ra sau lưng quái trước mốc `HitFrame`, đòn cắn sẽ vung vào khoảng không (trượt sát thương).

---

## 8. CHI TIẾT BẢNG `Character.csv`

Bảng cấu hình các nhân vật người chơi mặc định của các môn phái.

* Cấu trúc cột tương tự `NpcTemplate.csv`, bổ sung:
  * **`Sex`**: Giới tính (`1` = Nam, `2` = Nữ, `3` = Loli/Shota).
  * **`Faction`**: ID môn phái tương ứng.
  * **`StartActEvent`**: Chuỗi hoạt cảnh giới thiệu khi tạo nhân vật mới.

---

## 9. CHI TIẾT BẢNG `NpcAttribute.csv`

Bảng quy định toàn bộ chỉ số chiến đấu và sát thương Ngũ Hành.

| Tên Cột | Kiểu | Ý nghĩa trong Công thức Tính Dame |
| :--- | :---: | :--- |
| **`AttribID`** | `int` | ID bảng chỉ số (Khóa chính, trỏ từ `NpcTemplate` hoặc `Character`). |
| **`MaxLife`** | `int` | Sinh lực tối đa (HP Max). |
| **`MinBaseAttack`** | `int` | Sát thương tấn công cơ bản tối thiểu (Min Physical Attack). |
| **`MaxBaseAttack`** | `int` | Sát thương tấn công cơ bản tối đa (Max Physical Attack). |
| **`HitRate`** | `int` | Điểm chính xác (Tăng tỉ lệ đánh trúng). |
| **`Miss`** | `int` | Điểm né tránh (Tăng tỉ lệ đối phương đánh trượt). |
| **`DeadlyStrike`** | `int` | Điểm chí mạng / Bạo kích (Tăng tỉ lệ nổ sát thương $\times 1.8$). |
| **`MetalDamage`** | `int` | Điểm sát thương thuộc tính **Hệ Kim**. |
| **`WoodDamage`** | `int` | Điểm sát thương thuộc tính **Hệ Mộc**. |
| **`WaterDamage`** | `int` | Điểm sát thương thuộc tính **Hệ Thủy**. |
| **`FireDamage`** | `int` | Điểm sát thương thuộc tính **Hệ Hỏa**. |
| **`EarthDamage`** | `int` | Điểm sát thương thuộc tính **Hệ Thổ**. |
| **`MetalResist`** | `int` | Kháng sát thương Hệ Kim (% giảm sát thương). |
| **`WoodResist`** | `int` | Kháng sát thương Hệ Mộc. |
| **`WaterResist`** | `int` | Kháng sát thương Hệ Thủy. |
| **`FireResist`** | `int` | Kháng sát thương Hệ Hỏa. |
| **`EarthResist`** | `int` | Kháng sát thương Hệ Thổ. |
| **`AttackSpeed`** | `int` | Tốc độ xuất chiêu (% giảm thời gian animation). |

---

## 10. CHI TIẾT BẢNG `EffectRes.csv`

Từ điển tra cứu đường dẫn Prefab VFX 3D trong Unity.

| Tên Cột | Kiểu | Ý nghĩa |
| :--- | :---: | :--- |
| **`ResID`** | `int` | Mã số định danh của Effect (Khóa chính). |
| **`ResFilePath`** | `string` | **Đường dẫn Prefab hiệu ứng chất lượng cao (HD)** (vd: `Effect/Prefabs/JueSe/emei/JN_02_DD.prefab`). |
| **`LowResFilePath`**| `string` | Đường dẫn Prefab hiệu ứng bản nhẹ tối ưu cho máy yếu (Mobile Low Profile). |
| **`WeaponEffectSkillPath1`**| `string` | Đường dẫn Prefab hiệu ứng tương thích khi nhân vật có gắn Vũ Khí phát sáng. |
| **`LockRotate`** | `int (0/1)`| `1` = Cố định góc xoay hiệu ứng, không xoay theo nhân vật. |

---

## 11. CHI TIẾT BẢNG `StateEffect.csv`, `PartSlot.csv` & QUY TẮC XOAY / KHÓA XOAY EFFECT

Quy định toàn diện về khớp xương gắn hiệu ứng, quy tắc xoay tự do và cơ chế khóa góc xoay (Lock Rotation) cho toàn bộ hệ thống VFX 3D.

---

### 🏛️ Bảng Tra Cứu Toàn Bộ Khớp Xương (`PartSlot.csv`) & Chế Độ Xoay:

| `SlotId` | Tên Khớp Xương (`SlotName`) | Vị trí mô tả (`Des`) | Chế độ Xoay (Rotation Mode) | Ứng dụng thực tế |
| :---: | :--- | :--- | :---: | :--- |
| **`1` / `17`** | **`B_RH` / `Bip01 R Hand`** | Đục lỗ tay phải / Bàn tay phải | 🟢 **Follow Bone** (Xoay 100% theo tay) | Kiếm phát sáng, tụ lực vũ khí `_WQ`, vệt chém. |
| **`2` / `18`** | **`B_LH` / `Bip01 L Hand`** | Đục lỗ tay trái / Bàn tay trái | 🟢 **Follow Bone** (Xoay 100% theo tay) | Chưởng pháp, nạp tên, khiên năng lượng tay trái. |
| **`3`** | **`B_Spine2`** | Đục lỗ cánh / Xương sống trên | 🟢 **Follow Bone** (Xoay theo lưng) | Cánh ngoại trang bay (Wings). |
| **`4` / `5`** | **`Bip001` / `Bip01`** | Căn cốt gốc nhân vật | 🔒 **Lock Pitch & Roll** | Trọng tâm cơ thể. |
| **`6`** | **`back`** | Lỗ lưng / Sau lưng | 🟢 **Follow Bone** | Phi phong, cánh, hồ lô sau lưng. |
| **`7`** | **`Bip01 Spine1`** | Lỗ đai cơ thể / Ngực | 🔒 **Lock Pitch & Roll** (Chỉ xoay trục Y) | Hào quang hộ thể quanh người (Shield), hồi máu. |
| **`8`** | **`Bone001`** | Lỗ bên tay phải | 🟢 **Follow Bone** | Khớp phụ trợ tay phải. |
| **`11`** | **`S_RH`** | NPC tay phải | 🟢 **Follow Bone** | Hiệu ứng tay phải cho Quái/NPC. |
| **`12`** | **`S_LH`** | NPC tay trái | 🟢 **Follow Bone** | Hiệu ứng tay trái cho Quái/NPC. |
| **`13` / `14`** | **`S_RH_01` / `S_LH_01`** | NPC chân phải / chân trái | 🟢 **Follow Bone** | Vết lửa chân quái, đá chân. |
| **`15` / `16`** | **`S_Hat` / `S_HAT_01`** | NPC đầu / Đỉnh đầu (Dummy Anchor) | 🔒 **Lock Entire / Billboard** | Choáng stun (sao bay), icon Buff, câm lặng. |
| **`19` / `20`** | **`Bip01 R/L Foot`** | Chân phải / Chân trái | 🔒 **Flat Ground (Khóa phẳng Oxz)** | Vòng sáng trận pháp dưới chân, đài sen hồi máu. |
| **`21`** | **`Bip001 Head`** | Lỗ đầu / Đầu nhân vật | 🟢 **Follow Bone** | Mặt nạ, mắt phát sáng, nón đội. |
| **`22`** | **`Bip01 Pelvis`** | Vĩ ba cốt cách / Hông / Xương chậu | 🔒 **Lock Pitch & Roll** | Hào quang tỏa tròn quanh hông. |
| **`152..154`**| **`B_Hs` / `B_Hs001..002`**| Yên thú cưỡi (Trước / Sau) | 🟢 **Follow Mount** | Hiệu ứng bước chân ngựa, yên cương thú cưỡi. |
| **`155 / 156`**| **`Bone033` (L/R)** | Thần cơ ưng (Trái / Phải) | 🟢 **Follow Bone** | Hiệu ứng phái Thần Cơ bay lượn. |
| *(Để trống)* | **`Root` / `Transform.position`**| Gốc tọa độ chân nhân vật | 🔒 **Theo cờ `LockRotate`** | Đạn đạo, vệt quét AOE dưới đất. |

---

### 📐 3 Chế Độ Xoay & Nguyên Lý Xử Lý Trong Engine:

1. **🟢 Chế độ `Follow Bone Full` (Socket Mode - Xoay 100% theo xương):**
   * **Áp dụng cho:** `SlotID = 1, 2, 3, 6, 8, 11, 12, 17, 18, 21, 152..156`.
   * **Nguyên lý:** Gán làm con trực tiếp của Bone (`transform.SetParent(targetBone)`), `localPosition = Vector3.zero`, `localRotation = Quaternion.identity`.
   * **Mục đích:** Xương vung chém hay uốn lượn thì vệt kiếm, hào quang vũ khí và cánh uốn lượn theo 100%.

2. **🔒 Chế độ `Lock Pitch & Roll` (Upright Body Mode - Khóa góc gập lưng):**
   * **Áp dụng cho:** `SlotID = 7` (Spine1), `SlotID = 22` (Pelvis) hoặc các `StateEffect` để trống `SlotID`.
   * **Nguyên lý:** Tọa độ bám theo xương (`Position = bone.position`), nhưng góc xoay **chỉ xoay theo hướng mặt phẳng nằm ngang OXZ của nhân vật** (`Rotation = Quaternion.Euler(0, character.eulerAngles.y, 0)`).
   * **Mục đích:** Triệt tiêu hoàn toàn góc gập lưng/lắc người của Spine khi nhân vật chạy nhảy hay ngã đập đất $\rightarrow$ Vòng bảo hộ/khiên luôn đứng thẳng đứng theo trục trời (`Vector3.up`).

3. **🔒 Chế độ `Flat Ground / Lock Entire` (Ground & Head Mode - Khóa phẳng hoàn toàn):**
   * **Áp dụng cho:** `SlotID = 15, 16, 19, 20` hoặc bất kỳ Effect nào có **`LockRotate = 1`** trong `EffectRes.csv`.
   * **Nguyên lý:** Tọa độ bám theo nhân vật (`Position`), góc xoay giữ phẳng nằm ngang trên mặt đất hoặc giữ nguyên góc xoay ban đầu lúc xuất chiêu.
   * **Mục đích:** Bông sen nở dưới chân (`JN_01`), ma trận ngũ hành hay icon choáng trên đầu không bị quay lộn vòng khi nhân vật cử động chân tay.

---

### 💻 Code C# Mẫu Quản Lý Gắn & Xoay Chuẩn Cho Unity:

```csharp
public class EffectAttachmentController : MonoBehaviour
{
    public static void AttachEffect(
        GameObject effectObj, 
        Transform characterTransform, 
        Transform targetBone, 
        int slotId, 
        bool isLockRotateFromCsv)
    {
        // 1. Kiểm tra nhóm xoay 100% theo xương
        if (!isLockRotateFromCsv && (slotId == 1 || slotId == 2 || slotId == 3 || slotId == 6 || slotId == 21))
        {
            effectObj.transform.SetParent(targetBone != null ? targetBone : characterTransform);
            effectObj.transform.localPosition = Vector3.zero;
            effectObj.transform.localRotation = Quaternion.identity;
            return;
        }

        // 2. Nhóm khóa xoay phẳng mặt đất (LockRotate = 1 hoặc chân 19/20)
        if (isLockRotateFromCsv || slotId == 19 || slotId == 20)
        {
            var groundFollower = effectObj.AddComponent<FlatGroundFollower>();
            groundFollower.Init(characterTransform, targetBone);
            return;
        }

        // 3. Nhóm Buff thân / Trọng tâm ngực (Khóa Pitch/Roll, giữ trục thẳng đứng)
        var bodyFollower = effectObj.AddComponent<UprightBodyFollower>();
        bodyFollower.Init(characterTransform, targetBone);
    }
}
```

## 12. CHI TIẾT BẢNG `FactionSkill.csv` & `AutoAiSkill.csv`

* **`FactionSkill.csv`**: Định vị vị trí nút bấm chiêu trên giao diện UI:
  * `BtnName`: Tên slot nút (`Skill_1`, `Skill_2`, `Skill_3`, `Skill_4`, `Skill_Dodge`, `Attack`).
  * `IsAnger`: `1` = Nút Tuyệt kỹ Nộ (Ulti).

* **`AutoAiSkill.csv`**: Quy tắc AI tự động chọn mục tiêu & điều kiện xuất chiêu (Boss / Đồng hành / Auto Combat):
  * `LiftPercent`: Chỉ kích hoạt chiêu khi % Sinh lực bản thân hoặc đối phương $\le X\%$ (vd: `95` = máu dưới 95%).
  * **`Selector` (Quy tắc ưu tiên chọn mục tiêu):**
    * **`Player`**: Ưu tiên nhắm vào **Người chơi thật** (bỏ qua đệ tử/pet).
    * **`Poorest`**: Ưu tiên mục tiêu có **% Máu THẤP NHẤT** trong phạm vi (Dùng cho chiêu Hồi máu / Hộ thuẫn).
    * **`Random`**: Chọn **Ngẫu nhiên** 1 kẻ địch trong tầm đánh.
    * *(Để trống / Default)*: Tự động đánh kẻ địch **Gần nhất** (Closest Enemy) hoặc mục tiêu đang giữ điểm thù hận (Aggro) cao nhất.

---

## 13. CHI TIẾT BẢNG `Sound.csv`

* **`SoundID`**: Mã số âm thanh.
* **`Bank`**: Tên gói SoundBank Wwise / Audio Source (`Em` = Nga Mi, `Shaolin`, `Tianwang`...).
* **`Sound`**: Tên Event Audio (vd: `Play_Em_03_01`, `Play_Em_03_Hit`).

---

## 14. TỔNG HỢP TOÀN BỘ BẢNG MÃ ENUM CHUẨN C# CHO UNITY

Toàn bộ các enum dưới đây có thể copy trực tiếp vào project Unity C# để map 1:1 với dữ liệu CSV:

```csharp
namespace GameData.Combat
{
    // Phân loại thực thể
    public enum NpcKind
    {
        Monster = 0,    // Quái vật thường / Quái tinh anh
        Player = 1,     // Người chơi / Phân thân
        DialogNpc = 2,  // NPC giao tiếp / Nhiệm vụ
        Partner = 3,    // Đồng hành / Pet
        Portal = 4,     // Cổng dịch chuyển / Cơ quan
        GatherBox = 5,  // Rương báu / Lửa trại / Khoáng sản
        Trap = 6        // Cạm bẫy
    }

    // Phe phái chiến đấu
    public enum NpcCamp
    {
        Player = 0,     // Phe Người chơi
        Monster = 1,    // Phe Quái vật (Thù địch)
        Neutral = 2,    // Phe Trung lập
        Song = 3,       // Phe Tống
        Jin = 4         // Phe Kim
    }

    // Dạng đạn đạo / Hình thái kỹ năng
    public enum MissileFormType
    {
        StraightLinear = 1, // Đạn bắn thẳng theo đường thẳng
        SpreadFan = 2,      // Đạn bắn chùm nhiều tia hình quạt
        CircularRing = 3,   // Vòng tròn tỏa rộng quanh Caster
        ChainBouncing = 4,  // Đạn nảy bật liên hoàn giữa các mục tiêu
        SkyDrop = 5         // Mưa tên / Thiên thạch rơi từ trên trời xuống
    }

    // Cơ chế di chuyển của đạn
    public enum MissileMoveKind
    {
        StaticTrap = 0,     // Đặt bẫy / Bãi nổ cố định tại chỗ
        Linear = 1,         // Bay thẳng theo vector ban đầu
        HomingTracking = 2, // Tự bám đuổi / uốn lượn theo mục tiêu đang khóa
        DashWithCaster = 3  // Di chuyển dính liền theo thân người lướt
    }

    // Hình dạng Hitbox quét va chạm
    public enum HitboxShape
    {
        SingleTarget = 0,   // Đơn mục tiêu (Raycast)
        CircleSphere = 1,   // Hình tròn / Khối cầu (SphereCast / OverlapSphere)
        SectorFan = 2,      // Hình cánh quạt (Bán kính + Góc mở)
        LineBox = 3,        // Hình chữ nhật đâm tới (BoxCast)
        RingTorus = 4       // Vòng xuyến / Trụ rỗng
    }

    // Gốc tọa độ tham chiếu của Kỹ năng (StartPosType)
    public enum SkillStartPosType
    {
        CasterOrigin = 1,   // Caster-Based: Lấy người ra chiêu làm gốc (Đánh thường, bắn thẳng)
        TargetPosition = 2, // Target-Based: Lấy mục tiêu làm gốc tham chiếu (Chiêu khóa Target / Bẫy / Cầu nước)
        GroundPoint = 3     // Ground-Based: Lấy điểm chạm đất / con trỏ chỉ định
    }

    // Ngũ hành thuộc tính
    public enum ElementalSeries
    {
        None = 0,   // Vô hệ
        Metal = 1,  // Hệ Kim (Thiếu Lâm, Thiên Vương)
        Wood = 2,   // Hệ Mộc (Đường Môn, Ngũ Độc)
        Water = 3,  // Hệ Thủy (Nga Mi, Thúy Yên)
        Fire = 4,   // Hệ Hỏa (Thiên Nhẫn, Đào Hoa)
        Earth = 5   // Hệ Thổ (Võ Đang, Côn Lôn)
    }

    // Khớp xương gắn hiệu ứng (Bone Slot)
    public enum BoneSlotID
    {
        RightHand = 1,      // B_RH (Bàn tay phải / Kiếm)
        LeftHand = 2,       // B_LH (Bàn tay trái / Cung / Khiên)
        SpineUpper = 3,     // B_Spine2 (Xương sống trên)
        Back = 6,           // back (Phi phong / Cánh sau lưng)
        ChestCenter = 7,    // Bip01 Spine1 (Ngực / Khiên hộ thể)
        Head = 15,          // S_Hat / Bip001 Head (Đỉnh đầu / Buff / Stun)
        RightFoot = 19,     // Bip01 R Foot (Chân phải)
        LeftFoot = 20       // Bip01 L Foot (Chân trái / Trận pháp đất)
    }

    // Loại vòng ngắm / Chỉ thị mục tiêu (Skill Indicator)
    public enum SkillSelectorType
    {
        None = 0,
        SmartcastCircleAOE = 1, // Vòng tròn chọn vùng đất
        DirectionalArrow = 2    // Mũi tên định hướng xoay theo Joystick
    }

    // Chế độ di chuyển của Indicator (SkillController)
    public enum SelectorMoveType
    {
        Rotate = 0, // Cố định gốc, chỉ xoay theo hướng Joystick
        Move = 1    // Kéo tâm di chuyển tự do trên mặt đất
    }

    // ResID Prefab VFX Indicator chuẩn trong EffectRes.csv
    public static class IndicatorVfxResID
    {
        public const int DirectionArrow = 7;     // Mũi tên định hướng (xingdongfangxianjiantou_G2)
        public const int TargetPoint = 8;        // Tâm điểm chỉ định (xingjinmubiaodian)
        public const int SelectedEnemyAOE = 9;   // Vòng đỏ/vàng chọn địch (xuanzhong)
        public const int TargetArrowIcon = 10;   // Icon mũi tên trên đầu (xuanzhong_tubiao)
        public const int SelectedAllyAOE = 11;   // Vòng xanh lá hỗ trợ đồng đội (xuanzhong_LV)
        public const int DangerWarning = 14;     // Vùng cảnh báo nguy hiểm Boss (YuJing_S)
    }
}
```

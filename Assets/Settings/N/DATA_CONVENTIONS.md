# 📘 TÀI LIỆU QUY ƯỚC & NGUYÊN LÝ DỮ LIỆU GAME (DATA CONVENTIONS & SYSTEM ARCHITECTURE)

Tài liệu này tổng hợp toàn bộ quy ước, định nghĩa biến, công thức chuyển đổi sang Unity, bảng mã Enum và nguyên lý vận hành của hệ thống Combat / Animation Scale / NPC / VFX / Audio trích xuất từ dữ liệu game Kiếm Hiệp (Seasun / Kingsoft). Toàn bộ thông số đã được đối chiếu và kiểm chứng trực tiếp với các tệp dữ liệu CSV gốc.

---

## 📑 MỤC LỤC
1. [Hệ Thống Đơn Vị Đo Lường & Công Thức Quy Đổi Unity](#1-hệ-thống-đơn-vị-đo-lường--công-thức-quy-đổi-unity)
2. [Chi Tiết Bảng `Skill.csv` (Kỹ Năng & Logic Xuất Chiêu)](#2-chi-tiết-bảng-skillcsv)
3. [Chi Tiết Bảng `Missile.csv` (Đạn Đạo, Hitbox & Va Chạm)](#3-chi-tiết-bảng-missilecsv)
4. [Chi Tiết Bảng `ActionEvent.csv` (Dòng Thời Gian Từng Frame Sự Kiện)](#4-chi-tiết-bảng-actioneventcsv)
5. [Nguyên Lý Scale Animation & Đồng Bộ Hóa Timeline Chuyển Động](#5-nguyên-lý-scale-animation--đồng-bộ-hóa-timeline-chuyển-động)
6. [Chi Tiết Bảng `NpcRes.csv` (Kích Thước 3D, Collider & Frame Hoạt Ảnh)](#6-chi-tiết-bảng-npcrescsv)
7. [Chi Tiết Bảng `ActionName.csv` (Từ Điển Tên Hoạt Ảnh Chuẩn)](#7-chi-tiết-bảng-actionnamecsv)
8. [Chi Tiết Bảng `NpcTemplate.csv` & `Field_HeaderBoss.csv` (Dữ Liệu Quái & Boss)](#8-chi-tiết-bảng-npctemplatecsv--field_headerbosscsv)
9. [Chi Tiết Bảng `Character.csv` (Nhân Vật Người Chơi)](#9-chi-tiết-bảng-charactercsv)
10. [Chi Tiết Bảng `NpcAttribute.csv` (Chỉ Số Chiến Đấu & Thuộc Tính Ngũ Hành)](#10-chi-tiết-bảng-npcattributecsv)
11. [Chi Tiết Bảng `EffectRes.csv` (Tài Nguyên Prefab VFX & Vũ Khí Thần Binh)](#11-chi-tiết-bảng-effectrescsv)
12. [Chi Tiết Bảng `StateEffect.csv`, `PartSlot.csv` & Quản Lý Khớp Gắn VFX](#12-chi-tiết-bảng-stateeffectcsv-partslotcsv--quản-lý-khớp-gắn-vfx)
13. [Chi Tiết Bảng `FactionSkill.csv` & `AutoAiSkill.csv` (Cây Chiêu Thức & AI Tự Đánh)](#13-chi-tiết-bảng-factionskillcsv--autoaiskillcsv)
14. [Chi Tiết Bảng `Sound.csv` (Âm Thanh SFX / Wwise Bank)](#14-chi-tiết-bảng-soundcsv)
15. [Tổng Hợp Toàn Bộ Bảng Mã Enum Chuẩn C# Cho Unity](#15-tổng-hợp-toàn-bộ-bảng-mã-enum-chuẩn-c-cho-unity)

---

## 1. HỆ THỐNG ĐƠN VỊ ĐO LƯỜNG & CÔNG THỨC QUY ĐỔI UNITY

Toàn bộ hệ thống logic thời gian và chuyển động trong database được thiết kế chạy trên nền **chuẩn 15 FPS**.

| Đại lượng trong CSV | Đơn vị gốc | Tỉ lệ quy đổi | Đơn vị Unity C# | Công thức tính trong Unity |
| :--- | :--- | :---: | :--- | :--- |
| **Thời gian Frame (ActionEvent / LifeTime / ReviveFrame...)** | 1 frame (chuẩn **15 FPS**) | $\div 15$ | Giây (Seconds) | `float timeSec = frame / 15.0f;` |
| **Thời gian hồi chiêu (`TimePerCast` / Cooldown)** | 1 frame (chuẩn **15 FPS**) | $\div 15$ | Giây (Seconds) | `float cdSec = timePerCast / 15.0f;` |
| **Khoảng cách / Tầm đánh (`AttackRadius`, `PosOffsetLenght`)** | Centimet (cm) | $\div 100$ | Mét (Meters) | `float rangeMeter = val / 100.0f;` |
| **Bán kính sát thương Đạn (`DmgRange`)** | Decimet (dm) | $\div 10$ | Mét (Meters) | `float dmgRadius = dmgRange / 10.0f;` |
| **Vận tốc bay (`Speed`)** | Game Speed Unit | $\div 10$ | Mét/giây (m/s) | `float velocity = speed / 10.0f;` |
| **Gia tốc (`AcceSpeed`)** | Game Acce Unit | $\div 10$ | $m/s^2$ | `float accel = acceSpeed / 10.0f;` |
| **Tốc độ di chuyển (`RunSpeed`, `WalkSpeed`)** | cm/s | $\div 100$ | Mét/giây (m/s) | `float moveSpeed = runSpeed / 100.0f;` |
| **Góc quay tức thì (`InstantDir`)** | Độ/giây (°/s) | $\times 1$ | Độ/giây (°/s) | `transform.rotation = Quaternion.RotateTowards(...);` |
| **Cường độ chớp sáng (`CollBrigth`)** | $0 \sim 1000$ | $\div 1000$ | $0.0f \sim 1.0f$ | `float flashAlpha = collBrigth / 1000.0f;` |
| **Tỉ lệ phần trăm (`%`)** | $0 \sim 100$ | $\div 100$ | $0.0f \sim 1.0f$ | `float percent = val / 100.0f;` |

> **⚠️ Lưu ý quan trọng (Đã kiểm chứng thực nghiệm):**
> * Mọi cột tính bằng frame như `Frame` trong `ActionEvent.csv`, `LifeTime` trong `Missile.csv`, `TimePerCast` trong `Skill.csv`, `ReviveFrame` trong `NpcTemplate.csv` đều chia cho **15.0f** để ra số giây thực trong Unity.
> * *Bằng chứng vật lý:* Missile 301 có `LifeTime = 10 frames`, `Speed = 120` (12 m/s), `AttackRadius = 550` (5.5m). Ở 15 FPS: $t = 10 / 15 = 0.667\text{s} \rightarrow S = 0.667 \times 12 = 8.0\text{m}$ (đủ bao phủ tầm đánh 5.5m). Nếu chia 30 FPS: $S = 4.0\text{m}$ (đạn biến mất trước khi chạm tầm đánh tối đa $\rightarrow$ Lỗi).

---

## 2. CHI TIẾT BẢNG `Skill.csv`

Bảng định nghĩa thuộc tính cốt lõi của mọi chiêu thức (Người chơi, Quái vật, Boss, Đệ tử).

| Tên Cột | Kiểu | Ý nghĩa & Nguyên lý vận hành |
| :--- | :---: | :--- |
| **`SkillId`** | `int` | ID duy nhất của kỹ năng (Khóa chính). |
| **`SkillName`** | `string` | Tên hiển thị của kỹ năng (Tiếng Việt). |
| **`Property`** | `string` | Phân loại ngữ cảnh (Kỹ năng môn phái, Kỹ năng quái, Buff...). |
| **`SkillType`** | `int` | **Phân loại kỹ năng:**<br>• `1`: Bị động nội tại.<br>• `2`: Đòn đánh thường quái/sub-skill.<br>• `3`: Kỹ năng bị động / Buff môn phái (vd: Kiếm Tâm Thông Minh - Skill 309).<br>• `4`: Trận pháp / Hào quang liên tục.<br>• `5`: **Kỹ năng chủ động thi triển** (bao gồm cả chuỗi đánh thường combo của người chơi 301..304 và chiêu thức 306, 308, 310). |
| **`MeleeForm`** | `int` | Kiểu đánh cận chiến: `0`/rỗng = Bình thường, `1` = Áp sát nhanh, `2` = Xoay vòng quanh người. |
| **`StartPosType`** | `int` | **Vị trí xuất phát chiêu/đạn:**<br>• `1`: Xuất phát từ **Caster** (Bản thân người ra chiêu, vd: Hồi máu 306).<br>• `2`: Xuất phát hướng về / tại **Target** (Mục tiêu đang khóa, vd: 301..305, 308, 310).<br>• `3`: Xuất phát tại **Điểm va chạm (HitPoint)**. |
| **`StartDirType`** | `int` | Hướng ngắm ban đầu: `0` = Hướng mặt nhân vật, `1` = Hướng vector chỉ tới mục tiêu. |
| **`Icon`** | `string` | Tên Sprite Icon kỹ năng trong Sprite Atlas. |
| **`IconAtlas`** | `string` | Đường dẫn prefab UI chứa Sprite Atlas (vd: `UI/Atlas/SkillIcon/EM_Skill.prefab`). |
| **`WaitTime`** | `int` | Thời gian chờ tích lực trước khi ra chiêu (Frames). |
| **`ChildID`** | `int` | ID Missile hoặc Sub-skill sinh ra khi thi triển (Trỏ sang `Missile.csv` hoặc `Skill.csv`). |
| **`ChildCount`** | `int` | Số lượng đạn/tia sinh ra trong 1 lần xuất chiêu. |
| **`MissileForm`** | `int` | **Dạng đạn đạo / Hình thái Missile:**<br>• `1`: Đạn bay thẳng bình thường (Linear Straight).<br>• `2`: Đạn bay hình quạt / chùm nhiều tia (Spread Shot).<br>• `3`: Vòng tròn tỏa ra xung quanh Caster (Circular Ring).<br>• `4`: **Đạn nảy bật liên hoàn (Chain / Bouncing)** giữa các mục tiêu (vd: Bạch Lộ Ngưng Sương - Skill 308).<br>• `5`: Rơi từ trên trời xuống (Sky Drop / Meteor).<br>• **`7`**: **Chùm đa đạn đồng loạt / Luồng sóng tỏa** (vd: Nga Mi Kiếm Pháp 4 phụ - Skill 305, Giang Hải Ngưng Ba - Skill 310). |
| **`Relation`** | `string` | Đối tượng tác dụng: `enemy` (Kẻ địch), `ally` (Đồng minh), `self` (Bản thân), **`recover`** (Trị liệu hồi phục - Skill 306, 307). |
| **`TimePerCast`** | `int` | Thời gian hồi chiêu (Cooldown) tính theo frame chuẩn 15 FPS (`Cooldown = TimePerCast / 15.0f` giây). |
| **`IsUseAR`** | `int (0/1)` | `1` = Tự động áp dụng tầm đánh `AttackRadius` khi tìm mục tiêu. |
| **`Series`** | `int` | Ngũ hành kỹ năng: `0`=Vô, `1`=Kim, `2`=Mộc, `3`=Thủy, `4`=Hỏa, `5`=Thổ. |
| **`CastActionId`** | `int` | ID hoạt ảnh ra đòn (Trỏ sang `ActionName.csv`, vd: `16` = `at01`, `21` = `jn01`, `22` = `jn02`, `23` = `jn03`). |
| **`ActionEventID`** | `int` | ID dòng thời gian sự kiện hoạt ảnh (Trỏ sang `ActionEvent.csv`). |
| **`StateEffectId`** | `int` | ID hiệu ứng trạng thái buff/debuff gắn kèm (Trỏ sang `StateEffect.csv`). |
| **`SkillStyle`** | `string` | Phong cách chiêu: `normal_remote`, `skill_remote`, `heal`, `buff_remote`, `show` (đạn diễn hoạt). |
| **`IsMelee`** | `int (0/1)` | `1` = Chiêu đánh áp sát cận chiến, `0` = Đánh tầm xa. |
| **`FlySkillId`** | `int` | **ID Sub-skill tự động kích hoạt theo nhịp** khi viên đạn chính đang bay (vd: Skill 306 gọi Skill 307). |
| **`FlyEventInterval`**| `int` | Nhịp thời gian gọi `FlySkillId` (Frames, vd: `15` frames = 1 giây kích hoạt 1 lần $\rightarrow$ Cơ chế DoT / HoT). |
| **`StartSkillID`** | `int` | ID Sub-skill phụ kích hoạt đồng thời ngay lúc bắt đầu xuất chiêu (vd: Skill 310 kích hoạt Skill 311). |
| **`HitSkillID`** | `int` | ID Sub-skill kích hoạt khi đạn bắn trúng đích. |
| **`VanishedSkillId`**| `int` | ID Sub-skill kích hoạt khi đạn hết hạn bay mà không chạm ai. |
| **`AttackRadius`** | `int` | Tầm thi triển tối đa tính bằng Centimet (`AttackRadius / 100.0f = Mét`). |
| **`ClassName`** | `string` | Tên hàm xử lý công thức sát thương trong file Lua môn phái (vd: `em_pg1`, `em_chpd`, `em_blns`). |
| **`Param1..Param6`** | `string` | Tham số logic mở rộng tùy theo `MissileForm`:<br>• Khi `MissileForm = 4` (Nảy bật):<br>&nbsp;&nbsp;- `Param1`: Số lần nảy tối đa (vd: 5 lần).<br>&nbsp;&nbsp;- `Param2`: `1` = Bật tự động tìm mục tiêu kế tiếp.<br>&nbsp;&nbsp;- `Param3`: Phạm vi quét tìm mục tiêu nảy (cm, vd: 1000cm = 10m).<br>&nbsp;&nbsp;- `Param4`: Số lần lặp lại trên cùng 1 người (`0` = không giới hạn). |
| **`CostType`** | `int` | Loại tài nguyên tiêu hao: `0` = Không tốn, `1` = Mana/Nội lực, `2` = Nộ khí (Anger). |
| **`CostValue`** | `int` | Lượng tài nguyên tiêu hao mỗi lần xuất chiêu. |
| **`FactionLimit`** | `int` | Môn phái sở hữu: `1`=Thiên Vương, `2`=Nga Mi, `3`=Đào Hoa, `4`=Tiêu Dao, `5`=Võ Đang... |
| **`ReqLevel`** | `int` | Cấp độ nhân vật yêu cầu tối thiểu để học chiêu. |
| **`IsLinkSubSkill`**| `int (0/1)` | `1` = Chiêu nằm trong chuỗi combo liên hoàn (vd: Skill 302, 303, 304). |
| **`SelectorRange`** | `int` | Phạm vi quét tìm mục tiêu ưu tiên (cm, vd: 1000cm = 10m). |
| **`SelectorType`** | `int` | Tiêu chí chọn mục tiêu: `1` = Đồng minh/Bản thân máu thấp nhất (Dùng cho Hồi máu 306). |
| **`TargetSelf`** | `int (0/1)` | `1` = Cho phép tự chọn chính bản thân làm mục tiêu thi triển. |
| **`CastSoundID`** | `int` | ID âm thanh phát ra khi bắt đầu niệm chiêu (Trỏ sang `Sound.csv`). |
| **`CanDoSkillPri`** | `int` | Cấp độ ưu tiên ngắt chiêu (Interrupt Priority). |
| **`UseLimitType`** | `int` | Giới hạn dùng: `0`=Tự do, `1`=Chỉ khi đứng yên, `2`=Chỉ trong chiến đấu. |
| **`NotChangeActFrame`**| `int (0/1)`| `1` = Khóa cứng frame hoạt ảnh, KHÔNG bị tăng tốc bởi `AttackSpeed`. |

---

## 3. CHI TIẾT BẢNG `Missile.csv`

Bảng quy định toàn bộ cơ chế vật lý của Đạn Đạo, Hitbox, Tốc độ, Vùng sát thương và Hiệu ứng va chạm.

| Tên Cột | Kiểu | Ý nghĩa & Công thức vận hành trong Unity |
| :--- | :---: | :--- |
| **`MissileId`** | `int` | ID duy nhất của viên đạn/hitbox (Khóa chính). |
| **`MissileName`** | `string` | Tên định danh của viên đạn. |
| **`MoveKind`** | `int` | **Cơ chế chuyển động:**<br>• `0`: Cố định tại chỗ (AOE bẫy / Điểm hồi máu / Trận pháp đất).<br>• `1`: Bay thẳng theo hướng lúc bắn (Linear Directional).<br>• `2`: **Đạn tự bám đuổi / uốn lượn theo mục tiêu (Homing / Tracking)**.<br>• `3`: Lướt dính liền theo thân người Caster (Dash Hitbox).<br>• `5`: Đạn bay uốn cong / quay trở lại (Boomerang / Curved). |
| **`Speed`** | `int` | Vận tốc bay (`Velocity = Speed / 10.0f` m/s). |
| **`AcceSpeed`** | `int` | Gia tốc tăng tốc khi bay (`Acceleration = AcceSpeed / 10.0f` $m/s^2$). |
| **`DmgRangeType`** | `int` | **Hình dạng Hitbox sát thương trong Database:**<br>• `0`: **Đơn mục tiêu** (Single Target Raycast).<br>• `1`: **Vùng tròn / Khối cầu** (AOE Sphere, bán kính = `DmgRange / 10.0f` mét). |
| **`DmgRange`** | `int` | Kích thước bán kính Hitbox (`Radius = DmgRange / 10.0f` mét, vd: `DmgRange = 8` $\rightarrow$ $0.8\text{m}$ bán kính). |
| **`LifeTime`** | `int` | Thời gian sống tối đa của đạn (`LifeTime / 15.0f` giây). |
| **`DelayDeleteFrame`**| `int` | Thời gian trễ trước khi hủy hoàn toàn GameObject đạn sau khi hoàn tất hiệu ứng (Frames). |
| **`IsDmgVanish`** | `int (0/1)` | `1` = Đạn chạm trúng 1 mục tiêu là nổ và biến mất ngay lập tức. |
| **`CanRepeatDmg`** | `int (0/1)` | `1` = Đạn được phép gây sát thương nhiều lần (Dùng cho đạn xuyên thấu / đạn nảy). |
| **`MissileResID`** | `int` | ID Prefab 3D của viên đạn khi đang bay (Trỏ sang `EffectRes.csv`). |
| **`CollResID`** | `int` | ID Prefab 3D nổ khi va chạm trúng đích (Hit Impact VFX, trỏ `EffectRes.csv`). |
| **`VanishResID`** | `int` | ID Prefab 3D khi đạn hết thời gian bay mà không trúng ai (Fade Out VFX). |
| **`PosOffsetLenght`**| `int` | Khoảng cách spawn lệch về phía trước mặt Caster (`Offset = PosOffsetLenght / 100.0f` m). |
| **`ColFollowTarget`**| `int (0/1)`| `1` = Điểm va chạm dính chặt theo xương mục tiêu. |
| **`IsIgnoreBarrier`**| `int (0/1)`| `1` = Đạn bay xuyên qua chướng ngại vật địa hình thấp. |
| **`CollSoundID`** | `int` | ID âm thanh khi bắn trúng đích (Trỏ sang `Sound.csv`). |
| **`FlySoundID`** | `int` | ID âm thanh phát ra liên tục khi đạn đang bay (Looping Audio). |
| **`CollBrigth`** | `int` | Cường độ chớp sáng màn hình khi trúng đòn (`Alpha = CollBrigth / 1000.0f`). |
| **`CollBrigthFrame`**| `int` | Số frame duy trì chớp sáng màn hình. |
| **`IsHitFloat`** | `int (0/1)` | `1` = Trúng đạn làm mục tiêu bị hất nổi bồng bềnh trên không. |

---

## 4. CHI TIẾT BẢNG `ActionEvent.csv`

Bảng quy định **Timeline chính xác từng frame** của hoạt ảnh nhân vật khi tung đòn, hòa trộn animation, phát âm thanh, tạo vệt VFX và thời điểm nổ sát thương.

> **📌 Quy ước Frame:**
> * Cột `Frame` tính theo chuẩn **15 FPS** ($t = \text{Frame} / 15.0\text{s}$).
> * Nếu trường `Frame` **để trống**, mặc định hiểu là **`Frame = 0`** (kích hoạt tức thời tại $t = 0\text{s}$).

| Tên Sự Kiện (`EventName`) | `Param1` | `Param2` | `Param3` | `Param4` | `Param5` | Ý nghĩa logic trong Unity |
| :--- | :---: | :---: | :---: | :---: | :---: | :--- |
| **`CrossFade`** | Blend ms | — | — | — | — | Hòa trộn mượt hoạt ảnh mới (`Param1 = 1` tức blend nhanh, `500` tức 0.5s). |
| **`InstantDir`** | Tốc độ xoay | — | — | — | — | Tốc độ tự động xoay mặt về hướng mục tiêu (`Param1 = 1000` °/s). |
| **`PlayEffect`** | `ResID` | — | — | **`SlotId`** | **`Duration`** | Sinh VFX `ResID` gắn vào khớp xương `SlotId` (vd: `Param4 = 1` là tay phải). Duy trì trong `Duration` frame (`-1` = vĩnh viễn). |
| **`UnbindEffect`** | `ResID` | — | — | — | — | Tháo gỡ Prefab VFX `ResID` cụ thể đang bám trên người nhân vật. |
| **`ClearEffect`** | `ResID / SlotID`| — | — | — | — | Xóa sạch toàn bộ hiệu ứng trên nhân vật hoặc slot chỉ định. |
| **`PlayEffectNoClear`**| `ResID` | — | — | `SlotId` | `Duration` | Phát VFX đặc biệt không bị dọn dẹp bởi các lệnh `ClearEffect` sau đó. |
| **`PlaySound`** | `SoundID` | — | — | — | — | Phát âm thanh từ Wwise (`Param1 = SoundID` trong `Sound.csv`). |
| **`CastSkill`** | `SkillId` | — | — | — | — | **Thời điểm chính xác sinh đạn / nổ sát thương** của kỹ năng. |
| **`CanDoSkill`** | `1` | — | — | — | — | Mở cửa sổ cho phép người chơi bấm trước phím combo tiếp theo (Input Buffer). |
| **`CanDoRun`** | — | — | — | — | — | **Cho phép ngắt động tác thừa sớm** nếu người chơi bấm nút di chuyển (Animation Cancel). |
| **`LinkSkillInit`** | TimeOut (fr)| `NextSkillId`| — | — | — | Khởi tạo thời gian chờ bấm chuỗi combo (`Param2` = Chiêu kế tiếp). |
| **`CastLinkSkill`**| — | — | — | — | — | Mốc thời gian chuyển tiếp sang hoạt ảnh của chiêu kế nếu đã nhận lệnh combo. |
| **`MovePos`** | Quãng đường (cm)| Tốc độ | — | — | — | Ép nhân vật lướt tới trước theo khoảng cách và tốc độ quy định. |
| **`PlayShake`** | Biên độ | Tần số | Thời lượng| — | — | Kích hoạt hiệu ứng rung màn hình Camera Shake. |
| **`ModelEffectVisible`**| `0/1` | — | — | — | — | Ẩn (`0`) hoặc hiện (`1`) toàn bộ hiệu ứng Mesh Renderer của nhân vật. |

---

## 5. NGUYÊN LÝ SCALE ANIMATION & ĐỒNG BỘ HÓA TIMELINE CHUYỂN ĐỘNG

### 📐 1. Công Thức Scale Tốc Độ Animation:
Khi import Model 3D vào Unity, thời lượng file FBX gốc (`clip.length` tính bằng giây) có thể dài ngắn khác nhau tùy theo Animator. Trong game, mọi hoạt ảnh đều phải scale tốc độ phát để khớp chính xác với số frame quy định trong [NpcRes.csv](file:///C:/Users/zolcol/Desktop/Data/CSV/N/NpcRes.csv):

$$\text{Target Duration (giây)} = \frac{\text{action\_frame}}{15.0f}$$

$$\text{Base Anim Speed Multiplier} = \frac{\text{clip.length}}{\text{Target Duration}} = \frac{\text{clip.length} \times 15.0f}{\text{action\_frame}}$$

Khi nhân vật có chỉ số Tốc Độ Đánh (`AttackSpeed` trong `NpcAttribute.csv`), nếu chiêu thức không bị cờ `NotChangeActFrame = 1`:

$$\text{Final Anim Speed} = \text{Base Anim Speed Multiplier} \times \left(1.0f + \frac{\text{AttackSpeed}}{100.0f}\right)$$

---

### ⏱️ 2. Đồng Bộ Hóa Timeline Sự Kiện Theo `NormalizedTime`:
Để toàn bộ âm thanh, đạn nổ và VFX luôn khớp chuẩn 100% với chuyển động vung tay của Model 3D ở bất kỳ tốc độ phát nào, hệ thống phải quy đổi mốc `Frame` trong `ActionEvent.csv` sang tỉ lệ chuẩn hóa $0.0 \rightarrow 1.0$:

$$\text{Event Normalized Time} = \frac{\text{Event.Frame}}{\text{action\_frame}}$$

* **Ví dụ thực tế Skill 301 (Nga Mi Kiếm Pháp 1 - `at01_frame = 9`):**
  * Frame 1 (`PlaySound`): Kích hoạt khi animation chạy đến $1 / 9 = 11.1\%$.
  * Frame 3 (`CastSkill` - Ra kiếm khí): Kích hoạt khi animation chạy đến $3 / 9 = 33.3\%$.
  * Frame 5 (`CanDoRun` / `CastLinkSkill`): Mở cửa sổ ngắt chiêu khi animation chạy đến $5 / 9 = 55.5\%$.

---

### 🔄 3. Cơ Chế Ngắt Động Tác Thừa (Animation Cancel):
* **Từ Frame 0 đến Frame 5:** Giai đoạn ra đòn (Front Swing). Nhân vật không thể bị ngắt bởi lệnh di chuyển thông thường.
* **Tại Frame 5 (`CanDoRun`):** 
  * Nếu người chơi **bấm phím di chuyển** hoặc **bấm đánh tiếp chiêu 2**: Engine lập tức hủy bỏ 4 frame thu hồi đòn thừa (Back Swing từ Frame 5 đến Frame 9) và chuyển trạng thái ngay lập tức $\rightarrow$ Tạo cảm giác combat nhạy bén, không bị trễ.
  * Nếu người chơi **không bấm gì**: Nhân vật thực hiện nốt động tác thu kiếm đến hết Frame 9 rồi tự động chuyển mượt về thế đứng (`st` / `sta`) theo thời gian `at01_cross` (0.1s).

---

## 6. CHI TIẾT BẢNG `NpcRes.csv`

Bảng định nghĩa Model 3D, Kích thước Collider vật lý, Âm thanh di chuyển/chết và **Tổng số Frame chuẩn + Thời gian hòa trộn (CrossFade)** của từng clip hoạt ảnh.

| Tên Cột | Kiểu | Ý nghĩa trong Unity 3D |
| :--- | :---: | :--- |
| **`NpcResId`** | `int` | ID định danh tài nguyên Model 3D (Khóa chính). |
| **`NpcResFile`** | `string` | Đường dẫn file Model Prefab Unity (vd: `Player/Npcs/Prefabs/npc_289.prefab`). |
| **`Height`** | `float` | Chiều cao của nhân vật / quái $\rightarrow$ Gán cho `CapsuleCollider.height` (m). |
| **`Width`** | `float` | Bán kính thân của nhân vật / quái $\rightarrow$ Gán cho `CapsuleCollider.radius` (m). |
| **`HitSoundID`** | `int` | Âm thanh khi bị trúng đòn đau (Trỏ `Sound.csv`). |
| **`DeathSoundID`**| `int` | Âm thanh khi chết (Trỏ `Sound.csv`). |
| **`RunSoundID`** | `int` | Âm thanh bước chân khi chạy (Footstep Sound). |
| **`<Action>_frame`**| `int` | **Tổng số Frame chuẩn của clip đó tại 15 FPS** (vd: `at01_frame = 9`, `jn01_frame = 13`, `run_frame = 12`). |
| **`<Action>_cross`**| `float` | **Thời gian Blend hòa trộn khi chuyển sang clip đó** tính bằng giây (vd: `0.1s`). |

---

## 7. CHI TIẾT BẢNG `ActionName.csv`

Từ điển mã hóa tên viết tắt các Clip hoạt ảnh chuẩn của game:

| Tên viết tắt | Mã ID | Tên đầy đủ trong Animator | Ý nghĩa chuyển động & Hit Reaction |
| :--- | :---: | :--- | :--- |
| **`st`** | `1` | `Stand / Idle` | Đứng yên phi chiến đấu (Thư giãn). |
| **`run`** | `2` | `Run` | Di chuyển / Chạy bộ. |
| **`die`** | `3` | `Death` | Chết gục tại chỗ (Normal Death). |
| **`jt`** | `4` | `Knockback` | Bị trúng đòn nặng đẩy lùi ra sau (Trượt chân). |
| **`sta`** | `7` | `Combat Ready` | Thủ thế sẵn sàng chiến đấu. |
| **`bat`** | `9` | **`Hit Flinch`** | **Bị thương nhẹ giật mình tại chỗ (6 frames ~ 0.4s, chân đứng nguyên).** |
| **`at01` .. `at04`**| `16..19`| `Attack 1..4` | Đòn đánh thường chuỗi combo từ 1 đến 4. |
| **`jfd`** | `20` | `Knockdown Death` | Bị đánh văng lên đập đất nằm chết luôn. |
| **`jn01` .. `jn05`**| `21..25`| `Skill 1..5` | Kỹ năng phái 1, 2, 3, 4 và Tuyệt kỹ Nộ (Q, W, E, R, Ulti). |
| **`jf`** | `26` | **`Knockup & Getup`** | **Bị hất tung lên trời $\rightarrow$ Rơi xuống đập đất $\rightarrow$ Đứng dậy.** |
| **`dz`** | `28` | `Sit / Meditate` | Ngồi thiền / Đả tọa hồi phục sinh lực. |
| **`wlk`** | — | `Walk` | Đi bộ chậm rãi. |
| **`qg` / `qg01..05`**| — | `Qinggong` | Khinh công bay lượn / Né tránh (Dodge). |

---

## 8. CHI TIẾT BẢNG `NpcTemplate.csv` & `Field_HeaderBoss.csv`

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
| **`VisionRadius`** | `int` | Tầm nhìn phát hiện kẻ địch (`VisionRadius / 100.0f` mét). |
| **`ActiveRadius`** | `int` | Tầm truy đuổi tối đa trước khi tự động quay về điểm xuất phát (Leash Range). |
| **`AiFile`** | `string` | File kịch bản AI điều khiển hành vi (vd: `Normal_Monster.lua`, `Boss_Ai.lua`). |
| **`FightMode`** | `int` | Trạng thái chiến đấu: `0` = Hòa bình (Không thể bị đánh), `1` = Chiến đấu. |
| **`RunSpeed`** | `int` | Tốc độ chạy di chuyển (`RunSpeed / 100.0f` m/s, vd: `450` = `4.5 m/s`). |
| **`WalkSpeed`** | `int` | Tốc độ đi bộ (`WalkSpeed / 100.0f` m/s). |
| **`ReviveFrame`** | `int` | Thời gian hồi sinh sau khi chết (`ReviveFrame / 15.0f` giây). |
| **`BloodStyle`** | `int` | Kiểu thanh máu trên đầu: `1` = Thanh máu nhỏ (Quái thường), `2` = Thanh máu nhiều lớp (Boss). |
| **`AuraSkillId`** | `int` | Kỹ năng hào quang nội tại tự động phát ra liên tục. |
| **`ForbitMove`** | `int (0/1)` | `1` = Quái đứng cố định tại chỗ (vd: Trụ thủ thành, Bẫy gắp). |
| **`DropFile`** | `string` | File cấu hình bảng rơi đồ khi quái chết. |
| **`DropType`** | `int` | Quy tắc nhặt đồ: `0` = Rơi tự do ai cũng nhặt được, `1` = Thuộc về người gây sát thương cao nhất. |

---

## 9. CHI TIẾT BẢNG `Character.csv`

Bảng cấu hình các nhân vật người chơi mặc định của các môn phái. Cấu trúc cột tương tự `NpcTemplate.csv`, bổ sung:
* **`Sex`**: Giới tính (`1` = Nam, `2` = Nữ, `3` = Loli/Shota).
* **`Faction`**: ID môn phái tương ứng (`1`=Thiên Vương, `2`=Nga Mi, `3`=Đào Hoa...).
* **`StartActEvent`**: Chuỗi hoạt cảnh giới thiệu khi tạo nhân vật mới.

---

## 10. CHI TIẾT BẢNG `NpcAttribute.csv`

| Tên Cột | Kiểu | Ý nghĩa trong Công thức Tính Dame |
| :--- | :---: | :--- |
| **`AttribID`** | `int` | ID bảng chỉ số (Khóa chính, trỏ từ `NpcTemplate` hoặc `Character`). |
| **`MaxLife`** | `int` | Sinh lực tối đa (HP Max). |
| **`MinBaseAttack`** | `int` | Sát thương tấn công cơ bản tối thiểu (Min Physical Attack). |
| **`MaxBaseAttack`** | `int` | Sát thương tấn công cơ bản tối đa (Max Physical Attack). |
| **`HitRate`** | `int` | Điểm chính xác (Tăng tỉ lệ đánh trúng). |
| **`Miss`** | `int` | Điểm né tránh (Tăng tỉ lệ đối phương đánh trượt). |
| **`DeadlyStrike`** | `int` | Điểm chí mạng / Bạo kích (Tăng tỉ lệ nổ sát thương $\times 1.8$). |
| **`MetalDamage..EarthDamage`** | `int` | Điểm sát thương thuộc tính **Ngũ Hành** (Kim, Mộc, Thủy, Hỏa, Thổ). |
| **`MetalResist..EarthResist`** | `int` | Kháng sát thương thuộc tính **Ngũ Hành** (% giảm sát thương). |
| **`AttackSpeed`** | `int` | Tốc độ xuất chiêu (% gia tăng tốc độ animation: `Speed = BaseSpeed * (1 + AttackSpeed / 100.0f)`). |

---

## 11. CHI TIẾT BẢNG `EffectRes.csv`

| Tên Cột | Kiểu | Ý nghĩa & Cơ chế Override Thần Binh |
| :--- | :---: | :--- |
| **`ResID`** | `int` | Mã số định danh của Effect (Khóa chính). |
| **`ResFilePath`** | `string` | **Đường dẫn Prefab hiệu ứng tiêu chuẩn** (vd: `Effect/Prefabs/JueSe/emei/JN_01.prefab`). |
| **`LowResFilePath`**| `string` | Đường dẫn Prefab hiệu ứng rút gọn tối ưu cho thiết bị cấu hình yếu. |
| **`WeaponEffectSkillPath1`**| `string` | **Prefab thay thế độ ưu tiên cao khi mang Vũ Khí Thần Binh / Phát Sáng** (vd: tự động đổi sang `Effect/Prefabs/JueSe/emei/emei_C/JN_01_C.prefab`). |
| **`LockRotate`** | `int (0/1)`| `1` = **Khóa góc xoay phẳng mặt phẳng OXZ** (Flat Ground Mode), không bị chao đảo theo xương nhân vật. |

---

## 12. CHI TIẾT BẢNG `StateEffect.csv`, `PartSlot.csv` & QUẢN LÝ KHỚP GẮN VFX

### 🏛️ 1. Bảng Tra Cứu Toàn Bộ Khớp Xương (`PartSlot.csv`) & Chế Độ Xoay:

| `SlotId` | Tên Khớp Xương (`SlotName`) | Vị trí mô tả (`Des`) | Chế độ Xoay (Rotation Mode) | Ứng dụng thực tế |
| :---: | :--- | :--- | :---: | :--- |
| **`1` / `17`** | **`B_RH` / `Bip01 R Hand`** | Bàn tay phải / Kiếm | 🟢 **Follow Bone** (Xoay 100% theo tay) | Kiếm phát sáng, tụ lực vũ khí `_WQ`, vệt chém. |
| **`2` / `18`** | **`B_LH` / `Bip01 L Hand`** | Bàn tay trái / Khiên / Cung | 🟢 **Follow Bone** (Xoay 100% theo tay) | Chưởng pháp, nạp tên, khiên năng lượng tay trái. |
| **`3`** | **`B_Spine2`** | Đục lỗ cánh / Xương sống trên | 🟢 **Follow Bone** (Xoay theo lưng) | Cánh ngoại trang bay (Wings). |
| **`4` / `5`** | **`Bip001` / `Bip01`** | Căn cốt gốc nhân vật | 🔒 **Lock Pitch & Roll** | Trọng tâm cơ thể. |
| **`6`** | **`back`** | Lỗ lưng / Sau lưng | 🟢 **Follow Bone** | Phi phong, cánh, hồ lô sau lưng. |
| **`7`** | **`Bip01 Spine1`** | Lỗ đai cơ thể / Ngực | 🔒 **Lock Pitch & Roll** (Chỉ xoay trục Y) | Hào quang hộ thể quanh người (Shield), hồi máu. |
| **`8`** | **`Bone001`** | Lỗ bên tay phải | 🟢 **Follow Bone** | Khớp phụ trợ tay phải. |
| **`11` / `12`** | **`S_RH` / `S_LH`** | NPC tay phải / tay trái | 🟢 **Follow Bone** | Hiệu ứng tay cho Quái/Boss. |
| **`13` / `14`** | **`S_RH_01` / `S_LH_01`** | NPC chân phải / chân trái | 🟢 **Follow Bone** | Vết lửa chân quái, đá chân. |
| **`15` / `16`** | **`S_Hat` / `S_HAT_01`** | NPC đỉnh đầu (Dummy Anchor) | 🔒 **Lock Entire / Billboard** | Icon Buff, Stun choáng (sao bay), Câm lặng. |
| **`19` / `20`** | **`Bip01 R/L Foot`** | Chân phải / Chân trái | 🔒 **Flat Ground (Khóa phẳng Oxz)** | Vòng sáng trận pháp dưới chân, đài sen hồi máu. |
| **`21`** | **`Bip001 Head`** | Đỉnh đầu nhân vật | 🟢 **Follow Bone** | Mặt nạ, mắt phát sáng, nón đội. |
| **`22`** | **`Bip01 Pelvis`** | Hông / Xương chậu | 🔒 **Lock Pitch & Roll** | Hào quang tỏa tròn quanh hông. |
| **`152..154`**| **`B_Hs` / `B_Hs001..002`**| Yên thú cưỡi (Trước / Sau) | 🟢 **Follow Mount** | Hiệu ứng bước chân ngựa, yên cương thú cưỡi. |
| **`155 / 156`**| **`Bone033` (L/R)** | Thần cơ ưng (Trái / Phải) | 🟢 **Follow Bone** | Hiệu ứng phái Thần Cơ bay lượn. |

---

### 📦 2. Cấu Trúc Bảng Buff/Debuff `StateEffect.csv`:
* **Hỗ trợ 2 Socket VFX đồng thời:** `EffectResID1` gắn vào `SlotID1`, `EffectResID2` gắn vào `SlotID2`.
* **Neo VFX đỉnh đầu riêng:** `HeadResID` (Tự động gắn vào Slot đỉnh đầu `15/16/21`).
* **Hiệu ứng Phóng to / Thu nhỏ nhân vật:** `ChangeSize` (Tỉ lệ phóng to, ví dụ `1.5f`) và `ChangeSizeSpeed` (Tốc độ biến hình).

---

### 💻 3. Code C# Quản Lý Gắn & Xoay VFX Chuẩn Cho Unity:

```csharp
using UnityEngine;

public class EffectAttachmentController : MonoBehaviour
{
    public static GameObject AttachEffect(
        GameObject effectPrefab, 
        Transform characterTransform, 
        Transform targetBone, 
        int slotId, 
        bool isLockRotateFromCsv)
    {
        if (effectPrefab == null) return null;
        GameObject effectObj = Object.Instantiate(effectPrefab);

        // 1. Nhóm xoay 100% theo xương (Follow Bone Socket)
        if (!isLockRotateFromCsv && (slotId == 1 || slotId == 2 || slotId == 3 || slotId == 6 || slotId == 8 || slotId == 21))
        {
            effectObj.transform.SetParent(targetBone != null ? targetBone : characterTransform);
            effectObj.transform.localPosition = Vector3.zero;
            effectObj.transform.localRotation = Quaternion.identity;
            return effectObj;
        }

        // 2. Nhóm khóa xoay phẳng mặt đất (LockRotate = 1 hoặc chân 19/20)
        if (isLockRotateFromCsv || slotId == 19 || slotId == 20)
        {
            var groundFollower = effectObj.AddComponent<FlatGroundFollower>();
            groundFollower.Init(characterTransform, targetBone);
            return effectObj;
        }

        // 3. Nhóm Buff thân / Trọng tâm ngực / Đỉnh đầu (Khóa Pitch & Roll, giữ trục thẳng đứng Vector3.up)
        var bodyFollower = effectObj.AddComponent<UprightBodyFollower>();
        bodyFollower.Init(characterTransform, targetBone);
        return effectObj;
    }
}

public class FlatGroundFollower : MonoBehaviour
{
    private Transform _caster;
    public void Init(Transform caster, Transform bone) => _caster = caster;
    private void LateUpdate()
    {
        if (_caster == null) { Destroy(gameObject); return; }
        transform.position = _caster.position;
        transform.rotation = Quaternion.Euler(0, _caster.eulerAngles.y, 0); // Giữ phẳng Oxz
    }
}

public class UprightBodyFollower : MonoBehaviour
{
    private Transform _caster;
    private Transform _bone;
    public void Init(Transform caster, Transform bone) { _caster = caster; _bone = bone; }
    private void LateUpdate()
    {
        if (_caster == null) { Destroy(gameObject); return; }
        transform.position = _bone != null ? _bone.position : _caster.position;
        transform.rotation = Quaternion.Euler(0, _caster.eulerAngles.y, 0); // Khóa gập lưng Pitch/Roll
    }
}
```

---

## 13. CHI TIẾT BẢNG `FactionSkill.csv` & `AutoAiSkill.csv`

* **`FactionSkill.csv`**: Định vị vị trí nút bấm chiêu trên giao diện UI:
  * `BtnName`: Tên slot nút (`Attack`, `Skill1`, `Skill2`, `Skill3`, `Skill4`, `Skill_Dodge`).
  * `IsAnger`: `1` = Nút Tuyệt kỹ Nộ (Ulti).
  * `GainLevel`: Cấp độ nhân vật mở khóa chiêu.
* **`AutoAiSkill.csv`**: Điều kiện AI tự động tung chiêu:
  * `LiftPercent`: Chỉ tung chiêu khi máu đối thủ hoặc bản thân $\le X\%$.
  * `Selector`: Ưu tiên chọn mục tiêu (Máu thấp nhất / Gần nhất / Đông nhất).

---

## 14. CHI TIẾT BẢNG `Sound.csv`

* **`SoundID`**: Mã số âm thanh (Khóa chính).
* **`Bank`**: Tên gói SoundBank Wwise (`Em` = Nga Mi, `Shaolin`, `Tianwang`...).
* **`Sound`**: Tên Event Audio (vd: `Play_Em_01_01`, `Play_Em_02_01`, `Play_Em_01_Hit`).

---

## 15. TỔNG HỢP TOÀN BỘ BẢNG MÃ ENUM CHUẨN C# CHO UNITY

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

    // Phân loại kỹ năng trong Skill.csv
    public enum SkillCategoryType
    {
        Passive = 1,        // Bị động cơ bản
        MonsterNormal = 2,   // Đòn đánh thường của quái / Sub-skill
        SupportBuff = 3,    // Buff hỗ trợ / Bị động môn phái
        AuraArray = 4,      // Trận pháp hào quang
        ActivePlayer = 5    // Kỹ năng chủ động điều khiển (Đánh thường combo & Chiêu thức)
    }

    // Dạng đạn đạo / Hình thái kỹ năng
    public enum MissileFormType
    {
        StraightLinear = 1, // Đạn bay thẳng bình thường
        SpreadFan = 2,      // Đạn bắn chùm hình quạt
        CircularRing = 3,   // Vòng tròn tỏa rộng quanh Caster
        ChainBouncing = 4,  // Đạn nảy bật liên hoàn giữa các mục tiêu
        SkyDrop = 5,        // Rơi từ trên trời xuống
        MultiMissileWave = 7// Chùm đa đạn đồng loạt / Sóng nước tỏa rộng (Skill 305, 310)
    }

    // Cơ chế di chuyển của đạn
    public enum MissileMoveKind
    {
        StaticTrap = 0,     // Đặt bẫy / Điểm hồi máu / Trận pháp cố định
        Linear = 1,         // Bay thẳng theo vector ban đầu
        HomingTracking = 2, // Tự bám đuổi / uốn lượn theo mục tiêu đang khóa
        DashWithCaster = 3, // Di chuyển dính liền theo thân người lướt
        BoomerangCurved = 5 // Bay uốn lượn / quay ngược trở về
    }

    // Hình dạng Hitbox quét va chạm trong Missile.csv
    public enum HitboxShape
    {
        SingleTarget = 0,   // Đơn mục tiêu (Raycast trúng 1 người)
        CircleSphere = 1    // Vùng tròn / Khối cầu (Bán kính = DmgRange / 10.0f mét)
    }

    // Vị trí xuất phát của Kỹ năng
    public enum SkillStartPosType
    {
        CasterOrigin = 1,   // Xuất phát từ người ra chiêu
        TargetPosition = 2, // Xuất phát tại / hướng tới mục tiêu
        HitPoint = 3        // Xuất phát tại điểm va chạm
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
        SpineUpper = 3,     // B_Spine2 (Xương sống trên / Cánh)
        BodyRoot = 4,       // Bip001 (Trọng tâm cơ thể 1)
        BodyRoot2 = 5,      // Bip01 (Trọng tâm cơ thể 2)
        Back = 6,           // back (Phi phong / Cánh sau lưng)
        ChestCenter = 7,    // Bip01 Spine1 (Ngực / Khiên hộ thể)
        RightHandAux = 8,   // Bone001 (Khớp phụ tay phải)
        NpcRightHand = 11,  // S_RH (NPC tay phải)
        NpcLeftHand = 12,   // S_LH (NPC tay trái)
        NpcRightFoot = 13,  // S_RH_01 (NPC chân phải)
        NpcLeftFoot = 14,   // S_LH_01 (NPC chân trái)
        HeadDummy = 15,     // S_Hat (Đỉnh đầu / Buff / Stun)
        HeadTopDummy = 16,  // S_HAT_01 (Đỉnh đầu NPC)
        RightHandBip = 17,  // Bip01 R Hand (Tay phải)
        LeftHandBip = 18,   // Bip01 L Hand (Tay trái)
        RightFoot = 19,     // Bip01 R Foot (Chân phải)
        LeftFoot = 20,      // Bip01 L Foot (Chân trái / Trận pháp đất)
        Head = 21,          // Bip001 Head (Đầu nhân vật)
        Pelvis = 22,        // Bip01 Pelvis (Hông / Xương chậu)
        MountBack = 152,    // B_Hs (Lưng thú cưỡi)
        MountFront = 153,   // B_Hs001 (Thú cưỡi đôi - trước)
        MountRear = 154,    // B_Hs002 (Thú cưỡi đôi - sau)
        EagleLeft = 155,    // Bone033 (Thần Cơ Ưng - Trái)
        EagleRight = 156    // Bone033 (Thần Cơ Ưng - Phải)
    }
}
```

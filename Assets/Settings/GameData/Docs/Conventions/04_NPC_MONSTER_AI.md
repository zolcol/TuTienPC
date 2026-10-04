# 👾 QUY CHUẨN DỮ LIỆU QUÁI VẬT, NPC, ATTRIBUTE & TRÍ TUỆ NHÂN TẠO (AI)

> **Tài liệu thành phần:** Nằm trong bộ quy chuẩn dữ liệu [DATA_CONVENTIONS_V2.md](../DATA_CONVENTIONS_V2.md).
> **Phạm vi:** Dữ liệu nhân vật & quái vật (NpcRes.csv, ActionName.csv, NpcTemplate.csv, Field_HeaderBoss.csv), thuộc tính chỉ số (NpcAttribute.csv, MagicDesc.csv) và cấu hình AI hành vi (CommonActive.ini, CommonPassive.ini).

---

## 12. CHI TIẾT BẢNG `NpcRes.csv`

| Tên Cột | Kiểu | Ý nghĩa trong Unity 3D |
| :--- | :---: | :--- |
| **`NpcResId`** | `int` | ID định danh tài nguyên Model 3D (Khóa chính). |
| **`NpcResFile`** | `string` | Đường dẫn file Model Prefab Unity (vd: `Player/Npcs/Prefabs/npc_289.prefab`). |
| **`Height`** | `float` | Chiều cao của nhân vật / quái $\rightarrow$ Gán cho `CapsuleCollider.height` (m). |
| **`Width`** | `float` | Bán kính thân $\rightarrow$ Gán cho `CapsuleCollider.radius` (m). |
| **`<Action>_frame`**| `int` | **Tổng số Frame chuẩn của clip đó tại 15 FPS** (vd: `at01_frame = 9`, `jn01_frame = 13`). |
| **`<Action>_cross`**| `float` | **Thời gian Blend hòa trộn khi chuyển sang clip đó** tính bằng giây (vd: `0.1s`). |

---

---

## 13. CHI TIẾT BẢNG `ActionName.csv`

Từ điển mã hóa tên viết tắt các Clip hoạt ảnh chuẩn trích từ [ActionName.tab](file:///C:/Users/zolcol/Desktop/Data/CSV/N/ActionName.csv) & [NpcDefine.lua](file:///C:/Users/zolcol/Desktop/Data/unpacked_data/CommonScript/Npc/NpcDefine.lua#L56-L74):

| ActId | Mã viết tắt | Tên đầy đủ | Ý nghĩa chuyển động & Hit Reaction |
| :---: | :--- | :--- | :--- |
| **1** | `st` | `Stand / Idle` | Đứng yên phi chiến đấu. |
| **2** | `run` | `Run` | Di chuyển / Chạy bộ. |
| **3** | `die` | `Death` | Chết gục tại chỗ (Normal Death). |
| **4** | `jt` | `Knockback` | Bị trúng đòn nặng đẩy lùi trượt chân. |
| **5** | `bat` | `Drag` | Bị kéo giật về phía trước. |
| **6** | `qg` | `Qinggong` | Khinh công cơ bản / Lướt. |
| **7** | `sta` | `Combat Ready` | Thủ thế sẵn sàng chiến đấu. |
| **8** | `bat` | `Float Reaction` | Động tác bị đánh bay lên không. |
| **9** | `bat` | `Hit Flinch` | Bị thương giật mình tại chỗ. |
| **10** | `wlk` | `Walk` | Đi bộ tản bộ. |
| **11** | `zx` | `Pre-sit` | Động tác hạ người chuẩn bị ngồi. |
| **12** | `zst` | `Sit Still` | Ngồi yên tĩnh. |
| **13..14** | `st01..st02` | `Special Idle` | Vị trí đứng tạo dáng đặc biệt phi chiến đấu. |
| **16..19** | `at01..at04` | `Attack 1..4` | Đòn đánh thường chuỗi combo từ 1 đến 4. |
| **20** | `jfd` | `Knockdown Death` | Bị đánh văng lên đập đất chết luôn. |
| **21..25** | `jn01..jn05` | `Skill 1..5` | Kỹ năng phái 1, 2, 3, 4 và Tuyệt kỹ Nộ. |
| **26** | `jf` | `Knockup` | Đánh bay tung lên không. |
| **27..28** | `jn02b / jn02a`| `Skill Sub-action` | Phân nhánh động tác kỹ năng 2. |
| **29..34** | `qg01..qg05` | `Multi-step Qinggong`| Chuỗi khinh công đa đoạn 1 đến 5. |
| **35** | `dz` | `Meditate` | Ngồi thiền / Đả tọa hồi phục. |
| **39** | `sit` | `Sit` | Ngồi thông thường. |
| **40** | `jn01a` | `Skill 1 Sub-action` | Phân nhánh động tác kỹ năng 1. |
| **49** | `zc` | `Costume Qinggong Wing` | Ngoại trang khinh công — giương cánh. |
| **50** | `hx` | `Costume Qinggong Glide` | Ngoại trang khinh công — lướt. |
| **51** | `hyst01` | `Special Idle Costume` | Vị trí đặc biệt phi chiến đấu (ngoại trang). |
| **55** | `jn06` | `Special Field Skill` | KN Đặc Biệt Giang Hồ — Tung Chiêu. |

---

---

## 14. CHI TIẾT BẢNG `NpcTemplate.csv` & `Field_HeaderBoss.csv` (Dữ Liệu Quái, Boss & Nhân Vật)

Bảng `NpcTemplate.csv` là bảng trung tâm chứa **toàn bộ** entity trong game: quái thường, Boss, NPC nhiệm vụ, nhân vật player, đồng hành, phân thân.

| Tên Cột | Kiểu | Ý nghĩa & Bằng chứng mã nguồn |
| :--- | :---: | :--- |
| **`TemplateID`** | `int` | ID mẫu NPC/Boss/Nhân vật (Khóa chính). |
| **`Name`** | `string` | Tên hiển thị trên đầu NPC. |
| **`Kind`** | `int` | **Phân loại NPC (`NpcDefine.lua`):**<br>• `-1`: None.<br>• `0`: Monster / Quái thường / Nhân vật player.<br>• `1`: Player / Clone người chơi.<br>• `2`: NPC đối thoại nhiệm vụ (`dialoger`).<br>• `3`: Đồng hành (`partner`).<br>• `4`: Câm lặng / NPC tĩnh / Cơ quan (`silencer`).<br>• `7`: Đồng hành dạng Baby/Pet.<br>• `8`: Phân thân / Ảnh.<br>• `9`: Loại entity đặc biệt khác. |
| **`Camp`** | `int` | Phe phái: `0`=Player, `1`=Monster (Thù địch), `2`=Trung lập, `3`=Tống, `4`=Kim. |
| **`NpcResID`** | `int` | ID ngoại hình 3D (Trỏ sang `NpcRes.csv`). |
| **`NpcAttribID`** | `int` | ID chỉ số máu, công, thủ, kháng (Trỏ sang `NpcAttribute.csv`). |
| **`NormalSkill1..3`**| `int` | ID các chiêu thức tấn công NPC sở hữu (Trỏ sang `Skill.csv`). |
| **`VisionRadius`** | `int` | Tầm nhìn phát hiện kẻ địch (`VisionRadius / 100.0f` mét). |
| **`ActiveRadius`** | `int` | Tầm truy đuổi tối đa trước khi tự quay về (Leash Range, cm). |
| **`AiFile`** | `string` | File kịch bản AI điều khiển hành vi (`Setting/Npc/Ai/...`). |
| **`RunSpeed` / `WalkSpeed`**| `int` | Tốc độ di chuyển (`runSpeed * 15.0f / 100.0f = m/s`). |
| **`ReviveFrame`** | `int` | Thời gian hồi sinh sau khi chết (`ReviveFrame / 15.0f` giây). |
| **`Sex`** | `int` | Giới tính: `1` = Nam, `2` = Nữ. |

> **Lọc nhân vật player:** Dùng cột `Index` với prefix `Character_*`, `newrole_*`, `Shadow_*`, `Baby_*` hoặc lọc theo `Kind IN (0, 7, 8, 9)` kết hợp `Camp = 1`.
> Thông tin môn phái tra qua **`FactionSkill.csv`** (cột `Faction`).

---

---

## 15. CHI TIẾT BẢNG `NpcAttribute.csv` & `MagicDesc.csv`

Toàn bộ tên thuộc tính ánh xạ chuẩn theo [MagicDesc.csv](file:///C:/Users/zolcol/Desktop/Data/CSV/N/MagicDesc.csv):

| Tên Cột trong CSV | Tên Thuộc Tính trong `MagicDesc` | Ý nghĩa trong Gameplay |
| :--- | :--- | :--- |
| **`MaxLife`** | `lifemax_v` | Sinh lực tối đa (HP Max). |
| **`MinBaseAttack` / `MaxBaseAttack`** | `basic_damage_v` | Sát thương tấn công cơ bản tối thiểu / tối đa. |
| **`HitRate`** | `hit_v` | Điểm chính xác. |
| **`Miss`** | `miss_v` | Điểm né tránh. |
| **`DeadlyStrike`** | `deadlystrike_v` | Điểm bạo kích (Chí mạng). |
| **`MetalDamage..EarthDamage`** | `physical_metaldamage_v`... | Điểm sát thương thuộc tính Kim, Mộc, Thủy, Hỏa, Thổ. |
| **`MetalResist..EarthResist`** | `metal_resist_v`... | Điểm kháng sát thương Ngũ Hành. |
| **`AttackSpeed`** | `attackspeed_v` | Điểm tốc độ xuất chiêu (Giảm frame theo công thức mục 6). |

---

---

## 21. HỆ THỐNG AI QUÁI CƠ BẢN (`CommonActive.ini` & `CommonPassive.ini`)

Hệ thống AI cho quái vật thông thường trong game hoạt động dựa trên sự kết hợp giữa **dữ liệu cấu hình thực thể (`NpcTemplate.csv`)** và **tập tin cấu hình hành vi (`CSV/N/AI/*.ini`)**.

### ⚙️ 1. Cấu Trúc Khối `[Base]` Trong File Cấu Hình AI INI:

Hai profile phổ biến nhất cho quái dã ngoại và phó bản là [`CommonActive.ini`](file:///C:/Users/zolcol/Desktop/Data/CSV/N/AI/CommonActive.ini) (Quái chủ động) và [`CommonPassive.ini`](file:///C:/Users/zolcol/Desktop/Data/CSV/N/AI/CommonPassive.ini) (Quái bị động):

| Tham Số INI | Kiểu | Ý nghĩa trong Gameplay | Giá trị `CommonActive` | Giá trị `CommonPassive` | Quy đổi Unity C# |
| :--- | :---: | :--- | :---: | :---: | :--- |
| **`Attack`** | `int` | **Trạng thái chủ động tấn công:**<br>• `1`: Chủ động phát hiện và tấn công kẻ địch khi vào tầm nhìn.<br>• `0`: Bị động, chỉ tấn công khi bị gây sát thương trước. | `1` | `0` | `bool isAggressive = Attack == 1;` |
| **`StrikeBack`** | `int` | **Phản đòn khi bị đánh:**<br>• `1`: Tự động khóa và đánh trả kẻ vừa gây sát thương.<br>• `0`: Phớt lờ sát thương hoặc bỏ chạy. | `1` | `1` | `bool canCounter = StrikeBack == 1;` |
| **`RandmonMove`** | `int` | **Xác suất đi lang thang (%)** xung quanh điểm xuất phát khi đang ở trạng thái nhàn rỗi (Idle). | `0` (Đứng canh tại chỗ) | `10` (10% cơ hội tản bộ) | `int wanderChance = RandmonMove;` |
| **`AiBreathTime`**| `int` | **Nhịp suy nghĩ (Tick Rate) của AI** tính bằng frame chuẩn **15 FPS**. | `15` ($1.0$ giây/lần) | `30` ($2.0$ giây/lần) | `float tickInterval = AiBreathTime / 15.0f;` |
| **`SelectTarget`**| `string` | **Thuật toán ưu tiên chọn mục tiêu:**<br>• `StrikeBack`: Ưu tiên đánh trả kẻ vừa đánh mình.<br>• `Nearest`: Ưu tiên mục tiêu gần nhất.<br>• `Poorest`: Ưu tiên mục tiêu ít máu nhất.<br>• `Richest`: Ưu tiên mục tiêu nhiều máu nhất.<br>• `Random`: Chọn ngẫu nhiên trong tầm nhìn.<br>• `Player`: Ưu tiên đánh người chơi thay vì pet. | `StrikeBack` | `StrikeBack` | Enum `AiTargetSelectType` |
| **`ChangeTargetTime`**| `int` | **Thời gian tối thiểu duy trì mục tiêu** (Frames) trước khi được phép chuyển sang mục tiêu khác (Chống đổi mục tiêu liên tục). | `60` ($4.0$ giây) | `60` ($4.0$ giây) | `float lockDuration = ChangeTargetTime / 15.0f;` |
| **`FleeHpPrecent`** | `int` | Ngưỡng % HP kích hoạt trạng thái bỏ chạy hoảng loạn (`0` = không bao giờ chạy). | `0` | `0` | `int fleeHpThreshold = FleeHpPrecent;` |
| **`FleeNearRate`** | `int` | Xác suất (%) chạy trốn khi bị kẻ địch áp sát quá gần. | `0` | `0` | `int fleeNearChance = FleeNearRate;` |

---

### 🗺️ 2. Các Trường Dữ Liệu Tương Tác Trong `NpcTemplate.csv`:

Mỗi quái vật đọc dữ liệu từ `NpcTemplate.csv` để xác định không gian hoạt động và kỹ năng tấn công:

* **`VisionRadius`**: Bán kính tầm nhìn phát hiện mục tiêu ($\div 100$ ra mét).
* **`ActiveRadius`**: Giới hạn vùng hoạt động / khoảng cách rượt đuổi tối đa từ điểm hồi sinh (Leash Range, $\div 100$ ra mét). Nếu mục tiêu chạy vượt quá cự ly này, quái sẽ tự động bỏ truy đuổi và quay về điểm ban đầu.
* **`NormalSkill1`**: ID kỹ năng đánh cơ bản (trỏ sang `Skill.csv` để lấy tầm xuất chiêu `AttackRadius` và thời gian hồi `TimePerCast`).
* **`RunSpeed`**: Tốc độ di chuyển khi rượt đuổi (`RunSpeed * 15.0f / 100.0f` m/s).
* **`ForbitMove`**: `1` = Quái dạng cọc gỗ / trụ phòng thủ cố định không di chuyển.

---

### 🔄 3. Máy Trạng Thái Hữu Hạn Quái Thường (Simple Monster FSM):

```
                     ┌──────────────────┐
                     │ 1. IDLE / PATROL │◄─────────────────────────┐
                     └────────┬─────────┘                          │
                              │ Thấy địch trong VisionRadius       │
                              │ (hoặc bị tấn công nếu StrikeBack)  │
                     ┌────────▼─────────┐                          │
                     │    2. CHASE      │                          │
                     └────────┬─────────┘                          │
                              │ Cự ly <= AttackRadius              │
                     ┌────────▼─────────┐                   ┌──────┴──────┐
                     │    3. ATTACK     │                   │ 5. RETURN / │
                     └────────┬─────────┘                   │   RESET     │
                              │ HP <= 0                     └──────▲──────┘
                     ┌────────▼─────────┐                          │
                     │    4. DEAD       │                          │
                     └──────────────────┘                          │
                              ▲                                    │
                              └────────────────────────────────────┘
                               Khoảng cách đến SpawnPos > ActiveRadius
```

* **Vòng lặp thực thi (Update Cycle):**
  1. **Idle / Patrol**: Mỗi `BreathTimeSec` kiểm tra va chạm kẻ địch trong `VisionRadius`. Nếu có $\rightarrow$ Chuyển sang **Chase**. Nếu không, thực hiện bước đi ngẫu nhiên theo `RandomMovePercent`.
  2. **Chase**: Tiếp cận mục tiêu theo vận tốc `RunSpeed`. Nếu khoảng cách từ vị trí hiện tại đến `SpawnPos` $> \text{ActiveRadius} \rightarrow$ Chuyển sang **Return**. Nếu cự ly đến mục tiêu $\le \text{AttackRadius} \rightarrow$ Chuyển sang **Attack**.
  3. **Attack**: Hướng mặt về mục tiêu, tung `NormalSkill1` theo chu kỳ `TimePerCast / 15.0f` giây. Nếu mục tiêu di chuyển ra ngoài `AttackRadius` $\rightarrow$ Tiếp tục **Chase**.
  4. **Return / Reset**: Bỏ mục tiêu, quay về `SpawnPos`. Miễn nhiễm sát thương và hồi phục toàn bộ HP khi đang trên đường quay về. Khi tới nơi $\rightarrow$ Chuyển về **Idle**.
  5. **Dead**: Chạy hoạt ảnh chết (`Die`), rớt đồ (`DropFile`), đợi `ReviveFrame / 15.0f` giây để tái sinh.

# ✨ QUY CHUẨN HIỆU ỨNG HÌNH ẢNH (VFX), KHỚP GẮN XƯƠNG & ÂM THANH

> **Tài liệu thành phần:** Nằm trong bộ quy chuẩn dữ liệu [DATA_CONVENTIONS_V2.md](../DATA_CONVENTIONS_V2.md).
> **Phạm vi:** Quản lý tài nguyên VFX (EffectRes.csv, StateEffect.csv), cơ chế khóa trục xoay & khớp xương (PartSlot.csv), và cấu hình âm thanh Wwise (Sound.csv - xem thêm [AUDIO_MAPPING_RULES.md](../AUDIO_MAPPING_RULES.md)).

---

## 16. CHI TIẾT BẢNG `EffectRes.csv` (GỐC `EffectRes.tab`)

| Tên Cột | Kiểu | Ý nghĩa & Cơ chế Override Thần Binh |
| :--- | :---: | :--- |
| **`ResID`** | `int` | Mã số định danh của Effect (Khóa chính). |
| **`ResFilePath`** | `string` | **Đường dẫn Prefab hiệu ứng tiêu chuẩn** (vd: `Effect/Prefabs/JueSe/emei/JN_01.prefab`). |
| **`LowResFilePath`**| `string` | Đường dẫn Prefab hiệu ứng rút gọn tối ưu cấu hình yếu. |
| **`WeaponEffectSkillPath1`**| `string` | **Prefab thay thế khi mang Vũ Khí Thần Binh / Phát Sáng**. |
| **`LockRotate`** | `int (0/1)`| `1` = **Khóa góc xoay phẳng mặt phẳng OXZ** (Flat Ground Mode). |

---

## 17. CHI TIẾT BẢNG `StateEffect.csv` (GỐC `StateEffect.tab`)

Bảng quy định hiển thị hiệu ứng buff/debuff, hào quang, biến hình và biểu tượng trên đầu nhân vật:

| Tên Cột | Kiểu | Ý nghĩa & Vận hành trong Gameplay |
| :--- | :---: | :--- |
| **`StateEffectId`** | `int` | ID hiệu ứng trạng thái (Khóa chính, trỏ từ `Skill.csv` cột `StateEffectId`). |
| **`StateName`** | `string` | Tên trạng thái / Buff (Tiếng Việt). |
| **`EffectResID1`** | `int` | ID Prefab VFX chính 1 (Trỏ sang `EffectRes.csv`). |
| **`SlotID1`** | `int` | Vị trí khớp xương gắn `EffectResID1` (Trỏ sang `PartSlot.csv`). |
| **`EffectResID2`** | `int` | ID Prefab VFX phụ 2 (Trỏ sang `EffectRes.csv`). |
| **`SlotID2`** | `int` | Vị trí khớp xương gắn `EffectResID2` (Trỏ sang `PartSlot.csv`). |
| **`HeadResID`** | `int` | ID Sprite/Icon hiển thị trên đỉnh đầu nhân vật (Buff/Stun icon). |
| **`Alpha`** | `int` | Độ trong suốt làm mờ nhân vật ($Alpha / 1000.0f$). |
| **`ChangeSize`** | `int` | Tỉ lệ phóng to/thu nhỏ Model nhân vật (%) khi dính Buff (vd: $120\%$). |
| **`ChangeSizeSpeed`**| `int` | Tốc độ phóng to/thu nhỏ Model. |
| **`RunActID`** | `int` | Hoạt ảnh chạy đặc biệt thay thế khi đang có Buff này. |
| **`HeadWord`** | `string` | Chữ hiển thị nổi trên đầu khi dính hiệu ứng ("Định", "Choáng", "Thuẫn", "Ngự"...). |
| **`Icon` / `IconAtlas`** | `string` | Icon và Sprite Atlas hiển thị trên thanh Buff UI người chơi. |
| **`HideBody` / `HideHead`**| `int (0/1)`| `1` = Ẩn hoàn toàn thân hoặc đầu nhân vật (Tàng hình / Biến thể). |
| **`HightLightSkill`** | `int` | ID ô kỹ năng viền sáng vàng trên HUD khi Buff này kích hoạt. |
| **`RequireSuperpose`**| `int` | Số tầng cộng dồn tối thiểu để bắt đầu hiển thị VFX. |

---

## 18. CHI TIẾT BẢNG `PartSlot.csv` & QUẢN LÝ KHỚP GẮN VFX

### 🏛️ 1. Phân Bổ Dải Khớp Xương Chuẩn ([PartSlot.tab](file:///C:/Users/zolcol/Desktop/Data/unpacked_data/Setting/Npc/Res/PartSlot.tab) & [`NpcPartSlotID.cs`](file:///D:/Export%20VLTK/Project/ExportedProject/Assets/Scripts/Assembly-CSharp/NpcPartSlotID.cs)):
* **`SlotId 1 ~ 48`**: Khớp đục lỗ trên cơ thể nhân vật (`slot_body`).
* **`SlotId 50 ~ 99`**: Khớp đục lỗ trên cánh / phi phong (`slot_wing`).
* **`SlotId 100 ~ 150`**: Khớp đục lỗ trên vũ khí (`slot_weapon`).
* **`SlotId 151 ~ 200`**: Khớp đục lỗ trên thú cưỡi / ngựa (`slot_horse`).

### 📋 2. Bảng Tra Cứu Khớp Xương Chi Tiết:

| `SlotId` | Tên Khớp Xương (`SlotName`) | Vị trí mô tả | Chế độ Xoay (Rotation Mode) |
| :---: | :--- | :--- | :---: |
| **`1` / `17`** | **`B_RH` / `Bip01 R Hand`** | Bàn tay phải / Kiếm | 🟢 **Follow Bone** (Xoay theo tay) |
| **`2` / `18`** | **`B_LH` / `Bip01 L Hand`** | Bàn tay trái / Cung / Khiên | 🟢 **Follow Bone** |
| **`3`** | **`B_Spine2`** | Xương sống trên / Lỗ cánh | 🟢 **Follow Bone** |
| **`4` / `5`** | **`Bip001` / `Bip01`** | Căn cốt gốc nhân vật | 🔒 **Lock Pitch & Roll** |
| **`6`** | **`back`** | Sau lưng / Phi phong | 🟢 **Follow Bone** |
| **`7`** | **`Bip01 Spine1`** | Ngực / Khiên hộ thể | 🔒 **Lock Pitch & Roll** (Chỉ xoay trục Y) |
| **`8`** | **`Bone001`** | Bàn tay phải (biến thể NPC) | 🟢 **Follow Bone** |
| **`11` / `12`** | **`S_RH` / `S_LH`** | NPC tay phải / tay trái | 🟢 **Follow Bone** |
| **`13` / `14`** | **`S_RH_01` / `S_LH_01`** | NPC chân phải / chân trái | 🟢 **Follow Bone** |
| **`15` / `16`** | **`S_Hat` / `S_HAT_01`** | Đỉnh đầu (Icon Buff / Stun) | 🔒 **Billboard / Lock Entire** |
| **`19` / `20`** | **`Bip01 R/L Foot`** | Chân phải / Chân trái | 🔒 **Flat Ground (Khóa phẳng Oxz)** |
| **`21`** | **`Bip001 Head`** | Đầu nhân vật | 🟢 **Follow Bone** |
| **`22`** | **`Bip01 Pelvis`** | Hông / Xương chậu | 🔒 **Lock Pitch & Roll** |
| **`152`** | **`B_Hs`** | Lưng ngựa / thú cưỡi (gốc) | 🟢 **Follow Bone** |
| **`153`** | **`B_Hs001`** | Thú cưỡi đuôi — trước | 🟢 **Follow Bone** |
| **`154`** | **`B_Hs002`** | Thú cưỡi đuôi — sau | 🟢 **Follow Bone** |
| **`155`** | **`Bone033`** | Thân cổ ứng — trái | 🟢 **Follow Bone** |
| **`156`** | **`Bone033(mirrored)`** | Thân cổ ứng — phải | 🟢 **Follow Bone** |

---

## 19. CHI TIẾT BẢNG `Sound.csv` (GỐC `Sound.tab`) & ÂM THANH

| Tên Cột | Kiểu | Ý nghĩa trong Audio System |
| :--- | :---: | :--- |
| **`SoundID`** | `int` | ID âm thanh duy nhất (Khóa chính). |
| **`Desc`** | `string` | Mô tả ngữ cảnh phát âm thanh. |
| **`Bank`** | `string` | Tên gói Wwise SoundBank chứa audio track (vd: `Em`, `Th`, `Common`, `Npc`). |
| **`Sound`** | `string` | Tên Wwise Event phát âm thanh (vd: `Play_Em_01_01`, `Play_Em_05_Hit`). |

### 🔗 Ánh xạ Sound ID từ các bảng dữ liệu khác:
* **`Skill.csv`**: `CastSoundID` $\rightarrow$ Phát âm thanh lúc người chơi bấm xuất chiêu.
* **`Missile.csv`**:
  * `FlySoundID`: Âm thanh rít gió tuần hoàn khi đạn đang bay.
  * `CollSoundID`: Âm thanh nổ va chạm khi trúng đích.
  * `VanishSoundID`: Âm thanh khi đạn tan biến.
* **`ActionEvent.csv`**: `PlaySound` / `StopSound` $\rightarrow$ Phát/Dừng âm thanh tại frame chính xác trên dòng thời gian hoạt ảnh.
* **`NpcRes.csv`**: `RunSoundID`, `DeathSoundID`, `HitSoundID` $\rightarrow$ Âm thanh bước chân, tiếng gầm khi chết và tiếng kêu bị thương.

* **Quy tắc trích xuất file `.wav` từ Wwise Event:** Xem chi tiết tại [AUDIO_MAPPING_RULES.md](../AUDIO_MAPPING_RULES.md).

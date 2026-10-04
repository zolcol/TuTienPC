# ✨ QUY CHUẨN HIỆU ỨNG HÌNH ẢNH (VFX), KHỚP GẮN XƯƠNG & ÂM THANH

> **Tài liệu thành phần:** Nằm trong bộ quy chuẩn dữ liệu [DATA_CONVENTIONS_V2.md](../DATA_CONVENTIONS_V2.md).
> **Phạm vi:** Quản lý tài nguyên VFX (EffectRes.csv, StateEffect.csv), cơ chế khóa trục xoay & khớp xương (PartSlot.csv), và cấu hình âm thanh Wwise (Sound.csv - xem thêm [AUDIO_MAPPING_RULES.md](../AUDIO_MAPPING_RULES.md)).

---

## 16. CHI TIẾT BẢNG `EffectRes.csv`

| Tên Cột | Kiểu | Ý nghĩa & Cơ chế Override Thần Binh |
| :--- | :---: | :--- |
| **`ResID`** | `int` | Mã số định danh của Effect (Khóa chính). |
| **`ResFilePath`** | `string` | **Đường dẫn Prefab hiệu ứng tiêu chuẩn** (vd: `Effect/Prefabs/JueSe/emei/JN_01.prefab`). |
| **`LowResFilePath`**| `string` | Đường dẫn Prefab hiệu ứng rút gọn tối ưu cấu hình yếu. |
| **`WeaponEffectSkillPath1`**| `string` | **Prefab thay thế khi mang Vũ Khí Thần Binh / Phát Sáng**. |
| **`LockRotate`** | `int (0/1)`| `1` = **Khóa góc xoay phẳng mặt phẳng OXZ** (Flat Ground Mode). |

---

---

## 17. CHI TIẾT BẢNG `StateEffect.csv`, `PartSlot.csv` & QUẢN LÝ KHỚP GẮN VFX

### 🏛️ 1. Bảng Tra Cứu Khớp Xương Gốc ([PartSlot.csv](file:///C:/Users/zolcol/Desktop/Data/CSV/N/PartSlot.csv)):

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

---

## 3. CHI TIẾT BẢNG Sound.csv & ÂM THANH



* **Quy tắc ánh xạ âm thanh chuyên sâu:** Xem thêm tại [AUDIO_MAPPING_RULES.md](../AUDIO_MAPPING_RULES.md).

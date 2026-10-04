# 📈 HỆ THỐNG CẤP ĐỘ, KINH NGHIỆM (EXP) & MA TRẬN PHẦN THƯỞNG

> **Tài liệu thành phần:** Nằm trong bộ quy chuẩn dữ liệu [DATA_CONVENTIONS_V2.md](../DATA_CONVENTIONS_V2.md).
> **Phạm vi:** Cột mốc cấp độ người chơi (PlayerLevel.csv), ma trận tỉ lệ EXP khi đánh quái chênh cấp (ExpRule.csv), và thuật toán nội suy tính EXP thưởng.

---

## 20. HỆ THỐNG CẤP ĐỘ, KINH NGHIỆM NGƯỜI CHƠI & CƠ CHẾ EXP QUÁI RƠI (`PlayerLevel.csv` & `ExpRule.csv`)

Hệ thống cấp độ và kinh nghiệm (EXP) trong game được thiết kế dựa trên 2 bảng dữ liệu cốt lõi:

### 📊 1. Bảng `PlayerLevel.csv` (Cột Mốc Cấp Độ & EXP Lên Cấp):
Bảng quy định mức kinh nghiệm cần để thăng cấp của nhân vật và đơn vị kinh nghiệm chuẩn (`BaseAwardExp`):

| Tên Cột | Kiểu | Ý nghĩa trong Gameplay & Bằng chứng mã nguồn |
| :--- | :---: | :--- |
| **`Level`** | `int` | Cấp độ nhân vật người chơi ($1 \sim 390+$). |
| **`ExpUpGrade`** | `long` / `int` | **Tổng điểm kinh nghiệm cần đạt để thăng cấp tiếp theo** (từ `Level` $\rightarrow$ `Level + 1`). |
| **`BaseAwardExp`** | `int` | **Điểm EXP mốc cơ bản tại cấp độ hiện tại**. Được dùng làm đơn vị chuẩn nhân thưởng cho mọi hoạt động (đánh quái, nhiệm vụ, lửa trại, phó bản). |
| **`FightPower`** | `int` | Điểm lực chiến cơ bản cộng thêm khi đạt cấp độ này. |
| **`AttackSeriesResist`** | `int` | Điểm kháng ngũ hành cơ bản nhân vật nhận được theo cấp. |
| **`RunSpeed` / `AttackSpeed`** | `int` | Override tốc độ chạy / tốc độ đánh cơ bản (mặc định `0` = giữ nguyên). |

---

### ⚔️ 2. Bảng Ma Trận `ExpRule.csv` & Cơ Chế EXP Quái Rơi:
Quái vật **không lưu con số EXP cố định** trong `NpcTemplate.csv` hay `NpcAttribute.csv`. Khi người chơi tiêu diệt quái vật, lượng EXP nhận được tính theo công thức:

$$\text{EXP Thực Nhận} = \text{Player.BaseAwardExp} \times \frac{\text{ExpRule}[\text{PlayerLevel}, \text{MonsterLevel}]}{100}$$

#### 🎯 Nguyên Lý Vận Hành Ma Trận `ExpRule.csv`:
* **Hàng (Dòng đầu tiên của mỗi record):** Cấp độ Người Chơi (`PlayerLevel` $1 \sim 400$).
* **Cột (Header):** Cấp độ Quái Vật (`MonsterLevel` $0 \sim 400$).
* **Giá trị ô:** Tỉ lệ phần trăm ($\%$) kinh nghiệm người chơi được hưởng:
  - **Ngang hoặc chênh lệch ít ($\pm 0 \sim 5$ cấp):** Hưởng trọn $100\%$ EXP chuẩn.
  - **Đánh quái cấp thấp hơn nhiều:** Tỉ lệ giảm dần ($90\% \rightarrow 80\% \rightarrow 60\% \rightarrow 50\% \rightarrow 40\% \rightarrow 20\%$) nhằm chống lạm dụng farm quái cấp thấp.

---

### 💻 3. Cấu Trúc Struct Unity C# Quản Lý Level & Exp:

```csharp
namespace GameData
{
    [Serializable]
    public class PlayerLevelData
    {
        public int Level;
        public long ExpUpGrade;
        public int BaseAwardExp;
        public int FightPower;
        public int AttackSeriesResist;
    }

    public static class ExpCalculator
    {
        // Tính toán EXP khi tiêu diệt quái
        public static long CalculateMonsterExp(int playerLevel, int monsterLevel, int baseAwardExp, int[,] expRuleMatrix)
        {
            if (playerLevel < 1 || monsterLevel < 0) return 0;
            int percent = expRuleMatrix[playerLevel, monsterLevel];
            return (long)(baseAwardExp * (percent / 100.0f));
        }
    }
}
```

---

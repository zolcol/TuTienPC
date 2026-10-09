# Quy Tắc Ánh Xạ Âm Thanh: Sound.csv -> File Audio (.wav)

Tên trong cột `Sound` là **Wwise Event Name**, còn file trích xuất trong `Audio/output_wav/{Bank}` là **Audio Track gốc**.

---

## 1. Công thức cốt lõi

> **File `.wav` = Cắt bỏ tiền tố `Play_` từ tên Event**

---

## 2. Các trường hợp chi tiết

| Loại âm thanh | Cột `Sound` (CSV) | Tên file thực tế (`.wav`) | Ví dụ |
| :--- | :--- | :--- | :--- |
| **Chiêu thức (Skill)** | `Play_{Phái}_{Chiêu}_{Thức}` | `{Phái}_{Chiêu}_{Thức}.wav` | `Play_Em_01_01` &rarr; `Em_01_01.wav`<br>`Play_Th_02_01` &rarr; `Th_02_01.wav` |
| **Đăng trường & Nộ khí** | `Play_{Phái}_Dc`<br>`Play_{Phái}_00` | `{Phái}_Dc.wav`<br>`{Phái}_00.wav` | `Play_Em_Dc` &rarr; `Em_Dc.wav`<br>`Play_Sl_00` &rarr; `Sl_00.wav` |
| **Trúng đòn (Hit)** | `Play_{Phái}_{Chiêu}_Hit` | Khớp 1 trong 3 dạng:<br>&bull; `{Phái}_{Chiêu}_Hit.wav`<br>&bull; `{Phái}_{Chiêu}_Hit0x.wav`<br>&bull; `{Phái}_Hit_0x.wav` | `Play_Th_03_Hit` &rarr; `Th_03_Hit.wav`<br>`Play_Sl_01_Hit` &rarr; `Sl_01_Hit01.wav`, `Sl_01_Hit02.wav`<br>`Play_Em_01_Hit` &rarr; `Em_Hit_01.wav`, `Em_Hit_02.wav` |
| **Bản cập nhật (Remake)** | `Play_{Phái}_...` | `{Phái}_..._new.wav` | `Play_Wd_01_01` &rarr; `Wd_01_01_new.wav` |
| **Voice nhân vật (Lồng tiếng)** | *(Đi kèm trong bank)* | `{Phái}_Vo_{Mã}.wav` | `Em_Vo_00.wav`, `Sl_Vo_14a.wav` |
| **Âm thanh theo Giới tính (Gender)** | `Play_{Mã}_{Female/Male}` | Phân rã 2 phần phát đồng thời:<br>&bull; **Base SFX:** `{Mã}.wav` (hoặc `a~f`)<br>&bull; **Voice:** `{Mã}_Vo{Female/Male}.wav` | `Play_DS_03_01_Female` &rarr;<br>1. Base: `DS_03_01.wav`<br>2. Voice: `DS_03_01_VoFemale.wav`<br>`Play_DS_02_01_Male` &rarr;<br>1. Base: `DS_02_01a/b.wav`<br>2. Voice: `DS_02_01_VoMale.wav` |

---

## 3. Khi nào xảy ra cơ chế Random (Nhiều file cho 1 Event)?

Wwise sử dụng **Random Container** (1 Event phát ngẫu nhiên nhiều biến thể để âm thanh không bị lặp nhàm chán) chủ yếu trong 2 trường hợp:

1. **Âm thanh trúng đòn (`Hit`)**:
   - Khi đánh trúng mục tiêu liên tục, game gọi 1 event `..._Hit` nhưng random các file đuôi `01, 02, 03...` hoặc `Hit01, Hit02...` để tạo cảm giác đánh tự nhiên.
   - *Ví dụ:* `Play_Em_05_Hit` &rarr; Random 1 trong 5 file `Em_05_Hit01.wav` đến `Em_05_Hit05.wav`.
2. **Voice nhân vật (`Vo`)**:
   - Các file có hậu tố chữ cái/số như `a, b, c...` (ví dụ `Sl_Vo_14a.wav`, `Sl_Vo_14b.wav`) là các câu thoại/tiếng thét khác nhau cho cùng một động tác xuất chiêu.

---

## 4. Ví dụ tra cứu mẫu

### Ví dụ 1: Phái Nga Mi (`Bank: Em`)
* Event `Play_Em_01_01` &rarr; File `Em_01_01.wav`
* Event `Play_Em_05_Hit` &rarr; Tập hợp file `Em_05_Hit01.wav` ... `Em_05_Hit05.wav`

### Ví dụ 2: Phái Đào Hoa (`Bank: Th`)
* Event `Play_Th_04_01` &rarr; File `Th_04_01.wav`
* Event `Play_Th_Dc` &rarr; File `Th_Dc.wav`

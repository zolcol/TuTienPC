# 🧭 TÀI LIỆU QUY ƯỚC & NGUYÊN LÝ DỮ LIỆU GAME (DATA CONVENTIONS & SYSTEM ARCHITECTURE)

> **Mục tiêu tài liệu:** Đây là **Master Hub** đóng vai trò bản đồ kiến trúc dữ liệu tổng quan cho toàn bộ dự án Kiếm Hiệp 3D Action RPG (chuẩn Kingsoft/JX). Toàn bộ quy chuẩn chi tiết đã được chia nhỏ thành các module chuyên biệt dưới thư mục [Conventions/](file:///D:/Unity%20Project/test1/Assets/Settings/GameData/Docs/Conventions/) để tiện tra cứu và tránh tải quá nhiều context.

---

## 📑 BẢN ĐỒ QUY CHUẨN THEO MODULE (COMPONENT SPECIFICATIONS)

| Module | Tập tin chi tiết | Bảng dữ liệu / File liên quan | Trách nhiệm chính |
| :--- | :--- | :--- | :--- |
| **01** | [**01_CORE_STANDARDS.md**](file:///D:/Unity%20Project/test1/Assets/Settings/GameData/Docs/Conventions/01_CORE_STANDARDS.md) | `CombatFormula.cs`, `CombatEnums.cs` | Quy chuẩn đơn vị đo ($1\text{m} = 100$), Frame Timing 15 FPS, công thức Attack Speed và toàn bộ Enums & Structs C# chuẩn. |
| **02** | [**02_SKILLS_AND_MISSILES.md**](file:///D:/Unity%20Project/test1/Assets/Settings/GameData/Docs/Conventions/02_SKILLS_AND_MISSILES.md) | `Skill.csv`, `Missile.csv`, `ActionEvent.csv`, `AttackSkill.csv`, `PreciseCastSkill.csv`, `SkillSelector.csv`, `AutoSkill.csv`, `SkillLevelUp.csv` | Cơ chế kỹ năng, đạn đạo, DoT interval, tham số đa biến `Param1..6`, `MissileForm`, `AcceSpeedInfo`, cơ chế Joystick Smartcast Selector, icon & atlas kỹ năng. |
| **03** | [**03_COMBAT_AND_STATES.md**](file:///D:/Unity%20Project/test1/Assets/Settings/GameData/Docs/Conventions/03_COMBAT_AND_STATES.md) | `SkillSetting.ini`, `SkillConstant.csv`, `SpecialState.csv`, `CommonScript/Skill/` | Công thức tính Dame, Hit/Dodge/Crit, Khắc chế hệ ngũ hành tương sinh tương khắc, hệ thống hiệu ứng bất lợi & khống chế (CC). |
| **04** | [**04_NPC_MONSTER_AI.md**](file:///D:/Unity%20Project/test1/Assets/Settings/GameData/Docs/Conventions/04_NPC_MONSTER_AI.md) | `NpcRes.csv`, `ActionName.csv`, `NpcTemplate.csv`, `Field_HeaderBoss.csv`, `NpcAttribute.csv`, `MagicDesc.csv`, `CommonActive.ini`, `CommonPassive.ini` | Thông số thể tích quái, animation clips, template Boss/quái, thuộc tính sinh mệnh/công kích và máy trạng thái AI FSM cơ bản. |
| **05** | [**05_VFX_AUDIO_SLOTS.md**](file:///D:/Unity%20Project/test1/Assets/Settings/GameData/Docs/Conventions/05_VFX_AUDIO_SLOTS.md) | `EffectRes.csv`, `StateEffect.csv`, `PartSlot.csv`, `Sound.csv`, `AUDIO_MAPPING_RULES.md` | Cơ chế VFX Pooling, 4 chế độ khóa trục xoay `VfxLockRotation`, khớp xương gắn hiệu ứng (`PartSlot`) và tra cứu âm thanh Wwise. |
| **06** | [**06_LEVEL_AND_EXP.md**](file:///D:/Unity%20Project/test1/Assets/Settings/GameData/Docs/Conventions/06_LEVEL_AND_EXP.md) | `PlayerLevel.csv`, `ExpRule.csv` | Cột mốc cấp độ người chơi, ma trận % EXP quái rơi theo chênh lệch cấp độ và thuật toán thưởng EXP. |

---

## 🔄 THỨ TỰ NẠP PHỤ THUỘC DỮ LIỆU (DATA DEPENDENCY LOADING ORDER)

Khi gọi `GameDatabase.EnsureLoaded()`, dữ liệu **phải** được nạp theo đúng trình tự sau để tránh `NullReferenceException`:

1. `NpcAi` (`Settings/GameData/AI/*.ini`)
2. `Sounds` (`Settings/GameData/Feedback/Sound.csv`)
3. `FlyChars` (`Settings/GameData/Combat/FlyChar.csv`)
4. `PlayerLevels` (`Settings/GameData/Progression/PlayerLevel.csv`)
5. `ExpRules` (`Settings/GameData/Progression/ExpRule.csv`)
6. `Effects` (`Settings/GameData/VFX_Slots/EffectRes.csv`)
7. `Missiles` (`Settings/GameData/Combat/Missile.csv`) $\rightarrow$ cần `EffectDatabase` để lấy đường dẫn VFX bay/nổ.
8. `StateEffects` (`Settings/GameData/VFX_Slots/StateEffect.csv`) $\rightarrow$ cần `EffectDatabase`.
9. `PartSlots` (`Settings/GameData/VFX_Slots/PartSlot.csv`)
10. `NpcRes` (`Settings/GameData/NPC/NpcRes.csv`)
11. `NpcAttributes` (`Settings/GameData/NPC/NpcAttribute.csv`)
12. `NpcTemplates` (`Settings/GameData/NPC/NpcTemplate.csv`, `Character.csv`) $\rightarrow$ cần `NpcRes`, `NpcAttribute` & `NpcAi`.
13. `Skills` (`Settings/GameData/Combat/Skill.csv`, `ActionEvent.csv`) $\rightarrow$ cần toàn bộ các bảng trên.

---

## ⚡ NGUYÊN TẮC KỸ THUẬT BẤT BIẾN (ENGINEERING HARD RULES)

1. **Chuẩn Frame Timing (15 FPS):** $t = \frac{\text{frame}}{15.0}$. Toàn bộ logic ra đòn, hủy chiêu, animation duration và projectile interval đều tuân thủ 15 FPS.
2. **Zero GC Allocation trong Game Loop:** Tuyệt đối không dùng `new`, `GetComponent`, `FindObjectOfType`, `Physics.OverlapSphere` trong `Update` / `FixedUpdate`. Dùng Pooling tĩnh và `NonAlloc`.
3. **Legacy Animation Body + Head:** Sử dụng component `UnityEngine.Animation` đồng bộ qua `LegacyAnimationController.cs`. Không sử dụng Mecanim Controller.
4. **Data-Driven (CSV Source of Truth):** Mọi chỉ số phải nạp qua `GameDatabase` và CSV, không hardcode vào MonoBehaviour.

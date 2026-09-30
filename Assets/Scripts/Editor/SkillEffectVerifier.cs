using UnityEngine;
using UnityEditor;
using TopDownGame.Skills;
using TopDownGame.Data;
using TopDownGame.Combat;

namespace TopDownGame.Editor
{
    public static class SkillEffectVerifier
    {
        [MenuItem("Tools/Verify Skill Effects")]
        public static void Verify()
        {
            Debug.Log("==================== [VERIFYING SKILL EFFECTS] ====================");

            // Nạp lại toàn bộ database
            EffectDatabase.Instance.LoadDatabase();
            MissileDatabase.Instance.LoadDatabase();
            StateEffectDatabase.Reload();
            SkillDatabase.Instance.LoadDatabase();

            // 1. KIỂM TRA SKILL 306 (Từ Hàng Phổ Độ)
            SkillData s306 = SkillDatabase.GetSkill(306);
            if (s306 == null)
            {
                Debug.LogError("❌ Skill 306 KHÔNG tồn tại trong SkillDatabase!");
            }
            else
            {
                Debug.Log($"✅ Skill 306: Name='{s306.name}', IsHeal={s306.IsHeal}, ChildId={s306.childId}, StateEffectId={s306.stateEffectId}");
                Debug.Log($"   EffectEvents count: {s306.effectEvents.Count}");
                for (int i = 0; i < s306.effectEvents.Count; i++)
                {
                    var ev = s306.effectEvents[i];
                    Debug.Log($"   - Cast Event #{i + 1}: Frame={ev.frame}, SlotId={ev.slotId}, Path='{ev.effectPath}', Duration={ev.duration:F2}s");
                }

                // Kiểm tra 2 hiệu ứng thi triển chiêu (Cast effects)
                bool hasWQ = s306.effectEvents.Exists(e => e.effectPath.Contains("JN_01_WQ"));
                bool hasDL = s306.effectEvents.Exists(e => e.effectPath.Contains("JN_DL_WQ"));
                Debug.Log($"   [Hiệu ứng thi triển 1 - JN_01_WQ]: {(hasWQ ? "✅ ĐÃ CÓ" : "❌ THIẾU")}");
                Debug.Log($"   [Hiệu ứng thi triển 2 - JN_DL_WQ]: {(hasDL ? "✅ ĐÃ CÓ" : "❌ THIẾU")}");

                // Kiểm tra hiệu ứng hoa sen nở (End Effect / Missile 306)
                string lotusPath = EffectDatabase.GetEffectPath(s306.childId);
                Debug.Log($"   [Hiệu ứng hoa sen nở - JN_01 (Slot 19)]: Path='{lotusPath}' => {(!string.IsNullOrEmpty(lotusPath) ? "✅ ĐÃ CÓ" : "❌ THIẾU")}");

                // Kiểm tra hiệu ứng Buff gắn trên người (StateEffect 306 -> JN_01_BUFF)
                var stateEff = StateEffectDatabase.GetStateEffect(s306.stateEffectId);
                if (stateEff != null)
                {
                    Debug.Log($"   [Hiệu ứng Buff - JN_01_BUFF (Slot 7)]: Path1='{stateEff.effectPath1}' => {(!string.IsNullOrEmpty(stateEff.effectPath1) ? "✅ ĐÃ CÓ" : "❌ THIẾU")}");
                }
                else
                {
                    Debug.LogError("   ❌ StateEffect 306 KHÔNG tìm thấy trong StateEffectDatabase!");
                }
            }

            // 2. KIỂM TRA SKILL 308 (Bạch Lộ Ngưng Sương)
            SkillData s308 = SkillDatabase.GetSkill(308);
            if (s308 == null)
            {
                Debug.LogError("❌ Skill 308 KHÔNG tồn tại trong SkillDatabase!");
            }
            else
            {
                Debug.Log($"✅ Skill 308: Name='{s308.name}', ChildId={s308.childId}");
                Debug.Log($"   EffectEvents count: {s308.effectEvents.Count}");
                for (int i = 0; i < s308.effectEvents.Count; i++)
                {
                    var ev = s308.effectEvents[i];
                    Debug.Log($"   - Event #{i + 1}: Frame={ev.frame}, SlotId={ev.slotId}, Path='{ev.effectPath}', Duration={ev.duration:F2}s");
                }

                // Kiểm tra hiệu ứng toé nước khi vung kiếm (JN_02_SF - ResID 6308)
                bool hasSF = s308.effectEvents.Exists(e => e.effectPath.Contains("JN_02_SF"));
                Debug.Log($"   [Hiệu ứng toé nước khi vung kiếm - JN_02_SF]: {(hasSF ? "✅ ĐÃ CÓ" : "❌ THIẾU")}");

                // Kiểm tra hiệu ứng Missile (JN_02_DD - ResID 308)
                var missile = MissileDatabase.GetMissile(s308.childId);
                string missilePath = missile != null ? EffectDatabase.GetEffectPath(missile.missileResID) : "";
                Debug.Log($"   [Hiệu ứng Missile đạn cầu băng - JN_02_DD]: Path='{missilePath}' => {(!string.IsNullOrEmpty(missilePath) ? "✅ ĐÃ CÓ" : "❌ THIẾU")}");
            }

            // 3. KIỂM TRA SKILL 312 (Thiên Vũ Bảo Luân - Area DoT & StartSkill 313)
            SkillData s312 = SkillDatabase.GetSkill(312);
            if (s312 != null)
            {
                Debug.Log($"✅ Skill 312: Name='{s312.name}', MSGenerate={s312.msGenerate} (AreaDoT), ChildCount={s312.childCount}, MSGenerateParam='{s312.msGenerateParam}', StartSkillID={s312.startSkillId}");
            }

            // 4. KIỂM TRA SKILL 313 (Thiên Vũ Bảo Luân _ Hồi Sinh Lực - Static Repeat Heal)
            SkillData s313 = SkillDatabase.GetSkill(313);
            if (s313 != null)
            {
                var m313 = MissileDatabase.GetMissile(s313.childId);
                Debug.Log($"✅ Skill 313: Name='{s313.name}', IsHeal={s313.IsHeal}, Missile 313: MoveKind={m313?.moveKind}, CanRepeatDmg={m313?.canRepeatDmg}, DmgInterval={m313?.dmgInterval}, LifeTime={m313?.lifeTime}");
            }

            // 5. KIỂM TRA SKILL 631 (Vạn Kiếm Phong Thiên Quyết - FlySkill 632)
            SkillData s631 = SkillDatabase.GetSkill(631);
            if (s631 != null)
            {
                Debug.Log($"✅ Skill 631: Name='{s631.name}', FlySkillId={s631.flySkillId}, FlyEventInterval={s631.flyEventInterval}");
            }

            Debug.Log("===================================================================");
        }
    }
}

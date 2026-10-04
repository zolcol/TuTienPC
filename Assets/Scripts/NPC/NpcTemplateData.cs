using System;
using System.Collections.Generic;
using UnityEngine;
using TopDownGame.Data;
using TopDownGame.Combat;

namespace TopDownGame.NPC
{
    [Serializable]
    public class NpcTemplateData
    {
        public int id;
        public string name;
        public NpcKind kind = NpcKind.Normal;
        public int camp = 1;

        public int npcResId;
        public int npcAttribId;

        // Kỹ năng của NPC / Quái vật
        public int skill;
        public int skill1;
        public int skill2;
        public int skill3;

        // Tầm quan sát và tốc độ
        public float visionRadius = 10f;
        public float activeRadius = 15f;
        public float runSpeed = 5.0f;
        public float walkSpeed = 2.5f;

        // Cấu hình AI & Di chuyển theo DATA_CONVENTIONS_V2.md (Mục 21)
        public string aiFile = "CommonActive";
        public bool forbitMove = false;

        // Đường dẫn Prefab Model (tự động đồng bộ từ NpcRes.csv)
        public string prefab;

        /// <summary>
        /// Lấy tài nguyên Model 3D tương ứng từ NpcResDatabase
        /// </summary>
        public NpcResData GetRes()
        {
            return NpcResDatabase.GetRes(npcResId);
        }

        /// <summary>
        /// Lấy cấu hình AI từ NpcAiDatabase
        /// </summary>
        public NpcAiData GetAi()
        {
            return NpcAiDatabase.GetAi(aiFile);
        }

        /// <summary>
        /// Lấy chỉ số sinh mệnh (Máu, Công, Ngũ hành Level 1) từ NpcAttributeDatabase
        /// </summary>
        public NpcAttributeData GetAttribute()
        {
            return NpcAttributeDatabase.GetAttribute(npcAttribId);
        }

        /// <summary>
        /// Lấy danh sách toàn bộ các Skill ID hợp lệ (> 0) của NPC này
        /// </summary>
        public List<int> GetSkillList()
        {
            var list = new List<int>();
            if (skill > 0) list.Add(skill);
            if (skill1 > 0) list.Add(skill1);
            if (skill2 > 0) list.Add(skill2);
            if (skill3 > 0) list.Add(skill3);
            return list;
        }

        public override string ToString()
        {
            return $"[NpcTemplate #{id}] {name} | Kind: {kind} | ResID: {npcResId} | AttribID: {npcAttribId} | Skills: [{string.Join(", ", GetSkillList())}] | Prefab: {prefab}";
        }
    }
}

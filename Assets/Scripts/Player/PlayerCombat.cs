using System;
using System.Collections.Generic;
using UnityEngine;
using TopDownGame.Combat;
using TopDownGame.Skills;
using TopDownGame.Stats;

namespace TopDownGame.Player
{
    public class PlayerCombat : MonoBehaviour
    {
        [Header("=== CHARACTER & SKILL SLOTS ===")]
        [SerializeField] private int defaultNormalAttackId = 301;
        [SerializeField] private int npcResId = 2;
        [SerializeField] private int skillSlotQ_Id = 306;
        [SerializeField] private int skillSlotE_Id = 308;
        [SerializeField] private int skillSlotR_Id = 346;

        [Header("=== CHEAT & TESTING ===")]
        [SerializeField] private bool noCooldown = false;
        [SerializeField] private bool noManaCost = false;

        [Header("=== DEBUG & GIZMOS ===")]
        [SerializeField] private bool showHitGizmos = true;
        [SerializeField] private float gizmoDisplayDuration = 0.25f;

        private PlayerController player;
        private readonly Dictionary<int, float> cooldownTimers = new Dictionary<int, float>();
        private readonly List<int> cooldownKeysBuffer = new List<int>(8);
        private struct GizmoDrawInfo { public SkillType type; public Vector3 origin, forward; public float range, fanAngle, boxWidth; }
        private GizmoDrawInfo lastGizmo;
        private float gizmoTimer;

        public int DefaultNormalAttackId { get => defaultNormalAttackId; set => defaultNormalAttackId = value; }
        public int NpcResId 
        { 
            get => npcResId; 
            set 
            { 
                npcResId = value; 
                if (player != null) player.ApplyHitboxFromNpcRes(); 
            } 
        }
        public enum SkillHotbarSlot { Q = 0, E = 1, R = 2 }

        public event Action OnSkillSlotsChanged;

        public int SkillSlotQ_Id { get => skillSlotQ_Id; set { skillSlotQ_Id = value; OnSkillSlotsChanged?.Invoke(); } }
        public int SkillSlotE_Id { get => skillSlotE_Id; set { skillSlotE_Id = value; OnSkillSlotsChanged?.Invoke(); } }
        public int SkillSlotR_Id { get => skillSlotR_Id; set { skillSlotR_Id = value; OnSkillSlotsChanged?.Invoke(); } }

        public SkillData DefaultNormalAttack => SkillDatabase.GetSkill(defaultNormalAttackId);
        public SkillData SkillSlotQ => SkillDatabase.GetSkill(skillSlotQ_Id);
        public SkillData SkillSlotE => SkillDatabase.GetSkill(skillSlotE_Id);
        public SkillData SkillSlotR => SkillDatabase.GetSkill(skillSlotR_Id);
        public bool NoCooldown { get => noCooldown; set => noCooldown = value; }
        public bool NoManaCost { get => noManaCost; set => noManaCost = value; }

        public void AssignSkillToSlot(SkillHotbarSlot slot, int skillId)
        {
            switch (slot)
            {
                case SkillHotbarSlot.Q:
                    skillSlotQ_Id = skillId;
                    break;
                case SkillHotbarSlot.E:
                    skillSlotE_Id = skillId;
                    break;
                case SkillHotbarSlot.R:
                    skillSlotR_Id = skillId;
                    break;
            }
            OnSkillSlotsChanged?.Invoke();
        }

        public SkillHotbarSlot? GetEquippedSlot(int skillId)
        {
            if (skillId <= 0) return null;
            if (skillSlotQ_Id == skillId) return SkillHotbarSlot.Q;
            if (skillSlotE_Id == skillId) return SkillHotbarSlot.E;
            if (skillSlotR_Id == skillId) return SkillHotbarSlot.R;
            return null;
        }

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            SkillDatabase.Instance.EnsureLoaded();
        }

        public void TickCooldowns()
        {
            if (gizmoTimer > 0f) gizmoTimer -= Time.deltaTime;
            if (cooldownTimers.Count == 0) return;
            cooldownKeysBuffer.Clear();
            foreach (var kvp in cooldownTimers) if (kvp.Value > 0f) cooldownKeysBuffer.Add(kvp.Key);
            float dt = Time.deltaTime;
            for (int i = 0; i < cooldownKeysBuffer.Count; i++)
            {
                int id = cooldownKeysBuffer[i];
                float rem = cooldownTimers[id] - dt;
                cooldownTimers[id] = rem > 0f ? rem : 0f;
            }
        }

        public bool IsOnCooldown(int skillId) => !noCooldown && skillId > 0 && cooldownTimers.TryGetValue(skillId, out float t) && t > 0f;
        public float GetRemainingCooldown(int skillId) => (!noCooldown && skillId > 0 && cooldownTimers.TryGetValue(skillId, out float t)) ? Mathf.Max(0f, t) : 0f;
        public void StartCooldown(int skillId, float duration) { if (!noCooldown && skillId > 0 && duration > 0f) cooldownTimers[skillId] = duration; }

        public bool CheckAndTriggerSkills()
        {
            if (player?.InputReader == null) return false;
            var input = player.InputReader;
            return HandleSkillInput(input.Skill2Triggered, input.Skill2Held, input.Skill2Released, SkillSlotQ) ||
                   HandleSkillInput(input.Skill1Triggered, input.Skill1Held, input.Skill1Released, SkillSlotE) ||
                   HandleSkillInput(input.Skill3Triggered, input.Skill3Held, input.Skill3Released, SkillSlotR);
        }

        private bool HandleSkillInput(bool triggered, bool held, bool released, SkillData skill)
        {
            if (skill == null || !CanExecuteSkill(skill))
            {
                if (player?.Aiming?.AimingSkill == skill) player.Aiming.CancelAiming();
                return false;
            }
            if (skill.selectorType == SkillSelectorType.None)
            {
                if (triggered) { player?.Aiming?.CancelAiming(); return ExecuteSkill(skill); }
            }
            else
            {
                if (triggered) player?.Aiming?.StartAiming(skill);
                else if (released && player?.Aiming?.AimingSkill == skill) { player.Aiming.CancelAiming(); return ExecuteSkill(skill); }
                else if (!held && player?.Aiming?.AimingSkill == skill) player.Aiming.CancelAiming();
            }
            return false;
        }

        public bool CanExecuteSkill(SkillData skill) => skill != null && !IsOnCooldown(skill.id) && (noManaCost || player?.Stats == null || skill.manaCost <= 0f || player.Stats.HasEnoughMana(skill.manaCost));

        private bool ExecuteSkill(SkillData skill)
        {
            if (!ExecuteAction(skill)) return false;
            if (!noManaCost && player?.Stats != null && skill.manaCost > 0f) player.Stats.ConsumeMana(skill.manaCost);
            if (!noCooldown) StartCooldown(skill.id, skill.cooldown);
            return true;
        }

        public bool StartNormalAttack() => DefaultNormalAttack != null ? ExecuteAction(DefaultNormalAttack) : false;

        public bool ExecuteAction(SkillData skill)
        {
            if (skill == null || player == null) return false;
            bool isGamepad = player.InputReader != null && player.InputReader.IsUsingGamepad;
            Vector3 inputVec = player.Movement != null ? player.GetInputVector() : Vector3.zero;

            if (player.Aiming != null && !player.Aiming.AimSkill(skill, inputVec, isGamepad)) return false;
            if (player.Movement != null && player.Aiming != null && ((skill.instantDirSpeed > 0f ? skill.instantDirSpeed : 1000f) >= 1000f))
                player.Movement.RotateTowardsInstantly(player.Aiming.CurrentTargetDirection);

            player.AttackState.SetSkill(skill);
            if (player.StateMachine.CurrentState == player.AttackState) player.AttackState.Enter();
            else player.StateMachine.ChangeState(player.AttackState);
            return true;
        }

        public void ExecuteSkillDamage(SkillData skill, Transform lockTarget, Vector3 targetPoint)
        {
            if (skill == null || player == null) return;
            LayerMask mask = player.Aiming != null ? player.Aiming.TargetLayer : ~0;
            SkillDamageResolver.CastDamage(transform, player.Stats, skill, mask, lockTarget, targetPoint);
            if (showHitGizmos)
            {
                lastGizmo = new GizmoDrawInfo { type = skill.skillType, origin = transform.position, forward = transform.forward, range = skill.range, fanAngle = skill.fanAngle, boxWidth = skill.boxWidth };
                gizmoTimer = gizmoDisplayDuration;
            }
        }

        public void PlaySkillCastEffect(SkillData skill)
        {
            if (skill == null) return;
            if (skill.effectEvents != null && skill.effectEvents.Count > 0)
            {
                foreach (var ev in skill.effectEvents) if (ev.frame <= 0 && !string.IsNullOrEmpty(ev.effectPath)) PlaySkillEffectEvent(ev);
            }
            else if (!string.IsNullOrEmpty(skill.effectPath)) EffectManager.Instance.PlaySkillEffect(skill, transform);
        }

        public void PlaySkillEffectEvent(SkillEffectEvent ev)
        {
            if (ev == null || string.IsNullOrEmpty(ev.effectPath)) return;
            float dur = ev.duration > 0f ? ev.duration : 2.5f;
            if (ev.slotId > 0) EffectManager.Instance.SpawnEffectAtSlot(ev.effectPath, transform, ev.slotId, dur, true);
            else EffectManager.Instance.SpawnEffect(ev.effectPath, transform.position, transform.rotation, null, dur);
        }

        public void OnHitTriggered(SkillData skill)
        {
            if (skill != null && !skill.HasProjectile && !skill.IsHeal) EffectManager.Instance.PlaySkillEffect(skill, transform);
        }

        private void OnDrawGizmos()
        {
            if (!showHitGizmos || gizmoTimer <= 0f) return;
            SkillDamageResolver.DrawGizmo(lastGizmo.type, lastGizmo.origin, lastGizmo.forward, lastGizmo.range, lastGizmo.fanAngle, lastGizmo.boxWidth);
        }
    }
}

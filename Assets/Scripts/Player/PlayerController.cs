using UnityEngine;
using TopDownGame.Combat;
using TopDownGame.Data;
using TopDownGame.Input;
using TopDownGame.Skills;
using TopDownGame.StateMachine;
using TopDownGame.Stats;

namespace TopDownGame.Player
{
    [RequireComponent(typeof(CharacterController), typeof(PlayerInputReader))]
    [RequireComponent(typeof(CharacterMovement), typeof(PlayerAiming), typeof(PlayerCombat))]
    public class PlayerController : MonoBehaviour
    {
        public TopDownGame.StateMachine.StateMachine StateMachine { get; private set; }
        public PlayerIdleState IdleState { get; private set; }
        public PlayerMoveState MoveState { get; private set; }
        public PlayerAttackState AttackState { get; private set; }
        public PlayerHurtState HurtState { get; private set; }

        public CharacterMovement Movement { get; private set; }
        public PlayerAiming Aiming { get; private set; }
        public PlayerCombat Combat { get; private set; }
        public PlayerInputReader InputReader { get; private set; }
        public LegacyAnimationController AnimationController { get; private set; }
        public PlayerStats Stats { get; private set; }
        public CharacterController CharacterController => Movement != null ? Movement.CharacterController : GetComponent<CharacterController>();

        public float MoveSpeed => (Stats != null && Stats.MoveSpeed > 0f) ? Stats.MoveSpeed : (Movement != null ? Movement.MoveSpeed : 6f);
        public float AttackRotationSmoothTime => Movement != null ? Movement.AttackRotationSmoothTime : 0.14f;
        public int NpcResId => Combat != null ? Combat.NpcResId : 2;
        public SkillData DefaultNormalAttack => Combat != null ? Combat.DefaultNormalAttack : null;
        public SkillData SkillSlotQ => Combat != null ? Combat.SkillSlotQ : null;
        public SkillData SkillSlotE => Combat != null ? Combat.SkillSlotE : null;
        public SkillData SkillSlotR => Combat != null ? Combat.SkillSlotR : null;
        public int SkillSlotQ_Id => Combat != null ? Combat.SkillSlotQ_Id : 0;
        public int SkillSlotE_Id => Combat != null ? Combat.SkillSlotE_Id : 0;
        public int SkillSlotR_Id => Combat != null ? Combat.SkillSlotR_Id : 0;
        public bool NoCooldown { get => Combat != null && Combat.NoCooldown; set { if (Combat != null) Combat.NoCooldown = value; } }
        public bool NoManaCost { get => Combat != null && Combat.NoManaCost; set { if (Combat != null) Combat.NoManaCost = value; } }
        public Transform CurrentLockTarget => Aiming != null ? Aiming.CurrentLockTarget : null;
        public Vector3 CurrentTargetPoint => Aiming != null ? Aiming.CurrentTargetPoint : transform.position + transform.forward * 5f;
        public Vector3 CurrentTargetDirection => Aiming != null ? Aiming.CurrentTargetDirection : transform.forward;

        private void Awake()
        {
            Movement = GetComponent<CharacterMovement>() ?? gameObject.AddComponent<CharacterMovement>();
            Aiming = GetComponent<PlayerAiming>() ?? gameObject.AddComponent<PlayerAiming>();
            Combat = GetComponent<PlayerCombat>() ?? gameObject.AddComponent<PlayerCombat>();
            InputReader = GetComponent<PlayerInputReader>() ?? GetComponentInChildren<PlayerInputReader>();
            AnimationController = GetComponent<LegacyAnimationController>() ?? GetComponentInChildren<LegacyAnimationController>();
            Stats = GetComponent<PlayerStats>() ?? GetComponentInChildren<PlayerStats>();

            StateMachine = new TopDownGame.StateMachine.StateMachine();
            IdleState = new PlayerIdleState(this, StateMachine);
            MoveState = new PlayerMoveState(this, StateMachine);
            AttackState = new PlayerAttackState(this, StateMachine);
            HurtState = new PlayerHurtState(this, StateMachine);
        }

        private void Start()
        {
            ApplyHitboxFromNpcRes();
            ApplyStatsFromDatabase();
            if (Stats != null) Stats.OnDamaged += HandleDamaged;
            StateMachine.Initialize(IdleState);
        }

        public void ApplyStatsFromDatabase(int templateId = 0)
        {
            if (Stats == null) return;
            NpcStatDatabase.Instance.EnsureLoaded();
            var statData = NpcStatDatabase.GetStats(templateId);
            if (statData != null)
            {
                Stats.ApplyStatsFromData(statData, Stats.CurrentLevel);
            }
        }

        private void OnDestroy()
        {
            if (Stats != null) Stats.OnDamaged -= HandleDamaged;
        }

        private void HandleDamaged(float amount, Vector3 hitPoint, Vector3 hitDirection)
        {
            if (Stats == null || Stats.IsDead) return;
            // Chỉ nhảy hoạt ảnh bị thương khi người chơi đang đứng yên (Idle)
            if (StateMachine.CurrentState == IdleState)
            {
                StateMachine.ChangeState(HurtState);
            }
        }

        private void Update()
        {
            if (Combat != null) Combat.TickCooldowns();
            StateMachine.Update();
            if (Aiming != null) Aiming.UpdateSkillAiming(GetInputVector(), InputReader != null && InputReader.IsUsingGamepad);
            if (Movement != null) Movement.UpdateMovementAndGravity();
        }

        private void FixedUpdate() => StateMachine.PhysicsUpdate();

        public bool IsOnCooldown(int skillId) => Combat != null && Combat.IsOnCooldown(skillId);
        public float GetRemainingCooldown(int skillId) => Combat != null ? Combat.GetRemainingCooldown(skillId) : 0f;
        public void StartCooldown(int skillId, float duration) => Combat?.StartCooldown(skillId, duration);
        public Vector3 GetInputVector() => Movement != null ? Movement.CalculateCameraRelativeInput(InputReader != null ? InputReader.MoveInput : Vector2.zero) : Vector3.zero;
        public bool IsAttackPressed() => InputReader != null && (InputReader.AttackTriggered || InputReader.IsAttackHeld);
        public bool CheckAndTriggerSkills() => Combat != null && Combat.CheckAndTriggerSkills();
        public void CancelAiming() => Aiming?.CancelAiming();
        public bool CanExecuteSkill(SkillData skill) => Combat != null && Combat.CanExecuteSkill(skill);
        public void Move(Vector3 direction) => Movement?.Move(direction);
        public void MoveWithSpeed(Vector3 direction, float speed) => Movement?.MoveWithSpeed(direction, speed);
        public void RotateTowards(Vector3 direction) => Movement?.RotateTowards(direction);
        public void RotateTowards(Vector3 direction, float smoothTime) => Movement?.RotateTowards(direction, smoothTime);
        public void ResetTurnVelocity() => Movement?.ResetTurnVelocity();

        public void RotateTowardsCastDirection(float speedDegPerSec)
        {
            if (Movement == null) return;
            Vector3 dir = Aiming != null ? Aiming.CurrentTargetDirection : transform.forward;
            if (Aiming != null && Aiming.CurrentLockTarget != null)
            {
                dir = Aiming.CurrentLockTarget.position - transform.position;
                dir.y = 0f;
            }
            Movement.RotateTowardsDirection(dir, speedDegPerSec);
        }

        public bool UpdateAttackAim(SkillData skill, float smoothTime)
        {
            float rotSpeed = (skill != null && skill.instantDirSpeed > 0f) ? skill.instantDirSpeed : 1000f;
            RotateTowardsCastDirection(rotSpeed);
            return true;
        }

        public bool StartNormalAttack() => Combat != null && Combat.StartNormalAttack();
        public bool ExecuteAction(SkillData skill) => Combat != null && Combat.ExecuteAction(skill);
        public void ExecuteSkillDamage(SkillData skill) => Combat?.ExecuteSkillDamage(skill, Aiming?.CurrentLockTarget, Aiming != null ? Aiming.CurrentTargetPoint : transform.position);
        public void PlaySkillCastEffect(SkillData skill) => Combat?.PlaySkillCastEffect(skill);
        public void PlaySkillEffectEvent(SkillEffectEvent ev) => Combat?.PlaySkillEffectEvent(ev);
        public void OnHitTriggered(SkillData skill) => Combat?.OnHitTriggered(skill);

        /// <summary>
        /// Tự động cập nhật kích thước CharacterController theo dữ liệu NpcRes (height, width)
        /// </summary>
        public void ApplyHitboxFromNpcRes()
        {
            NpcResDatabase.Instance.EnsureLoaded();
            var resData = NpcResDatabase.GetRes(NpcResId);
            if (resData == null) return;

            CharacterController cc = CharacterController;
            if (cc == null) return;

            float height = resData.height > 0f ? resData.height : 1.8f;
            float radius = resData.width > 0f ? resData.width : 0.5f;

            cc.height = height;
            cc.radius = radius;
            cc.center = new Vector3(0f, height * 0.5f, 0f);
        }
    }
}

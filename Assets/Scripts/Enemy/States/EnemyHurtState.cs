using UnityEngine;
using TopDownGame.Skills;

namespace TopDownGame.Enemy
{
    /// <summary>
    /// Trạng thái bị thương cơ bản cho Quái vật (ActId = 9, bat).
    /// Khi bị thương, quái vật dừng lại chờ hết thời lượng hoạt ảnh mới tiếp tục di chuyển / tấn công.
    /// </summary>
    public class EnemyHurtState : EnemyBaseState
    {
        private float timer;
        private float hurtDuration;

        public EnemyHurtState(EnemyController enemy, TopDownGame.StateMachine.StateMachine stateMachine) 
            : base(enemy, stateMachine) { }

        public override void Enter()
        {
            base.Enter();
            timer = 0f;

            bool hasHurtClip = enemy.AnimationController != null && enemy.AnimationController.HasClip(LegacyAnimationController.CLIP_HURT);
            if (hasHurtClip)
            {
                enemy.AnimationController.PlayHurt(0.05f);
                hurtDuration = enemy.AnimationController.GetClipDuration(LegacyAnimationController.CLIP_HURT);
                if (hurtDuration <= 0.05f)
                {
                    hurtDuration = 0.25f;
                }
            }
            else
            {
                hurtDuration = 0f;
            }
        }

        public override void Update()
        {
            base.Update();

            if (enemy.Perception != null)
            {
                enemy.Perception.TickPerception(Time.deltaTime);
            }

            // Nếu trong lúc bị thương có chiêu thức sẵn sàng và mục tiêu trong tầm -> Xuất chiêu tấn công ngay
            float distanceToTarget = enemy.GetDistanceToTarget();
            var readyAttack = enemy.GetReadyAttack(distanceToTarget);
            if (readyAttack != null)
            {
                enemy.AttackState.SetSkill(readyAttack);
                stateMachine.ChangeState(enemy.AttackState);
                return;
            }

            timer += Time.deltaTime;
            if (timer >= hurtDuration)
            {
                // Khi hoàn tất hoạt ảnh bị thương, ưu tiên quay lại truy đuổi nếu target còn trong tầm
                if (enemy.Target != null && enemy.GetDistanceToTarget() <= enemy.ActiveRadius && enemy.GetDistanceToSpawn() <= enemy.ActiveRadius)
                {
                    stateMachine.ChangeState(enemy.ChaseState);
                }
                else
                {
                    stateMachine.ChangeState(enemy.IdleState);
                }
            }
        }

        public override void Exit()
        {
            base.Exit();
            if (enemy.AnimationController != null)
            {
                enemy.AnimationController.ForceResetClip();
            }
        }
    }
}

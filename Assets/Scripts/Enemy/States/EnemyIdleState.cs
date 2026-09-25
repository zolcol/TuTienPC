using UnityEngine;

namespace TopDownGame.Enemy
{
    public class EnemyIdleState : EnemyBaseState
    {
        public EnemyIdleState(EnemyController enemy, TopDownGame.StateMachine.StateMachine stateMachine) 
            : base(enemy, stateMachine) { }

        public override void Enter()
        {
            base.Enter();
            if (enemy.AnimationController != null)
            {
                enemy.AnimationController.PlayIdle();
            }
        }

        public override void Update()
        {
            base.Update();

            if (enemy.Target == null)
            {
                enemy.TryFindTarget();
                return;
            }

            float distanceToTarget = enemy.GetDistanceToTarget();

            // 1. Nếu phát hiện người chơi trong tầm quan sát
            if (distanceToTarget <= enemy.DetectionRange)
            {
                // Nếu đã ở ngay trong tầm đánh và có chiêu sẵn sàng -> Tấn công ngay
                if (distanceToTarget <= enemy.AttackRange)
                {
                    var readyAttack = enemy.GetReadyAttack();
                    if (readyAttack != null)
                    {
                        enemy.AttackState.SetSkill(readyAttack);
                        stateMachine.ChangeState(enemy.AttackState);
                        return;
                    }
                }

                // Nếu chưa vào tầm đánh hoặc chưa có chiêu -> Chuyển sang rượt đuổi
                stateMachine.ChangeState(enemy.ChaseState);
            }
        }
    }
}

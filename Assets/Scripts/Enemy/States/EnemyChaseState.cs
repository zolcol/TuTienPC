using UnityEngine;

namespace TopDownGame.Enemy
{
    public class EnemyChaseState : EnemyBaseState
    {
        public EnemyChaseState(EnemyController enemy, TopDownGame.StateMachine.StateMachine stateMachine) 
            : base(enemy, stateMachine) { }

        public override void Enter()
        {
            base.Enter();
            if (enemy.AnimationController != null)
            {
                enemy.AnimationController.PlayRun();
            }
        }

        public override void Update()
        {
            base.Update();

            if (enemy.Target == null)
            {
                enemy.TryFindTarget();
                if (enemy.Target == null)
                {
                    stateMachine.ChangeState(enemy.IdleState);
                    return;
                }
            }

            float distanceToTarget = enemy.GetDistanceToTarget();

            // 1. Mất dấu mục tiêu (mục tiêu chạy quá xa vùng quan sát)
            if (distanceToTarget > enemy.DetectionRange * 1.3f)
            {
                stateMachine.ChangeState(enemy.IdleState);
                return;
            }

            // 2. Đã tiếp cận trong tầm đánh
            if (distanceToTarget <= enemy.AttackRange)
            {
                var readyAttack = enemy.GetReadyAttack();
                if (readyAttack != null)
                {
                    enemy.AttackState.SetSkill(readyAttack);
                    stateMachine.ChangeState(enemy.AttackState);
                    return;
                }
                else
                {
                    // Chiêu đang hồi -> Dừng bước, xoay mặt nhìn mục tiêu, đứng thủ thế chiến đấu chờ hồi chiêu (sta)
                    enemy.RotateTowardsTarget();
                    if (enemy.AnimationController != null)
                    {
                        enemy.AnimationController.PlayBattleIdle();
                    }
                    return;
                }
            }

            // 3. Đang ngoài tầm đánh -> Chạy thẳng tới mục tiêu
            if (enemy.AnimationController != null)
            {
                enemy.AnimationController.PlayRun();
            }

            enemy.MoveTowardsTarget();
            enemy.RotateTowardsTarget();
        }
    }
}

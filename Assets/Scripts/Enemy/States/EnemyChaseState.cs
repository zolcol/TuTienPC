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

            if (enemy.Perception != null)
            {
                enemy.Perception.TickPerception(Time.deltaTime);
            }

            if (enemy.Target == null)
            {
                stateMachine.ChangeState(enemy.IdleState);
                return;
            }

            float distanceToTarget = enemy.GetDistanceToTarget();
            float distanceFromSpawn = enemy.GetDistanceToSpawn();

            // 1. Vượt quá tầm truy đuổi / Leash Range (ActiveRadius) tính từ điểm xuất phát hoặc khoảng cách mục tiêu
            if (distanceToTarget > enemy.ActiveRadius || distanceFromSpawn > enemy.ActiveRadius)
            {
                stateMachine.ChangeState(enemy.ReturnState);
                return;
            }

            // 2. Đã tiếp cận trong tầm đánh của chiêu thức sẵn sàng
            var readyAttack = enemy.GetReadyAttack(distanceToTarget);
            if (readyAttack != null)
            {
                enemy.AttackState.SetSkill(readyAttack);
                stateMachine.ChangeState(enemy.AttackState);
                return;
            }

            // 3. Nếu là quái cọc gỗ / cố định (ForbitMove = true):
            if (enemy.ForbitMove)
            {
                enemy.RotateTowardsTarget();
                if (enemy.AnimationController != null)
                {
                    enemy.AnimationController.PlayBattleIdle();
                }
                return;
            }

            // 4. Nếu chiêu đang hồi Cooldown:
            // Chỉ dừng bước thủ thế nếu đã áp sát cận chiến (GetMinAttackRange)
            float minCombatRange = enemy.GetMinAttackRange();
            if (distanceToTarget <= minCombatRange)
            {
                enemy.RotateTowardsTarget();
                if (enemy.AnimationController != null)
                {
                    enemy.AnimationController.PlayBattleIdle();
                }
                return;
            }

            // 5. Chưa vào cự ly ra đòn -> Tiếp tục chạy thẳng tới mục tiêu
            if (enemy.AnimationController != null)
            {
                enemy.AnimationController.PlayRun();
            }

            enemy.MoveTowardsTarget();
            enemy.RotateTowardsTarget();
        }
    }
}

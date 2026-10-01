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
            }

            float distanceToTarget = enemy.GetDistanceToTarget();
            float distanceFromSpawn = enemy.GetDistanceToSpawn();

            // Nếu đang quay về điểm spawn (Leash Mode), chỉ cho phép rượt lại khi đã lùi an toàn vào trong hoặc Player áp sát cực gần
            if (enemy.IsReturningToSpawn)
            {
                if (distanceFromSpawn <= enemy.ActiveRadius * 0.75f || distanceToTarget <= enemy.GetMinAttackRange() || distanceFromSpawn <= 0.8f)
                {
                    enemy.IsReturningToSpawn = false;
                }
            }

            // 1. Nếu không trong trạng thái trả quái về spawn, phát hiện người chơi trong tầm quan sát (VisionRadius) và trong vùng hoạt động hợp lệ (ActiveRadius)
            if (!enemy.IsReturningToSpawn && enemy.Target != null && distanceToTarget <= enemy.DetectionRange && distanceFromSpawn <= enemy.ActiveRadius)
            {
                // Nếu đã ở ngay trong tầm đánh và có chiêu sẵn sàng -> Tấn công ngay
                var readyAttack = enemy.GetReadyAttack(distanceToTarget);
                if (readyAttack != null)
                {
                    enemy.AttackState.SetSkill(readyAttack);
                    stateMachine.ChangeState(enemy.AttackState);
                    return;
                }

                // Nếu chưa vào tầm đánh hoặc chưa có chiêu -> Chuyển sang rượt đuổi
                stateMachine.ChangeState(enemy.ChaseState);
                return;
            }

            // 2. Nếu quái đang ở xa điểm spawn (ví dụ sau khi kết thúc truy đuổi), tự động đi bộ quay về
            if (distanceFromSpawn > 0.8f)
            {
                if (enemy.AnimationController != null)
                {
                    enemy.AnimationController.PlayWalk();
                }
                enemy.MoveTowardsSpawn();
                enemy.RotateTowardsSpawn();
            }
            else
            {
                enemy.IsReturningToSpawn = false;
                if (enemy.AnimationController != null)
                {
                    enemy.AnimationController.PlayIdle();
                }
            }
        }
    }
}

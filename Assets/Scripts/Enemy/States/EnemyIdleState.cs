using UnityEngine;

namespace TopDownGame.Enemy
{
    public class EnemyIdleState : EnemyBaseState
    {
        private float wanderWaitTimer;
        private Vector3 wanderTargetPos;
        private bool isWandering;
        private float wanderTimeout;

        public EnemyIdleState(EnemyController enemy, TopDownGame.StateMachine.StateMachine stateMachine) 
            : base(enemy, stateMachine) { }

        public override void Enter()
        {
            base.Enter();
            isWandering = false;
            wanderWaitTimer = Random.Range(1.5f, 3.5f);

            if (enemy.AnimationController != null)
            {
                enemy.AnimationController.PlayIdle();
            }
        }

        public override void Update()
        {
            base.Update();

            if (enemy.Perception != null)
            {
                enemy.Perception.TickPerception(Time.deltaTime);
            }

            float distanceToTarget = enemy.GetDistanceToTarget();
            float distanceFromSpawn = enemy.GetDistanceToSpawn();

            // 1. Kiểm tra nếu có mục tiêu hợp lệ
            if (enemy.Target != null)
            {
                // Nếu khoảng cách vượt quá Leash Range (ActiveRadius) -> Quay về
                if (distanceFromSpawn > enemy.ActiveRadius || distanceToTarget > enemy.ActiveRadius)
                {
                    stateMachine.ChangeState(enemy.ReturnState);
                    return;
                }

                // Nếu có chiêu thức sẵn sàng và trong tầm đánh -> Tấn công ngay
                var readyAttack = enemy.GetReadyAttack(distanceToTarget);
                if (readyAttack != null)
                {
                    enemy.AttackState.SetSkill(readyAttack);
                    stateMachine.ChangeState(enemy.AttackState);
                    return;
                }

                // Chưa vào tầm đánh -> Chuyển sang rượt đuổi (nếu được phép di chuyển) hoặc đứng ngắm
                stateMachine.ChangeState(enemy.ChaseState);
                return;
            }

            // 2. Không có mục tiêu: Kiểm tra nếu quái đang ở xa điểm Spawn -> Đi bộ về
            if (distanceFromSpawn > 1.2f)
            {
                isWandering = false;
                if (enemy.AnimationController != null)
                {
                    enemy.AnimationController.PlayWalk();
                }
                enemy.MoveTowardsSpawn();
                enemy.RotateTowardsSpawn();
                return;
            }

            // 3. Cơ chế tản bộ ngẫu nhiên (RandmonMove %) theo DATA_CONVENTIONS_V2.md (Mục 21)
            var aiData = enemy.AiData;
            if (aiData != null && aiData.wanderChance > 0 && !enemy.ForbitMove)
            {
                if (isWandering)
                {
                    wanderTimeout -= Time.deltaTime;
                    Vector3 diff = wanderTargetPos - enemy.transform.position;
                    diff.y = 0f;

                    if (diff.sqrMagnitude <= 0.15f || wanderTimeout <= 0f)
                    {
                        isWandering = false;
                        wanderWaitTimer = Random.Range(2.0f, 4.5f);
                        if (enemy.AnimationController != null)
                        {
                            enemy.AnimationController.PlayIdle();
                        }
                    }
                    else
                    {
                        if (enemy.AnimationController != null)
                        {
                            enemy.AnimationController.PlayWalk();
                        }
                        enemy.MoveTowardsPosition(wanderTargetPos, enemy.WalkSpeed);
                        enemy.RotateTowardsDirection(diff);
                    }
                    return;
                }
                else
                {
                    wanderWaitTimer -= Time.deltaTime;
                    if (wanderWaitTimer <= 0f)
                    {
                        wanderWaitTimer = Random.Range(2.0f, 4.0f);
                        if (Random.Range(0, 100) < aiData.wanderChance)
                        {
                            Vector2 randCircle = Random.insideUnitCircle * Mathf.Min(3.0f, enemy.ActiveRadius * 0.4f);
                            wanderTargetPos = enemy.SpawnPosition + new Vector3(randCircle.x, 0f, randCircle.y);
                            isWandering = true;
                            wanderTimeout = 3.5f;
                        }
                    }
                }
            }

            if (enemy.AnimationController != null)
            {
                enemy.AnimationController.PlayIdle();
            }
        }
    }
}

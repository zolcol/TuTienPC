using UnityEngine;

namespace TopDownGame.Enemy
{
    public class EnemyDeadState : EnemyBaseState
    {
        private float despawnTimer;

        public EnemyDeadState(EnemyController enemy, TopDownGame.StateMachine.StateMachine stateMachine) 
            : base(enemy, stateMachine) { }

        public override void Enter()
        {
            base.Enter();

            // Tắt va chạm vật lý để Player không bị kẹt khi đi qua xác
            if (enemy.CharacterController != null)
            {
                enemy.CharacterController.enabled = false;
            }

            // Chơi animation chết nếu có cấu hình trong AnimationController
            if (enemy.AnimationController != null)
            {
                enemy.AnimationController.PlayDie();
            }

            despawnTimer = enemy.Stats != null ? enemy.Stats.DespawnDelay : 3.0f;
        }

        public override void Update()
        {
            base.Update();

            despawnTimer -= Time.deltaTime;
            if (despawnTimer <= 0f)
            {
                Object.Destroy(enemy.gameObject);
            }
        }
    }
}

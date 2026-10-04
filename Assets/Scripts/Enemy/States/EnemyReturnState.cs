using UnityEngine;

namespace TopDownGame.Enemy
{
    public class EnemyReturnState : EnemyBaseState
    {
        public EnemyReturnState(EnemyController enemy, TopDownGame.StateMachine.StateMachine stateMachine) 
            : base(enemy, stateMachine) { }

        public override void Enter()
        {
            base.Enter();

            if (enemy.Stats != null)
            {
                enemy.Stats.IsInvulnerable = true;
            }

            if (enemy.Perception != null)
            {
                enemy.Perception.ClearTarget();
                enemy.Perception.ClearAttacker();
            }

            if (enemy.AnimationController != null)
            {
                enemy.AnimationController.PlayWalk();
            }
        }

        public override void Update()
        {
            base.Update();

            float distToSpawn = enemy.GetDistanceToSpawn();

            if (distToSpawn <= 0.6f)
            {
                if (enemy.Stats != null)
                {
                    enemy.Stats.ResetToFullHealth();
                    enemy.Stats.IsInvulnerable = false;
                }

                stateMachine.ChangeState(enemy.IdleState);
                return;
            }

            if (enemy.AnimationController != null)
            {
                enemy.AnimationController.PlayWalk();
            }

            enemy.MoveTowardsSpawn();
            enemy.RotateTowardsSpawn();
        }

        public override void Exit()
        {
            base.Exit();

            if (enemy.Stats != null)
            {
                enemy.Stats.IsInvulnerable = false;
            }
        }
    }
}

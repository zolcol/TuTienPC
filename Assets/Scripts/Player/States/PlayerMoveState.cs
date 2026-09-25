using UnityEngine;

namespace TopDownGame.Player
{
    public class PlayerMoveState : PlayerBaseState
    {
        public PlayerMoveState(PlayerController player, TopDownGame.StateMachine.StateMachine stateMachine) 
            : base(player, stateMachine) { }

        public override void Enter()
        {
            base.Enter();
            if (player.AnimationController != null)
            {
                player.AnimationController.PlayRun();
            }
        }

        public override void Update()
        {
            base.Update();

            // Ưu tiên 1: Tung kỹ năng khi đang di chuyển (Skill E, Q, R)
            if (player.CheckAndTriggerSkills()) return;

            // Ưu tiên 2: Đánh thường khi đang di chuyển
            if (player.IsAttackPressed())
            {
                if (player.StartNormalAttack()) return;
            }

            Vector3 input = player.GetInputVector();

            // Thả phím -> Về Idle
            if (input.sqrMagnitude < 0.001f)
            {
                stateMachine.ChangeState(player.IdleState);
                return;
            }

            player.Move(input);
            player.RotateTowards(input);
        }
    }
}


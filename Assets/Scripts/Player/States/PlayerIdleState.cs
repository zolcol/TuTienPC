using UnityEngine;

namespace TopDownGame.Player
{
    public class PlayerIdleState : PlayerBaseState
    {
        public PlayerIdleState(PlayerController player, TopDownGame.StateMachine.StateMachine stateMachine) 
            : base(player, stateMachine) { }

        public override void Enter()
        {
            base.Enter();
            if (player.AnimationController != null)
            {
                // Mặc định hiện tại Player luôn đứng ở tư thế thủ thế chiến đấu (sta)
                // [Giai đoạn sau khi bổ sung logic vào thành/nội thất sẽ chuyển sang PlayIdle (st)]
                player.AnimationController.PlayBattleIdle();
            }
        }

        public override void Update()
        {
            base.Update();

            // Ưu tiên 1: Tung kỹ năng (Skill E, Q, R)
            if (player.CheckAndTriggerSkills()) return;

            // Ưu tiên 2: Đánh thường (Combo)
            if (player.IsAttackPressed())
            {
                if (player.StartNormalAttack()) return;
            }

            // Ưu tiên 3: Di chuyển
            Vector3 input = player.GetInputVector();
            if (input.sqrMagnitude > 0.001f)
            {
                stateMachine.ChangeState(player.MoveState);
            }
        }
    }
}


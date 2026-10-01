using UnityEngine;
using TopDownGame.Skills;

namespace TopDownGame.Player
{
    /// <summary>
    /// Trạng thái bị thương cơ bản cho Người chơi (ActId = 9, bat).
    /// Kích hoạt khi người chơi đang đứng yên (Idle) và có thể bị ngắt lập tức bởi Di chuyển, Đánh thường, Tung chiêu.
    /// </summary>
    public class PlayerHurtState : PlayerBaseState
    {
        private float timer;
        private float hurtDuration;

        public PlayerHurtState(PlayerController player, TopDownGame.StateMachine.StateMachine stateMachine) 
            : base(player, stateMachine) { }

        public override void Enter()
        {
            base.Enter();
            timer = 0f;

            if (player.AnimationController != null)
            {
                player.AnimationController.PlayHurt(0.05f);
                hurtDuration = player.AnimationController.GetClipDuration(LegacyAnimationController.CLIP_HURT);
            }
            else
            {
                hurtDuration = 0.4f;
            }

            if (hurtDuration <= 0.05f)
            {
                hurtDuration = 0.4f;
            }
        }

        public override void Update()
        {
            base.Update();

            // Ưu tiên 1: Ngắt hoạt ảnh bị thương bằng Tung kỹ năng (Q, E, R)
            if (player.CheckAndTriggerSkills()) return;

            // Ưu tiên 2: Ngắt hoạt ảnh bị thương bằng Đánh thường
            if (player.IsAttackPressed())
            {
                if (player.StartNormalAttack()) return;
            }

            // Ưu tiên 3: Ngắt hoạt ảnh bị thương bằng Di chuyển
            Vector3 input = player.GetInputVector();
            if (input.sqrMagnitude > 0.001f)
            {
                stateMachine.ChangeState(player.MoveState);
                return;
            }

            // Ưu tiên 4: Chờ hết thời lượng hoạt ảnh bị thương rồi trở về trạng thái Idle
            timer += Time.deltaTime;
            if (timer >= hurtDuration)
            {
                stateMachine.ChangeState(player.IdleState);
            }
        }

        public override void Exit()
        {
            base.Exit();
            if (player.AnimationController != null)
            {
                player.AnimationController.ForceResetClip();
            }
        }
    }
}

using UnityEngine;
using TopDownGame.Skills;

namespace TopDownGame.Player
{
    public class PlayerAttackState : PlayerBaseState
    {
        private SkillData currentSkill;
        private float timer;
        private float totalDuration;
        private bool hasTriggeredHit;
        private bool hasTriggeredSound;
        private bool hasBufferedComboInput;

        public PlayerAttackState(PlayerController player, TopDownGame.StateMachine.StateMachine stateMachine) 
            : base(player, stateMachine) { }

        public void SetSkill(SkillData skill)
        {
            this.currentSkill = skill;
        }

        public override void Enter()
        {
            base.Enter();

            if (currentSkill == null || string.IsNullOrEmpty(currentSkill.ClipName))
            {
                stateMachine.ChangeState(player.IdleState);
                return;
            }

            timer = 0f;
            hasTriggeredHit = false;
            hasTriggeredSound = false;
            hasBufferedComboInput = false;

            // Chạy Animation clip từ LegacyAnimationController
            if (player.AnimationController != null)
            {
                float fade = currentSkill.crossFade > 0f ? Mathf.Clamp(currentSkill.crossFade, 0.05f, 0.15f) : 0.08f;
                player.AnimationController.PlayAction(currentSkill.ClipName, WrapMode.ClampForever, fade);
                totalDuration = player.AnimationController.GetClipDuration(currentSkill.ClipName);
            }
            else
            {
                totalDuration = 0.5f;
            }

            if (totalDuration <= 0.1f)
            {
                totalDuration = 0.6f;
            }

            // Khởi động việc xoay người theo hướng bấm phím
            player.ResetTurnVelocity();
            Vector3 input = player.GetInputVector();
            if (input.sqrMagnitude > 0.001f)
            {
                player.RotateTowards(input, player.AttackRotationSmoothTime);
            }

            // Kích hoạt hiệu ứng tụ khí / phát sáng vũ khí khi bắt đầu vung đòn (CastEffect)
            player.PlaySkillCastEffect(currentSkill);
        }

        public override void Update()
        {
            base.Update();

            if (currentSkill == null) return;

            timer += Time.deltaTime;

            // 1. Bẻ lái hướng đánh trước mốc castSkill (áp damage)
            float lockRotationTime = currentSkill.InstantDirTime >= 0f ? currentSkill.InstantDirTime : currentSkill.CastSkillTime;

            if (timer <= lockRotationTime)
            {
                Vector3 input = player.GetInputVector();
                if (input.sqrMagnitude > 0.001f)
                {
                    if (currentSkill.InstantDirTime >= 0f && timer >= currentSkill.InstantDirTime - Time.deltaTime)
                    {
                        player.RotateTowards(input, 0f);
                    }
                    else
                    {
                        player.RotateTowards(input, player.AttackRotationSmoothTime);
                    }
                }
            }

            // 2. Bước nhích tiến về phía trước (Forward Lunge - MovePos)
            if (currentSkill.movePosSpeed > 0f && currentSkill.movePosDistance > 0f)
            {
                float duration = currentSkill.movePosDistance / currentSkill.movePosSpeed;
                float moveStartTime = currentSkill.MovePosTime >= 0f ? currentSkill.MovePosTime : 0f;
                if (timer >= moveStartTime && timer < moveStartTime + duration)
                {
                    float timeInMove = timer - moveStartTime;
                    float progress = timeInMove / duration;
                    float currentSpeed = Mathf.Lerp(currentSkill.movePosSpeed, 0f, progress);
                    player.MoveWithSpeed(player.transform.forward, currentSpeed);
                }
            }

            // 3. Kích hoạt âm thanh tại mốc PlaySoundTime (nếu có cấu hình âm thanh)
            if (!hasTriggeredSound && currentSkill.HasSound && timer >= currentSkill.PlaySoundTime)
            {
                hasTriggeredSound = true;
                TopDownGame.Audio.SoundManager.Instance.PlaySkillSound(currentSkill, player.transform);
            }

            // 4. Kích hoạt gây sát thương tại mốc castSkill (frame/15s)
            if (!hasTriggeredHit && timer >= currentSkill.CastSkillTime)
            {
                hasTriggeredHit = true;
                player.ExecuteSkillDamage(currentSkill);
                player.OnHitTriggered(currentSkill);
            }

            // 5. Cửa sổ nhận lệnh combo đòn tiếp theo (nếu có cấu hình chiêu kế tiếp)
            if (currentSkill.HasCombo && (currentSkill.ComboEndTime < 0 || timer <= currentSkill.ComboEndTime))
            {
                // A. Nhận lệnh khi người chơi bấm thêm 1 click mới (AttackTriggered)
                if (player.InputReader != null && player.InputReader.AttackTriggered)
                {
                    hasBufferedComboInput = true;
                }
                // B. Hoặc nếu giữ chặt nút đánh (Auto-combo), chỉ buffer SAU KHI đòn hiện tại đã áp sát thương (hasTriggeredHit)
                else if (hasTriggeredHit && player.InputReader != null && player.InputReader.IsAttackHeld)
                {
                    hasBufferedComboInput = true;
                }
            }

            // Thực sự chuyển sang chiêu tiếp theo khi đã có lệnh combo và đạt mốc CastLinkSkillTime
            if (hasBufferedComboInput && currentSkill.CastLinkSkillTime >= 0 && timer >= currentSkill.CastLinkSkillTime)
            {
                TriggerNextCombo();
                return;
            }

            // 6. Cho phép phím Skill khác ngắt chiêu tại mốc candoskill nếu canCancel = true
            // Chỉ ngắt SAU KHI đòn hiện tại đã áp sát thương (hasTriggeredHit == true)
            if (hasTriggeredHit && currentSkill.CanCancelBySkill && timer >= currentSkill.CanDoSkillTime)
            {
                if (player.CheckAndTriggerSkills()) return;
            }

            // 7. Cho phép hủy hoạt ảnh sớm để di chuyển (Recovery Cancel / canDoRun):
            // - Tuyệt đối không hủy nếu đang buffer combo đòn kế tiếp
            // - Chỉ hủy sau khi đã áp sát thương (hasTriggeredHit == true)
            // - Đòn combo thường: cần hoàn thành tối thiểu 65% thời lượng hoạt ảnh
            // - Kỹ năng đặc biệt (Q, E, R): cần hoàn thành tối thiểu 85% thời lượng hoạt ảnh để trọn vẹn phép thuật
            if (!hasBufferedComboInput && hasTriggeredHit && currentSkill.CanCancelByRun)
            {
                float minActionDuration = currentSkill.HasCombo 
                    ? Mathf.Max(currentSkill.CanDoRunTime, totalDuration * 0.65f)
                    : Mathf.Max(currentSkill.CanDoRunTime, totalDuration * 0.85f);

                if (timer >= minActionDuration)
                {
                    Vector3 moveInput = player.GetInputVector();
                    if (moveInput.sqrMagnitude > 0.001f)
                    {
                        stateMachine.ChangeState(player.MoveState);
                        return;
                    }
                }
            }

            // 8. Hoàn thành toàn bộ thời lượng animation -> Về Move hoặc Idle
            if (timer >= totalDuration)
            {
                Vector3 input = player.GetInputVector();
                if (input.sqrMagnitude > 0.001f)
                {
                    stateMachine.ChangeState(player.MoveState);
                }
                else
                {
                    stateMachine.ChangeState(player.IdleState);
                }
            }
        }

        private void TriggerNextCombo()
        {
            if (currentSkill.NextComboSkillId > 0)
            {
                SkillData nextSkill = SkillDatabase.GetSkill(currentSkill.NextComboSkillId);
                if (nextSkill != null)
                {
                    bool success = player.ExecuteAction(nextSkill);
                    if (!success)
                    {
                        // Nếu chiêu tiếp theo thất bại (vì hết mana, quái chết nên ngắm hụt, v.v.)
                        // Xóa buffer để không bị kẹt vòng lặp, cho phép animation hiện tại chạy hết rồi về Idle
                        hasBufferedComboInput = false;
                    }
                }
                else
                {
                    stateMachine.ChangeState(player.IdleState);
                }
            }
            else
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
            currentSkill = null;
        }
    }
}



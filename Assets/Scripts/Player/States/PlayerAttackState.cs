using System.Collections.Generic;
using UnityEngine;
using TopDownGame.Skills;
using TopDownGame.Combat;

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
        private readonly HashSet<SkillEffectEvent> triggeredEvents = new HashSet<SkillEffectEvent>();

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

            // Chạy Animation clip từ LegacyAnimationController và scale theo NpcRes / AttackSpeed
            if (player.AnimationController != null)
            {
                float fade = currentSkill.crossFade > 0f ? currentSkill.crossFade : 0.08f;
                player.AnimationController.PlayAction(currentSkill, WrapMode.ClampForever, fade);
                totalDuration = player.AnimationController.GetClipDuration(currentSkill);
            }
            else
            {
                totalDuration = 0.5f;
            }

            if (totalDuration <= 0.1f)
            {
                totalDuration = 0.6f;
            }

            triggeredEvents.Clear();

            // Cập nhật hướng xoay ban đầu khi vung đòn theo InstantDir (DATA_CONVENTIONS.md Mục 1 & 4)
            player.ResetTurnVelocity();
            player.RotateTowardsCastDirection(currentSkill.instantDirSpeed);

            // Kích hoạt hiệu ứng tụ khí / phát sáng vũ khí khi bắt đầu vung đòn (CastEffect)
            player.PlaySkillCastEffect(currentSkill);
            if (currentSkill.effectEvents != null)
            {
                foreach (var ev in currentSkill.effectEvents)
                {
                    if (ev.frame <= 0)
                    {
                        triggeredEvents.Add(ev);
                    }
                }
            }
        }

        public override void Update()
        {
            base.Update();

            if (currentSkill == null) return;

            timer += Time.deltaTime;

            // Hệ số tăng tốc hoạt ảnh theo AttackSpeed (DATA_CONVENTIONS_V2.md Mục 6 - SkillSetting.ini L83-L95)
            float speedFactor = 1.0f;
            if (player.Stats != null && !currentSkill.notChangeActFrame)
            {
                int originalFrame = 15;
                var resData = player.AnimationController != null ? player.AnimationController.GetNpcResData() : null;
                if (resData != null && player.AnimationController != null && player.AnimationController.TryGetActionFrame(resData, currentSkill.ClipName, out int actFrame))
                {
                    originalFrame = actFrame;
                }
                var (_, factor) = CombatFormula.CalculateScaledActionFrame(originalFrame, player.Stats.AttackSpeed);
                speedFactor = factor;
                if (speedFactor < 0.1f) speedFactor = 0.1f;
            }

            // 1. Tự động xoay mặt về hướng thi triển theo InstantDir (độ/giây theo DATA_CONVENTIONS.md Mục 1 & 4)
            // Hướng thi triển và điểm đích đã được chốt cố định ngay khi nhấn chiêu, chuột di chuyển sau đó không ảnh hưởng.
            float castSkillTimeScaled = currentSkill.CastSkillTime / speedFactor;
            if (timer <= castSkillTimeScaled)
            {
                player.RotateTowardsCastDirection(currentSkill.instantDirSpeed);
            }

            // 2. Bước nhích tiến về phía trước (Forward Lunge - MovePos)
            if (currentSkill.movePosSpeed > 0f && currentSkill.movePosDistance > 0f)
            {
                float duration = (currentSkill.movePosDistance / currentSkill.movePosSpeed) / speedFactor;
                float moveStartTime = (currentSkill.MovePosTime >= 0f ? currentSkill.MovePosTime : 0f) / speedFactor;
                if (timer >= moveStartTime && timer < moveStartTime + duration)
                {
                    float timeInMove = timer - moveStartTime;
                    float progress = timeInMove / duration;
                    float currentSpeed = Mathf.Lerp(currentSkill.movePosSpeed * speedFactor, 0f, progress);
                    player.MoveWithSpeed(player.transform.forward, currentSpeed);
                }
            }

            // 3. Kích hoạt âm thanh tại mốc PlaySoundTime (nếu có cấu hình âm thanh)
            if (!hasTriggeredSound && currentSkill.HasSound && timer >= (currentSkill.PlaySoundTime / speedFactor))
            {
                hasTriggeredSound = true;
                TopDownGame.Audio.SoundManager.Instance.PlaySkillSound(currentSkill, player.transform);
            }

            // 3.5. Kích hoạt các hiệu ứng timeline (frame > 0)
            if (currentSkill.effectEvents != null && currentSkill.effectEvents.Count > 0)
            {
                foreach (var ev in currentSkill.effectEvents)
                {
                    if (!triggeredEvents.Contains(ev))
                    {
                        float evTime = CombatFormula.FrameToSeconds(ev.frame) / speedFactor;
                        if (timer >= evTime)
                        {
                            triggeredEvents.Add(ev);
                            player.PlaySkillEffectEvent(ev);
                        }
                    }
                }
            }

            // 4. Kích hoạt gây sát thương tại mốc castSkill (frame/15s)
            if (!hasTriggeredHit && timer >= (currentSkill.CastSkillTime / speedFactor))
            {
                hasTriggeredHit = true;
                player.ExecuteSkillDamage(currentSkill);
                player.OnHitTriggered(currentSkill);
            }

            // 5. Cửa sổ nhận lệnh combo đòn tiếp theo (nếu có cấu hình chiêu kế tiếp)
            float comboEndTimeScaled = currentSkill.ComboEndTime >= 0f ? (currentSkill.ComboEndTime / speedFactor) : -1f;
            if (currentSkill.HasCombo && (comboEndTimeScaled < 0 || timer <= comboEndTimeScaled))
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
            float castLinkScaled = currentSkill.CastLinkSkillTime >= 0f ? (currentSkill.CastLinkSkillTime / speedFactor) : -1f;
            if (hasBufferedComboInput && castLinkScaled >= 0 && timer >= castLinkScaled)
            {
                TriggerNextCombo();
                return;
            }

            // 6. Cho phép phím Skill khác ngắt chiêu tại mốc candoskill nếu canCancel = true
            // Chỉ ngắt SAU KHI đòn hiện tại đã áp sát thương (hasTriggeredHit == true)
            float canDoSkillScaled = currentSkill.CanDoSkillTime >= 0f ? (currentSkill.CanDoSkillTime / speedFactor) : -1f;
            if (hasTriggeredHit && currentSkill.CanCancelBySkill && timer >= canDoSkillScaled)
            {
                if (player.CheckAndTriggerSkills()) return;
            }

            // 7. Cho phép hủy hoạt ảnh sớm để di chuyển (Animation Cancel / CanDoRun theo DATA_CONVENTIONS.md Mục 4 & 5):
            // - Tuyệt đối không hủy nếu đang buffer combo đòn kế tiếp
            // - Chỉ hủy sau khi đã áp sát thương (hasTriggeredHit == true) và đạt mốc canDoRun quy định trong ActionEvent.csv
            if (!hasBufferedComboInput && hasTriggeredHit && currentSkill.CanCancelByRun)
            {
                float canDoRunScaled = currentSkill.CanDoRunTime / speedFactor;
                if (timer >= canDoRunScaled)
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



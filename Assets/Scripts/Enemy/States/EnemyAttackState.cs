using UnityEngine;
using TopDownGame.Skills;

namespace TopDownGame.Enemy
{
    public class EnemyAttackState : EnemyBaseState
    {
        private SkillData currentSkill;
        private float timer;
        private float totalDuration;
        private bool hasTriggeredHit;
        private bool hasTriggeredSound;

        public EnemyAttackState(EnemyController enemy, TopDownGame.StateMachine.StateMachine stateMachine) 
            : base(enemy, stateMachine) { }

        public void SetSkill(SkillData skill)
        {
            this.currentSkill = skill;
        }

        public override void Enter()
        {
            base.Enter();

            if (currentSkill == null || string.IsNullOrEmpty(currentSkill.ClipName))
            {
                stateMachine.ChangeState(enemy.IdleState);
                return;
            }

            timer = 0f;
            hasTriggeredHit = false;
            hasTriggeredSound = false;

            // Xoay dứt khoát về phía người chơi khi bắt đầu ra đòn
            enemy.RotateTowardsTarget();

            // Kích hoạt Animation
            if (enemy.AnimationController != null)
            {
                enemy.AnimationController.PlayAction(currentSkill, WrapMode.ClampForever, currentSkill.crossFade);
                totalDuration = enemy.AnimationController.GetClipDuration(currentSkill);
            }
            else
            {
                totalDuration = 0.8f;
            }
        }

        public override void Update()
        {
            base.Update();

            if (currentSkill == null) return;

            timer += Time.deltaTime;

            // 1. Nhích tiến nếu có MovePos
            if (currentSkill.movePosSpeed > 0f && currentSkill.movePosDistance > 0f)
            {
                float duration = currentSkill.movePosDistance / currentSkill.movePosSpeed;
                float moveStartTime = currentSkill.MovePosTime >= 0f ? currentSkill.MovePosTime : 0f;
                if (timer >= moveStartTime && timer < moveStartTime + duration)
                {
                    float timeInMove = timer - moveStartTime;
                    float progress = timeInMove / duration;
                    float currentSpeed = Mathf.Lerp(currentSkill.movePosSpeed, 0f, progress);
                    if (enemy.CharacterController != null && enemy.CharacterController.enabled && enemy.CharacterController.gameObject.activeInHierarchy)
                    {
                        enemy.CharacterController.Move(enemy.transform.forward * currentSpeed * Time.deltaTime);
                    }
                }
            }

            // 2. Kích hoạt âm thanh (nếu có cấu hình âm thanh)
            if (!hasTriggeredSound && currentSkill.HasSound && timer >= currentSkill.PlaySoundTime)
            {
                hasTriggeredSound = true;
                TopDownGame.Audio.SoundManager.Instance.PlaySkillSound(currentSkill, enemy.transform);
            }

            // 3. Gây sát thương tại mốc castSkill (frame/15s)
            if (!hasTriggeredHit && timer >= currentSkill.CastSkillTime)
            {
                hasTriggeredHit = true;
                enemy.ExecuteSkillDamage(currentSkill);
            }

            // 4. Kết thúc đòn đánh sau khi hết thời lượng animation hoặc đến mốc canDoRun
            bool isFinished = timer >= totalDuration;
            if (currentSkill.CanCancelByRun && timer >= currentSkill.CanDoRunTime)
            {
                isFinished = true;
            }

            if (isFinished)
            {
                enemy.StartCooldown(currentSkill.id, currentSkill.cooldown);

                if (enemy.Target != null && enemy.GetDistanceToTarget() <= enemy.DetectionRange)
                {
                    stateMachine.ChangeState(enemy.ChaseState);
                }
                else
                {
                    stateMachine.ChangeState(enemy.IdleState);
                }
            }
        }

        public override void Exit()
        {
            base.Exit();
            if (enemy.AnimationController != null)
            {
                enemy.AnimationController.ForceResetClip();
            }
            currentSkill = null;
        }
    }
}

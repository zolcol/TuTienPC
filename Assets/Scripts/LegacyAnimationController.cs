using System;
using UnityEngine;
using TopDownGame.Skills;
using TopDownGame.Combat;
using TopDownGame.Stats;
using TopDownGame.Data;

namespace TopDownGame
{
    /// <summary>
    /// Component điều khiển và phát hoạt ảnh kế thừa (Legacy Animation Component) cho Nhân vật và Quái vật.
    /// Đảm bảo Zero GC Alloc trong quá trình chạy, tương thích chuẩn 15 FPS và tốc đánh (CombatFormula).
    /// </summary>
    public class LegacyAnimationController : MonoBehaviour
    {
        public const string CLIP_STAND = "st";          // Đứng chờ phi chiến đấu (Normal Stand / Idle)
        public const string CLIP_BATTLE_STAND = "sta";   // Đứng thủ thế chiến đấu (Battle Idle / Combat Ready)
        public const string CLIP_RUN = "run";            // Chạy bộ (Run)
        public const string CLIP_WALK = "wlk";           // Đi bộ / Tản bộ (Walk)
        public const string CLIP_DIE = "die";            // Tử vong / Gục ngã (Die)
        public const string CLIP_HURT = "bat";          // Bị thương / Giật mình tại chỗ (Hit Flinch - ActId 9)

        [Header("Animation Components (Đầu & Thân)")]
        [Tooltip("Component Animation gắn trên GameObject Thân (tự tìm nếu để trống)")]
        [SerializeField] private Animation bodyAnimation;

        [Tooltip("Component Animation gắn trên GameObject Đầu (tự tìm nếu để trống)")]
        [SerializeField] private Animation headAnimation;

        [Header("Fade / Blending Settings")]
        [Tooltip("Thời gian chuyển mượt giữa Đứng yên và Chạy (khuyên dùng 0.2s - 0.25s)")]
        [SerializeField] private float moveCrossFadeTime = 0.25f;

        [Tooltip("Thời gian chuyển mượt khi tung chiêu / đánh thường (khuyên dùng 0.1s - 0.15s)")]
        [SerializeField] private float actionCrossFadeTime = 0.15f;

        private string currentClip = "";
        private bool isLocked = false;

        private TopDownGame.Player.PlayerController cachedPlayer;
        private TopDownGame.Enemy.EnemyController cachedEnemy;
        private EntityStats cachedStats;
        private NpcResData cachedNpcResData;

        public string CurrentClip => currentClip;
        public bool IsLocked => isLocked;
        public Animation BodyAnimation => bodyAnimation;
        public Animation HeadAnimation => headAnimation;

        private void Awake()
        {
            AutoFindAnimationComponents();
            CacheReferences();
        }

        public void CacheReferences()
        {
            if (cachedPlayer == null)
                cachedPlayer = GetComponent<TopDownGame.Player.PlayerController>() ?? GetComponentInParent<TopDownGame.Player.PlayerController>();

            if (cachedEnemy == null && cachedPlayer == null)
                cachedEnemy = GetComponent<TopDownGame.Enemy.EnemyController>() ?? GetComponentInParent<TopDownGame.Enemy.EnemyController>();

            if (cachedStats == null)
            {
                if (cachedPlayer != null) cachedStats = cachedPlayer.Stats;
                else if (cachedEnemy != null) cachedStats = cachedEnemy.Stats;
                else cachedStats = GetComponent<EntityStats>() ?? GetComponentInParent<EntityStats>();
            }

            if (cachedNpcResData == null)
                GetNpcResData();
        }

        private EntityStats GetEntityStats()
        {
            if (cachedStats != null) return cachedStats;
            CacheReferences();
            return cachedStats;
        }

        public void AutoFindAnimationComponents()
        {
            Animation[] allAnims = GetComponentsInChildren<Animation>(true);
            if (allAnims != null && allAnims.Length > 0)
            {
                bodyAnimation = null;
                headAnimation = null;

                foreach (var anim in allAnims)
                {
                    string objName = anim.gameObject.name.ToLowerInvariant();
                    if (objName.Contains("head") || objName.Contains("tou"))
                    {
                        headAnimation = anim;
                    }
                    else if (objName.Contains("body") || objName.Contains("shen") || bodyAnimation == null)
                    {
                        if (bodyAnimation == null || objName.Contains("body") || objName.Contains("shen"))
                        {
                            bodyAnimation = anim;
                        }
                    }
                }

                if (allAnims.Length >= 2 && headAnimation == null && bodyAnimation != null)
                {
                    foreach (var anim in allAnims)
                    {
                        if (anim != bodyAnimation)
                        {
                            headAnimation = anim;
                            break;
                        }
                    }
                }
            }
            else if (transform.parent != null)
            {
                bodyAnimation = transform.parent.GetComponentInChildren<Animation>();
            }

            ConfigureAnimationSettings(bodyAnimation);
            ConfigureAnimationSettings(headAnimation);
            cachedNpcResData = null;
        }

        private void ConfigureAnimationSettings(Animation animComp)
        {
            if (animComp == null) return;
            animComp.enabled = true;
            animComp.cullingType = AnimationCullingType.AlwaysAnimate;
            foreach (AnimationState state in animComp)
            {
                state.blendMode = AnimationBlendMode.Blend;
            }
        }

        private string FindDirectClip(string targetClip)
        {
            if (string.IsNullOrEmpty(targetClip)) return null;

            if (bodyAnimation != null)
            {
                if (bodyAnimation[targetClip] != null) return targetClip;
                foreach (AnimationState state in bodyAnimation)
                {
                    if (string.Equals(state.name, targetClip, StringComparison.OrdinalIgnoreCase))
                        return state.name;
                }
            }

            if (headAnimation != null)
            {
                if (headAnimation[targetClip] != null) return targetClip;
                foreach (AnimationState state in headAnimation)
                {
                    if (string.Equals(state.name, targetClip, StringComparison.OrdinalIgnoreCase))
                        return state.name;
                }
            }

            return null;
        }

        public string ResolveClipName(string targetClip)
        {
            if (string.IsNullOrEmpty(targetClip)) return null;

            string found = FindDirectClip(targetClip);
            if (!string.IsNullOrEmpty(found)) return found;

            // Fallback thông minh theo chuẩn quy ước CastActionID
            string lower = targetClip.ToLowerInvariant();
            if (lower == "st") found = FindDirectClip("sta");
            else if (lower == "sta") found = FindDirectClip("st");
            else if (lower.StartsWith("at0") || lower.StartsWith("at1")) found = FindDirectClip("at");
            else if (lower == "at") found = FindDirectClip("at01") ?? FindDirectClip("at02");
            else if (lower == "run") found = FindDirectClip("wlk") ?? FindDirectClip("jsrun");
            else if (lower == "wlk") found = FindDirectClip("run");
            else if (lower == "die") found = FindDirectClip("jfd");
            else if (lower == "jfd") found = FindDirectClip("die");
            else if (lower == "bat") found = FindDirectClip("jt");
            else if (lower == "jt") found = FindDirectClip("bat");

            return found;
        }

        public bool HasClip(string clipName)
        {
            return !string.IsNullOrEmpty(ResolveClipName(clipName));
        }

        public void PlayIdle()
        {
            if (isLocked) return;
            string clip = ResolveClipName(CLIP_STAND) ?? ResolveClipName(CLIP_BATTLE_STAND);
            if (string.IsNullOrEmpty(clip) && bodyAnimation != null && bodyAnimation.clip != null)
                clip = bodyAnimation.clip.name;

            if (string.IsNullOrEmpty(clip)) return;
            if (currentClip == clip && bodyAnimation != null && bodyAnimation.IsPlaying(clip)) return;
            PlayActionInternal(clip, WrapMode.Loop, moveCrossFadeTime, false);
        }

        public void PlayBattleIdle()
        {
            if (isLocked) return;
            string clip = ResolveClipName(CLIP_BATTLE_STAND) ?? ResolveClipName(CLIP_STAND);
            if (string.IsNullOrEmpty(clip) && bodyAnimation != null && bodyAnimation.clip != null)
                clip = bodyAnimation.clip.name;

            if (string.IsNullOrEmpty(clip)) return;
            if (currentClip == clip && bodyAnimation != null && bodyAnimation.IsPlaying(clip)) return;
            PlayActionInternal(clip, WrapMode.Loop, moveCrossFadeTime, false);
        }

        public void PlayRun()
        {
            if (isLocked) return;
            string clip = ResolveClipName(CLIP_RUN) ?? ResolveClipName(CLIP_WALK) ?? ResolveClipName("jsrun");
            if (string.IsNullOrEmpty(clip)) return;
            if (currentClip == clip && bodyAnimation != null && bodyAnimation.IsPlaying(clip)) return;
            PlayActionInternal(clip, WrapMode.Loop, moveCrossFadeTime, false);
        }

        public void PlayWalk()
        {
            if (isLocked) return;
            string clip = ResolveClipName(CLIP_WALK) ?? ResolveClipName(CLIP_RUN);
            if (string.IsNullOrEmpty(clip)) return;
            if (currentClip == clip && bodyAnimation != null && bodyAnimation.IsPlaying(clip)) return;
            PlayActionInternal(clip, WrapMode.Loop, moveCrossFadeTime, false);
        }

        public void PlayDie()
        {
            string clip = ResolveClipName(CLIP_DIE) ?? ResolveClipName("jfd");
            if (string.IsNullOrEmpty(clip)) return;
            PlayActionInternal(clip, WrapMode.ClampForever, actionCrossFadeTime, true, null);
        }

        public void PlayHurt(float customFadeTime = -1f)
        {
            string clip = ResolveClipName(CLIP_HURT) ?? ResolveClipName("jt");
            if (string.IsNullOrEmpty(clip)) return;
            float fadeDuration = customFadeTime >= 0f ? customFadeTime : 0.05f;
            PlayActionInternal(clip, WrapMode.ClampForever, fadeDuration, true, null);
        }

        public void PlayAction(SkillData skill, WrapMode wrapMode = WrapMode.Once, float customFadeTime = -1f)
        {
            if (skill == null) return;
            float fadeDuration = customFadeTime >= 0f ? customFadeTime : (skill.crossFade > 0f ? skill.crossFade : actionCrossFadeTime);
            PlayActionInternal(skill.ClipName, wrapMode, fadeDuration, true, skill);
        }

        public void PlayAction(string clipName, WrapMode wrapMode = WrapMode.Once, float customFadeTime = -1f)
        {
            if (string.IsNullOrEmpty(clipName)) return;
            float fadeDuration = customFadeTime >= 0f ? customFadeTime : actionCrossFadeTime;
            PlayActionInternal(clipName, wrapMode, fadeDuration, true, null);
        }

        public void PlayAction(CastActionID actionId, WrapMode wrapMode = WrapMode.Once, float customFadeTime = -1f)
        {
            string clip = CastActionHelper.GetClipName(actionId);
            PlayAction(clip, wrapMode, customFadeTime);
        }

        public void PlayAction(int actionId, WrapMode wrapMode = WrapMode.Once, float customFadeTime = -1f)
        {
            string clip = CastActionHelper.GetClipName(actionId);
            PlayAction(clip, wrapMode, customFadeTime);
        }

        public NpcResData GetNpcResData()
        {
            if (cachedNpcResData != null) return cachedNpcResData;

            int resId = 0;
            if (cachedPlayer == null && cachedEnemy == null)
            {
                cachedPlayer = GetComponent<TopDownGame.Player.PlayerController>() ?? GetComponentInParent<TopDownGame.Player.PlayerController>();
                if (cachedPlayer == null)
                    cachedEnemy = GetComponent<TopDownGame.Enemy.EnemyController>() ?? GetComponentInParent<TopDownGame.Enemy.EnemyController>();
            }

            if (cachedPlayer != null) resId = cachedPlayer.NpcResId;
            else if (cachedEnemy != null) resId = cachedEnemy.NpcResId;

            if (resId > 0)
                cachedNpcResData = NpcResDatabase.GetRes(resId);

            if (cachedNpcResData == null)
                cachedNpcResData = NpcResDatabase.GetResByName(gameObject.name);

            return cachedNpcResData;
        }

        public bool TryGetActionFrame(NpcResData resData, string clipName, out int targetFrame)
        {
            targetFrame = 0;
            if (resData == null || resData.ActionFrames == null || string.IsNullOrEmpty(clipName)) return false;
            return resData.ActionFrames.TryGetValue(clipName, out targetFrame) && targetFrame > 0;
        }

        public bool TryGetActionCrossFade(NpcResData resData, string clipName, out float crossFade)
        {
            crossFade = -1f;
            if (resData == null || resData.ActionCrossFades == null || string.IsNullOrEmpty(clipName)) return false;
            return resData.ActionCrossFades.TryGetValue(clipName, out crossFade) && crossFade >= 0f;
        }

        private void PlayActionInternal(string clipName, WrapMode wrapMode, float fadeTime, bool forceRewind, SkillData skill = null)
        {
            string realClip = ResolveClipName(clipName);
            if (string.IsNullOrEmpty(realClip))
            {
                Debug.LogWarning($"[LegacyAnimation] ⚠️ Không tìm thấy clip '{clipName}' trong model '{gameObject.name}'.");
                return;
            }

            currentClip = realClip;
            NpcResData resData = GetNpcResData();
            float targetDuration = 0f;
            int targetFrame = 0;

            if (resData != null)
            {
                string key = !string.IsNullOrEmpty(realClip) ? realClip : clipName;
                if (TryGetActionFrame(resData, key, out targetFrame))
                    targetDuration = CombatFormula.FrameToSeconds(targetFrame);

                if (TryGetActionCrossFade(resData, key, out float targetCross))
                    fadeTime = targetCross;
            }

            float attackSpeedPercent = 0f;
            if (skill == null || !skill.notChangeActFrame)
            {
                var stats = GetEntityStats();
                if (stats != null) attackSpeedPercent = stats.AttackSpeed;
            }

            if (targetFrame > 0)
            {
                targetDuration = CombatFormula.CalculateActionDuration(targetFrame, attackSpeedPercent);
            }

            CrossFadeOnComponent(bodyAnimation, realClip, wrapMode, fadeTime, forceRewind, targetDuration);
            CrossFadeOnComponent(headAnimation, realClip, wrapMode, fadeTime, forceRewind, targetDuration);
        }

        public static (int finalFrame, float speedFactor) CalculateScaledActionFrame(int originalFrame, float attackSpeedPercent)
        {
            return CombatFormula.CalculateScaledActionFrame(originalFrame, attackSpeedPercent);
        }

        private void CrossFadeOnComponent(Animation animComp, string clipName, WrapMode wrapMode, float fadeTime, bool forceRewind, float targetDuration)
        {
            if (animComp == null) return;

            AnimationState state = animComp[clipName];
            if (state != null)
            {
                state.enabled = true;
                state.wrapMode = wrapMode;
                state.blendMode = AnimationBlendMode.Blend;

                if (targetDuration > 0f && state.clip != null && state.clip.length > 0f)
                    state.speed = state.clip.length / targetDuration;
                else
                    state.speed = 1.0f;

                if (forceRewind) state.time = 0f;

                if (fadeTime > 0f)
                    animComp.CrossFade(clipName, fadeTime, PlayMode.StopSameLayer);
                else
                {
                    state.weight = 1f;
                    animComp.Play(clipName);
                }
            }
        }

        public float GetClipDuration(SkillData skill)
        {
            if (skill == null) return 0.5f;
            return GetClipDuration(skill.ClipName, skill);
        }

        public float GetClipDuration(string clipName, SkillData skill = null)
        {
            string realClip = ResolveClipName(clipName);
            NpcResData resData = GetNpcResData();

            float attackSpeedPercent = 0f;
            if (skill == null || !skill.notChangeActFrame)
            {
                var stats = GetEntityStats();
                if (stats != null) attackSpeedPercent = stats.AttackSpeed;
            }

            string key = !string.IsNullOrEmpty(realClip) ? realClip : clipName;
            if (resData != null && TryGetActionFrame(resData, key, out int targetFrame))
            {
                return CombatFormula.CalculateActionDuration(targetFrame, attackSpeedPercent);
            }

            if (!string.IsNullOrEmpty(realClip))
            {
                if (bodyAnimation != null && bodyAnimation[realClip] != null)
                {
                    float length = bodyAnimation[realClip].length;
                    float speed = bodyAnimation[realClip].speed;
                    return speed > 0f ? length / speed : length;
                }

                if (headAnimation != null && headAnimation[realClip] != null)
                {
                    float length = headAnimation[realClip].length;
                    float speed = headAnimation[realClip].speed;
                    return speed > 0f ? length / speed : length;
                }
            }

            return 0.5f;
        }

        public void ForceResetClip()
        {
            currentClip = "";
        }
    }
}

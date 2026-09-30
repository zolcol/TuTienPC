using System;
using UnityEngine;
using TopDownGame.Skills;
using TopDownGame.Combat;
using TopDownGame.Stats;

namespace TopDownGame
{
    public class LegacyAnimationController : MonoBehaviour
    {
        // =========================================================================
        // TÊN ANIMATION CHUẨN HOÁ TOÀN BỘ GAME (Không cần nhập tay trong Inspector)
        // =========================================================================
        public const string CLIP_STAND = "st";          // Đứng chờ phi chiến đấu (Normal Stand / Idle)
        public const string CLIP_BATTLE_STAND = "sta";   // Đứng thủ thế chiến đấu (Battle Idle / Combat Ready)
        public const string CLIP_RUN = "run";            // Chạy bộ (Run)
        public const string CLIP_WALK = "wlk";           // Đi bộ / Tản bộ (Walk)
        public const string CLIP_DIE = "die";            // Tử vong / Gục ngã (Die)

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
        private TopDownGame.Data.NpcResData cachedNpcResData;

        public string CurrentClip => currentClip;
        public bool IsLocked => isLocked;
        public Animation BodyAnimation => bodyAnimation;

        private void Awake()
        {
            AutoFindAnimationComponents();
            CacheReferences();
        }

        public void CacheReferences()
        {
            if (cachedPlayer == null)
            {
                cachedPlayer = GetComponent<TopDownGame.Player.PlayerController>() ?? GetComponentInParent<TopDownGame.Player.PlayerController>();
            }
            if (cachedEnemy == null && cachedPlayer == null)
            {
                cachedEnemy = GetComponent<TopDownGame.Enemy.EnemyController>() ?? GetComponentInParent<TopDownGame.Enemy.EnemyController>();
            }
            if (cachedStats == null)
            {
                if (cachedPlayer != null) cachedStats = cachedPlayer.Stats;
                else if (cachedEnemy != null) cachedStats = cachedEnemy.Stats;
                else cachedStats = GetComponent<EntityStats>() ?? GetComponentInParent<EntityStats>();
            }
            if (cachedNpcResData == null)
            {
                GetNpcResData();
            }
        }

        private EntityStats GetEntityStats()
        {
            if (cachedStats != null) return cachedStats;
            CacheReferences();
            return cachedStats;
        }

        public void AutoFindAnimationComponents()
        {
            if (bodyAnimation == null)
            {
                // 1. Thử tìm trên chính mình hoặc các con
                bodyAnimation = GetComponentInChildren<Animation>();

                // 2. Thử tìm trên cha nếu script được gắn ở cấp root
                if (bodyAnimation == null && transform.parent != null)
                {
                    bodyAnimation = transform.parent.GetComponentInChildren<Animation>();
                }
            }

            if (headAnimation == null)
            {
                Transform headTf = transform.Find("Head");
                if (headTf != null)
                {
                    headAnimation = headTf.GetComponentInChildren<Animation>();
                }
            }

            ConfigureAnimationSettings(bodyAnimation);
            ConfigureAnimationSettings(headAnimation);
        }

        private void ConfigureAnimationSettings(Animation animComp)
        {
            if (animComp == null) return;

            animComp.cullingType = AnimationCullingType.AlwaysAnimate;

            foreach (AnimationState state in animComp)
            {
                state.blendMode = AnimationBlendMode.Blend;
            }
        }

        /// <summary>
        /// Tìm tên Animation Clip thực tế có sẵn trên model (hỗ trợ không phân biệt hoa/thường)
        /// </summary>
        public string ResolveClipName(string targetClip)
        {
            if (string.IsNullOrEmpty(targetClip)) return null;

            if (bodyAnimation != null)
            {
                if (bodyAnimation[targetClip] != null) return targetClip;

                foreach (AnimationState state in bodyAnimation)
                {
                    if (string.Equals(state.name, targetClip, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return state.name;
                    }
                }
            }

            if (headAnimation != null)
            {
                if (headAnimation[targetClip] != null) return targetClip;

                foreach (AnimationState state in headAnimation)
                {
                    if (string.Equals(state.name, targetClip, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return state.name;
                    }
                }
            }

#if UNITY_EDITOR
            // Tự động tìm kiếm và nạp Clip vào Animation Component nếu trong Editor chưa kịp gán
            string resolved = TryFindAndAddMissingClip(targetClip);
            if (!string.IsNullOrEmpty(resolved)) return resolved;
#endif

            return null;
        }

#if UNITY_EDITOR
        private string TryFindAndAddMissingClip(string clipName)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"{clipName} t:AnimationClip");
            AnimationClip foundBodyClip = null;
            AnimationClip foundHeadClip = null;

            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path).Equals(clipName, System.StringComparison.OrdinalIgnoreCase))
                {
                    var loadedClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if (loadedClip != null)
                    {
                        if (path.IndexOf("head", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            foundHeadClip = loadedClip;
                        }
                        else
                        {
                            foundBodyClip = loadedClip;
                        }
                    }
                }
            }

            if (foundBodyClip != null && bodyAnimation != null)
            {
                bodyAnimation.AddClip(foundBodyClip, foundBodyClip.name);
            }
            if (foundHeadClip != null && headAnimation != null)
            {
                headAnimation.AddClip(foundHeadClip, foundHeadClip.name);
            }

            if (foundBodyClip != null) return foundBodyClip.name;
            if (foundHeadClip != null) return foundHeadClip.name;
            return null;
        }

        [ContextMenu("Tự động nạp toàn bộ Animation Clips cho Model")]
        public void AutoPopulateAllClips()
        {
            AutoFindAnimationComponents();

            int bodyAdded = 0;
            int headAdded = 0;

            if (bodyAnimation != null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/Animation/player/f2_em_body" });
                foreach (var g in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                    var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if (clip != null && bodyAnimation.GetClip(clip.name) == null)
                    {
                        bodyAnimation.AddClip(clip, clip.name);
                        bodyAdded++;
                    }
                }
            }

            if (headAnimation != null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/Animation/player/f2_em_head" });
                foreach (var g in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                    var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if (clip != null && headAnimation.GetClip(clip.name) == null)
                    {
                        headAnimation.AddClip(clip, clip.name);
                        headAdded++;
                    }
                }
            }

            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log($"✅ <color=green>[LegacyAnimation]</color> Đã nạp thành công <b>{bodyAdded}</b> clips cho Body và <b>{headAdded}</b> clips cho Head của {gameObject.name}!");
        }
#endif

        public bool HasClip(string clipName)
        {
            return !string.IsNullOrEmpty(ResolveClipName(clipName));
        }

        /// <summary>
        /// Đứng chờ phi chiến đấu bình thường (st).
        /// Nếu model không có st, tự động fallback sang sta (thủ thế) hoặc clip mặc định.
        /// </summary>
        public void PlayIdle()
        {
            if (isLocked) return;

            string clip = ResolveClipName(CLIP_STAND);
            if (string.IsNullOrEmpty(clip))
            {
                clip = ResolveClipName(CLIP_BATTLE_STAND);
                if (string.IsNullOrEmpty(clip) && bodyAnimation != null && bodyAnimation.clip != null)
                {
                    clip = bodyAnimation.clip.name;
                }
            }

            if (string.IsNullOrEmpty(clip)) return;
            if (currentClip == clip) return;

            PlayActionInternal(clip, WrapMode.Loop, moveCrossFadeTime, false);
        }

        /// <summary>
        /// Đứng thủ thế chiến đấu (sta) - Dùng khi đang giao tranh hoặc chờ hồi chiêu bên cạnh mục tiêu.
        /// Nếu model không có sta, tự động fallback sang st hoặc clip mặc định.
        /// </summary>
        public void PlayBattleIdle()
        {
            if (isLocked) return;

            string clip = ResolveClipName(CLIP_BATTLE_STAND);
            if (string.IsNullOrEmpty(clip))
            {
                clip = ResolveClipName(CLIP_STAND);
                if (string.IsNullOrEmpty(clip) && bodyAnimation != null && bodyAnimation.clip != null)
                {
                    clip = bodyAnimation.clip.name;
                }
            }

            if (string.IsNullOrEmpty(clip)) return;
            if (currentClip == clip) return;

            PlayActionInternal(clip, WrapMode.Loop, moveCrossFadeTime, false);
        }

        /// <summary>
        /// Di chuyển chạy bộ (run).
        /// Fallback sang wlk hoặc jsrun nếu model không có run.
        /// </summary>
        public void PlayRun()
        {
            if (isLocked) return;

            string clip = ResolveClipName(CLIP_RUN);
            if (string.IsNullOrEmpty(clip))
            {
                clip = ResolveClipName(CLIP_WALK) ?? ResolveClipName("jsrun");
            }

            if (string.IsNullOrEmpty(clip)) return;
            if (currentClip == clip) return;

            PlayActionInternal(clip, WrapMode.Loop, moveCrossFadeTime, false);
        }

        /// <summary>
        /// Đi bộ / tản bộ (wlk).
        /// Fallback sang run nếu model không có wlk.
        /// </summary>
        public void PlayWalk()
        {
            if (isLocked) return;

            string clip = ResolveClipName(CLIP_WALK) ?? ResolveClipName(CLIP_RUN);
            if (string.IsNullOrEmpty(clip)) return;
            if (currentClip == clip) return;

            PlayActionInternal(clip, WrapMode.Loop, moveCrossFadeTime, false);
        }

        /// <summary>
        /// Tử vong / Chết (die).
        /// Fallback sang jfd nếu model không có die.
        /// </summary>
        public void PlayDie()
        {
            string clip = ResolveClipName(CLIP_DIE) ?? ResolveClipName("jfd");
            if (string.IsNullOrEmpty(clip)) return;

            PlayActionInternal(clip, WrapMode.ClampForever, actionCrossFadeTime, true, null);
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

        public TopDownGame.Data.NpcResData GetNpcResData()
        {
            if (cachedNpcResData != null) return cachedNpcResData;

            int resId = 0;
            if (cachedPlayer == null && cachedEnemy == null)
            {
                cachedPlayer = GetComponent<TopDownGame.Player.PlayerController>() ?? GetComponentInParent<TopDownGame.Player.PlayerController>();
                if (cachedPlayer == null)
                {
                    cachedEnemy = GetComponent<TopDownGame.Enemy.EnemyController>() ?? GetComponentInParent<TopDownGame.Enemy.EnemyController>();
                }
            }

            if (cachedPlayer != null)
            {
                resId = cachedPlayer.NpcResId;
            }
            else if (cachedEnemy != null)
            {
                resId = cachedEnemy.NpcResId;
            }

            if (resId > 0)
            {
                cachedNpcResData = TopDownGame.Data.NpcResDatabase.GetRes(resId);
            }
            if (cachedNpcResData == null)
            {
                cachedNpcResData = TopDownGame.Data.NpcResDatabase.GetResByName(gameObject.name);
            }

            return cachedNpcResData;
        }

        /// <summary>
        /// Tra cứu số frame chuẩn (action_frame) của clip trong NpcResData theo DATA_CONVENTIONS.md Mục 5 & 7.
        /// Tên clip đã được chuẩn hóa trong toàn bộ dự án (at01, at02, run, st,...).
        /// </summary>
        public bool TryGetActionFrame(TopDownGame.Data.NpcResData resData, string clipName, out int targetFrame)
        {
            targetFrame = 0;
            if (resData == null || resData.ActionFrames == null || string.IsNullOrEmpty(clipName)) return false;

            return resData.ActionFrames.TryGetValue(clipName, out targetFrame) && targetFrame > 0;
        }

        public bool TryGetActionCrossFade(TopDownGame.Data.NpcResData resData, string clipName, out float crossFade)
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

            TopDownGame.Data.NpcResData resData = GetNpcResData();
            float targetDuration = 0f;
            int targetFrame = 0;

            if (resData != null)
            {
                string key = !string.IsNullOrEmpty(realClip) ? realClip : clipName;
                if (TryGetActionFrame(resData, key, out targetFrame))
                {
                    targetDuration = targetFrame / TopDownGame.Skills.SkillData.ACTION_EVENT_FPS;
                }

                // Ghi đè crossFade từ NpcRes.csv nếu có (Ưu tiên thông số của Model hơn là của Skill chung)
                if (TryGetActionCrossFade(resData, key, out float targetCross))
                {
                    fadeTime = targetCross;
                }
            }

            // Tính toán AttackSpeed theo DATA_CONVENTIONS_V2.md Mục 6 (SkillSetting.ini L83-L95):
            // Calculated Frame = Original Frame * (1.0 - floor(AttackSpeed / 10) / 20)
            // Final Action Frame = Clamp(Calculated Frame, Min = 9, Max = 100)
            // Target Duration = Final Action Frame / 15.0f
            float attackSpeedPercent = 0f;
            if (skill != null && skill.notChangeActFrame)
            {
                attackSpeedPercent = 0f;
            }
            else
            {
                var stats = GetEntityStats();
                if (stats != null)
                {
                    attackSpeedPercent = stats.AttackSpeed;
                }
            }

            if (targetFrame > 0)
            {
                var (finalFrame, _) = CalculateScaledActionFrame(targetFrame, attackSpeedPercent);
                targetDuration = finalFrame / TopDownGame.Skills.SkillData.ACTION_EVENT_FPS;
            }

            CrossFadeOnComponent(bodyAnimation, realClip, wrapMode, fadeTime, forceRewind, targetDuration);
            CrossFadeOnComponent(headAnimation, realClip, wrapMode, fadeTime, forceRewind, targetDuration);

            // In debug thông số theo yêu cầu: animation được gọi, độ dài thực tế, độ dài yêu cầu, scale
            float actualLength = 0f;
            float scale = 1.0f;
            if (bodyAnimation != null && bodyAnimation[realClip] != null)
            {
                var state = bodyAnimation[realClip];
                actualLength = state.clip != null ? state.clip.length : state.length;
                scale = state.speed;
            }
            else if (headAnimation != null && headAnimation[realClip] != null)
            {
                var state = headAnimation[realClip];
                actualLength = state.clip != null ? state.clip.length : state.length;
                scale = state.speed;
            }

            // string reqStr = targetDuration > 0f ? $"{targetDuration:F3}s ({targetFrame} frames)" : $"{actualLength:F3}s (mặc định)";
            // Debug.Log($"[Animation] Animation được gọi: <b>{realClip}</b> | Độ dài thực tế: <b>{actualLength:F3}s</b> | Độ dài yêu cầu: <b>{reqStr}</b> | Scale: <b>{scale:F3}</b>");
        }

        /// <summary>
        /// Tính toán số frame thực tế sau khi áp dụng Tốc Đánh theo DATA_CONVENTIONS_V2.md Mục 6
        /// </summary>
        public static (int finalFrame, float speedFactor) CalculateScaledActionFrame(int originalFrame, float attackSpeedPercent)
        {
            if (originalFrame <= 0) return (originalFrame, 1.0f);
            float speedReduction = Mathf.Floor(attackSpeedPercent / 10f) / 20f;
            float calculatedFrame = originalFrame * (1.0f - speedReduction);
            int finalFrame = Mathf.Clamp(Mathf.RoundToInt(calculatedFrame), 9, 100);
            float factor = (float)originalFrame / finalFrame;
            return (finalFrame, factor);
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

                // Áp dụng công thức chuẩn DATA_CONVENTIONS_V2.md Mục 6:
                // Speed = state.clip.length / Target Duration = state.clip.length * 15.0f / Final Action Frame
                if (targetDuration > 0f && state.clip != null && state.clip.length > 0f)
                {
                    state.speed = state.clip.length / targetDuration;
                }
                else
                {
                    state.speed = 1.0f;
                }

                if (forceRewind)
                {
                    state.time = 0f;
                }

                if (fadeTime > 0f)
                {
                    animComp.CrossFade(clipName, fadeTime, PlayMode.StopSameLayer);
                }
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
            TopDownGame.Data.NpcResData resData = GetNpcResData();

            float attackSpeedPercent = 0f;
            if (skill != null && skill.notChangeActFrame)
            {
                attackSpeedPercent = 0f;
            }
            else
            {
                var stats = GetEntityStats();
                if (stats != null)
                {
                    attackSpeedPercent = stats.AttackSpeed;
                }
            }

            string key = !string.IsNullOrEmpty(realClip) ? realClip : clipName;
            if (resData != null && TryGetActionFrame(resData, key, out int targetFrame))
            {
                var (finalFrame, _) = CalculateScaledActionFrame(targetFrame, attackSpeedPercent);
                return finalFrame / TopDownGame.Skills.SkillData.ACTION_EVENT_FPS;
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

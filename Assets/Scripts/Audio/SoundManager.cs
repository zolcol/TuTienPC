using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TopDownGame.Skills;

namespace TopDownGame.Audio
{
    public class SoundManager : MonoBehaviour
    {
        private static SoundManager instance;
        private static bool isApplicationQuitting = false;

        public static bool HasInstance => instance != null && !isApplicationQuitting;

        public static SoundManager Instance
        {
            get
            {
                if (isApplicationQuitting)
                {
                    return null;
                }

                if (instance == null)
                {
                    instance = FindObjectOfType<SoundManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("[SoundManager]");
                        instance = go.AddComponent<SoundManager>();
                        if (Application.isPlaying)
                        {
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return instance;
            }
        }

        [Header("=== VOLUME SETTINGS ===")]
        [Range(0f, 1f)] public float masterVolume = 1.0f;
        [Range(0f, 1f)] public float sfxVolume = 1.0f;
        [Range(0f, 1f)] public float bgmVolume = 0.8f;

        [Header("=== AUDIO ENGINE CONFIG ===")]
        [Tooltip("Số lượng kênh AudioSource tối đa trong Pool (tái sử dụng 0 GC Alloc)")]
        [SerializeField] private int audioSourcePoolSize = 20;

        [Tooltip("Tự động biến tấu nhẹ cao độ (Pitch) để tiếng kiếm chém không bị máy móc")]
        [SerializeField] private bool enablePitchVariation = true;
        [Range(0f, 0.1f)] [SerializeField] private float pitchVariationRange = 0.04f;

        [Tooltip("Độ lan tỏa 3D (0 = 2D nghe toàn màn hình, 1 = 3D hoàn toàn theo vị trí)")]
        [Range(0f, 1f)] [SerializeField] private float sfxSpatialBlend = 0.25f;

        [SerializeField] private bool logSoundPlayback = false;

        // Internal Pool & Cache
        private struct SoundClipGroup
        {
            public AudioClip[] sfxClips;
            public AudioClip[] voiceClips;

            public bool HasAny => (sfxClips != null && sfxClips.Length > 0) || (voiceClips != null && voiceClips.Length > 0);
        }

        private readonly List<AudioSource> audioSourcePool = new List<AudioSource>();
        private readonly Dictionary<string, SoundClipGroup> loadedClipGroups = new Dictionary<string, SoundClipGroup>();
        private readonly Dictionary<int, float> lastSoundPlayTime = new Dictionary<int, float>();

        // BGM Channels
        private AudioSource bgmSource;
        private AudioSource global2DSource;

        private const float SOUND_THROTTLE_MIN_INTERVAL = 0.04f; // Chặn spam cùng 1 âm thanh trong 40ms

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticData()
        {
            instance = null;
            isApplicationQuitting = false;
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(gameObject);
                }
                InitializeAudioEngine();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnApplicationQuit()
        {
            isApplicationQuitting = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void InitializeAudioEngine()
        {
            EnsureUnityAudioListener();

            // 1. Tạo kênh BGM
            GameObject bgmObj = new GameObject("BGM_Source");
            bgmObj.transform.SetParent(transform);
            bgmSource = bgmObj.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.spatialBlend = 0f;

            // 2. Tạo kênh 2D Global (UI / Thông báo)
            GameObject globalObj = new GameObject("Global2D_Source");
            globalObj.transform.SetParent(transform);
            global2DSource = globalObj.AddComponent<AudioSource>();
            global2DSource.playOnAwake = false;
            global2DSource.spatialBlend = 0f;

            // 3. Khởi tạo sẵn AudioSource Pool (Tái sử dụng 100%, không sinh rác GC)
            GameObject poolContainer = new GameObject("AudioSource_Pool");
            poolContainer.transform.SetParent(transform);

            for (int i = 0; i < audioSourcePoolSize; i++)
            {
                GameObject poolItem = new GameObject($"SFX_Channel_{i:D2}");
                poolItem.transform.SetParent(poolContainer.transform);

                AudioSource src = poolItem.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = sfxSpatialBlend;
                src.minDistance = 3f;
                src.maxDistance = 25f;
                src.rolloffMode = AudioRolloffMode.Linear;

                audioSourcePool.Add(src);
            }
        }

        public void EnsureUnityAudioListener()
        {
            AudioListener.volume = masterVolume;
            AudioListener.pause = false;

            AudioListener listener = FindObjectOfType<AudioListener>();
            if (listener == null)
            {
                if (UnityEngine.Camera.main != null)
                {
                    UnityEngine.Camera.main.gameObject.AddComponent<AudioListener>();
                }
                else
                {
                    gameObject.AddComponent<AudioListener>();
                }
            }
            else if (!listener.enabled)
            {
                listener.enabled = true;
            }
        }

        // ==================== PHÁT ÂM THANH THEO BẢNG SOUND.CSV ====================

        /// <summary>
        /// Phát âm thanh theo Sound ID tra cứu từ Sound.csv
        /// </summary>
        public void PlaySound(int soundId, Transform emitter = null)
        {
            if (soundId <= 0) return;

            // Chặn spam cùng 1 âm thanh trong vài miligiây (ví dụ quét trúng 10 quái cùng lúc)
            float currentTime = Time.time;
            if (lastSoundPlayTime.TryGetValue(soundId, out float lastTime))
            {
                if (currentTime - lastTime < SOUND_THROTTLE_MIN_INTERVAL)
                {
                    return;
                }
            }
            lastSoundPlayTime[soundId] = currentTime;

            SoundData soundData = SoundDatabase.GetSound(soundId);
            if (soundData == null)
            {
                if (logSoundPlayback)
                {
                    Debug.LogWarning($"[SoundManager] ⚠️ Không tìm thấy Sound ID: {soundId} trong Sound.csv");
                }
                return;
            }

            PlaySoundData(soundData, emitter != null ? emitter.position : (Vector3?)null);
        }

        /// <summary>
        /// Phát âm thanh tại vị trí 3D cố định (ví dụ điểm trúng đòn va chạm của đạn)
        /// </summary>
        public void PlaySoundAtPosition(int soundId, Vector3 position)
        {
            if (soundId <= 0) return;
            SoundData soundData = SoundDatabase.GetSound(soundId);
            if (soundData != null)
            {
                PlaySoundData(soundData, position);
            }
        }

        /// <summary>
        /// Phát âm thanh của kỹ năng (Player / Enemy)
        /// </summary>
        public void PlaySkillSound(SkillData skill, Transform emitter = null)
        {
            if (skill == null || skill.playsound <= 0) return;
            if (logSoundPlayback)
            {
                Debug.Log($"<color=#38bdf8>[SoundManager] 🎯 Kỹ năng:</color> <b>[{skill.id}] {skill.name}</b> ➔ Phát Sound ID: <b>#{skill.playsound}</b> (Frame: {skill.playsoundFrame})");
            }
            PlaySound(skill.playsound, emitter);
        }

        private void PlaySoundData(SoundData soundData, Vector3? position)
        {
            if (soundData == null) return;

            SoundClipGroup clipGroup = GetSoundClips(soundData);
            bool playedAny = false;

            // 1. Phát âm thanh SFX chiêu thức (nếu có)
            if (clipGroup.sfxClips != null && clipGroup.sfxClips.Length > 0)
            {
                AudioClip sfxClip = clipGroup.sfxClips.Length == 1
                    ? clipGroup.sfxClips[0]
                    : clipGroup.sfxClips[UnityEngine.Random.Range(0, clipGroup.sfxClips.Length)];

                PlayClipInternal(sfxClip, position, 1.0f);
                playedAny = true;
            }

            // 2. Phát âm thanh Voice nhân vật (nếu có)
            if (clipGroup.voiceClips != null && clipGroup.voiceClips.Length > 0)
            {
                AudioClip voiceClip = clipGroup.voiceClips.Length == 1
                    ? clipGroup.voiceClips[0]
                    : clipGroup.voiceClips[UnityEngine.Random.Range(0, clipGroup.voiceClips.Length)];

                PlayClipInternal(voiceClip, position, 1.0f);
                playedAny = true;
            }

            if (logSoundPlayback)
            {
                string status = playedAny ? "<color=#4ade80>Success</color>" : "<color=#f87171>Clip Not Found</color>";
                Debug.Log($"<color=#38bdf8>[SoundManager] 🔊 Phát âm thanh:</color> <b>ID #{soundData.soundId}</b> ({soundData.description}) | Event: <i>{soundData.wwiseEvent}</i> | Bank: <i>{soundData.bankBnk}</i> | Status: <b>{status}</b> (SFX: {clipGroup.sfxClips?.Length ?? 0}, Vo: {clipGroup.voiceClips?.Length ?? 0})");
            }

            if (!playedAny)
            {
                Debug.LogWarning($"[SoundManager] ⚠️ Không tìm thấy AudioClip trong Assets/resources/Audio/{soundData.bankBnk}/{soundData.CleanEventName} cho ID: {soundData.soundId}");
            }
        }

        // ==================== AUDIO POOLING CORE (HIỆU NĂNG CAO) ====================

        private void PlayClipInternal(AudioClip clip, Vector3? position, float volumeMultiplier)
        {
            if (clip == null) return;

            AudioSource availableSource = GetAvailableAudioSource();
            if (availableSource == null)
            {
                // Nếu pool bận hết, phát tạm bằng global
                global2DSource.PlayOneShot(clip, sfxVolume * masterVolume * volumeMultiplier);
                return;
            }

            // Thiết lập vị trí 3D
            if (position.HasValue)
            {
                availableSource.transform.position = position.Value;
                availableSource.spatialBlend = sfxSpatialBlend;
            }
            else
            {
                availableSource.transform.position = Vector3.zero;
                availableSource.spatialBlend = 0f; // 2D
            }

            // Biến tấu Pitch nhẹ cho tự nhiên
            if (enablePitchVariation)
            {
                availableSource.pitch = 1f + UnityEngine.Random.Range(-pitchVariationRange, pitchVariationRange);
            }
            else
            {
                availableSource.pitch = 1f;
            }

            availableSource.volume = sfxVolume * masterVolume * volumeMultiplier;
            availableSource.clip = clip;
            availableSource.Play();
        }

        private AudioSource GetAvailableAudioSource()
        {
            for (int i = 0; i < audioSourcePool.Count; i++)
            {
                if (!audioSourcePool[i].isPlaying)
                {
                    return audioSourcePool[i];
                }
            }

            // Nếu toàn bộ pool đang bận, chọn nguồn có thời gian phát sắp hết nhất
            AudioSource oldestSource = audioSourcePool[0];
            float maxTime = -1f;
            for (int i = 0; i < audioSourcePool.Count; i++)
            {
                if (audioSourcePool[i].time > maxTime)
                {
                    maxTime = audioSourcePool[i].time;
                    oldestSource = audioSourcePool[i];
                }
            }

            return oldestSource;
        }

        private SoundClipGroup GetSoundClips(SoundData data)
        {
            if (data == null) return default;

            string key = !string.IsNullOrEmpty(data.wwiseEvent) ? data.wwiseEvent : data.soundId.ToString();
            if (loadedClipGroups.TryGetValue(key, out SoundClipGroup cachedGroup))
            {
                return cachedGroup;
            }

            SoundClipGroup group = ResolveSoundClipGroup(data);
            loadedClipGroups[key] = group;
            return group;
        }

        /// <summary>
        /// Ánh xạ chính xác theo AUDIO_MAPPING_RULES.md, hỗ trợ phân rã Gender (Base SFX + Vo) và Wwise Random Container
        /// </summary>
        private SoundClipGroup ResolveSoundClipGroup(SoundData data)
        {
            SoundClipGroup group = default;
            if (data == null) return group;

            string bank = data.bankBnk?.Trim() ?? "";
            string cleanEvent = data.CleanEventName;
            string rawEvent = data.wwiseEvent ?? "";

            // 1. Quy tắc Âm thanh có hậu tố Giới tính (Female / Male) -> Phân rã thành 2 phần: Base SFX & Voice
            if (data.HasGenderSuffix)
            {
                string baseEvent = data.BaseEventName;
                string voiceEvent = data.VoiceEventName;

                if (!string.IsNullOrEmpty(baseEvent))
                {
                    group.sfxClips = ResolveClipsForName(baseEvent, bank);
                }

                if (!string.IsNullOrEmpty(voiceEvent))
                {
                    group.voiceClips = ResolveVoiceClips(voiceEvent, bank);
                }

                if (!group.HasAny)
                {
                    group.sfxClips = ResolveClipsForName(cleanEvent, bank);
                }
            }
            // 2. Quy tắc Âm thanh trúng đòn (Hit) - Hỗ trợ Random Container 3 dạng
            else if (cleanEvent.IndexOf("Hit", StringComparison.OrdinalIgnoreCase) >= 0 || rawEvent.IndexOf("Hit", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                group.sfxClips = ResolveHitClips(cleanEvent, bank);
            }
            // 3. Quy tắc Voice nhân vật (Vo) - Hỗ trợ Random Container
            else if (cleanEvent.IndexOf("Vo", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                group.voiceClips = ResolveVoiceClips(cleanEvent, bank);
            }
            // 4. Chiêu thức thông thường, Đăng trường (Dc), Nộ khí (00)
            else
            {
                group.sfxClips = ResolveClipsForName(cleanEvent, bank);
            }

            // Fallback: nếu vẫn chưa tìm thấy clip nào, thử rawEvent và soundId
            if (!group.HasAny)
            {
                if (!string.IsNullOrEmpty(rawEvent) && !rawEvent.Equals(cleanEvent, StringComparison.OrdinalIgnoreCase))
                {
                    group.sfxClips = ResolveClipsForName(rawEvent, bank);
                }
                if (!group.HasAny && data.soundId > 0)
                {
                    group.sfxClips = ResolveClipsForName(data.soundId.ToString(), bank);
                }
            }

            return group;
        }

        private AudioClip[] ResolveClipsForName(string name, string bank)
        {
            if (string.IsNullOrEmpty(name)) return null;

            List<AudioClip> foundClips = new List<AudioClip>();
            HashSet<string> testedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void TryAddDirect(string path)
            {
                if (string.IsNullOrEmpty(path) || testedPaths.Contains(path)) return;
                testedPaths.Add(path);

                AudioClip clip = Resources.Load<AudioClip>(path);
                if (clip != null && !foundClips.Contains(clip))
                {
                    foundClips.Add(clip);
                }
            }

            void TryAdd(string clipName)
            {
                if (string.IsNullOrEmpty(clipName)) return;
                if (!string.IsNullOrEmpty(bank))
                {
                    TryAddDirect($"Audio/{bank}/{clipName}");
                }
                TryAddDirect($"Audio/{clipName}");
                TryAddDirect(clipName);
            }

            // 1. Tên gốc chính xác (vd: DS_03_01, Em_01_01)
            TryAdd(name);

            // 2. Các biến thể hậu tố chữ cái (vd: DS_02_01a..b, DS_04_02a..f, DS_05_01a..b)
            char[] suffixes = new[] { 'a', 'b', 'c', 'd', 'e', 'f' };
            for (int i = 0; i < suffixes.Length; i++)
            {
                TryAdd($"{name}{suffixes[i]}");
            }

            // 3. Hậu tố _new (Remake)
            TryAdd($"{name}_new");

            return foundClips.Count > 0 ? foundClips.ToArray() : null;
        }

        private AudioClip[] ResolveVoiceClips(string voiceName, string bank)
        {
            if (string.IsNullOrEmpty(voiceName)) return null;

            List<AudioClip> foundClips = new List<AudioClip>();
            HashSet<string> testedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void TryAddDirect(string path)
            {
                if (string.IsNullOrEmpty(path) || testedPaths.Contains(path)) return;
                testedPaths.Add(path);

                AudioClip clip = Resources.Load<AudioClip>(path);
                if (clip != null && !foundClips.Contains(clip))
                {
                    foundClips.Add(clip);
                }
            }

            void TryAdd(string clipName)
            {
                if (string.IsNullOrEmpty(clipName)) return;
                if (!string.IsNullOrEmpty(bank))
                {
                    TryAddDirect($"Audio/{bank}/{clipName}");
                }
                TryAddDirect($"Audio/{clipName}");
                TryAddDirect(clipName);
            }

            // 1. Tên Voice chính xác (vd: DS_03_01_VoFemale, Em_Vo_00)
            TryAdd(voiceName);

            // 2. Biến thể Random Container (vd: Sl_Vo_14a, Sl_Vo_14b)
            char[] voiceSuffixes = new[] { 'a', 'b', 'c', 'd', 'e' };
            for (int i = 0; i < voiceSuffixes.Length; i++)
            {
                TryAdd($"{voiceName}{voiceSuffixes[i]}");
            }

            // 3. Biến thể số thứ tự (vd: Vo_01, Vo_02)
            for (int i = 1; i <= 9; i++)
            {
                TryAdd($"{voiceName}_{i:D2}");
                TryAdd($"{voiceName}{i:D2}");
            }

            return foundClips.Count > 0 ? foundClips.ToArray() : null;
        }

        private AudioClip[] ResolveHitClips(string hitEventName, string bank)
        {
            if (string.IsNullOrEmpty(hitEventName)) return null;

            List<AudioClip> foundClips = new List<AudioClip>();
            HashSet<string> testedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void TryAddDirect(string path)
            {
                if (string.IsNullOrEmpty(path) || testedPaths.Contains(path)) return;
                testedPaths.Add(path);

                AudioClip clip = Resources.Load<AudioClip>(path);
                if (clip != null && !foundClips.Contains(clip))
                {
                    foundClips.Add(clip);
                }
            }

            void TryAdd(string clipName)
            {
                if (string.IsNullOrEmpty(clipName)) return;
                if (!string.IsNullOrEmpty(bank))
                {
                    TryAddDirect($"Audio/{bank}/{clipName}");
                }
                TryAddDirect($"Audio/{clipName}");
                TryAddDirect(clipName);
            }

            var match = System.Text.RegularExpressions.Regex.Match(hitEventName, @"^([A-Za-z]+)_(\d+)_(Hit.*)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string phai = match.Groups[1].Value;
                string chieu = match.Groups[2].Value;
                string hitPart = match.Groups[3].Value;

                // Dạng 1: {Phái}_{Chiêu}_Hit (vd: Th_03_Hit, Em_03_Hit)
                TryAdd($"{phai}_{chieu}_{hitPart}");

                // Dạng 2: {Phái}_{Chiêu}_Hit0x / {Phái}_{Chiêu}_Hit_0x (vd: Em_04_Hit01..03, Em_05_Hit01..05, Sl_01_Hit01..02)
                for (int i = 1; i <= 9; i++)
                {
                    TryAdd($"{phai}_{chieu}_{hitPart}{i:D2}");
                    TryAdd($"{phai}_{chieu}_{hitPart}_{i:D2}");
                    TryAdd($"{phai}_{chieu}_{hitPart}{i}");
                }

                // Dạng 3: {Phái}_Hit_0x / {Phái}_Hit0x (vd: Em_Hit_01..03)
                for (int i = 1; i <= 9; i++)
                {
                    TryAdd($"{phai}_{hitPart}_{i:D2}");
                    TryAdd($"{phai}_{hitPart}{i:D2}");
                    TryAdd($"{phai}_{hitPart}_{i}");
                }

                TryAdd($"{phai}_{hitPart}");
            }
            else
            {
                TryAdd(hitEventName);
                for (int i = 1; i <= 9; i++)
                {
                    TryAdd($"{hitEventName}{i:D2}");
                    TryAdd($"{hitEventName}_{i:D2}");
                    TryAdd($"{hitEventName}{i}");
                }
            }

            return foundClips.Count > 0 ? foundClips.ToArray() : null;
        }

        // ==================== NHẠC NỀN BGM (SMOOTH CROSSFADE) ====================

        public void PlayBGM(AudioClip newBgmClip, float fadeDuration = 1.0f)
        {
            if (newBgmClip == null || bgmSource.clip == newBgmClip) return;
            StartCoroutine(CrossFadeBGMCoroutine(newBgmClip, fadeDuration));
        }

        private IEnumerator CrossFadeBGMCoroutine(AudioClip newClip, float duration)
        {
            float startVol = bgmSource.volume;
            float timer = 0f;

            // Fade out
            while (timer < duration / 2f)
            {
                timer += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(startVol, 0f, timer / (duration / 2f));
                yield return null;
            }

            bgmSource.clip = newClip;
            bgmSource.Play();

            // Fade in
            timer = 0f;
            float targetVol = bgmVolume * masterVolume;
            while (timer < duration / 2f)
            {
                timer += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(0f, targetVol, timer / (duration / 2f));
                yield return null;
            }

            bgmSource.volume = targetVol;
        }

        public void StopBGM()
        {
            if (bgmSource != null)
            {
                bgmSource.Stop();
            }
        }
    }
}

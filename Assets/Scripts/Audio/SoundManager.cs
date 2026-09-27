using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TopDownGame.Skills;

namespace TopDownGame.Audio
{
    public class SoundManager : MonoBehaviour
    {
        private static SoundManager instance;
        public static SoundManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<SoundManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("[SoundManager]");
                        instance = go.AddComponent<SoundManager>();
                        DontDestroyOnLoad(go);
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
        private readonly List<AudioSource> audioSourcePool = new List<AudioSource>();
        private readonly Dictionary<string, AudioClip> loadedClips = new Dictionary<string, AudioClip>();
        private readonly Dictionary<int, float> lastSoundPlayTime = new Dictionary<int, float>();

        // BGM Channels
        private AudioSource bgmSource;
        private AudioSource global2DSource;

        private const float SOUND_THROTTLE_MIN_INTERVAL = 0.04f; // Chặn spam cùng 1 âm thanh trong 40ms

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudioEngine();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
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
            PlaySound(skill.playsound, emitter);
        }

        private void PlaySoundData(SoundData soundData, Vector3? position)
        {
            if (logSoundPlayback)
            {
                Debug.Log($"<color=#38bdf8>[SoundManager] 🔊 Phát âm thanh:</color> <b>#{soundData.soundId}</b> ({soundData.description}) | Event: <i>{soundData.wwiseEvent}</i>");
            }

            AudioClip clip = LoadAudioClip(soundData);
            if (clip != null)
            {
                PlayClipInternal(clip, position, 1.0f);
            }
            else
            {
                Debug.LogWarning($"[SoundManager] ⚠️ Không tìm thấy AudioClip trong Assets/Audio/{soundData.bankBnk}/{soundData.CleanEventName} cho ID: {soundData.soundId}");
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
                availableSource.pitch = 1f + Random.Range(-pitchVariationRange, pitchVariationRange);
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

        private AudioClip LoadAudioClip(SoundData data)
        {
            if (data == null) return null;

            string key = !string.IsNullOrEmpty(data.wwiseEvent) ? data.wwiseEvent : data.soundId.ToString();
            if (loadedClips.TryGetValue(key, out AudioClip cached)) return cached;

            AudioClip clip = null;
            string cleanEvent = data.CleanEventName;
            string rawEvent = data.wwiseEvent ?? "";
            string bank = data.bankBnk?.Trim() ?? "";

            // 1. Thử nạp qua Resources API (nếu thư mục là Resources)
            List<string> resPaths = new List<string>();
            if (!string.IsNullOrEmpty(bank) && !string.IsNullOrEmpty(cleanEvent))
                resPaths.Add($"Audio/{bank}/{cleanEvent}");
            if (!string.IsNullOrEmpty(bank) && !string.IsNullOrEmpty(rawEvent))
                resPaths.Add($"Audio/{bank}/{rawEvent}");
            if (!string.IsNullOrEmpty(cleanEvent))
                resPaths.Add($"Audio/{cleanEvent}");
            if (!string.IsNullOrEmpty(rawEvent))
                resPaths.Add($"Audio/{rawEvent}");
            if (data.soundId > 0)
            {
                if (!string.IsNullOrEmpty(bank))
                    resPaths.Add($"Audio/{bank}/{data.soundId}");
                resPaths.Add($"Audio/{data.soundId}");
            }

            foreach (var rPath in resPaths)
            {
                clip = Resources.Load<AudioClip>(rPath);
                if (clip != null) break;
            }

            // 2. Thử nạp linh hoạt qua AssetDatabase trong Editor (Assets/Audio/{Bank}/{Event}.wav, v.v.)
            #if UNITY_EDITOR
            if (clip == null)
            {
                List<string> possiblePaths = new List<string>();
                string[] baseFolders = new string[]
                {
                    "Assets/Audio",
                    "Assets/Resources/Audio",
                    "Assets/resources/Audio"
                };
                string[] extensions = new string[] { ".wav", ".mp3", ".ogg" };

                foreach (var baseFolder in baseFolders)
                {
                    // Thư mục con theo SoundBank (ví dụ: Assets/Audio/Em/Em_01_01.wav)
                    if (!string.IsNullOrEmpty(bank))
                    {
                        if (!string.IsNullOrEmpty(cleanEvent))
                        {
                            foreach (var ext in extensions)
                                possiblePaths.Add($"{baseFolder}/{bank}/{cleanEvent}{ext}");
                        }
                        if (!string.IsNullOrEmpty(rawEvent))
                        {
                            foreach (var ext in extensions)
                                possiblePaths.Add($"{baseFolder}/{bank}/{rawEvent}{ext}");
                        }
                        if (data.soundId > 0)
                        {
                            foreach (var ext in extensions)
                                possiblePaths.Add($"{baseFolder}/{bank}/{data.soundId}{ext}");
                        }
                    }

                    // Không có thư mục con (ví dụ: Assets/Audio/Em_01_01.wav)
                    if (!string.IsNullOrEmpty(cleanEvent))
                    {
                        foreach (var ext in extensions)
                            possiblePaths.Add($"{baseFolder}/{cleanEvent}{ext}");
                    }
                    if (!string.IsNullOrEmpty(rawEvent))
                    {
                        foreach (var ext in extensions)
                            possiblePaths.Add($"{baseFolder}/{rawEvent}{ext}");
                    }
                    if (data.soundId > 0)
                    {
                        foreach (var ext in extensions)
                            possiblePaths.Add($"{baseFolder}/{data.soundId}{ext}");
                    }
                }

                foreach (var path in possiblePaths)
                {
                    clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    if (clip != null) break;
                }

                // Fallback nếu có suffix Hit hoặc số thứ tự (ví dụ: tìm Em_04_Hit mà file tên Em_04_Hit01.wav hoặc Em_Hit_01.wav)
                if (clip == null && !string.IsNullOrEmpty(bank) && !string.IsNullOrEmpty(cleanEvent))
                {
                    foreach (var baseFolder in baseFolders)
                    {
                        string bankFolder = $"{baseFolder}/{bank}";
                        if (System.IO.Directory.Exists(bankFolder))
                        {
                            string[] files = System.IO.Directory.GetFiles(bankFolder, $"{cleanEvent}*.wav");
                            if (files.Length > 0)
                            {
                                string assetPath = files[0].Replace('\\', '/');
                                clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                                if (clip != null) break;
                            }
                            
                            if (clip == null && cleanEvent.Contains("Hit"))
                            {
                                string[] hitFiles = System.IO.Directory.GetFiles(bankFolder, "*Hit*.wav");
                                if (hitFiles.Length > 0)
                                {
                                    string assetPath = hitFiles[0].Replace('\\', '/');
                                    clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                                    if (clip != null) break;
                                }
                            }
                        }
                    }
                }
            }
            #endif

            if (clip != null)
            {
                loadedClips[key] = clip;
                return clip;
            }

            return null;
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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace TopDownGame.Audio
{
    public class SoundDatabase : MonoBehaviour
    {
        private static SoundDatabase instance;
        public static SoundDatabase Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<SoundDatabase>();
                    if (instance == null)
                    {
                        GameObject dbObj = new GameObject("[SoundDatabase]");
                        instance = dbObj.AddComponent<SoundDatabase>();
                        if (Application.isPlaying)
                        {
                            DontDestroyOnLoad(dbObj);
                        }
                        else
                        {
                            dbObj.hideFlags = HideFlags.HideAndDontSave;
                        }
                    }
                    instance.EnsureLoaded();
                }
                return instance;
            }
        }

        [Tooltip("Kéo file Sound.csv vào đây (hoặc để trống để tự động nạp từ thư mục Assets/Settings/Sound.csv)")]
        [SerializeField] private TextAsset csvFile;

        private readonly Dictionary<int, SoundData> sounds = new Dictionary<int, SoundData>();
        private readonly Dictionary<string, SoundData> soundsByEvent = new Dictionary<string, SoundData>();
        private bool isLoaded = false;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            EnsureLoaded();
        }

        public void EnsureLoaded()
        {
            if (!isLoaded || sounds.Count == 0)
            {
                LoadDatabase();
            }
        }

        [ContextMenu("Tải lại Sound Database từ CSV")]
        public void LoadDatabase()
        {
            sounds.Clear();
            soundsByEvent.Clear();

            string csvContent = "";

            if (csvFile != null)
            {
                csvContent = csvFile.text;
            }
            else
            {
                string nSoundPath = Path.Combine(Application.dataPath, "Settings", "N", "Sound.csv");
                string legacySoundPath = Path.Combine(Application.dataPath, "Settings", "Sound.csv");

                string targetPath = File.Exists(nSoundPath) ? nSoundPath : (File.Exists(legacySoundPath) ? legacySoundPath : "");

                if (!string.IsNullOrEmpty(targetPath))
                {
                    try
                    {
                        using (var fs = new FileStream(targetPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var reader = new StreamReader(fs, System.Text.Encoding.UTF8))
                        {
                            csvContent = reader.ReadToEnd();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[SoundDatabase] ❌ Lỗi khi đọc file Sound.csv ({targetPath}): {ex.Message}");
                    }
                }
                else
                {
                    TextAsset resCsv = Resources.Load<TextAsset>("Sound");
                    if (resCsv != null)
                    {
                        csvContent = resCsv.text;
                    }
                }
            }

            if (string.IsNullOrEmpty(csvContent))
            {
                Debug.LogWarning("[SoundDatabase] ⚠️ Không tìm thấy nội dung file Sound.csv để nạp.");
                return;
            }

            ParseCsv(csvContent);
            isLoaded = true;
            // Debug.Log($"✅ <color=yellow>[SoundDatabase]</color> Đã nạp thành công <b>{sounds.Count}</b> âm thanh từ Sound.csv!");
        }

        private void ParseCsv(string text)
        {
            string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length <= 1) return;

            // Bỏ qua header dòng 0: SoundID,Description,Bank_Bnk,Wwise_Event,Audio_File_Wem
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] tokens = SplitCsvLine(line);
                if (tokens.Length < 4) continue;

                try
                {
                    int soundId = ParseInt(GetToken(tokens, 0));
                    if (soundId <= 0) continue;

                    string desc = GetToken(tokens, 1);
                    string bank = GetToken(tokens, 2);
                    string wwiseEvent = GetToken(tokens, 3);
                    string wem = GetToken(tokens, 4);

                    SoundData data = new SoundData
                    {
                        soundId = soundId,
                        description = desc,
                        bankBnk = bank,
                        wwiseEvent = wwiseEvent,
                        audioFileWem = wem
                    };

                    sounds[soundId] = data;
                    if (!string.IsNullOrEmpty(wwiseEvent))
                    {
                        soundsByEvent[wwiseEvent] = data;
                    }
                    if (!string.IsNullOrEmpty(data.CleanEventName))
                    {
                        soundsByEvent[data.CleanEventName] = data;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SoundDatabase] ⚠️ Lỗi đọc dòng {i + 1}: {ex.Message}");
                }
            }
        }

        private string[] SplitCsvLine(string line)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            int startIndex = 0;

            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (line[i] == ',' && !inQuotes)
                {
                    string token = line.Substring(startIndex, i - startIndex).Trim().Trim('"');
                    result.Add(token);
                    startIndex = i + 1;
                }
            }

            if (startIndex <= line.Length)
            {
                string token = line.Substring(startIndex).Trim().Trim('"');
                result.Add(token);
            }

            return result.ToArray();
        }

        private string GetToken(string[] tokens, int index)
        {
            return index < tokens.Length ? tokens[index].Trim() : "";
        }

        private int ParseInt(string s, int defaultVal = 0)
        {
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int val) ? val : defaultVal;
        }

        // ==================== STATIC METHODS ====================

        public static SoundData GetSound(int soundId)
        {
            if (soundId <= 0) return null;
            Instance.sounds.TryGetValue(soundId, out SoundData data);
            return data;
        }

        public static SoundData GetSoundByEvent(string wwiseEvent)
        {
            if (string.IsNullOrEmpty(wwiseEvent)) return null;
            Instance.soundsByEvent.TryGetValue(wwiseEvent, out SoundData data);
            return data;
        }

        public static Dictionary<int, SoundData> GetAllSounds()
        {
            return Instance.sounds;
        }

        public static void Reload()
        {
            Instance.LoadDatabase();
        }
    }
}

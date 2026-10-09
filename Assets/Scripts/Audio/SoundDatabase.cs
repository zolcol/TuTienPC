using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using TopDownGame.Data;

namespace TopDownGame.Audio
{
    public class SoundDatabase : ICsvTable
    {
        private static SoundDatabase instance;
        public static SoundDatabase Instance => instance ?? (instance = new SoundDatabase());

        private readonly Dictionary<int, SoundData> sounds = new Dictionary<int, SoundData>();
        private readonly Dictionary<string, SoundData> soundsByEvent = new Dictionary<string, SoundData>();
        
        public bool IsLoaded { get; private set; }

        public void EnsureLoaded()
        {
            if (!IsLoaded || sounds.Count == 0)
            {
                Load();
            }
        }

        public void LoadDatabase() => Load();

        public void Clear()
        {
            sounds.Clear();
            soundsByEvent.Clear();
            IsLoaded = false;
        }

        public void Load()
        {
            Clear();

            string targetPath = GameDataPaths.SoundCsv;

            string csvContent = "";
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
                TextAsset resCsv = Resources.Load<TextAsset>(GameConstants.ResourcePaths.SoundCsv);
                if (resCsv != null)
                {
                    csvContent = resCsv.text;
                }
            }

            if (string.IsNullOrEmpty(csvContent)) return;

            ParseCsv(csvContent);
            IsLoaded = true;
        }

        private void ParseCsv(string text)
        {
            string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length <= 1) return;

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] tokens = CsvParserHelper.SplitCsvLine(line);
                if (tokens.Length < 4) continue;

                try
                {
                    int soundId = CsvParserHelper.ParseInt(CsvParserHelper.GetToken(tokens, 0));
                    if (soundId <= 0) continue;

                    string desc = CsvParserHelper.GetToken(tokens, 1);
                    string bank = CsvParserHelper.GetToken(tokens, 2);
                    string wwiseEvent = CsvParserHelper.GetToken(tokens, 3);
                    string wem = CsvParserHelper.GetToken(tokens, 4);

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
                    if (data.HasGenderSuffix)
                    {
                        if (!string.IsNullOrEmpty(data.BaseEventName) && !soundsByEvent.ContainsKey(data.BaseEventName))
                        {
                            soundsByEvent[data.BaseEventName] = data;
                        }
                        if (!string.IsNullOrEmpty(data.VoiceEventName) && !soundsByEvent.ContainsKey(data.VoiceEventName))
                        {
                            soundsByEvent[data.VoiceEventName] = data;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SoundDatabase] ⚠️ Lỗi đọc dòng {i + 1}: {ex.Message}");
                }
            }
        }

        public static SoundData GetSound(int soundId)
        {
            if (soundId <= 0) return null;
            Instance.EnsureLoaded();
            Instance.sounds.TryGetValue(soundId, out SoundData data);
            return data;
        }

        public static SoundData GetSoundByEvent(string wwiseEvent)
        {
            if (string.IsNullOrEmpty(wwiseEvent)) return null;
            Instance.EnsureLoaded();
            Instance.soundsByEvent.TryGetValue(wwiseEvent, out SoundData data);
            return data;
        }

        public static Dictionary<int, SoundData> GetAllSounds()
        {
            Instance.EnsureLoaded();
            return Instance.sounds;
        }

        public static void Reload()
        {
            Instance.Load();
        }
    }
}

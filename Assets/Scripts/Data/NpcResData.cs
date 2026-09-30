using System;
using System.Collections.Generic;
using UnityEngine;

namespace TopDownGame.Data
{
    [Serializable]
    public class NpcResData
    {
        public int resId;
        public string resFile;
        public string desc;
        public int runSoundId;
        public int deathSoundId;
        public int hitSoundId;
        public float height = 1.8f;
        public float width = 0.5f;

        public Dictionary<string, int> ActionFrames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, float> ActionCrossFades = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        private GameObject cachedPrefab;
        private bool attemptedLoad = false;

        public GameObject LoadPrefab()
        {
            if (cachedPrefab != null) return cachedPrefab;
            if (attemptedLoad) return null;

            attemptedLoad = true;
            cachedPrefab = NpcResDatabase.LoadPrefab(resFile);
            return cachedPrefab;
        }
    }
}

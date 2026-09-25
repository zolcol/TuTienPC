#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TopDownGame.Player;

namespace TopDownGame.Editor
{
    public static class PlayerAnimationPopulator
    {
        [MenuItem("Tools/TopDownGame/Populate Player Animation Clips", false, 30)]
        public static void PopulateClips()
        {
            PlayerController player = Object.FindObjectOfType<PlayerController>();
            if (player == null)
            {
                Debug.LogWarning("[AnimationPopulator] ⚠️ Không tìm thấy PlayerController trong Scene!");
                return;
            }

            LegacyAnimationController animCtrl = player.GetComponent<LegacyAnimationController>() ?? player.GetComponentInChildren<LegacyAnimationController>();
            if (animCtrl == null)
            {
                Debug.LogWarning("[AnimationPopulator] ⚠️ Không tìm thấy LegacyAnimationController trên Player!");
                return;
            }

            animCtrl.AutoFindAnimationComponents();

            int bodyAdded = 0;
            int headAdded = 0;

            // 1. Nạp toàn bộ clip vào Body Animation
            if (animCtrl.BodyAnimation != null)
            {
                bodyAdded = AddClipsFromFolder(animCtrl.BodyAnimation, "Assets/Animation/player/f2_em_body");
            }

            // 2. Nạp toàn bộ clip vào Head Animation
            Transform headTf = player.transform.Find("Head") ?? animCtrl.transform.Find("Head");
            Animation headAnim = headTf != null ? headTf.GetComponentInChildren<Animation>() : null;
            if (headAnim != null)
            {
                headAdded = AddClipsFromFolder(headAnim, "Assets/Animation/player/f2_em_head");
            }

            EditorUtility.SetDirty(player.gameObject);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log($"✅ <color=green>[AnimationPopulator]</color> Đã nạp thành công <b>{bodyAdded}</b> clips cho Body và <b>{headAdded}</b> clips cho Head của Player!");
        }

        private static int AddClipsFromFolder(Animation animComponent, string folderPath)
        {
            if (!Directory.Exists(folderPath)) return 0;

            int addedCount = 0;
            string[] animFiles = Directory.GetFiles(folderPath, "*.anim", SearchOption.AllDirectories);

            foreach (string file in animFiles)
            {
                string assetPath = file.Replace("\\", "/");
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
                if (clip == null) continue;

                string clipName = Path.GetFileNameWithoutExtension(file);

                // Nếu chưa có clip trong Animation component thì AddClip
                if (animComponent.GetClip(clipName) == null)
                {
                    animComponent.AddClip(clip, clipName);
                    addedCount++;
                }
            }

            return addedCount;
        }
    }
}
#endif

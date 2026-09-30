#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TopDownGame.Player;

namespace TopDownGame.Editor
{
    /// <summary>
    /// Editor tool tự động tìm kiếm, liên kết và nạp Animation Clips vào Body và Head Animation components.
    /// Giữ toàn bộ API UnityEditor và logic quét AssetDatabase riêng biệt trong thư mục Editor.
    /// </summary>
    public static class PlayerAnimationPopulator
    {
        [MenuItem("Tools/TopDownGame/Populate Player Animation Clips", false, 30)]
        public static void PopulatePlayerClips()
        {
            PlayerController player = Object.FindObjectOfType<PlayerController>();
            if (player == null)
            {
                Debug.LogWarning("[AnimationPopulator] ⚠️ Không tìm thấy PlayerController trong Scene!");
                return;
            }

            PopulateClipsOnObject(player.gameObject);
        }

        [MenuItem("GameObject/TopDownGame/Populate Animation Clips", false, 0)]
        public static void PopulateSelectedClips()
        {
            GameObject targetGo = Selection.activeGameObject;
            if (targetGo == null)
            {
                Debug.LogWarning("[AnimationPopulator] ⚠️ Chưa chọn GameObject nào!");
                return;
            }

            PopulateClipsOnObject(targetGo);
        }

        public static void PopulateClipsOnObject(GameObject go)
        {
            if (go == null) return;

            LegacyAnimationController animCtrl = go.GetComponent<LegacyAnimationController>() ?? go.GetComponentInChildren<LegacyAnimationController>();
            if (animCtrl == null)
            {
                Debug.LogWarning($"[AnimationPopulator] ⚠️ Không tìm thấy LegacyAnimationController trên {go.name}!");
                return;
            }

            animCtrl.AutoFindAnimationComponents();

            int bodyAdded = 0;
            int headAdded = 0;

            if (animCtrl.BodyAnimation != null)
            {
                bodyAdded += AddClipsFromFolder(animCtrl.BodyAnimation, "Assets/Animation/player/f2_em_body");
            }

            Transform headTf = go.transform.Find("Head") ?? animCtrl.transform.Find("Head");
            Animation headAnim = headTf != null ? headTf.GetComponentInChildren<Animation>() : animCtrl.HeadAnimation;
            if (headAnim != null)
            {
                headAdded += AddClipsFromFolder(headAnim, "Assets/Animation/player/f2_em_head");
            }

            EditorUtility.SetDirty(go);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log($"✅ <color=green>[AnimationPopulator]</color> Đã nạp thành công <b>{bodyAdded}</b> clips Body và <b>{headAdded}</b> clips Head cho {go.name}!");
        }

        public static string TryFindAndAddMissingClip(Animation bodyAnim, Animation headAnim, string clipName)
        {
            if (string.IsNullOrEmpty(clipName)) return null;

            string[] guids = AssetDatabase.FindAssets($"{clipName} t:AnimationClip");
            AnimationClip foundBodyClip = null;
            AnimationClip foundHeadClip = null;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path).Equals(clipName, System.StringComparison.OrdinalIgnoreCase))
                {
                    var loadedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if (loadedClip != null)
                    {
                        if (path.IndexOf("head", System.StringComparison.OrdinalIgnoreCase) >= 0)
                            foundHeadClip = loadedClip;
                        else
                            foundBodyClip = loadedClip;
                    }
                }
            }

            if (foundBodyClip != null && bodyAnim != null)
            {
                bodyAnim.AddClip(foundBodyClip, foundBodyClip.name);
            }
            if (foundHeadClip != null && headAnim != null)
            {
                headAnim.AddClip(foundHeadClip, foundHeadClip.name);
            }

            if (foundBodyClip != null) return foundBodyClip.name;
            if (foundHeadClip != null) return foundHeadClip.name;
            return null;
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

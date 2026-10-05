using UnityEditor;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace TopDownGame.Editor
{
    public static class VietnameseFontAutoSetup
    {
        private const string FontPath = "Assets/Fonts/arial.ttf";
        private const string OutputSdfPath = "Assets/Fonts/Arial SDF.asset";
        private const string LiberationSansPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        [MenuItem("Tools/TopDownGame/Setup Vietnamese Font (Arial SDF)", false, 50)]
        public static void SetupVietnameseFont()
        {
            // 1. Kiểm tra file font TTF
            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null)
            {
                Debug.LogError($"❌ [VietnameseFontAutoSetup] Không tìm thấy file font tại '{FontPath}'! Vui lòng đảm bảo file arial.ttf nằm ở Assets/Fonts/arial.ttf.");
                return;
            }

            // 2. Tạo hoặc nạp Font Asset dạng Dynamic
            TMP_FontAsset arialSdf = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputSdfPath);
            if (arialSdf == null)
            {
                arialSdf = TMP_FontAsset.CreateFontAsset(font, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 512, 512, AtlasPopulationMode.Dynamic);
                if (arialSdf == null)
                {
                    Debug.LogError("❌ [VietnameseFontAutoSetup] Không thể tạo Dynamic TMP_FontAsset từ arial.ttf!");
                    return;
                }

                AssetDatabase.CreateAsset(arialSdf, OutputSdfPath);
                AssetDatabase.AddObjectToAsset(arialSdf.material, arialSdf);
                Debug.Log($"✅ Đã tạo Dynamic Font Asset mới tại: {OutputSdfPath}");
            }
            else
            {
                arialSdf.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                EditorUtility.SetDirty(arialSdf);
                Debug.Log($"ℹ️ Đã tìm thấy Font Asset hiện có tại: {OutputSdfPath}");
            }

            // 3. Thêm Arial SDF vào Fallback Font List của LiberationSans SDF
            TMP_FontAsset liberationSans = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LiberationSansPath);
            if (liberationSans != null)
            {
                if (liberationSans.fallbackFontAssetTable == null)
                {
                    liberationSans.fallbackFontAssetTable = new List<TMP_FontAsset>();
                }

                if (!liberationSans.fallbackFontAssetTable.Contains(arialSdf))
                {
                    liberationSans.fallbackFontAssetTable.Add(arialSdf);
                    EditorUtility.SetDirty(liberationSans);
                    Debug.Log("✅ Đã thêm Arial SDF vào Fallback list của LiberationSans SDF.");
                }
                else
                {
                    Debug.Log("ℹ️ Arial SDF đã có sẵn trong Fallback list của LiberationSans SDF.");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ Không tìm thấy LiberationSans SDF tại '{LiberationSansPath}'.");
            }

            // 4. Cập nhật TMP_Settings Fallback Font List
            TMP_Settings settings = TMP_Settings.instance;
            if (settings != null)
            {
                SerializedObject settingsSo = new SerializedObject(settings);
                SerializedProperty fallbackListProp = settingsSo.FindProperty("m_fallbackFontAssets");
                if (fallbackListProp != null)
                {
                    bool alreadyInList = false;
                    for (int i = 0; i < fallbackListProp.arraySize; i++)
                    {
                        if (fallbackListProp.GetArrayElementAtIndex(i).objectReferenceValue == arialSdf)
                        {
                            alreadyInList = true;
                            break;
                        }
                    }

                    if (!alreadyInList)
                    {
                        int index = fallbackListProp.arraySize;
                        fallbackListProp.InsertArrayElementAtIndex(index);
                        fallbackListProp.GetArrayElementAtIndex(index).objectReferenceValue = arialSdf;
                        settingsSo.ApplyModifiedProperties();
                        EditorUtility.SetDirty(settings);
                        Debug.Log("✅ Đã thêm Arial SDF vào Global Fallback list trong TMP Settings.");
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("🎉 [VietnameseFontAutoSetup] HOÀN TẤT! Toàn bộ UI TextMesh Pro trong dự án hiện đã hỗ trợ đầy đủ Tiếng Việt và các ký tự Unicode.");
        }
    }
}

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TopDownGame.Editor
{
    public class ComboTimingAnalyzer : EditorWindow
    {
        private AnimationClip currentClip;
        private AnimationClip nextClip;

        private float minSearchRatio = 0.25f;
        private float maxSearchRatio = 0.90f;
        private int sampleSteps = 200;

        private string analysisResult = "Chưa phân tích. Hãy kéo 2 clip vào để bắt đầu.";
        private float bestMatchTimestamp = -1f;

        [MenuItem("Tools/TopDownGame/Combo Timing Analyzer")]
        public static void ShowWindow()
        {
            var window = GetWindow<ComboTimingAnalyzer>("Combo Timing Analyzer");
            window.minSize = new Vector2(450, 420);
        }

        private void OnGUI()
        {
            GUILayout.Label("🤖 Công cụ tự động dò tìm điểm khớp Animation Combo", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Tool sẽ quét toàn bộ xương (Bones/Curves) của Clip 1 và so sánh với Frame 0 (Start) của Clip 2 để tìm ra khoảnh khắc 2 tư thế trùng khớp nhất để bạn điền vào bảng Skills.xlsx.", MessageType.Info);

            EditorGUILayout.Space(10);

            currentClip = (AnimationClip)EditorGUILayout.ObjectField("Clip hiện tại (vd: at01)", currentClip, typeof(AnimationClip), false);
            nextClip = (AnimationClip)EditorGUILayout.ObjectField("Clip tiếp theo (vd: at02)", nextClip, typeof(AnimationClip), false);

            EditorGUILayout.Space(10);
            GUILayout.Label("Cài đặt phân tích", EditorStyles.boldLabel);
            minSearchRatio = EditorGUILayout.Slider("Bắt đầu quét từ (%)", minSearchRatio, 0.1f, 0.5f);
            maxSearchRatio = EditorGUILayout.Slider("Kết thúc quét ở (%)", maxSearchRatio, 0.6f, 0.95f);
            sampleSteps = EditorGUILayout.IntSlider("Độ chi tiết quét (Steps)", sampleSteps, 100, 500);

            EditorGUILayout.Space(15);

            if (GUILayout.Button("🔍 PHÂN TÍCH VÀ TÌM MỐC THỜI GIAN KHỚP NHẤT", GUILayout.Height(35)))
            {
                AnalyzeMatchingTime();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(analysisResult, bestMatchTimestamp > 0 ? MessageType.Info : MessageType.None);
        }

        private void AnalyzeMatchingTime()
        {
            if (currentClip == null || nextClip == null)
            {
                analysisResult = "❌ Vui lòng kéo đủ cả 2 Animation Clip (Clip 1 và Clip 2) để phân tích!";
                bestMatchTimestamp = -1f;
                return;
            }

            EditorCurveBinding[] currentBindings = AnimationUtility.GetCurveBindings(currentClip);
            EditorCurveBinding[] nextBindings = AnimationUtility.GetCurveBindings(nextClip);

            var commonBindings = new List<(AnimationCurve curve1, AnimationCurve curve2)>();

            var nextCurveMap = new Dictionary<string, AnimationCurve>();
            foreach (var b in nextBindings)
            {
                string key = $"{b.path}/{b.propertyName}";
                AnimationCurve curve = AnimationUtility.GetEditorCurve(nextClip, b);
                if (curve != null) nextCurveMap[key] = curve;
            }

            foreach (var b in currentBindings)
            {
                string key = $"{b.path}/{b.propertyName}";
                if (nextCurveMap.TryGetValue(key, out AnimationCurve curve2))
                {
                    AnimationCurve curve1 = AnimationUtility.GetEditorCurve(currentClip, b);
                    if (curve1 != null)
                    {
                        commonBindings.Add((curve1, curve2));
                    }
                }
            }

            if (commonBindings.Count == 0)
            {
                analysisResult = "⚠️ Không tìm thấy khớp xương tương đồng giữa 2 clip.";
                bestMatchTimestamp = -1f;
                return;
            }

            float clipLength = currentClip.length;
            float startTime = clipLength * minSearchRatio;
            float endTime = clipLength * maxSearchRatio;
            float stepSize = (endTime - startTime) / sampleSteps;

            float minDifference = float.MaxValue;
            float bestTime = startTime;

            for (float t = startTime; t <= endTime; t += stepSize)
            {
                float totalDiff = 0f;

                foreach (var (curve1, curve2) in commonBindings)
                {
                    float val1 = curve1.Evaluate(t);
                    float val2 = curve2.Evaluate(0f);
                    float delta = val1 - val2;
                    totalDiff += delta * delta;
                }

                if (totalDiff < minDifference)
                {
                    minDifference = totalDiff;
                    bestTime = t;
                }
            }

            bestMatchTimestamp = (float)System.Math.Round(bestTime, 2);
            float percentage = (bestMatchTimestamp / clipLength) * 100f;

            analysisResult = $"✅ ĐÃ TÌM THẤY MỐC KHỚP NHẤT:\n\n" +
                             $"• combo_start tối ưu nhất: {bestMatchTimestamp:F2} giây (ở mức {percentage:F0}% độ dài clip)\n" +
                             $"• Gợi ý combo_end: {Mathf.Min(clipLength * 0.85f, bestMatchTimestamp + 0.35f):F2} giây\n" +
                             $"• Tổng độ dài clip {currentClip.name}: {clipLength:F2} giây";
        }
    }
}

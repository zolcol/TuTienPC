using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TopDownGame.Player;
using TopDownGame.Stats;
using TopDownGame.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace TopDownGame.Editor
{
    public static class CharacterStatsUIBuilder
    {
        private static readonly Color BgDarkColor = new Color(0.08f, 0.08f, 0.11f, 0.96f);
        private static readonly Color HeaderBgColor = new Color(0.12f, 0.12f, 0.16f, 0.98f);
        private static readonly Color SectionHeaderColor = new Color(0.15f, 0.15f, 0.20f, 0.85f);
        private static readonly Color RowBgColor = new Color(0.10f, 0.10f, 0.14f, 0.50f);
        private static readonly Color GoldColor = new Color(1f, 0.85f, 0.35f, 1f);
        private static readonly Color WhiteTextColor = new Color(0.92f, 0.92f, 0.95f, 1f);
        private static readonly Color LabelTextColor = new Color(0.70f, 0.72f, 0.78f, 1f);
        private static readonly Color BorderColor = new Color(0.24f, 0.24f, 0.30f, 0.9f);
        private static readonly Color ButtonNormalColor = new Color(0.18f, 0.18f, 0.24f, 1f);

        [MenuItem("Tools/TopDownGame/Create Character Stats UI", false, 11)]
        public static void GenerateCharacterStatsUI()
        {
            EnsureEventSystem();
            Canvas canvas = EnsureCanvas();

            // Tìm Player
            PlayerController player = Object.FindObjectOfType<PlayerController>();
            if (player != null && player.GetComponent<PlayerStats>() == null)
            {
                Undo.AddComponent<PlayerStats>(player.gameObject);
            }

            // Xóa cái cũ nếu có
            CharacterStatsUI oldUI = canvas.GetComponentInChildren<CharacterStatsUI>(true);
            if (oldUI != null)
            {
                Undo.DestroyObjectImmediate(oldUI.gameObject);
            }

            // Xóa nút mở cũ nếu có
            Transform oldToggleBtn = canvas.transform.Find("Btn_ToggleStats");
            if (oldToggleBtn != null)
            {
                Undo.DestroyObjectImmediate(oldToggleBtn.gameObject);
            }

            // 1. Tạo Nút Toggle Bảng ở góc dưới bên trái cạnh HUD
            GameObject toggleBtnObj = CreateToggleButton(canvas.transform);

            // 2. Tạo Cửa sổ bảng thuộc tính chính (Panel)
            GameObject panelObj = new GameObject("CharacterStatsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(CharacterStatsUI));
            Undo.RegisterCreatedObjectUndo(panelObj, "Create Character Stats Panel");
            panelObj.transform.SetParent(canvas.transform, false);

            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(620f, 800f);
            panelRect.anchoredPosition = new Vector2(0f, 0f);

            Image panelBg = panelObj.GetComponent<Image>();
            panelBg.color = BgDarkColor;

            // Thêm viền Outline
            Outline outline = panelObj.AddComponent<Outline>();
            outline.effectColor = BorderColor;
            outline.effectDistance = new Vector2(2f, -2f);

            CharacterStatsUI statsUI = panelObj.GetComponent<CharacterStatsUI>();
            Button toggleBtn = toggleBtnObj.GetComponent<Button>();

            // Gán Serialized Fields qua SerializedObject
            SerializedObject so = new SerializedObject(statsUI);
            so.FindProperty("player").objectReferenceValue = player;
            so.FindProperty("canvasGroup").objectReferenceValue = panelObj.GetComponent<CanvasGroup>();
            so.FindProperty("panelRect").objectReferenceValue = panelRect;
            so.FindProperty("toggleButton").objectReferenceValue = toggleBtn;

            // Header (Tiêu đề + Nút Đóng)
            Button closeBtn = CreateHeader(panelObj.transform, so);
            so.FindProperty("closeButton").objectReferenceValue = closeBtn;

            // Content Container (Scroll hoặc Vertical List)
            GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentObj.transform.SetParent(panelObj.transform, false);
            RectTransform contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = new Vector2(16f, 16f);
            contentRect.offsetMax = new Vector2(-16f, -64f);

            VerticalLayoutGroup vlg = contentObj.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.spacing = 6f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // 1. Cấp độ & Thanh EXP
            CreateLevelSection(contentObj.transform, so);

            // 2. Nhóm Sinh tồn (HP / MP)
            CreateSectionHeader(contentObj.transform, "SINH LỰC & NỘI LỰC");
            CreateStatRow(contentObj.transform, "Sinh Lực", so.FindProperty("hpText"), so.FindProperty("hpRegenText"));
            CreateStatRow(contentObj.transform, "Nội Lực", so.FindProperty("mpText"), so.FindProperty("mpRegenText"));

            // 3. Nhóm Tấn công
            CreateSectionHeader(contentObj.transform, "NĂNG LỰC TẤN CÔNG");
            CreateStatRow(contentObj.transform, "Công Vật Lý", so.FindProperty("physicalDamageText"), null);
            CreateStatRow(contentObj.transform, "Công Phép", so.FindProperty("magicDamageText"), null);
            CreateStatRow(contentObj.transform, "Tốc Độ Đánh", so.FindProperty("attackSpeedText"), null);
            CreateStatRow(contentObj.transform, "Tỉ Lệ Chí Mạng", so.FindProperty("critRateText"), null);
            CreateStatRow(contentObj.transform, "Sát Thương Chí Mạng", so.FindProperty("critDamageText"), null);

            // 4. Nhóm Phòng ngự & Thân pháp
            CreateSectionHeader(contentObj.transform, "PHÒNG THỦ & THÂN PHÁP");
            CreateStatRow(contentObj.transform, "Hộ Giáp Vật Lý", so.FindProperty("armorText"), null);
            CreateStatRow(contentObj.transform, "Kháng Phép", so.FindProperty("magicResistText"), null);
            CreateStatRow(contentObj.transform, "Tốc Độ Di Chuyển", so.FindProperty("moveSpeedText"), null);

            so.ApplyModifiedProperties();

            // Chọn panel trong Editor
            Selection.activeGameObject = panelObj;
            Debug.Log("✅ [CharacterStatsUIBuilder] Đã tạo thành công Bảng thuộc tính nhân vật (Character Stats UI)!");
        }

        private static GameObject CreateToggleButton(Transform parent)
        {
            GameObject btnObj = new GameObject("Btn_ToggleStats", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(180f, 50f);
            rt.anchoredPosition = new Vector2(28f, 28f);

            Image img = btnObj.GetComponent<Image>();
            img.color = ButtonNormalColor;

            Outline outline = btnObj.AddComponent<Outline>();
            outline.effectColor = BorderColor;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = "Thuộc Tính (C)";
            tmp.fontSize = 17;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = GoldColor;
            tmp.outlineColor = new Color32(10, 10, 15, 255);
            tmp.outlineWidth = 0.25f;

            return btnObj;
        }

        private static Button CreateHeader(Transform parent, SerializedObject so)
        {
            GameObject headerObj = new GameObject("Header", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            headerObj.transform.SetParent(parent, false);

            RectTransform rt = headerObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 58f);
            rt.anchoredPosition = Vector2.zero;

            Image img = headerObj.GetComponent<Image>();
            img.color = HeaderBgColor;

            // Title Text
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(headerObj.transform, false);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(18f, 0f);
            titleRt.offsetMax = new Vector2(-54f, 0f);

            TextMeshProUGUI tmp = titleObj.GetComponent<TextMeshProUGUI>();
            tmp.text = "THUỘC TÍNH NHÂN VẬT";
            tmp.fontSize = 22;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = GoldColor;
            tmp.outlineColor = new Color32(10, 10, 15, 255);
            tmp.outlineWidth = 0.25f;

            // Close Button
            GameObject closeBtnObj = new GameObject("Btn_Close", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            closeBtnObj.transform.SetParent(headerObj.transform, false);

            RectTransform closeRt = closeBtnObj.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 0.5f);
            closeRt.anchorMax = new Vector2(1f, 0.5f);
            closeRt.pivot = new Vector2(1f, 0.5f);
            closeRt.sizeDelta = new Vector2(38f, 38f);
            closeRt.anchoredPosition = new Vector2(-10f, 0f);

            Image closeImg = closeBtnObj.GetComponent<Image>();
            closeImg.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);

            GameObject xTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            xTextObj.transform.SetParent(closeBtnObj.transform, false);
            RectTransform xRt = xTextObj.GetComponent<RectTransform>();
            xRt.anchorMin = Vector2.zero;
            xRt.anchorMax = Vector2.one;
            xRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI xTmp = xTextObj.GetComponent<TextMeshProUGUI>();
            xTmp.text = "X";
            xTmp.fontSize = 21;
            xTmp.fontStyle = FontStyles.Bold;
            xTmp.alignment = TextAlignmentOptions.Center;
            xTmp.color = Color.white;

            return closeBtnObj.GetComponent<Button>();
        }

        private static void CreateLevelSection(Transform parent, SerializedObject so)
        {
            GameObject lvlObj = new GameObject("Section_Level", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            lvlObj.transform.SetParent(parent, false);

            RectTransform rt = lvlObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 74f);

            Image bg = lvlObj.GetComponent<Image>();
            bg.color = RowBgColor;

            // Level Text
            GameObject lvlTextObj = new GameObject("Txt_Level", typeof(RectTransform), typeof(TextMeshProUGUI));
            lvlTextObj.transform.SetParent(lvlObj.transform, false);
            RectTransform lvlRt = lvlTextObj.GetComponent<RectTransform>();
            lvlRt.anchorMin = new Vector2(0f, 0.45f);
            lvlRt.anchorMax = new Vector2(0.35f, 1f);
            lvlRt.offsetMin = new Vector2(14f, 0f);
            lvlRt.offsetMax = Vector2.zero;

            TextMeshProUGUI lvlTmp = lvlTextObj.GetComponent<TextMeshProUGUI>();
            lvlTmp.text = "Cấp: 1";
            lvlTmp.fontSize = 21;
            lvlTmp.fontStyle = FontStyles.Bold;
            lvlTmp.alignment = TextAlignmentOptions.MidlineLeft;
            lvlTmp.color = WhiteTextColor;
            lvlTmp.outlineColor = new Color32(10, 10, 15, 255);
            lvlTmp.outlineWidth = 0.2f;
            so.FindProperty("levelText").objectReferenceValue = lvlTmp;

            // Exp Text
            GameObject expTextObj = new GameObject("Txt_Exp", typeof(RectTransform), typeof(TextMeshProUGUI));
            expTextObj.transform.SetParent(lvlObj.transform, false);
            RectTransform expRt = expTextObj.GetComponent<RectTransform>();
            expRt.anchorMin = new Vector2(0.35f, 0.45f);
            expRt.anchorMax = new Vector2(1f, 1f);
            expRt.offsetMin = Vector2.zero;
            expRt.offsetMax = new Vector2(-14f, 0f);

            TextMeshProUGUI expTmp = expTextObj.GetComponent<TextMeshProUGUI>();
            expTmp.text = "EXP: 0 / 28,000";
            expTmp.fontSize = 17f;
            expTmp.alignment = TextAlignmentOptions.MidlineRight;
            expTmp.color = LabelTextColor;
            expTmp.outlineColor = new Color32(10, 10, 15, 255);
            expTmp.outlineWidth = 0.15f;
            so.FindProperty("expText").objectReferenceValue = expTmp;

            // Exp Slider
            GameObject sliderObj = new GameObject("Slider_Exp", typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(lvlObj.transform, false);
            RectTransform sliderRt = sliderObj.GetComponent<RectTransform>();
            sliderRt.anchorMin = new Vector2(0f, 0f);
            sliderRt.anchorMax = new Vector2(1f, 0.45f);
            sliderRt.offsetMin = new Vector2(14f, 8f);
            sliderRt.offsetMax = new Vector2(-14f, -4f);

            Slider slider = sliderObj.GetComponent<Slider>();
            slider.interactable = false;

            // Background of Slider
            GameObject sBg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            sBg.transform.SetParent(sliderObj.transform, false);
            RectTransform sBgRt = sBg.GetComponent<RectTransform>();
            sBgRt.anchorMin = Vector2.zero;
            sBgRt.anchorMax = Vector2.one;
            sBgRt.sizeDelta = Vector2.zero;
            sBg.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.06f, 0.9f);

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.sizeDelta = Vector2.zero;

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;
            Image fillImg = fill.GetComponent<Image>();
            fillImg.color = new Color(1.0f, 0.75f, 0.15f, 1f);

            slider.fillRect = fillRt;
            slider.value = 0f;
            so.FindProperty("expSlider").objectReferenceValue = slider;
        }

        private static void CreateSectionHeader(Transform parent, string title)
        {
            GameObject sectionObj = new GameObject("Header_" + title, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            sectionObj.transform.SetParent(parent, false);

            RectTransform rt = sectionObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 38f);

            Image img = sectionObj.GetComponent<Image>();
            img.color = SectionHeaderColor;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(sectionObj.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(14f, 0f);
            textRt.offsetMax = new Vector2(-14f, 0f);

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = title;
            tmp.fontSize = 18;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = GoldColor;
            tmp.outlineColor = new Color32(10, 10, 15, 255);
            tmp.outlineWidth = 0.2f;
        }

        private static void CreateStatRow(Transform parent, string labelName, SerializedProperty valProp, SerializedProperty subValProp)
        {
            GameObject rowObj = new GameObject("Row_" + labelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rowObj.transform.SetParent(parent, false);

            RectTransform rt = rowObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 40f);

            Image img = rowObj.GetComponent<Image>();
            img.color = RowBgColor;

            bool hasSubVal = (subValProp != null);

            // Label
            GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(rowObj.transform, false);
            RectTransform labelRt = labelObj.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = hasSubVal ? new Vector2(0.46f, 1f) : new Vector2(0.60f, 1f);
            labelRt.offsetMin = new Vector2(14f, 0f);
            labelRt.offsetMax = Vector2.zero;

            TextMeshProUGUI labelTmp = labelObj.GetComponent<TextMeshProUGUI>();
            labelTmp.text = labelName;
            labelTmp.fontSize = 18;
            labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
            labelTmp.color = LabelTextColor;
            labelTmp.outlineColor = new Color32(10, 10, 15, 255);
            labelTmp.outlineWidth = 0.15f;

            // Value
            GameObject valObj = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
            valObj.transform.SetParent(rowObj.transform, false);
            RectTransform valRt = valObj.GetComponent<RectTransform>();
            valRt.anchorMin = hasSubVal ? new Vector2(0.46f, 0f) : new Vector2(0.60f, 0f);
            valRt.anchorMax = hasSubVal ? new Vector2(0.76f, 1f) : new Vector2(1f, 1f);
            valRt.offsetMin = Vector2.zero;
            valRt.offsetMax = hasSubVal ? Vector2.zero : new Vector2(-14f, 0f);

            TextMeshProUGUI valTmp = valObj.GetComponent<TextMeshProUGUI>();
            valTmp.text = "0";
            valTmp.fontSize = 19;
            valTmp.fontStyle = FontStyles.Bold;
            valTmp.alignment = TextAlignmentOptions.MidlineRight;
            valTmp.color = WhiteTextColor;
            valTmp.outlineColor = new Color32(10, 10, 15, 255);
            valTmp.outlineWidth = 0.15f;

            if (valProp != null) valProp.objectReferenceValue = valTmp;

            // Sub Value (Regen rate if any)
            if (hasSubVal)
            {
                GameObject subValObj = new GameObject("SubValue", typeof(RectTransform), typeof(TextMeshProUGUI));
                subValObj.transform.SetParent(rowObj.transform, false);
                RectTransform subValRt = subValObj.GetComponent<RectTransform>();
                subValRt.anchorMin = new Vector2(0.76f, 0f);
                subValRt.anchorMax = new Vector2(1f, 1f);
                subValRt.offsetMin = Vector2.zero;
                subValRt.offsetMax = new Vector2(-14f, 0f);

                TextMeshProUGUI subValTmp = subValObj.GetComponent<TextMeshProUGUI>();
                subValTmp.text = "+0/s";
                subValTmp.fontSize = 17;
                subValTmp.fontStyle = FontStyles.Bold;
                subValTmp.alignment = TextAlignmentOptions.MidlineRight;
                subValTmp.color = new Color(0.35f, 0.90f, 0.45f, 1f);
                subValTmp.outlineColor = new Color32(10, 10, 15, 255);
                subValTmp.outlineWidth = 0.15f;

                subValProp.objectReferenceValue = subValTmp;
            }
        }

        private static Canvas EnsureCanvas()
        {
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
            }
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
            }
        }
    }
}

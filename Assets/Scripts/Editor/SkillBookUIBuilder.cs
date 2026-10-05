using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TopDownGame.Player;
using TopDownGame.Skills;
using TopDownGame.Stats;
using TopDownGame.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace TopDownGame.Editor
{
    public static class SkillBookUIBuilder
    {
        private static readonly Color BgDarkColor = new Color(0.08f, 0.08f, 0.11f, 0.96f);
        private static readonly Color PanelInnerBg = new Color(0.06f, 0.06f, 0.09f, 0.85f);
        private static readonly Color HeaderBgColor = new Color(0.12f, 0.12f, 0.16f, 0.98f);
        private static readonly Color SectionHeaderColor = new Color(0.15f, 0.15f, 0.20f, 0.85f);
        private static readonly Color RowBgColor = new Color(0.11f, 0.11f, 0.15f, 0.60f);
        private static readonly Color GoldColor = new Color(1f, 0.85f, 0.35f, 1f);
        private static readonly Color WhiteTextColor = new Color(0.92f, 0.92f, 0.95f, 1f);
        private static readonly Color LabelTextColor = new Color(0.70f, 0.72f, 0.78f, 1f);
        private static readonly Color BorderColor = new Color(0.24f, 0.24f, 0.30f, 0.9f);
        private static readonly Color ButtonNormalColor = new Color(0.18f, 0.18f, 0.24f, 1f);
        private static readonly Color UpgradeBtnColor = new Color(0.75f, 0.55f, 0.15f, 1f);

        [MenuItem("Tools/TopDownGame/Create Skill Book UI", false, 12)]
        public static void GenerateSkillBookUI()
        {
            EnsureEventSystem();
            Canvas canvas = EnsureCanvas();

            // Tìm Player
            PlayerController player = Object.FindObjectOfType<PlayerController>();
            if (player != null)
            {
                if (player.GetComponent<PlayerStats>() == null) Undo.AddComponent<PlayerStats>(player.gameObject);
                if (player.GetComponent<PlayerSkillManager>() == null) Undo.AddComponent<PlayerSkillManager>(player.gameObject);
            }

            // Xóa UI cũ nếu có
            SkillBookUI oldUI = canvas.GetComponentInChildren<SkillBookUI>(true);
            if (oldUI != null)
            {
                Undo.DestroyObjectImmediate(oldUI.gameObject);
            }

            // Xóa nút toggle cũ nếu có
            Transform oldToggleBtn = canvas.transform.Find("Btn_ToggleSkillBook");
            if (oldToggleBtn != null)
            {
                Undo.DestroyObjectImmediate(oldToggleBtn.gameObject);
            }

            // 1. Tạo Nút Toggle Bảng ở góc dưới bên trái (cạnh nút Thuộc Tính C)
            GameObject toggleBtnObj = CreateToggleButton(canvas.transform);

            // 2. Tạo Cửa sổ bảng kỹ năng chính (Panel)
            GameObject panelObj = new GameObject("SkillBookPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(SkillBookUI));
            Undo.RegisterCreatedObjectUndo(panelObj, "Create Skill Book Panel");
            panelObj.transform.SetParent(canvas.transform, false);

            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(1040f, 680f);
            panelRect.anchoredPosition = new Vector2(0f, 0f);

            Image panelBg = panelObj.GetComponent<Image>();
            panelBg.color = BgDarkColor;

            Outline outline = panelObj.AddComponent<Outline>();
            outline.effectColor = BorderColor;
            outline.effectDistance = new Vector2(2f, -2f);

            SkillBookUI skillUI = panelObj.GetComponent<SkillBookUI>();
            Button toggleBtn = toggleBtnObj.GetComponent<Button>();

            SerializedObject so = new SerializedObject(skillUI);
            so.FindProperty("player").objectReferenceValue = player;
            so.FindProperty("canvasGroup").objectReferenceValue = panelObj.GetComponent<CanvasGroup>();
            so.FindProperty("panelRect").objectReferenceValue = panelRect;
            so.FindProperty("toggleButton").objectReferenceValue = toggleBtn;

            // Header (Tiêu đề, Điểm SP, Nút Đóng)
            Button closeBtn = CreateHeader(panelObj.transform, so);
            so.FindProperty("closeButton").objectReferenceValue = closeBtn;

            // Body: Cột Trái (Danh sách) & Cột Phải (Chi tiết)
            CreateLeftColumn(panelObj.transform, so);
            CreateRightColumn(panelObj.transform, so);

            so.ApplyModifiedProperties();

            Selection.activeGameObject = panelObj;
            Debug.Log("✅ [SkillBookUIBuilder] Đã tạo thành công Bảng Võ Học & Kỹ Năng (Skill Book UI)!");
        }

        private static GameObject CreateToggleButton(Transform parent)
        {
            GameObject btnObj = new GameObject("Btn_ToggleSkillBook", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(180f, 50f);
            rt.anchoredPosition = new Vector2(220f, 28f); // Nằm cạnh nút Btn_ToggleStats (28 + 180 + 12 = 220)

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
            tmp.text = "Võ Học (K)";
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
            rt.sizeDelta = new Vector2(0f, 54f);
            rt.anchoredPosition = Vector2.zero;

            Image img = headerObj.GetComponent<Image>();
            img.color = HeaderBgColor;

            // Title Text
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(headerObj.transform, false);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.offsetMin = new Vector2(18f, 0f);
            titleRt.offsetMax = new Vector2(0f, 0f);

            TextMeshProUGUI tmp = titleObj.GetComponent<TextMeshProUGUI>();
            tmp.text = "VÕ HỌC & KỸ NĂNG";
            tmp.fontSize = 21;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = GoldColor;
            tmp.outlineColor = new Color32(10, 10, 15, 255);
            tmp.outlineWidth = 0.25f;

            // Skill Points Text
            GameObject spObj = new GameObject("Text_SkillPoints", typeof(RectTransform), typeof(TextMeshProUGUI));
            spObj.transform.SetParent(headerObj.transform, false);
            RectTransform spRt = spObj.GetComponent<RectTransform>();
            spRt.anchorMin = new Vector2(0.5f, 0f);
            spRt.anchorMax = new Vector2(1f, 1f);
            spRt.offsetMin = new Vector2(0f, 0f);
            spRt.offsetMax = new Vector2(-54f, 0f);

            TextMeshProUGUI spTmp = spObj.GetComponent<TextMeshProUGUI>();
            spTmp.text = "Điểm Kỹ Năng: <color=#FFD54F>0 SP</color>";
            spTmp.fontSize = 17;
            spTmp.fontStyle = FontStyles.Bold;
            spTmp.alignment = TextAlignmentOptions.MidlineRight;
            spTmp.color = WhiteTextColor;
            so.FindProperty("skillPointsText").objectReferenceValue = spTmp;

            // Close Button
            GameObject closeBtnObj = new GameObject("Btn_Close", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            closeBtnObj.transform.SetParent(headerObj.transform, false);

            RectTransform closeRt = closeBtnObj.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 0.5f);
            closeRt.anchorMax = new Vector2(1f, 0.5f);
            closeRt.pivot = new Vector2(1f, 0.5f);
            closeRt.sizeDelta = new Vector2(36f, 36f);
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
            xTmp.fontSize = 20;
            xTmp.fontStyle = FontStyles.Bold;
            xTmp.alignment = TextAlignmentOptions.Center;
            xTmp.color = Color.white;

            return closeBtnObj.GetComponent<Button>();
        }

        private static void CreateLeftColumn(Transform parent, SerializedObject so)
        {
            GameObject leftCol = new GameObject("Left_SkillListPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            leftCol.transform.SetParent(parent, false);

            RectTransform rt = leftCol.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.offsetMin = new Vector2(16f, 16f);
            rt.offsetMax = new Vector2(370f, -62f);

            Image bg = leftCol.GetComponent<Image>();
            bg.color = PanelInnerBg;

            Outline outline = leftCol.AddComponent<Outline>();
            outline.effectColor = BorderColor;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // Sub-header
            GameObject subHeader = new GameObject("SubHeader", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            subHeader.transform.SetParent(leftCol.transform, false);
            RectTransform subRt = subHeader.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0f, 1f);
            subRt.anchorMax = new Vector2(1f, 1f);
            subRt.pivot = new Vector2(0.5f, 1f);
            subRt.sizeDelta = new Vector2(0f, 36f);
            subRt.anchoredPosition = Vector2.zero;
            subHeader.GetComponent<Image>().color = SectionHeaderColor;

            GameObject subTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            subTextObj.transform.SetParent(subHeader.transform, false);
            RectTransform subTextRt = subTextObj.GetComponent<RectTransform>();
            subTextRt.anchorMin = Vector2.zero;
            subTextRt.anchorMax = Vector2.one;
            subTextRt.offsetMin = new Vector2(12f, 0f);
            subTextRt.offsetMax = new Vector2(-12f, 0f);
            TextMeshProUGUI subTmp = subTextObj.GetComponent<TextMeshProUGUI>();
            subTmp.text = "DANH SÁCH CHIÊU THỨC";
            subTmp.fontSize = 14;
            subTmp.fontStyle = FontStyles.Bold;
            subTmp.alignment = TextAlignmentOptions.MidlineLeft;
            subTmp.color = LabelTextColor;

            // Scroll View
            GameObject scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollObj.transform.SetParent(leftCol.transform, false);
            RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(6f, 6f);
            scrollRt.offsetMax = new Vector2(-6f, -40f);

            ScrollRect sr = scrollObj.GetComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;

            // Viewport
            GameObject viewObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewObj.transform.SetParent(scrollObj.transform, false);
            RectTransform viewRt = viewObj.GetComponent<RectTransform>();
            viewRt.anchorMin = Vector2.zero;
            viewRt.anchorMax = Vector2.one;
            viewRt.sizeDelta = Vector2.zero;
            sr.viewport = viewRt;

            // Content
            GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObj.transform.SetParent(viewObj.transform, false);
            RectTransform contentRt = contentObj.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0f, 0f);
            sr.content = contentRt;

            VerticalLayoutGroup vlg = contentObj.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.spacing = 8f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = contentObj.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            so.FindProperty("skillListContent").objectReferenceValue = contentRt;

            // Template Item Card
            GameObject itemTemplate = CreateItemTemplate(contentObj.transform);
            so.FindProperty("itemTemplate").objectReferenceValue = itemTemplate.GetComponent<SkillBookItemUI>();
        }

        private static GameObject CreateItemTemplate(Transform parent)
        {
            GameObject cardObj = new GameObject("ItemTemplate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(SkillBookItemUI));
            cardObj.transform.SetParent(parent, false);

            RectTransform cardRt = cardObj.GetComponent<RectTransform>();
            cardRt.sizeDelta = new Vector2(0f, 66f);

            Image cardBg = cardObj.GetComponent<Image>();
            cardBg.color = RowBgColor;

            // Selection Border (Outline image)
            GameObject selObj = new GameObject("SelectionBorder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            selObj.transform.SetParent(cardObj.transform, false);
            RectTransform selRt = selObj.GetComponent<RectTransform>();
            selRt.anchorMin = Vector2.zero;
            selRt.anchorMax = Vector2.one;
            selRt.sizeDelta = Vector2.zero;
            Image selImg = selObj.GetComponent<Image>();
            selImg.color = new Color(1f, 0.85f, 0.35f, 0.15f);
            Outline selOutline = selObj.AddComponent<Outline>();
            selOutline.effectColor = GoldColor;
            selOutline.effectDistance = new Vector2(1.5f, -1.5f);
            selObj.SetActive(false);

            // Icon
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObj.transform.SetParent(cardObj.transform, false);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.sizeDelta = new Vector2(48f, 48f);
            iconRt.anchoredPosition = new Vector2(10f, 0f);
            Image iconImg = iconObj.GetComponent<Image>();
            iconImg.color = Color.white;

            // Name
            GameObject nameObj = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameObj.transform.SetParent(cardObj.transform, false);
            RectTransform nameRt = nameObj.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0.5f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.offsetMin = new Vector2(66f, 0f);
            nameRt.offsetMax = new Vector2(-60f, -4f);
            TextMeshProUGUI nameTmp = nameObj.GetComponent<TextMeshProUGUI>();
            nameTmp.text = "Tên Kỹ Năng";
            nameTmp.fontSize = 17;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.alignment = TextAlignmentOptions.BottomLeft;
            nameTmp.color = WhiteTextColor;

            // Level Text
            GameObject lvObj = new GameObject("LevelText", typeof(RectTransform), typeof(TextMeshProUGUI));
            lvObj.transform.SetParent(cardObj.transform, false);
            RectTransform lvRt = lvObj.GetComponent<RectTransform>();
            lvRt.anchorMin = new Vector2(0f, 0f);
            lvRt.anchorMax = new Vector2(1f, 0.5f);
            lvRt.offsetMin = new Vector2(66f, 4f);
            lvRt.offsetMax = new Vector2(-60f, 0f);
            TextMeshProUGUI lvTmp = lvObj.GetComponent<TextMeshProUGUI>();
            lvTmp.text = "Lv.1";
            lvTmp.fontSize = 14;
            lvTmp.alignment = TextAlignmentOptions.TopLeft;
            lvTmp.color = LabelTextColor;

            // Slot Badge (e.g. [ Q ])
            GameObject badgeObj = new GameObject("SlotBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            badgeObj.transform.SetParent(cardObj.transform, false);
            RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(1f, 0.5f);
            badgeRt.anchorMax = new Vector2(1f, 0.5f);
            badgeRt.pivot = new Vector2(1f, 0.5f);
            badgeRt.sizeDelta = new Vector2(50f, 28f);
            badgeRt.anchoredPosition = new Vector2(-8f, 0f);
            Image badgeImg = badgeObj.GetComponent<Image>();
            badgeImg.color = new Color(0.15f, 0.35f, 0.2f, 0.9f);

            GameObject badgeTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            badgeTextObj.transform.SetParent(badgeObj.transform, false);
            RectTransform badgeTextRt = badgeTextObj.GetComponent<RectTransform>();
            badgeTextRt.anchorMin = Vector2.zero;
            badgeTextRt.anchorMax = Vector2.one;
            badgeTextRt.sizeDelta = Vector2.zero;
            TextMeshProUGUI badgeTmp = badgeTextObj.GetComponent<TextMeshProUGUI>();
            badgeTmp.text = "[ Q ]";
            badgeTmp.fontSize = 14;
            badgeTmp.fontStyle = FontStyles.Bold;
            badgeTmp.alignment = TextAlignmentOptions.Center;
            badgeTmp.color = GoldColor;

            // Connect Item fields
            SkillBookItemUI itemUI = cardObj.GetComponent<SkillBookItemUI>();
            SerializedObject itemSo = new SerializedObject(itemUI);
            itemSo.FindProperty("iconImage").objectReferenceValue = iconImg;
            itemSo.FindProperty("nameText").objectReferenceValue = nameTmp;
            itemSo.FindProperty("levelText").objectReferenceValue = lvTmp;
            itemSo.FindProperty("slotBadgeText").objectReferenceValue = badgeTmp;
            itemSo.FindProperty("slotBadgeObj").objectReferenceValue = badgeObj;
            itemSo.FindProperty("selectionBorder").objectReferenceValue = selImg;
            itemSo.FindProperty("selectButton").objectReferenceValue = cardObj.GetComponent<Button>();
            itemSo.ApplyModifiedProperties();

            return cardObj;
        }

        private static void CreateRightColumn(Transform parent, SerializedObject so)
        {
            GameObject rightCol = new GameObject("Right_DetailPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rightCol.transform.SetParent(parent, false);

            RectTransform rt = rightCol.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.offsetMin = new Vector2(386f, 16f);
            rt.offsetMax = new Vector2(-16f, -62f);

            Image bg = rightCol.GetComponent<Image>();
            bg.color = PanelInnerBg;

            Outline outline = rightCol.AddComponent<Outline>();
            outline.effectColor = BorderColor;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // 1. Header Detail (Icon lớn, Tên, Loại chiêu, Cấp độ)
            CreateDetailHeader(rightCol.transform, so);

            // 2. Mô tả (Description)
            CreateDetailDescription(rightCol.transform, so);

            // 3. Bảng thông số (Stats Grid)
            CreateDetailStats(rightCol.transform, so);

            // 4. Khu vực Gán Phím Nhanh (Q, E, R)
            CreateAssignHotbarSection(rightCol.transform, so);

            // 5. Khu vực Nâng Cấp (Upgrade)
            CreateUpgradeSection(rightCol.transform, so);
        }

        private static void CreateDetailHeader(Transform parent, SerializedObject so)
        {
            GameObject headerObj = new GameObject("DetailHeader", typeof(RectTransform));
            headerObj.transform.SetParent(parent, false);
            RectTransform rt = headerObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 86f);
            rt.anchoredPosition = new Vector2(0f, -8f);

            // Big Icon
            GameObject iconObj = new GameObject("DetailIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObj.transform.SetParent(headerObj.transform, false);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.sizeDelta = new Vector2(68f, 68f);
            iconRt.anchoredPosition = new Vector2(16f, 0f);
            Image iconImg = iconObj.GetComponent<Image>();
            iconImg.color = Color.white;
            Outline iconOutline = iconObj.AddComponent<Outline>();
            iconOutline.effectColor = GoldColor;
            iconOutline.effectDistance = new Vector2(1.5f, -1.5f);
            so.FindProperty("detailIcon").objectReferenceValue = iconImg;

            // Name
            GameObject nameObj = new GameObject("DetailName", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameObj.transform.SetParent(headerObj.transform, false);
            RectTransform nameRt = nameObj.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 1f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.pivot = new Vector2(0f, 1f);
            nameRt.offsetMin = new Vector2(96f, -34f);
            nameRt.offsetMax = new Vector2(-16f, 0f);
            TextMeshProUGUI nameTmp = nameObj.GetComponent<TextMeshProUGUI>();
            nameTmp.text = "Tên Kỹ Năng";
            nameTmp.fontSize = 22;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.color = GoldColor;
            so.FindProperty("detailNameText").objectReferenceValue = nameTmp;

            // Type
            GameObject typeObj = new GameObject("DetailType", typeof(RectTransform), typeof(TextMeshProUGUI));
            typeObj.transform.SetParent(headerObj.transform, false);
            RectTransform typeRt = typeObj.GetComponent<RectTransform>();
            typeRt.anchorMin = new Vector2(0f, 0f);
            typeRt.anchorMax = new Vector2(0.6f, 1f);
            typeRt.offsetMin = new Vector2(96f, 6f);
            typeRt.offsetMax = new Vector2(0f, -36f);
            TextMeshProUGUI typeTmp = typeObj.GetComponent<TextMeshProUGUI>();
            typeTmp.text = "Kỹ Năng Võ Học";
            typeTmp.fontSize = 15;
            typeTmp.fontStyle = FontStyles.Italic;
            typeTmp.color = LabelTextColor;
            so.FindProperty("detailTypeText").objectReferenceValue = typeTmp;

            // Level Text
            GameObject lvObj = new GameObject("DetailLevel", typeof(RectTransform), typeof(TextMeshProUGUI));
            lvObj.transform.SetParent(headerObj.transform, false);
            RectTransform lvRt = lvObj.GetComponent<RectTransform>();
            lvRt.anchorMin = new Vector2(0.6f, 0f);
            lvRt.anchorMax = new Vector2(1f, 1f);
            lvRt.offsetMin = new Vector2(0f, 6f);
            lvRt.offsetMax = new Vector2(-16f, -36f);
            TextMeshProUGUI lvTmp = lvObj.GetComponent<TextMeshProUGUI>();
            lvTmp.text = "Cấp độ: <color=#FFD54F>1</color> / 5";
            lvTmp.fontSize = 16;
            lvTmp.fontStyle = FontStyles.Bold;
            lvTmp.alignment = TextAlignmentOptions.MidlineRight;
            lvTmp.color = WhiteTextColor;
            so.FindProperty("detailLevelText").objectReferenceValue = lvTmp;

            // Separator
            CreateSeparator(parent, -96f);
        }

        private static void CreateDetailDescription(Transform parent, SerializedObject so)
        {
            GameObject descBox = new GameObject("DescBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            descBox.transform.SetParent(parent, false);
            RectTransform rt = descBox.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 86f);
            rt.anchoredPosition = new Vector2(0f, -102f);
            rt.offsetMin = new Vector2(16f, rt.offsetMin.y);
            rt.offsetMax = new Vector2(-16f, rt.offsetMax.y);
            descBox.GetComponent<Image>().color = new Color(0.10f, 0.10f, 0.14f, 0.5f);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(descBox.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(12f, 8f);
            textRt.offsetMax = new Vector2(-12f, -8f);

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = "Mô tả kỹ năng chi tiết sẽ hiển thị ở đây.";
            tmp.fontSize = 15f;
            tmp.color = WhiteTextColor;
            tmp.enableWordWrapping = true;
            so.FindProperty("detailDescriptionText").objectReferenceValue = tmp;
        }

        private static void CreateDetailStats(Transform parent, SerializedObject so)
        {
            GameObject statsObj = new GameObject("StatsGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            statsObj.transform.SetParent(parent, false);
            RectTransform rt = statsObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 76f);
            rt.anchoredPosition = new Vector2(0f, -196f);
            rt.offsetMin = new Vector2(16f, rt.offsetMin.y);
            rt.offsetMax = new Vector2(-16f, rt.offsetMax.y);

            GridLayoutGroup glg = statsObj.GetComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(286f, 32f);
            glg.spacing = new Vector2(12f, 8f);

            so.FindProperty("detailCooldownText").objectReferenceValue = CreateStatBox(statsObj.transform, "Hồi chiêu:", "10.0s");
            so.FindProperty("detailManaCostText").objectReferenceValue = CreateStatBox(statsObj.transform, "Tiêu hao:", "50 MP");
            so.FindProperty("detailDamageText").objectReferenceValue = CreateStatBox(statsObj.transform, "Sát thương:", "20");
            so.FindProperty("detailScalingText").objectReferenceValue = CreateStatBox(statsObj.transform, "Tỉ lệ công:", "200% Vật Lý");

            CreateSeparator(parent, -280f);
        }

        private static TextMeshProUGUI CreateStatBox(Transform parent, string label, string defaultValue)
        {
            GameObject boxObj = new GameObject("StatBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            boxObj.transform.SetParent(parent, false);
            boxObj.GetComponent<Image>().color = RowBgColor;

            GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(boxObj.transform, false);
            RectTransform labelRt = labelObj.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(0.48f, 1f);
            labelRt.offsetMin = new Vector2(10f, 0f);
            labelRt.offsetMax = Vector2.zero;
            TextMeshProUGUI lTmp = labelObj.GetComponent<TextMeshProUGUI>();
            lTmp.text = label;
            lTmp.fontSize = 15;
            lTmp.alignment = TextAlignmentOptions.MidlineLeft;
            lTmp.color = LabelTextColor;

            GameObject valObj = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
            valObj.transform.SetParent(boxObj.transform, false);
            RectTransform valRt = valObj.GetComponent<RectTransform>();
            valRt.anchorMin = new Vector2(0.48f, 0f);
            valRt.anchorMax = new Vector2(1f, 1f);
            valRt.offsetMin = Vector2.zero;
            valRt.offsetMax = new Vector2(-10f, 0f);
            TextMeshProUGUI vTmp = valObj.GetComponent<TextMeshProUGUI>();
            vTmp.text = defaultValue;
            vTmp.fontSize = 15.5f;
            vTmp.fontStyle = FontStyles.Bold;
            vTmp.alignment = TextAlignmentOptions.MidlineRight;
            vTmp.color = WhiteTextColor;

            return vTmp;
        }

        private static void CreateAssignHotbarSection(Transform parent, SerializedObject so)
        {
            GameObject assignGroup = new GameObject("AssignHotbarSection", typeof(RectTransform));
            assignGroup.transform.SetParent(parent, false);
            RectTransform rt = assignGroup.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 84f);
            rt.anchoredPosition = new Vector2(0f, -288f);
            rt.offsetMin = new Vector2(16f, rt.offsetMin.y);
            rt.offsetMax = new Vector2(-16f, rt.offsetMax.y);

            // Title
            GameObject titleObj = new GameObject("SectionTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(assignGroup.transform, false);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(0f, 26f);
            titleRt.anchoredPosition = Vector2.zero;
            TextMeshProUGUI tTmp = titleObj.GetComponent<TextMeshProUGUI>();
            tTmp.text = "GÁN PHÍM NHANH (HOTBAR)";
            tTmp.fontSize = 14f;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.color = LabelTextColor;

            // Horizontal Button Group
            GameObject btnRow = new GameObject("ButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            btnRow.transform.SetParent(assignGroup.transform, false);
            RectTransform rowRt = btnRow.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0f, 0f);
            rowRt.anchorMax = new Vector2(1f, 1f);
            rowRt.offsetMin = Vector2.zero;
            rowRt.offsetMax = new Vector2(0f, -28f);

            HorizontalLayoutGroup hlg = btnRow.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            var (btnQ, txtQ) = CreateActionButton(btnRow.transform, "Gán vào [ Q ]");
            var (btnE, txtE) = CreateActionButton(btnRow.transform, "Gán vào [ E ]");
            var (btnR, txtR) = CreateActionButton(btnRow.transform, "Gán vào [ R ]");

            so.FindProperty("assignQButton").objectReferenceValue = btnQ;
            so.FindProperty("assignQText").objectReferenceValue = txtQ;
            so.FindProperty("assignEButton").objectReferenceValue = btnE;
            so.FindProperty("assignEText").objectReferenceValue = txtE;
            so.FindProperty("assignRButton").objectReferenceValue = btnR;
            so.FindProperty("assignRText").objectReferenceValue = txtR;

            CreateSeparator(parent, -380f);
        }

        private static void CreateUpgradeSection(Transform parent, SerializedObject so)
        {
            GameObject upgradeGroup = new GameObject("UpgradeSection", typeof(RectTransform));
            upgradeGroup.transform.SetParent(parent, false);
            RectTransform rt = upgradeGroup.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(16f, 12f);
            rt.offsetMax = new Vector2(-16f, -388f);

            // Title
            GameObject titleObj = new GameObject("SectionTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(upgradeGroup.transform, false);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(0f, 26f);
            titleRt.anchoredPosition = Vector2.zero;
            TextMeshProUGUI tTmp = titleObj.GetComponent<TextMeshProUGUI>();
            tTmp.text = "CỘNG ĐIỂM NÂNG CẤP VÕ HỌC";
            tTmp.fontSize = 14f;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.color = LabelTextColor;

            // Requirement Text
            GameObject reqObj = new GameObject("ReqText", typeof(RectTransform), typeof(TextMeshProUGUI));
            reqObj.transform.SetParent(upgradeGroup.transform, false);
            RectTransform reqRt = reqObj.GetComponent<RectTransform>();
            reqRt.anchorMin = new Vector2(0f, 1f);
            reqRt.anchorMax = new Vector2(1f, 1f);
            reqRt.pivot = new Vector2(0f, 1f);
            reqRt.sizeDelta = new Vector2(0f, 24f);
            reqRt.anchoredPosition = new Vector2(0f, -28f);
            TextMeshProUGUI reqTmp = reqObj.GetComponent<TextMeshProUGUI>();
            reqTmp.text = "Yêu cầu: Nhân vật Cấp 1 | Tốn 1 SP";
            reqTmp.fontSize = 15;
            reqTmp.color = LabelTextColor;
            so.FindProperty("upgradeRequirementText").objectReferenceValue = reqTmp;

            // Next Level Preview
            GameObject prevObj = new GameObject("PreviewText", typeof(RectTransform), typeof(TextMeshProUGUI));
            prevObj.transform.SetParent(upgradeGroup.transform, false);
            RectTransform prevRt = prevObj.GetComponent<RectTransform>();
            prevRt.anchorMin = new Vector2(0f, 1f);
            prevRt.anchorMax = new Vector2(1f, 1f);
            prevRt.pivot = new Vector2(0f, 1f);
            prevRt.sizeDelta = new Vector2(0f, 24f);
            prevRt.anchoredPosition = new Vector2(0f, -52f);
            TextMeshProUGUI prevTmp = prevObj.GetComponent<TextMeshProUGUI>();
            prevTmp.text = "Cấp tiếp theo: +15% Hiệu quả";
            prevTmp.fontSize = 15;
            prevTmp.color = GoldColor;
            so.FindProperty("nextLevelPreviewText").objectReferenceValue = prevTmp;

            // Upgrade Button
            GameObject btnObj = new GameObject("Btn_Upgrade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(upgradeGroup.transform, false);
            RectTransform btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0f, 0f);
            btnRt.anchorMax = new Vector2(1f, 0f);
            btnRt.pivot = new Vector2(0.5f, 0f);
            btnRt.sizeDelta = new Vector2(0f, 48f);
            btnRt.anchoredPosition = Vector2.zero;

            Image btnImg = btnObj.GetComponent<Image>();
            btnImg.color = UpgradeBtnColor;

            Outline btnOutline = btnObj.AddComponent<Outline>();
            btnOutline.effectColor = GoldColor;
            btnOutline.effectDistance = new Vector2(1.5f, -1.5f);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI btnTmp = textObj.GetComponent<TextMeshProUGUI>();
            btnTmp.text = "+ NÂNG CẤP (1 SP)";
            btnTmp.fontSize = 18;
            btnTmp.fontStyle = FontStyles.Bold;
            btnTmp.alignment = TextAlignmentOptions.Center;
            btnTmp.color = Color.white;
            btnTmp.outlineColor = new Color32(10, 10, 15, 255);
            btnTmp.outlineWidth = 0.25f;

            so.FindProperty("upgradeButton").objectReferenceValue = btnObj.GetComponent<Button>();
            so.FindProperty("upgradeButtonText").objectReferenceValue = btnTmp;
        }

        private static (Button, TextMeshProUGUI) CreateActionButton(Transform parent, string defaultLabel)
        {
            GameObject btnObj = new GameObject("Btn_Action", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            Image img = btnObj.GetComponent<Image>();
            img.color = ButtonNormalColor;

            Outline outline = btnObj.AddComponent<Outline>();
            outline.effectColor = BorderColor;
            outline.effectDistance = new Vector2(1f, -1f);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = defaultLabel;
            tmp.fontSize = 15.5f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = WhiteTextColor;

            return (btnObj.GetComponent<Button>(), tmp);
        }

        private static void CreateSeparator(Transform parent, float anchoredY)
        {
            GameObject sep = new GameObject("Separator", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            sep.transform.SetParent(parent, false);
            RectTransform rt = sep.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 1.5f);
            rt.anchoredPosition = new Vector2(0f, anchoredY);
            rt.offsetMin = new Vector2(12f, rt.offsetMin.y);
            rt.offsetMax = new Vector2(-12f, rt.offsetMax.y);
            sep.GetComponent<Image>().color = new Color(0.20f, 0.20f, 0.28f, 0.6f);
        }

        private static Canvas EnsureCanvas()
        {
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("MainCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            EventSystem es = Object.FindObjectOfType<EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
            }
        }
    }
}

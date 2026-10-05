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
    public static class PlayerHUDBuilder
    {
        private static readonly Color BgDarkColor = new Color(0.06f, 0.06f, 0.08f, 0.92f);
        private static readonly Color FrameBorderColor = new Color(0.18f, 0.18f, 0.22f, 0.95f);

        private static readonly Color HpBgColor = new Color(0.20f, 0.04f, 0.04f, 0.95f);
        private static readonly Color HpGhostColor = new Color(1f, 0.78f, 0.10f, 1f); // Vạch vàng tụt theo
        private static readonly Color HpFillColor = new Color(0.92f, 0.16f, 0.16f, 1f); // Đỏ tươi

        private static readonly Color MpBgColor = new Color(0.04f, 0.09f, 0.22f, 0.95f);
        private static readonly Color MpFillColor = new Color(0.15f, 0.60f, 1f, 1f); // Xanh lam sáng

        private static readonly Color ExpBgColor = new Color(0.18f, 0.14f, 0.04f, 0.95f);
        private static readonly Color ExpFillColor = new Color(1.0f, 0.72f, 0.12f, 1f); // Vàng cam sáng
        private static readonly Color LevelBadgeBgColor = new Color(0.12f, 0.10f, 0.05f, 0.95f);

        private static readonly Color CooldownOverlayColor = new Color(0f, 0f, 0f, 0.75f);
        private static readonly Color GoldTextColor = new Color(1f, 0.88f, 0.45f, 1f);

        [MenuItem("Tools/TopDownGame/Create Player HUD", false, 10)]
        public static void GenerateHUD()
        {
            // 1. Kiểm tra EventSystem
            EnsureEventSystem();

            // 2. Tìm hoặc Tạo Canvas
            Canvas canvas = EnsureCanvas();

            // 3. Tự động kiểm tra Player và gắn PlayerStats nếu chưa có
            PlayerController player = Object.FindObjectOfType<PlayerController>();
            if (player != null && player.GetComponent<PlayerStats>() == null)
            {
                Undo.AddComponent<PlayerStats>(player.gameObject);
                Debug.Log("✅ [HUD Builder] Đã tự động thêm component PlayerStats vào Player GameObject.");
            }

            // 4. Xóa PlayerHUD cũ nếu đã tồn tại để cập nhật phiên bản mới
            PlayerHUD oldHUD = canvas.GetComponentInChildren<PlayerHUD>(true);
            if (oldHUD != null)
            {
                Undo.DestroyObjectImmediate(oldHUD.gameObject);
            }

            // 5. Tạo root GameObject PlayerHUD
            GameObject hudRoot = new GameObject("PlayerHUD", typeof(RectTransform), typeof(PlayerHUD));
            Undo.RegisterCreatedObjectUndo(hudRoot, "Create Player HUD");
            hudRoot.transform.SetParent(canvas.transform, false);

            RectTransform hudRect = hudRoot.GetComponent<RectTransform>();
            hudRect.anchorMin = Vector2.zero;
            hudRect.anchorMax = Vector2.one;
            hudRect.sizeDelta = Vector2.zero;
            hudRect.anchoredPosition = Vector2.zero;

            PlayerHUD playerHUD = hudRoot.GetComponent<PlayerHUD>();

            // Lấy hoặc tạo Sprite trắng phẳng (không bo cong) để hỗ trợ Image.Type.Filled
            Sprite flatSprite = GetOrCreateFlatWhiteSprite();

            // 6. Tạo cụm Thanh Máu, Mana, EXP & Level Badge ở Góc trên bên Trái (Top-Left)
            CreateStatusPanel(hudRoot.transform, flatSprite, 
                out Image healthFill, out Image healthGhostFill, out TextMeshProUGUI healthText, 
                out Image manaFill, out TextMeshProUGUI manaText,
                out Image expFill, out TextMeshProUGUI expText, out TextMeshProUGUI levelText);

            // 7. Tạo cụm 3 Ô Kỹ Năng ở Dưới cùng Chính giữa theo thứ tự Q - E - R
            CreateSkillPanel(hudRoot.transform, flatSprite, out SkillSlotUI slotQ, out SkillSlotUI slotE, out SkillSlotUI slotR);

            // 8. Tự động kết nối toàn bộ References vào PlayerHUD qua SerializedObject
            SerializedObject hudSO = new SerializedObject(playerHUD);
            if (player != null)
            {
                hudSO.FindProperty("player").objectReferenceValue = player;
            }
            hudSO.FindProperty("healthFill").objectReferenceValue = healthFill;
            hudSO.FindProperty("healthGhostFill").objectReferenceValue = healthGhostFill;
            hudSO.FindProperty("healthText").objectReferenceValue = healthText;
            hudSO.FindProperty("manaFill").objectReferenceValue = manaFill;
            hudSO.FindProperty("manaText").objectReferenceValue = manaText;
            hudSO.FindProperty("expFill").objectReferenceValue = expFill;
            hudSO.FindProperty("expText").objectReferenceValue = expText;
            hudSO.FindProperty("levelText").objectReferenceValue = levelText;
            hudSO.FindProperty("skillSlot_Q").objectReferenceValue = slotQ;
            hudSO.FindProperty("skillSlot_E").objectReferenceValue = slotE;
            hudSO.FindProperty("skillSlot_R").objectReferenceValue = slotR;
            hudSO.ApplyModifiedProperties();

            Selection.activeGameObject = hudRoot;
            EditorUtility.SetDirty(hudRoot);

            EditorUtility.DisplayDialog(
                "Thành Công!",
                "Đã tạo giao diện HUD hoàn chỉnh:\n" +
                "• Cụm Status Góc trên Trái: Thanh Máu (HP), Mana (MP), Vạch EXP và Huy hiệu Cấp độ (Lv.1)\n" +
                "• 3 Ô Kỹ năng: Dưới cùng Chính giữa (Thứ tự: Q - E - R)\n" +
                "• Canvas đã được gắn khít vào Main Camera\n" +
                (player != null ? "• Đã tự động liên kết với PlayerController và PlayerStats trong Scene!" : "• Lưu ý: Chưa tìm thấy PlayerController trong Scene."),
                "Tuyệt vời"
            );
        }

        private static Sprite GetOrCreateFlatWhiteSprite()
        {
            string assetPath = "Assets/UI/FlatWhite.png";
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            }
            return sprite;
        }

        private static Canvas EnsureCanvas()
        {
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
                canvas = canvasGO.GetComponent<Canvas>();
            }

            // Gắn khít Canvas vào Camera thay vì để Overlay giữa map 3D
            UnityEngine.Camera mainCam = UnityEngine.Camera.main ?? Object.FindObjectOfType<UnityEngine.Camera>();
            if (mainCam != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = mainCam;
                canvas.planeDistance = 1f;
                canvas.sortingOrder = 100;
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            SetLayerRecursively(canvas.gameObject, GameConstants.Layers.UILayer);

            return canvas;
        }

        private static void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (newLayer < 0) return;
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }

        private static void EnsureEventSystem()
        {
            EventSystem es = Object.FindObjectOfType<EventSystem>();
            if (es == null)
            {
                GameObject esGO = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                Undo.RegisterCreatedObjectUndo(esGO, "Create EventSystem");
            }
        }

        private static void CreateStatusPanel(Transform parent, Sprite flatSprite, 
            out Image healthFill, out Image healthGhostFill, out TextMeshProUGUI healthText, 
            out Image manaFill, out TextMeshProUGUI manaText,
            out Image expFill, out TextMeshProUGUI expText, out TextMeshProUGUI levelText)
        {
            // Panel nền góc trên-trái (Chuẩn tỷ lệ Full HD 1080p)
            GameObject panelGO = new GameObject("StatusPanel_TopLeft", typeof(RectTransform), typeof(Image));
            panelGO.transform.SetParent(parent, false);

            RectTransform panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(24f, -24f);
            panelRect.sizeDelta = new Vector2(420f, 126f);

            Image panelImg = panelGO.GetComponent<Image>();
            panelImg.sprite = flatSprite;
            panelImg.color = BgDarkColor;

            // 1. THANH MÁU (HEALTH BAR)
            GameObject hpBarGO = new GameObject("HealthBar", typeof(RectTransform), typeof(Image));
            hpBarGO.transform.SetParent(panelGO.transform, false);
            RectTransform hpBarRect = hpBarGO.GetComponent<RectTransform>();
            hpBarRect.anchorMin = new Vector2(0f, 1f);
            hpBarRect.anchorMax = new Vector2(0f, 1f);
            hpBarRect.pivot = new Vector2(0f, 1f);
            hpBarRect.anchoredPosition = new Vector2(56f, -12f);
            hpBarRect.sizeDelta = new Vector2(350f, 32f);

            Image hpBgImg = hpBarGO.GetComponent<Image>();
            hpBgImg.sprite = flatSprite;
            hpBgImg.color = HpBgColor;

            // Label "HP"
            CreateBadgeText(panelGO.transform, "HP", new Vector2(12f, -12f), new Vector2(40f, 32f), 16, new Color(1f, 0.35f, 0.35f));

            // Vạch vàng Ghost tụt theo
            GameObject ghostGO = new GameObject("GhostFill", typeof(RectTransform), typeof(Image));
            ghostGO.transform.SetParent(hpBarGO.transform, false);
            RectTransform ghostRect = ghostGO.GetComponent<RectTransform>();
            ghostRect.anchorMin = Vector2.zero;
            ghostRect.anchorMax = Vector2.one;
            ghostRect.sizeDelta = Vector2.zero;

            healthGhostFill = ghostGO.GetComponent<Image>();
            healthGhostFill.sprite = flatSprite;
            healthGhostFill.type = Image.Type.Filled;
            healthGhostFill.fillMethod = Image.FillMethod.Horizontal;
            healthGhostFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            healthGhostFill.fillAmount = 1f;
            healthGhostFill.color = HpGhostColor;

            // Vạch Máu đỏ tươi
            GameObject hpFillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            hpFillGO.transform.SetParent(hpBarGO.transform, false);
            RectTransform hpFillRect = hpFillGO.GetComponent<RectTransform>();
            hpFillRect.anchorMin = Vector2.zero;
            hpFillRect.anchorMax = Vector2.one;
            hpFillRect.sizeDelta = Vector2.zero;

            healthFill = hpFillGO.GetComponent<Image>();
            healthFill.sprite = flatSprite;
            healthFill.type = Image.Type.Filled;
            healthFill.fillMethod = Image.FillMethod.Horizontal;
            healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            healthFill.fillAmount = 1f;
            healthFill.color = HpFillColor;

            // Text số Máu
            healthText = CreateValueText(hpBarGO.transform, "100 / 100", 18);

            // 2. THANH MANA
            GameObject mpBarGO = new GameObject("ManaBar", typeof(RectTransform), typeof(Image));
            mpBarGO.transform.SetParent(panelGO.transform, false);
            RectTransform mpBarRect = mpBarGO.GetComponent<RectTransform>();
            mpBarRect.anchorMin = new Vector2(0f, 1f);
            mpBarRect.anchorMax = new Vector2(0f, 1f);
            mpBarRect.pivot = new Vector2(0f, 1f);
            mpBarRect.anchoredPosition = new Vector2(56f, -50f);
            mpBarRect.sizeDelta = new Vector2(350f, 26f);

            Image mpBgImg = mpBarGO.GetComponent<Image>();
            mpBgImg.sprite = flatSprite;
            mpBgImg.color = MpBgColor;

            // Label "MP"
            CreateBadgeText(panelGO.transform, "MP", new Vector2(12f, -50f), new Vector2(40f, 26f), 14, new Color(0.35f, 0.75f, 1f));

            // Fill Mana xanh
            GameObject mpFillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            mpFillGO.transform.SetParent(mpBarGO.transform, false);
            RectTransform mpFillRect = mpFillGO.GetComponent<RectTransform>();
            mpFillRect.anchorMin = Vector2.zero;
            mpFillRect.anchorMax = Vector2.one;
            mpFillRect.sizeDelta = Vector2.zero;

            manaFill = mpFillGO.GetComponent<Image>();
            manaFill.sprite = flatSprite;
            manaFill.type = Image.Type.Filled;
            manaFill.fillMethod = Image.FillMethod.Horizontal;
            manaFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            manaFill.fillAmount = 1f;
            manaFill.color = MpFillColor;

            // Text số Mana
            manaText = CreateValueText(mpBarGO.transform, "100 / 100", 15);

            // 3. VẠCH LEVEL & KINH NGHIỆM (EXP BAR)
            GameObject expBarGO = new GameObject("ExpBar", typeof(RectTransform), typeof(Image));
            expBarGO.transform.SetParent(panelGO.transform, false);
            RectTransform expBarRect = expBarGO.GetComponent<RectTransform>();
            expBarRect.anchorMin = new Vector2(0f, 1f);
            expBarRect.anchorMax = new Vector2(0f, 1f);
            expBarRect.pivot = new Vector2(0f, 1f);
            expBarRect.anchoredPosition = new Vector2(56f, -84f);
            expBarRect.sizeDelta = new Vector2(350f, 22f);

            Image expBgImg = expBarGO.GetComponent<Image>();
            expBgImg.sprite = flatSprite;
            expBgImg.color = ExpBgColor;

            // Huy hiệu Level bên trái (Ví dụ: Lv.1)
            GameObject levelBadgeGO = new GameObject("LevelBadge", typeof(RectTransform), typeof(Image));
            levelBadgeGO.transform.SetParent(panelGO.transform, false);
            RectTransform levelBadgeRect = levelBadgeGO.GetComponent<RectTransform>();
            levelBadgeRect.anchorMin = new Vector2(0f, 1f);
            levelBadgeRect.anchorMax = new Vector2(0f, 1f);
            levelBadgeRect.pivot = new Vector2(0f, 1f);
            levelBadgeRect.anchoredPosition = new Vector2(12f, -83f);
            levelBadgeRect.sizeDelta = new Vector2(40f, 24f);

            Image levelBadgeImg = levelBadgeGO.GetComponent<Image>();
            levelBadgeImg.sprite = flatSprite;
            levelBadgeImg.color = LevelBadgeBgColor;

            levelText = CreateBadgeText(levelBadgeGO.transform, "Lv.1", Vector2.zero, Vector2.zero, 13, GoldTextColor, true);

            // Fill EXP vàng cam sáng
            GameObject expFillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            expFillGO.transform.SetParent(expBarGO.transform, false);
            RectTransform expFillRect = expFillGO.GetComponent<RectTransform>();
            expFillRect.anchorMin = Vector2.zero;
            expFillRect.anchorMax = Vector2.one;
            expFillRect.sizeDelta = Vector2.zero;

            expFill = expFillGO.GetComponent<Image>();
            expFill.sprite = flatSprite;
            expFill.type = Image.Type.Filled;
            expFill.fillMethod = Image.FillMethod.Horizontal;
            expFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            expFill.fillAmount = 0f;
            expFill.color = ExpFillColor;

            // Text số EXP
            expText = CreateValueText(expBarGO.transform, "0 / 28,000", 13);
        }

        private static void CreateSkillPanel(Transform parent, Sprite flatSprite, out SkillSlotUI slotQ, out SkillSlotUI slotE, out SkillSlotUI slotR)
        {
            // Panel chứa 3 nút kỹ năng ở đáy chính giữa (Tỷ lệ 1080p chuẩn)
            GameObject panelGO = new GameObject("SkillPanel_BottomCenter", typeof(RectTransform), typeof(Image));
            panelGO.transform.SetParent(parent, false);

            RectTransform panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 24f);
            panelRect.sizeDelta = new Vector2(270f, 90f);

            Image panelImg = panelGO.GetComponent<Image>();
            panelImg.sprite = flatSprite;
            panelImg.color = BgDarkColor;

            // 3 ô skill (size 72x72) cách đều
            slotQ = CreateSingleSkillSlot(panelGO.transform, "SkillSlot_Q", "Q", -86f, flatSprite);
            slotE = CreateSingleSkillSlot(panelGO.transform, "SkillSlot_E", "E", 0f, flatSprite);
            slotR = CreateSingleSkillSlot(panelGO.transform, "SkillSlot_R", "R", 86f, flatSprite);
        }

        private static SkillSlotUI CreateSingleSkillSlot(Transform parent, string name, string keyName, float posX, Sprite flatSprite)
        {
            GameObject slotGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(SkillSlotUI));
            slotGO.transform.SetParent(parent, false);

            RectTransform slotRect = slotGO.GetComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = new Vector2(posX, 0f);
            slotRect.sizeDelta = new Vector2(72f, 72f);

            Image slotFrame = slotGO.GetComponent<Image>();
            slotFrame.sprite = flatSprite;
            slotFrame.color = FrameBorderColor;

            SkillSlotUI slotUI = slotGO.GetComponent<SkillSlotUI>();

            // 1. Icon kỹ năng
            GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(slotGO.transform, false);
            RectTransform iconRect = iconGO.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.sizeDelta = new Vector2(-4f, -4f);

            Image iconImg = iconGO.GetComponent<Image>();
            iconImg.sprite = flatSprite;
            iconImg.color = new Color(0.12f, 0.12f, 0.16f, 1f);

            // 2. Cooldown Overlay (Xoay tròn 360 - Cần flatSprite để Image.Type.Filled hoạt động)
            GameObject overlayGO = new GameObject("CooldownOverlay", typeof(RectTransform), typeof(Image));
            overlayGO.transform.SetParent(slotGO.transform, false);
            RectTransform overlayRect = overlayGO.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = new Vector2(-4f, -4f);

            Image overlayImg = overlayGO.GetComponent<Image>();
            overlayImg.sprite = flatSprite;
            overlayImg.type = Image.Type.Filled;
            overlayImg.fillMethod = Image.FillMethod.Radial360;
            overlayImg.fillOrigin = (int)Image.Origin360.Top;
            overlayImg.fillClockwise = false;
            overlayImg.fillAmount = 0f;
            overlayImg.color = CooldownOverlayColor;

            // 3. Cooldown Countdown Text
            GameObject cdTextGO = new GameObject("CooldownText", typeof(RectTransform), typeof(TextMeshProUGUI));
            cdTextGO.transform.SetParent(slotGO.transform, false);
            RectTransform cdTextRect = cdTextGO.GetComponent<RectTransform>();
            cdTextRect.anchorMin = Vector2.zero;
            cdTextRect.anchorMax = Vector2.one;
            cdTextRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI cdTmp = cdTextGO.GetComponent<TextMeshProUGUI>();
            cdTmp.text = "";
            cdTmp.fontSize = 26;
            cdTmp.fontStyle = FontStyles.Bold;
            cdTmp.alignment = TextAlignmentOptions.Center;
            cdTmp.color = GoldTextColor;
            cdTmp.outlineColor = new Color32(10, 10, 15, 255);
            cdTmp.outlineWidth = 0.25f;
            cdTextGO.SetActive(false);

            // 4. Key Badge (Chữ Q, E, R)
            GameObject badgeGO = new GameObject("KeyBadge", typeof(RectTransform), typeof(Image));
            badgeGO.transform.SetParent(slotGO.transform, false);
            RectTransform badgeRect = badgeGO.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(1f, 0f);
            badgeRect.anchorMax = new Vector2(1f, 0f);
            badgeRect.pivot = new Vector2(1f, 0f);
            badgeRect.anchoredPosition = new Vector2(-3f, 3f);
            badgeRect.sizeDelta = new Vector2(28f, 26f);

            Image badgeImg = badgeGO.GetComponent<Image>();
            badgeImg.sprite = flatSprite;
            badgeImg.color = new Color(0.04f, 0.04f, 0.06f, 0.95f);

            GameObject badgeTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            badgeTextGO.transform.SetParent(badgeGO.transform, false);
            RectTransform badgeTextRect = badgeTextGO.GetComponent<RectTransform>();
            badgeTextRect.anchorMin = Vector2.zero;
            badgeTextRect.anchorMax = Vector2.one;
            badgeTextRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI keyTmp = badgeTextGO.GetComponent<TextMeshProUGUI>();
            keyTmp.text = keyName;
            keyTmp.fontSize = 14;
            keyTmp.fontStyle = FontStyles.Bold;
            keyTmp.alignment = TextAlignmentOptions.Center;
            keyTmp.color = GoldTextColor;
            keyTmp.outlineColor = new Color32(10, 10, 15, 255);
            keyTmp.outlineWidth = 0.2f;

            // 5. Nối dây references cho SkillSlotUI
            SerializedObject slotSO = new SerializedObject(slotUI);
            slotSO.FindProperty("iconImage").objectReferenceValue = iconImg;
            slotSO.FindProperty("cooldownOverlay").objectReferenceValue = overlayImg;
            slotSO.FindProperty("cooldownText").objectReferenceValue = cdTmp;
            slotSO.FindProperty("keyBadgeText").objectReferenceValue = keyTmp;
            slotSO.ApplyModifiedProperties();

            return slotUI;
        }

        private static TextMeshProUGUI CreateValueText(Transform parent, string text, float fontSize)
        {
            GameObject textGO = new GameObject("ValueText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(parent, false);

            RectTransform rect = textGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.outlineColor = new Color32(10, 10, 15, 255);
            tmp.outlineWidth = 0.25f;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        private static TextMeshProUGUI CreateBadgeText(Transform parent, string text, Vector2 anchoredPos, Vector2 size, float fontSize, Color color, bool isStretch = false)
        {
            GameObject labelGO = new GameObject("Label_" + text, typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(parent, false);

            RectTransform rect = labelGO.GetComponent<RectTransform>();
            if (isStretch)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
            }
            else
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = anchoredPos;
                rect.sizeDelta = size;
            }

            TextMeshProUGUI tmp = labelGO.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.outlineColor = new Color32(10, 10, 15, 255);
            tmp.outlineWidth = 0.25f;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }
    }
}

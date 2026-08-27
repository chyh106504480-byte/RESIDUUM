using System;
using UnityEngine;

#if UNITY_EDITOR
namespace Residuum.UI.Editor
{
    /// <summary>
    /// 在当前场景中搭建 HUD 与结算界面，并完成 UI 控制器的界面引用接线。
    /// </summary>
    public static class UISetupBuilder
    {
        private const string MenuPath = "Residuum/搭建 HUD 与结算界面";
        private const string UndoLabel = "搭建 HUD 与结算界面";
        private const string EventSystemName = "EventSystem";
        private const string HudCanvasName = "HUD Canvas";
        private const string ResultCanvasName = "Result Canvas";
        private const string CrosshairName = "Crosshair";
        private const string PromptLabelName = "PromptLabel";
        private const string SanityBarName = "SanityBar";
        private const string EvidenceListName = "EvidenceList";
        private const string ItemLabelName = "ItemLabel";
        private const string BatteryBarName = "BatteryBar";
        private const string TemperatureLabelName = "TemperatureLabel";
        private const string HuntVignetteName = "HuntVignette";
        private const string FillName = "Fill";
        private const string PanelName = "Panel";
        private const string GradeName = "Grade";
        private const string TitleName = "Title";
        private const string DetailName = "Detail";
        private const string RestartButtonName = "RestartButton";
        private const string RestartButtonTextName = "Text";

        private const int HudSortingOrder = 0;
        private const int ResultSortingOrder = 100;
        private const int EvidenceLabelCount = 3;
        private const int ReferenceResolutionWidth = 1920;
        private const int ReferenceResolutionHeight = 1080;
        private const int CrosshairSize = 8;
        private const int PromptFontSize = 24;
        private const int EvidenceFontSize = 20;
        private const int ItemFontSize = 22;
        private const int TemperatureFontSize = 20;
        private const int GradeFontSize = 140;
        private const int ResultTitleFontSize = 36;
        private const int ResultDetailFontSize = 24;
        private const int RestartButtonFontSize = 24;
        private const int EvidenceLineHeight = 26;

        private const float CanvasMatchWidthOrHeight = 0.5f;
        private const float PanelAlpha = 0.85f;
        private const float PromptOffsetY = -60f;
        private const float HudHorizontalMargin = 40f;
        private const float HudVerticalMargin = 40f;
        private const float SanityBarWidth = 260f;
        private const float SanityBarHeight = 18f;
        private const float EvidenceListWidth = 360f;
        private const float EvidenceLineHeightFloat = 24f;
        private const float ItemLabelWidth = 420f;
        private const float ItemLabelHeight = 32f;
        private const float BatteryBarWidth = 160f;
        private const float BatteryBarHeight = 10f;
        private const float BatteryBarOffsetY = 82f;
        private const float TemperatureOffsetY = 80f;
        private const float TemperatureLabelHeight = 30f;
        private const float GradeOffsetY = 170f;
        private const float GradeHeight = 160f;
        private const float ResultTitleOffsetY = 55f;
        private const float ResultTitleHeight = 52f;
        private const float ResultDetailOffsetY = -50f;
        private const float ResultDetailHeight = 130f;
        private const float RestartButtonOffsetY = -230f;
        private const float RestartButtonWidth = 240f;
        private const float RestartButtonHeight = 56f;
        private const float ResultContentWidth = 900f;

        private static readonly Color BackgroundColor = new Color(0.04f, 0.04f, 0.04f, 0.9f);
        private static readonly Color BarFillColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        private static readonly Color ResultPanelColor = new Color(0f, 0f, 0f, PanelAlpha);
        private static readonly Color HuntVignetteColor = new Color(1f, 0f, 0f, 0f);
        private static readonly Color RestartButtonColor = new Color(0.25f, 0.25f, 0.25f, 1f);

        [UnityEditor.MenuItem(MenuPath)]
        private static void BuildUI()
        {
            int undoGroup = UnityEditor.Undo.GetCurrentGroup();
            UnityEditor.Undo.SetCurrentGroupName(UndoLabel);

            try
            {
                EnsureEventSystem();

                TMPro.TMP_FontAsset defaultFont = TMPro.TMP_Settings.defaultFontAsset;
                if (defaultFont == null)
                {
                    Debug.LogWarning("未找到 TMP 默认字体。需要先导入 TMP Essentials；UI 层级仍已创建。");
                }

                GameObject hudCanvas = GetOrCreateRootCanvas(HudCanvasName);
                ConfigureCanvas(hudCanvas, HudSortingOrder);
                HUDController hudController = EnsureComponent<HUDController>(hudCanvas);
                HudReferences hudReferences = BuildHudHierarchy(hudCanvas.transform, defaultFont);
                WireHudController(hudController, hudReferences);

                GameObject resultCanvas = GetOrCreateRootCanvas(ResultCanvasName);
                ConfigureCanvas(resultCanvas, ResultSortingOrder);
                ResultScreenUI resultScreenUI = EnsureComponent<ResultScreenUI>(resultCanvas);
                ResultReferences resultReferences = BuildResultHierarchy(resultCanvas.transform, defaultFont);
                WireResultScreenUI(resultScreenUI, resultReferences);

                UnityEditor.Undo.CollapseUndoOperations(undoGroup);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                UnityEditor.Selection.activeGameObject = hudCanvas;
                Debug.Log(
                    "HUD Canvas 与 Result Canvas 已搭建并完成界面引用接线。" +
                    "还需要手动连两项：将玩家物体上的 PlayerController 拖到 " +
                    "Result Canvas/ResultScreenUI 的 _playerControllerBehaviour；" +
                    "将 GameManager.StartRound 拖到 Result Canvas/ResultScreenUI 的 onRestartRequested。",
                    hudCanvas);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                UnityEditor.Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogError("HUD 与结算界面搭建失败，已撤销本次创建和接线。");
            }
        }

        private static void EnsureEventSystem()
        {
            UnityEngine.EventSystems.EventSystem existingEventSystem =
                FindComponentInActiveScene<UnityEngine.EventSystems.EventSystem>();
            if (existingEventSystem != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject(EventSystemName);
            UnityEditor.Undo.RegisterCreatedObjectUndo(eventSystemObject, UndoLabel);

            UnityEngine.EventSystems.EventSystem eventSystem =
                UnityEditor.Undo.AddComponent<UnityEngine.EventSystems.EventSystem>(eventSystemObject);
            UnityEngine.InputSystem.UI.InputSystemUIInputModule inputSystemModule =
                UnityEditor.Undo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(
                    eventSystemObject);
            if (eventSystem == null || inputSystemModule == null)
            {
                throw new InvalidOperationException("无法创建 EventSystem 或 InputSystemUIInputModule。");
            }
        }

        private static T FindComponentInActiveScene<T>() where T : Component
        {
            GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                T component = roots[rootIndex].GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private static GameObject GetOrCreateRootCanvas(string canvasName)
        {
            GameObject existingCanvas = FindRootObject(canvasName);
            if (existingCanvas != null)
            {
                RequireRectTransform(existingCanvas, canvasName);
                return existingCanvas;
            }

            GameObject canvasObject = new GameObject(canvasName, typeof(RectTransform));
            UnityEditor.Undo.RegisterCreatedObjectUndo(canvasObject, UndoLabel);
            return canvasObject;
        }

        private static GameObject FindRootObject(string objectName)
        {
            GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                if (roots[rootIndex].name == objectName)
                {
                    return roots[rootIndex];
                }
            }

            return null;
        }

        private static void ConfigureCanvas(GameObject canvasObject, int sortingOrder)
        {
            RequireRectTransform(canvasObject, canvasObject.name);
            Canvas canvas = EnsureComponent<Canvas>(canvasObject);
            UnityEngine.UI.CanvasScaler canvasScaler =
                EnsureComponent<UnityEngine.UI.CanvasScaler>(canvasObject);
            EnsureComponent<UnityEngine.UI.GraphicRaycaster>(canvasObject);

            UnityEditor.Undo.RecordObject(canvas, UndoLabel);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            UnityEditor.Undo.RecordObject(canvasScaler, UndoLabel);
            canvasScaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(
                ReferenceResolutionWidth,
                ReferenceResolutionHeight);
            canvasScaler.matchWidthOrHeight = CanvasMatchWidthOrHeight;
        }

        private static HudReferences BuildHudHierarchy(
            Transform hudCanvasTransform,
            TMPro.TMP_FontAsset defaultFont)
        {
            UnityEngine.UI.Image crosshair = EnsureImage(hudCanvasTransform, CrosshairName);
            ConfigureRect(
                crosshair.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(CrosshairSize, CrosshairSize));
            ConfigureImage(crosshair, Color.white, false);

            TMPro.TextMeshProUGUI promptLabel = EnsureText(hudCanvasTransform, PromptLabelName);
            ConfigureRect(
                promptLabel.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, PromptOffsetY),
                new Vector2(ResultContentWidth, ResultTitleHeight));
            ConfigureText(
                promptLabel,
                defaultFont,
                string.Empty,
                PromptFontSize,
                TMPro.TextAlignmentOptions.Center,
                false,
                false);

            UnityEngine.UI.Image sanityBackground = EnsureImage(hudCanvasTransform, SanityBarName);
            ConfigureRect(
                sanityBackground.rectTransform,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                new Vector2(HudHorizontalMargin, HudVerticalMargin),
                new Vector2(SanityBarWidth, SanityBarHeight));
            ConfigureImage(sanityBackground, BackgroundColor, false);

            UnityEngine.UI.Image sanityFill = EnsureImage(sanityBackground.transform, FillName);
            ConfigureStretchRect(sanityFill.rectTransform);
            ConfigureFilledImage(sanityFill, BarFillColor, false);

            GameObject evidenceList = GetOrCreateUiChild(hudCanvasTransform, EvidenceListName);
            RectTransform evidenceListRect = RequireRectTransform(evidenceList, EvidenceListName);
            ConfigureRect(
                evidenceListRect,
                Vector2.one,
                Vector2.one,
                Vector2.one,
                new Vector2(-HudHorizontalMargin, -HudVerticalMargin),
                new Vector2(EvidenceListWidth, EvidenceLabelCount * EvidenceLineHeight));

            TMPro.TextMeshProUGUI[] evidenceLabels = new TMPro.TextMeshProUGUI[EvidenceLabelCount];
            for (int labelIndex = 0; labelIndex < EvidenceLabelCount; labelIndex++)
            {
                TMPro.TextMeshProUGUI evidenceLabel = EnsureText(
                    evidenceList.transform,
                    $"Evidence{labelIndex}");
                ConfigureRect(
                    evidenceLabel.rectTransform,
                    Vector2.one,
                    Vector2.one,
                    Vector2.one,
                    new Vector2(0f, -labelIndex * EvidenceLineHeight),
                    new Vector2(EvidenceListWidth, EvidenceLineHeightFloat));
                ConfigureText(
                    evidenceLabel,
                    defaultFont,
                    string.Empty,
                    EvidenceFontSize,
                    TMPro.TextAlignmentOptions.Right,
                    false,
                    false);
                evidenceLabels[labelIndex] = evidenceLabel;
            }

            TMPro.TextMeshProUGUI itemLabel = EnsureText(hudCanvasTransform, ItemLabelName);
            ConfigureRect(
                itemLabel.rectTransform,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-HudHorizontalMargin, HudVerticalMargin),
                new Vector2(ItemLabelWidth, ItemLabelHeight));
            ConfigureText(
                itemLabel,
                defaultFont,
                string.Empty,
                ItemFontSize,
                TMPro.TextAlignmentOptions.Right,
                false,
                false);

            UnityEngine.UI.Image batteryBackground = EnsureImage(hudCanvasTransform, BatteryBarName);
            ConfigureRect(
                batteryBackground.rectTransform,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-HudHorizontalMargin, BatteryBarOffsetY),
                new Vector2(BatteryBarWidth, BatteryBarHeight));
            ConfigureImage(batteryBackground, BackgroundColor, false);

            UnityEngine.UI.Image batteryFill = EnsureImage(batteryBackground.transform, FillName);
            ConfigureStretchRect(batteryFill.rectTransform);
            ConfigureFilledImage(batteryFill, BarFillColor, false);

            TMPro.TextMeshProUGUI temperatureLabel = EnsureText(hudCanvasTransform, TemperatureLabelName);
            ConfigureRect(
                temperatureLabel.rectTransform,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                new Vector2(HudHorizontalMargin, TemperatureOffsetY),
                new Vector2(SanityBarWidth, TemperatureLabelHeight));
            ConfigureText(
                temperatureLabel,
                defaultFont,
                string.Empty,
                TemperatureFontSize,
                TMPro.TextAlignmentOptions.Left,
                false,
                false);

            UnityEngine.UI.Image huntVignette = EnsureImage(hudCanvasTransform, HuntVignetteName);
            ConfigureStretchRect(huntVignette.rectTransform);
            ConfigureImage(huntVignette, HuntVignetteColor, false);
            UnityEditor.Undo.RecordObject(huntVignette.transform, UndoLabel);
            huntVignette.transform.SetAsLastSibling();

            return new HudReferences(
                crosshair,
                promptLabel,
                sanityFill,
                evidenceLabels,
                itemLabel,
                batteryFill,
                temperatureLabel,
                huntVignette);
        }

        private static ResultReferences BuildResultHierarchy(
            Transform resultCanvasTransform,
            TMPro.TMP_FontAsset defaultFont)
        {
            UnityEngine.UI.Image panel = EnsureImage(resultCanvasTransform, PanelName);
            ConfigureStretchRect(panel.rectTransform);
            ConfigureImage(panel, ResultPanelColor, true);

            TMPro.TextMeshProUGUI gradeLabel = EnsureText(panel.transform, GradeName);
            ConfigureRect(
                gradeLabel.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, GradeOffsetY),
                new Vector2(ResultContentWidth, GradeHeight));
            ConfigureText(
                gradeLabel,
                defaultFont,
                string.Empty,
                GradeFontSize,
                TMPro.TextAlignmentOptions.Center,
                false,
                false);

            TMPro.TextMeshProUGUI titleLabel = EnsureText(panel.transform, TitleName);
            ConfigureRect(
                titleLabel.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, ResultTitleOffsetY),
                new Vector2(ResultContentWidth, ResultTitleHeight));
            ConfigureText(
                titleLabel,
                defaultFont,
                string.Empty,
                ResultTitleFontSize,
                TMPro.TextAlignmentOptions.Center,
                false,
                false);

            TMPro.TextMeshProUGUI detailLabel = EnsureText(panel.transform, DetailName);
            ConfigureRect(
                detailLabel.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, ResultDetailOffsetY),
                new Vector2(ResultContentWidth, ResultDetailHeight));
            ConfigureText(
                detailLabel,
                defaultFont,
                string.Empty,
                ResultDetailFontSize,
                TMPro.TextAlignmentOptions.Center,
                true,
                false);

            UnityEngine.UI.Image restartButtonImage = EnsureImage(panel.transform, RestartButtonName);
            ConfigureRect(
                restartButtonImage.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, RestartButtonOffsetY),
                new Vector2(RestartButtonWidth, RestartButtonHeight));
            ConfigureImage(restartButtonImage, RestartButtonColor, true);

            UnityEngine.UI.Button restartButton =
                EnsureComponent<UnityEngine.UI.Button>(restartButtonImage.gameObject);
            UnityEditor.Undo.RecordObject(restartButton, UndoLabel);
            restartButton.targetGraphic = restartButtonImage;

            TMPro.TextMeshProUGUI restartButtonText = EnsureText(
                restartButtonImage.transform,
                RestartButtonTextName);
            ConfigureStretchRect(restartButtonText.rectTransform);
            ConfigureText(
                restartButtonText,
                defaultFont,
                "再来一局",
                RestartButtonFontSize,
                TMPro.TextAlignmentOptions.Center,
                false,
                false);

            UnityEditor.Undo.RecordObject(panel.gameObject, UndoLabel);
            panel.gameObject.SetActive(false);

            return new ResultReferences(
                panel.gameObject,
                gradeLabel,
                titleLabel,
                detailLabel,
                restartButton);
        }

        private static void WireHudController(HUDController hudController, HudReferences references)
        {
            UnityEditor.Undo.RecordObject(hudController, UndoLabel);
            UnityEditor.SerializedObject serializedHudController =
                new UnityEditor.SerializedObject(hudController);
            serializedHudController.Update();

            GetRequiredProperty(serializedHudController, "_crosshair").objectReferenceValue = references.Crosshair;
            GetRequiredProperty(serializedHudController, "_promptLabel").objectReferenceValue = references.PromptLabel;
            GetRequiredProperty(serializedHudController, "_sanityFill").objectReferenceValue = references.SanityFill;
            GetRequiredProperty(serializedHudController, "_itemLabel").objectReferenceValue = references.ItemLabel;
            GetRequiredProperty(serializedHudController, "_batteryFill").objectReferenceValue = references.BatteryFill;
            GetRequiredProperty(serializedHudController, "_temperatureLabel").objectReferenceValue = references.TemperatureLabel;
            GetRequiredProperty(serializedHudController, "_huntVignette").objectReferenceValue = references.HuntVignette;

            UnityEditor.SerializedProperty evidenceLabelsProperty =
                GetRequiredProperty(serializedHudController, "_evidenceLabels");
            if (!evidenceLabelsProperty.isArray)
            {
                Debug.LogError("HUDController 的 _evidenceLabels 不是数组，无法完成接线。", hudController);
                throw new InvalidOperationException("HUDController 的 _evidenceLabels 不是数组。");
            }

            evidenceLabelsProperty.arraySize = EvidenceLabelCount;
            for (int labelIndex = 0; labelIndex < EvidenceLabelCount; labelIndex++)
            {
                evidenceLabelsProperty.GetArrayElementAtIndex(labelIndex).objectReferenceValue =
                    references.EvidenceLabels[labelIndex];
            }

            serializedHudController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireResultScreenUI(
            ResultScreenUI resultScreenUI,
            ResultReferences references)
        {
            UnityEditor.Undo.RecordObject(resultScreenUI, UndoLabel);
            UnityEditor.SerializedObject serializedResultScreenUI =
                new UnityEditor.SerializedObject(resultScreenUI);
            serializedResultScreenUI.Update();

            GetRequiredProperty(serializedResultScreenUI, "_panelRoot").objectReferenceValue =
                references.PanelRoot;
            GetRequiredProperty(serializedResultScreenUI, "_gradeLabel").objectReferenceValue =
                references.GradeLabel;
            GetRequiredProperty(serializedResultScreenUI, "_titleLabel").objectReferenceValue =
                references.TitleLabel;
            GetRequiredProperty(serializedResultScreenUI, "_detailLabel").objectReferenceValue =
                references.DetailLabel;
            GetRequiredProperty(serializedResultScreenUI, "_restartButton").objectReferenceValue =
                references.RestartButton;

            serializedResultScreenUI.ApplyModifiedPropertiesWithoutUndo();
        }

        private static UnityEditor.SerializedProperty GetRequiredProperty(
            UnityEditor.SerializedObject serializedObject,
            string propertyName)
        {
            UnityEditor.SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError(
                    $"{serializedObject.targetObject.GetType().Name} 缺少序列化字段 {propertyName}，已中止 UI 接线。",
                    serializedObject.targetObject);
                throw new InvalidOperationException($"找不到序列化字段：{propertyName}");
            }

            return property;
        }

        private static GameObject GetOrCreateUiChild(Transform parent, string childName)
        {
            Transform existingChild = FindDirectChild(parent, childName);
            if (existingChild != null)
            {
                RequireRectTransform(existingChild.gameObject, childName);
                return existingChild.gameObject;
            }

            GameObject child = new GameObject(childName, typeof(RectTransform));
            UnityEditor.Undo.RegisterCreatedObjectUndo(child, UndoLabel);
            UnityEditor.Undo.SetTransformParent(child.transform, parent, UndoLabel);
            return child;
        }

        private static Transform FindDirectChild(Transform parent, string childName)
        {
            for (int childIndex = 0; childIndex < parent.childCount; childIndex++)
            {
                Transform child = parent.GetChild(childIndex);
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private static UnityEngine.UI.Image EnsureImage(Transform parent, string objectName)
        {
            GameObject imageObject = GetOrCreateUiChild(parent, objectName);
            return EnsureComponent<UnityEngine.UI.Image>(imageObject);
        }

        private static TMPro.TextMeshProUGUI EnsureText(Transform parent, string objectName)
        {
            GameObject textObject = GetOrCreateUiChild(parent, objectName);
            return EnsureComponent<TMPro.TextMeshProUGUI>(textObject);
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            component = UnityEditor.Undo.AddComponent<T>(gameObject);
            if (component == null)
            {
                throw new InvalidOperationException(
                    $"无法为 {gameObject.name} 添加组件 {typeof(T).Name}。");
            }

            return component;
        }

        private static RectTransform RequireRectTransform(GameObject gameObject, string objectName)
        {
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                Debug.LogError($"{objectName} 必须使用 RectTransform，已中止 UI 搭建。", gameObject);
                throw new InvalidOperationException($"{objectName} 缺少 RectTransform。");
            }

            return rectTransform;
        }

        private static void ConfigureRect(
            RectTransform rectTransform,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            UnityEditor.Undo.RecordObject(rectTransform, UndoLabel);
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
        }

        private static void ConfigureStretchRect(RectTransform rectTransform)
        {
            UnityEditor.Undo.RecordObject(rectTransform, UndoLabel);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void ConfigureImage(
            UnityEngine.UI.Image image,
            Color color,
            bool raycastTarget)
        {
            UnityEditor.Undo.RecordObject(image, UndoLabel);
            image.color = color;
            image.raycastTarget = raycastTarget;
        }

        private static void ConfigureFilledImage(
            UnityEngine.UI.Image image,
            Color color,
            bool raycastTarget)
        {
            ConfigureImage(image, color, raycastTarget);
            UnityEditor.Undo.RecordObject(image, UndoLabel);
            image.type = UnityEngine.UI.Image.Type.Filled;
            image.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            image.fillAmount = 1f;
        }

        private static void ConfigureText(
            TMPro.TextMeshProUGUI text,
            TMPro.TMP_FontAsset defaultFont,
            string value,
            float fontSize,
            TMPro.TextAlignmentOptions alignment,
            bool enableWordWrapping,
            bool raycastTarget)
        {
            UnityEditor.Undo.RecordObject(text, UndoLabel);
            if (defaultFont != null)
            {
                text.font = defaultFont;
            }

            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.enableWordWrapping = enableWordWrapping;
            text.raycastTarget = raycastTarget;
            text.color = Color.white;
        }

        private readonly struct HudReferences
        {
            public HudReferences(
                UnityEngine.UI.Image crosshair,
                TMPro.TextMeshProUGUI promptLabel,
                UnityEngine.UI.Image sanityFill,
                TMPro.TextMeshProUGUI[] evidenceLabels,
                TMPro.TextMeshProUGUI itemLabel,
                UnityEngine.UI.Image batteryFill,
                TMPro.TextMeshProUGUI temperatureLabel,
                UnityEngine.UI.Image huntVignette)
            {
                Crosshair = crosshair;
                PromptLabel = promptLabel;
                SanityFill = sanityFill;
                EvidenceLabels = evidenceLabels;
                ItemLabel = itemLabel;
                BatteryFill = batteryFill;
                TemperatureLabel = temperatureLabel;
                HuntVignette = huntVignette;
            }

            public UnityEngine.UI.Image Crosshair { get; }
            public TMPro.TextMeshProUGUI PromptLabel { get; }
            public UnityEngine.UI.Image SanityFill { get; }
            public TMPro.TextMeshProUGUI[] EvidenceLabels { get; }
            public TMPro.TextMeshProUGUI ItemLabel { get; }
            public UnityEngine.UI.Image BatteryFill { get; }
            public TMPro.TextMeshProUGUI TemperatureLabel { get; }
            public UnityEngine.UI.Image HuntVignette { get; }
        }

        private readonly struct ResultReferences
        {
            public ResultReferences(
                GameObject panelRoot,
                TMPro.TextMeshProUGUI gradeLabel,
                TMPro.TextMeshProUGUI titleLabel,
                TMPro.TextMeshProUGUI detailLabel,
                UnityEngine.UI.Button restartButton)
            {
                PanelRoot = panelRoot;
                GradeLabel = gradeLabel;
                TitleLabel = titleLabel;
                DetailLabel = detailLabel;
                RestartButton = restartButton;
            }

            public GameObject PanelRoot { get; }
            public TMPro.TextMeshProUGUI GradeLabel { get; }
            public TMPro.TextMeshProUGUI TitleLabel { get; }
            public TMPro.TextMeshProUGUI DetailLabel { get; }
            public UnityEngine.UI.Button RestartButton { get; }
        }
    }
}
#endif

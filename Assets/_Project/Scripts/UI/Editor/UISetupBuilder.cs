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
        private const string JournalMenuPath = "Residuum/搭建判定笔记本";
        private const string JournalUndoLabel = "搭建判定笔记本";
        private const string EventSystemName = "EventSystem";
        private const string HudCanvasName = "HUD Canvas";
        private const string ResultCanvasName = "Result Canvas";
        private const string ConfirmCanvasName = "Confirm Canvas";
        private const string JournalCanvasName = "Journal Canvas";
        private const string MainMenuCanvasName = "Main Menu Canvas";
        private const string EvidenceManagerName = "EvidenceManager";
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
        private const string BackgroundName = "Background";
        private const string SubtitleName = "Subtitle";
        private const string StartButtonName = "StartButton";
        private const string QuitButtonName = "QuitButton";
        private const string BoxName = "Box";
        private const string MessageName = "Message";
        private const string ConfirmButtonName = "ConfirmButton";
        private const string CancelButtonName = "CancelButton";
        private const string RestartButtonTextName = "Text";
        private const string EvidenceName = "Evidence";
        private const string DeductionTableName = "DeductionTable";
        private const string GuessButtonsName = "GuessButtons";
        private const string LabelName = "Label";
        private const string RuleButtonName = "RuleButton";
        private const string ButtonTextName = "Text";
        private const string GhostNamePrefix = "GhostName";
        private const string GhostEvidencePrefix = "GhostEvidence";
        private const string HeaderPrefix = "Header";
        private const string RowPrefix = "Row";
        private const string GuessPrefix = "Guess";
        private const string GameManagerTypeName = "Residuum.Core.GameManager";
        private const string PlayerControllerTypeName = "Residuum.Player.PlayerController";
        private const string EvidenceManagerTypeName = "Residuum.Evidence.EvidenceManager";

        private const int HudSortingOrder = 0;
        private const int ResultSortingOrder = 100;
        private const int ConfirmSortingOrder = 80;
        private const int JournalSortingOrder = 50;
        private const int MainMenuSortingOrder = 200;
        private const int EvidenceLabelCount = 3;
        private const int GhostCount = 3;
        private const int EvidenceColumnsPerGhost = 3;
        private const int TableColumnCount = 4;
        private const int GhostEvidenceCellCount = GhostCount * EvidenceColumnsPerGhost;
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
        private const int ConfirmMessageFontSize = 26;
        private const int ConfirmButtonFontSize = 24;
        private const int JournalEvidenceFontSize = 26;
        private const int JournalTableFontSize = 22;
        private const int JournalButtonFontSize = 22;
        private const int MainMenuTitleFontSize = 96;
        private const int MainMenuSubtitleFontSize = 28;
        private const int MainMenuButtonFontSize = 28;
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
        private const float ConfirmBoxWidth = 560f;
        private const float ConfirmBoxHeight = 220f;
        private const float ConfirmMessageOffsetY = 42f;
        private const float ConfirmMessageWidth = 500f;
        private const float ConfirmMessageHeight = 86f;
        private const float ConfirmButtonOffsetX = 125f;
        private const float ConfirmButtonOffsetY = -66f;
        private const float ConfirmButtonWidth = 200f;
        private const float ConfirmButtonHeight = 52f;
        private const float JournalLeftMargin = 80f;
        private const float JournalTopMargin = 160f;
        private const float JournalEvidenceRowHeight = 44f;
        private const float JournalEvidenceRowSpacing = 8f;
        private const float JournalEvidenceLabelWidth = 300f;
        private const float JournalRuleButtonWidth = 96f;
        private const float JournalRuleButtonHeight = 36f;
        private const float JournalEvidenceRowWidth = 420f;
        private const float JournalTableRightMargin = 80f;
        private const float JournalTableCellWidth = 150f;
        private const float JournalTableCellHeight = 44f;
        private const float JournalTableSpacing = 6f;
        private const float JournalGuessButtonWidth = 200f;
        private const float JournalGuessButtonHeight = 52f;
        private const float JournalGuessButtonSpacing = 24f;
        private const float JournalGuessButtonBottomMargin = 80f;
        private const float MainMenuTitleOffsetY = 180f;
        private const float MainMenuTitleWidth = 1200f;
        private const float MainMenuTitleHeight = 120f;
        private const float MainMenuSubtitleOffsetY = 90f;
        private const float MainMenuSubtitleWidth = 1000f;
        private const float MainMenuSubtitleHeight = 48f;
        private const float MainMenuStartButtonOffsetY = -140f;
        private const float MainMenuButtonWidth = 280f;
        private const float MainMenuButtonHeight = 64f;
        private const float MainMenuButtonSpacing = 20f;

        private static readonly Color BackgroundColor = new Color(0.04f, 0.04f, 0.04f, 0.9f);
        private static readonly Color BarFillColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        private static readonly Color ResultPanelColor = new Color(0f, 0f, 0f, PanelAlpha);
        private static readonly Color HuntVignetteColor = new Color(1f, 0f, 0f, 0f);
        private static readonly Color RestartButtonColor = new Color(0.25f, 0.25f, 0.25f, 1f);
        private static readonly Color ConfirmBoxColor = new Color(0.12f, 0.12f, 0.12f, 1f);
        private static readonly Color ConfirmButtonColor = new Color(0.25f, 0.25f, 0.25f, 1f);
        private static readonly Color JournalPanelColor = new Color(0f, 0f, 0f, PanelAlpha);
        private static readonly Color JournalButtonColor = new Color(0.25f, 0.25f, 0.25f, 1f);
        private static readonly Color MainMenuBackgroundColor = new Color(0.06f, 0.06f, 0.07f, 1f);
        private static readonly Color MainMenuButtonColor = new Color(0.25f, 0.25f, 0.25f, 1f);

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

                GameObject confirmCanvas = GetOrCreateRootCanvas(ConfirmCanvasName);
                ConfigureCanvas(confirmCanvas, ConfirmSortingOrder);
                EvacuateConfirmUI evacuateConfirmUI = EnsureComponent<EvacuateConfirmUI>(confirmCanvas);
                ConfirmReferences confirmReferences = BuildConfirmHierarchy(
                    confirmCanvas.transform,
                    defaultFont);
                WireEvacuateConfirmUI(evacuateConfirmUI, confirmReferences);

                Component gameManager = FindComponentInActiveSceneByTypeName(GameManagerTypeName);
                if (gameManager == null)
                {
                    Debug.LogWarning(
                        "没找到 GameManager：EvacuateConfirmUI.onEvacuateConfirmed → " +
                        "GameManager.RequestEvacuate 未连接。",
                        confirmCanvas);
                }
                else
                {
                    WireEvacuateConfirmedEvent(evacuateConfirmUI, gameManager);
                }

                GameObject mainMenuCanvas = GetOrCreateRootCanvas(MainMenuCanvasName);
                ConfigureCanvas(mainMenuCanvas, MainMenuSortingOrder);
                MainMenuUI mainMenuUI = EnsureComponent<MainMenuUI>(mainMenuCanvas);
                MainMenuReferences mainMenuReferences = BuildMainMenuHierarchy(
                    mainMenuCanvas.transform,
                    defaultFont);

                Component playerController = FindComponentInActiveSceneByTypeName(PlayerControllerTypeName);
                WireMainMenuUI(mainMenuUI, mainMenuReferences, playerController);

                if (gameManager == null)
                {
                    WireMainMenuStartEvent(mainMenuUI, null);
                    Debug.LogWarning(
                        "没找到 GameManager：MainMenuUI.onStartRequested → " +
                        "GameManager.StartRound 未连接。",
                        mainMenuCanvas);
                }
                else
                {
                    WireMainMenuStartEvent(mainMenuUI, gameManager);
                }

                if (playerController == null)
                {
                    Debug.LogWarning(
                        "没找到 PlayerController：MainMenuUI._playerControllerBehaviour 未连接。",
                        mainMenuCanvas);
                }

                UnityEditor.Undo.CollapseUndoOperations(undoGroup);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                UnityEditor.Selection.activeGameObject = hudCanvas;
                Debug.Log(
                    "HUD Canvas、Result Canvas、Confirm Canvas 与 Main Menu Canvas 已搭建并完成界面内部引用接线。" +
                    "要让主菜单真正生效，请取消勾选 GameManager 的 Auto Start On Play；" +
                    "搭建工具不会自动修改该场景数据。" +
                    "请继续运行 Residuum/搭建判定笔记本，它会自动补齐 PlayerController、" +
                    "GameManager 与结算界面的跨物体接线。",
                    hudCanvas);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                UnityEditor.Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogError("HUD 与结算界面搭建失败，已撤销本次创建和接线。");
            }
        }

        [UnityEditor.MenuItem(JournalMenuPath)]
        private static void BuildJournal()
        {
            int undoGroup = UnityEditor.Undo.GetCurrentGroup();
            UnityEditor.Undo.SetCurrentGroupName(JournalUndoLabel);

            try
            {
                UnityEngine.Object[] ghostDefinitions = FindSortedGhostDefinitions();
                if (ghostDefinitions == null)
                {
                    UnityEditor.Undo.RevertAllDownToGroup(undoGroup);
                    return;
                }

                EnsureEventSystem();

                TMPro.TMP_FontAsset defaultFont = TMPro.TMP_Settings.defaultFontAsset;
                if (defaultFont == null)
                {
                    Debug.LogWarning("未找到 TMP 默认字体。需要先导入 TMP Essentials；笔记本层级仍已创建。");
                }

                GameObject journalCanvas = GetOrCreateRootCanvas(JournalCanvasName);
                ConfigureCanvas(journalCanvas, JournalSortingOrder);
                JournalUI journalUI = EnsureComponent<JournalUI>(journalCanvas);
                JournalReferences journalReferences = BuildJournalHierarchy(journalCanvas.transform, defaultFont);
                WireJournalUI(journalUI, journalReferences, ghostDefinitions);

                Component evidenceManager = GetOrCreateEvidenceManager();
                WireGhostDefinitionList(evidenceManager, "_allGhosts", ghostDefinitions);
                WireEvidenceManager(journalUI, evidenceManager);

                ResultScreenUI resultScreenUI = FindComponentInActiveScene<ResultScreenUI>();
                Component gameManager = FindComponentInActiveSceneByTypeName(GameManagerTypeName);
                Component playerController = FindComponentInActiveSceneByTypeName(PlayerControllerTypeName);
                string missingConnections = WireRemainingSceneConnections(
                    journalUI,
                    resultScreenUI,
                    gameManager,
                    playerController);

                UnityEditor.Undo.CollapseUndoOperations(undoGroup);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                UnityEditor.Selection.activeGameObject = journalCanvas;

                Debug.Log(
                    "判定笔记本已搭建并完成可用的场景接线。_ghostEvidenceCells 数组顺序：" +
                    string.Join("、", journalReferences.GhostEvidenceCellNames),
                    journalCanvas);

                if (!string.IsNullOrEmpty(missingConnections))
                {
                    Debug.LogWarning(missingConnections, journalCanvas);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                UnityEditor.Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogError("判定笔记本搭建失败，已撤销本次创建和接线。");
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

        private static ConfirmReferences BuildConfirmHierarchy(
            Transform confirmCanvasTransform,
            TMPro.TMP_FontAsset defaultFont)
        {
            UnityEngine.UI.Image panel = EnsureImage(confirmCanvasTransform, PanelName);
            ConfigureStretchRect(panel.rectTransform);
            ConfigureImage(panel, ResultPanelColor, true);

            UnityEngine.UI.Image box = EnsureImage(panel.transform, BoxName);
            ConfigureRect(
                box.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(ConfirmBoxWidth, ConfirmBoxHeight));
            ConfigureImage(box, ConfirmBoxColor, false);

            TMPro.TextMeshProUGUI messageLabel = EnsureText(box.transform, MessageName);
            ConfigureRect(
                messageLabel.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, ConfirmMessageOffsetY),
                new Vector2(ConfirmMessageWidth, ConfirmMessageHeight));
            ConfigureText(
                messageLabel,
                defaultFont,
                string.Empty,
                ConfirmMessageFontSize,
                TMPro.TextAlignmentOptions.Center,
                true,
                false);

            UnityEngine.UI.Button confirmButton = BuildConfirmButton(
                box.transform,
                ConfirmButtonName,
                new Vector2(-ConfirmButtonOffsetX, ConfirmButtonOffsetY),
                "撤离",
                defaultFont);
            UnityEngine.UI.Button cancelButton = BuildConfirmButton(
                box.transform,
                CancelButtonName,
                new Vector2(ConfirmButtonOffsetX, ConfirmButtonOffsetY),
                "再等等",
                defaultFont);

            UnityEditor.Undo.RecordObject(panel.gameObject, UndoLabel);
            panel.gameObject.SetActive(false);

            return new ConfirmReferences(
                panel.gameObject,
                messageLabel,
                confirmButton,
                cancelButton);
        }

        private static MainMenuReferences BuildMainMenuHierarchy(
            Transform mainMenuCanvasTransform,
            TMPro.TMP_FontAsset defaultFont)
        {
            UnityEngine.UI.Image panel = EnsureImage(mainMenuCanvasTransform, PanelName);
            ConfigureStretchRect(panel.rectTransform);
            ConfigureImage(panel, MainMenuBackgroundColor, true);

            UnityEngine.UI.Image background = EnsureImage(panel.transform, BackgroundName);
            ConfigureStretchRect(background.rectTransform);
            ConfigureImage(background, MainMenuBackgroundColor, false);
            UnityEditor.Undo.RecordObject(background, UndoLabel);
            background.sprite = null;
            SetMainMenuSiblingIndex(background.transform, 0);

            TMPro.TextMeshProUGUI titleLabel = EnsureText(panel.transform, TitleName);
            ConfigureRect(
                titleLabel.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, MainMenuTitleOffsetY),
                new Vector2(MainMenuTitleWidth, MainMenuTitleHeight));
            ConfigureText(
                titleLabel,
                defaultFont,
                "残响",
                MainMenuTitleFontSize,
                TMPro.TextAlignmentOptions.Center,
                false,
                false);
            SetMainMenuSiblingIndex(titleLabel.transform, 1);

            TMPro.TextMeshProUGUI subtitleLabel = EnsureText(panel.transform, SubtitleName);
            ConfigureRect(
                subtitleLabel.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, MainMenuSubtitleOffsetY),
                new Vector2(MainMenuSubtitleWidth, MainMenuSubtitleHeight));
            ConfigureText(
                subtitleLabel,
                defaultFont,
                "RESIDUUM",
                MainMenuSubtitleFontSize,
                TMPro.TextAlignmentOptions.Center,
                false,
                false);
            SetMainMenuSiblingIndex(subtitleLabel.transform, 2);

            UnityEngine.UI.Button startButton = BuildMainMenuButton(
                panel.transform,
                StartButtonName,
                MainMenuStartButtonOffsetY,
                "开始游戏",
                defaultFont);
            SetMainMenuSiblingIndex(startButton.transform, 3);

            UnityEngine.UI.Button quitButton = BuildMainMenuButton(
                panel.transform,
                QuitButtonName,
                MainMenuStartButtonOffsetY - MainMenuButtonHeight - MainMenuButtonSpacing,
                "退出",
                defaultFont);
            SetMainMenuSiblingIndex(quitButton.transform, 4);

            UnityEditor.Undo.RecordObject(panel.gameObject, UndoLabel);
            panel.gameObject.SetActive(true);

            return new MainMenuReferences(
                panel.gameObject,
                background,
                titleLabel,
                subtitleLabel,
                startButton,
                quitButton);
        }

        private static UnityEngine.UI.Button BuildMainMenuButton(
            Transform parent,
            string buttonName,
            float offsetY,
            string label,
            TMPro.TMP_FontAsset defaultFont)
        {
            UnityEngine.UI.Image buttonImage = EnsureImage(parent, buttonName);
            ConfigureRect(
                buttonImage.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, offsetY),
                new Vector2(MainMenuButtonWidth, MainMenuButtonHeight));
            ConfigureImage(buttonImage, MainMenuButtonColor, true);

            UnityEngine.UI.Button button =
                EnsureComponent<UnityEngine.UI.Button>(buttonImage.gameObject);
            UnityEditor.Undo.RecordObject(button, UndoLabel);
            button.targetGraphic = buttonImage;
            ConfigureButtonText(buttonImage.transform, defaultFont, label, MainMenuButtonFontSize);
            return button;
        }

        private static UnityEngine.UI.Button BuildConfirmButton(
            Transform parent,
            string buttonName,
            Vector2 anchoredPosition,
            string label,
            TMPro.TMP_FontAsset defaultFont)
        {
            UnityEngine.UI.Image buttonImage = EnsureImage(parent, buttonName);
            ConfigureRect(
                buttonImage.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                anchoredPosition,
                new Vector2(ConfirmButtonWidth, ConfirmButtonHeight));
            ConfigureImage(buttonImage, ConfirmButtonColor, true);

            UnityEngine.UI.Button button =
                EnsureComponent<UnityEngine.UI.Button>(buttonImage.gameObject);
            UnityEditor.Undo.RecordObject(button, UndoLabel);
            button.targetGraphic = buttonImage;
            ConfigureButtonText(buttonImage.transform, defaultFont, label, ConfirmButtonFontSize);
            return button;
        }

        private static JournalReferences BuildJournalHierarchy(
            Transform journalCanvasTransform,
            TMPro.TMP_FontAsset defaultFont)
        {
            UnityEngine.UI.Image panel = EnsureImage(journalCanvasTransform, PanelName);
            ConfigureStretchRect(panel.rectTransform);
            ConfigureImage(panel, JournalPanelColor, true);

            GameObject evidence = GetOrCreateUiChild(panel.transform, EvidenceName);
            SetSiblingIndex(evidence.transform, 0);
            ConfigureStretchRect(RequireRectTransform(evidence, EvidenceName));
            TMPro.TextMeshProUGUI[] evidenceLabels = new TMPro.TextMeshProUGUI[EvidenceLabelCount];
            UnityEngine.UI.Button[] evidenceRuleButtons =
                new UnityEngine.UI.Button[EvidenceLabelCount];
            for (int rowIndex = 0; rowIndex < EvidenceLabelCount; rowIndex++)
            {
                GameObject row = GetOrCreateUiChild(evidence.transform, RowPrefix + rowIndex);
                SetSiblingIndex(row.transform, rowIndex);
                ConfigureRect(
                    RequireRectTransform(row, row.name),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(
                        JournalLeftMargin,
                        -JournalTopMargin - rowIndex *
                        (JournalEvidenceRowHeight + JournalEvidenceRowSpacing)),
                    new Vector2(JournalEvidenceRowWidth, JournalEvidenceRowHeight));

                TMPro.TextMeshProUGUI label = EnsureText(row.transform, LabelName);
                SetSiblingIndex(label.transform, 0);
                ConfigureRect(
                    label.rectTransform,
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    Vector2.zero,
                    new Vector2(JournalEvidenceLabelWidth, JournalEvidenceRowHeight));
                ConfigureText(
                    label,
                    defaultFont,
                    string.Empty,
                    JournalEvidenceFontSize,
                    TMPro.TextAlignmentOptions.Left,
                    false,
                    false);
                evidenceLabels[rowIndex] = label;

                UnityEngine.UI.Image ruleButtonImage =
                    EnsureImage(row.transform, RuleButtonName);
                SetSiblingIndex(ruleButtonImage.transform, 1);
                ConfigureRect(
                    ruleButtonImage.rectTransform,
                    new Vector2(1f, 0.5f),
                    new Vector2(1f, 0.5f),
                    new Vector2(1f, 0.5f),
                    Vector2.zero,
                    new Vector2(JournalRuleButtonWidth, JournalRuleButtonHeight));
                ConfigureImage(ruleButtonImage, JournalButtonColor, true);
                UnityEngine.UI.Button ruleButton =
                    EnsureComponent<UnityEngine.UI.Button>(ruleButtonImage.gameObject);
                UnityEditor.Undo.RecordObject(ruleButton, JournalUndoLabel);
                ruleButton.targetGraphic = ruleButtonImage;
                ConfigureButtonText(ruleButtonImage.transform, defaultFont, "排除", JournalButtonFontSize);
                evidenceRuleButtons[rowIndex] = ruleButton;
            }

            GameObject deductionTable = GetOrCreateUiChild(panel.transform, DeductionTableName);
            SetSiblingIndex(deductionTable.transform, 1);
            UnityEngine.UI.GridLayoutGroup gridLayout =
                EnsureComponent<UnityEngine.UI.GridLayoutGroup>(deductionTable);
            ConfigureRect(
                RequireRectTransform(deductionTable, DeductionTableName),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-JournalTableRightMargin, -JournalTopMargin),
                new Vector2(
                    TableColumnCount * JournalTableCellWidth +
                    (TableColumnCount - 1) * JournalTableSpacing,
                    (GhostCount + 1) * JournalTableCellHeight +
                    GhostCount * JournalTableSpacing));
            ConfigureGridLayout(gridLayout);

            string[] headerTexts = { "鬼种", "EMF-5", "紫外线", "鬼影书写" };
            for (int headerIndex = 0; headerIndex < TableColumnCount; headerIndex++)
            {
                TMPro.TextMeshProUGUI header = EnsureText(
                    deductionTable.transform,
                    HeaderPrefix + headerIndex);
                SetSiblingIndex(header.transform, headerIndex);
                ConfigureText(
                    header,
                    defaultFont,
                    headerTexts[headerIndex],
                    JournalTableFontSize,
                    TMPro.TextAlignmentOptions.Center,
                    false,
                    false);
            }

            TMPro.TextMeshProUGUI[] ghostNameLabels =
                new TMPro.TextMeshProUGUI[GhostCount];
            TMPro.TextMeshProUGUI[] ghostEvidenceCells =
                new TMPro.TextMeshProUGUI[GhostEvidenceCellCount];
            for (int ghostIndex = 0; ghostIndex < GhostCount; ghostIndex++)
            {
                int rowStartIndex = TableColumnCount + ghostIndex * TableColumnCount;
                TMPro.TextMeshProUGUI ghostName = EnsureText(
                    deductionTable.transform,
                    GhostNamePrefix + ghostIndex);
                SetSiblingIndex(ghostName.transform, rowStartIndex);
                ConfigureText(
                    ghostName,
                    defaultFont,
                    string.Empty,
                    JournalTableFontSize,
                    TMPro.TextAlignmentOptions.Center,
                    false,
                    false);
                ghostNameLabels[ghostIndex] = ghostName;

                for (int evidenceIndex = 0; evidenceIndex < EvidenceColumnsPerGhost; evidenceIndex++)
                {
                    int cellIndex = ghostIndex * EvidenceColumnsPerGhost + evidenceIndex;
                    TMPro.TextMeshProUGUI evidenceCell = EnsureText(
                        deductionTable.transform,
                        GhostEvidencePrefix + cellIndex);
                    SetSiblingIndex(evidenceCell.transform, rowStartIndex + evidenceIndex + 1);
                    ConfigureText(
                        evidenceCell,
                        defaultFont,
                        string.Empty,
                        JournalTableFontSize,
                        TMPro.TextAlignmentOptions.Center,
                        false,
                        false);
                    ghostEvidenceCells[cellIndex] = evidenceCell;
                }
            }

            GameObject guessButtonsObject = GetOrCreateUiChild(panel.transform, GuessButtonsName);
            SetSiblingIndex(guessButtonsObject.transform, 2);
            ConfigureRect(
                RequireRectTransform(guessButtonsObject, GuessButtonsName),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, JournalGuessButtonBottomMargin),
                new Vector2(
                    GhostCount * JournalGuessButtonWidth +
                    (GhostCount - 1) * JournalGuessButtonSpacing,
                    JournalGuessButtonHeight));
            UnityEngine.UI.HorizontalLayoutGroup guessLayout =
                EnsureComponent<UnityEngine.UI.HorizontalLayoutGroup>(guessButtonsObject);
            ConfigureGuessButtonLayout(guessLayout);

            UnityEngine.UI.Button[] guessButtons = new UnityEngine.UI.Button[GhostCount];
            for (int guessIndex = 0; guessIndex < GhostCount; guessIndex++)
            {
                UnityEngine.UI.Image guessButtonImage = EnsureImage(
                    guessButtonsObject.transform,
                    GuessPrefix + guessIndex);
                SetSiblingIndex(guessButtonImage.transform, guessIndex);
                ConfigureRect(
                    guessButtonImage.rectTransform,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(JournalGuessButtonWidth, JournalGuessButtonHeight));
                ConfigureImage(guessButtonImage, JournalButtonColor, true);
                UnityEngine.UI.Button guessButton =
                    EnsureComponent<UnityEngine.UI.Button>(guessButtonImage.gameObject);
                UnityEditor.Undo.RecordObject(guessButton, JournalUndoLabel);
                guessButton.targetGraphic = guessButtonImage;
                ConfigureButtonText(
                    guessButtonImage.transform,
                    defaultFont,
                    "判定 " + (guessIndex + 1),
                    JournalButtonFontSize);
                guessButtons[guessIndex] = guessButton;
            }

            UnityEditor.Undo.RecordObject(panel.gameObject, JournalUndoLabel);
            panel.gameObject.SetActive(false);

            return new JournalReferences(
                panel.gameObject,
                evidenceLabels,
                evidenceRuleButtons,
                ghostNameLabels,
                ghostEvidenceCells,
                guessButtons);
        }

        private static void WireJournalUI(
            JournalUI journalUI,
            JournalReferences references,
            UnityEngine.Object[] ghostDefinitions)
        {
            UnityEditor.Undo.RecordObject(journalUI, JournalUndoLabel);
            UnityEditor.SerializedObject serializedJournalUI =
                new UnityEditor.SerializedObject(journalUI);
            serializedJournalUI.Update();

            GetRequiredProperty(serializedJournalUI, "_journalRoot").objectReferenceValue =
                references.PanelRoot;
            WireObjectArray(
                GetRequiredProperty(serializedJournalUI, "_evidenceLabels"),
                references.EvidenceLabels,
                "JournalUI._evidenceLabels");
            WireObjectArray(
                GetRequiredProperty(serializedJournalUI, "_evidenceRuleButtons"),
                references.EvidenceRuleButtons,
                "JournalUI._evidenceRuleButtons");
            WireObjectArray(
                GetRequiredProperty(serializedJournalUI, "_ghostNameLabels"),
                references.GhostNameLabels,
                "JournalUI._ghostNameLabels");
            WireObjectArray(
                GetRequiredProperty(serializedJournalUI, "_ghostEvidenceCells"),
                references.GhostEvidenceCells,
                "JournalUI._ghostEvidenceCells");
            WireObjectArray(
                GetRequiredProperty(serializedJournalUI, "_guessButtons"),
                references.GuessButtons,
                "JournalUI._guessButtons");
            WireObjectArray(
                GetRequiredProperty(serializedJournalUI, "_allGhosts"),
                ghostDefinitions,
                "JournalUI._allGhosts");

            serializedJournalUI.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireEvidenceManager(JournalUI journalUI, Component evidenceManager)
        {
            UnityEditor.Undo.RecordObject(journalUI, JournalUndoLabel);
            UnityEditor.SerializedObject serializedJournalUI =
                new UnityEditor.SerializedObject(journalUI);
            serializedJournalUI.Update();
            GetRequiredProperty(serializedJournalUI, "_evidenceManager").objectReferenceValue =
                evidenceManager;
            serializedJournalUI.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Component GetOrCreateEvidenceManager()
        {
            Type evidenceManagerType = FindLoadedType(EvidenceManagerTypeName);
            if (evidenceManagerType == null)
            {
                throw new InvalidOperationException(
                    "未找到 Residuum.Evidence.EvidenceManager 类型，无法创建场景依赖。");
            }

            GameObject evidenceManagerObject = FindSceneObject(EvidenceManagerName);
            if (evidenceManagerObject == null)
            {
                evidenceManagerObject = new GameObject(EvidenceManagerName);
                UnityEditor.Undo.RegisterCreatedObjectUndo(evidenceManagerObject, JournalUndoLabel);
            }

            Component evidenceManager = evidenceManagerObject.GetComponent(evidenceManagerType);
            if (evidenceManager == null)
            {
                evidenceManager = UnityEditor.Undo.AddComponent(
                    evidenceManagerObject,
                    evidenceManagerType);
            }

            if (evidenceManager == null)
            {
                throw new InvalidOperationException("无法为 EvidenceManager 物体添加 EvidenceManager 组件。");
            }

            return evidenceManager;
        }

        private static void WireGhostDefinitionList(
            Component target,
            string fieldName,
            UnityEngine.Object[] ghostDefinitions)
        {
            UnityEditor.Undo.RecordObject(target, JournalUndoLabel);
            UnityEditor.SerializedObject serializedTarget = new UnityEditor.SerializedObject(target);
            serializedTarget.Update();
            WireObjectArray(
                GetRequiredProperty(serializedTarget, fieldName),
                ghostDefinitions,
                target.GetType().Name + "." + fieldName);
            serializedTarget.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string WireRemainingSceneConnections(
            JournalUI journalUI,
            ResultScreenUI resultScreenUI,
            Component gameManager,
            Component playerController)
        {
            System.Text.StringBuilder warnings = new System.Text.StringBuilder();

            if (gameManager == null)
            {
                warnings.AppendLine(
                    "没找到 GameManager：JournalUI.onGuessSubmitted → GameManager.SubmitGuess 未连接。");
                warnings.AppendLine(
                    "没找到 GameManager：ResultScreenUI.onRestartRequested → GameManager.StartRound 未连接。");
            }
            else
            {
                WireJournalGuessEvent(journalUI, gameManager);

                if (resultScreenUI != null)
                {
                    WireResultRestartEvent(resultScreenUI, gameManager);
                }
                else
                {
                    warnings.AppendLine(
                        "没找到 ResultScreenUI：onRestartRequested → GameManager.StartRound 未连接。");
                }
            }

            if (resultScreenUI == null)
            {
                warnings.AppendLine(
                    "没找到 ResultScreenUI：_playerControllerBehaviour 未连接。");
            }
            else if (playerController == null)
            {
                warnings.AppendLine(
                    "没找到 PlayerController：ResultScreenUI._playerControllerBehaviour 未连接。");
            }
            else
            {
                UnityEditor.Undo.RecordObject(resultScreenUI, JournalUndoLabel);
                UnityEditor.SerializedObject serializedResultScreenUI =
                    new UnityEditor.SerializedObject(resultScreenUI);
                serializedResultScreenUI.Update();
                GetRequiredProperty(serializedResultScreenUI, "_playerControllerBehaviour").objectReferenceValue =
                    playerController;
                serializedResultScreenUI.ApplyModifiedPropertiesWithoutUndo();
            }

            return warnings.ToString().TrimEnd();
        }

        private static void WireJournalGuessEvent(JournalUI journalUI, Component gameManager)
        {
            UnityEditor.Undo.RecordObject(journalUI, JournalUndoLabel);
            RemovePersistentListeners(journalUI.onGuessSubmitted, "SubmitGuess");
            UnityEngine.Events.UnityAction<Residuum.Ghost.GhostDefinition> listener =
                CreatePersistentListener<UnityEngine.Events.UnityAction<Residuum.Ghost.GhostDefinition>>(
                    gameManager,
                    "SubmitGuess");
            if (listener == null)
            {
                throw new InvalidOperationException(
                    "GameManager 缺少 SubmitGuess(GhostDefinition)，无法连接 JournalUI.onGuessSubmitted。");
            }

            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                journalUI.onGuessSubmitted,
                listener);
        }

        private static void WireResultRestartEvent(ResultScreenUI resultScreenUI, Component gameManager)
        {
            UnityEditor.Undo.RecordObject(resultScreenUI, JournalUndoLabel);
            RemovePersistentListeners(resultScreenUI.onRestartRequested, "StartRound");
            UnityEngine.Events.UnityAction listener =
                CreatePersistentListener<UnityEngine.Events.UnityAction>(gameManager, "StartRound");
            if (listener == null)
            {
                throw new InvalidOperationException(
                    "GameManager 缺少 StartRound()，无法连接 ResultScreenUI.onRestartRequested。");
            }

            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                resultScreenUI.onRestartRequested,
                listener);
        }

        private static void WireMainMenuStartEvent(MainMenuUI mainMenuUI, Component gameManager)
        {
            UnityEditor.Undo.RecordObject(mainMenuUI, UndoLabel);
            if (mainMenuUI.onStartRequested == null)
            {
                mainMenuUI.onStartRequested = new UnityEngine.Events.UnityEvent();
            }

            RemovePersistentListeners(mainMenuUI.onStartRequested, "StartRound");
            if (gameManager == null)
            {
                return;
            }

            UnityEngine.Events.UnityAction listener =
                CreatePersistentListener<UnityEngine.Events.UnityAction>(gameManager, "StartRound");
            if (listener == null)
            {
                Debug.LogWarning(
                    "GameManager 缺少 StartRound()：MainMenuUI.onStartRequested 未连接。",
                    mainMenuUI);
                return;
            }

            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                mainMenuUI.onStartRequested,
                listener);
        }

        private static void WireEvacuateConfirmedEvent(
            EvacuateConfirmUI evacuateConfirmUI,
            Component gameManager)
        {
            UnityEditor.Undo.RecordObject(evacuateConfirmUI, UndoLabel);
            RemovePersistentListeners(evacuateConfirmUI.onEvacuateConfirmed, "RequestEvacuate");
            UnityEngine.Events.UnityAction listener =
                CreatePersistentListener<UnityEngine.Events.UnityAction>(
                    gameManager,
                    "RequestEvacuate");
            if (listener == null)
            {
                throw new InvalidOperationException(
                    "GameManager 缺少 RequestEvacuate()，无法连接 " +
                    "EvacuateConfirmUI.onEvacuateConfirmed。");
            }

            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                evacuateConfirmUI.onEvacuateConfirmed,
                listener);
        }

        private static TDelegate CreatePersistentListener<TDelegate>(Component target, string methodName)
            where TDelegate : Delegate
        {
            try
            {
                return (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), target, methodName);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static void RemovePersistentListeners(
            UnityEngine.Events.UnityEventBase unityEvent,
            string methodName)
        {
            for (int listenerIndex = unityEvent.GetPersistentEventCount() - 1;
                 listenerIndex >= 0;
                 listenerIndex--)
            {
                if (unityEvent.GetPersistentMethodName(listenerIndex) == methodName)
                {
                    UnityEditor.Events.UnityEventTools.RemovePersistentListener(
                        unityEvent,
                        listenerIndex);
                }
            }
        }

        private static UnityEngine.Object[] FindSortedGhostDefinitions()
        {
            string[] assetGuids = UnityEditor.AssetDatabase.FindAssets("t:GhostDefinition");
            if (assetGuids == null || assetGuids.Length == 0)
            {
                Debug.LogError("未找到任何 GhostDefinition 资产，已中止判定笔记本搭建。");
                return null;
            }

            if (assetGuids.Length != GhostCount)
            {
                Debug.LogError(
                    "当前笔记本固定为三行推理表，但找到 " + assetGuids.Length +
                    " 个 GhostDefinition 资产。请先保持鬼种资产数量为 3，已中止判定笔记本搭建。");
                return null;
            }

            string[] assetPaths = new string[assetGuids.Length];
            for (int assetIndex = 0; assetIndex < assetGuids.Length; assetIndex++)
            {
                assetPaths[assetIndex] = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuids[assetIndex]);
            }

            Array.Sort(assetPaths, StringComparer.Ordinal);
            UnityEngine.Object[] ghostDefinitions = new UnityEngine.Object[assetPaths.Length];
            for (int assetIndex = 0; assetIndex < assetPaths.Length; assetIndex++)
            {
                ghostDefinitions[assetIndex] =
                    UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPaths[assetIndex]);
                if (ghostDefinitions[assetIndex] == null)
                {
                    Debug.LogError(
                        "无法加载 GhostDefinition 资产：" + assetPaths[assetIndex] + "，已中止判定笔记本搭建。");
                    return null;
                }
            }

            return ghostDefinitions;
        }

        private static Type FindLoadedType(string typeName)
        {
            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static Component FindComponentInActiveSceneByTypeName(string typeName)
        {
            Type componentType = FindLoadedType(typeName);
            if (componentType == null || !typeof(Component).IsAssignableFrom(componentType))
            {
                return null;
            }

            GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                Component[] components = roots[rootIndex].GetComponentsInChildren(componentType, true);
                if (components.Length > 0)
                {
                    return components[0];
                }
            }

            return null;
        }

        private static GameObject FindSceneObject(string objectName)
        {
            GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                Transform foundTransform = FindTransformByName(roots[rootIndex].transform, objectName);
                if (foundTransform != null)
                {
                    return foundTransform.gameObject;
                }
            }

            return null;
        }

        private static Transform FindTransformByName(Transform current, string objectName)
        {
            if (current.name == objectName)
            {
                return current;
            }

            for (int childIndex = 0; childIndex < current.childCount; childIndex++)
            {
                Transform foundTransform = FindTransformByName(current.GetChild(childIndex), objectName);
                if (foundTransform != null)
                {
                    return foundTransform;
                }
            }

            return null;
        }

        private static void WireObjectArray(
            UnityEditor.SerializedProperty property,
            UnityEngine.Object[] values,
            string propertyDescription)
        {
            if (!property.isArray)
            {
                Debug.LogError(propertyDescription + " 不是数组，无法完成接线。");
                throw new InvalidOperationException(propertyDescription + " 不是数组。");
            }

            property.arraySize = values.Length;
            for (int valueIndex = 0; valueIndex < values.Length; valueIndex++)
            {
                property.GetArrayElementAtIndex(valueIndex).objectReferenceValue = values[valueIndex];
            }
        }

        private static void ConfigureGridLayout(UnityEngine.UI.GridLayoutGroup gridLayout)
        {
            UnityEditor.Undo.RecordObject(gridLayout, JournalUndoLabel);
            gridLayout.cellSize = new Vector2(JournalTableCellWidth, JournalTableCellHeight);
            gridLayout.spacing = new Vector2(JournalTableSpacing, JournalTableSpacing);
            gridLayout.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = TableColumnCount;
            gridLayout.childAlignment = TextAnchor.UpperLeft;
            gridLayout.padding = new RectOffset();
        }

        private static void ConfigureGuessButtonLayout(
            UnityEngine.UI.HorizontalLayoutGroup layoutGroup)
        {
            UnityEditor.Undo.RecordObject(layoutGroup, JournalUndoLabel);
            layoutGroup.spacing = JournalGuessButtonSpacing;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.padding = new RectOffset();
        }

        private static void ConfigureButtonText(
            Transform buttonTransform,
            TMPro.TMP_FontAsset defaultFont,
            string value,
            float fontSize)
        {
            TMPro.TextMeshProUGUI buttonText = EnsureText(buttonTransform, ButtonTextName);
            ConfigureStretchRect(buttonText.rectTransform);
            ConfigureText(
                buttonText,
                defaultFont,
                value,
                fontSize,
                TMPro.TextAlignmentOptions.Center,
                false,
                false);
        }

        private static void SetSiblingIndex(Transform transform, int siblingIndex)
        {
            if (transform.GetSiblingIndex() == siblingIndex)
            {
                return;
            }

            UnityEditor.Undo.RecordObject(transform, JournalUndoLabel);
            transform.SetSiblingIndex(siblingIndex);
        }

        private static void SetMainMenuSiblingIndex(Transform transform, int siblingIndex)
        {
            if (transform.GetSiblingIndex() == siblingIndex)
            {
                return;
            }

            UnityEditor.Undo.RecordObject(transform, UndoLabel);
            transform.SetSiblingIndex(siblingIndex);
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

        private static void WireEvacuateConfirmUI(
            EvacuateConfirmUI evacuateConfirmUI,
            ConfirmReferences references)
        {
            UnityEditor.Undo.RecordObject(evacuateConfirmUI, UndoLabel);
            UnityEditor.SerializedObject serializedEvacuateConfirmUI =
                new UnityEditor.SerializedObject(evacuateConfirmUI);
            serializedEvacuateConfirmUI.Update();

            GetRequiredProperty(serializedEvacuateConfirmUI, "_panelRoot").objectReferenceValue =
                references.PanelRoot;
            GetRequiredProperty(serializedEvacuateConfirmUI, "_messageLabel").objectReferenceValue =
                references.MessageLabel;
            GetRequiredProperty(serializedEvacuateConfirmUI, "_confirmButton").objectReferenceValue =
                references.ConfirmButton;
            GetRequiredProperty(serializedEvacuateConfirmUI, "_cancelButton").objectReferenceValue =
                references.CancelButton;

            serializedEvacuateConfirmUI.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireMainMenuUI(
            MainMenuUI mainMenuUI,
            MainMenuReferences references,
            Component playerController)
        {
            UnityEditor.Undo.RecordObject(mainMenuUI, UndoLabel);
            UnityEditor.SerializedObject serializedMainMenuUI =
                new UnityEditor.SerializedObject(mainMenuUI);
            serializedMainMenuUI.Update();

            GetRequiredProperty(serializedMainMenuUI, "_panelRoot").objectReferenceValue =
                references.PanelRoot;
            GetRequiredProperty(serializedMainMenuUI, "_backgroundImage").objectReferenceValue =
                references.BackgroundImage;
            GetRequiredProperty(serializedMainMenuUI, "_titleLabel").objectReferenceValue =
                references.TitleLabel;
            GetRequiredProperty(serializedMainMenuUI, "_subtitleLabel").objectReferenceValue =
                references.SubtitleLabel;
            GetRequiredProperty(serializedMainMenuUI, "_startButton").objectReferenceValue =
                references.StartButton;
            GetRequiredProperty(serializedMainMenuUI, "_quitButton").objectReferenceValue =
                references.QuitButton;
            GetRequiredProperty(serializedMainMenuUI, "_playerControllerBehaviour").objectReferenceValue =
                playerController;

            serializedMainMenuUI.ApplyModifiedPropertiesWithoutUndo();
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
            GameObject child;
            if (existingChild == null)
            {
                child = new GameObject(childName, typeof(RectTransform));
                UnityEditor.Undo.RegisterCreatedObjectUndo(child, UndoLabel);
                UnityEditor.Undo.SetTransformParent(child.transform, parent, UndoLabel);
            }
            else
            {
                child = existingChild.gameObject;
            }

            RequireRectTransform(child, childName);
            UnityEditor.Undo.RecordObject(child.transform, UndoLabel);
            child.transform.localScale = Vector3.one;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localPosition = Vector3.zero;
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

        private readonly struct ConfirmReferences
        {
            public ConfirmReferences(
                GameObject panelRoot,
                TMPro.TextMeshProUGUI messageLabel,
                UnityEngine.UI.Button confirmButton,
                UnityEngine.UI.Button cancelButton)
            {
                PanelRoot = panelRoot;
                MessageLabel = messageLabel;
                ConfirmButton = confirmButton;
                CancelButton = cancelButton;
            }

            public GameObject PanelRoot { get; }
            public TMPro.TextMeshProUGUI MessageLabel { get; }
            public UnityEngine.UI.Button ConfirmButton { get; }
            public UnityEngine.UI.Button CancelButton { get; }
        }

        private readonly struct MainMenuReferences
        {
            public MainMenuReferences(
                GameObject panelRoot,
                UnityEngine.UI.Image backgroundImage,
                TMPro.TextMeshProUGUI titleLabel,
                TMPro.TextMeshProUGUI subtitleLabel,
                UnityEngine.UI.Button startButton,
                UnityEngine.UI.Button quitButton)
            {
                PanelRoot = panelRoot;
                BackgroundImage = backgroundImage;
                TitleLabel = titleLabel;
                SubtitleLabel = subtitleLabel;
                StartButton = startButton;
                QuitButton = quitButton;
            }

            public GameObject PanelRoot { get; }
            public UnityEngine.UI.Image BackgroundImage { get; }
            public TMPro.TextMeshProUGUI TitleLabel { get; }
            public TMPro.TextMeshProUGUI SubtitleLabel { get; }
            public UnityEngine.UI.Button StartButton { get; }
            public UnityEngine.UI.Button QuitButton { get; }
        }

        private readonly struct JournalReferences
        {
            public JournalReferences(
                GameObject panelRoot,
                TMPro.TextMeshProUGUI[] evidenceLabels,
                UnityEngine.UI.Button[] evidenceRuleButtons,
                TMPro.TextMeshProUGUI[] ghostNameLabels,
                TMPro.TextMeshProUGUI[] ghostEvidenceCells,
                UnityEngine.UI.Button[] guessButtons)
            {
                PanelRoot = panelRoot;
                EvidenceLabels = evidenceLabels;
                EvidenceRuleButtons = evidenceRuleButtons;
                GhostNameLabels = ghostNameLabels;
                GhostEvidenceCells = ghostEvidenceCells;
                GuessButtons = guessButtons;

                GhostEvidenceCellNames = new string[ghostEvidenceCells.Length];
                for (int cellIndex = 0; cellIndex < ghostEvidenceCells.Length; cellIndex++)
                {
                    GhostEvidenceCellNames[cellIndex] = ghostEvidenceCells[cellIndex].gameObject.name;
                }
            }

            public GameObject PanelRoot { get; }
            public TMPro.TextMeshProUGUI[] EvidenceLabels { get; }
            public UnityEngine.UI.Button[] EvidenceRuleButtons { get; }
            public TMPro.TextMeshProUGUI[] GhostNameLabels { get; }
            public TMPro.TextMeshProUGUI[] GhostEvidenceCells { get; }
            public UnityEngine.UI.Button[] GuessButtons { get; }
            public string[] GhostEvidenceCellNames { get; }
        }
    }
}
#endif

using System;
using UnityEngine;

namespace Residuum.Core.Editor
{
    /// <summary>
    /// 在当前场景中创建回合流程所需的最小装配，并完成跨模块引用接线。
    /// </summary>
    public static class RoundSetupBuilder
    {
        private const string MenuPath = "Residuum/搭建回合装配";
        private const string UndoLabel = "搭建回合装配";
        private const string RoundSetupName = "RoundSetup";
        private const string GameManagerName = "GameManager";
        private const string HuntControllerName = "HuntController";
        private const string ExitZoneName = "ExitZone";
        private const string SafeZoneName = "SafeZone";
        private const string GhostAssetFolder = "Assets/_Project/ScriptableObjects/Ghosts";
        private const string SpiritAssetPath = GhostAssetFolder + "/GhostDef_Spirit.asset";
        private const string WraithAssetPath = GhostAssetFolder + "/GhostDef_Wraith.asset";
        private const string PoltergeistAssetPath = GhostAssetFolder + "/GhostDef_Poltergeist.asset";

        private const int GhostDefinitionCount = 3;
        private static readonly Vector3 ExitZoneSize = new Vector3(3f, 3f, 3f);
        private static readonly Vector3 SafeZoneSize = new Vector3(10f, 4f, 10f);

        [UnityEditor.MenuItem(MenuPath)]
        private static void BuildRoundSetup()
        {
            GameObject existingRoundSetup = FindExistingRoundSetup();
            if (existingRoundSetup != null)
            {
                UnityEditor.Selection.activeGameObject = existingRoundSetup;
                Debug.Log("场景中已经存在 RoundSetup，未重复创建回合装配，已选中它。", existingRoundSetup);
                return;
            }

            Residuum.Player.PlayerController playerController =
                UnityEngine.Object.FindAnyObjectByType<Residuum.Player.PlayerController>(
                    UnityEngine.FindObjectsInactive.Include);
            if (playerController == null)
            {
                Debug.LogError("场景中没有 PlayerController。请先执行 Residuum/搭建玩家装配，未创建回合装配。");
                return;
            }

            Residuum.Ghost.GhostAI ghostAI =
                UnityEngine.Object.FindAnyObjectByType<Residuum.Ghost.GhostAI>(
                    UnityEngine.FindObjectsInactive.Include);
            if (ghostAI == null)
            {
                Debug.LogError("场景中没有 GhostAI。请先执行 Residuum/搭建鬼装配，未创建回合装配。");
                return;
            }

            Residuum.World.RoomManager roomManager =
                UnityEngine.Object.FindAnyObjectByType<Residuum.World.RoomManager>(
                    UnityEngine.FindObjectsInactive.Include);
            if (roomManager == null)
            {
                Debug.LogError("场景中没有 RoomManager。请先手动创建并摆好 RoomVolume，未创建回合装配。");
                return;
            }

            Residuum.Ghost.GhostDefinition[] ghostDefinitions = LoadGhostDefinitions();
            if (ghostDefinitions == null)
            {
                return;
            }

            int undoGroup = UnityEditor.Undo.GetCurrentGroup();
            UnityEditor.Undo.SetCurrentGroupName(UndoLabel);

            try
            {
                GameObject roundSetup = new GameObject(RoundSetupName);
                UnityEditor.Undo.RegisterCreatedObjectUndo(roundSetup, UndoLabel);

                Residuum.Player.PlayerSanity sanity = EnsurePlayerSanity(playerController);
                Residuum.Ghost.HuntController huntController = CreateHuntController(roundSetup.transform, ghostAI);
                BoxCollider exitZone = CreateTriggerZone(
                    roundSetup.transform,
                    ExitZoneName,
                    playerController.transform.position,
                    ExitZoneSize);
                BoxCollider safeZone = CreateTriggerZone(
                    roundSetup.transform,
                    SafeZoneName,
                    playerController.transform.position,
                    SafeZoneSize);
                CreateGameManager(
                    roundSetup.transform,
                    ghostDefinitions,
                    ghostAI,
                    huntController,
                    sanity,
                    playerController.transform,
                    exitZone);

                ConfigureSafeZone(sanity, safeZone);

                UnityEditor.Undo.CollapseUndoOperations(undoGroup);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                UnityEditor.Selection.activeGameObject = roundSetup;
                Debug.Log(
                    "回合装配已完成：GameManager、HuntController、ExitZone 与 SafeZone 已接线。",
                    roundSetup);
                Debug.Log(
                    "ExitZone 当前位于玩家位置，仅为占位。请手动挪到一楼大门口。",
                    exitZone);
                Debug.Log(
                    "SafeZone 当前位于玩家位置，仅为占位。请手动挪到一楼 Lobby 并调整大小。",
                    safeZone);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                UnityEditor.Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogError("回合装配失败，已撤销本次创建和接线。", playerController);
            }
        }

        private static Residuum.Ghost.GhostDefinition[] LoadGhostDefinitions()
        {
            string[] definitionGuids = UnityEditor.AssetDatabase.FindAssets(
                "t:GhostDefinition",
                new[] { GhostAssetFolder });
            Residuum.Ghost.GhostDefinition[] definitions =
                new Residuum.Ghost.GhostDefinition[GhostDefinitionCount];

            for (int guidIndex = 0; guidIndex < definitionGuids.Length; guidIndex++)
            {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(definitionGuids[guidIndex]);
                int definitionIndex = GetDefinitionIndex(assetPath);
                if (definitionIndex < 0)
                {
                    continue;
                }

                definitions[definitionIndex] =
                    UnityEditor.AssetDatabase.LoadAssetAtPath<Residuum.Ghost.GhostDefinition>(assetPath);
            }

            if (definitions[0] == null || definitions[1] == null || definitions[2] == null)
            {
                Debug.LogError(
                    "找不到完整的三种鬼定义资产（GhostDef_Spirit、GhostDef_Wraith、GhostDef_Poltergeist）。" +
                    "请先执行 Residuum/创建三种鬼资产，未创建回合装配。");
                return null;
            }

            return definitions;
        }

        private static int GetDefinitionIndex(string assetPath)
        {
            if (assetPath == SpiritAssetPath)
            {
                return 0;
            }

            if (assetPath == WraithAssetPath)
            {
                return 1;
            }

            if (assetPath == PoltergeistAssetPath)
            {
                return 2;
            }

            return -1;
        }

        private static Residuum.Player.PlayerSanity EnsurePlayerSanity(
            Residuum.Player.PlayerController playerController)
        {
            Residuum.Player.PlayerSanity sanity =
                playerController.GetComponent<Residuum.Player.PlayerSanity>();
            if (sanity != null)
            {
                return sanity;
            }

            sanity = UnityEditor.Undo.AddComponent<Residuum.Player.PlayerSanity>(
                playerController.gameObject);
            if (sanity == null)
            {
                throw new InvalidOperationException("无法为 PlayerController 所在物体添加 PlayerSanity。");
            }

            return sanity;
        }

        private static Residuum.Ghost.HuntController CreateHuntController(
            Transform roundSetupTransform,
            Residuum.Ghost.GhostAI ghostAI)
        {
            GameObject huntControllerObject = new GameObject(HuntControllerName);
            UnityEditor.Undo.RegisterCreatedObjectUndo(huntControllerObject, UndoLabel);
            UnityEditor.Undo.SetTransformParent(
                huntControllerObject.transform,
                roundSetupTransform,
                UndoLabel);

            Residuum.Ghost.HuntController huntController =
                UnityEditor.Undo.AddComponent<Residuum.Ghost.HuntController>(huntControllerObject);
            if (huntController == null)
            {
                throw new InvalidOperationException("无法为 RoundSetup/HuntController 添加 HuntController。");
            }

            UnityEditor.Undo.RecordObject(huntController, UndoLabel);
            UnityEditor.SerializedObject serializedHuntController =
                new UnityEditor.SerializedObject(huntController);
            serializedHuntController.Update();

            UnityEditor.SerializedProperty ghostAIProperty =
                serializedHuntController.FindProperty("_ghostAI");
            if (ghostAIProperty == null)
            {
                throw new InvalidOperationException(
                    "HuntController 缺少 _ghostAI 序列化字段，无法完成回合装配接线。");
            }

            ghostAIProperty.objectReferenceValue = ghostAI;
            serializedHuntController.ApplyModifiedProperties();
            return huntController;
        }

        private static BoxCollider CreateTriggerZone(
            Transform roundSetupTransform,
            string zoneName,
            Vector3 playerPosition,
            Vector3 zoneSize)
        {
            GameObject zoneObject = new GameObject(zoneName);
            UnityEditor.Undo.RegisterCreatedObjectUndo(zoneObject, UndoLabel);
            UnityEditor.Undo.SetTransformParent(zoneObject.transform, roundSetupTransform, UndoLabel);
            zoneObject.transform.position = playerPosition;

            BoxCollider zoneCollider = UnityEditor.Undo.AddComponent<BoxCollider>(zoneObject);
            if (zoneCollider == null)
            {
                throw new InvalidOperationException($"无法为 RoundSetup/{zoneName} 添加 BoxCollider。");
            }

            UnityEditor.Undo.RecordObject(zoneCollider, UndoLabel);
            zoneCollider.isTrigger = true;
            zoneCollider.size = zoneSize;
            return zoneCollider;
        }

        private static Residuum.Core.GameManager CreateGameManager(
            Transform roundSetupTransform,
            Residuum.Ghost.GhostDefinition[] ghostDefinitions,
            Residuum.Ghost.GhostAI ghostAI,
            Residuum.Ghost.HuntController huntController,
            Residuum.Player.PlayerSanity sanity,
            Transform playerTransform,
            Collider exitZone)
        {
            GameObject gameManagerObject = new GameObject(GameManagerName);
            UnityEditor.Undo.RegisterCreatedObjectUndo(gameManagerObject, UndoLabel);
            UnityEditor.Undo.SetTransformParent(
                gameManagerObject.transform,
                roundSetupTransform,
                UndoLabel);

            Residuum.Core.GameManager gameManager =
                UnityEditor.Undo.AddComponent<Residuum.Core.GameManager>(gameManagerObject);
            if (gameManager == null)
            {
                throw new InvalidOperationException("无法为 RoundSetup/GameManager 添加 GameManager。");
            }

            UnityEditor.Undo.RecordObject(gameManager, UndoLabel);
            UnityEditor.SerializedObject serializedGameManager =
                new UnityEditor.SerializedObject(gameManager);
            serializedGameManager.Update();

            UnityEditor.SerializedProperty allGhostsProperty =
                serializedGameManager.FindProperty("_allGhosts");
            UnityEditor.SerializedProperty ghostAIProperty =
                serializedGameManager.FindProperty("_ghostAI");
            UnityEditor.SerializedProperty huntControllerProperty =
                serializedGameManager.FindProperty("_huntController");
            UnityEditor.SerializedProperty sanityProperty =
                serializedGameManager.FindProperty("_sanity");
            UnityEditor.SerializedProperty playerProperty =
                serializedGameManager.FindProperty("_player");
            UnityEditor.SerializedProperty exitZoneProperty =
                serializedGameManager.FindProperty("_exitZone");
            if (allGhostsProperty == null || ghostAIProperty == null || huntControllerProperty == null
                || sanityProperty == null || playerProperty == null || exitZoneProperty == null)
            {
                throw new InvalidOperationException(
                    "GameManager 缺少 _allGhosts、_ghostAI、_huntController、_sanity、_player 或 _exitZone " +
                    "序列化字段，无法完成回合装配接线。");
            }

            if (!allGhostsProperty.isArray)
            {
                throw new InvalidOperationException(
                    "GameManager 的 _allGhosts 序列化字段不是数组，无法完成回合装配接线。");
            }

            allGhostsProperty.arraySize = ghostDefinitions.Length;
            for (int ghostIndex = 0; ghostIndex < ghostDefinitions.Length; ghostIndex++)
            {
                UnityEditor.SerializedProperty ghostDefinitionProperty =
                    allGhostsProperty.GetArrayElementAtIndex(ghostIndex);
                if (ghostDefinitionProperty == null)
                {
                    throw new InvalidOperationException(
                        "GameManager 的 _allGhosts 数组元素不可访问，无法完成回合装配接线。");
                }

                ghostDefinitionProperty.objectReferenceValue = ghostDefinitions[ghostIndex];
            }

            ghostAIProperty.objectReferenceValue = ghostAI;
            huntControllerProperty.objectReferenceValue = huntController;
            sanityProperty.objectReferenceValue = sanity;
            playerProperty.objectReferenceValue = playerTransform;
            exitZoneProperty.objectReferenceValue = exitZone;
            serializedGameManager.ApplyModifiedProperties();
            return gameManager;
        }

        private static void ConfigureSafeZone(Residuum.Player.PlayerSanity sanity, Collider safeZone)
        {
            UnityEditor.Undo.RecordObject(sanity, UndoLabel);
            UnityEditor.SerializedObject serializedSanity = new UnityEditor.SerializedObject(sanity);
            serializedSanity.Update();

            UnityEditor.SerializedProperty safeZoneProperty =
                serializedSanity.FindProperty("_safeZone");
            if (safeZoneProperty == null)
            {
                throw new InvalidOperationException(
                    "PlayerSanity 缺少 _safeZone 序列化字段，无法完成安全区接线。");
            }

            safeZoneProperty.objectReferenceValue = safeZone;
            serializedSanity.ApplyModifiedProperties();
        }

        private static GameObject FindExistingRoundSetup()
        {
            Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(
                UnityEngine.FindObjectsInactive.Include);
            for (int transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
            {
                Transform candidate = transforms[transformIndex];
                if (candidate != null && candidate.name == RoundSetupName)
                {
                    return candidate.gameObject;
                }
            }

            return null;
        }
    }
}

using System;
using UnityEngine;

namespace Residuum.Player.Editor
{
    /// <summary>
    /// 在当前打开的场景中搭建可直接进入播放模式测试的玩家装配。
    /// </summary>
    public static class PlayerRigBuilder
    {
        private const string MenuPath = "Residuum/搭建玩家装配";
        private const string InputActionsAssetPath = "Assets/InputSystem_Actions.inputactions";
        private const string UndoLabel = "搭建玩家装配";
        private const string GroundName = "Ground";
        private const string PlayerName = "Player";
        private const string CameraPivotName = "CameraPivot";
        private const string MainCameraName = "Main Camera";

        private const float GroundScale = 2f;
        private const float PlayerGroundOffset = 0.1f;
        private const float StandingHeight = 1.8f;
        private const float StandingCenterY = 0.9f;
        private const float ControllerRadius = 0.3f;
        private const float CameraPivotHeight = 1.6f;

        [UnityEditor.MenuItem(MenuPath)]
        private static void BuildPlayerRig()
        {
            // 用 FindAnyObjectByType 而非 FindFirstObjectByType：后者在 Unity 6000.5 已弃用
            // （它依赖 instance ID 排序）。这里只是判断"场景里有没有玩家"，不关心是哪一个。
            PlayerController existingPlayer =
                UnityEngine.Object.FindAnyObjectByType<PlayerController>(
                    UnityEngine.FindObjectsInactive.Include);
            if (existingPlayer != null)
            {
                UnityEditor.Selection.activeGameObject = existingPlayer.gameObject;
                Debug.Log("场景中已经存在 PlayerController，未重复创建玩家装配。", existingPlayer);
                return;
            }

            UnityEngine.InputSystem.InputActionAsset inputActions =
                UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
                    InputActionsAssetPath);
            if (inputActions == null)
            {
                Debug.LogError(
                    $"找不到工程现有的 Input Action Asset：{InputActionsAssetPath}。未创建玩家装配。");
                return;
            }

            int undoGroup = UnityEditor.Undo.GetCurrentGroup();
            UnityEditor.Undo.SetCurrentGroupName(UndoLabel);

            try
            {
                EnsureGround();

                GameObject player = new GameObject(PlayerName);
                UnityEditor.Undo.RegisterCreatedObjectUndo(player, UndoLabel);
                player.transform.position = new Vector3(0f, PlayerGroundOffset, 0f);

                CharacterController characterController =
                    UnityEditor.Undo.AddComponent<CharacterController>(player);
                if (characterController == null)
                {
                    throw new InvalidOperationException("无法为 Player 添加 CharacterController。");
                }

                characterController.height = StandingHeight;
                characterController.center = new Vector3(0f, StandingCenterY, 0f);
                characterController.radius = ControllerRadius;

                PlayerController playerController =
                    UnityEditor.Undo.AddComponent<PlayerController>(player);
                if (playerController == null)
                {
                    throw new InvalidOperationException("无法为 Player 添加 PlayerController。");
                }

                GameObject cameraPivot = new GameObject(CameraPivotName);
                UnityEditor.Undo.RegisterCreatedObjectUndo(cameraPivot, UndoLabel);
                UnityEditor.Undo.SetTransformParent(cameraPivot.transform, player.transform, UndoLabel);
                cameraPivot.transform.localPosition = new Vector3(0f, CameraPivotHeight, 0f);
                cameraPivot.transform.localRotation = Quaternion.identity;

                Camera sceneCamera = FindSceneCamera();
                if (sceneCamera == null)
                {
                    GameObject cameraObject = new GameObject(MainCameraName);
                    UnityEditor.Undo.RegisterCreatedObjectUndo(cameraObject, UndoLabel);
                    cameraObject.tag = "MainCamera";
                    sceneCamera = UnityEditor.Undo.AddComponent<Camera>(cameraObject);
                    if (sceneCamera == null)
                    {
                        throw new InvalidOperationException("无法创建 Main Camera。");
                    }

                    AudioListener audioListener =
                        UnityEditor.Undo.AddComponent<AudioListener>(cameraObject);
                    if (audioListener == null)
                    {
                        throw new InvalidOperationException("无法为 Main Camera 添加 AudioListener。");
                    }
                }

                UnityEditor.Undo.SetTransformParent(sceneCamera.transform, cameraPivot.transform, UndoLabel);
                UnityEditor.Undo.RecordObject(sceneCamera.transform, UndoLabel);
                sceneCamera.transform.localPosition = Vector3.zero;
                sceneCamera.transform.localRotation = Quaternion.identity;

                UnityEditor.SerializedObject serializedPlayerController =
                    new UnityEditor.SerializedObject(playerController);
                serializedPlayerController.Update();

                UnityEditor.SerializedProperty inputActionsProperty =
                    serializedPlayerController.FindProperty("_inputActions");
                UnityEditor.SerializedProperty cameraPivotProperty =
                    serializedPlayerController.FindProperty("_cameraPivot");
                if (inputActionsProperty == null || cameraPivotProperty == null)
                {
                    throw new InvalidOperationException(
                        "PlayerController 缺少 _inputActions 或 _cameraPivot 序列化字段。");
                }

                inputActionsProperty.objectReferenceValue = inputActions;
                cameraPivotProperty.objectReferenceValue = cameraPivot.transform;
                serializedPlayerController.ApplyModifiedProperties();

                UnityEditor.Undo.CollapseUndoOperations(undoGroup);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                UnityEditor.Selection.activeGameObject = player;
                Debug.Log(
                    "玩家装配已完成：Player、CameraPivot 和 Main Camera 已创建并完成引用连接。",
                    player);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                UnityEditor.Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogError("玩家装配失败，已撤销本次创建的半成品。");
            }
        }

        private static void EnsureGround()
        {
            Collider[] colliders = UnityEngine.Object.FindObjectsByType<Collider>(
                UnityEngine.FindObjectsInactive.Include);
            for (int colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
            {
                Collider collider = colliders[colliderIndex];
                if (collider != null && collider.enabled && !collider.isTrigger)
                {
                    return;
                }
            }

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            UnityEditor.Undo.RegisterCreatedObjectUndo(ground, UndoLabel);
            ground.name = GroundName;
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(GroundScale, 1f, GroundScale);
        }

        private static Camera FindSceneCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                return mainCamera;
            }

            // 不按名字找相机：GameObject.Find 只能找到激活对象、且对改名脆弱，
            // 而下面这个按类型找的兜底本来就覆盖了它能找到的全部情况。
            return UnityEngine.Object.FindAnyObjectByType<Camera>(
                UnityEngine.FindObjectsInactive.Include);
        }
    }
}

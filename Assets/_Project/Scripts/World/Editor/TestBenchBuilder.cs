using System;
using UnityEngine;

namespace Residuum.World.Editor
{
    /// <summary>
    /// 在当前打开的场景中搭建门和手电筒的最小手工验收测试台。
    /// </summary>
    public static class TestBenchBuilder
    {
        private const string MenuPath = "Residuum/搭建测试台";
        private const string InputActionsAssetPath = "Assets/InputSystem_Actions.inputactions";
        private const string UndoLabel = "搭建测试台";
        private const string TestBenchName = "TestBench";
        private const string TestDoorName = "TestDoor";
        private const string FlashlightName = "Flashlight";
        private const string SpotName = "Spot";
        private const string HandAnchorName = "HandAnchor";

        private const int SlotCount = 3;
        private const int FlashlightSlotIndex = 0;
        private const float DoorThickness = 0.1f;
        private const float DoorHeight = 2.1f;
        private const float DoorWidth = 1f;
        private const float DoorCenterY = 1.05f;
        private const float DoorForwardOffset = 4f;
        private const float SpotRange = 12f;
        private const float SpotOuterAngle = 45f;
        private const float SpotInnerAngle = 30f;

        [UnityEditor.MenuItem(MenuPath)]
        private static void BuildTestBench()
        {
            GameObject existingTestBench = FindExistingTestBench();
            if (existingTestBench != null)
            {
                UnityEditor.Selection.activeGameObject = existingTestBench;
                Debug.Log("场景中已经存在 TestBench，未重复创建测试台，已选中它。", existingTestBench);
                return;
            }

            Residuum.Player.PlayerController playerController =
                UnityEngine.Object.FindAnyObjectByType<Residuum.Player.PlayerController>(
                    UnityEngine.FindObjectsInactive.Include);
            if (playerController == null)
            {
                Debug.LogError("场景中没有 PlayerController。请先执行 Residuum/搭建玩家装配，再搭建测试台。");
                return;
            }

            UnityEngine.InputSystem.InputActionAsset inputActions =
                UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
                    InputActionsAssetPath);
            if (inputActions == null)
            {
                Debug.LogError(
                    $"找不到工程现有的 Input Action Asset：{InputActionsAssetPath}。未创建测试台。");
                return;
            }

            int undoGroup = UnityEditor.Undo.GetCurrentGroup();
            UnityEditor.Undo.SetCurrentGroupName(UndoLabel);

            try
            {
                Camera playerCamera = FindPlayerCamera(playerController);
                if (playerCamera == null)
                {
                    throw new InvalidOperationException(
                        "PlayerController 所在物体下找不到玩家相机，无法创建 HandAnchor。");
                }

                GameObject testBench = new GameObject(TestBenchName);
                UnityEditor.Undo.RegisterCreatedObjectUndo(testBench, UndoLabel);

                CreateTestDoor(testBench.transform);
                Residuum.Items.Flashlight flashlight = CreateFlashlight(testBench.transform, inputActions);
                Transform handAnchor = EnsureHandAnchor(playerCamera.transform);
                Residuum.Items.ItemSlotSystem itemSlotSystem = EnsureItemSlotSystem(playerController.gameObject);
                ConfigureItemSlotSystem(itemSlotSystem, flashlight, handAnchor, inputActions);

                UnityEditor.Undo.CollapseUndoOperations(undoGroup);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                UnityEditor.Selection.activeGameObject = testBench;
                Debug.Log("测试台已完成：TestDoor、Flashlight、HandAnchor 与道具槽均已连接。", testBench);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                UnityEditor.Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogError("测试台搭建失败，已撤销本次创建和接线。", playerController);
            }
        }

        private static void CreateTestDoor(Transform testBenchTransform)
        {
            GameObject doorObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEditor.Undo.RegisterCreatedObjectUndo(doorObject, UndoLabel);
            doorObject.name = TestDoorName;
            UnityEditor.Undo.SetTransformParent(doorObject.transform, testBenchTransform, UndoLabel);
            doorObject.transform.localPosition = new Vector3(0f, DoorCenterY, DoorForwardOffset);
            doorObject.transform.localRotation = Quaternion.identity;
            doorObject.transform.localScale = new Vector3(DoorThickness, DoorHeight, DoorWidth);

            BoxCollider boxCollider = doorObject.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                throw new InvalidOperationException("无法获取 TestDoor 自带的 BoxCollider。");
            }

            UnityEditor.Undo.RecordObject(boxCollider, UndoLabel);
            boxCollider.isTrigger = false;

            Residuum.World.Door door = UnityEditor.Undo.AddComponent<Residuum.World.Door>(doorObject);
            if (door == null)
            {
                throw new InvalidOperationException("无法为 TestDoor 添加 Door 组件。");
            }
        }

        private static Residuum.Items.Flashlight CreateFlashlight(
            Transform testBenchTransform,
            UnityEngine.InputSystem.InputActionAsset inputActions)
        {
            GameObject flashlightObject = new GameObject(FlashlightName);
            UnityEditor.Undo.RegisterCreatedObjectUndo(flashlightObject, UndoLabel);
            UnityEditor.Undo.SetTransformParent(flashlightObject.transform, testBenchTransform, UndoLabel);
            flashlightObject.transform.localPosition = Vector3.zero;
            flashlightObject.transform.localRotation = Quaternion.identity;

            GameObject spotObject = new GameObject(SpotName);
            UnityEditor.Undo.RegisterCreatedObjectUndo(spotObject, UndoLabel);
            UnityEditor.Undo.SetTransformParent(spotObject.transform, flashlightObject.transform, UndoLabel);
            spotObject.transform.localPosition = Vector3.zero;
            spotObject.transform.localRotation = Quaternion.identity;

            Light spotLight = UnityEditor.Undo.AddComponent<Light>(spotObject);
            if (spotLight == null)
            {
                throw new InvalidOperationException("无法为 Flashlight/Spot 添加 Light 组件。");
            }

            UnityEditor.Undo.RecordObject(spotLight, UndoLabel);
            spotLight.type = LightType.Spot;
            spotLight.range = SpotRange;
            spotLight.spotAngle = SpotOuterAngle;
            spotLight.innerSpotAngle = SpotInnerAngle;

            Residuum.Items.Flashlight flashlight =
                UnityEditor.Undo.AddComponent<Residuum.Items.Flashlight>(flashlightObject);
            if (flashlight == null)
            {
                throw new InvalidOperationException("无法为 Flashlight 添加 Flashlight 组件。");
            }

            UnityEditor.Undo.RecordObject(flashlight, UndoLabel);
            UnityEditor.SerializedObject serializedFlashlight = new UnityEditor.SerializedObject(flashlight);
            serializedFlashlight.Update();

            UnityEditor.SerializedProperty spotLightProperty =
                serializedFlashlight.FindProperty("_spotLight");
            UnityEditor.SerializedProperty inputActionsProperty =
                serializedFlashlight.FindProperty("_inputActions");
            if (spotLightProperty == null || inputActionsProperty == null)
            {
                throw new InvalidOperationException(
                    "Flashlight 缺少 _spotLight 或 _inputActions 序列化字段，无法完成测试台接线。");
            }

            spotLightProperty.objectReferenceValue = spotLight;
            inputActionsProperty.objectReferenceValue = inputActions;
            serializedFlashlight.ApplyModifiedProperties();
            return flashlight;
        }

        private static Transform EnsureHandAnchor(Transform playerCameraTransform)
        {
            Transform handAnchor = playerCameraTransform.Find(HandAnchorName);
            if (handAnchor == null)
            {
                GameObject handAnchorObject = new GameObject(HandAnchorName);
                UnityEditor.Undo.RegisterCreatedObjectUndo(handAnchorObject, UndoLabel);
                UnityEditor.Undo.SetTransformParent(
                    handAnchorObject.transform,
                    playerCameraTransform,
                    UndoLabel);
                handAnchor = handAnchorObject.transform;
            }

            UnityEditor.Undo.RecordObject(handAnchor, UndoLabel);
            handAnchor.localPosition = new Vector3(0.25f, -0.2f, 0.4f);
            handAnchor.localRotation = Quaternion.identity;
            return handAnchor;
        }

        private static Residuum.Items.ItemSlotSystem EnsureItemSlotSystem(GameObject playerObject)
        {
            Residuum.Items.ItemSlotSystem itemSlotSystem =
                playerObject.GetComponent<Residuum.Items.ItemSlotSystem>();
            if (itemSlotSystem != null)
            {
                return itemSlotSystem;
            }

            itemSlotSystem = UnityEditor.Undo.AddComponent<Residuum.Items.ItemSlotSystem>(playerObject);
            if (itemSlotSystem == null)
            {
                throw new InvalidOperationException("无法为 PlayerController 所在物体添加 ItemSlotSystem。");
            }

            return itemSlotSystem;
        }

        private static void ConfigureItemSlotSystem(
            Residuum.Items.ItemSlotSystem itemSlotSystem,
            Residuum.Items.Flashlight flashlight,
            Transform handAnchor,
            UnityEngine.InputSystem.InputActionAsset inputActions)
        {
            UnityEditor.Undo.RecordObject(itemSlotSystem, UndoLabel);
            UnityEditor.SerializedObject serializedItemSlotSystem =
                new UnityEditor.SerializedObject(itemSlotSystem);
            serializedItemSlotSystem.Update();

            UnityEditor.SerializedProperty slotsProperty =
                serializedItemSlotSystem.FindProperty("_slots");
            UnityEditor.SerializedProperty heldModelsProperty =
                serializedItemSlotSystem.FindProperty("_heldModels");
            UnityEditor.SerializedProperty handAnchorProperty =
                serializedItemSlotSystem.FindProperty("_handAnchor");
            UnityEditor.SerializedProperty inputActionsProperty =
                serializedItemSlotSystem.FindProperty("_inputActions");
            if (slotsProperty == null || heldModelsProperty == null || handAnchorProperty == null
                || inputActionsProperty == null)
            {
                throw new InvalidOperationException(
                    "ItemSlotSystem 缺少 _slots、_heldModels、_handAnchor 或 _inputActions 序列化字段，无法完成测试台接线。");
            }

            if (!slotsProperty.isArray || !heldModelsProperty.isArray)
            {
                throw new InvalidOperationException(
                    "ItemSlotSystem 的 _slots 或 _heldModels 序列化字段不是数组，无法完成测试台接线。");
            }

            slotsProperty.arraySize = SlotCount;
            heldModelsProperty.arraySize = SlotCount;
            for (int slotIndex = 0; slotIndex < SlotCount; slotIndex++)
            {
                bool isFlashlightSlot = slotIndex == FlashlightSlotIndex;
                slotsProperty.GetArrayElementAtIndex(slotIndex).objectReferenceValue =
                    isFlashlightSlot ? flashlight : null;
                heldModelsProperty.GetArrayElementAtIndex(slotIndex).objectReferenceValue =
                    isFlashlightSlot ? flashlight.gameObject : null;
            }

            handAnchorProperty.objectReferenceValue = handAnchor;
            inputActionsProperty.objectReferenceValue = inputActions;
            serializedItemSlotSystem.ApplyModifiedProperties();
        }

        private static Camera FindPlayerCamera(Residuum.Player.PlayerController playerController)
        {
            return playerController.GetComponentInChildren<Camera>(true);
        }

        private static GameObject FindExistingTestBench()
        {
            Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(
                UnityEngine.FindObjectsInactive.Include);
            for (int transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
            {
                Transform candidate = transforms[transformIndex];
                if (candidate != null && candidate.name == TestBenchName)
                {
                    return candidate.gameObject;
                }
            }

            return null;
        }
    }
}

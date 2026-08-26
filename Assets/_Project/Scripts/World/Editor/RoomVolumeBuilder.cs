using System;
using UnityEngine;

namespace Residuum.World.Editor
{
    /// <summary>
    /// 根据 Hierarchy 中明确选中的房间物体，快速创建贴合地面的 RoomVolume。
    /// </summary>
    public static class RoomVolumeBuilder
    {
        private const string CreateRoomVolumeMenuPath = "Residuum/为选中对象创建 RoomVolume";
        private const string CheckOverlapMenuPath = "Residuum/检查 RoomVolume 重叠";
        private const string CreateRoomManagerMenuPath = "Residuum/创建 RoomManager";
        private const string CreateRoomVolumeUndoLabel = "创建 RoomVolume";
        private const string CreateRoomManagerUndoLabel = "创建 RoomManager";
        private const string RoomVolumeObjectNamePrefix = "BLK_Room_";
        private const string RoomManagerObjectName = "RoomManager";

        // 内缩可避免随机落点越过墙体进入相邻房间。
        private const float HorizontalInset = 0.3f;
        private const float RoomHeight = 2.5f;
        private const float MinimumInsetSize = 0.5f;

        [UnityEditor.MenuItem(CreateRoomVolumeMenuPath)]
        private static void CreateRoomVolumeForSelection()
        {
            GameObject[] selectedObjects = GetSelectedSceneObjects();
            if (selectedObjects.Length == 0)
            {
                Debug.LogError("请先在 Hierarchy 中选中至少一个场景物体，再创建 RoomVolume。未创建任何物体。");
                return;
            }

            if (!TryCalculateCombinedBounds(selectedObjects, out Bounds combinedBounds))
            {
                Debug.LogError(
                    "选中物体及其子物体中没有 Renderer 或 Collider，无法计算 RoomVolume 尺寸。未创建任何物体。");
                return;
            }

            int undoGroup = UnityEditor.Undo.GetCurrentGroup();
            UnityEditor.Undo.SetCurrentGroupName(CreateRoomVolumeUndoLabel);

            try
            {
                string roomName = selectedObjects[0].name;
                GameObject roomVolumeObject = new GameObject(RoomVolumeObjectNamePrefix + roomName);
                UnityEditor.Undo.RegisterCreatedObjectUndo(roomVolumeObject, CreateRoomVolumeUndoLabel);

                Vector3 roomPosition = combinedBounds.center;
                roomPosition.y = combinedBounds.min.y + RoomHeight * 0.5f;
                roomVolumeObject.transform.position = roomPosition;

                BoxCollider boxCollider = UnityEditor.Undo.AddComponent<BoxCollider>(roomVolumeObject);
                if (boxCollider == null)
                {
                    throw new InvalidOperationException("无法为新建的 RoomVolume 添加 BoxCollider。");
                }

                UnityEditor.Undo.RecordObject(boxCollider, CreateRoomVolumeUndoLabel);
                boxCollider.isTrigger = true;
                boxCollider.size = GetRoomColliderSize(combinedBounds, roomName);

                RoomVolume roomVolume = UnityEditor.Undo.AddComponent<RoomVolume>(roomVolumeObject);
                if (roomVolume == null)
                {
                    throw new InvalidOperationException("无法为新建物体添加 RoomVolume 组件。");
                }

                ConfigureRoomVolume(roomVolume, roomName);

                UnityEditor.Undo.CollapseUndoOperations(undoGroup);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                UnityEditor.Selection.activeGameObject = roomVolumeObject;

                if (selectedObjects.Length > 1)
                {
                    Debug.Log(
                        $"已将 {selectedObjects.Length} 个选中物体合并为一个 RoomVolume：{roomVolumeObject.name}。",
                        roomVolumeObject);
                }
                else
                {
                    Debug.Log($"已创建 RoomVolume：{roomVolumeObject.name}。", roomVolumeObject);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                UnityEditor.Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogError("创建 RoomVolume 失败，已撤销本次创建。", selectedObjects[0]);
            }
        }

        [UnityEditor.MenuItem(CheckOverlapMenuPath)]
        private static void CheckRoomVolumeOverlaps()
        {
            RoomVolume[] roomVolumes = UnityEngine.Object.FindObjectsByType<RoomVolume>(
                UnityEngine.FindObjectsInactive.Include);
            System.Collections.Generic.List<string> overlaps =
                new System.Collections.Generic.List<string>();
            System.Collections.Generic.List<string> missingColliders =
                new System.Collections.Generic.List<string>();

            for (int firstIndex = 0; firstIndex < roomVolumes.Length; firstIndex++)
            {
                RoomVolume firstRoom = roomVolumes[firstIndex];
                if (!TryGetRoomCollider(firstRoom, out BoxCollider firstCollider))
                {
                    missingColliders.Add(GetRoomDisplayName(firstRoom));
                    continue;
                }

                for (int secondIndex = firstIndex + 1;
                     secondIndex < roomVolumes.Length;
                     secondIndex++)
                {
                    RoomVolume secondRoom = roomVolumes[secondIndex];
                    if (!TryGetRoomCollider(secondRoom, out BoxCollider secondCollider))
                    {
                        string roomName = GetRoomDisplayName(secondRoom);
                        if (!missingColliders.Contains(roomName))
                        {
                            missingColliders.Add(roomName);
                        }

                        continue;
                    }

                    Bounds firstBounds = firstCollider.bounds;
                    Bounds secondBounds = secondCollider.bounds;
                    if (!firstBounds.Intersects(secondBounds))
                    {
                        continue;
                    }

                    float overlapVolume = CalculateOverlapVolume(firstBounds, secondBounds);
                    if (overlapVolume <= 0f)
                    {
                        continue;
                    }

                    overlaps.Add(
                        $"{GetRoomDisplayName(firstRoom)} ↔ {GetRoomDisplayName(secondRoom)}：{overlapVolume:F3} 立方米");
                }
            }

            if (overlaps.Count == 0)
            {
                Debug.Log("未发现 RoomVolume 重叠。");
            }
            else
            {
                Debug.LogWarning("发现 RoomVolume 重叠：\n" + string.Join("\n", overlaps));
            }

            if (missingColliders.Count > 0)
            {
                Debug.LogWarning(
                    "以下 RoomVolume 缺少 BoxCollider，已跳过重叠检查："
                    + string.Join("、", missingColliders));
            }
        }

        [UnityEditor.MenuItem(CreateRoomManagerMenuPath)]
        private static void CreateRoomManager()
        {
            RoomManager existingRoomManager = UnityEngine.Object.FindAnyObjectByType<RoomManager>(
                UnityEngine.FindObjectsInactive.Include);
            if (existingRoomManager != null)
            {
                UnityEditor.Selection.activeGameObject = existingRoomManager.gameObject;
                Debug.Log("场景中已经存在 RoomManager，未重复创建，已选中它。", existingRoomManager);
                return;
            }

            int undoGroup = UnityEditor.Undo.GetCurrentGroup();
            UnityEditor.Undo.SetCurrentGroupName(CreateRoomManagerUndoLabel);

            try
            {
                GameObject roomManagerObject = new GameObject(RoomManagerObjectName);
                UnityEditor.Undo.RegisterCreatedObjectUndo(roomManagerObject, CreateRoomManagerUndoLabel);

                RoomManager roomManager = UnityEditor.Undo.AddComponent<RoomManager>(roomManagerObject);
                if (roomManager == null)
                {
                    throw new InvalidOperationException("无法为新建物体添加 RoomManager 组件。");
                }

                UnityEditor.Undo.CollapseUndoOperations(undoGroup);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                UnityEditor.Selection.activeGameObject = roomManagerObject;
                Debug.Log("已创建 RoomManager。", roomManagerObject);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                UnityEditor.Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogError("创建 RoomManager 失败，已撤销本次创建。");
            }
        }

        private static GameObject[] GetSelectedSceneObjects()
        {
            System.Collections.Generic.List<GameObject> sceneObjects =
                new System.Collections.Generic.List<GameObject>();
            foreach (GameObject selectedObject in UnityEditor.Selection.gameObjects)
            {
                if (selectedObject != null && selectedObject.scene.IsValid())
                {
                    sceneObjects.Add(selectedObject);
                }
            }

            return sceneObjects.ToArray();
        }

        private static bool TryCalculateCombinedBounds(GameObject[] selectedObjects, out Bounds combinedBounds)
        {
            combinedBounds = new Bounds();
            if (TryEncapsulateRendererBounds(selectedObjects, ref combinedBounds))
            {
                return true;
            }

            return TryEncapsulateColliderBounds(selectedObjects, ref combinedBounds);
        }

        private static bool TryEncapsulateRendererBounds(GameObject[] selectedObjects, ref Bounds combinedBounds)
        {
            bool hasBounds = false;
            foreach (GameObject selectedObject in selectedObjects)
            {
                Renderer[] renderers = selectedObject.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer renderer in renderers)
                {
                    if (renderer == null)
                    {
                        continue;
                    }

                    EncapsulateBounds(ref combinedBounds, renderer.bounds, ref hasBounds);
                }
            }

            return hasBounds;
        }

        private static bool TryEncapsulateColliderBounds(GameObject[] selectedObjects, ref Bounds combinedBounds)
        {
            bool hasBounds = false;
            foreach (GameObject selectedObject in selectedObjects)
            {
                Collider[] colliders = selectedObject.GetComponentsInChildren<Collider>(true);
                foreach (Collider collider in colliders)
                {
                    if (collider == null)
                    {
                        continue;
                    }

                    EncapsulateBounds(ref combinedBounds, collider.bounds, ref hasBounds);
                }
            }

            return hasBounds;
        }

        private static void EncapsulateBounds(ref Bounds combinedBounds, Bounds bounds, ref bool hasBounds)
        {
            if (!hasBounds)
            {
                combinedBounds = bounds;
                hasBounds = true;
                return;
            }

            combinedBounds.Encapsulate(bounds);
        }

        private static Vector3 GetRoomColliderSize(Bounds combinedBounds, string roomName)
        {
            float insetWidth = combinedBounds.size.x - HorizontalInset;
            float insetDepth = combinedBounds.size.z - HorizontalInset;
            if (insetWidth < MinimumInsetSize || insetDepth < MinimumInsetSize)
            {
                Debug.LogWarning(
                    $"房间“{roomName}”在内缩后任一边小于 {MinimumInsetSize:F1} 米，已改用原始 X/Z 尺寸。\n"
                    + "请手动确认该 RoomVolume 不会伸出墙外。");
                insetWidth = combinedBounds.size.x;
                insetDepth = combinedBounds.size.z;
            }

            return new Vector3(insetWidth, RoomHeight, insetDepth);
        }

        private static void ConfigureRoomVolume(RoomVolume roomVolume, string roomName)
        {
            UnityEditor.Undo.RecordObject(roomVolume, CreateRoomVolumeUndoLabel);
            UnityEditor.SerializedObject serializedRoomVolume = new UnityEditor.SerializedObject(roomVolume);
            serializedRoomVolume.Update();

            UnityEditor.SerializedProperty roomNameProperty =
                serializedRoomVolume.FindProperty("_roomName");
            UnityEditor.SerializedProperty roomIdProperty =
                serializedRoomVolume.FindProperty("_roomId");
            UnityEditor.SerializedProperty canBeGhostRoomProperty =
                serializedRoomVolume.FindProperty("_canBeGhostRoom");
            if (roomNameProperty == null || roomIdProperty == null || canBeGhostRoomProperty == null)
            {
                throw new InvalidOperationException(
                    "RoomVolume 缺少 _roomName、_roomId 或 _canBeGhostRoom 序列化字段，无法完成创建。");
            }

            roomNameProperty.stringValue = roomName;
            roomIdProperty.stringValue = roomName;
            canBeGhostRoomProperty.boolValue = false;
            serializedRoomVolume.ApplyModifiedProperties();
        }

        private static bool TryGetRoomCollider(RoomVolume roomVolume, out BoxCollider boxCollider)
        {
            boxCollider = roomVolume != null ? roomVolume.GetComponent<BoxCollider>() : null;
            return boxCollider != null;
        }

        private static string GetRoomDisplayName(RoomVolume roomVolume)
        {
            if (roomVolume == null)
            {
                return "已销毁房间";
            }

            return string.IsNullOrEmpty(roomVolume.RoomName)
                ? roomVolume.gameObject.name
                : roomVolume.RoomName;
        }

        private static float CalculateOverlapVolume(Bounds firstBounds, Bounds secondBounds)
        {
            float overlapWidth = Mathf.Max(
                0f,
                Mathf.Min(firstBounds.max.x, secondBounds.max.x) - Mathf.Max(firstBounds.min.x, secondBounds.min.x));
            float overlapHeight = Mathf.Max(
                0f,
                Mathf.Min(firstBounds.max.y, secondBounds.max.y) - Mathf.Max(firstBounds.min.y, secondBounds.min.y));
            float overlapDepth = Mathf.Max(
                0f,
                Mathf.Min(firstBounds.max.z, secondBounds.max.z) - Mathf.Max(firstBounds.min.z, secondBounds.min.z));
            return overlapWidth * overlapHeight * overlapDepth;
        }
    }
}

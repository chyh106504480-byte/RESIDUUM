using System;
using UnityEngine;

namespace Residuum.World.Editor
{
    /// <summary>
    /// 为场景中选中物体的可见网格批量补齐静态 MeshCollider。
    /// </summary>
    public static class ColliderBuilder
    {
        private const string AddCollidersMenuPath = "Residuum/为选中对象批量加碰撞体";
        private const string RemoveCollidersMenuPath = "Residuum/移除选中对象的碰撞体";
        private const string AddCollidersUndoLabel = "批量添加 MeshCollider";
        private const string RemoveCollidersUndoLabel = "批量移除 MeshCollider";
        private const int WarnThreshold = 2000;

        private static readonly string[] ExcludedNameKeywords =
        {
            "Light",
            "Lamp",
            "Particle",
            "FX",
            "Decal",
            "Glass"
        };

        [UnityEditor.MenuItem(AddCollidersMenuPath)]
        private static void AddMeshCollidersForSelection()
        {
            GameObject[] selectedObjects = GetSelectedSceneObjects();
            if (selectedObjects.Length == 0)
            {
                Debug.LogError("请先在 Hierarchy 中选中至少一个场景物体，再批量添加碰撞体。未添加任何碰撞体。");
                return;
            }

            System.Collections.Generic.List<MeshFilter> meshFilters = GetUniqueMeshFilters(selectedObjects);
            if (meshFilters.Count > WarnThreshold)
            {
                Debug.LogWarning(
                    $"本次将检查 {meshFilters.Count} 个带 MeshFilter 的物体，超过 {WarnThreshold} 个。"
                    + "批量添加 MeshCollider 会显著增加场景体积与物理开销，但仍将继续执行。",
                    selectedObjects[0]);
            }

            int undoGroup = UnityEditor.Undo.GetCurrentGroup();
            UnityEditor.Undo.SetCurrentGroupName(AddCollidersUndoLabel);

            try
            {
                int addedCount = 0;
                int skippedExistingColliderCount = 0;
                int skippedMissingMeshCount = 0;
                int skippedKeywordCount = 0;
                int skippedRoomVolumeCount = 0;

                foreach (MeshFilter meshFilter in meshFilters)
                {
                    if (meshFilter == null)
                    {
                        continue;
                    }

                    GameObject targetObject = meshFilter.gameObject;
                    if (meshFilter.sharedMesh == null)
                    {
                        skippedMissingMeshCount++;
                        continue;
                    }

                    if (ContainsExcludedKeyword(targetObject.name))
                    {
                        skippedKeywordCount++;
                        continue;
                    }

                    // 只跳过 RoomVolume 所在的物体本身，子孙墙体与楼板仍需碰撞体来阻挡玩家和鬼的视线。
                    if (targetObject.GetComponent<Residuum.World.RoomVolume>() != null)
                    {
                        skippedRoomVolumeCount++;
                        continue;
                    }

                    if (targetObject.GetComponent<Collider>() != null)
                    {
                        skippedExistingColliderCount++;
                        continue;
                    }

                    MeshCollider meshCollider = UnityEditor.Undo.AddComponent<MeshCollider>(targetObject);
                    if (meshCollider == null)
                    {
                        throw new InvalidOperationException($"无法为物体“{targetObject.name}”添加 MeshCollider。");
                    }

                    UnityEditor.Undo.RecordObject(meshCollider, AddCollidersUndoLabel);
                    meshCollider.convex = false;
                    addedCount++;
                }

                UnityEditor.Undo.CollapseUndoOperations(undoGroup);
                MarkSelectedScenesDirty(selectedObjects);

                int skippedCount = skippedExistingColliderCount
                    + skippedMissingMeshCount
                    + skippedKeywordCount
                    + skippedRoomVolumeCount;
                Debug.Log(
                    $"批量添加碰撞体完成：处理 {meshFilters.Count} 个物体，新增 {addedCount} 个 MeshCollider，"
                    + $"跳过 {skippedCount} 个（已有 Collider：{skippedExistingColliderCount}，"
                    + $"无网格：{skippedMissingMeshCount}，命中关键词：{skippedKeywordCount}，"
                    + $"RoomVolume 物体自身：{skippedRoomVolumeCount}）。",
                    selectedObjects[0]);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                UnityEditor.Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogError("批量添加 MeshCollider 失败，已撤销本次添加。", selectedObjects[0]);
            }
        }

        [UnityEditor.MenuItem(RemoveCollidersMenuPath)]
        private static void RemoveMeshCollidersForSelection()
        {
            GameObject[] selectedObjects = GetSelectedSceneObjects();
            if (selectedObjects.Length == 0)
            {
                Debug.LogError("请先在 Hierarchy 中选中至少一个场景物体，再移除碰撞体。未移除任何碰撞体。");
                return;
            }

            System.Collections.Generic.List<MeshCollider> meshColliders = GetUniqueMeshColliders(selectedObjects);
            int undoGroup = UnityEditor.Undo.GetCurrentGroup();
            UnityEditor.Undo.SetCurrentGroupName(RemoveCollidersUndoLabel);

            try
            {
                int removedCount = 0;
                foreach (MeshCollider meshCollider in meshColliders)
                {
                    if (meshCollider == null)
                    {
                        continue;
                    }

                    UnityEditor.Undo.DestroyObjectImmediate(meshCollider);
                    removedCount++;
                }

                UnityEditor.Undo.CollapseUndoOperations(undoGroup);
                MarkSelectedScenesDirty(selectedObjects);
                Debug.Log($"批量移除碰撞体完成：移除了 {removedCount} 个 MeshCollider。", selectedObjects[0]);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                UnityEditor.Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogError("批量移除 MeshCollider 失败，已撤销本次移除。", selectedObjects[0]);
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

        private static System.Collections.Generic.List<MeshFilter> GetUniqueMeshFilters(GameObject[] selectedObjects)
        {
            System.Collections.Generic.List<MeshFilter> meshFilters =
                new System.Collections.Generic.List<MeshFilter>();
            System.Collections.Generic.HashSet<GameObject> objectIds =
                new System.Collections.Generic.HashSet<GameObject>();

            foreach (GameObject selectedObject in selectedObjects)
            {
                MeshFilter[] childMeshFilters = selectedObject.GetComponentsInChildren<MeshFilter>(true);
                foreach (MeshFilter meshFilter in childMeshFilters)
                {
                    if (meshFilter != null && objectIds.Add(meshFilter.gameObject))
                    {
                        meshFilters.Add(meshFilter);
                    }
                }
            }

            return meshFilters;
        }

        private static System.Collections.Generic.List<MeshCollider> GetUniqueMeshColliders(GameObject[] selectedObjects)
        {
            System.Collections.Generic.List<MeshCollider> meshColliders =
                new System.Collections.Generic.List<MeshCollider>();
            System.Collections.Generic.HashSet<MeshCollider> colliderIds =
                new System.Collections.Generic.HashSet<MeshCollider>();

            foreach (GameObject selectedObject in selectedObjects)
            {
                MeshCollider[] childMeshColliders = selectedObject.GetComponentsInChildren<MeshCollider>(true);
                foreach (MeshCollider meshCollider in childMeshColliders)
                {
                    if (meshCollider != null && colliderIds.Add(meshCollider))
                    {
                        meshColliders.Add(meshCollider);
                    }
                }
            }

            return meshColliders;
        }

        private static bool ContainsExcludedKeyword(string objectName)
        {
            foreach (string keyword in ExcludedNameKeywords)
            {
                if (objectName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void MarkSelectedScenesDirty(GameObject[] selectedObjects)
        {
            System.Collections.Generic.HashSet<UnityEngine.SceneManagement.Scene> scenes =
                new System.Collections.Generic.HashSet<UnityEngine.SceneManagement.Scene>();
            foreach (GameObject selectedObject in selectedObjects)
            {
                if (scenes.Add(selectedObject.scene))
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(selectedObject.scene);
                }
            }
        }
    }
}

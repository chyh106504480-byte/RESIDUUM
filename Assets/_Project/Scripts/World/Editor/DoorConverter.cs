using System;
using UnityEngine;

namespace Residuum.World.Editor
{
    /// <summary>
    /// 把选中的装饰门网格转换为带独立侧边铰链的可交互门。
    /// </summary>
    public static class DoorConverter
    {
        private const string LeftHingeMenuPath = "Residuum/把选中对象转换为可交互门（左侧铰链）";
        private const string RightHingeMenuPath = "Residuum/把选中对象转换为可交互门（右侧铰链）";
        private const string LeftHingeUndoLabel = "转换为可交互门（左侧铰链）";
        private const string RightHingeUndoLabel = "转换为可交互门（右侧铰链）";
        private const string HingePropertyName = "_hinge";
        private const string HingeNameSuffix = "_Hinge";

        private static readonly string[] ExcludedNameKeywords =
        {
            "Frame",
            "Frames",
            "门框"
        };

        private enum HingeSide
        {
            Left,
            Right
        }

        [UnityEditor.MenuItem(LeftHingeMenuPath)]
        private static void ConvertSelectionWithLeftHinge()
        {
            ConvertSelection(HingeSide.Left, LeftHingeUndoLabel);
        }

        [UnityEditor.MenuItem(RightHingeMenuPath)]
        private static void ConvertSelectionWithRightHinge()
        {
            ConvertSelection(HingeSide.Right, RightHingeUndoLabel);
        }

        private static void ConvertSelection(HingeSide hingeSide, string undoLabel)
        {
            GameObject[] selectedObjects = GetSelectedSceneObjects();
            if (selectedObjects.Length == 0)
            {
                Debug.LogError("请先在 Hierarchy 中选中至少一个场景物体，再转换可交互门。未转换任何物体。");
                return;
            }

            System.Collections.Generic.List<MeshFilter> meshFilters = GetUniqueMeshFilters(selectedObjects);
            int undoGroup = UnityEditor.Undo.GetCurrentGroup();
            UnityEditor.Undo.SetCurrentGroupName(undoLabel);

            try
            {
                int convertedCount = 0;
                int skippedMissingMeshCount = 0;
                int skippedFrameCount = 0;
                int skippedExistingDoorCount = 0;
                int skippedMissingRendererCount = 0;

                foreach (MeshFilter meshFilter in meshFilters)
                {
                    if (meshFilter == null || meshFilter.sharedMesh == null)
                    {
                        skippedMissingMeshCount++;
                        continue;
                    }

                    GameObject doorPanel = meshFilter.gameObject;
                    if (ContainsExcludedKeyword(doorPanel.name))
                    {
                        skippedFrameCount++;
                        continue;
                    }

                    if (HasDoorOnSelfOrAncestor(doorPanel.transform))
                    {
                        skippedExistingDoorCount++;
                        continue;
                    }

                    Renderer doorRenderer = doorPanel.GetComponent<Renderer>();
                    if (doorRenderer == null)
                    {
                        skippedMissingRendererCount++;
                        continue;
                    }

                    ConvertDoorPanel(meshFilter, doorRenderer, hingeSide, undoLabel);
                    convertedCount++;
                }

                UnityEditor.Undo.CollapseUndoOperations(undoGroup);
                if (convertedCount > 0)
                {
                    MarkSelectedScenesDirty(selectedObjects);
                }

                int skippedCount = skippedMissingMeshCount
                    + skippedFrameCount
                    + skippedExistingDoorCount
                    + skippedMissingRendererCount;
                Debug.Log(
                    $"可交互门转换完成：转换 {convertedCount} 扇，跳过 {skippedCount} 个"
                    + $"（MeshFilter 无 sharedMesh：{skippedMissingMeshCount}，"
                    + $"门框名称：{skippedFrameCount}，已在 Door 层级下：{skippedExistingDoorCount}，"
                    + $"缺少 Renderer：{skippedMissingRendererCount}）。",
                    selectedObjects[0]);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                UnityEditor.Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogError("可交互门转换失败，已整体撤销本次转换。", selectedObjects[0]);
            }
        }

        private static void ConvertDoorPanel(
            MeshFilter meshFilter,
            Renderer doorRenderer,
            HingeSide hingeSide,
            string undoLabel)
        {
            GameObject doorPanel = meshFilter.gameObject;
            Transform doorPanelTransform = doorPanel.transform;
            Transform originalParent = doorPanelTransform.parent;
            Bounds worldBounds = doorRenderer.bounds;
            Vector3 hingePosition = GetHingePosition(worldBounds, hingeSide);

            GameObject hingeObject = new GameObject(doorPanel.name + HingeNameSuffix);
            UnityEditor.Undo.RegisterCreatedObjectUndo(hingeObject, undoLabel);

            Transform hingeTransform = hingeObject.transform;
            if (originalParent != null)
            {
                UnityEditor.Undo.SetTransformParent(hingeTransform, originalParent, true, undoLabel);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(
                    hingeObject,
                    doorPanel.scene);
            }

            UnityEditor.Undo.RecordObject(hingeTransform, undoLabel);
            hingeTransform.position = hingePosition;
            hingeTransform.rotation = doorPanelTransform.rotation;
            hingeTransform.localScale = Vector3.one;

            UnityEditor.Undo.SetTransformParent(doorPanelTransform, hingeTransform, true, undoLabel);

            Residuum.World.Door door = UnityEditor.Undo.AddComponent<Residuum.World.Door>(hingeObject);
            if (door == null)
            {
                throw new InvalidOperationException($"无法为物体“{hingeObject.name}”添加 Door 组件。");
            }

            AssignHingeReference(door, hingeTransform, undoLabel);
            EnsureBoxCollider(meshFilter, undoLabel);
            AddNavMeshObstacle(hingeObject, hingeTransform, worldBounds, undoLabel);
        }

        private static Vector3 GetHingePosition(Bounds worldBounds, HingeSide hingeSide)
        {
            Vector3 hingePosition = worldBounds.center;
            hingePosition.y = worldBounds.min.y;
            bool useMinimumEdge = hingeSide == HingeSide.Left;

            // Renderer 的世界包围盒在 XZ 平面中较长的轴就是门板宽度轴。
            if (worldBounds.size.x >= worldBounds.size.z)
            {
                hingePosition.x = useMinimumEdge ? worldBounds.min.x : worldBounds.max.x;
            }
            else
            {
                hingePosition.z = useMinimumEdge ? worldBounds.min.z : worldBounds.max.z;
            }

            return hingePosition;
        }

        private static void AssignHingeReference(
            Residuum.World.Door door,
            Transform hingeTransform,
            string undoLabel)
        {
            UnityEditor.SerializedObject serializedDoor = new UnityEditor.SerializedObject(door);
            serializedDoor.Update();
            UnityEditor.SerializedProperty hingeProperty = serializedDoor.FindProperty(HingePropertyName);
            if (hingeProperty == null)
            {
                throw new MissingFieldException(typeof(Residuum.World.Door).FullName, HingePropertyName);
            }

            UnityEditor.Undo.RecordObject(door, undoLabel);
            hingeProperty.objectReferenceValue = hingeTransform;
            serializedDoor.ApplyModifiedProperties();
        }

        private static void EnsureBoxCollider(MeshFilter meshFilter, string undoLabel)
        {
            GameObject doorPanel = meshFilter.gameObject;
            if (doorPanel.GetComponent<Collider>() != null)
            {
                return;
            }

            BoxCollider boxCollider = UnityEditor.Undo.AddComponent<BoxCollider>(doorPanel);
            if (boxCollider == null)
            {
                throw new InvalidOperationException($"无法为物体“{doorPanel.name}”添加 BoxCollider。");
            }

            UnityEditor.Undo.RecordObject(boxCollider, undoLabel);
            Bounds meshBounds = meshFilter.sharedMesh.bounds;
            boxCollider.center = meshBounds.center;
            boxCollider.size = meshBounds.size;
            boxCollider.isTrigger = false;
        }

        private static void AddNavMeshObstacle(
            GameObject hingeObject,
            Transform hingeTransform,
            Bounds worldBounds,
            string undoLabel)
        {
            UnityEngine.AI.NavMeshObstacle obstacle =
                UnityEditor.Undo.AddComponent<UnityEngine.AI.NavMeshObstacle>(hingeObject);
            if (obstacle == null)
            {
                throw new InvalidOperationException($"无法为物体“{hingeObject.name}”添加 NavMeshObstacle。");
            }

            Bounds localBounds = TransformWorldBoundsToLocal(worldBounds, hingeTransform);
            UnityEditor.Undo.RecordObject(obstacle, undoLabel);
            obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
            obstacle.center = localBounds.center;
            obstacle.size = localBounds.size;
            obstacle.carving = true;
        }

        private static Bounds TransformWorldBoundsToLocal(Bounds worldBounds, Transform localTransform)
        {
            Vector3 worldMin = worldBounds.min;
            Vector3 worldMax = worldBounds.max;
            Vector3 firstCorner = localTransform.InverseTransformPoint(worldMin);
            Bounds localBounds = new Bounds(firstCorner, Vector3.zero);

            for (int xIndex = 0; xIndex < 2; xIndex++)
            {
                for (int yIndex = 0; yIndex < 2; yIndex++)
                {
                    for (int zIndex = 0; zIndex < 2; zIndex++)
                    {
                        Vector3 worldCorner = new Vector3(
                            xIndex == 0 ? worldMin.x : worldMax.x,
                            yIndex == 0 ? worldMin.y : worldMax.y,
                            zIndex == 0 ? worldMin.z : worldMax.z);
                        localBounds.Encapsulate(localTransform.InverseTransformPoint(worldCorner));
                    }
                }
            }

            return localBounds;
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
            System.Collections.Generic.HashSet<GameObject> processedObjects =
                new System.Collections.Generic.HashSet<GameObject>();

            foreach (GameObject selectedObject in selectedObjects)
            {
                MeshFilter[] childMeshFilters = selectedObject.GetComponentsInChildren<MeshFilter>(true);
                foreach (MeshFilter meshFilter in childMeshFilters)
                {
                    if (meshFilter != null && processedObjects.Add(meshFilter.gameObject))
                    {
                        meshFilters.Add(meshFilter);
                    }
                }
            }

            return meshFilters;
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

        private static bool HasDoorOnSelfOrAncestor(Transform targetTransform)
        {
            Transform currentTransform = targetTransform;
            while (currentTransform != null)
            {
                if (currentTransform.GetComponent<Residuum.World.Door>() != null)
                {
                    return true;
                }

                currentTransform = currentTransform.parent;
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

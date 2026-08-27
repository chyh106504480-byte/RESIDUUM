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

            System.Collections.Generic.List<GameObject> doorObjects = GetDoorObjects(
                selectedObjects,
                out int skippedFrameCount);
            if (doorObjects.Count == 0)
            {
                Debug.LogError(
                    "选中物体均为门框或已在 Door 层级下，无法转换可交互门。未转换任何物体。",
                    selectedObjects[0]);
                return;
            }

            if (!AreInSameScene(doorObjects))
            {
                Debug.LogError("选中的门组件不在同一个场景中，无法合并为一扇可交互门。未转换任何物体。", selectedObjects[0]);
                return;
            }

            if (!TryCalculateCombinedRendererBounds(doorObjects, out Bounds worldBounds))
            {
                Debug.LogError(
                    "选中物体及其子物体中没有可用的 Renderer，无法计算整扇门的包围盒。未转换任何物体。",
                    selectedObjects[0]);
                return;
            }

            System.Collections.Generic.List<MeshFilter> meshFilters = GetUniqueMeshFilters(doorObjects);
            int undoGroup = UnityEditor.Undo.GetCurrentGroup();
            UnityEditor.Undo.SetCurrentGroupName(undoLabel);

            try
            {
                GameObject hingeObject = ConvertDoorGroup(
                    doorObjects,
                    selectedObjects[0].name,
                    meshFilters,
                    worldBounds,
                    hingeSide,
                    undoLabel);

                UnityEditor.Undo.CollapseUndoOperations(undoGroup);
                MarkSelectedScenesDirty(selectedObjects);
                Debug.Log(
                    $"已将 {doorObjects.Count} 个选中物体合并为一扇可交互门：{hingeObject.name}。"
                    + $"跳过 {skippedFrameCount} 个门框。",
                    hingeObject);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                UnityEditor.Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogError("可交互门转换失败，已整体撤销本次转换。", selectedObjects[0]);
            }
        }

        private static GameObject ConvertDoorGroup(
            System.Collections.Generic.List<GameObject> doorObjects,
            string hingeNameSource,
            System.Collections.Generic.List<MeshFilter> meshFilters,
            Bounds worldBounds,
            HingeSide hingeSide,
            string undoLabel)
        {
            Vector3 hingePosition = GetHingePosition(worldBounds, hingeSide);
            Transform firstDoorTransform = doorObjects[0].transform;
            Transform originalParent = firstDoorTransform.parent;

            GameObject hingeObject = new GameObject(hingeNameSource + HingeNameSuffix);
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
                    firstDoorTransform.gameObject.scene);
            }

            UnityEditor.Undo.RecordObject(hingeTransform, undoLabel);
            hingeTransform.position = hingePosition;
            hingeTransform.rotation = firstDoorTransform.rotation;
            hingeTransform.localScale = Vector3.one;

            foreach (GameObject doorObject in doorObjects)
            {
                UnityEditor.Undo.SetTransformParent(doorObject.transform, hingeTransform, true, undoLabel);
            }

            Residuum.World.Door door = UnityEditor.Undo.AddComponent<Residuum.World.Door>(hingeObject);
            if (door == null)
            {
                throw new InvalidOperationException($"无法为物体“{hingeObject.name}”添加 Door 组件。");
            }

            AssignHingeReference(door, hingeTransform, undoLabel);
            foreach (MeshFilter meshFilter in meshFilters)
            {
                EnsureBoxCollider(meshFilter, undoLabel);
            }

            AddNavMeshObstacle(hingeObject, hingeTransform, worldBounds, undoLabel);
            return hingeObject;
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
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                return;
            }

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

        private static System.Collections.Generic.List<GameObject> GetDoorObjects(
            GameObject[] selectedObjects,
            out int skippedFrameCount)
        {
            skippedFrameCount = 0;
            System.Collections.Generic.HashSet<GameObject> eligibleObjects =
                new System.Collections.Generic.HashSet<GameObject>();
            System.Collections.Generic.List<GameObject> eligibleObjectOrder =
                new System.Collections.Generic.List<GameObject>();

            foreach (GameObject selectedObject in selectedObjects)
            {
                if (ContainsExcludedKeyword(selectedObject.name))
                {
                    skippedFrameCount++;
                    continue;
                }

                if (HasDoorOnSelfOrAncestor(selectedObject.transform))
                {
                    continue;
                }

                if (eligibleObjects.Add(selectedObject))
                {
                    eligibleObjectOrder.Add(selectedObject);
                }
            }

            System.Collections.Generic.List<GameObject> doorObjects =
                new System.Collections.Generic.List<GameObject>();
            foreach (GameObject eligibleObject in eligibleObjectOrder)
            {
                if (!HasSelectedAncestor(eligibleObject.transform, eligibleObjects))
                {
                    doorObjects.Add(eligibleObject);
                }
            }

            return doorObjects;
        }

        private static bool TryCalculateCombinedRendererBounds(
            System.Collections.Generic.List<GameObject> doorObjects,
            out Bounds combinedBounds)
        {
            combinedBounds = new Bounds();
            bool hasRenderer = false;

            foreach (GameObject doorObject in doorObjects)
            {
                Renderer[] renderers = doorObject.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer renderer in renderers)
                {
                    if (renderer == null || ContainsExcludedKeyword(renderer.gameObject.name))
                    {
                        continue;
                    }

                    if (!hasRenderer)
                    {
                        combinedBounds = renderer.bounds;
                        hasRenderer = true;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(renderer.bounds);
                    }
                }
            }

            return hasRenderer;
        }

        private static System.Collections.Generic.List<MeshFilter> GetUniqueMeshFilters(
            System.Collections.Generic.List<GameObject> doorObjects)
        {
            System.Collections.Generic.List<MeshFilter> meshFilters =
                new System.Collections.Generic.List<MeshFilter>();
            System.Collections.Generic.HashSet<GameObject> processedObjects =
                new System.Collections.Generic.HashSet<GameObject>();

            foreach (GameObject doorObject in doorObjects)
            {
                MeshFilter[] childMeshFilters = doorObject.GetComponentsInChildren<MeshFilter>(true);
                foreach (MeshFilter meshFilter in childMeshFilters)
                {
                    if (meshFilter != null
                        && !ContainsExcludedKeyword(meshFilter.gameObject.name)
                        && processedObjects.Add(meshFilter.gameObject))
                    {
                        meshFilters.Add(meshFilter);
                    }
                }
            }

            return meshFilters;
        }

        private static bool HasSelectedAncestor(
            Transform targetTransform,
            System.Collections.Generic.HashSet<GameObject> selectedObjects)
        {
            Transform currentTransform = targetTransform.parent;
            while (currentTransform != null)
            {
                if (selectedObjects.Contains(currentTransform.gameObject))
                {
                    return true;
                }

                currentTransform = currentTransform.parent;
            }

            return false;
        }

        private static bool AreInSameScene(System.Collections.Generic.List<GameObject> doorObjects)
        {
            UnityEngine.SceneManagement.Scene firstScene = doorObjects[0].scene;
            foreach (GameObject doorObject in doorObjects)
            {
                if (doorObject.scene != firstScene)
                {
                    return false;
                }
            }

            return true;
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

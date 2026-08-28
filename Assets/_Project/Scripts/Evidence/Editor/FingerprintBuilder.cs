using System;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
namespace Residuum.Evidence.Editor
{
    /// <summary>
    /// 为当前场景中的门铺设指纹点位，并完成 GhostAI 到 FingerprintSpawner 的事件接线。
    /// </summary>
    public static class FingerprintBuilder
    {
        private const string MenuPath = "Residuum/铺设指纹点位";
        private const string UndoLabel = "铺设指纹点位";
        private const string FingerprintName = "Fingerprint";
        private const string SpawnerName = "FingerprintSpawner";
        private const string SpawnerMethodName = "Spawn";
        private const string MaterialPath = "Assets/_Project/Art/Materials/Fingerprint.mat";
        private const string TextureFolderPath = "Assets/_Project/Art/Textures";
        private const string FingerprintTextureNamePart = "fingerprint";
        private const string ShaderName = "Universal Render Pipeline/Unlit";
        private const string RenderTypeTag = "RenderType";
        private const string TransparentRenderType = "Transparent";
        private const string SurfaceProperty = "_Surface";
        private const string BlendProperty = "_Blend";
        private const string BaseMapProperty = "_BaseMap";
        private const string SourceBlendProperty = "_SrcBlend";
        private const string DestinationBlendProperty = "_DstBlend";
        private const string SourceBlendAlphaProperty = "_SrcBlendAlpha";
        private const string DestinationBlendAlphaProperty = "_DstBlendAlpha";
        private const string ZWriteProperty = "_ZWrite";
        private const string TransparentSurfaceKeyword = "_SURFACE_TYPE_TRANSPARENT";
        private const string AlphaTestKeyword = "_ALPHATEST_ON";
        private const string AlphaPremultiplyKeyword = "_ALPHAPREMULTIPLY_ON";
        private const string AlphaModulateKeyword = "_ALPHAMODULATE_ON";

        private const float TransparentSurfaceValue = 1f;
        // URP 17.0.1 的 BlendMode 枚举中 Additive = 2。
        private const float AdditiveBlendValue = 2f;
        private const float DisabledValue = 0f;
        private const float FingerprintScale = 0.18f;
        private const float FingerprintHeightAboveDoorBottom = 1f;
        private const float SurfaceOffset = 0.012f;
        private const float HandleSideOffsetRatio = 0.65f;
        private const float DirectionEpsilon = 0.0001f;

        [UnityEditor.MenuItem(MenuPath)]
        private static void BuildFingerprintPoints()
        {
            int undoGroup = UnityEditor.Undo.GetCurrentGroup();
            UnityEditor.Undo.SetCurrentGroupName(UndoLabel);

            try
            {
                Material fingerprintMaterial = GetOrCreateMaterial();
                Residuum.World.Door[] doors =
                    UnityEngine.Object.FindObjectsByType<Residuum.World.Door>(
                        FindObjectsInactive.Include);

                int createdCount = 0;
                int reusedCount = 0;
                for (int doorIndex = 0; doorIndex < doors.Length; doorIndex++)
                {
                    Residuum.World.Door door = doors[doorIndex];
                    if (door == null)
                    {
                        continue;
                    }

                    if (BuildFingerprintForDoor(door, fingerprintMaterial))
                    {
                        createdCount++;
                    }
                    else
                    {
                        reusedCount++;
                    }
                }

                FingerprintSpawner spawner = GetOrCreateSpawner();
                WireGhostFingerprintRequest(spawner);

                UnityEditor.Undo.CollapseUndoOperations(undoGroup);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                UnityEditor.Selection.activeGameObject = spawner.gameObject;
                Debug.Log(
                    $"指纹点位铺设完成：共处理 {createdCount + reusedCount} 扇门，" +
                    $"新建 {createdCount} 个、复用 {reusedCount} 个；FingerprintSpawner 已检查接线。",
                    spawner);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                UnityEditor.Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogError("指纹点位铺设失败，已撤销本次创建和接线。");
            }
        }

        private static Material GetOrCreateMaterial()
        {
            Material existingMaterial =
                UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existingMaterial != null)
            {
                return existingMaterial;
            }

            UnityEngine.Object existingAsset =
                UnityEditor.AssetDatabase.LoadMainAssetAtPath(MaterialPath);
            if (existingAsset != null)
            {
                throw new InvalidOperationException(
                    $"{MaterialPath} 已被非 Material 资产占用，无法创建指纹材质。");
            }

            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                throw new InvalidOperationException(
                    $"找不到 Shader：{ShaderName}，无法创建指纹材质。");
            }

            Material material = new Material(shader)
            {
                name = FingerprintName
            };
            ConfigureNewMaterial(material);

            Texture2D fingerprintTexture = FindFingerprintTexture();
            if (fingerprintTexture != null)
            {
                material.SetTexture(BaseMapProperty, fingerprintTexture);
            }
            else
            {
                Debug.LogWarning(
                    $"在 {TextureFolderPath} 下没有找到文件名包含 fingerprint 的 Texture2D；" +
                    "Fingerprint.mat 仍会创建，请放入贴图后手动赋给 Base Map。");
            }

            UnityEditor.AssetDatabase.CreateAsset(material, MaterialPath);
            UnityEditor.Undo.RegisterCreatedObjectUndo(material, UndoLabel);
            return material;
        }

        private static void ConfigureNewMaterial(Material material)
        {
            material.SetFloat(SurfaceProperty, TransparentSurfaceValue);
            material.SetFloat(BlendProperty, AdditiveBlendValue);
            material.SetFloat(
                SourceBlendProperty,
                (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat(
                DestinationBlendProperty,
                (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat(
                SourceBlendAlphaProperty,
                (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat(
                DestinationBlendAlphaProperty,
                (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat(ZWriteProperty, DisabledValue);
            material.EnableKeyword(TransparentSurfaceKeyword);
            material.DisableKeyword(AlphaTestKeyword);
            material.DisableKeyword(AlphaPremultiplyKeyword);
            material.DisableKeyword(AlphaModulateKeyword);
            material.SetOverrideTag(RenderTypeTag, TransparentRenderType);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        private static Texture2D FindFingerprintTexture()
        {
            string[] textureGuids = UnityEditor.AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { TextureFolderPath });
            for (int textureIndex = 0; textureIndex < textureGuids.Length; textureIndex++)
            {
                string texturePath =
                    UnityEditor.AssetDatabase.GUIDToAssetPath(textureGuids[textureIndex]);
                string fileName = Path.GetFileNameWithoutExtension(texturePath);
                if (fileName.IndexOf(
                        FingerprintTextureNamePart,
                        StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                Texture2D texture =
                    UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (texture != null)
                {
                    return texture;
                }
            }

            return null;
        }

        private static bool BuildFingerprintForDoor(
            Residuum.World.Door door,
            Material fingerprintMaterial)
        {
            Transform fingerprintTransform = FindDirectChild(door.transform, FingerprintName);
            bool wasCreated = fingerprintTransform == null;
            GameObject fingerprintObject;
            if (wasCreated)
            {
                fingerprintObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                fingerprintObject.name = FingerprintName;
                UnityEditor.Undo.RegisterCreatedObjectUndo(fingerprintObject, UndoLabel);
                UnityEditor.Undo.SetTransformParent(
                    fingerprintObject.transform,
                    door.transform,
                    UndoLabel);

                // SetTransformParent 会保持世界变换，必须先清掉补偿后的本地变换。
                fingerprintTransform = fingerprintObject.transform;
                fingerprintTransform.localPosition = Vector3.zero;
                fingerprintTransform.localRotation = Quaternion.identity;
                fingerprintTransform.localScale = Vector3.one;
            }
            else
            {
                fingerprintObject = fingerprintTransform.gameObject;
                UnityEditor.Undo.RecordObject(fingerprintTransform, UndoLabel);
            }

            MeshCollider meshCollider = fingerprintObject.GetComponent<MeshCollider>();
            if (meshCollider != null)
            {
                UnityEditor.Undo.DestroyObjectImmediate(meshCollider);
            }

            Renderer fingerprintRenderer = fingerprintObject.GetComponent<Renderer>();
            if (fingerprintRenderer == null)
            {
                throw new InvalidOperationException(
                    $"{GetHierarchyPath(fingerprintTransform)} 缺少 Renderer，无法配置指纹材质。");
            }

            if (fingerprintObject.GetComponent<Fingerprint>() == null)
            {
                Fingerprint fingerprint =
                    UnityEditor.Undo.AddComponent<Fingerprint>(fingerprintObject);
                if (fingerprint == null)
                {
                    throw new InvalidOperationException(
                        $"无法为 {GetHierarchyPath(fingerprintTransform)} 添加 Fingerprint 组件。");
                }
            }

            Bounds doorBounds = GetDoorBounds(door);
            Vector3 thicknessAxis;
            Vector3 widthAxis;
            GetDoorAxes(door.transform, doorBounds, out thicknessAxis, out widthAxis);

            Vector3 outwardDirection = GetOutwardDirection(
                door.transform,
                doorBounds,
                thicknessAxis);
            Vector3 handleDirection = GetHandleDirection(
                door.transform,
                doorBounds,
                widthAxis);
            float normalExtent = GetBoundsExtentAlongDirection(
                doorBounds.extents,
                outwardDirection);
            float widthExtent = GetBoundsExtentAlongDirection(
                doorBounds.extents,
                handleDirection);
            float targetHeight = Mathf.Min(
                doorBounds.max.y,
                doorBounds.min.y + FingerprintHeightAboveDoorBottom);

            Vector3 targetPosition = doorBounds.center
                + handleDirection * widthExtent * HandleSideOffsetRatio
                + Vector3.up * (targetHeight - doorBounds.center.y)
                + outwardDirection * (normalExtent + SurfaceOffset);

            fingerprintTransform.localScale = new Vector3(
                FingerprintScale,
                FingerprintScale,
                1f);
            fingerprintTransform.SetPositionAndRotation(
                targetPosition,
                // Unity 内置 Quad 的正面法线是本地 -Z。
                Quaternion.LookRotation(-outwardDirection, Vector3.up));

            UnityEditor.Undo.RecordObject(fingerprintRenderer, UndoLabel);
            fingerprintRenderer.sharedMaterial = fingerprintMaterial;
            return wasCreated;
        }

        private static Bounds GetDoorBounds(Residuum.World.Door door)
        {
            Renderer[] renderers = door.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            Bounds combinedBounds = default;
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer doorRenderer = renderers[rendererIndex];
                if (doorRenderer == null
                    || doorRenderer.gameObject.name == FingerprintName)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    combinedBounds = doorRenderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(doorRenderer.bounds);
                }
            }

            if (!hasBounds)
            {
                throw new InvalidOperationException(
                    $"门 {GetHierarchyPath(door.transform)} 及其子物体中没有 Renderer，无法放置指纹。");
            }

            return combinedBounds;
        }

        private static void GetDoorAxes(
            Transform doorTransform,
            Bounds doorBounds,
            out Vector3 thicknessAxis,
            out Vector3 widthAxis)
        {
            Vector3 forward = doorTransform.forward.normalized;
            Vector3 right = doorTransform.right.normalized;
            float forwardExtent = GetBoundsExtentAlongDirection(doorBounds.extents, forward);
            float rightExtent = GetBoundsExtentAlongDirection(doorBounds.extents, right);

            if (forwardExtent <= rightExtent)
            {
                thicknessAxis = forward;
                widthAxis = right;
            }
            else
            {
                thicknessAxis = right;
                widthAxis = forward;
            }
        }

        private static Vector3 GetOutwardDirection(
            Transform doorTransform,
            Bounds doorBounds,
            Vector3 thicknessAxis)
        {
            float side = Vector3.Dot(
                doorTransform.position - doorBounds.center,
                thicknessAxis);
            return side < -DirectionEpsilon ? -thicknessAxis : thicknessAxis;
        }

        private static Vector3 GetHandleDirection(
            Transform doorTransform,
            Bounds doorBounds,
            Vector3 widthAxis)
        {
            float towardDoorCenter = Vector3.Dot(
                doorBounds.center - doorTransform.position,
                widthAxis);
            return towardDoorCenter < -DirectionEpsilon ? -widthAxis : widthAxis;
        }

        private static float GetBoundsExtentAlongDirection(
            Vector3 boundsExtents,
            Vector3 direction)
        {
            return Mathf.Abs(direction.x) * boundsExtents.x
                + Mathf.Abs(direction.y) * boundsExtents.y
                + Mathf.Abs(direction.z) * boundsExtents.z;
        }

        private static FingerprintSpawner GetOrCreateSpawner()
        {
            GameObject spawnerObject = FindSceneObject(SpawnerName);
            if (spawnerObject == null)
            {
                spawnerObject = new GameObject(SpawnerName);
                UnityEditor.Undo.RegisterCreatedObjectUndo(spawnerObject, UndoLabel);
            }

            FingerprintSpawner spawner = spawnerObject.GetComponent<FingerprintSpawner>();
            if (spawner == null)
            {
                spawner = UnityEditor.Undo.AddComponent<FingerprintSpawner>(spawnerObject);
            }

            if (spawner == null)
            {
                throw new InvalidOperationException(
                    "无法创建或获取 FingerprintSpawner 组件。");
            }

            return spawner;
        }

        private static void WireGhostFingerprintRequest(FingerprintSpawner spawner)
        {
            Residuum.Ghost.GhostAI ghostAI =
                UnityEngine.Object.FindAnyObjectByType<Residuum.Ghost.GhostAI>(
                    FindObjectsInactive.Include);
            if (ghostAI == null)
            {
                Debug.LogWarning(
                    "场景中找不到 GhostAI：FingerprintSpawner 已创建，但 onFingerprintRequest 未接线。",
                    spawner);
                return;
            }

            UnityEditor.Undo.RecordObject(ghostAI, UndoLabel);
            if (ghostAI.onFingerprintRequest == null)
            {
                ghostAI.onFingerprintRequest = new UnityEngine.Events.UnityEvent<Transform>();
            }

            RemovePersistentListeners(ghostAI.onFingerprintRequest, SpawnerMethodName);
            UnityEngine.Events.UnityAction<Transform> listener = spawner.Spawn;
            UnityEditor.Events.UnityEventTools.AddPersistentListener<Transform>(
                ghostAI.onFingerprintRequest,
                listener);
            UnityEditor.EditorUtility.SetDirty(ghostAI);
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

        private static Transform FindDirectChild(Transform parent, string childName)
        {
            for (int childIndex = 0; childIndex < parent.childCount; childIndex++)
            {
                Transform child = parent.GetChild(childIndex);
                if (child != null && child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private static GameObject FindSceneObject(string objectName)
        {
            Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include);
            for (int transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
            {
                Transform candidate = transforms[transformIndex];
                if (candidate != null && candidate.name == objectName)
                {
                    return candidate.gameObject;
                }
            }

            return null;
        }

        private static string GetHierarchyPath(Transform target)
        {
            string path = target.name;
            Transform current = target.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }
    }
}
#endif

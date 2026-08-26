using System;
using UnityEngine;
using Residuum.Evidence;

namespace Residuum.Ghost.Editor
{
    /// <summary>
    /// 创建三种鬼定义资产，并在当前场景生成最小可手工调整的鬼装配。
    /// </summary>
    public static class GhostSetupBuilder
    {
        private const string CreateDefinitionsMenuPath = "Residuum/创建三种鬼资产";
        private const string BuildGhostMenuPath = "Residuum/搭建鬼装配";
        private const string UndoLabel = "搭建鬼装配";
        private const string GhostAssetFolder = "Assets/_Project/ScriptableObjects/Ghosts";
        private const string SpiritAssetName = "GhostDef_Spirit";
        private const string WraithAssetName = "GhostDef_Wraith";
        private const string PoltergeistAssetName = "GhostDef_Poltergeist";
        private const string GhostName = "Ghost";
        private const string BodyName = "Body";
        private const string RoamPointsName = "RoamPoints";
        private const string RoamPointNamePrefix = "RoamPoint_";
        private const string CapsuleMeshResourcePath = "Capsule.fbx";

        private const int EvidenceCount = 2;
        private const int RoamPointCount = 4;
        private const float NavMeshAgentRadius = 0.3f;
        private const float NavMeshAgentHeight = 2f;
        private const float NavMeshAgentStoppingDistance = 0.5f;
        private const float GhostRoomRadius = 4f;

        [UnityEditor.MenuItem(CreateDefinitionsMenuPath)]
        private static void CreateGhostDefinitions()
        {
            EnsureGhostAssetFolder();

            string[] createdAssetNames = new string[3];
            int createdAssetCount = 0;

            if (CreateDefinitionIfMissing(
                    SpiritAssetName,
                    "怨灵",
                    "Spirit",
                    "迟缓、执拗，长时间停留在鬼房，脚步声沉重清晰。",
                    EvidenceType.EMF5,
                    EvidenceType.UVFingerprint,
                    1.6f,
                    1.6f,
                    true,
                    30f,
                    25f,
                    1f,
                    false,
                    6f,
                    false,
                    1f))
            {
                createdAssetNames[createdAssetCount] = SpiritAssetName;
                createdAssetCount++;
            }

            if (CreateDefinitionIfMissing(
                    WraithAssetName,
                    "幽影",
                    "Wraith",
                    "飘忽无踪，不在地板留下脚印。",
                    EvidenceType.EMF5,
                    EvidenceType.GhostWriting,
                    1.7f,
                    1.7f,
                    false,
                    25f,
                    25f,
                    1f,
                    true,
                    6f,
                    false,
                    1f))
            {
                createdAssetNames[createdAssetCount] = WraithAssetName;
                createdAssetCount++;
            }

            if (CreateDefinitionIfMissing(
                    PoltergeistAssetName,
                    "骚灵",
                    "Poltergeist",
                    "暴躁、破坏，频繁抛掷物品制造巨响。",
                    EvidenceType.UVFingerprint,
                    EvidenceType.GhostWriting,
                    1.7f,
                    1.8f,
                    true,
                    22f,
                    25f,
                    1.5f,
                    false,
                    6f,
                    true,
                    2f))
            {
                createdAssetNames[createdAssetCount] = PoltergeistAssetName;
                createdAssetCount++;
            }

            UnityEditor.AssetDatabase.SaveAssets();

            if (createdAssetCount == 0)
            {
                Debug.Log("三种鬼资产均已存在，未覆盖任何资产。");
                return;
            }

            string createdAssetList = string.Join("、", createdAssetNames, 0, createdAssetCount);
            Debug.Log($"已创建鬼定义资产：{createdAssetList}。");
        }

        [UnityEditor.MenuItem(BuildGhostMenuPath)]
        private static void BuildGhostSetup()
        {
            GhostAI existingGhost = UnityEngine.Object.FindAnyObjectByType<GhostAI>(
                UnityEngine.FindObjectsInactive.Include);
            if (existingGhost != null)
            {
                UnityEditor.Selection.activeGameObject = existingGhost.gameObject;
                Debug.Log("场景中已经存在 GhostAI，未重复创建鬼装配，已选中它。", existingGhost);
                return;
            }

            Residuum.Player.PlayerController playerController =
                UnityEngine.Object.FindAnyObjectByType<Residuum.Player.PlayerController>(
                    UnityEngine.FindObjectsInactive.Include);
            if (playerController == null)
            {
                Debug.LogError("场景中没有 PlayerController。请先执行 Residuum/搭建玩家装配，未创建鬼装配。");
                return;
            }

            GhostDefinition spiritDefinition = LoadDefinition(SpiritAssetName);
            if (spiritDefinition == null)
            {
                Debug.LogError(
                    "找不到 GhostDef_Spirit 鬼定义资产。请先执行 Residuum/创建三种鬼资产，未创建鬼装配。");
                return;
            }

            int undoGroup = UnityEditor.Undo.GetCurrentGroup();
            UnityEditor.Undo.SetCurrentGroupName(UndoLabel);

            try
            {
                GameObject ghostObject = new GameObject(GhostName);
                UnityEditor.Undo.RegisterCreatedObjectUndo(ghostObject, UndoLabel);

                UnityEngine.AI.NavMeshAgent navMeshAgent =
                    UnityEditor.Undo.AddComponent<UnityEngine.AI.NavMeshAgent>(ghostObject);
                if (navMeshAgent == null)
                {
                    throw new InvalidOperationException("无法为 Ghost 添加 NavMeshAgent。");
                }

                UnityEditor.Undo.RecordObject(navMeshAgent, UndoLabel);
                navMeshAgent.radius = NavMeshAgentRadius;
                navMeshAgent.height = NavMeshAgentHeight;
                navMeshAgent.stoppingDistance = NavMeshAgentStoppingDistance;

                GhostAI ghostAI = UnityEditor.Undo.AddComponent<GhostAI>(ghostObject);
                if (ghostAI == null)
                {
                    throw new InvalidOperationException("无法为 Ghost 添加 GhostAI。");
                }

                Renderer bodyRenderer = CreateBody(ghostObject.transform);
                Transform[] roamPoints = CreateRoamPoints(ghostObject.transform);
                ConfigureGhostAI(
                    ghostAI,
                    spiritDefinition,
                    playerController.transform,
                    ghostObject.transform,
                    roamPoints,
                    bodyRenderer);

                UnityEditor.Undo.CollapseUndoOperations(undoGroup);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                UnityEditor.Selection.activeGameObject = ghostObject;
                Debug.LogWarning(
                    "鬼装配已完成：鬼房中心暂用 Ghost 自身位置、半径暂用 4。正式回合应由 T15 GameManager 注入实际鬼房数据。",
                    ghostObject);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                UnityEditor.Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogError("鬼装配失败，已撤销本次创建和接线。", playerController);
            }
        }

        private static void EnsureGhostAssetFolder()
        {
            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/_Project"))
            {
                throw new InvalidOperationException("找不到 Assets/_Project 目录，无法创建鬼定义资产。");
            }

            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/_Project/ScriptableObjects"))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets/_Project", "ScriptableObjects");
            }

            if (!UnityEditor.AssetDatabase.IsValidFolder(GhostAssetFolder))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets/_Project/ScriptableObjects", "Ghosts");
            }
        }

        private static bool CreateDefinitionIfMissing(
            string assetName,
            string ghostName,
            string displayNameEN,
            string journalDescription,
            EvidenceType firstEvidence,
            EvidenceType secondEvidence,
            float walkSpeed,
            float huntSpeed,
            bool leavesFootprints,
            float huntDuration,
            float huntCooldown,
            float sanityDrainMultiplier,
            bool canSprintBurst,
            float sprintBurstInterval,
            bool massThrowOnHunt,
            float interactFrequency)
        {
            string assetPath = $"{GhostAssetFolder}/{assetName}.asset";
            UnityEngine.Object existingAsset =
                UnityEditor.AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (existingAsset != null)
            {
                Debug.Log($"同名资产已存在，未覆盖：{assetPath}。", existingAsset);
                return false;
            }

            GhostDefinition definition = ScriptableObject.CreateInstance<GhostDefinition>();
            definition.name = assetName;
            ConfigureDefinition(
                definition,
                ghostName,
                displayNameEN,
                journalDescription,
                firstEvidence,
                secondEvidence,
                walkSpeed,
                huntSpeed,
                leavesFootprints,
                huntDuration,
                huntCooldown,
                sanityDrainMultiplier,
                canSprintBurst,
                sprintBurstInterval,
                massThrowOnHunt,
                interactFrequency);
            UnityEditor.AssetDatabase.CreateAsset(definition, assetPath);
            return true;
        }

        private static void ConfigureDefinition(
            GhostDefinition definition,
            string ghostName,
            string displayNameEN,
            string journalDescription,
            EvidenceType firstEvidence,
            EvidenceType secondEvidence,
            float walkSpeed,
            float huntSpeed,
            bool leavesFootprints,
            float huntDuration,
            float huntCooldown,
            float sanityDrainMultiplier,
            bool canSprintBurst,
            float sprintBurstInterval,
            bool massThrowOnHunt,
            float interactFrequency)
        {
            UnityEditor.SerializedObject serializedDefinition =
                new UnityEditor.SerializedObject(definition);
            serializedDefinition.Update();

            UnityEditor.SerializedProperty ghostNameProperty =
                serializedDefinition.FindProperty("ghostName");
            UnityEditor.SerializedProperty displayNameENProperty =
                serializedDefinition.FindProperty("displayNameEN");
            UnityEditor.SerializedProperty journalDescriptionProperty =
                serializedDefinition.FindProperty("journalDescription");
            UnityEditor.SerializedProperty evidencesProperty =
                serializedDefinition.FindProperty("evidences");
            UnityEditor.SerializedProperty walkSpeedProperty =
                serializedDefinition.FindProperty("walkSpeed");
            UnityEditor.SerializedProperty huntSpeedProperty =
                serializedDefinition.FindProperty("huntSpeed");
            UnityEditor.SerializedProperty leavesFootprintsProperty =
                serializedDefinition.FindProperty("leavesFootprints");
            UnityEditor.SerializedProperty huntDurationProperty =
                serializedDefinition.FindProperty("huntDuration");
            UnityEditor.SerializedProperty huntCooldownProperty =
                serializedDefinition.FindProperty("huntCooldown");
            UnityEditor.SerializedProperty sanityDrainMultiplierProperty =
                serializedDefinition.FindProperty("sanityDrainMultiplier");
            UnityEditor.SerializedProperty canSprintBurstProperty =
                serializedDefinition.FindProperty("canSprintBurst");
            UnityEditor.SerializedProperty sprintBurstIntervalProperty =
                serializedDefinition.FindProperty("sprintBurstInterval");
            UnityEditor.SerializedProperty massThrowOnHuntProperty =
                serializedDefinition.FindProperty("massThrowOnHunt");
            UnityEditor.SerializedProperty interactFrequencyProperty =
                serializedDefinition.FindProperty("interactFrequency");

            if (ghostNameProperty == null || displayNameENProperty == null
                || journalDescriptionProperty == null || evidencesProperty == null
                || walkSpeedProperty == null || huntSpeedProperty == null
                || leavesFootprintsProperty == null || huntDurationProperty == null
                || huntCooldownProperty == null || sanityDrainMultiplierProperty == null
                || canSprintBurstProperty == null || sprintBurstIntervalProperty == null
                || massThrowOnHuntProperty == null || interactFrequencyProperty == null)
            {
                throw new InvalidOperationException(
                    "GhostDefinition 缺少预期的序列化字段，无法创建三种鬼资产。");
            }

            if (!evidencesProperty.isArray)
            {
                throw new InvalidOperationException(
                    "GhostDefinition 的 evidences 序列化字段不是数组，无法创建三种鬼资产。");
            }

            evidencesProperty.arraySize = EvidenceCount;
            ghostNameProperty.stringValue = ghostName;
            displayNameENProperty.stringValue = displayNameEN;
            journalDescriptionProperty.stringValue = journalDescription;
            evidencesProperty.GetArrayElementAtIndex(0).enumValueIndex = (int)firstEvidence;
            evidencesProperty.GetArrayElementAtIndex(1).enumValueIndex = (int)secondEvidence;
            walkSpeedProperty.floatValue = walkSpeed;
            huntSpeedProperty.floatValue = huntSpeed;
            leavesFootprintsProperty.boolValue = leavesFootprints;
            huntDurationProperty.floatValue = huntDuration;
            huntCooldownProperty.floatValue = huntCooldown;
            sanityDrainMultiplierProperty.floatValue = sanityDrainMultiplier;
            canSprintBurstProperty.boolValue = canSprintBurst;
            sprintBurstIntervalProperty.floatValue = sprintBurstInterval;
            massThrowOnHuntProperty.boolValue = massThrowOnHunt;
            interactFrequencyProperty.floatValue = interactFrequency;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GhostDefinition LoadDefinition(string assetName)
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GhostDefinition>(
                $"{GhostAssetFolder}/{assetName}.asset");
        }

        private static Renderer CreateBody(Transform ghostTransform)
        {
            Mesh capsuleMesh = Resources.GetBuiltinResource<Mesh>(CapsuleMeshResourcePath);
            if (capsuleMesh == null)
            {
                throw new InvalidOperationException("无法获取 Unity 内置 Capsule 网格，无法创建 Ghost/Body。");
            }

            GameObject bodyObject = new GameObject(BodyName);
            UnityEditor.Undo.RegisterCreatedObjectUndo(bodyObject, UndoLabel);
            UnityEditor.Undo.SetTransformParent(bodyObject.transform, ghostTransform, UndoLabel);
            bodyObject.transform.localPosition = Vector3.zero;
            bodyObject.transform.localRotation = Quaternion.identity;

            MeshFilter meshFilter = UnityEditor.Undo.AddComponent<MeshFilter>(bodyObject);
            MeshRenderer meshRenderer = UnityEditor.Undo.AddComponent<MeshRenderer>(bodyObject);
            if (meshFilter == null || meshRenderer == null)
            {
                throw new InvalidOperationException("无法为 Ghost/Body 添加 MeshFilter 或 MeshRenderer。");
            }

            UnityEditor.Undo.RecordObject(meshFilter, UndoLabel);
            meshFilter.sharedMesh = capsuleMesh;
            return meshRenderer;
        }

        private static Transform[] CreateRoamPoints(Transform ghostTransform)
        {
            GameObject roamPointsObject = new GameObject(RoamPointsName);
            UnityEditor.Undo.RegisterCreatedObjectUndo(roamPointsObject, UndoLabel);
            UnityEditor.Undo.SetTransformParent(roamPointsObject.transform, ghostTransform, UndoLabel);
            roamPointsObject.transform.localPosition = Vector3.zero;
            roamPointsObject.transform.localRotation = Quaternion.identity;

            Transform[] roamPoints = new Transform[RoamPointCount];
            for (int pointIndex = 0; pointIndex < RoamPointCount; pointIndex++)
            {
                GameObject roamPointObject = new GameObject($"{RoamPointNamePrefix}{pointIndex + 1}");
                UnityEditor.Undo.RegisterCreatedObjectUndo(roamPointObject, UndoLabel);
                UnityEditor.Undo.SetTransformParent(
                    roamPointObject.transform,
                    roamPointsObject.transform,
                    UndoLabel);
                roamPointObject.transform.localPosition = Vector3.zero;
                roamPointObject.transform.localRotation = Quaternion.identity;
                roamPoints[pointIndex] = roamPointObject.transform;
            }

            return roamPoints;
        }

        private static void ConfigureGhostAI(
            GhostAI ghostAI,
            GhostDefinition definition,
            Transform playerTransform,
            Transform ghostRoomCenter,
            Transform[] roamPoints,
            Renderer bodyRenderer)
        {
            UnityEditor.Undo.RecordObject(ghostAI, UndoLabel);
            UnityEditor.SerializedObject serializedGhostAI = new UnityEditor.SerializedObject(ghostAI);
            serializedGhostAI.Update();

            UnityEditor.SerializedProperty definitionProperty =
                serializedGhostAI.FindProperty("_definition");
            UnityEditor.SerializedProperty playerProperty =
                serializedGhostAI.FindProperty("_player");
            UnityEditor.SerializedProperty ghostRoomCenterProperty =
                serializedGhostAI.FindProperty("_ghostRoomCenter");
            UnityEditor.SerializedProperty ghostRoomRadiusProperty =
                serializedGhostAI.FindProperty("_ghostRoomRadius");
            UnityEditor.SerializedProperty roamPointsProperty =
                serializedGhostAI.FindProperty("_roamPoints");
            UnityEditor.SerializedProperty renderersProperty =
                serializedGhostAI.FindProperty("_renderers");

            if (definitionProperty == null || playerProperty == null || ghostRoomCenterProperty == null
                || ghostRoomRadiusProperty == null || roamPointsProperty == null || renderersProperty == null)
            {
                throw new InvalidOperationException(
                    "GhostAI 缺少 _definition、_player、_ghostRoomCenter、_ghostRoomRadius、_roamPoints 或 _renderers 序列化字段，无法完成鬼装配接线。");
            }

            if (!roamPointsProperty.isArray || !renderersProperty.isArray)
            {
                throw new InvalidOperationException(
                    "GhostAI 的 _roamPoints 或 _renderers 序列化字段不是数组，无法完成鬼装配接线。");
            }

            definitionProperty.objectReferenceValue = definition;
            playerProperty.objectReferenceValue = playerTransform;
            ghostRoomCenterProperty.objectReferenceValue = ghostRoomCenter;
            ghostRoomRadiusProperty.floatValue = GhostRoomRadius;
            roamPointsProperty.arraySize = roamPoints.Length;
            for (int pointIndex = 0; pointIndex < roamPoints.Length; pointIndex++)
            {
                roamPointsProperty.GetArrayElementAtIndex(pointIndex).objectReferenceValue = roamPoints[pointIndex];
            }

            renderersProperty.arraySize = 1;
            renderersProperty.GetArrayElementAtIndex(0).objectReferenceValue = bodyRenderer;
            serializedGhostAI.ApplyModifiedProperties();
        }
    }
}

using System;
using UnityEngine;

namespace Residuum.World.Editor
{
    /// <summary>
    /// 为场景中的 Door 与 LightSwitch 批量补齐三维交互音效配置。
    /// </summary>
    public static class InteractAudioBuilder
    {
        private const string MenuPath = "Residuum/为门与开关配音效";
        private const string UndoLabel = "为门与开关配音效";
        private const string AudioSearchFolder = "Assets/_Project/Audio";
        private const string DoorAudioSourcePropertyName = "_audioSource";
        private const string DoorOpenClipPropertyName = "_openClip";
        private const string LightSwitchAudioSourcePropertyName = "_audioSource";
        private const string LightSwitchToggleClipPropertyName = "_toggleClip";
        private const float FullSpatialBlend = 1f;
        private const float DoorMaxDistance = 20f;
        private const float LightSwitchMaxDistance = 12f;

        private static readonly string[] DoorClipKeywords = { "开门", "吱呀" };
        private static readonly string[] LightSwitchClipKeywords = { "开灯", "按键" };

        [UnityEditor.MenuItem(MenuPath)]
        private static void BuildInteractAudio()
        {
            AudioClip doorClip = FindAudioClip(DoorClipKeywords);
            AudioClip lightSwitchClip = FindAudioClip(LightSwitchClipKeywords);
            WarnIfClipMissing(doorClip, "开门音效（关键字：开门、吱呀）");
            WarnIfClipMissing(lightSwitchClip, "开关音效（关键字：开灯、按键）");

            Door[] doors = UnityEngine.Object.FindObjectsByType<Door>(FindObjectsInactive.Include);
            LightSwitch[] lightSwitches =
                UnityEngine.Object.FindObjectsByType<LightSwitch>(FindObjectsInactive.Include);
            int undoGroup = UnityEditor.Undo.GetCurrentGroup();
            UnityEditor.Undo.SetCurrentGroupName(UndoLabel);

            try
            {
                System.Collections.Generic.HashSet<UnityEngine.SceneManagement.Scene> affectedScenes =
                    new System.Collections.Generic.HashSet<UnityEngine.SceneManagement.Scene>();

                foreach (Door door in doors)
                {
                    if (door == null)
                    {
                        continue;
                    }

                    ConfigureDoor(door, doorClip);
                    affectedScenes.Add(door.gameObject.scene);
                }

                foreach (LightSwitch lightSwitch in lightSwitches)
                {
                    if (lightSwitch == null)
                    {
                        continue;
                    }

                    ConfigureLightSwitch(lightSwitch, lightSwitchClip);
                    affectedScenes.Add(lightSwitch.gameObject.scene);
                }

                UnityEditor.Undo.CollapseUndoOperations(undoGroup);
                foreach (UnityEngine.SceneManagement.Scene scene in affectedScenes)
                {
                    if (scene.IsValid())
                    {
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                    }
                }

                Debug.Log(
                    $"门与开关音效配置完成：处理了 {doors.Length} 扇门、{lightSwitches.Length} 个开关；"
                    + $"开门 clip：{GetClipStatus(doorClip)}；开关 clip：{GetClipStatus(lightSwitchClip)}。");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                UnityEditor.Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogError("门与开关音效配置失败，已撤销本次修改。");
            }
        }

        private static void ConfigureDoor(Door door, AudioClip doorClip)
        {
            AudioSource audioSource = GetOrAddAudioSource(door.gameObject);
            ConfigureAudioSource(audioSource, DoorMaxDistance);

            UnityEditor.Undo.RecordObject(door, UndoLabel);
            UnityEditor.SerializedObject serializedDoor = new UnityEditor.SerializedObject(door);
            serializedDoor.Update();
            GetRequiredProperty(serializedDoor, DoorAudioSourcePropertyName, door).objectReferenceValue = audioSource;

            UnityEditor.SerializedProperty openClipProperty =
                GetRequiredProperty(serializedDoor, DoorOpenClipPropertyName, door);
            if (openClipProperty.objectReferenceValue == null && doorClip != null)
            {
                openClipProperty.objectReferenceValue = doorClip;
            }

            serializedDoor.ApplyModifiedProperties();
        }

        private static void ConfigureLightSwitch(LightSwitch lightSwitch, AudioClip lightSwitchClip)
        {
            AudioSource audioSource = GetOrAddAudioSource(lightSwitch.gameObject);
            ConfigureAudioSource(audioSource, LightSwitchMaxDistance);

            UnityEditor.Undo.RecordObject(lightSwitch, UndoLabel);
            UnityEditor.SerializedObject serializedLightSwitch = new UnityEditor.SerializedObject(lightSwitch);
            serializedLightSwitch.Update();
            GetRequiredProperty(serializedLightSwitch, LightSwitchAudioSourcePropertyName, lightSwitch)
                .objectReferenceValue = audioSource;

            UnityEditor.SerializedProperty toggleClipProperty =
                GetRequiredProperty(serializedLightSwitch, LightSwitchToggleClipPropertyName, lightSwitch);
            if (toggleClipProperty.objectReferenceValue == null && lightSwitchClip != null)
            {
                toggleClipProperty.objectReferenceValue = lightSwitchClip;
            }

            serializedLightSwitch.ApplyModifiedProperties();
        }

        private static AudioSource GetOrAddAudioSource(GameObject targetObject)
        {
            AudioSource audioSource = targetObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = UnityEditor.Undo.AddComponent<AudioSource>(targetObject);
            }

            if (audioSource == null)
            {
                throw new InvalidOperationException($"无法为物体“{targetObject.name}”添加或取得 AudioSource。");
            }

            return audioSource;
        }

        private static void ConfigureAudioSource(AudioSource audioSource, float maxDistance)
        {
            UnityEditor.Undo.RecordObject(audioSource, UndoLabel);
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = FullSpatialBlend;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.maxDistance = maxDistance;
        }

        private static AudioClip FindAudioClip(string[] keywords)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AudioClip", new[] { AudioSearchFolder });
            Array.Sort(guids, CompareAssetPaths);

            foreach (string guid in guids)
            {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                if (clip != null && ContainsAnyKeyword(clip.name, keywords))
                {
                    return clip;
                }
            }

            return null;
        }

        private static int CompareAssetPaths(string leftGuid, string rightGuid)
        {
            string leftPath = UnityEditor.AssetDatabase.GUIDToAssetPath(leftGuid);
            string rightPath = UnityEditor.AssetDatabase.GUIDToAssetPath(rightGuid);
            return string.Compare(leftPath, rightPath, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ContainsAnyKeyword(string fileName, string[] keywords)
        {
            foreach (string keyword in keywords)
            {
                if (fileName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static UnityEditor.SerializedProperty GetRequiredProperty(
            UnityEditor.SerializedObject serializedObject,
            string propertyName,
            UnityEngine.Object target)
        {
            UnityEditor.SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name} 缺少序列化字段“{propertyName}”，无法完成音效接线。");
            }

            return property;
        }

        private static void WarnIfClipMissing(AudioClip clip, string clipDescription)
        {
            if (clip == null)
            {
                Debug.LogWarning($"未在“{AudioSearchFolder}”找到{clipDescription}，已仅配置 AudioSource 与音源引用。");
            }
        }

        private static string GetClipStatus(AudioClip clip)
        {
            return clip == null ? "未找到" : $"已找到（{clip.name}）";
        }
    }
}

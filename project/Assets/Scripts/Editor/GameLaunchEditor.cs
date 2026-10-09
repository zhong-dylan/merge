using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameLaunch))]
public class GameLaunchEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var configName = serializedObject.FindProperty("configName");
        var configNames = AssetDatabase.FindAssets("t:LaunchConfig", new[] { "Assets/Settings" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(Path.GetFileNameWithoutExtension)
            .OrderBy(name => name)
            .ToArray();

        if (configNames.Length == 0)
        {
            EditorGUILayout.HelpBox("Add a LaunchConfig asset to Assets/Settings and register its Addressables address as LaunchConfigs/<name>.", MessageType.Warning);
        }
        else
        {
            var index = Array.IndexOf(configNames, configName.stringValue);
            EditorGUI.BeginChangeCheck();
            var selected = EditorGUILayout.Popup("Launch Config", index, configNames);
            if (EditorGUI.EndChangeCheck() && selected >= 0)
            {
                configName.stringValue = configNames[selected];
            }

            if (index < 0)
            {
                EditorGUILayout.HelpBox($"Launch config not found: {configName.stringValue}", MessageType.Warning);
            }
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("userName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("loginPrefab"));
        serializedObject.ApplyModifiedProperties();
    }
}

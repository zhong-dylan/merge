using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameLaunch))]
public class GameLaunchEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var launchConfigProperty = serializedObject.FindProperty("launchConfig");
        var selectedServerIndexProperty = serializedObject.FindProperty("selectedServerIndex");

        EditorGUILayout.PropertyField(launchConfigProperty);

        var launchConfig = launchConfigProperty.objectReferenceValue as LaunchConfig;
        if (launchConfig == null || launchConfig.Servers == null || launchConfig.Servers.Count == 0)
        {
            selectedServerIndexProperty.intValue = 0;
            EditorGUILayout.HelpBox("Assign a LaunchConfig with at least one server entry.", MessageType.Warning);
        }
        else
        {
            var options = launchConfig.Servers
                .Select((server, index) =>
                {
                    if (!string.IsNullOrWhiteSpace(server.modeName))
                    {
                        return server.modeName;
                    }

                    if (!string.IsNullOrWhiteSpace(server.serverAddress))
                    {
                        return server.serverAddress;
                    }

                    return $"server-{index}";
                })
                .ToArray();

            var selectedIndex = Mathf.Clamp(selectedServerIndexProperty.intValue, 0, options.Length - 1);
            selectedServerIndexProperty.intValue = EditorGUILayout.Popup("Selected Server", selectedIndex, options);
        }

        serializedObject.ApplyModifiedProperties();
    }
}

using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LaunchConfig))]
public class LaunchConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("serverKey"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("userName"));

        var serverOptionsProperty = serializedObject.FindProperty("serverOptions");
        var selectedServerIndexProperty = serializedObject.FindProperty("selectedServerIndex");

        EditorGUILayout.PropertyField(serverOptionsProperty, true);

        if (serverOptionsProperty.arraySize > 0)
        {
            var options = Enumerable.Range(0, serverOptionsProperty.arraySize)
                .Select(index =>
                {
                    var option = serverOptionsProperty.GetArrayElementAtIndex(index);
                    var displayName = option.FindPropertyRelative("displayName").stringValue;
                    var address = option.FindPropertyRelative("address").stringValue;
                    return string.IsNullOrWhiteSpace(displayName) ? address : $"{displayName} ({address})";
                })
                .ToArray();

            var selectedIndex = Mathf.Clamp(selectedServerIndexProperty.intValue, 0, serverOptionsProperty.arraySize - 1);
            selectedServerIndexProperty.intValue = EditorGUILayout.Popup("Selected Server", selectedIndex, options);
        }
        else
        {
            selectedServerIndexProperty.intValue = 0;
            EditorGUILayout.HelpBox("Add at least one server address.", MessageType.Warning);
        }

        serializedObject.ApplyModifiedProperties();
    }
}

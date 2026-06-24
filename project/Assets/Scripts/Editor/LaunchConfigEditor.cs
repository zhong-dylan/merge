using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LaunchConfig))]
public class LaunchConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("enableLogger"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("userName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("servers"), new GUIContent("Servers"), true);

        serializedObject.ApplyModifiedProperties();
    }
}

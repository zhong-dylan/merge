using UnityEditor;

[CustomEditor(typeof(GameLaunch))]
public class GameLaunchEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("enableLogger"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("userName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("selectedServerMode"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("serverVersion"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("localHost"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("devHost"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("releaseHost"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("serverKey"));

        serializedObject.ApplyModifiedProperties();
    }
}

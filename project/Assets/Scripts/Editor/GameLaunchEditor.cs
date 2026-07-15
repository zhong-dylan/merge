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

        serializedObject.ApplyModifiedProperties();
    }
}

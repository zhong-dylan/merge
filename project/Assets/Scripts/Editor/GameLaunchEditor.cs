using UnityEditor;

[CustomEditor(typeof(GameLaunch))]
public class GameLaunchEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("launchConfig"), true);

        serializedObject.ApplyModifiedProperties();
    }
}

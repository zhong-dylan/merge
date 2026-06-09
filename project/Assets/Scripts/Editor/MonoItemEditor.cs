using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(MonoItem), true)]
public class MonoItemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var monoItem = target as MonoItem;
        if (monoItem == null)
        {
            return;
        }

        if (!Application.isPlaying)
        {
            RefreshNodes(monoItem);
        }

        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Auto Collected Nodes", EditorStyles.boldLabel);

        DrawMap("Buttons", monoItem.Buttons);
        DrawMap("Images", monoItem.Images);
        DrawMap("Texts", monoItem.Texts);
        DrawMap("GameObjects", monoItem.GameObjects);
    }

    private static void RefreshNodes(MonoItem monoItem)
    {
        EnsureAutoTextComponents(monoItem);
        var method = typeof(MonoItem).GetMethod("CollectNodes", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        method?.Invoke(monoItem, null);
        EditorUtility.SetDirty(monoItem);
    }

    private static void EnsureAutoTextComponents(MonoItem monoItem)
    {
        var texts = monoItem.GetComponentsInChildren<TMP_Text>(true);
        for (var i = 0; i < texts.Length; i++)
        {
            var current = texts[i];
            if (current == null || current.transform == monoItem.transform)
            {
                continue;
            }

            if (current.GetComponentInParent<MonoItem>() != monoItem)
            {
                continue;
            }

            if (current.GetComponent<AutoText>() != null)
            {
                continue;
            }

            Undo.AddComponent<AutoText>(current.gameObject);
            EditorUtility.SetDirty(current.gameObject);
        }
    }

    private static void DrawMap<T>(string title, IReadOnlyDictionary<string, T> map) where T : Object
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"{title} ({map.Count})", EditorStyles.boldLabel);

        if (map.Count == 0)
        {
            EditorGUILayout.LabelField("None");
            return;
        }

        foreach (var pair in map)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.TextField(pair.Key, GUILayout.MaxWidth(180f));
                EditorGUILayout.ObjectField(pair.Value, typeof(T), true);
            }
        }
    }

    private static void DrawMap(string title, IReadOnlyDictionary<string, Component> map)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"{title} ({map.Count})", EditorStyles.boldLabel);

        if (map.Count == 0)
        {
            EditorGUILayout.LabelField("None");
            return;
        }

        foreach (var pair in map)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.TextField(pair.Key, GUILayout.MaxWidth(180f));
                EditorGUILayout.ObjectField(pair.Value, typeof(Component), true);
            }
        }
    }
}

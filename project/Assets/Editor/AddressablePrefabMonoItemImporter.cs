using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class AddressablePrefabMonoItemImporter : AssetPostprocessor
{
    private const string LocalPrefabRoot = "Assets/Addressables_Local/Prefabs";
    private const string RemotePrefabRoot = "Assets/Addressables_Remote/Prefabs";

    private static readonly HashSet<string> pendingPrefabPaths = new();

    [MenuItem("Tools/UI/Ensure MonoItem On Addressable Prefabs")]
    public static void EnsureAllAddressablePrefabs()
    {
        var prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { LocalPrefabRoot, RemotePrefabRoot })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(IsManagedPrefabPath)
            .Distinct()
            .ToArray();

        var changedCount = 0;
        for (var i = 0; i < prefabPaths.Length; i++)
        {
            if (EnsureMonoItem(prefabPaths[i]))
            {
                changedCount++;
            }
        }

        if (changedCount > 0)
        {
            AssetDatabase.SaveAssets();
        }

        Debug.Log($"MonoItem checked for {prefabPaths.Length} addressable prefabs. Added: {changedCount}.");
    }

    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        AddPendingPrefabPaths(importedAssets);
        AddPendingPrefabPaths(movedAssets);

        if (pendingPrefabPaths.Count == 0)
        {
            return;
        }

        EditorApplication.delayCall -= EnsurePendingPrefabs;
        EditorApplication.delayCall += EnsurePendingPrefabs;
    }

    private static void AddPendingPrefabPaths(IEnumerable<string> assetPaths)
    {
        foreach (var assetPath in assetPaths)
        {
            if (IsManagedPrefabPath(assetPath))
            {
                pendingPrefabPaths.Add(assetPath);
            }
        }
    }

    private static void EnsurePendingPrefabs()
    {
        var prefabPaths = pendingPrefabPaths.ToArray();
        pendingPrefabPaths.Clear();

        var changed = false;
        for (var i = 0; i < prefabPaths.Length; i++)
        {
            changed |= EnsureMonoItem(prefabPaths[i]);
        }

        if (changed)
        {
            AssetDatabase.SaveAssets();
        }
    }

    private static bool IsManagedPrefabPath(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return false;
        }

        return assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)
            && (assetPath.StartsWith(LocalPrefabRoot, StringComparison.OrdinalIgnoreCase)
                || assetPath.StartsWith(RemotePrefabRoot, StringComparison.OrdinalIgnoreCase));
    }

    private static bool EnsureMonoItem(string prefabPath)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
        {
            return false;
        }

        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            if (root == null || root.GetComponent<MonoItem>() != null)
            {
                return false;
            }

            root.AddComponent<MonoItem>();
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log($"Added MonoItem to prefab: {prefabPath}", root);
            return true;
        }
        finally
        {
            if (root != null)
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}

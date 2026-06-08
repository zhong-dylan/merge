using System.Linq;
using UnityEditor;

public class AddressablesGroupAutoSync : AssetPostprocessor
{
    private static readonly System.Collections.Generic.HashSet<string> pendingPaths = new();

    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (!HasManagedAssetChange(importedAssets)
            && !HasManagedAssetChange(deletedAssets)
            && !HasManagedAssetChange(movedAssets)
            && !HasManagedAssetChange(movedFromAssetPaths))
        {
            return;
        }

        var changedPaths = importedAssets
            .Concat(deletedAssets)
            .Concat(movedAssets)
            .Concat(movedFromAssetPaths)
            .Where(AddressablesGroupBuilder.IsManagedPath)
            .Distinct();

        foreach (var path in changedPaths)
        {
            pendingPaths.Add(path);
        }

        EditorApplication.delayCall -= DelayedSync;
        EditorApplication.delayCall += DelayedSync;
    }

    private static void DelayedSync()
    {
        var paths = pendingPaths.ToArray();
        pendingPaths.Clear();
        AddressablesGroupBuilder.SyncChangedPaths(paths);
    }

    private static bool HasManagedAssetChange(string[] assetPaths)
    {
        return assetPaths.Any(AddressablesGroupBuilder.IsManagedPath);
    }
}

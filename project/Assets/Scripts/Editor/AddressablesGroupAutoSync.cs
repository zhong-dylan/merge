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
        var hasAddressableChange = HasManagedAssetChange(importedAssets)
            || HasManagedAssetChange(deletedAssets)
            || HasManagedAssetChange(movedAssets)
            || HasManagedAssetChange(movedFromAssetPaths);

        var hasAtlasChange = HasManagedAtlasChange(importedAssets)
            || HasManagedAtlasChange(deletedAssets)
            || HasManagedAtlasChange(movedAssets)
            || HasManagedAtlasChange(movedFromAssetPaths);

        if (!hasAddressableChange && !hasAtlasChange)
        {
            return;
        }

        var changedPaths = importedAssets
            .Concat(deletedAssets)
            .Concat(movedAssets)
            .Concat(movedFromAssetPaths)
            .Distinct()
            .ToArray();

        foreach (var path in changedPaths)
        {
            if (AddressablesGroupBuilder.IsManagedPath(path) || SpriteAtlasAutoBuilder.IsManagedAtlasPath(path))
            {
                pendingPaths.Add(path);
            }
        }

        EditorApplication.delayCall -= DelayedSync;
        EditorApplication.delayCall += DelayedSync;
    }

    private static void DelayedSync()
    {
        var paths = pendingPaths.ToArray();
        pendingPaths.Clear();
        SpriteAtlasAutoBuilder.SyncChangedPaths(paths);
        AddressablesGroupBuilder.SyncChangedPaths(paths);
    }

    private static bool HasManagedAssetChange(string[] assetPaths)
    {
        return assetPaths.Any(AddressablesGroupBuilder.IsManagedPath);
    }

    private static bool HasManagedAtlasChange(string[] assetPaths)
    {
        return assetPaths.Any(SpriteAtlasAutoBuilder.IsManagedAtlasPath);
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

public static class SpriteAtlasAutoBuilder
{
    private const int DefaultAtlasBudgetSize = 2048;

    private sealed class AtlasRule
    {
        public string SourceRoot;
        public string OutputRoot;
        public bool IncludeInBuild;
    }

    private static readonly AtlasRule[] Rules =
    {
        new()
        {
            SourceRoot = "Assets/Arts/Atlas_Local",
            OutputRoot = "Assets/Addressables_Local/Atlas",
            IncludeInBuild = true,
        },
        new()
        {
            SourceRoot = "Assets/Arts/Atlas_Remote",
            OutputRoot = "Assets/Addressables_Remote/Atlas",
            IncludeInBuild = false,
        },
    };

    [MenuItem("Tools/Atlas/Sync SpriteAtlases")]
    public static void SyncAll()
    {
        var validAtlasPaths = new HashSet<string>();
        foreach (var rule in Rules)
        {
            EnsureFolder(rule.OutputRoot);

            if (!AssetDatabase.IsValidFolder(rule.SourceRoot))
            {
                continue;
            }

            foreach (var folderPath in AssetDatabase.GetSubFolders(rule.SourceRoot))
            {
                var atlasPath = SyncFolder(rule, folderPath);
                if (!string.IsNullOrWhiteSpace(atlasPath))
                {
                    validAtlasPaths.Add(atlasPath);
                }
            }
        }

        RemoveUnusedAtlases(validAtlasPaths, Rules.Select(rule => rule.OutputRoot));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static bool IsManagedAtlasPath(string assetPath)
    {
        return Rules.Any(rule => assetPath.StartsWith(rule.SourceRoot, StringComparison.OrdinalIgnoreCase));
    }

    public static void SyncChangedPaths(IEnumerable<string> assetPaths)
    {
        if (assetPaths == null)
        {
            return;
        }

        var changedFolders = assetPaths
            .Select(GetManagedFolderPath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct()
            .ToArray();

        if (changedFolders.Length == 0)
        {
            return;
        }

        var validAtlasPaths = CollectValidAtlasPaths();

        foreach (var folderPath in changedFolders)
        {
            var rule = GetRule(folderPath);
            if (rule == null)
            {
                continue;
            }

            EnsureFolder(rule.OutputRoot);

            if (AssetDatabase.IsValidFolder(folderPath))
            {
                var atlasPath = SyncFolder(rule, folderPath);
                if (!string.IsNullOrWhiteSpace(atlasPath))
                {
                    validAtlasPaths.Add(atlasPath);
                }
            }
            else
            {
                RemoveAtlasForFolder(rule, folderPath);
            }
        }

        RemoveUnusedAtlases(validAtlasPaths, Rules.Select(rule => rule.OutputRoot));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static string SyncFolder(AtlasRule rule, string folderPath)
    {
        var folderName = Path.GetFileName(folderPath);
        if (string.IsNullOrWhiteSpace(folderName))
        {
            return null;
        }

        var atlasPath = $"{rule.OutputRoot}/{folderName}.spriteatlas";
        var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
        if (atlas == null)
        {
            atlas = new SpriteAtlas();
            ApplyDefaultSettings(atlas, rule.IncludeInBuild);
            AssetDatabase.CreateAsset(atlas, atlasPath);
        }
        else
        {
            ApplyDefaultSettings(atlas, rule.IncludeInBuild);
        }

        var packables = atlas.GetPackables().OfType<UnityEngine.Object>().ToArray();
        if (packables.Length > 0)
        {
            atlas.Remove(packables);
        }

        var folderObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folderPath);
        if (folderObject != null)
        {
            atlas.Add(new[] { folderObject });
        }

        WarnIfAtlasMayOverflow(folderPath, folderName);
        EditorUtility.SetDirty(atlas);
        SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
        return atlasPath;
    }

    private static void ApplyDefaultSettings(SpriteAtlas atlas, bool includeInBuild)
    {
        atlas.SetIncludeInBuild(includeInBuild);

        var packingSettings = atlas.GetPackingSettings();
        packingSettings.enableRotation = false;
        packingSettings.enableTightPacking = false;
        packingSettings.padding = 4;
        atlas.SetPackingSettings(packingSettings);

        var textureSettings = atlas.GetTextureSettings();
        textureSettings.readable = false;
        textureSettings.generateMipMaps = false;
        textureSettings.sRGB = true;
        textureSettings.filterMode = FilterMode.Bilinear;
        atlas.SetTextureSettings(textureSettings);

        var platformSettings = atlas.GetPlatformSettings("DefaultTexturePlatform");
        platformSettings.overridden = false;
        atlas.SetPlatformSettings(platformSettings);
    }

    private static void RemoveUnusedAtlases(HashSet<string> validAtlasPaths, IEnumerable<string> outputRoots)
    {
        foreach (var outputRoot in outputRoots)
        {
            if (!AssetDatabase.IsValidFolder(outputRoot))
            {
                continue;
            }

            var atlasGuids = AssetDatabase.FindAssets("t:SpriteAtlas", new[] { outputRoot });
            foreach (var guid in atlasGuids)
            {
                var atlasPath = AssetDatabase.GUIDToAssetPath(guid);
                if (validAtlasPaths.Contains(atlasPath))
                {
                    continue;
                }

                AssetDatabase.DeleteAsset(atlasPath);
            }
        }
    }

    private static void RemoveAtlasForFolder(AtlasRule rule, string folderPath)
    {
        var folderName = Path.GetFileName(folderPath);
        if (string.IsNullOrWhiteSpace(folderName))
        {
            return;
        }

        var atlasPath = $"{rule.OutputRoot}/{folderName}.spriteatlas";
        if (AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath) != null)
        {
            AssetDatabase.DeleteAsset(atlasPath);
        }
    }

    private static HashSet<string> CollectValidAtlasPaths()
    {
        var result = new HashSet<string>();
        foreach (var rule in Rules)
        {
            if (!AssetDatabase.IsValidFolder(rule.SourceRoot))
            {
                continue;
            }

            foreach (var folderPath in AssetDatabase.GetSubFolders(rule.SourceRoot))
            {
                result.Add($"{rule.OutputRoot}/{Path.GetFileName(folderPath)}.spriteatlas");
            }
        }
        return result;
    }

    private static string GetManagedFolderPath(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return null;
        }

        var normalizedPath = assetPath.Replace('\\', '/');
        var rule = GetRule(normalizedPath);
        if (rule == null)
        {
            return null;
        }

        var relativePath = normalizedPath.Substring(rule.SourceRoot.Length).Trim('/');
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var firstSegment = relativePath.Split('/')[0];
        return $"{rule.SourceRoot}/{firstSegment}";
    }

    private static AtlasRule GetRule(string assetPath)
    {
        var normalizedPath = assetPath.Replace('\\', '/');
        return Rules.FirstOrDefault(rule => normalizedPath.StartsWith(rule.SourceRoot, StringComparison.OrdinalIgnoreCase));
    }

    private static void WarnIfAtlasMayOverflow(string folderPath, string folderName)
    {
        var spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
        if (spriteGuids.Length == 0)
        {
            return;
        }

        long totalArea = 0;
        var maxWidth = 0;
        var maxHeight = 0;

        foreach (var guid in spriteGuids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                continue;
            }

            var rect = sprite.rect;
            var width = Mathf.RoundToInt(rect.width);
            var height = Mathf.RoundToInt(rect.height);

            maxWidth = Mathf.Max(maxWidth, width);
            maxHeight = Mathf.Max(maxHeight, height);
            totalArea += (long)width * height;
        }

        var budgetArea = (long)DefaultAtlasBudgetSize * DefaultAtlasBudgetSize;
        if (totalArea <= budgetArea && maxWidth <= DefaultAtlasBudgetSize && maxHeight <= DefaultAtlasBudgetSize)
        {
            return;
        }

        Debug.LogWarning(
            $"SpriteAtlas folder may exceed {DefaultAtlasBudgetSize}x{DefaultAtlasBudgetSize}: {folderName}\n" +
            $"Folder: {folderPath}\n" +
            $"Sprites: {spriteGuids.Length}\n" +
            $"Total Area: {totalArea}\n" +
            $"Max Sprite: {maxWidth}x{maxHeight}");
    }

    private static void EnsureFolder(string folderPath)
    {
        var segments = folderPath.Split('/');
        var current = segments[0];
        for (var i = 1; i < segments.Length; i++)
        {
            var next = $"{current}/{segments[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, segments[i]);
            }

            current = next;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

public static class AddressablesGroupBuilder
{
    private const string LocalRoot = "Assets/Addressables_Local";
    private const string RemoteRoot = "Assets/Addressables_Remote";
    private const string LocalPrefix = "local_";
    private const string RemotePrefix = "remote_";

    [MenuItem("Tools/Addressables/Sync Folder Groups")]
    public static void SyncFolderGroups()
    {
        var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
        if (settings == null)
        {
            Debug.LogError("Addressables settings not found. Open Addressables Groups once to initialize them.");
            return;
        }

        var validGroupNames = new HashSet<string>();

        SyncRoot(settings, LocalRoot, LocalPrefix, false, validGroupNames);
        SyncRoot(settings, RemoteRoot, RemotePrefix, true, validGroupNames);
        RemoveUnusedGroups(settings, validGroupNames);

        AssetDatabase.SaveAssets();

        Debug.Log("Addressables groups synced from Addressables_Local and Addressables_Remote.");
    }

    public static bool IsManagedPath(string assetPath)
    {
        return assetPath.StartsWith(LocalRoot, StringComparison.OrdinalIgnoreCase)
            || assetPath.StartsWith(RemoteRoot, StringComparison.OrdinalIgnoreCase);
    }

    public static void SyncChangedPaths(IEnumerable<string> assetPaths)
    {
        var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
        if (settings == null)
        {
            Debug.LogError("Addressables settings not found. Open Addressables Groups once to initialize them.");
            return;
        }

        var rootsToSync = assetPaths
            .Select(GetManagedFolderPath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct()
            .ToArray();

        if (rootsToSync.Length == 0)
        {
            return;
        }

        foreach (var folderPath in rootsToSync)
        {
            SyncSingleFolder(settings, folderPath);
        }

        RemoveUnusedGroups(settings, BuildValidGroupNames());
        AssetDatabase.SaveAssets();
    }

    private static void SyncRoot(AddressableAssetSettings settings, string rootPath, string prefix, bool isRemote, HashSet<string> validGroupNames)
    {
        if (!AssetDatabase.IsValidFolder(rootPath))
        {
            return;
        }

        var childFolders = AssetDatabase.GetSubFolders(rootPath);
        foreach (var folderPath in childFolders)
        {
            var folderName = Path.GetFileName(folderPath);
            var groupName = $"{prefix}{folderName}";
            validGroupNames.Add(groupName);
            var group = GetOrCreateGroup(settings, groupName, isRemote);
            SyncGroupEntries(settings, group, folderPath, folderName, isRemote);
        }
    }

    private static void SyncSingleFolder(AddressableAssetSettings settings, string folderPath)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            RemoveGroupForFolder(settings, folderPath);
            return;
        }

        var rootInfo = GetRootInfo(folderPath);
        if (!rootInfo.HasValue)
        {
            return;
        }

        var (prefix, isRemote) = rootInfo.Value;
        var folderName = Path.GetFileName(folderPath);
        var groupName = $"{prefix}{folderName}";
        var group = GetOrCreateGroup(settings, groupName, isRemote);
        SyncGroupEntries(settings, group, folderPath, folderName, isRemote);
    }

    private static void SyncGroupEntries(AddressableAssetSettings settings, AddressableAssetGroup group, string folderPath, string folderName, bool isRemote)
    {
        var assetPaths = CollectAssets(folderPath);
        var validGuids = new HashSet<string>();

        foreach (var assetPath in assetPaths)
        {
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrWhiteSpace(guid))
            {
                continue;
            }

            validGuids.Add(guid);
            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = BuildAddress(folderName, assetPath, isRemote);
        }

        var entries = group.entries.ToArray();
        foreach (var entry in entries)
        {
            if (validGuids.Contains(entry.guid))
            {
                continue;
            }

            settings.RemoveAssetEntry(entry.guid, false);
        }
    }

    private static void RemoveUnusedGroups(AddressableAssetSettings settings, HashSet<string> validGroupNames)
    {
        var groups = settings.groups
            .Where(group => group != null && IsManagedGroup(group.Name))
            .ToArray();

        foreach (var group in groups)
        {
            if (validGroupNames.Contains(group.Name))
            {
                continue;
            }

            settings.RemoveGroup(group);
        }
    }

    private static void RemoveGroupForFolder(AddressableAssetSettings settings, string folderPath)
    {
        var rootInfo = GetRootInfo(folderPath);
        if (!rootInfo.HasValue)
        {
            return;
        }

        var groupName = $"{rootInfo.Value.prefix}{Path.GetFileName(folderPath)}";
        var group = settings.FindGroup(groupName);
        if (group != null)
        {
            settings.RemoveGroup(group);
        }
    }

    private static HashSet<string> BuildValidGroupNames()
    {
        var result = new HashSet<string>();
        AddValidGroupNames(result, LocalRoot, LocalPrefix);
        AddValidGroupNames(result, RemoteRoot, RemotePrefix);
        return result;
    }

    private static void AddValidGroupNames(HashSet<string> result, string rootPath, string prefix)
    {
        if (!AssetDatabase.IsValidFolder(rootPath))
        {
            return;
        }

        foreach (var folderPath in AssetDatabase.GetSubFolders(rootPath))
        {
            result.Add($"{prefix}{Path.GetFileName(folderPath)}");
        }
    }

    private static string GetManagedFolderPath(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return null;
        }

        var normalizedPath = assetPath.Replace('\\', '/');
        foreach (var rootPath in new[] { LocalRoot, RemoteRoot })
        {
            if (!normalizedPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var relativePath = normalizedPath.Substring(rootPath.Length).Trim('/');
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return null;
            }

            var firstSegment = relativePath.Split('/')[0];
            return $"{rootPath}/{firstSegment}";
        }

        return null;
    }

    private static (string prefix, bool isRemote)? GetRootInfo(string folderPath)
    {
        var normalizedPath = folderPath.Replace('\\', '/');
        if (normalizedPath.StartsWith(LocalRoot, StringComparison.OrdinalIgnoreCase))
        {
            return (LocalPrefix, false);
        }

        if (normalizedPath.StartsWith(RemoteRoot, StringComparison.OrdinalIgnoreCase))
        {
            return (RemotePrefix, true);
        }

        return null;
    }

    private static bool IsManagedGroup(string groupName)
    {
        return groupName.StartsWith(LocalPrefix, StringComparison.OrdinalIgnoreCase)
            || groupName.StartsWith(RemotePrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string groupName, bool isRemote)
    {
        var group = settings.FindGroup(groupName);
        if (group != null)
        {
            ApplyGroupSchema(group, isRemote);
            return group;
        }

        group = settings.CreateGroup(groupName, false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
        ApplyGroupSchema(group, isRemote);
        return group;
    }

    private static void ApplyGroupSchema(AddressableAssetGroup group, bool isRemote)
    {
        var bundledSchema = group.GetSchema<BundledAssetGroupSchema>();
        if (bundledSchema != null)
        {
            bundledSchema.BuildPath.SetVariableByName(group.Settings, isRemote ? AddressableAssetSettings.kRemoteBuildPath : AddressableAssetSettings.kLocalBuildPath);
            bundledSchema.LoadPath.SetVariableByName(group.Settings, isRemote ? AddressableAssetSettings.kRemoteLoadPath : AddressableAssetSettings.kLocalLoadPath);
        }

        var contentUpdateSchema = group.GetSchema<ContentUpdateGroupSchema>();
        if (contentUpdateSchema != null)
        {
            contentUpdateSchema.StaticContent = !isRemote;
        }

        EditorUtility.SetDirty(group);
    }

    private static List<string> CollectAssets(string folderPath)
    {
        return AssetDatabase.FindAssets(string.Empty, new[] { folderPath })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(IsAddressableAssetFile)
            .ToList();
    }

    private static bool IsAddressableAssetFile(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
        {
            return false;
        }

        var fileName = Path.GetFileName(assetPath);
        if (fileName.StartsWith(".", StringComparison.Ordinal))
        {
            return false;
        }

        return !assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
            && !assetPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildAddress(string folderName, string assetPath, bool isRemote)
    {
        var assetName = Path.GetFileNameWithoutExtension(assetPath);
        var suffix = isRemote ? "_remote" : "_local";
        return $"{folderName}/{assetName}{suffix}";
    }
}

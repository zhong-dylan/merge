using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class PngquantTool
{
    private const string MenuPath = "Assets/Compress Images With pngquant";
    private static readonly string[] SourceExtensions = { ".png", ".jpg", ".jpeg" };
    private static readonly string[] JpegExtensions = { ".jpg", ".jpeg" };
    private static readonly string[] PngquantCandidates =
    {
        "/opt/homebrew/bin/pngquant",
        "/usr/local/bin/pngquant",
        "pngquant",
    };

    [MenuItem(MenuPath, false, 2000)]
    private static void CompressSelectedFolders()
    {
        var folders = GetSelectedFolders().ToArray();
        if (folders.Length == 0)
        {
            EditorUtility.DisplayDialog("pngquant", "Please select at least one folder in the Project window.", "OK");
            return;
        }

        var pngquantPath = ResolvePngquantPath();
        if (string.IsNullOrWhiteSpace(pngquantPath))
        {
            EditorUtility.DisplayDialog("pngquant", "pngquant was not found. Please install it first.", "OK");
            return;
        }

        var imagePaths = CollectImagePaths(folders).ToArray();
        if (imagePaths.Length == 0)
        {
            EditorUtility.DisplayDialog("pngquant", "No png/jpg/jpeg images were found under the selected folders.", "OK");
            return;
        }

        var convertedCount = 0;
        var compressedCount = 0;
        var skippedCount = 0;
        var failedFiles = new List<string>();

        try
        {
            for (var i = 0; i < imagePaths.Length; i++)
            {
                var sourceAssetPath = imagePaths[i];
                EditorUtility.DisplayProgressBar("pngquant", $"Processing {sourceAssetPath}", (float)(i + 1) / imagePaths.Length);

                try
                {
                    var currentAssetPath = sourceAssetPath;
                    if (IsJpegPath(currentAssetPath))
                    {
                        currentAssetPath = ConvertJpegToPng(currentAssetPath);
                        convertedCount++;
                    }

                    if (CompressPng(pngquantPath, currentAssetPath))
                    {
                        compressedCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }
                }
                catch (Exception exception)
                {
                    failedFiles.Add($"{sourceAssetPath}: {exception.Message}");
                    Debug.LogException(exception);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        var summary = $"Processed: {imagePaths.Length}\nConverted jpg/jpeg to png: {convertedCount}\nCompressed png: {compressedCount}\nSkipped: {skippedCount}";
        if (failedFiles.Count > 0)
        {
            summary += $"\nFailed: {failedFiles.Count}\n\n{string.Join("\n", failedFiles.Take(10))}";
        }

        EditorUtility.DisplayDialog("pngquant", summary, "OK");
    }

    [MenuItem(MenuPath, true)]
    private static bool ValidateCompressSelectedFolders()
    {
        return GetSelectedFolders().Any();
    }

    private static IEnumerable<string> GetSelectedFolders()
    {
        foreach (var guid in Selection.assetGUIDs)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                yield return assetPath;
            }
        }
    }

    private static IEnumerable<string> CollectImagePaths(IEnumerable<string> folders)
    {
        var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var folder in folders)
        {
            foreach (var guid in AssetDatabase.FindAssets(string.Empty, new[] { folder }))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(assetPath))
                {
                    continue;
                }

                var extension = Path.GetExtension(assetPath);
                if (SourceExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    results.Add(assetPath);
                }
            }
        }

        return results.OrderBy(path => path, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsJpegPath(string assetPath)
    {
        var extension = Path.GetExtension(assetPath);
        return JpegExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    private static string ConvertJpegToPng(string assetPath)
    {
        var fullSourcePath = ToFullPath(assetPath);
        var fullTargetPath = Path.ChangeExtension(fullSourcePath, ".png");
        var targetAssetPath = Path.ChangeExtension(assetPath, ".png");

        if (File.Exists(fullTargetPath))
        {
            throw new InvalidOperationException($"Target png already exists: {targetAssetPath}");
        }

        var bytes = File.ReadAllBytes(fullSourcePath);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!texture.LoadImage(bytes))
            {
                throw new InvalidOperationException("Failed to decode source image.");
            }

            var pngBytes = texture.EncodeToPNG();
            if (pngBytes == null || pngBytes.Length == 0)
            {
                throw new InvalidOperationException("Failed to encode png.");
            }

            File.WriteAllBytes(fullTargetPath, pngBytes);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(texture);
        }

        var sourceMetaPath = $"{fullSourcePath}.meta";
        var targetMetaPath = $"{fullTargetPath}.meta";
        if (File.Exists(sourceMetaPath))
        {
            File.Move(sourceMetaPath, targetMetaPath);
        }

        File.Delete(fullSourcePath);
        AssetDatabase.Refresh();
        return targetAssetPath;
    }

    private static bool CompressPng(string pngquantPath, string assetPath)
    {
        var fullPath = ToFullPath(assetPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Image file not found.", fullPath);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = pngquantPath,
            Arguments = $"--force --skip-if-larger --ext .png -- \"{fullPath}\"",
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            WorkingDirectory = Directory.GetCurrentDirectory(),
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start pngquant.");
        }

        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return process.ExitCode switch
        {
            0 => true,
            98 => false,
            _ => throw new InvalidOperationException(string.IsNullOrWhiteSpace(standardError) ? standardOutput : standardError),
        };
    }

    private static string ResolvePngquantPath()
    {
        foreach (var candidate in PngquantCandidates)
        {
            if (!candidate.Contains(Path.DirectorySeparatorChar) && !candidate.Contains(Path.AltDirectorySeparatorChar))
            {
                return candidate;
            }

            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string ToFullPath(string assetPath)
    {
        return Path.GetFullPath(assetPath);
    }
}

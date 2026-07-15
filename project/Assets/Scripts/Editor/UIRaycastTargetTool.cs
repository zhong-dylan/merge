using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class UIRaycastTargetTool
{
    [MenuItem("GameObject/UI/Disable Raycast Target Recursively", false, 49)]
    private static void DisableRaycastTargetInSelection()
    {
        var roots = Selection.gameObjects;
        if (roots == null || roots.Length == 0)
        {
            EditorUtility.DisplayDialog("Disable Raycast Target Recursively", "Please select at least one node in the Hierarchy.", "OK");
            return;
        }

        var graphics = CollectGraphics(roots);
        if (graphics.Count == 0)
        {
            EditorUtility.DisplayDialog("Disable Raycast Target Recursively", "No Graphic components were found on the selected node or its children.", "OK");
            return;
        }

        Undo.RecordObjects(graphics.ToArray(), "Disable Raycast Target Recursively");

        var changedCount = 0;
        foreach (var graphic in graphics)
        {
            if (!graphic.raycastTarget)
            {
                continue;
            }

            graphic.raycastTarget = false;
            EditorUtility.SetDirty(graphic);
            changedCount++;
        }

        EditorUtility.DisplayDialog(
            "Disable Raycast Target Recursively",
            changedCount > 0
                ? $"Disabled Raycast Target on {changedCount} component(s)."
                : "All matching components were already disabled.",
            "OK");
    }

    [MenuItem("GameObject/UI/Disable Raycast Target Recursively", true)]
    private static bool ValidateDisableRaycastTargetInSelection()
    {
        return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
    }

    private static List<Graphic> CollectGraphics(GameObject[] roots)
    {
        var result = new List<Graphic>();
        var visited = new HashSet<Graphic>();

        foreach (var root in roots)
        {
            if (root == null)
            {
                continue;
            }

            var graphics = root.GetComponentsInChildren<Graphic>(true);
            foreach (var graphic in graphics)
            {
                if (graphic == null || !visited.Add(graphic))
                {
                    continue;
                }

                result.Add(graphic);
            }
        }

        return result;
    }
}

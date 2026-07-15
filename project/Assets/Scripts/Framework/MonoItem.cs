using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MonoItem : MonoBehaviour
{
    private readonly Dictionary<string, Button> buttonMap = new();
    private readonly Dictionary<string, Image> imageMap = new();
    private readonly Dictionary<string, Component> textMap = new();
    private readonly Dictionary<string, GameObject> gameObjectMap = new();

    public IReadOnlyDictionary<string, Button> Buttons => buttonMap;
    public IReadOnlyDictionary<string, Image> Images => imageMap;
    public IReadOnlyDictionary<string, Component> Texts => textMap;
    public IReadOnlyDictionary<string, GameObject> GameObjects => gameObjectMap;

    protected virtual void Awake()
    {
        CollectNodes();
    }

    protected virtual void OnValidate()
    {
        CollectNodes();
    }

    protected virtual void OnDestroy()
    {
        Utils.ReleaseMonoItemState(this);
    }

    protected void CollectNodes()
    {
        buttonMap.Clear();
        imageMap.Clear();
        textMap.Clear();
        gameObjectMap.Clear();
        Utils.ClearMonoItemState(this);

        var transforms = GetComponentsInChildren<Transform>(true);
        foreach (var current in transforms)
        {
            if (current == transform)
            {
                continue;
            }

            if (Utils.IsUnderNestedMonoItem(current, transform))
            {
                continue;
            }

            var nodeName = current.name;

            if (nodeName.StartsWith("btn_", StringComparison.OrdinalIgnoreCase))
            {
                var button = current.GetComponent<Button>();
                if (button != null)
                {
                    AddNode(buttonMap, nodeName, button);
                }
                continue;
            }

            if (nodeName.StartsWith("img_", StringComparison.OrdinalIgnoreCase))
            {
                var image = current.GetComponent<Image>();
                if (image != null)
                {
                    AddNode(imageMap, nodeName, image);
                }
                continue;
            }

            if (nodeName.StartsWith("txt_", StringComparison.OrdinalIgnoreCase))
            {
                var tmpText = current.GetComponent<TMP_Text>();
                if (tmpText != null)
                {
                    AutoText.EnsureComponent(tmpText);
                    AddNode(textMap, nodeName, tmpText);
                    continue;
                }

                var text = current.GetComponent<Text>();
                if (text != null)
                {
                    AddNode(textMap, nodeName, text);
                }

                continue;
            }

            if (nodeName.StartsWith("go_", StringComparison.OrdinalIgnoreCase))
            {
                AddNode(gameObjectMap, nodeName, current.gameObject);
            }
        }
    }

    protected virtual void OnDuplicateNodeFound(string nodeName)
    {
        Logger.Warn($"Duplicate node name detected in {name}: {nodeName}", this);
    }

    private void AddNode<T>(Dictionary<string, T> map, string key, T value)
    {
        if (map.ContainsKey(key))
        {
            OnDuplicateNodeFound(key);
        }

        map[key] = value;
    }
}

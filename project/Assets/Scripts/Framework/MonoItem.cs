using System;
using System.Collections.Generic;
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

    protected void CollectNodes()
    {
        buttonMap.Clear();
        imageMap.Clear();
        textMap.Clear();
        gameObjectMap.Clear();

        var transforms = GetComponentsInChildren<Transform>(true);
        foreach (var current in transforms)
        {
            if (current == transform)
            {
                continue;
            }

            if (IsUnderNestedMonoItem(current))
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
                var text = current.GetComponent("TMP_Text") ?? current.GetComponent("Text");
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

    private bool IsUnderNestedMonoItem(Transform current)
    {
        var parent = current.parent;
        while (parent != null && parent != transform)
        {
            if (parent.GetComponent<MonoItem>() != null)
            {
                return true;
            }

            parent = parent.parent;
        }

        return false;
    }

    public Button GetButton(string name)
    {
        buttonMap.TryGetValue(name, out var button);
        return button;
    }

    public Button GetButton(int hash)
    {
        return GetByHash(buttonMap, hash);
    }

    public Image GetImage(string name)
    {
        imageMap.TryGetValue(name, out var image);
        return image;
    }

    public Image GetImage(int hash)
    {
        return GetByHash(imageMap, hash);
    }

    public T GetText<T>(string name) where T : Component
    {
        textMap.TryGetValue(name, out var text);
        return text as T;
    }

    public T GetText<T>(int hash) where T : Component
    {
        return GetByHash(textMap, hash) as T;
    }

    public GameObject GetGameObject(string name)
    {
        gameObjectMap.TryGetValue(name, out var node);
        return node;
    }

    public GameObject GetGameObject(int hash)
    {
        return GetByHash(gameObjectMap, hash);
    }

    private static T GetByHash<T>(IReadOnlyDictionary<string, T> map, int hash)
    {
        foreach (var pair in map)
        {
            if (Animator.StringToHash(pair.Key) == hash)
            {
                return pair.Value;
            }
        }

        return default;
    }
}

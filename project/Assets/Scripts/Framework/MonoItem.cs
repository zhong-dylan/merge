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
    private readonly Dictionary<string, UIPointerHandler> pointerHandlerMap = new();

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
        pointerHandlerMap.Clear();

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

    public void SetClick(string name, Action callback)
    {
        SetPointerClick(GetOrCreatePointerHandler(name), callback);
    }

    public void SetClick(int hash, Action callback)
    {
        SetPointerClick(GetOrCreatePointerHandler(hash), callback);
    }

    public void SetPress(string name, Action callback)
    {
        SetPointerPress(GetOrCreatePointerHandler(name), callback);
    }

    public void SetPress(int hash, Action callback)
    {
        SetPointerPress(GetOrCreatePointerHandler(hash), callback);
    }

    public void SetDown(string name, Action callback)
    {
        SetPointerDown(GetOrCreatePointerHandler(name), callback);
    }

    public void SetDown(int hash, Action callback)
    {
        SetPointerDown(GetOrCreatePointerHandler(hash), callback);
    }

    public void SetUp(string name, Action callback)
    {
        SetPointerUp(GetOrCreatePointerHandler(name), callback);
    }

    public void SetUp(int hash, Action callback)
    {
        SetPointerUp(GetOrCreatePointerHandler(hash), callback);
    }

    public void SetBeginDrag(string name, Action callback)
    {
        SetPointerBeginDrag(GetOrCreatePointerHandler(name), callback);
    }

    public void SetBeginDrag(int hash, Action callback)
    {
        SetPointerBeginDrag(GetOrCreatePointerHandler(hash), callback);
    }

    public void SetEndDrag(string name, Action callback)
    {
        SetPointerEndDrag(GetOrCreatePointerHandler(name), callback);
    }

    public void SetEndDrag(int hash, Action callback)
    {
        SetPointerEndDrag(GetOrCreatePointerHandler(hash), callback);
    }

    public void SetDrag(string name, Action<Vector2> callback)
    {
        SetPointerDrag(GetOrCreatePointerHandler(name), callback);
    }

    public void SetDrag(int hash, Action<Vector2> callback)
    {
        SetPointerDrag(GetOrCreatePointerHandler(hash), callback);
    }

    public UIPointerHandler GetPointerHandler(string name)
    {
        pointerHandlerMap.TryGetValue(name, out var handler);
        if (handler != null)
        {
            return handler;
        }

        var target = ResolvePointerTarget(name);
        if (target == null)
        {
            return null;
        }

        handler = target.GetComponent<UIPointerHandler>();
        if (handler != null)
        {
            pointerHandlerMap[name] = handler;
        }

        return handler;
    }

    public UIPointerHandler GetPointerHandler(int hash)
    {
        foreach (var pair in gameObjectMap)
        {
            if (Animator.StringToHash(pair.Key) == hash)
            {
                return GetPointerHandler(pair.Key);
            }
        }

        foreach (var pair in buttonMap)
        {
            if (Animator.StringToHash(pair.Key) == hash)
            {
                return GetPointerHandler(pair.Key);
            }
        }

        foreach (var pair in imageMap)
        {
            if (Animator.StringToHash(pair.Key) == hash)
            {
                return GetPointerHandler(pair.Key);
            }
        }

        foreach (var pair in textMap)
        {
            if (Animator.StringToHash(pair.Key) == hash)
            {
                return GetPointerHandler(pair.Key);
            }
        }

        return null;
    }

    private UIPointerHandler GetOrCreatePointerHandler(string name)
    {
        var handler = GetPointerHandler(name);
        if (handler != null)
        {
            return handler;
        }

        var target = ResolvePointerTarget(name);
        if (target == null)
        {
            Logger.Warn($"Pointer target not found: {name}", this);
            return null;
        }

        handler = target.GetComponent<UIPointerHandler>();
        if (handler == null)
        {
            handler = target.AddComponent<UIPointerHandler>();
        }

        pointerHandlerMap[name] = handler;
        return handler;
    }

    private UIPointerHandler GetOrCreatePointerHandler(int hash)
    {
        var handler = GetPointerHandler(hash);
        if (handler != null)
        {
            return handler;
        }

        Logger.Warn($"Pointer target not found by hash: {hash}", this);
        return null;
    }

    private GameObject ResolvePointerTarget(string name)
    {
        var button = GetButton(name);
        if (button != null)
        {
            return button.gameObject;
        }

        var image = GetImage(name);
        if (image != null)
        {
            return image.gameObject;
        }

        var text = GetText<Component>(name);
        if (text != null)
        {
            return text.gameObject;
        }

        return GetGameObject(name);
    }

    private static void SetPointerClick(UIPointerHandler handler, Action callback)
    {
        if (handler == null)
        {
            return;
        }

        handler.ClearClickListeners();
        if (callback != null)
        {
            handler.Clicked += callback;
        }
    }

    private static void SetPointerPress(UIPointerHandler handler, Action callback)
    {
        if (handler == null)
        {
            return;
        }

        handler.ClearPressListeners();
        if (callback != null)
        {
            handler.LongPressed += callback;
        }
    }

    private static void SetPointerDown(UIPointerHandler handler, Action callback)
    {
        if (handler == null)
        {
            return;
        }

        handler.ClearPointerDownListeners();
        if (callback != null)
        {
            handler.PointerDowned += callback;
        }
    }

    private static void SetPointerUp(UIPointerHandler handler, Action callback)
    {
        if (handler == null)
        {
            return;
        }

        handler.ClearPointerUpListeners();
        if (callback != null)
        {
            handler.PointerUpped += callback;
        }
    }

    private static void SetPointerBeginDrag(UIPointerHandler handler, Action callback)
    {
        if (handler == null)
        {
            return;
        }

        handler.ClearBeginDragListeners();
        if (callback != null)
        {
            handler.BeginDragged += callback;
        }
    }

    private static void SetPointerEndDrag(UIPointerHandler handler, Action callback)
    {
        if (handler == null)
        {
            return;
        }

        handler.ClearEndDragListeners();
        if (callback != null)
        {
            handler.EndDragged += callback;
        }
    }

    private static void SetPointerDrag(UIPointerHandler handler, Action<Vector2> callback)
    {
        if (handler == null)
        {
            return;
        }

        handler.ClearDragListeners();
        if (callback != null)
        {
            handler.Dragged += callback;
        }
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

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class MonoItem : MonoBehaviour
{
    // Track runtime image bindings so atlas/bg Addressables can be released when swapped or destroyed.
    private sealed class ImageBinding
    {
        public string AtlasName;
        public string SpriteName;
        public bool IsAtlasSprite;
    }

    private sealed class ImageMaterialState
    {
        public Material OriginalMaterial;
        public bool IsGray;
        public int RequestVersion;
    }

    private sealed class TextColorState
    {
        public Color OriginalColor;
        public bool IsGray;
    }

    private readonly Dictionary<string, Button> buttonMap = new();
    private readonly Dictionary<string, Image> imageMap = new();
    private readonly Dictionary<string, Component> textMap = new();
    private readonly Dictionary<string, GameObject> gameObjectMap = new();
    private readonly Dictionary<string, UIPointerHandler> pointerHandlerMap = new();
    private readonly Dictionary<string, ImageBinding> imageBindings = new();
    private readonly Dictionary<string, ImageMaterialState> imageMaterialStates = new();
    private readonly Dictionary<string, TextColorState> textColorStates = new();

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
        var bindingKeys = new List<string>(imageBindings.Keys);
        for (var i = 0; i < bindingKeys.Count; i++)
        {
            ReleaseImageBinding(bindingKeys[i]);
        }
    }

    protected void CollectNodes()
    {
        buttonMap.Clear();
        imageMap.Clear();
        textMap.Clear();
        gameObjectMap.Clear();
        pointerHandlerMap.Clear();
        imageBindings.Clear();
        imageMaterialStates.Clear();
        textColorStates.Clear();

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

    public void SetText(string name, string value)
    {
        var textComponent = GetText<Component>(name);
        if (textComponent == null)
        {
            Logger.Warn($"Text target not found: {name}", this);
            return;
        }

        SetTextComponentValue(textComponent, value);
    }

    public void SetText(int hash, string value)
    {
        var textComponent = GetText<Component>(hash);
        if (textComponent == null)
        {
            return;
        }

        SetTextComponentValue(textComponent, value);
    }

    public void SetTextFormat(string name, string format, params object[] args)
    {
        SetText(name, string.Format(format, args));
    }

    public void SetTextFormat(int hash, string format, params object[] args)
    {
        SetText(hash, string.Format(format, args));
    }

    public void SetTextColor(string name, Color color)
    {
        var graphic = GetText<Graphic>(name);
        if (graphic == null)
        {
            Logger.Warn($"Text target not found: {name}", this);
            return;
        }

        graphic.color = color;
    }

    public void SetTextColor(int hash, Color color)
    {
        var graphic = GetText<Graphic>(hash);
        if (graphic == null)
        {
            return;
        }

        graphic.color = color;
    }

    public void SetTextAlpha(string name, float alpha)
    {
        var graphic = GetText<Graphic>(name);
        if (graphic == null)
        {
            Logger.Warn($"Text target not found: {name}", this);
            return;
        }

        var color = graphic.color;
        color.a = Mathf.Clamp01(alpha);
        graphic.color = color;
    }

    public void SetTextAlpha(int hash, float alpha)
    {
        var graphic = GetText<Graphic>(hash);
        if (graphic == null)
        {
            return;
        }

        var color = graphic.color;
        color.a = Mathf.Clamp01(alpha);
        graphic.color = color;
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

    // Image helpers keep common UI setup at the MonoItem layer so views don't need to touch Image directly.
    public void SetImage(string name, string spriteName)
    {
        SetImageInternal(name, null, spriteName, false);
    }

    public void SetImage(string name, string spriteName, bool preserveAspect, bool setNativeSize)
    {
        SetImageInternal(name, null, spriteName, false, preserveAspect, setNativeSize);
    }

    public void SetImage(string name, string atlasName, string spriteName)
    {
        SetImageInternal(name, atlasName, spriteName, true);
    }

    public void SetImage(string name, string atlasName, string spriteName, bool preserveAspect, bool setNativeSize)
    {
        SetImageInternal(name, atlasName, spriteName, true, preserveAspect, setNativeSize);
    }

    public void SetImage(int hash, string spriteName)
    {
        var image = GetImage(hash);
        if (image == null)
        {
            return;
        }

        SetImage(image.name, spriteName);
    }

    public void SetImage(int hash, string spriteName, bool preserveAspect, bool setNativeSize)
    {
        var image = GetImage(hash);
        if (image == null)
        {
            return;
        }

        SetImage(image.name, spriteName, preserveAspect, setNativeSize);
    }

    public void SetImage(int hash, string atlasName, string spriteName)
    {
        var image = GetImage(hash);
        if (image == null)
        {
            return;
        }

        SetImage(image.name, atlasName, spriteName);
    }

    public void SetImage(int hash, string atlasName, string spriteName, bool preserveAspect, bool setNativeSize)
    {
        var image = GetImage(hash);
        if (image == null)
        {
            return;
        }

        SetImage(image.name, atlasName, spriteName, preserveAspect, setNativeSize);
    }

    public void SetImagePreserveAspect(string name, bool preserveAspect)
    {
        var image = GetImage(name);
        if (image == null)
        {
            Logger.Warn($"Image target not found: {name}", this);
            return;
        }

        image.preserveAspect = preserveAspect;
    }

    public void SetImagePreserveAspect(int hash, bool preserveAspect)
    {
        var image = GetImage(hash);
        if (image == null)
        {
            return;
        }

        image.preserveAspect = preserveAspect;
    }

    public void SetImageNativeSize(string name)
    {
        var image = GetImage(name);
        if (image == null)
        {
            Logger.Warn($"Image target not found: {name}", this);
            return;
        }

        if (image.sprite == null)
        {
            Logger.Warn($"Image sprite is null, cannot SetNativeSize: {name}", this);
            return;
        }

        image.SetNativeSize();
    }

    public void SetImageNativeSize(int hash)
    {
        var image = GetImage(hash);
        if (image == null)
        {
            return;
        }

        if (image.sprite == null)
        {
            Logger.Warn($"Image sprite is null, cannot SetNativeSize by hash: {hash}", this);
            return;
        }

        image.SetNativeSize();
    }

    public void SetImageColor(string name, Color color)
    {
        var image = GetImage(name);
        if (image == null)
        {
            Logger.Warn($"Image target not found: {name}", this);
            return;
        }

        image.color = color;
    }

    public void SetImageColor(int hash, Color color)
    {
        var image = GetImage(hash);
        if (image == null)
        {
            return;
        }

        image.color = color;
    }

    public void SetImageAlpha(string name, float alpha)
    {
        var image = GetImage(name);
        if (image == null)
        {
            Logger.Warn($"Image target not found: {name}", this);
            return;
        }

        var color = image.color;
        color.a = Mathf.Clamp01(alpha);
        image.color = color;
    }

    public void SetImageAlpha(int hash, float alpha)
    {
        var image = GetImage(hash);
        if (image == null)
        {
            return;
        }

        var color = image.color;
        color.a = Mathf.Clamp01(alpha);
        image.color = color;
    }

    public void SetImageGray(string name, bool isGray)
    {
        var image = GetImage(name);
        if (image == null)
        {
            Logger.Warn($"Image target not found: {name}", this);
            return;
        }

        SetImageGrayInternal(name, image, isGray);
    }

    public void SetImageGray(int hash, bool isGray)
    {
        var image = GetImage(hash);
        if (image == null)
        {
            return;
        }

        SetImageGrayInternal(image.name, image, isGray);
    }

    public void SetTextGray(string name, bool isGray)
    {
        var graphic = GetText<Graphic>(name);
        if (graphic == null)
        {
            Logger.Warn($"Text target not found: {name}", this);
            return;
        }

        SetTextGrayInternal(name, graphic, isGray);
    }

    public void SetTextGray(int hash, bool isGray)
    {
        var graphic = GetText<Graphic>(hash);
        if (graphic == null)
        {
            return;
        }

        SetTextGrayInternal(GetGraphicGrayKey(graphic), graphic, isGray);
    }

    public void SetButtonGray(string name, bool isGray)
    {
        var button = GetButton(name);
        if (button == null)
        {
            Logger.Warn($"Button target not found: {name}", this);
            return;
        }

        SetButtonGrayInternal(button, isGray);
    }

    public void SetButtonGray(int hash, bool isGray)
    {
        var button = GetButton(hash);
        if (button == null)
        {
            return;
        }

        SetButtonGrayInternal(button, isGray);
    }

    public void SetGray(string name, bool isGray)
    {
        var button = GetButton(name);
        if (button != null)
        {
            SetButtonGrayInternal(button, isGray);
            return;
        }

        var image = GetImage(name);
        if (image != null)
        {
            SetImageGrayInternal(GetGraphicGrayKey(image), image, isGray);
            return;
        }

        var graphic = GetText<Graphic>(name);
        if (graphic != null)
        {
            SetTextGrayInternal(GetGraphicGrayKey(graphic), graphic, isGray);
            return;
        }

        Logger.Warn($"Gray target not found: {name}", this);
    }

    public void SetGray(int hash, bool isGray)
    {
        var button = GetButton(hash);
        if (button != null)
        {
            SetButtonGrayInternal(button, isGray);
            return;
        }

        var image = GetImage(hash);
        if (image != null)
        {
            SetImageGrayInternal(GetGraphicGrayKey(image), image, isGray);
            return;
        }

        var graphic = GetText<Graphic>(hash);
        if (graphic != null)
        {
            SetTextGrayInternal(GetGraphicGrayKey(graphic), graphic, isGray);
        }
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

    private static void SetTextComponentValue(Component textComponent, string value)
    {
        if (textComponent is Text text)
        {
            text.text = value;
            return;
        }

        var textProperty = textComponent.GetType().GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
        if (textProperty != null && textProperty.CanWrite && textProperty.PropertyType == typeof(string))
        {
            textProperty.SetValue(textComponent, value);
        }
    }

    private void SetImageInternal(string imageName, string atlasName, string spriteName, bool isAtlasSprite)
    {
        SetImageInternal(imageName, atlasName, spriteName, isAtlasSprite, false, false);
    }

    private void SetImageInternal(string imageName, string atlasName, string spriteName, bool isAtlasSprite, bool preserveAspect, bool setNativeSize)
    {
        var image = GetImage(imageName);
        if (image == null)
        {
            Logger.Warn($"Image target not found: {imageName}", this);
            return;
        }

        ReleaseImageBinding(imageName);

        if (isAtlasSprite)
        {
            AtlasMgr.I.LoadSpriteFromAtlas(atlasName, spriteName, sprite =>
            {
                if (image == null)
                {
                    AtlasMgr.I.ReleaseAtlasSprite(atlasName);
                    return;
                }

                image.sprite = sprite;
                image.preserveAspect = preserveAspect;
                if (setNativeSize && sprite != null)
                {
                    image.SetNativeSize();
                }
                imageBindings[imageName] = new ImageBinding
                {
                    AtlasName = atlasName,
                    SpriteName = spriteName,
                    IsAtlasSprite = true,
                };
            });
            return;
        }

        AtlasMgr.I.LoadBgSprite(spriteName, sprite =>
        {
            if (image == null)
            {
                AtlasMgr.I.ReleaseBgSprite(spriteName);
                return;
            }

            image.sprite = sprite;
            image.preserveAspect = preserveAspect;
            if (setNativeSize && sprite != null)
            {
                image.SetNativeSize();
            }
            imageBindings[imageName] = new ImageBinding
            {
                SpriteName = spriteName,
                IsAtlasSprite = false,
            };
        });
    }

    private void SetImageGrayInternal(string imageName, Image image, bool isGray)
    {
        if (!imageMaterialStates.TryGetValue(imageName, out var state) || state == null)
        {
            state = new ImageMaterialState
            {
                OriginalMaterial = image.material,
                IsGray = false,
                RequestVersion = 0,
            };
            imageMaterialStates[imageName] = state;
        }

        state.RequestVersion++;
        var requestVersion = state.RequestVersion;

        if (isGray)
        {
            if (state.IsGray)
            {
                return;
            }

            state.OriginalMaterial = image.material;
            AtlasMgr.I.LoadGrayMaterial(material =>
            {
                if (image == null)
                {
                    AtlasMgr.I.ReleaseGrayMaterial();
                    return;
                }

                if (!imageMaterialStates.TryGetValue(imageName, out var latestState) || latestState == null || latestState.RequestVersion != requestVersion)
                {
                    AtlasMgr.I.ReleaseGrayMaterial();
                    return;
                }

                if (material == null)
                {
                    return;
                }

                image.material = material;
                latestState.IsGray = true;
            });
            return;
        }

        if (!state.IsGray)
        {
            return;
        }

        image.material = state.OriginalMaterial;
        state.IsGray = false;
        AtlasMgr.I.ReleaseGrayMaterial();
    }

    private void SetButtonGrayInternal(Button button, bool isGray)
    {
        var graphics = button.GetComponentsInChildren<Graphic>(true);
        for (var i = 0; i < graphics.Length; i++)
        {
            var graphic = graphics[i];
            if (graphic == null)
            {
                continue;
            }

            switch (graphic)
            {
                case Image image:
                    SetImageGrayInternal(GetGraphicGrayKey(image), image, isGray);
                    break;
                default:
                    SetTextGrayInternal(GetGraphicGrayKey(graphic), graphic, isGray);
                    break;
            }
        }
    }

    private void SetTextGrayInternal(string textName, Graphic graphic, bool isGray)
    {
        if (!textColorStates.TryGetValue(textName, out var state) || state == null)
        {
            state = new TextColorState
            {
                OriginalColor = graphic.color,
                IsGray = false,
            };
            textColorStates[textName] = state;
        }

        if (isGray)
        {
            if (!state.IsGray)
            {
                state.OriginalColor = graphic.color;
            }

            var color = state.OriginalColor;
            var gray = color.grayscale;
            graphic.color = new Color(gray, gray, gray, color.a);
            state.IsGray = true;
            return;
        }

        if (!state.IsGray)
        {
            return;
        }

        graphic.color = state.OriginalColor;
        state.IsGray = false;
    }

    private static string GetGraphicGrayKey(Graphic graphic)
    {
        return $"{graphic.name}#{graphic.GetInstanceID()}";
    }

    private void ReleaseImageBinding(string imageName)
    {
        if (!imageBindings.TryGetValue(imageName, out var binding) || binding == null)
        {
            return;
        }

        if (binding.IsAtlasSprite)
        {
            AtlasMgr.I.ReleaseAtlasSprite(binding.AtlasName);
        }
        else
        {
            AtlasMgr.I.ReleaseBgSprite(binding.SpriteName);
        }

        imageBindings.Remove(imageName);
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

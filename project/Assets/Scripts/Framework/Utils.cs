using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class Utils
{
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

    private sealed class MonoItemState
    {
        public readonly Dictionary<string, UIPointerHandler> PointerHandlers = new();
        public readonly Dictionary<string, ImageBinding> ImageBindings = new();
        public readonly Dictionary<string, ImageMaterialState> ImageMaterialStates = new();
        public readonly Dictionary<string, TextColorState> TextColorStates = new();
    }

    private static readonly Dictionary<MonoItem, MonoItemState> monoItemStates = new();

    private static MonoItemState GetMonoItemState(MonoItem item)
    {
        if (item == null)
        {
            return null;
        }

        if (!monoItemStates.TryGetValue(item, out var state) || state == null)
        {
            state = new MonoItemState();
            monoItemStates[item] = state;
        }

        return state;
    }

    public static void ClearMonoItemState(MonoItem item)
    {
        if (item == null || !monoItemStates.TryGetValue(item, out var state) || state == null)
        {
            return;
        }

        state.PointerHandlers.Clear();
        state.ImageBindings.Clear();
        state.ImageMaterialStates.Clear();
        state.TextColorStates.Clear();
    }

    public static void ReleaseMonoItemState(MonoItem item)
    {
        if (item == null || !monoItemStates.TryGetValue(item, out var state) || state == null)
        {
            return;
        }

        var bindingKeys = new List<string>(state.ImageBindings.Keys);
        for (var i = 0; i < bindingKeys.Count; i++)
        {
            ReleaseImageBinding(state, bindingKeys[i]);
        }

        monoItemStates.Remove(item);
    }

    public static bool IsUnderNestedMonoItem(Transform current, Transform root)
    {
        if (current == null || root == null)
        {
            return false;
        }

        var parent = current.parent;
        while (parent != null && parent != root)
        {
            if (parent.GetComponent<MonoItem>() != null)
            {
                return true;
            }

            parent = parent.parent;
        }

        return false;
    }

    public static T GetByHash<T>(IReadOnlyDictionary<string, T> map, int hash)
    {
        if (map == null)
        {
            return default;
        }

        foreach (var pair in map)
        {
            if (Animator.StringToHash(pair.Key) == hash)
            {
                return pair.Value;
            }
        }

        return default;
    }

    public static T GetByName<T>(IReadOnlyDictionary<string, T> map, string name)
    {
        if (map == null || string.IsNullOrEmpty(name))
        {
            return default;
        }

        map.TryGetValue(name, out var value);
        return value;
    }

    public static bool TryGetNameByHash<T>(IReadOnlyDictionary<string, T> map, int hash, out string name)
    {
        name = null;
        if (map == null)
        {
            return false;
        }

        foreach (var pair in map)
        {
            if (Animator.StringToHash(pair.Key) == hash)
            {
                name = pair.Key;
                return true;
            }
        }

        return false;
    }

    public static T GetText<T>(IReadOnlyDictionary<string, Component> map, string name) where T : Component
    {
        return GetByName(map, name) as T;
    }

    public static T GetText<T>(IReadOnlyDictionary<string, Component> map, int hash) where T : Component
    {
        return GetByHash(map, hash) as T;
    }

    public static Button GetButton(MonoItem item, string name)
    {
        return item == null ? null : GetByName(item.Buttons, name);
    }

    public static Button GetButton(MonoItem item, int hash)
    {
        return item == null ? null : GetByHash(item.Buttons, hash);
    }

    public static Image GetImage(MonoItem item, string name)
    {
        return item == null ? null : GetByName(item.Images, name);
    }

    public static Image GetImage(MonoItem item, int hash)
    {
        return item == null ? null : GetByHash(item.Images, hash);
    }

    public static T GetText<T>(MonoItem item, string name) where T : Component
    {
        return item == null ? null : GetText<T>(item.Texts, name);
    }

    public static T GetText<T>(MonoItem item, int hash) where T : Component
    {
        return item == null ? null : GetText<T>(item.Texts, hash);
    }

    public static GameObject GetGameObject(MonoItem item, string name)
    {
        return item == null ? null : GetByName(item.GameObjects, name);
    }

    public static GameObject GetGameObject(MonoItem item, int hash)
    {
        return item == null ? null : GetByHash(item.GameObjects, hash);
    }

    public static void SetTextComponentValue(Component textComponent, string value)
    {
        if (textComponent == null)
        {
            return;
        }

        AutoText.EnsureFont(textComponent);

        if (textComponent is Text text)
        {
            text.text = value;
            return;
        }

        if (textComponent is TMP_Text tmpText)
        {
            tmpText.text = value;
            return;
        }

        var textProperty = textComponent.GetType().GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
        if (textProperty != null && textProperty.CanWrite && textProperty.PropertyType == typeof(string))
        {
            textProperty.SetValue(textComponent, value);
        }
    }

    public static bool SetText<T>(IReadOnlyDictionary<string, Component> map, string name, string value) where T : Component
    {
        var textComponent = GetText<T>(map, name);
        if (textComponent == null)
        {
            return false;
        }

        SetTextComponentValue(textComponent, value);
        return true;
    }

    public static bool SetText<T>(IReadOnlyDictionary<string, Component> map, int hash, string value) where T : Component
    {
        var textComponent = GetText<T>(map, hash);
        if (textComponent == null)
        {
            return false;
        }

        SetTextComponentValue(textComponent, value);
        return true;
    }

    public static bool SetTextColor(IReadOnlyDictionary<string, Component> map, string name, Color color)
    {
        return SetGraphicColor(GetText<Graphic>(map, name), color);
    }

    public static bool SetTextColor(IReadOnlyDictionary<string, Component> map, int hash, Color color)
    {
        return SetGraphicColor(GetText<Graphic>(map, hash), color);
    }

    public static bool SetTextAlpha(IReadOnlyDictionary<string, Component> map, string name, float alpha)
    {
        return SetGraphicAlpha(GetText<Graphic>(map, name), alpha);
    }

    public static bool SetTextAlpha(IReadOnlyDictionary<string, Component> map, int hash, float alpha)
    {
        return SetGraphicAlpha(GetText<Graphic>(map, hash), alpha);
    }

    public static void SetText(MonoItem item, string name, string value)
    {
        if (item == null)
        {
            return;
        }

        if (!SetText<Component>(item.Texts, name, value))
        {
            Logger.Warn($"Text target not found: {name}", item);
        }
    }

    public static void SetText(MonoItem item, int hash, string value)
    {
        if (item == null)
        {
            return;
        }

        SetText<Component>(item.Texts, hash, value);
    }

    public static void SetTextFormat(MonoItem item, string name, string format, params object[] args)
    {
        SetText(item, name, string.Format(format, args));
    }

    public static void SetTextFormat(MonoItem item, int hash, string format, params object[] args)
    {
        SetText(item, hash, string.Format(format, args));
    }

    public static void SetTextColor(MonoItem item, string name, Color color)
    {
        if (item == null)
        {
            return;
        }

        if (!SetTextColor(item.Texts, name, color))
        {
            Logger.Warn($"Text target not found: {name}", item);
        }
    }

    public static void SetTextColor(MonoItem item, int hash, Color color)
    {
        if (item == null)
        {
            return;
        }

        SetTextColor(item.Texts, hash, color);
    }

    public static void SetTextAlpha(MonoItem item, string name, float alpha)
    {
        if (item == null)
        {
            return;
        }

        if (!SetTextAlpha(item.Texts, name, alpha))
        {
            Logger.Warn($"Text target not found: {name}", item);
        }
    }

    public static void SetTextAlpha(MonoItem item, int hash, float alpha)
    {
        if (item == null)
        {
            return;
        }

        SetTextAlpha(item.Texts, hash, alpha);
    }

    public static bool SetGraphicColor(Graphic graphic, Color color)
    {
        if (graphic == null)
        {
            return false;
        }

        graphic.color = color;
        return true;
    }

    public static bool SetGraphicAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null)
        {
            return false;
        }

        var color = graphic.color;
        color.a = Mathf.Clamp01(alpha);
        graphic.color = color;
        return true;
    }

    public static bool SetImagePreserveAspect(Image image, bool preserveAspect)
    {
        if (image == null)
        {
            return false;
        }

        image.preserveAspect = preserveAspect;
        return true;
    }

    public static bool SetImageNativeSize(Image image)
    {
        if (image == null || image.sprite == null)
        {
            return false;
        }

        image.SetNativeSize();
        return true;
    }

    public static bool SetImagePreserveAspect(IReadOnlyDictionary<string, Image> map, string name, bool preserveAspect)
    {
        return SetImagePreserveAspect(GetByName(map, name), preserveAspect);
    }

    public static bool SetImagePreserveAspect(IReadOnlyDictionary<string, Image> map, int hash, bool preserveAspect)
    {
        return SetImagePreserveAspect(GetByHash(map, hash), preserveAspect);
    }

    public static bool SetImageNativeSize(IReadOnlyDictionary<string, Image> map, string name)
    {
        return SetImageNativeSize(GetByName(map, name));
    }

    public static bool SetImageNativeSize(IReadOnlyDictionary<string, Image> map, int hash)
    {
        return SetImageNativeSize(GetByHash(map, hash));
    }

    public static bool SetImageColor(IReadOnlyDictionary<string, Image> map, string name, Color color)
    {
        return SetGraphicColor(GetByName(map, name), color);
    }

    public static bool SetImageColor(IReadOnlyDictionary<string, Image> map, int hash, Color color)
    {
        return SetGraphicColor(GetByHash(map, hash), color);
    }

    public static bool SetImageAlpha(IReadOnlyDictionary<string, Image> map, string name, float alpha)
    {
        return SetGraphicAlpha(GetByName(map, name), alpha);
    }

    public static bool SetImageAlpha(IReadOnlyDictionary<string, Image> map, int hash, float alpha)
    {
        return SetGraphicAlpha(GetByHash(map, hash), alpha);
    }

    public static void SetImage(MonoItem item, string name, string spriteName)
    {
        SetImageInternal(item, name, null, spriteName, false, false, false);
    }

    public static void SetImage(MonoItem item, string name, string spriteName, bool preserveAspect, bool setNativeSize)
    {
        SetImageInternal(item, name, null, spriteName, false, preserveAspect, setNativeSize);
    }

    public static void SetImage(MonoItem item, string name, string atlasName, string spriteName)
    {
        SetImageInternal(item, name, atlasName, spriteName, true, false, false);
    }

    public static void SetImage(MonoItem item, string name, string atlasName, string spriteName, bool preserveAspect, bool setNativeSize)
    {
        SetImageInternal(item, name, atlasName, spriteName, true, preserveAspect, setNativeSize);
    }

    public static void SetImage(MonoItem item, int hash, string spriteName)
    {
        if (item == null || !TryGetNameByHash(item.Images, hash, out var imageName))
        {
            return;
        }

        SetImage(item, imageName, spriteName);
    }

    public static void SetImage(MonoItem item, int hash, string spriteName, bool preserveAspect, bool setNativeSize)
    {
        if (item == null || !TryGetNameByHash(item.Images, hash, out var imageName))
        {
            return;
        }

        SetImage(item, imageName, spriteName, preserveAspect, setNativeSize);
    }

    public static void SetImage(MonoItem item, int hash, string atlasName, string spriteName)
    {
        if (item == null || !TryGetNameByHash(item.Images, hash, out var imageName))
        {
            return;
        }

        SetImage(item, imageName, atlasName, spriteName);
    }

    public static void SetImage(MonoItem item, int hash, string atlasName, string spriteName, bool preserveAspect, bool setNativeSize)
    {
        if (item == null || !TryGetNameByHash(item.Images, hash, out var imageName))
        {
            return;
        }

        SetImage(item, imageName, atlasName, spriteName, preserveAspect, setNativeSize);
    }

    public static void SetImagePreserveAspect(MonoItem item, string name, bool preserveAspect)
    {
        if (item == null)
        {
            return;
        }

        if (!SetImagePreserveAspect(item.Images, name, preserveAspect))
        {
            Logger.Warn($"Image target not found: {name}", item);
        }
    }

    public static void SetImagePreserveAspect(MonoItem item, int hash, bool preserveAspect)
    {
        if (item == null)
        {
            return;
        }

        SetImagePreserveAspect(item.Images, hash, preserveAspect);
    }

    public static void SetImageNativeSize(MonoItem item, string name)
    {
        var image = GetImage(item, name);
        if (image == null)
        {
            Logger.Warn($"Image target not found: {name}", item);
            return;
        }

        if (!SetImageNativeSize(image))
        {
            Logger.Warn($"Image sprite is null, cannot SetNativeSize: {name}", item);
        }
    }

    public static void SetImageNativeSize(MonoItem item, int hash)
    {
        var image = GetImage(item, hash);
        if (image == null)
        {
            return;
        }

        if (!SetImageNativeSize(image))
        {
            Logger.Warn($"Image sprite is null, cannot SetNativeSize by hash: {hash}", item);
        }
    }

    public static void SetImageColor(MonoItem item, string name, Color color)
    {
        if (item == null)
        {
            return;
        }

        if (!SetImageColor(item.Images, name, color))
        {
            Logger.Warn($"Image target not found: {name}", item);
        }
    }

    public static void SetImageColor(MonoItem item, int hash, Color color)
    {
        if (item == null)
        {
            return;
        }

        SetImageColor(item.Images, hash, color);
    }

    public static void SetImageAlpha(MonoItem item, string name, float alpha)
    {
        if (item == null)
        {
            return;
        }

        if (!SetImageAlpha(item.Images, name, alpha))
        {
            Logger.Warn($"Image target not found: {name}", item);
        }
    }

    public static void SetImageAlpha(MonoItem item, int hash, float alpha)
    {
        if (item == null)
        {
            return;
        }

        SetImageAlpha(item.Images, hash, alpha);
    }

    public static void SetImageGray(MonoItem item, string name, bool isGray)
    {
        var image = GetImage(item, name);
        if (image == null)
        {
            Logger.Warn($"Image target not found: {name}", item);
            return;
        }

        SetImageGrayInternal(item, name, image, isGray);
    }

    public static void SetImageGray(MonoItem item, int hash, bool isGray)
    {
        var image = GetImage(item, hash);
        if (image == null)
        {
            return;
        }

        SetImageGrayInternal(item, image.name, image, isGray);
    }

    public static void SetTextGray(MonoItem item, string name, bool isGray)
    {
        var graphic = GetText<Graphic>(item, name);
        if (graphic == null)
        {
            Logger.Warn($"Text target not found: {name}", item);
            return;
        }

        SetTextGrayInternal(item, name, graphic, isGray);
    }

    public static void SetTextGray(MonoItem item, int hash, bool isGray)
    {
        var graphic = GetText<Graphic>(item, hash);
        if (graphic == null)
        {
            return;
        }

        SetTextGrayInternal(item, GetGraphicGrayKey(graphic), graphic, isGray);
    }

    public static void SetButtonGray(MonoItem item, string name, bool isGray)
    {
        var button = GetButton(item, name);
        if (button == null)
        {
            Logger.Warn($"Button target not found: {name}", item);
            return;
        }

        SetButtonGrayInternal(item, button, isGray);
    }

    public static void SetButtonGray(MonoItem item, int hash, bool isGray)
    {
        var button = GetButton(item, hash);
        if (button == null)
        {
            return;
        }

        SetButtonGrayInternal(item, button, isGray);
    }

    public static void SetGray(MonoItem item, string name, bool isGray)
    {
        var button = GetButton(item, name);
        if (button != null)
        {
            SetButtonGrayInternal(item, button, isGray);
            return;
        }

        var image = GetImage(item, name);
        if (image != null)
        {
            SetImageGrayInternal(item, GetGraphicGrayKey(image), image, isGray);
            return;
        }

        var graphic = GetText<Graphic>(item, name);
        if (graphic != null)
        {
            SetTextGrayInternal(item, GetGraphicGrayKey(graphic), graphic, isGray);
            return;
        }

        Logger.Warn($"Gray target not found: {name}", item);
    }

    public static void SetGray(MonoItem item, int hash, bool isGray)
    {
        var button = GetButton(item, hash);
        if (button != null)
        {
            SetButtonGrayInternal(item, button, isGray);
            return;
        }

        var image = GetImage(item, hash);
        if (image != null)
        {
            SetImageGrayInternal(item, GetGraphicGrayKey(image), image, isGray);
            return;
        }

        var graphic = GetText<Graphic>(item, hash);
        if (graphic != null)
        {
            SetTextGrayInternal(item, GetGraphicGrayKey(graphic), graphic, isGray);
        }
    }

    public static string GetGraphicGrayKey(Graphic graphic)
    {
        return graphic == null ? string.Empty : $"{graphic.name}#{graphic.GetInstanceID()}";
    }

    public static void SetPointerClick(UIPointerHandler handler, Action callback)
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

    public static void SetPointerPress(UIPointerHandler handler, Action callback)
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

    public static void SetPointerDown(UIPointerHandler handler, Action callback)
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

    public static void SetPointerUp(UIPointerHandler handler, Action callback)
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

    public static void SetPointerBeginDrag(UIPointerHandler handler, Action callback)
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

    public static void SetPointerEndDrag(UIPointerHandler handler, Action callback)
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

    public static void SetPointerDrag(UIPointerHandler handler, Action<Vector2> callback)
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

    public static UIPointerHandler GetPointerHandler(MonoItem item, string name)
    {
        var state = GetMonoItemState(item);
        if (state == null)
        {
            return null;
        }

        state.PointerHandlers.TryGetValue(name, out var handler);
        if (handler != null)
        {
            return handler;
        }

        var target = ResolvePointerTarget(item, name);
        if (target == null)
        {
            return null;
        }

        handler = target.GetComponent<UIPointerHandler>();
        if (handler != null)
        {
            state.PointerHandlers[name] = handler;
        }

        return handler;
    }

    public static UIPointerHandler GetPointerHandler(MonoItem item, int hash)
    {
        if (item == null)
        {
            return null;
        }

        if (TryGetNameByHash(item.GameObjects, hash, out var name) ||
            TryGetNameByHash(item.Buttons, hash, out name) ||
            TryGetNameByHash(item.Images, hash, out name) ||
            TryGetNameByHash(item.Texts, hash, out name))
        {
            return GetPointerHandler(item, name);
        }

        return null;
    }

    public static void SetClick(MonoItem item, string name, Action callback)
    {
        SetPointerClick(GetOrCreatePointerHandler(item, name), callback);
    }

    public static void SetClick(MonoItem item, int hash, Action callback)
    {
        SetPointerClick(GetOrCreatePointerHandler(item, hash), callback);
    }

    public static void SetPress(MonoItem item, string name, Action callback)
    {
        SetPointerPress(GetOrCreatePointerHandler(item, name), callback);
    }

    public static void SetPress(MonoItem item, int hash, Action callback)
    {
        SetPointerPress(GetOrCreatePointerHandler(item, hash), callback);
    }

    public static void SetDown(MonoItem item, string name, Action callback)
    {
        SetPointerDown(GetOrCreatePointerHandler(item, name), callback);
    }

    public static void SetDown(MonoItem item, int hash, Action callback)
    {
        SetPointerDown(GetOrCreatePointerHandler(item, hash), callback);
    }

    public static void SetUp(MonoItem item, string name, Action callback)
    {
        SetPointerUp(GetOrCreatePointerHandler(item, name), callback);
    }

    public static void SetUp(MonoItem item, int hash, Action callback)
    {
        SetPointerUp(GetOrCreatePointerHandler(item, hash), callback);
    }

    public static void SetBeginDrag(MonoItem item, string name, Action callback)
    {
        SetPointerBeginDrag(GetOrCreatePointerHandler(item, name), callback);
    }

    public static void SetBeginDrag(MonoItem item, int hash, Action callback)
    {
        SetPointerBeginDrag(GetOrCreatePointerHandler(item, hash), callback);
    }

    public static void SetEndDrag(MonoItem item, string name, Action callback)
    {
        SetPointerEndDrag(GetOrCreatePointerHandler(item, name), callback);
    }

    public static void SetEndDrag(MonoItem item, int hash, Action callback)
    {
        SetPointerEndDrag(GetOrCreatePointerHandler(item, hash), callback);
    }

    public static void SetDrag(MonoItem item, string name, Action<Vector2> callback)
    {
        SetPointerDrag(GetOrCreatePointerHandler(item, name), callback);
    }

    public static void SetDrag(MonoItem item, int hash, Action<Vector2> callback)
    {
        SetPointerDrag(GetOrCreatePointerHandler(item, hash), callback);
    }

    private static UIPointerHandler GetOrCreatePointerHandler(MonoItem item, string name)
    {
        var state = GetMonoItemState(item);
        if (state == null)
        {
            return null;
        }

        var handler = GetPointerHandler(item, name);
        if (handler != null)
        {
            return handler;
        }

        var target = ResolvePointerTarget(item, name);
        if (target == null)
        {
            Logger.Warn($"Pointer target not found: {name}", item);
            return null;
        }

        handler = target.GetComponent<UIPointerHandler>();
        if (handler == null)
        {
            handler = target.AddComponent<UIPointerHandler>();
        }

        state.PointerHandlers[name] = handler;
        return handler;
    }

    private static UIPointerHandler GetOrCreatePointerHandler(MonoItem item, int hash)
    {
        var handler = GetPointerHandler(item, hash);
        if (handler != null)
        {
            return handler;
        }

        Logger.Warn($"Pointer target not found by hash: {hash}", item);
        return null;
    }

    private static GameObject ResolvePointerTarget(MonoItem item, string name)
    {
        var button = GetButton(item, name);
        if (button != null)
        {
            return button.gameObject;
        }

        var image = GetImage(item, name);
        if (image != null)
        {
            return image.gameObject;
        }

        var text = GetText<Component>(item, name);
        if (text != null)
        {
            return text.gameObject;
        }

        return GetGameObject(item, name);
    }

    private static void SetImageInternal(MonoItem item, string imageName, string atlasName, string spriteName, bool isAtlasSprite, bool preserveAspect, bool setNativeSize)
    {
        var image = GetImage(item, imageName);
        if (image == null)
        {
            Logger.Warn($"Image target not found: {imageName}", item);
            return;
        }

        var state = GetMonoItemState(item);
        if (state == null)
        {
            return;
        }

        ReleaseImageBinding(state, imageName);

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
                SetImagePreserveAspect(image, preserveAspect);
                if (setNativeSize && sprite != null)
                {
                    image.SetNativeSize();
                }
                state.ImageBindings[imageName] = new ImageBinding
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
            SetImagePreserveAspect(image, preserveAspect);
            if (setNativeSize && sprite != null)
            {
                image.SetNativeSize();
            }
            state.ImageBindings[imageName] = new ImageBinding
            {
                SpriteName = spriteName,
                IsAtlasSprite = false,
            };
        });
    }

    private static void SetImageGrayInternal(MonoItem item, string imageName, Image image, bool isGray)
    {
        var ownerState = GetMonoItemState(item);
        if (ownerState == null)
        {
            return;
        }

        if (!ownerState.ImageMaterialStates.TryGetValue(imageName, out var state) || state == null)
        {
            state = new ImageMaterialState
            {
                OriginalMaterial = image.material,
                IsGray = false,
                RequestVersion = 0,
            };
            ownerState.ImageMaterialStates[imageName] = state;
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

                if (!ownerState.ImageMaterialStates.TryGetValue(imageName, out var latestState) ||
                    latestState == null ||
                    latestState.RequestVersion != requestVersion)
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

    private static void SetButtonGrayInternal(MonoItem item, Button button, bool isGray)
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
                    SetImageGrayInternal(item, GetGraphicGrayKey(image), image, isGray);
                    break;
                default:
                    SetTextGrayInternal(item, GetGraphicGrayKey(graphic), graphic, isGray);
                    break;
            }
        }
    }

    private static void SetTextGrayInternal(MonoItem item, string textName, Graphic graphic, bool isGray)
    {
        var ownerState = GetMonoItemState(item);
        if (ownerState == null)
        {
            return;
        }

        if (!ownerState.TextColorStates.TryGetValue(textName, out var state) || state == null)
        {
            state = new TextColorState
            {
                OriginalColor = graphic.color,
                IsGray = false,
            };
            ownerState.TextColorStates[textName] = state;
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

    private static void ReleaseImageBinding(MonoItemState state, string imageName)
    {
        if (state == null || !state.ImageBindings.TryGetValue(imageName, out var binding) || binding == null)
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

        state.ImageBindings.Remove(imageName);
    }
}

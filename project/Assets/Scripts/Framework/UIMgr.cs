using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIMgr : MonoSingle<UIMgr>
{
    private const int ViewSortingOrderInterval = 100;

    private readonly Dictionary<UILayer, RectTransform> layerRoots = new();
    private readonly Dictionary<UILayer, List<ViewBase>> layerViews = new();
    private readonly Dictionary<Type, ViewBase> openedViews = new();

    private Loader loader;
    private RectTransform root;
    private Camera uiCamera;
    private Canvas rootCanvas;
    private CanvasScaler rootCanvasScaler;
    private GraphicRaycaster rootGraphicRaycaster;

    protected override void Init()
    {
        base.Init();
        EnsureLoader();
        EnsureRoot();
        EnsureDefaultLayers();
    }

    public void RegisterView(ViewBase view)
    {
        if (view == null)
        {
            return;
        }

        EnsureRoot();
        EnsureLayer(view.Layer);

        var layerRoot = layerRoots[view.Layer];
        if (view.transform.parent != layerRoot)
        {
            view.transform.SetParent(layerRoot, false);
        }

        if (!layerViews.TryGetValue(view.Layer, out var views))
        {
            views = new List<ViewBase>();
            layerViews[view.Layer] = views;
        }

        if (!views.Contains(view))
        {
            views.Add(view);
        }

        RefreshLayerOrders(view.Layer);
    }

    public void UnregisterView(ViewBase view)
    {
        if (view == null)
        {
            return;
        }

        if (!layerViews.TryGetValue(view.Layer, out var views))
        {
            return;
        }

        if (!views.Remove(view))
        {
            return;
        }

        if (openedViews.TryGetValue(view.GetType(), out var cachedView) && cachedView == view)
        {
            openedViews.Remove(view.GetType());
        }

        RefreshLayerOrders(view.Layer);
    }

    public void OpenView<T>(Action<T> onLoaded = null) where T : ViewBase
    {
        var existingView = GetView<T>();
        if (existingView != null)
        {
            existingView.gameObject.SetActive(true);
            RegisterView(existingView);
            onLoaded?.Invoke(existingView);
            return;
        }

        EnsureLoader();
        var prefabPath = GetPrefabPath<T>();
        if (string.IsNullOrWhiteSpace(prefabPath))
        {
            Logger.Error($"UI prefab path is empty for view type: {typeof(T).Name}");
            onLoaded?.Invoke(null);
            return;
        }

        loader.Load<GameObject>(prefabPath, prefab =>
        {
            if (prefab == null)
            {
                Logger.Error($"UI prefab load failed: {prefabPath}");
                onLoaded?.Invoke(null);
                return;
            }

            var prefabView = prefab.GetComponent<T>();
            var layer = prefabView != null ? prefabView.Layer : UILayer.Main;
            EnsureLayer(layer);

            var instance = Instantiate(prefab, layerRoots[layer], false);
            var view = instance.GetComponent<T>();
            if (view == null)
            {
                view = instance.AddComponent<T>();
            }

            openedViews[typeof(T)] = view;
            RegisterView(view);
            onLoaded?.Invoke(view);
        });
    }

    public T GetView<T>() where T : ViewBase
    {
        if (!openedViews.TryGetValue(typeof(T), out var view))
        {
            return null;
        }

        if (view == null)
        {
            openedViews.Remove(typeof(T));
            return null;
        }

        return view as T;
    }

    public void CloseView<T>() where T : ViewBase
    {
        var view = GetView<T>();
        if (view == null)
        {
            return;
        }

        CloseView(view);
    }

    public void CloseView(ViewBase view)
    {
        if (view == null)
        {
            return;
        }

        UnregisterView(view);
        openedViews.Remove(view.GetType());
        Destroy(view.gameObject);
    }

    private void EnsureLoader()
    {
        if (loader == null)
        {
            loader = GetComponent<Loader>();
            if (loader == null)
            {
                loader = gameObject.AddComponent<Loader>();
            }
        }
    }

    private void EnsureRoot()
    {
        if (root != null)
        {
            EnsureRootCanvasComponents();
            return;
        }

        var rootTransform = transform.Find("UIRoot");
        if (rootTransform == null)
        {
            var rootObject = new GameObject("UIRoot", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            rootObject.transform.SetParent(transform, false);
            root = rootObject.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
        }
        else
        {
            root = rootTransform as RectTransform;
        }

        EnsureRootCanvasComponents();
    }

    private void EnsureRootCanvasComponents()
    {
        if (root == null)
        {
            return;
        }

        if (rootCanvas == null)
        {
            rootCanvas = root.GetComponent<Canvas>();
            if (rootCanvas == null)
            {
                rootCanvas = root.gameObject.AddComponent<Canvas>();
            }
        }

        if (rootCanvasScaler == null)
        {
            rootCanvasScaler = root.GetComponent<CanvasScaler>();
            if (rootCanvasScaler == null)
            {
                rootCanvasScaler = root.gameObject.AddComponent<CanvasScaler>();
            }
        }

        if (rootGraphicRaycaster == null)
        {
            rootGraphicRaycaster = root.GetComponent<GraphicRaycaster>();
            if (rootGraphicRaycaster == null)
            {
                rootGraphicRaycaster = root.gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        rootCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        rootCanvas.worldCamera = ResolveUICamera();
        rootCanvas.planeDistance = 100f;
        rootCanvas.overrideSorting = false;

        rootCanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        rootCanvasScaler.referenceResolution = UIConst.DesignResolution;
        rootCanvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        rootCanvasScaler.matchWidthOrHeight = 0.5f;
    }

    private void EnsureDefaultLayers()
    {
        foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
        {
            EnsureLayer(layer);
        }
    }

    private void EnsureLayer(UILayer layer)
    {
        EnsureRoot();

        if (layerRoots.ContainsKey(layer))
        {
            return;
        }

        var layerName = layer.ToString();
        var layerTransform = root.Find(layerName);
        RectTransform layerRoot;

        if (layerTransform == null)
        {
            var layerObject = new GameObject(layerName, typeof(RectTransform));
            layerObject.transform.SetParent(root, false);
            layerRoot = layerObject.GetComponent<RectTransform>();
            layerRoot.anchorMin = Vector2.zero;
            layerRoot.anchorMax = Vector2.one;
            layerRoot.offsetMin = Vector2.zero;
            layerRoot.offsetMax = Vector2.zero;
        }
        else
        {
            layerRoot = layerTransform as RectTransform;
        }

        layerRoots[layer] = layerRoot;

        if (!layerViews.ContainsKey(layer))
        {
            layerViews[layer] = new List<ViewBase>();
        }
    }

    private void RefreshLayerOrders(UILayer layer)
    {
        if (!layerViews.TryGetValue(layer, out var views))
        {
            return;
        }

        uiCamera = ResolveUICamera();

        for (var i = views.Count - 1; i >= 0; i--)
        {
            if (views[i] == null)
            {
                views.RemoveAt(i);
            }
        }

        for (var i = 0; i < views.Count; i++)
        {
            views[i].ApplyCanvas(layer, i * ViewSortingOrderInterval, uiCamera);
        }
    }

    private Camera ResolveUICamera()
    {
        if (uiCamera != null)
        {
            return uiCamera;
        }

        uiCamera = Camera.main;
        if (uiCamera == null)
        {
            uiCamera = FindFirstObjectByType<Camera>();
        }

        return uiCamera;
    }

    private static string GetPrefabPath<T>() where T : ViewBase
    {
        var tempObject = new GameObject($"__{typeof(T).Name}_PrefabPath__");
        try
        {
            var tempView = tempObject.AddComponent<T>();
            return tempView.PrefabPath;
        }
        finally
        {
            DestroyImmediate(tempObject);
        }
    }
}

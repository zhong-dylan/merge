using UnityEngine;
using UnityEngine.UI;

public class ViewBase : ViewItem
{
    private Canvas canvas;
    private GraphicRaycaster graphicRaycaster;
    private Coroutine autoCloseCoroutine;
    private bool isOpened;

    public virtual UILayer Layer => UILayer.Main;
    public Canvas Canvas => canvas;
    public virtual string PrefabPath => string.Empty;
    protected virtual bool EnableAutoClose => true;
    protected virtual float AutoCloseDelaySeconds => 30f;

    public override void Bind(MonoItem item)
    {
        base.Bind(item);
        ApplyLayout();
        EnsureCanvas();
    }

    /// <summary>
    /// ui初始化完毕
    /// </summary>
    public void Open()
    {
        if (isOpened)
        {
            return;
        }

        isOpened = true;
        OpenUpdate();
        OnEvent();
        OnOpened();
        StartAutoClose();
    }

    public void Close()
    {
        if (!isOpened)
        {
            return;
        }

        isOpened = false;
        StopAutoClose();
        OnRemoveEvent();
        CloseUpdate();
    }

    public override void Dispose()
    {
        Close();
        base.Dispose();
        canvas = null;
        graphicRaycaster = null;
    }

    public void ApplyCanvas(UILayer sortingLayer, int sortingOrder, Camera targetCamera)
    {
        EnsureCanvas();

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = targetCamera;
        canvas.planeDistance = 100f;
        canvas.overrideSorting = true;
        canvas.sortingLayerName = sortingLayer.ToString();
        canvas.sortingOrder = sortingOrder;
    }

    private void EnsureCanvas()
    {
        if (Item == null)
        {
            return;
        }

        if (canvas == null)
        {
            canvas = Item.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = Item.gameObject.AddComponent<Canvas>();
            }
        }

        if (graphicRaycaster == null)
        {
            graphicRaycaster = Item.GetComponent<GraphicRaycaster>();
            if (graphicRaycaster == null)
            {
                graphicRaycaster = Item.gameObject.AddComponent<GraphicRaycaster>();
            }
        }
    }

    private void ApplyLayout()
    {
        if (Item == null)
        {
            return;
        }

        var rectTransform = transform as RectTransform;
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one;
            rectTransform.localPosition = Vector3.zero;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
        }

        var context = transform.Find("context") as RectTransform;
        if (context == null)
        {
            return;
        }
        context.localScale = Vector3.one;
        context.localPosition = Vector3.zero;
        context.anchorMin = new Vector2(0.5f, 0.5f);
        context.anchorMax = new Vector2(0.5f, 0.5f);
        context.pivot = new Vector2(0.5f, 0.5f);
        context.sizeDelta = Vector2.zero;
        context.anchoredPosition = Vector2.zero;
    }

    protected virtual void OnEvent()
    {
    }

    protected virtual void OnOpened()
    {
    }

    protected virtual void OnRemoveEvent()
    {
    }

    private void StartAutoClose()
    {
        if (!EnableAutoClose || AutoCloseDelaySeconds <= 0f)
        {
            return;
        }

        StopAutoClose();
        autoCloseCoroutine = TimeMgr.I.DelayCall(AutoCloseDelaySeconds, AutoClose);
    }

    private void StopAutoClose()
    {
        if (autoCloseCoroutine == null)
        {
            return;
        }

        if (TimeMgr.TryGet(out var timeMgr))
        {
            timeMgr.StopManagedCoroutine(autoCloseCoroutine);
        }
        autoCloseCoroutine = null;
    }

    private void AutoClose()
    {
        autoCloseCoroutine = null;
        if (gameObject == null || !gameObject.activeInHierarchy)
        {
            return;
        }

        if (UIMgr.TryGet(out var uiMgr))
        {
            uiMgr.CloseView(this);
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class ViewBase : MonoItem
{
    [SerializeField] private UILayer layer = UILayer.Main;

    private Canvas canvas;
    private GraphicRaycaster graphicRaycaster;
    private Coroutine autoCloseCoroutine;
    private bool isInited;

    public UILayer Layer => Enum.IsDefined(typeof(UILayer), layer) ? layer : UILayer.Main;
    public Canvas Canvas => canvas;
    public virtual string PrefabPath => string.Empty;
    protected virtual bool EnableAutoClose => true;
    protected virtual float AutoCloseDelaySeconds => 30f;

    protected override void Awake()
    {
        base.Awake();
        ApplyLayout();
        if (Application.isPlaying)
        {
            EnsureCanvas();
            TryInit();
        }
    }

    protected virtual void Reset()
    {
        ApplyLayout();
    }

    protected virtual void OnValidate()
    {
        ApplyLayout();
    }

    protected virtual void OnEnable()
    {
        if (Application.isPlaying)
        {
            UIMgr.I.RegisterView(this);
            TimeMgr.I.AddUpdate(HandleUpdate);
            OnEvent();
            StartAutoClose();
        }
    }

    protected virtual void OnDisable()
    {
        if (Application.isPlaying)
        {
            StopAutoClose();
            OnRemoveEvent();
            if (TimeMgr.TryGet(out var timeMgr))
            {
                timeMgr.RemoveUpdate(HandleUpdate);
            }

            if (UIMgr.TryGet(out var uiMgr))
            {
                uiMgr.UnregisterView(this);
            }
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        StopAutoClose();
        if (Application.isPlaying && TimeMgr.TryGet(out var timeMgr))
        {
            timeMgr.RemoveUpdate(HandleUpdate);
        }

        OnDestory();
    }

    public void SetLayer(UILayer targetLayer)
    {
        layer = targetLayer;
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
        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }
        }

        if (graphicRaycaster == null)
        {
            graphicRaycaster = GetComponent<GraphicRaycaster>();
            if (graphicRaycaster == null)
            {
                graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
            }
        }
    }

    private void ApplyLayout()
    {
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

    protected virtual void OnInit()
    {
    }

    protected virtual void OnEvent()
    {
    }

    protected virtual void OnRemoveEvent()
    {
    }

    protected virtual void OnUpdate(float deltaTime)
    {
    }

    protected virtual void OnDestory()
    {
    }

    private void TryInit()
    {
        if (isInited)
        {
            return;
        }

        isInited = true;
        OnInit();
    }

    private void HandleUpdate(float deltaTime)
    {
        OnUpdate(deltaTime);
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
        if (this == null || !gameObject.activeInHierarchy)
        {
            return;
        }

        if (UIMgr.TryGet(out var uiMgr))
        {
            uiMgr.CloseView(this);
        }
    }
}

using UnityEngine;

public class ViewItem
{
    private MonoItem monoItem;
    private bool isInited;

    public MonoItem Item => monoItem;
    public GameObject gameObject => monoItem != null ? monoItem.gameObject : null;
    public Transform transform => monoItem != null ? monoItem.transform : null;

    protected virtual bool EnableUpdate => false;

    public ViewItem()
    {
        TryInit();
    }

    public virtual void Bind(MonoItem item)
    {
        monoItem = item;
        OnBind();
    }

    public virtual void Dispose()
    {
        OnDestory();
        if (monoItem != null)
        {
            Utils.ReleaseMonoItemState(monoItem);
        }

        monoItem = null;
    }

    protected void OpenUpdate()
    {
        if (EnableUpdate)
        {
            TimeMgr.I.AddUpdate(HandleUpdate);
        }
    }

    protected void CloseUpdate()
    {
        if (EnableUpdate && TimeMgr.TryGet(out var timeMgr))
        {
            timeMgr.RemoveUpdate(HandleUpdate);
        }
    }

    protected virtual void OnBind()
    {
    }

    protected virtual void OnInit()
    {
    }

    protected virtual void OnDestory()
    {
    }

    protected virtual void OnUpdate(float deltaTime)
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
}

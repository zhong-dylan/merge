using UnityEngine;

public class ViewItem : MonoItem
{
    private bool isInited;
    private bool isDisposed;

    public MonoItem Item => this;

    protected virtual bool EnableUpdate => false;

    protected override void Awake()
    {
        base.Awake();
        TryInit();
    }

    protected override void OnDestroy()
    {
        Dispose();
        base.OnDestroy();
    }

    public virtual void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;
        CloseUpdate();
        OnDestory();
        Utils.ReleaseMonoItemState(this);
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

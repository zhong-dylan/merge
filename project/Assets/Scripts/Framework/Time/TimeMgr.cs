using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class TimeMgr : MonoSingle<TimeMgr>
{
    private readonly List<Action<float>> updateActions = new();
    private readonly List<Action<float>> pendingAddActions = new();
    private readonly List<Action<float>> pendingRemoveActions = new();
    private bool isUpdating;

    public void AddUpdate(Action<float> onUpdate)
    {
        if (onUpdate == null)
        {
            return;
        }

        if (isUpdating)
        {
            pendingAddActions.Add(onUpdate);
            return;
        }

        if (!updateActions.Contains(onUpdate))
        {
            updateActions.Add(onUpdate);
        }
    }

    public void RemoveUpdate(Action<float> onUpdate)
    {
        if (onUpdate == null)
        {
            return;
        }

        if (isUpdating)
        {
            pendingRemoveActions.Add(onUpdate);
            return;
        }

        updateActions.Remove(onUpdate);
    }

    public Coroutine RunCoroutine(IEnumerator routine)
    {
        if (routine == null)
        {
            return null;
        }

        return StartCoroutine(routine);
    }

    public void StopManagedCoroutine(Coroutine routine)
    {
        if (routine == null)
        {
            return;
        }

        StopCoroutine(routine);
    }

    public Coroutine RunCor(IEnumerator routine)
    {
        return RunCoroutine(routine);
    }

    public void StopCor(Coroutine routine)
    {
        StopManagedCoroutine(routine);
    }

    public Coroutine DelayCall(float delaySeconds, Action callback, bool useRealtime = false)
    {
        return StartCoroutine(DelayCoroutine(delaySeconds, callback, useRealtime));
    }

    private void Update()
    {
        isUpdating = true;

        var deltaTime = Time.deltaTime;
        for (var i = 0; i < updateActions.Count; i++)
        {
            updateActions[i]?.Invoke(deltaTime);
        }

        isUpdating = false;
        FlushPendingActions();
    }

    private IEnumerator DelayCoroutine(float delaySeconds, Action callback, bool useRealtime)
    {
        if (delaySeconds > 0f)
        {
            yield return useRealtime
                ? new WaitForSecondsRealtime(delaySeconds)
                : new WaitForSeconds(delaySeconds);
        }

        callback?.Invoke();
    }

    private void FlushPendingActions()
    {
        if (pendingRemoveActions.Count > 0)
        {
            for (var i = 0; i < pendingRemoveActions.Count; i++)
            {
                updateActions.Remove(pendingRemoveActions[i]);
            }

            pendingRemoveActions.Clear();
        }

        if (pendingAddActions.Count > 0)
        {
            for (var i = 0; i < pendingAddActions.Count; i++)
            {
                var action = pendingAddActions[i];
                if (!updateActions.Contains(action))
                {
                    updateActions.Add(action);
                }
            }

            pendingAddActions.Clear();
        }
    }
}

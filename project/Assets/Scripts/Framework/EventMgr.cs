using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class EventMgr : MonoSingle<EventMgr>
{
    private readonly Dictionary<GameEventType, Action> listeners = new();
    private readonly Dictionary<GameEventType, Delegate> payloadListeners = new();

    public void AddListener(GameEventType eventType, Action callback)
    {
        if (eventType == GameEventType.None || callback == null)
        {
            return;
        }

        listeners.TryGetValue(eventType, out var existing);
        listeners[eventType] = existing + callback;
    }

    public void RemoveListener(GameEventType eventType, Action callback)
    {
        if (eventType == GameEventType.None || callback == null)
        {
            return;
        }

        if (!listeners.TryGetValue(eventType, out var existing))
        {
            return;
        }

        existing -= callback;
        if (existing == null)
        {
            listeners.Remove(eventType);
            return;
        }

        listeners[eventType] = existing;
    }

    public void Dispatch(GameEventType eventType)
    {
        if (eventType == GameEventType.None)
        {
            return;
        }

        if (listeners.TryGetValue(eventType, out var callback))
        {
            callback?.Invoke();
        }
    }

    public void AddListener<T>(GameEventType eventType, Action<T> callback)
    {
        if (eventType == GameEventType.None || callback == null)
        {
            return;
        }

        if (payloadListeners.TryGetValue(eventType, out var existing) && existing is not Action<T>)
        {
            Logger.Warn($"Event payload type mismatch when adding listener: {eventType}", this);
            return;
        }

        payloadListeners.TryGetValue(eventType, out existing);
        var typed = existing as Action<T>;
        payloadListeners[eventType] = typed + callback;
    }

    public void RemoveListener<T>(GameEventType eventType, Action<T> callback)
    {
        if (eventType == GameEventType.None || callback == null)
        {
            return;
        }

        if (!payloadListeners.TryGetValue(eventType, out var existing) || existing is not Action<T> typed)
        {
            return;
        }

        typed -= callback;
        if (typed == null)
        {
            payloadListeners.Remove(eventType);
            return;
        }

        payloadListeners[eventType] = typed;
    }

    public void Dispatch<T>(GameEventType eventType, T payload)
    {
        if (eventType == GameEventType.None)
        {
            return;
        }

        if (!payloadListeners.TryGetValue(eventType, out var existing))
        {
            return;
        }

        if (existing is not Action<T> callback)
        {
            Logger.Warn($"Event payload type mismatch when dispatching: {eventType}", this);
            return;
        }

        callback.Invoke(payload);
    }

    public void Clear(GameEventType eventType)
    {
        listeners.Remove(eventType);
        payloadListeners.Remove(eventType);
    }

    public void ClearAll()
    {
        listeners.Clear();
        payloadListeners.Clear();
    }
}

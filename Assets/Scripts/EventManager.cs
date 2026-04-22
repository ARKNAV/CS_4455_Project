using System;
using System.Collections.Generic;
using UnityEngine;

public struct PlayerLandsEvent { }
public struct NoiseEmittedEvent { }
public struct SuspicionChangedEvent { }
public struct ZoneViolationEvent { }
public struct DisguiseChangedEvent { }
public struct MissionFailEvent { }
public struct GlobalChaseCascadeEvent { }

public static class EventManager
{
    private static readonly Dictionary<Type, Delegate> Listeners = new Dictionary<Type, Delegate>();

    public static void AddListener<TEvent, T1, T2>(Action<T1, T2> listener)
    {
        var eventType = typeof(TEvent);
        if (Listeners.TryGetValue(eventType, out var existing))
            Listeners[eventType] = Delegate.Combine(existing, listener);
        else
            Listeners[eventType] = listener;
    }

    public static void RemoveListener<TEvent, T1, T2>(Action<T1, T2> listener)
    {
        var eventType = typeof(TEvent);
        if (!Listeners.TryGetValue(eventType, out var existing)) return;

        var updated = Delegate.Remove(existing, listener);
        if (updated == null)
            Listeners.Remove(eventType);
        else
            Listeners[eventType] = updated;
    }

    public static void TriggerEvent<TEvent, T1, T2>(T1 arg1, T2 arg2)
    {
        var eventType = typeof(TEvent);
        if (Listeners.TryGetValue(eventType, out var existing))
        {
            if (existing is Action<T1, T2> callback)
                callback.Invoke(arg1, arg2);
            else
                Debug.LogWarning($"EventManager: Listener signature mismatch for {eventType.Name}.");
        }
    }

    public static void AddListener<TEvent>(Action listener)
    {
        var eventType = typeof(TEvent);
        if (Listeners.TryGetValue(eventType, out var existing))
            Listeners[eventType] = Delegate.Combine(existing, listener);
        else
            Listeners[eventType] = listener;
    }

    public static void RemoveListener<TEvent>(Action listener)
    {
        var eventType = typeof(TEvent);
        if (!Listeners.TryGetValue(eventType, out var existing)) return;

        var updated = Delegate.Remove(existing, listener);
        if (updated == null)
            Listeners.Remove(eventType);
        else
            Listeners[eventType] = updated;
    }

    public static void TriggerEvent<TEvent>()
    {
        var eventType = typeof(TEvent);
        if (Listeners.TryGetValue(eventType, out var existing))
        {
            if (existing is Action callback)
                callback.Invoke();
            else
                Debug.LogWarning($"EventManager: Listener signature mismatch for {eventType.Name}.");
        }
    }
}

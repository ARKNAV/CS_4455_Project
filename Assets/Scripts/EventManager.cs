using System;
using System.Collections.Generic;
using UnityEngine;

// ── Existing events ──
public struct PlayerLandsEvent { }
public struct NoiseEmittedEvent { }

// ── New disguise / suspicion events ──
/// <summary>Raised when suspicion changes. Args: suspicion delta, reason string.</summary>
public struct SuspicionChangedEvent { }

/// <summary>Raised when the player enters a zone they lack clearance for. Args: required clearance, zone name.</summary>
public struct ZoneViolationEvent { }

/// <summary>Raised when the player's disguise changes. Args: new clearance, old clearance.</summary>
public struct DisguiseChangedEvent { }

/// <summary>Raised when the player is captured / mission fails. Args: reason string, empty string.</summary>
public struct MissionFailEvent { }

public static class EventManager
{
    private static readonly Dictionary<Type, Delegate> Listeners = new Dictionary<Type, Delegate>();

    // ── Two-argument listeners ──

    public static void AddListener<TEvent, T1, T2>(Action<T1, T2> listener)
    {
        var eventType = typeof(TEvent);
        if (Listeners.TryGetValue(eventType, out var existing))
        {
            Listeners[eventType] = Delegate.Combine(existing, listener);
        }
        else
        {
            Listeners[eventType] = listener;
        }
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
            {
                callback.Invoke(arg1, arg2);
            }
            else
            {
                Debug.LogWarning($"EventManager: Listener signature mismatch for {eventType.Name}.");
            }
        }
    }

    // ── Zero-argument listeners ──

    public static void AddListener<TEvent>(Action listener)
    {
        var eventType = typeof(TEvent);
        if (Listeners.TryGetValue(eventType, out var existing))
        {
            Listeners[eventType] = Delegate.Combine(existing, listener);
        }
        else
        {
            Listeners[eventType] = listener;
        }
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
            {
                callback.Invoke();
            }
            else
            {
                Debug.LogWarning($"EventManager: Listener signature mismatch for {eventType.Name}.");
            }
        }
    }
}

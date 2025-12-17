using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace StatisticsAnalysisTool.Core.EventBus;

/// <summary>
/// Simple in-memory event bus implementation for decoupling services from UI
/// </summary>
public class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _subscriptions = new();
    private readonly object _lock = new();

    public void Publish<TEvent>(TEvent @event) where TEvent : class
    {
        if (@event == null)
        {
            return;
        }

        var eventType = typeof(TEvent);
        
        if (_subscriptions.TryGetValue(eventType, out var handlers))
        {
            // Create a copy to avoid modification during iteration
            List<Delegate> handlersCopy;
            lock (_lock)
            {
                handlersCopy = handlers.ToList();
            }

            foreach (var handler in handlersCopy)
            {
                try
                {
                    ((Action<TEvent>)handler)(@event);
                }
                catch (Exception)
                {
                    // Consider logging here, but don't let one handler crash others
                    // For now, swallow to prevent cascade failures
                }
            }
        }
    }

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        var eventType = typeof(TEvent);

        lock (_lock)
        {
            if (!_subscriptions.ContainsKey(eventType))
            {
                _subscriptions[eventType] = new List<Delegate>();
            }

            _subscriptions[eventType].Add(handler);
        }

        return new Subscription(() => Unsubscribe(eventType, handler));
    }

    private void Unsubscribe<TEvent>(Type eventType, Action<TEvent> handler)
    {
        lock (_lock)
        {
            if (_subscriptions.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(handler);
                
                if (handlers.Count == 0)
                {
                    _subscriptions.TryRemove(eventType, out _);
                }
            }
        }
    }

    private class Subscription : IDisposable
    {
        private readonly Action _unsubscribe;
        private bool _disposed;

        public Subscription(Action unsubscribe)
        {
            _unsubscribe = unsubscribe;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _unsubscribe?.Invoke();
                _disposed = true;
            }
        }
    }
}


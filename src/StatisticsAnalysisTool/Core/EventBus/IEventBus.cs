using System;

namespace StatisticsAnalysisTool.Core.EventBus;

/// <summary>
/// Event bus for pub/sub communication between services and UI layer
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publish an event to all subscribers
    /// </summary>
    void Publish<TEvent>(TEvent @event) where TEvent : class;

    /// <summary>
    /// Subscribe to events of a specific type
    /// </summary>
    /// <returns>Disposable subscription that can be used to unsubscribe</returns>
    IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
}


// Observer pattern - core contracts.
//
// The subject owns the state and knows nothing about who is watching it.
// Observers register themselves and get notified when the state changes.
//
// Named IEventObserver / IEventSubject on purpose, so they never clash with
// System.IObserver<T> when a file has `using System;`.

/// <summary>Implemented by anything that wants to be told about changes.</summary>
public interface IEventObserver<in T>
{
    /// <summary>Called by the subject every time it notifies its observers.</summary>
    void OnNotify(T data);
}

/// <summary>The part of a subject that observers are allowed to see.</summary>
public interface IEventSubject<T>
{
    void Subscribe(IEventObserver<T> observer);
    void Unsubscribe(IEventObserver<T> observer);
}

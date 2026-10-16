using System.Collections.Generic;

/// <summary>
/// Reusable observer list. Use it by composition inside any class (including MonoBehaviours):
/// keep a private Subject&lt;T&gt;, expose it as IEventSubject&lt;T&gt;, and call Notify() when your state changes.
/// </summary>
public class Subject<T> : IEventSubject<T>
{
    private readonly List<IEventObserver<T>> _observers = new List<IEventObserver<T>>();

    public int ObserverCount => _observers.Count;

    public void Subscribe(IEventObserver<T> observer)
    {
        // Ignore nulls and duplicates so an observer can never be notified twice.
        if (observer == null || _observers.Contains(observer))
            return;

        _observers.Add(observer);
    }

    public void Unsubscribe(IEventObserver<T> observer)
    {
        _observers.Remove(observer);
    }

    public void Notify(T data)
    {
        // Iterate backwards so an observer can safely unsubscribe itself inside OnNotify.
        for (int i = _observers.Count - 1; i >= 0; i--)
        {
            // Another observer may have removed several entries during this loop.
            if (i >= _observers.Count)
                continue;

            _observers[i].OnNotify(data);
        }
    }

    public void Clear()
    {
        _observers.Clear();
    }
}

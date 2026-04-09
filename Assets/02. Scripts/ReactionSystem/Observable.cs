using System;
using System.Collections.Generic;

public interface IObservable<T>
{
    public void Subscribe(IObserver<T> listener);
    public void Unsubscribe(IObserver<T> listener);
}

public interface IObserver<T>
{
    public Action<T> EventHandler { get; set; }
}

public class ObservableValue<T> : IObservable<T>
{
    private T _value;
    private readonly List<IObserver<T>> observers = new List<IObserver<T>>();

    public T Value
    {
        get => _value;
        set
        {
            if (EqualityComparer<T>.Default.Equals(_value, value))
            {
                return;
            }

            _value = value;
            Notify(value);
        }
    }

    public ObservableValue()
    {
        _value = default(T);
    }

    public ObservableValue(T value)
    {
        _value = value;
    }

    public void Subscribe(IObserver<T> listener)
    {
        observers.Add(listener);
    }

    public void Unsubscribe(IObserver<T> listener)
    {
        observers.Remove(listener);
    }

    private void Notify(T value)
    {
        foreach (var observer in observers)
        {
            observer.EventHandler?.Invoke(value);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;
using GdUnit4;

namespace GodotTask.Tests;

[TestSuite]
public class GDTaskTest_Observable
{
    [TestCase]
    public static async Task Observable_ToGDTask_FirstValue()
    {
        var observable = new IntObservableNext();

        Constants
            .DelayWithReturn()
            .ContinueWith(observable.UpdateValue)
            .Forget();

        var result = await observable.ToGDTask(true);

        Assertions
            .AssertThat(result)
            .IsEqual(Constants.ReturnValue);
    }

    [TestCase]
    public static async Task Observable_ToGDTask_Completed()
    {
        var observable = new IntObservableComplete();

        Constants
            .DelayWithReturn()
            .ContinueWith(observable.UpdateValue)
            .Forget();

        var result = await observable.ToGDTask();

        Assertions
            .AssertThat(result)
            .IsEqual(Constants.ReturnValue);
    }

    [TestCase]
    public static async Task GDTask_ToObservable()
    {
        var asyncUnitObserver = new AwaitableObserver<AsyncUnit>();
        var asyncUnitObservable = Constants.Delay().ToObservable();
        using (asyncUnitObservable.Subscribe(asyncUnitObserver))
        {
            await asyncUnitObserver;
        }
    }

    [TestCase]
    public static async Task GDTaskT_ToObservable()
    {
        var asyncUnitObserver = new AwaitableObserver<int>();
        var asyncUnitObservable = Constants.DelayWithReturn().ToObservable();
        int? result;
        using (asyncUnitObservable.Subscribe(asyncUnitObserver))
        {
            result = await asyncUnitObserver;
        }

        Assertions
            .AssertThat(result!.Value)
            .IsEqual(Constants.ReturnValue);
    }
    
    [TestCase]
    public static async Task GDTask_ToObservableTUnit()
    {
        var asyncUnitObserver = new AwaitableObserver<Unit>();
        var asyncUnitObservable = Constants.Delay().ToObservable<Unit>();
        using (asyncUnitObservable.Subscribe(asyncUnitObserver))
        {
            await asyncUnitObserver;
        }
    }
    
    private class AwaitableDisposable : IDisposable
    {
        private bool _disposed;

        public GDTask.Awaiter GetAwaiter() => GDTask.WaitUntil(() => _disposed).GetAwaiter();

        public void Dispose()
        {
            _disposed = true;
        }
    }

    private class AwaitableObserver<T> : IObserver<T> where T : struct
    {
        private T? value;
        private bool _isCompleted;

        void IObserver<T>.OnError(Exception error)
        {
        }

        void IObserver<T>.OnNext(T newValue) => value = newValue;
        void IObserver<T>.OnCompleted() => _isCompleted = true;
        public GDTask<T?>.Awaiter GetAwaiter() => 
            GDTask
                .WaitUntil(() => _isCompleted)
                .ContinueWith(() => value)
                .GetAwaiter();
    }

    private class IntObservableNext : IntObservable
    {
        protected override void UpdateValueInternal(int newValue)
        {
            var invocationQueue = new Queue<IObserver<int>>();
            foreach (var observer in _observers)
            {
                invocationQueue.Enqueue(observer);
            }
            _observers.Clear();
            while (invocationQueue.TryDequeue(out var observer))
            {
                observer.OnNext(newValue);
            }
        }
    }
    
    private class IntObservableComplete : IntObservable
    {
        protected override void UpdateValueInternal(int newValue)
        {
            var invocationQueue = new Queue<IObserver<int>>();
            foreach (var observer in _observers)
            {
                invocationQueue.Enqueue(observer);
            }
            _observers.Clear();
            while (invocationQueue.TryDequeue(out var observer))
            {
                observer.OnNext(newValue * 2);
                observer.OnNext(newValue);
                observer.OnCompleted();
            }
        }
    }
    
    private abstract class IntObservable : IObservable<int>
    {
        protected readonly List<IObserver<int>> _observers = new();

        public IDisposable Subscribe(IObserver<int> observer)
        {
            if (!_observers.Contains(observer))
                _observers.Add(observer);
            return new UnSubscriber(_observers, observer);
        }

        public void UpdateValue(int newValue)
        {
            UpdateValueInternal(newValue);
        }
        
        protected abstract void UpdateValueInternal(int newValue);

        private class UnSubscriber : IDisposable
        {
            private readonly List<IObserver<int>> _observers;
            private readonly IObserver<int> _observer;

            public UnSubscriber(List<IObserver<int>> observers, IObserver<int> observer)
            {
                _observers = observers;
                _observer = observer;
            }

            public void Dispose()
            {
                if (_observer != null && _observers.Contains(_observer))
                    _observers.Remove(_observer);
            }
        }
    }
}
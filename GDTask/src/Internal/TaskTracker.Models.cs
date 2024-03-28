using System;

namespace GodotTask;

public static partial class TaskTracker
{
    internal record TrackingData(string FormattedType, int TrackingId, DateTime AddTime, string StackTrace, Func<GDTaskStatus> StatusProvider);

    internal class ObservableProperty : IObservable<bool>
    {
        public static implicit operator bool(ObservableProperty observableProperty)
        {
            return observableProperty._value;
        }

        public ObservableProperty(bool value)
        {
            _value = value;
        }

        public bool Value
        {
            get => _value;
            set
            {
                _value = value;

                if (_singleSubscriber is null) return;

                _singleSubscriber.OnNext(_value);
                _singleSubscriber.OnCompleted();
            }
        }

        private IObserver<bool> _singleSubscriber;

        private bool _value;

        public IDisposable Subscribe(IObserver<bool> observer)
        {
            _singleSubscriber = observer;
            observer.OnNext(_value);
            return new DisposeHandle(this);
        }

        internal class DisposeHandle : IDisposable
        {
            private readonly ObservableProperty _property;

            public DisposeHandle(ObservableProperty property) => _property = property;

            public void Dispose() => _property._singleSubscriber = null;
        }
    }
}
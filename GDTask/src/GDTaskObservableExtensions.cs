using System;
using System.Collections.Immutable;
using System.Runtime.ExceptionServices;
using System.Threading;
using GodotTask.Internal;

namespace GodotTask
{
    /// <summary>
    /// Provides extensions methods for <see cref="GDTask" /> on <see cref="IObservable{T}" /> related use cases.
    /// </summary>
    public static class GDTaskObservableExtensions
    {
        /// <summary>
        /// Create task that completes when the <see cref="IObservable{T}" /> it subscribes to fires.
        /// </summary>
        /// <param name="source">The source <see cref="IObservable{T}" /> to subscribe to.</param>
        /// <param name="useFirstValue">
        /// If set to true, <see cref="IObserver{T}.OnNext" /> is used, otherwise
        /// <see cref="IObserver{T}.OnCompleted" /> is used.
        /// </param>
        /// <param name="cancellationToken">The cancellation token with which to cancel the task.</param>
        /// <typeparam name="T">The object that provides notification information.</typeparam>
        public static GDTask<T> ToGDTask<T>(this IObservable<T> source, bool useFirstValue = false, CancellationToken cancellationToken = default)
        {
            var promise = new GDTaskCompletionSource<T>();
            var disposable = new SingleAssignmentDisposable();

            var observer = useFirstValue
                ? new FirstValueToGDTaskObserver<T>(promise, disposable, cancellationToken)
                : (IObserver<T>)new ToGDTaskObserver<T>(promise, disposable, cancellationToken);

            try { disposable.Disposable = source.Subscribe(observer); }
            catch (Exception ex) { promise.TrySetException(ex); }

            return promise.Task;
        }

        /// <summary>
        /// Create an <see cref="IObservable{T}" /> that fires when the supplied <see cref="GDTask{T}" /> completes.
        /// </summary>
        /// <param name="task">The source <see cref="GDTask{T}" /> to watch for.</param>
        /// <typeparam name="T">The object that provides notification information.</typeparam>
        public static IObservable<T> ToObservable<T>(this GDTask<T> task)
        {
            if (task.Status.IsCompleted())
                try { return new ReturnObservable<T>(task.GetAwaiter().GetResult()); }
                catch (Exception ex) { return new ThrowObservable<T>(ex); }

            var subject = new AsyncSubject<T>();
            Fire(subject, task).Forget();
            return subject;
        }

        /// <summary>
        /// Create an <see cref="IObservable{AsyncUnit}" /> that fires when the supplied <see cref="GDTask" /> completes.
        /// </summary>
        /// <param name="task">The source <see cref="GDTask" /> to watch for.</param>
        public static IObservable<AsyncUnit> ToObservable(this GDTask task) => task.ToObservable<AsyncUnit>();

        /// <summary>
        /// Create an <see cref="IObservable{TUnit}" /> that fires when the supplied <see cref="GDTask" /> completes.
        /// </summary>
        /// <param name="task">The source <see cref="GDTask" /> to watch for.</param>
        /// <typeparam name="TUnit">
        /// A type with a single value, used to denote the successful completion of a void-returning
        /// action, such as <see cref="AsyncUnit" />.
        /// </typeparam>
        public static IObservable<TUnit> ToObservable<TUnit>(this GDTask task)
        {
            if (task.Status.IsCompleted())
                try
                {
                    task.GetAwaiter().GetResult();
                    return new ReturnObservable<TUnit>(default);
                }
                catch (Exception ex) { return new ThrowObservable<TUnit>(ex); }

            var subject = new AsyncSubject<TUnit>();
            Fire(subject, task).Forget();
            return subject;
        }

        private static async GDTaskVoid Fire<T>(IObserver<T> subject, GDTask<T> task)
        {
            T value;

            try { value = await task; }
            catch (Exception ex)
            {
                subject.OnError(ex);
                return;
            }

            subject.OnNext(value);
            subject.OnCompleted();
        }

        private static async GDTaskVoid Fire<TUnit>(IObserver<TUnit> subject, GDTask task)
        {
            try { await task; }
            catch (Exception ex)
            {
                subject.OnError(ex);
                return;
            }

            subject.OnNext(default);
            subject.OnCompleted();
        }

        private class ToGDTaskObserver<T> : IObserver<T>
        {
            private readonly CancellationToken _cancellationToken;
            private readonly SingleAssignmentDisposable _disposable;
            private readonly GDTaskCompletionSource<T> _promise;
            private readonly CancellationTokenRegistration _registration;

            private bool _hasValue;
            private T _latestValue;

            public ToGDTaskObserver(GDTaskCompletionSource<T> promise, SingleAssignmentDisposable disposable, CancellationToken cancellationToken)
            {
                _promise = promise;
                _disposable = disposable;
                _cancellationToken = cancellationToken;

                if (_cancellationToken.CanBeCanceled) _registration = _cancellationToken.RegisterWithoutCaptureExecutionContext(OnCanceled, this);
            }

            public void OnNext(T value)
            {
                _hasValue = true;
                _latestValue = value;
            }

            public void OnError(Exception error)
            {
                try { _promise.TrySetException(error); }
                finally
                {
                    _registration.Dispose();
                    _disposable.Dispose();
                }
            }

            public void OnCompleted()
            {
                try
                {
                    if (_hasValue) _promise.TrySetResult(_latestValue);
                    else _promise.TrySetException(new InvalidOperationException("Sequence has no elements"));
                }
                finally
                {
                    _registration.Dispose();
                    _disposable.Dispose();
                }
            }

            private static void OnCanceled(object state)
            {
                var self = (ToGDTaskObserver<T>)state;
                self._disposable.Dispose();
                self._promise.TrySetCanceled(self._cancellationToken);
            }
        }

        private class FirstValueToGDTaskObserver<T> : IObserver<T>
        {
            private readonly CancellationToken _cancellationToken;
            private readonly SingleAssignmentDisposable _disposable;

            private readonly GDTaskCompletionSource<T> _promise;
            private readonly CancellationTokenRegistration _registration;

            private bool _hasValue;

            public FirstValueToGDTaskObserver(GDTaskCompletionSource<T> promise, SingleAssignmentDisposable disposable, CancellationToken cancellationToken)
            {
                _promise = promise;
                _disposable = disposable;
                _cancellationToken = cancellationToken;

                if (_cancellationToken.CanBeCanceled) _registration = _cancellationToken.RegisterWithoutCaptureExecutionContext(OnCanceled, this);
            }

            public void OnNext(T value)
            {
                _hasValue = true;

                try { _promise.TrySetResult(value); }
                finally
                {
                    _registration.Dispose();
                    _disposable.Dispose();
                }
            }

            public void OnError(Exception error)
            {
                try { _promise.TrySetException(error); }
                finally
                {
                    _registration.Dispose();
                    _disposable.Dispose();
                }
            }

            public void OnCompleted()
            {
                try
                {
                    if (!_hasValue) _promise.TrySetException(new InvalidOperationException("Sequence has no elements"));
                }
                finally
                {
                    _registration.Dispose();
                    _disposable.Dispose();
                }
            }

            private static void OnCanceled(object state)
            {
                var self = (FirstValueToGDTaskObserver<T>)state;
                self._disposable.Dispose();
                self._promise.TrySetCanceled(self._cancellationToken);
            }
        }

        private class ReturnObservable<T>(T value) : IObservable<T>
        {
            public IDisposable Subscribe(IObserver<T> observer)
            {
                observer.OnNext(value);
                observer.OnCompleted();
                return EmptyDisposable.Instance;
            }
        }

        private class ThrowObservable<T>(Exception value) : IObservable<T>
        {
            public IDisposable Subscribe(IObserver<T> observer)
            {
                observer.OnError(value);
                return EmptyDisposable.Instance;
            }
        }
    }
}

namespace GodotTask.Internal
{
    // Bridges for Rx.

    class EmptyDisposable : IDisposable
    {
        public static readonly EmptyDisposable Instance = new();

        private EmptyDisposable() { }

        public void Dispose() { }
    }

    sealed class SingleAssignmentDisposable : IDisposable
    {
#if NET9_0_OR_GREATER
        private readonly Lock _gate = new();
#else
        private readonly object _gate = new();
#endif
        private IDisposable _current;
        private bool _disposed;

        public bool IsDisposed
        {
            get
            {
                lock (_gate) { return _disposed; }
            }
        }

        public IDisposable Disposable
        {
            get => _current;
            set
            {
                IDisposable old;
                bool alreadyDisposed;

                lock (_gate)
                {
                    alreadyDisposed = _disposed;
                    old = _current;

                    if (!alreadyDisposed)
                    {
                        if (value == null) return;
                        _current = value;
                    }
                }

                if (alreadyDisposed && value != null)
                {
                    value.Dispose();
                    return;
                }

                if (old != null) throw new InvalidOperationException("Disposable is already set");
            }
        }

        public void Dispose()
        {
            IDisposable old = null;

            lock (_gate)
            {
                if (!_disposed)
                {
                    _disposed = true;
                    old = _current;
                    _current = null;
                }
            }

            if (old != null) old.Dispose();
        }
    }

    sealed class AsyncSubject<T> : IObservable<T>, IObserver<T>
    {
#if NET9_0_OR_GREATER
        private readonly Lock _observerLock = new();
#else
        private readonly object _observerLock = new();
#endif

        private T _lastValue;
        private bool _hasValue;
        private bool _isDisposed;
        private Exception _lastError;
        private IObserver<T> _outObserver = EmptyObserver<T>.Instance;

        public T Value
        {
            get
            {
                ThrowIfDisposed();
                if (!IsCompleted) throw new InvalidOperationException("AsyncSubject is not completed yet");
                if (_lastError != null) ExceptionDispatchInfo.Capture(_lastError).Throw();
                return _lastValue;
            }
        }

        public bool HasObservers => _outObserver is not EmptyObserver<T> && !IsCompleted && !_isDisposed;

        public bool IsCompleted { get; private set; }

        public void OnCompleted()
        {
            IObserver<T> old;
            T v;
            bool hv;

            lock (_observerLock)
            {
                ThrowIfDisposed();
                if (IsCompleted) return;

                old = _outObserver;
                _outObserver = EmptyObserver<T>.Instance;
                IsCompleted = true;
                v = _lastValue;
                hv = _hasValue;
            }

            if (hv)
            {
                old.OnNext(v);
                old.OnCompleted();
            }
            else old.OnCompleted();
        }

        public void OnError(Exception error)
        {
            if (error == null) throw new ArgumentNullException(nameof(error));

            IObserver<T> old;

            lock (_observerLock)
            {
                ThrowIfDisposed();
                if (IsCompleted) return;

                old = _outObserver;
                _outObserver = EmptyObserver<T>.Instance;
                IsCompleted = true;
                _lastError = error;
            }

            old.OnError(error);
        }

        public void OnNext(T value)
        {
            lock (_observerLock)
            {
                ThrowIfDisposed();
                if (IsCompleted) return;

                _hasValue = true;
                _lastValue = value;
            }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));

            var ex = default(Exception);
            var v = default(T);
            var hv = false;

            lock (_observerLock)
            {
                ThrowIfDisposed();

                if (!IsCompleted)
                {
                    if (_outObserver is ListObserver<T> listObserver) _outObserver = listObserver.Add(observer);
                    else
                    {
                        var current = _outObserver;
                        if (current is EmptyObserver<T>) _outObserver = observer;
                        else _outObserver = new ListObserver<T>(ImmutableArray.Create(current, observer));
                    }

                    return new Subscription(this, observer);
                }

                ex = _lastError;
                v = _lastValue;
                hv = _hasValue;
            }

            if (ex != null) observer.OnError(ex);
            else if (hv)
            {
                observer.OnNext(v);
                observer.OnCompleted();
            }
            else observer.OnCompleted();

            return EmptyDisposable.Instance;
        }

        public void Dispose()
        {
            lock (_observerLock)
            {
                _isDisposed = true;
                _outObserver = DisposedObserver<T>.Instance;
                _lastError = null;
                _lastValue = default;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed) throw new ObjectDisposedException("");
        }

        private class Subscription(AsyncSubject<T> parent, IObserver<T> unsubscribeTarget) : IDisposable
        {
#if NET9_0_OR_GREATER
        private readonly Lock _gate = new();
#else
            private readonly object _gate = new();
#endif
            private AsyncSubject<T> _parent = parent;
            private IObserver<T> _unsubscribeTarget = unsubscribeTarget;

            public void Dispose()
            {
                lock (_gate)
                {
                    if (_parent != null)
                        lock (_parent._observerLock)
                        {
                            if (_parent._outObserver is ListObserver<T> listObserver) _parent._outObserver = listObserver.Remove(_unsubscribeTarget);
                            else _parent._outObserver = EmptyObserver<T>.Instance;

                            _unsubscribeTarget = null;
                            _parent = null;
                        }
                }
            }
        }
    }

    class ListObserver<T>(ImmutableArray<IObserver<T>> observers) : IObserver<T>
    {
        public void OnCompleted()
        {
            for (var i = 0; i < observers.Length; i++) observers[i].OnCompleted();
        }

        public void OnError(Exception error)
        {
            for (var i = 0; i < observers.Length; i++) observers[i].OnError(error);
        }

        public void OnNext(T value)
        {
            for (var i = 0; i < observers.Length; i++) observers[i].OnNext(value);
        }

        internal IObserver<T> Add(IObserver<T> observer) => new ListObserver<T>(observers.Add(observer));

        internal IObserver<T> Remove(IObserver<T> observer)
        {
            var i = observers.IndexOf(observer);
            if (i < 0)
                return this;

            if (observers.Length == 2) return observers[1 - i];

            return new ListObserver<T>(observers.Remove(observer));
        }
    }

    class EmptyObserver<T> : IObserver<T>
    {
        public static readonly EmptyObserver<T> Instance = new();

        private EmptyObserver() { }

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(T value) { }
    }

    class ThrowObserver<T> : IObserver<T>
    {
        public static readonly ThrowObserver<T> Instance = new();

        private ThrowObserver() { }

        public void OnCompleted() { }

        public void OnError(Exception error) => ExceptionDispatchInfo.Capture(error).Throw();

        public void OnNext(T value) { }
    }

    class DisposedObserver<T> : IObserver<T>
    {
        public static readonly DisposedObserver<T> Instance = new();

        private DisposedObserver() { }

        public void OnCompleted() => throw new ObjectDisposedException("");

        public void OnError(Exception error) => throw new ObjectDisposedException("");

        public void OnNext(T value) => throw new ObjectDisposedException("");
    }
}
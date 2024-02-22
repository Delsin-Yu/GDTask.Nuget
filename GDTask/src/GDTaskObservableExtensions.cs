using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using GodotTask.Internal;

namespace GodotTask
{
    /// <summary>
    /// Provides extensions methods for <see cref="GDTask"/> on <see cref="IObservable{T}"/> related use cases.
    /// </summary>
    public static class GDTaskObservableExtensions
    {
        /// <summary>
        /// Create task that completes when the <see cref="IObservable{T}"/> it subscribes to fires.
        /// </summary>
        /// <param name="source">The source <see cref="IObservable{T}"/> to subscribe to.</param>
        /// <param name="useFirstValue">If set to true, <see cref="IObserver{T}.OnNext"/> is used, otherwise <see cref="IObserver{T}.OnCompleted"/> is used.</param>
        /// <param name="cancellationToken">The cancellation token with which to cancel the task.</param>
        /// <typeparam name="T">The object that provides notification information.</typeparam>
        public static GDTask<T> ToGDTask<T>(this IObservable<T> source, bool useFirstValue = false, CancellationToken cancellationToken = default)
        {
            var promise = new GDTaskCompletionSource<T>();
            var disposable = new SingleAssignmentDisposable();

            var observer = useFirstValue
                ? new FirstValueToGDTaskObserver<T>(promise, disposable, cancellationToken)
                : (IObserver<T>)new ToGDTaskObserver<T>(promise, disposable, cancellationToken);

            try
            {
                disposable.Disposable = source.Subscribe(observer);
            }
            catch (Exception ex)
            {
                promise.TrySetException(ex);
            }

            return promise.Task;
        }

        /// <summary>
        /// Create an <see cref="IObservable{T}"/> that fires when the supplied <see cref="GDTask{T}"/> completes.
        /// </summary>
        /// <param name="task">The source <see cref="GDTask{T}"/> to watch for.</param>
        /// <typeparam name="T">The object that provides notification information.</typeparam>
        public static IObservable<T> ToObservable<T>(this GDTask<T> task)
        {
            if (task.Status.IsCompleted())
            {
                try
                {
                    return new ReturnObservable<T>(task.GetAwaiter().GetResult());
                }
                catch (Exception ex)
                {
                    return new ThrowObservable<T>(ex);
                }
            }

            var subject = new AsyncSubject<T>();
            Fire(subject, task).Forget();
            return subject;
        }

        /// <summary>
        /// Create an <see cref="IObservable{AsyncUnit}"/> that fires when the supplied <see cref="GDTask"/> completes.
        /// </summary>
        /// <param name="task">The source <see cref="GDTask"/> to watch for.</param>
        public static IObservable<AsyncUnit> ToObservable(this GDTask task) => task.ToObservable<AsyncUnit>();

        /// <summary>
        /// Create an <see cref="IObservable{TUnit}"/> that fires when the supplied <see cref="GDTask"/> completes.
        /// </summary>
        /// <param name="task">The source <see cref="GDTask"/> to watch for.</param>
        /// <typeparam name="TUnit">A type with a single value, used to denote the successful completion of a void-returning action, such as <see cref="AsyncUnit"/>.</typeparam>
        public static IObservable<TUnit> ToObservable<TUnit>(this GDTask task)
        {
            if (task.Status.IsCompleted())
            {
                try
                {
                    task.GetAwaiter().GetResult();
                    return new ReturnObservable<TUnit>(default);
                }
                catch (Exception ex)
                {
                    return new ThrowObservable<TUnit>(ex);
                }
            }

            var subject = new AsyncSubject<TUnit>();
            Fire(subject, task).Forget();
            return subject;
        }

        private static async GDTaskVoid Fire<T>(IObserver<T> subject, GDTask<T> task)
        {
            T value;
            try
            {
                value = await task;
            }
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
            try
            {
                await task;
            }
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
            private readonly GDTaskCompletionSource<T> promise;
            private readonly SingleAssignmentDisposable disposable;
            private readonly CancellationToken cancellationToken;
            private readonly CancellationTokenRegistration registration;

            private bool hasValue;
            private T latestValue;

            public ToGDTaskObserver(GDTaskCompletionSource<T> promise, SingleAssignmentDisposable disposable, CancellationToken cancellationToken)
            {
                this.promise = promise;
                this.disposable = disposable;
                this.cancellationToken = cancellationToken;

                if (this.cancellationToken.CanBeCanceled)
                {
                    registration = this.cancellationToken.RegisterWithoutCaptureExecutionContext(OnCanceled, this);
                }
            }

            private static void OnCanceled(object state)
            {
                var self = (ToGDTaskObserver<T>)state;
                self.disposable.Dispose();
                self.promise.TrySetCanceled(self.cancellationToken);
            }

            public void OnNext(T value)
            {
                hasValue = true;
                latestValue = value;
            }

            public void OnError(Exception error)
            {
                try
                {
                    promise.TrySetException(error);
                }
                finally
                {
                    registration.Dispose();
                    disposable.Dispose();
                }
            }

            public void OnCompleted()
            {
                try
                {
                    if (hasValue)
                    {
                        promise.TrySetResult(latestValue);
                    }
                    else
                    {
                        promise.TrySetException(new InvalidOperationException("Sequence has no elements"));
                    }
                }
                finally
                {
                    registration.Dispose();
                    disposable.Dispose();
                }
            }
        }

        private class FirstValueToGDTaskObserver<T> : IObserver<T>
        {

            private readonly GDTaskCompletionSource<T> promise;
            private readonly SingleAssignmentDisposable disposable;
            private readonly CancellationToken cancellationToken;
            private readonly CancellationTokenRegistration registration;

            private bool hasValue;

            public FirstValueToGDTaskObserver(GDTaskCompletionSource<T> promise, SingleAssignmentDisposable disposable, CancellationToken cancellationToken)
            {
                this.promise = promise;
                this.disposable = disposable;
                this.cancellationToken = cancellationToken;

                if (this.cancellationToken.CanBeCanceled)
                {
                    registration = this.cancellationToken.RegisterWithoutCaptureExecutionContext(OnCanceled, this);
                }
            }

            private static void OnCanceled(object state)
            {
                var self = (FirstValueToGDTaskObserver<T>)state;
                self.disposable.Dispose();
                self.promise.TrySetCanceled(self.cancellationToken);
            }

            public void OnNext(T value)
            {
                hasValue = true;
                try
                {
                    promise.TrySetResult(value);
                }
                finally
                {
                    registration.Dispose();
                    disposable.Dispose();
                }
            }

            public void OnError(Exception error)
            {
                try
                {
                    promise.TrySetException(error);
                }
                finally
                {
                    registration.Dispose();
                    disposable.Dispose();
                }
            }

            public void OnCompleted()
            {
                try
                {
                    if (!hasValue)
                    {
                        promise.TrySetException(new InvalidOperationException("Sequence has no elements"));
                    }
                }
                finally
                {
                    registration.Dispose();
                    disposable.Dispose();
                }
            }
        }

        private class ReturnObservable<T> : IObservable<T>
        {
            private readonly T value;

            public ReturnObservable(T value)
            {
                this.value = value;
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                observer.OnNext(value);
                observer.OnCompleted();
                return EmptyDisposable.Instance;
            }
        }

        private class ThrowObservable<T> : IObservable<T>
        {
            private readonly Exception value;

            public ThrowObservable(Exception value)
            {
                this.value = value;
            }

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

    internal class EmptyDisposable : IDisposable
    {
        public static readonly EmptyDisposable Instance = new EmptyDisposable();

        private EmptyDisposable()
        {

        }

        public void Dispose()
        {
        }
    }

    internal sealed class SingleAssignmentDisposable : IDisposable
    {
        private readonly object gate = new object();
        private IDisposable current;
        private bool disposed;

        public bool IsDisposed { get { lock (gate) { return disposed; } } }

        public IDisposable Disposable
        {
            get => current;
            set
            {
                IDisposable old;
                bool alreadyDisposed;
                lock (gate)
                {
                    alreadyDisposed = disposed;
                    old = current;
                    if (!alreadyDisposed)
                    {
                        if (value == null) return;
                        current = value;
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

            lock (gate)
            {
                if (!disposed)
                {
                    disposed = true;
                    old = current;
                    current = null;
                }
            }

            if (old != null) old.Dispose();
        }
    }

    internal sealed class AsyncSubject<T> : IObservable<T>, IObserver<T>
    {
        private readonly object observerLock = new object();

        private T lastValue;
        private bool hasValue;
        private bool isStopped;
        private bool isDisposed;
        private Exception lastError;
        private IObserver<T> outObserver = EmptyObserver<T>.Instance;

        public T Value
        {
            get
            {
                ThrowIfDisposed();
                if (!isStopped) throw new InvalidOperationException("AsyncSubject is not completed yet");
                if (lastError != null) ExceptionDispatchInfo.Capture(lastError).Throw();
                return lastValue;
            }
        }

        public bool HasObservers => !(outObserver is EmptyObserver<T>) && !isStopped && !isDisposed;

        public bool IsCompleted => isStopped;

        public void OnCompleted()
        {
            IObserver<T> old;
            T v;
            bool hv;
            lock (observerLock)
            {
                ThrowIfDisposed();
                if (isStopped) return;

                old = outObserver;
                outObserver = EmptyObserver<T>.Instance;
                isStopped = true;
                v = lastValue;
                hv = hasValue;
            }

            if (hv)
            {
                old.OnNext(v);
                old.OnCompleted();
            }
            else
            {
                old.OnCompleted();
            }
        }

        public void OnError(Exception error)
        {
            if (error == null) throw new ArgumentNullException(nameof(error));

            IObserver<T> old;
            lock (observerLock)
            {
                ThrowIfDisposed();
                if (isStopped) return;

                old = outObserver;
                outObserver = EmptyObserver<T>.Instance;
                isStopped = true;
                lastError = error;
            }

            old.OnError(error);
        }

        public void OnNext(T value)
        {
            lock (observerLock)
            {
                ThrowIfDisposed();
                if (isStopped) return;

                hasValue = true;
                lastValue = value;
            }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));

            var ex = default(Exception);
            var v = default(T);
            var hv = false;

            lock (observerLock)
            {
                ThrowIfDisposed();
                if (!isStopped)
                {
                    if (outObserver is ListObserver<T> listObserver)
                    {
                        outObserver = listObserver.Add(observer);
                    }
                    else
                    {
                        var current = outObserver;
                        if (current is EmptyObserver<T>)
                        {
                            outObserver = observer;
                        }
                        else
                        {
                            outObserver = new ListObserver<T>(new ImmutableList<IObserver<T>>(new[] { current, observer }));
                        }
                    }

                    return new Subscription(this, observer);
                }

                ex = lastError;
                v = lastValue;
                hv = hasValue;
            }

            if (ex != null)
            {
                observer.OnError(ex);
            }
            else if (hv)
            {
                observer.OnNext(v);
                observer.OnCompleted();
            }
            else
            {
                observer.OnCompleted();
            }

            return EmptyDisposable.Instance;
        }

        public void Dispose()
        {
            lock (observerLock)
            {
                isDisposed = true;
                outObserver = DisposedObserver<T>.Instance;
                lastError = null;
                lastValue = default(T);
            }
        }

        private void ThrowIfDisposed()
        {
            if (isDisposed) throw new ObjectDisposedException("");
        }

        private class Subscription : IDisposable
        {
            private readonly object gate = new object();
            private AsyncSubject<T> parent;
            private IObserver<T> unsubscribeTarget;

            public Subscription(AsyncSubject<T> parent, IObserver<T> unsubscribeTarget)
            {
                this.parent = parent;
                this.unsubscribeTarget = unsubscribeTarget;
            }

            public void Dispose()
            {
                lock (gate)
                {
                    if (parent != null)
                    {
                        lock (parent.observerLock)
                        {
                            if (parent.outObserver is ListObserver<T> listObserver)
                            {
                                parent.outObserver = listObserver.Remove(unsubscribeTarget);
                            }
                            else
                            {
                                parent.outObserver = EmptyObserver<T>.Instance;
                            }

                            unsubscribeTarget = null;
                            parent = null;
                        }
                    }
                }
            }
        }
    }

    internal class ListObserver<T> : IObserver<T>
    {
        private readonly ImmutableList<IObserver<T>> _observers;

        public ListObserver(ImmutableList<IObserver<T>> observers)
        {
            _observers = observers;
        }

        public void OnCompleted()
        {
            var targetObservers = _observers.Data;
            for (int i = 0; i < targetObservers.Length; i++)
            {
                targetObservers[i].OnCompleted();
            }
        }

        public void OnError(Exception error)
        {
            var targetObservers = _observers.Data;
            for (int i = 0; i < targetObservers.Length; i++)
            {
                targetObservers[i].OnError(error);
            }
        }

        public void OnNext(T value)
        {
            var targetObservers = _observers.Data;
            for (int i = 0; i < targetObservers.Length; i++)
            {
                targetObservers[i].OnNext(value);
            }
        }

        internal IObserver<T> Add(IObserver<T> observer)
        {
            return new ListObserver<T>(_observers.Add(observer));
        }

        internal IObserver<T> Remove(IObserver<T> observer)
        {
            var i = Array.IndexOf(_observers.Data, observer);
            if (i < 0)
                return this;

            if (_observers.Data.Length == 2)
            {
                return _observers.Data[1 - i];
            }
            else
            {
                return new ListObserver<T>(_observers.Remove(observer));
            }
        }
    }

    internal class EmptyObserver<T> : IObserver<T>
    {
        public static readonly EmptyObserver<T> Instance = new EmptyObserver<T>();

        private EmptyObserver()
        {

        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(T value)
        {
        }
    }

    internal class ThrowObserver<T> : IObserver<T>
    {
        public static readonly ThrowObserver<T> Instance = new ThrowObserver<T>();

        private ThrowObserver()
        {

        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
            ExceptionDispatchInfo.Capture(error).Throw();
        }

        public void OnNext(T value)
        {
        }
    }

    internal class DisposedObserver<T> : IObserver<T>
    {
        public static readonly DisposedObserver<T> Instance = new DisposedObserver<T>();

        private DisposedObserver()
        {

        }

        public void OnCompleted()
        {
            throw new ObjectDisposedException("");
        }

        public void OnError(Exception error)
        {
            throw new ObjectDisposedException("");
        }

        public void OnNext(T value)
        {
            throw new ObjectDisposedException("");
        }
    }

    internal class ImmutableList<T>
    {
        public static readonly ImmutableList<T> Empty = new ImmutableList<T>();

        private T[] data;

        public T[] Data => data;

        private ImmutableList()
        {
            data = Array.Empty<T>();
        }

        public ImmutableList(T[] data)
        {
            this.data = data;
        }

        public ImmutableList<T> Add(T value)
        {
            var newData = new T[data.Length + 1];
            Array.Copy(data, newData, data.Length);
            newData[data.Length] = value;
            return new ImmutableList<T>(newData);
        }

        public ImmutableList<T> Remove(T value)
        {
            var i = IndexOf(value);
            if (i < 0) return this;

            var length = data.Length;
            if (length == 1) return Empty;

            var newData = new T[length - 1];

            Array.Copy(data, 0, newData, 0, i);
            Array.Copy(data, i + 1, newData, i, length - i - 1);

            return new ImmutableList<T>(newData);
        }

        public int IndexOf(T value)
        {
            var comparer = EqualityComparer<T>.Default;
            for (var i = 0; i < data.Length; ++i)
            {
                // ImmutableList only use for IObserver(no worry for boxed)
                if (comparer.Equals(data[i], value)) return i;
            }
            return -1;
        }
    }
}


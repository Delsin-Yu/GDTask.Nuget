using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace GodotTask;

public partial struct GDTask
{
    private static readonly GDTask CanceledGDTask = new Func<GDTask>(() => new(new CanceledResultSource(CancellationToken.None), 0))();

    private static class CanceledGDTaskCache<T>
    {
        public static readonly GDTask<T> Task;

        static CanceledGDTaskCache()
        {
            Task = new(new CanceledResultSource<T>(CancellationToken.None), 0);
        }
    }

    /// <summary>
    /// Gets a <see cref="GDTask" /> that has already completed successfully.
    /// </summary>
    public static readonly GDTask CompletedTask = new();

    /// <summary>
    /// Creates a <see cref="GDTask" /> that has completed with the specified exception.
    /// </summary>
    /// <param name="ex">The exception with which to fault the task.</param>
    /// <returns>The faulted task.</returns>
    public static GDTask FromException(Exception ex)
    {
        if (ex is OperationCanceledException oce) return FromCanceled(oce.CancellationToken);

        return new(new ExceptionResultSource(ex), 0);
    }

    /// <inheritdoc cref="FromException" />
    public static GDTask<T> FromException<T>(Exception ex)
    {
        if (ex is OperationCanceledException oce) return FromCanceled<T>(oce.CancellationToken);

        return new(new ExceptionResultSource<T>(ex), 0);
    }

    /// <summary>
    /// Creates a <see cref="GDTask{TResult}" /> that's completed successfully with the specified result.
    /// </summary>
    /// <typeparam name="T">The type of the result returned by the task.</typeparam>
    /// <param name="value">The result to store into the completed task.</param>
    /// <returns>The successfully completed task.</returns>
    public static GDTask<T> FromResult<T>(T value) => new(value);

    /// <summary>
    /// Creates a <see cref="GDTask" /> that has completed due to cancellation with the specified cancellation token.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token with which to cancel the task.</param>
    /// <returns>The canceled task.</returns>
    public static GDTask FromCanceled(CancellationToken cancellationToken = default)
    {
        if (cancellationToken == CancellationToken.None) return CanceledGDTask;

        return new(new CanceledResultSource(cancellationToken), 0);
    }

    /// <inheritdoc cref="FromCanceled" />
    public static GDTask<T> FromCanceled<T>(CancellationToken cancellationToken = default)
    {
        if (cancellationToken == CancellationToken.None) return CanceledGDTaskCache<T>.Task;

        return new(new CanceledResultSource<T>(cancellationToken), 0);
    }

    /// <summary>
    /// Create the specified asynchronous work to run and returns a <see cref="GDTask" /> handle for that work.
    /// </summary>
    /// <param name="factory">The work to execute asynchronously</param>
    /// <returns>A <see cref="GDTask" /> that represents the work to execute.</returns>
    public static GDTask Create(Func<GDTask> factory) => factory();

    /// <inheritdoc cref="Create" />
    public static GDTask<T> Create<T>(Func<GDTask<T>> factory) => factory();

    /// <summary>
    /// Defers the creation of a specified asynchronous work until it's acquired.
    /// </summary>
    /// <param name="factory">The work to execute asynchronously</param>
    /// <returns>An <see cref="AsyncLazy" /> that represents the work for lazy initialization.</returns>
    public static IAsyncLazy Lazy(Func<GDTask> factory) => new AsyncLazy(factory);

    /// <inheritdoc cref="Lazy" />
    public static IAsyncLazy<T> Lazy<T>(Func<GDTask<T>> factory) => new AsyncLazy<T>(factory);

    /// <summary>
    /// Execute a lightweight task that does not have awaitable completion.
    /// </summary>
    public static void Void(Func<GDTaskVoid> asyncAction) => asyncAction().Forget();

    /// <summary>
    /// Execute a lightweight task that does not have awaitable completion, with specified <see cref="CancellationToken" />.
    /// </summary>
    public static void Void(Func<CancellationToken, GDTaskVoid> asyncAction, CancellationToken cancellationToken) => asyncAction(cancellationToken).Forget();

    /// <summary>
    /// Execute a lightweight task that does not have awaitable completion, with specified input value
    /// <typeparamref name="T" />.
    /// </summary>
    public static void Void<T>(Func<T, GDTaskVoid> asyncAction, T state) => asyncAction(state).Forget();

    /// <summary>
    /// Creates a delegate that execute a lightweight task that does not have awaitable completion.
    /// </summary>
    public static Action Action(Func<GDTaskVoid> asyncAction) => () => asyncAction().Forget();

    /// <summary>
    /// Creates a delegate that execute a lightweight task that does not have awaitable completion, with specified
    /// <see cref="CancellationToken" />.
    /// </summary>
    public static Action Action(Func<CancellationToken, GDTaskVoid> asyncAction, CancellationToken cancellationToken) => () => asyncAction(cancellationToken).Forget();

    /// <summary>
    /// Defers the creation of a specified asynchronous work when it's awaited.
    /// </summary>
    public static GDTask Defer(Func<GDTask> factory) => new(new DeferPromise(factory), 0);

    /// <inheritdoc cref="Defer" />
    public static GDTask<T> Defer<T>(Func<GDTask<T>> factory) => new(new DeferPromise<T>(factory), 0);

    /// <summary>
    /// Creates a task that never completes, with specified <see cref="CancellationToken" />.
    /// </summary>
    public static GDTask Never(CancellationToken cancellationToken = default) => new GDTask<AsyncUnit>(new NeverPromise<AsyncUnit>(cancellationToken), 0);

    /// <inheritdoc cref="Never" />
    public static GDTask<T> Never<T>(CancellationToken cancellationToken = default) => new(new NeverPromise<T>(cancellationToken), 0);

    private sealed class ExceptionResultSource(Exception exception) : IGDTaskSource
    {
        private readonly ExceptionDispatchInfo _exception = ExceptionDispatchInfo.Capture(exception);
        private bool _calledGet;

        public void GetResult(short token)
        {
            if (!_calledGet)
            {
                _calledGet = true;
                GC.SuppressFinalize(this);
            }

            _exception.Throw();
        }

        public GDTaskStatus GetStatus(short token) => GDTaskStatus.Faulted;

        public GDTaskStatus UnsafeGetStatus() => GDTaskStatus.Faulted;

        public void OnCompleted(Action<object> continuation, object state, short token) => continuation(state);

        ~ExceptionResultSource()
        {
            if (!_calledGet) GDTaskExceptionHandler.PublishUnobservedTaskException(_exception.SourceException);
        }
    }

    private sealed class ExceptionResultSource<T>(Exception exception) : IGDTaskSource<T>
    {
        private readonly ExceptionDispatchInfo _exception = ExceptionDispatchInfo.Capture(exception);
        private bool _calledGet;

        public T GetResult(short token)
        {
            if (!_calledGet)
            {
                _calledGet = true;
                GC.SuppressFinalize(this);
            }

            _exception.Throw();
            return default;
        }

        void IGDTaskSource.GetResult(short token)
        {
            if (!_calledGet)
            {
                _calledGet = true;
                GC.SuppressFinalize(this);
            }

            _exception.Throw();
        }

        public GDTaskStatus GetStatus(short token) => GDTaskStatus.Faulted;

        public GDTaskStatus UnsafeGetStatus() => GDTaskStatus.Faulted;

        public void OnCompleted(Action<object> continuation, object state, short token) => continuation(state);

        ~ExceptionResultSource()
        {
            if (!_calledGet) GDTaskExceptionHandler.PublishUnobservedTaskException(_exception.SourceException);
        }
    }

    private sealed class CanceledResultSource(CancellationToken cancellationToken) : IGDTaskSource
    {
        public void GetResult(short token) => throw new OperationCanceledException(cancellationToken);

        public GDTaskStatus GetStatus(short token) => GDTaskStatus.Canceled;

        public GDTaskStatus UnsafeGetStatus() => GDTaskStatus.Canceled;

        public void OnCompleted(Action<object> continuation, object state, short token) => continuation(state);
    }

    private sealed class CanceledResultSource<T>(CancellationToken cancellationToken) : IGDTaskSource<T>
    {
        public T GetResult(short token) => throw new OperationCanceledException(cancellationToken);

        void IGDTaskSource.GetResult(short token) => throw new OperationCanceledException(cancellationToken);

        public GDTaskStatus GetStatus(short token) => GDTaskStatus.Canceled;

        public GDTaskStatus UnsafeGetStatus() => GDTaskStatus.Canceled;

        public void OnCompleted(Action<object> continuation, object state, short token) => continuation(state);
    }

    private sealed class DeferPromise(Func<GDTask> factory) : IGDTaskSource
    {
        private Awaiter _awaiter;
        private Func<GDTask> _factory = factory;
        private GDTask _task;

        public void GetResult(short token) => _awaiter.GetResult();

        public GDTaskStatus GetStatus(short token)
        {
            var f = Interlocked.Exchange(ref _factory, null);

            if (f != null)
            {
                _task = f();
                _awaiter = _task.GetAwaiter();
            }

            return _task.Status;
        }

        public void OnCompleted(Action<object> continuation, object state, short token) => _awaiter.SourceOnCompleted(continuation, state);

        public GDTaskStatus UnsafeGetStatus() => _task.Status;
    }

    private sealed class DeferPromise<T>(Func<GDTask<T>> factory) : IGDTaskSource<T>
    {
        private GDTask<T>.Awaiter _awaiter;
        private Func<GDTask<T>> _factory = factory;
        private GDTask<T> _task;

        public T GetResult(short token) => _awaiter.GetResult();

        void IGDTaskSource.GetResult(short token) => _awaiter.GetResult();

        public GDTaskStatus GetStatus(short token)
        {
            var f = Interlocked.Exchange(ref _factory, null);

            if (f != null)
            {
                _task = f();
                _awaiter = _task.GetAwaiter();
            }

            return _task.Status;
        }

        public void OnCompleted(Action<object> continuation, object state, short token) => _awaiter.SourceOnCompleted(continuation, state);

        public GDTaskStatus UnsafeGetStatus() => _task.Status;
    }

    private sealed class NeverPromise<T> : IGDTaskSource<T>
    {
        private readonly CancellationToken _cancellationToken;
        private GDTaskCompletionSourceCore<T> _core;

        public NeverPromise(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            if (_cancellationToken.CanBeCanceled) _cancellationToken.RegisterWithoutCaptureExecutionContext(CancellationCallback, this);
        }

        public T GetResult(short token) => _core.GetResult(token);

        public GDTaskStatus GetStatus(short token) => _core.GetStatus(token);

        public GDTaskStatus UnsafeGetStatus() => _core.UnsafeGetStatus();

        public void OnCompleted(Action<object> continuation, object state, short token) => _core.OnCompleted(continuation, state, token);

        void IGDTaskSource.GetResult(short token) => _core.GetResult(token);

        private static void CancellationCallback(object state)
        {
            var self = (NeverPromise<T>)state;
            self._core.TrySetCanceled(self._cancellationToken);
        }
    }
}

static class CompletedTasks
{
    public static readonly GDTask<AsyncUnit> AsyncUnit = GDTask.FromResult(GodotTask.AsyncUnit.Default);
    public static readonly GDTask<bool> True = GDTask.FromResult(true);
    public static readonly GDTask<bool> False = GDTask.FromResult(false);
    public static readonly GDTask<int> Zero = GDTask.FromResult(0);
    public static readonly GDTask<int> MinusOne = GDTask.FromResult(-1);
    public static readonly GDTask<int> One = GDTask.FromResult(1);
}
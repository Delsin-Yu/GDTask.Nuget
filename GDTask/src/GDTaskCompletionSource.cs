using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace GodotTask;

interface IResolvePromise
{
    bool TrySetResult();
}

interface IResolvePromise<in T>
{
    bool TrySetResult(T value);
}

interface IRejectPromise
{
    bool TrySetException(Exception exception);
}

interface ICancelPromise
{
    bool TrySetCanceled(CancellationToken cancellationToken = default);
}

interface IPromise<in T> : IResolvePromise<T>, IRejectPromise, ICancelPromise;

interface IPromise : IResolvePromise, IRejectPromise, ICancelPromise;

class ExceptionHolder(ExceptionDispatchInfo exception)
{
    private bool _calledGet;

    public ExceptionDispatchInfo GetException()
    {
        if (!_calledGet)
        {
            _calledGet = true;
            GC.SuppressFinalize(this);
        }

        return exception;
    }

    ~ExceptionHolder()
    {
        if (!_calledGet) GDTaskExceptionHandler.PublishUnobservedTaskException(exception.SourceException);
    }
}

[StructLayout(LayoutKind.Auto)]
struct GDTaskCompletionSourceCore<TResult>
{
    // Struct Size: TResult + (8 + 2 + 1 + 1 + 8 + 8)

    private const int Pending = 0;
    private const int Completing = 1;
    private const int Completed = 2;

    private TResult _result;
    private object _error; // ExceptionHolder or OperationCanceledException
    private bool _hasUnhandledError;
    private int _completedCount;
    private Action<object> _continuation;
    private object _continuationState;

    [DebuggerHidden]
    public void Reset()
    {
        ReportUnhandledError();

        unchecked
        {
            Version += 1; // incr version.
        }

        _completedCount = Pending;
        _result = default;
        _error = null;
        _hasUnhandledError = false;
        _continuation = null;
        _continuationState = null;
    }

    private readonly void ReportUnhandledError()
    {
        if (_hasUnhandledError)
            try
            {
                if (_error is OperationCanceledException oc) GDTaskExceptionHandler.PublishUnobservedTaskException(oc);
                else if (_error is ExceptionHolder e) GDTaskExceptionHandler.PublishUnobservedTaskException(e.GetException().SourceException);
            }
            catch { }
    }

    internal void MarkHandled() => _hasUnhandledError = false;

    /// <summary>Completes with a successful result.</summary>
    /// <param name="result">The result.</param>
    [DebuggerHidden]
    public bool TrySetResult(TResult result)
    {
        if (Interlocked.CompareExchange(ref _completedCount, Completing, Pending) == Pending)
        {
            _result = result;

            Volatile.Write(ref _completedCount, Completed);

            if (_continuation != null || Interlocked.CompareExchange(ref _continuation, GDTaskCompletionSourceCoreShared.SSentinel, null) != null)
            {
                _continuation!(_continuationState);
            }

            return true;
        }

        return false;
    }

    /// <summary>Completes with an error.</summary>
    /// <param name="error">The exception.</param>
    [DebuggerHidden]
    public bool TrySetException(Exception error)
    {
        if (Interlocked.CompareExchange(ref _completedCount, Completing, Pending) == Pending)
        {
            _hasUnhandledError = true;
            if (error is OperationCanceledException) _error = error;
            else _error = new ExceptionHolder(ExceptionDispatchInfo.Capture(error));

            Volatile.Write(ref _completedCount, Completed);

            if (_continuation != null || Interlocked.CompareExchange(ref _continuation, GDTaskCompletionSourceCoreShared.SSentinel, null) != null)
            {
                _continuation!(_continuationState);
            }

            return true;
        }

        return false;
    }

    [DebuggerHidden]
    public bool TrySetCanceled(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _completedCount, Completing, Pending) == Pending)
        {
            _hasUnhandledError = true;
            _error = new OperationCanceledException(cancellationToken);

            Volatile.Write(ref _completedCount, Completed);

            if (_continuation != null || Interlocked.CompareExchange(ref _continuation, GDTaskCompletionSourceCoreShared.SSentinel, null) != null)
            {
                _continuation!(_continuationState);
            }

            return true;
        }

        return false;
    }

    /// <summary>Gets the operation version.</summary>
    [DebuggerHidden]
    public short Version { get; private set; }

    /// <summary>Gets the status of the operation.</summary>
    /// <param name="token">Opaque value that was provided to the <see cref="GDTask" />'s constructor.</param>
    [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GDTaskStatus GetStatus(short token)
    {
        ValidateToken(token);
        return Volatile.Read(ref _completedCount) != Completed ? GDTaskStatus.Pending
            : _error == null ? GDTaskStatus.Succeeded
            : _error is OperationCanceledException ? GDTaskStatus.Canceled
            : GDTaskStatus.Faulted;
    }

    /// <summary>Gets the status of the operation without token validation.</summary>
    [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GDTaskStatus UnsafeGetStatus() =>
        Volatile.Read(ref _completedCount) != Completed ? GDTaskStatus.Pending
        : _error == null ? GDTaskStatus.Succeeded
        : _error is OperationCanceledException ? GDTaskStatus.Canceled
        : GDTaskStatus.Faulted;

    /// <summary>Gets the result of the operation.</summary>
    /// <param name="token">Opaque value that was provided to the <see cref="GDTask" />'s constructor.</param>
    // [StackTraceHidden]
    [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TResult GetResult(short token)
    {
        ValidateToken(token);
        if (Volatile.Read(ref _completedCount) != Completed) throw new InvalidOperationException("Not yet completed, GDTask only allow to use await.");

        if (_error != null)
        {
            _hasUnhandledError = false;
            if (_error is OperationCanceledException oce) throw oce;

            if (_error is ExceptionHolder eh) eh.GetException().Throw();

            throw new InvalidOperationException("Critical: invalid exception type was held.");
        }

        return _result;
    }

    /// <summary>Schedules the continuation action for this operation.</summary>
    /// <param name="continuation">The continuation to invoke when the operation has completed.</param>
    /// <param name="state">The state object to pass to <paramref name="continuation" /> when it's invoked.</param>
    /// <param name="token">Opaque value that was provided to the <see cref="GDTask" />'s constructor.</param>
    [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action<object> continuation, object state, short token /*, ValueTaskSourceOnCompletedFlags flags */)
    {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        ValidateToken(token);

        /* no use ValueTaskSourceOnCOmpletedFlags, always no capture ExecutionContext and SynchronizationContext. */

        /*
            PatternA: GetStatus=Pending => OnCompleted => TrySet*** => GetResult
            PatternB: TrySet*** => GetStatus=!Pending => GetResult
            PatternC: GetStatus=Pending => TrySet/OnCompleted(race condition) => GetResult
            C.1: win OnCompleted -> TrySet invoke saved continuation
            C.2: win TrySet -> should invoke continuation here.
        */

        // not set continuation yet.
        object oldContinuation = _continuation;

        if (oldContinuation == null)
        {
            _continuationState = state;
            oldContinuation = Interlocked.CompareExchange(ref _continuation, continuation, null);
        }

        if (oldContinuation != null)
        {
            // already running continuation in TrySet.
            // It will cause call OnCompleted multiple time, invalid.
            if (!ReferenceEquals(oldContinuation, GDTaskCompletionSourceCoreShared.SSentinel)) throw new InvalidOperationException("Already continuation registered, can not await twice or get Status after await.");

            continuation(state);
        }
    }

    [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly void ValidateToken(short token)
    {
        if (token != Version) throw new InvalidOperationException("Token version is not matched, can not await twice or get Status after await.");
    }
}

static class GDTaskCompletionSourceCoreShared // separated out of generic to avoid unnecessary duplication
{
    internal static readonly Action<object> SSentinel = CompletionSentinel;

    private static void CompletionSentinel(object _) // named method to aid debugging
        =>
            throw new InvalidOperationException("The sentinel delegate should never be invoked.");
}

class AutoResetGDTaskCompletionSource : IGDTaskSource, ITaskPoolNode<AutoResetGDTaskCompletionSource>, IPromise
{
    private static TaskPool<AutoResetGDTaskCompletionSource> Pool;

    private GDTaskCompletionSourceCore<AsyncUnit> _core;
    private AutoResetGDTaskCompletionSource _nextNode;

    private AutoResetGDTaskCompletionSource() { }

    public GDTask Task
    {
        [DebuggerHidden]
        get => new(this, _core.Version);
    }

    [DebuggerHidden]
    public void GetResult(short token)
    {
        try { _core.GetResult(token); }
        finally { TryReturn(); }
    }

    [DebuggerHidden]
    public GDTaskStatus GetStatus(short token) => _core.GetStatus(token);

    [DebuggerHidden]
    public GDTaskStatus UnsafeGetStatus() => _core.UnsafeGetStatus();

    [DebuggerHidden]
    public void OnCompleted(Action<object> continuation, object state, short token) => _core.OnCompleted(continuation, state, token);

    [DebuggerHidden]
    public bool TrySetResult() => _core.TrySetResult(AsyncUnit.Default);

    [DebuggerHidden]
    public bool TrySetCanceled(CancellationToken cancellationToken = default) => _core.TrySetCanceled(cancellationToken);

    [DebuggerHidden]
    public bool TrySetException(Exception exception) => _core.TrySetException(exception);

    public ref AutoResetGDTaskCompletionSource NextNode => ref _nextNode;

    [DebuggerHidden]
    public static AutoResetGDTaskCompletionSource Create()
    {
        if (!Pool.TryPop(out var result)) result = new();
        TaskTracker.TrackActiveTask(result, 2);
        return result;
    }

    [DebuggerHidden]
    public static AutoResetGDTaskCompletionSource CreateFromCanceled(CancellationToken cancellationToken, out short token)
    {
        var source = Create();
        source.TrySetCanceled(cancellationToken);
        token = source._core.Version;
        return source;
    }

    [DebuggerHidden]
    public static AutoResetGDTaskCompletionSource CreateFromException(Exception exception, out short token)
    {
        var source = Create();
        source.TrySetException(exception);
        token = source._core.Version;
        return source;
    }

    [DebuggerHidden]
    public static AutoResetGDTaskCompletionSource CreateCompleted(out short token)
    {
        var source = Create();
        source.TrySetResult();
        token = source._core.Version;
        return source;
    }

    [DebuggerHidden]
    private bool TryReturn()
    {
        TaskTracker.RemoveTracking(this);
        _core.Reset();
        return Pool.TryPush(this);
    }
}

class AutoResetGDTaskCompletionSource<T> : IGDTaskSource<T>, ITaskPoolNode<AutoResetGDTaskCompletionSource<T>>, IPromise<T>
{
    private static TaskPool<AutoResetGDTaskCompletionSource<T>> Pool;

    private GDTaskCompletionSourceCore<T> _core;
    private AutoResetGDTaskCompletionSource<T> _nextNode;

    private AutoResetGDTaskCompletionSource() { }

    public GDTask<T> Task
    {
        [DebuggerHidden]
        get => new(this, _core.Version);
    }

    [DebuggerHidden]
    public T GetResult(short token)
    {
        try { return _core.GetResult(token); }
        finally { TryReturn(); }
    }

    [DebuggerHidden]
    void IGDTaskSource.GetResult(short token) => GetResult(token);

    [DebuggerHidden]
    public GDTaskStatus GetStatus(short token) => _core.GetStatus(token);

    [DebuggerHidden]
    public GDTaskStatus UnsafeGetStatus() => _core.UnsafeGetStatus();

    [DebuggerHidden]
    public void OnCompleted(Action<object> continuation, object state, short token) => _core.OnCompleted(continuation, state, token);

    [DebuggerHidden]
    public bool TrySetResult(T result) => _core.TrySetResult(result);

    [DebuggerHidden]
    public bool TrySetCanceled(CancellationToken cancellationToken = default) => _core.TrySetCanceled(cancellationToken);

    [DebuggerHidden]
    public bool TrySetException(Exception exception) => _core.TrySetException(exception);

    public ref AutoResetGDTaskCompletionSource<T> NextNode => ref _nextNode;

    [DebuggerHidden]
    private static AutoResetGDTaskCompletionSource<T> Create()
    {
        if (!Pool.TryPop(out var result)) result = new();
        TaskTracker.TrackActiveTask(result, 2);
        return result;
    }

    [DebuggerHidden]
    public static AutoResetGDTaskCompletionSource<T> CreateFromCanceled(CancellationToken cancellationToken, out short token)
    {
        var source = Create();
        source.TrySetCanceled(cancellationToken);
        token = source._core.Version;
        return source;
    }

    [DebuggerHidden]
    public static AutoResetGDTaskCompletionSource<T> CreateFromException(Exception exception, out short token)
    {
        var source = Create();
        source.TrySetException(exception);
        token = source._core.Version;
        return source;
    }

    [DebuggerHidden]
    public static AutoResetGDTaskCompletionSource<T> CreateFromResult(T result, out short token)
    {
        var source = Create();
        source.TrySetResult(result);
        token = source._core.Version;
        return source;
    }

    [DebuggerHidden]
    private bool TryReturn()
    {
        TaskTracker.RemoveTracking(this);
        _core.Reset();
        return Pool.TryPush(this);
    }
}

/// <summary>
/// Represents the producer side of a <see cref="GDTask" /> unbound to a
/// delegate, providing access to the consumer side through the <see cref="GDTask" /> property.
/// </summary>
/// <remarks>
/// <para>
/// It is often the case that a <see cref="GDTask" /> is desired to
/// represent another asynchronous operation.
/// <see cref="GDTaskCompletionSource">GDTaskCompletionSource</see> is provided for this purpose. It enables
/// the creation of a task that can be handed out to consumers, and those consumers can use the members
/// of the task as they would any other. However, unlike most tasks, the state of a task created by a
/// GDTaskCompletionSource is controlled explicitly by the methods on GDTaskCompletionSource. This enables the
/// completion of the external asynchronous operation to be propagated to the underlying GDTask. The
/// separation also ensures that consumers are not able to transition the state without access to the
/// corresponding GDTaskCompletionSource.
/// </para>
/// <para>
/// All members of <see cref="GDTaskCompletionSource" /> are thread-safe
/// and may be used from multiple threads concurrently.
/// </para>
/// </remarks>
public class GDTaskCompletionSource : IGDTaskSource, IPromise
{
    private CancellationToken _cancellationToken;
    private ExceptionHolder _exception;

#if NET9_0_OR_GREATER
    private Lock _gate;
#else
    private object _gate;
#endif

    private Action<object> _singleContinuation;
    private object _singleState;
    private List<(Action<object>, object)> _secondaryContinuationList;

    private int _intStatus; // GDTaskStatus
    private bool _handled;

    /// <summary>
    /// Initializes a new instance of the <see cref="GDTaskCompletionSource" /> class.
    /// </summary>
    public GDTaskCompletionSource()
    {
        TaskTracker.TrackActiveTask(this, 2);
    }

    [DebuggerHidden]
    internal void MarkHandled()
    {
        if (!_handled)
        {
            _handled = true;
            TaskTracker.RemoveTracking(this);
        }
    }

    /// <summary>
    /// Gets the <see cref="GDTask" /> created
    /// by this <see cref="GDTaskCompletionSource" />.
    /// </summary>
    /// <remarks>
    /// This property enables a consumer access to the <see cref="GDTask" /> that is controlled by this instance.
    /// The <see cref="TrySetResult" />, <see cref="TrySetException(Exception)" />,
    /// and <see cref="TrySetCanceled" /> methods (and their "Try" variants) on this instance all result in the relevant state
    /// transitions on this underlying GDTask.
    /// </remarks>
    public GDTask Task
    {
        [DebuggerHidden]
        get => new(this, 0);
    }

    /// <summary>
    /// Attempts to transition the underlying <see cref="GDTask" /> into the <see cref="GDTaskStatus.Succeeded" /> state.
    /// </summary>
    /// <returns>True if the operation was successful; otherwise, false.</returns>
    [DebuggerHidden]
    public bool TrySetResult() => TrySignalCompletion(GDTaskStatus.Succeeded, static () => { });

    /// <summary>
    /// Attempts to transition the underlying <see cref="GDTask" /> into the <see cref="GDTaskStatus.Canceled" /> state.
    /// </summary>
    /// <returns>True if the operation was successful; otherwise, false.</returns>
    [DebuggerHidden]
    public bool TrySetCanceled(CancellationToken cancellationToken = default)
    {
        return TrySignalCompletion(GDTaskStatus.Canceled, () => _cancellationToken = cancellationToken);
    }

    /// <summary>
    /// Attempts to transition the underlying <see cref="GDTask" /> into the <see cref="GDTaskStatus.Faulted" /> state.
    /// </summary>
    /// <param name="exception">The exception to bind to this <see cref="GDTask" />.</param>
    /// <returns>True if the operation was successful; otherwise, false.</returns>
    [DebuggerHidden]
    public bool TrySetException(Exception exception)
    {
        if (exception is OperationCanceledException oce) return TrySetCanceled(oce.CancellationToken);

        return TrySignalCompletion(GDTaskStatus.Faulted, () => _exception = new(ExceptionDispatchInfo.Capture(exception)));
    }

    /// <summary>
    /// Gets the result of the underlying <see cref="GDTask" />.
    /// </summary>
    /// <remarks>
    /// This method is used by the compiler to implement the await operator.
    /// It is not intended to be called directly by user code.
    /// </remarks>
    [DebuggerHidden]
    public void GetResult(short token)
    {
        MarkHandled();

        var status = (GDTaskStatus)Volatile.Read(ref _intStatus);

        switch (status)
        {
            case GDTaskStatus.Succeeded:
                return;
            case GDTaskStatus.Faulted:
                _exception.GetException().Throw();
                return;
            case GDTaskStatus.Canceled:
                throw new OperationCanceledException(_cancellationToken);
            default:
            case GDTaskStatus.Pending:
                throw new InvalidOperationException("not yet completed.");
        }
    }

    /// <summary>
    /// Gets the status of the underlying <see cref="GDTask" />.
    /// </summary>
    /// <remarks>
    /// This method is used by the compiler to implement the await operator.
    /// It is not intended to be called directly by user code.
    /// </remarks>
    [DebuggerHidden]
    public GDTaskStatus GetStatus(short token) => (GDTaskStatus)Volatile.Read(ref _intStatus);

    /// <summary>
    /// Gets the status of the underlying <see cref="GDTask" /> without validating the token
    /// or checking if the task is completed.
    /// </summary>
    /// <remarks>
    /// This method is used by the compiler to implement the await operator.
    /// It is not intended to be called directly by user code.
    /// </remarks>
    [DebuggerHidden]
    public GDTaskStatus UnsafeGetStatus() => (GDTaskStatus)Volatile.Read(ref _intStatus);

    /// <summary>
    /// Schedules the continuation action for this operation.
    /// </summary>
    /// <remarks>
    /// This method is used by the compiler to implement the await operator.
    /// It is not intended to be called directly by user code.
    /// </remarks>
    [DebuggerHidden]
    public void OnCompleted(Action<object> continuation, object state, short token)
    {
        if (_gate == null)
        {
#if NET9_0_OR_GREATER
            Interlocked.CompareExchange(ref _gate, new(), null);
#else
            Interlocked.CompareExchange(ref _gate, new(), null);
#endif
        }

        var lockGate = Volatile.Read(ref _gate);

        lock (lockGate!) // wait TrySignalCompletion, after status is not pending.
        {
            if ((GDTaskStatus)Volatile.Read(ref _intStatus) != GDTaskStatus.Pending)
            {
                continuation(state);
                return;
            }

            if (_singleContinuation == null)
            {
                _singleContinuation = continuation;
                _singleState = state;
            }
            else
            {
                if (_secondaryContinuationList == null) _secondaryContinuationList = new();
                _secondaryContinuationList.Add((continuation, state));
            }
        }
    }

    [DebuggerHidden]
    private bool TrySignalCompletion(GDTaskStatus status, Action storePayload)
    {
        if (_gate == null)
        {
#if NET9_0_OR_GREATER
            Interlocked.CompareExchange(ref _gate, new(), null);
#else
            Interlocked.CompareExchange(ref _gate, new(), null);
#endif
        }

        var lockGate = Volatile.Read(ref _gate);

        lock (lockGate!) // wait OnCompleted.
        {
            if ((GDTaskStatus)Volatile.Read(ref _intStatus) != GDTaskStatus.Pending)
            {
                return false;
            }

            storePayload();
            Volatile.Write(ref _intStatus, (int)status);

            if (_singleContinuation != null)
                try { _singleContinuation(_singleState); }
                catch (Exception ex) { GDTaskExceptionHandler.PublishUnobservedTaskException(ex); }

            if (_secondaryContinuationList != null)
                foreach (var (continuation, continuationState) in _secondaryContinuationList)
                    try { continuation(continuationState); }
                    catch (Exception ex) { GDTaskExceptionHandler.PublishUnobservedTaskException(ex); }

            _singleContinuation = null;
            _singleState = null;
            _secondaryContinuationList = null;

            return true;
        }
    }
}

/// <summary>
/// Represents the producer side of a <see cref="GDTask{T}" /> unbound to a delegate,
/// providing access to the consumer side through the <see cref="GDTask{T}" /> property.
/// </summary>
/// <remarks>
/// It is often the case that a <see cref="GDTask{T}" /> is desired to represent another asynchronous operation.
/// <see cref="GDTaskCompletionSource{T}">GDTaskCompletionSource{T}</see> is provided for this purpose. It enables
/// the creation of a task that can be handed out to consumers, and those consumers can use the members
/// of the task as they would any other. However, unlike most tasks, the state of a task created by a
/// GDTaskCompletionSource{T} is controlled explicitly by the methods on GDTaskCompletionSource{T}. This enables the
/// completion of the external asynchronous operation to be propagated to the underlying GDTask{T}. The
/// separation also ensures that consumers are not able to transition the state without access to the
/// corresponding GDTaskCompletionSource{T}.
/// <para>
/// All members of <see cref="GDTaskCompletionSource{T}" /> are thread-safe
/// and may be used from multiple threads concurrently.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of the result value associated with this GDTask.</typeparam>
public class GDTaskCompletionSource<T> : IGDTaskSource<T>, IPromise<T>
{
    private CancellationToken _cancellationToken;
    private T _result;
    private ExceptionHolder _exception;

#if NET9_0_OR_GREATER
        private Lock _gate;
#else
    private object _gate;
#endif

    private Action<object> _singleContinuation;
    private object _singleState;
    private List<(Action<object>, object)> _secondaryContinuationList;

    private int _intStatus; // GDTaskStatus
    private bool _handled;

    /// <summary> Initializes a new instance of the <see cref="GDTaskCompletionSource{T}" /> class.</summary>
    public GDTaskCompletionSource()
    {
        TaskTracker.TrackActiveTask(this, 2);
    }

    [DebuggerHidden]
    internal void MarkHandled()
    {
        if (!_handled)
        {
            _handled = true;
            TaskTracker.RemoveTracking(this);
        }
    }

    /// <summary>
    /// Gets the <see cref="GDTask{T}" /> created
    /// by this <see cref="GDTaskCompletionSource{T}" />.
    /// </summary>
    /// <remarks>
    /// This property enables a consumer access to the <see cref="GDTask{T}" /> that is controlled by this instance.
    /// The <see cref="TrySetResult" />, <see cref="TrySetException
    /// (Exception)" />, and <see cref="TrySetCanceled" /> methods
    /// (and their "Try" variants) on this instance
    /// all result in the relevant state transitions on this underlying GDTask{T}.
    /// </remarks>
    public GDTask<T> Task
    {
        [DebuggerHidden]
        get => new(this, 0);
    }

    /// <summary>
    /// Attempts to transition the underlying <see cref="GDTask{T}" /> into the <see cref="GDTaskStatus.Succeeded" /> state.
    /// </summary>
    /// <param name="result">The result to set.</param>
    /// <returns>True if the operation was successful; otherwise, false.</returns>
    [DebuggerHidden]
    public bool TrySetResult(T result)
    {
        return TrySignalCompletion(GDTaskStatus.Succeeded, () => _result = result);
    }

    /// <summary>
    /// Attempts to transition the underlying <see cref="GDTask{T}" /> into the <see cref="GDTaskStatus.Canceled" /> state.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to set.</param>
    /// <returns>True if the operation was successful; otherwise, false.</returns>
    [DebuggerHidden]
    public bool TrySetCanceled(CancellationToken cancellationToken = default)
    {
        return TrySignalCompletion(GDTaskStatus.Canceled, () => _cancellationToken = cancellationToken);
    }

    /// <summary>
    /// Attempts to transition the underlying <see cref="GDTask{T}" /> into the <see cref="GDTaskStatus.Faulted" /> state.
    /// </summary>
    /// <param name="exception">The exception to set.</param>
    /// <returns>True if the operation was successful; otherwise, false.</returns>
    [DebuggerHidden]
    public bool TrySetException(Exception exception)
    {
        if (exception is OperationCanceledException oce) return TrySetCanceled(oce.CancellationToken);

        return TrySignalCompletion(GDTaskStatus.Faulted, () => _exception = new(ExceptionDispatchInfo.Capture(exception)));
    }

    /// <summary>
    /// Gets the result of the underlying <see cref="GDTask{T}" />.
    /// </summary>
    /// <param name="token">The token to use for the operation.</param>
    /// <returns>The result of the operation.</returns>
    /// <remarks>
    /// This method is used by the compiler to implement the await operator.
    /// It is not intended to be called directly by user code.
    /// </remarks>
    [DebuggerHidden]
    public T GetResult(short token)
    {
        MarkHandled();

        var status = (GDTaskStatus)Volatile.Read(ref _intStatus);

        switch (status)
        {
            case GDTaskStatus.Succeeded:
                return _result;
            case GDTaskStatus.Faulted:
                _exception.GetException().Throw();
                return default;
            case GDTaskStatus.Canceled:
                throw new OperationCanceledException(_cancellationToken);
            default:
            case GDTaskStatus.Pending:
                throw new InvalidOperationException("not yet completed.");
        }
    }

    [DebuggerHidden]
    void IGDTaskSource.GetResult(short token) => GetResult(token);

    /// <summary>
    /// Gets the status of the underlying <see cref="GDTask{T}" />.
    /// </summary>
    /// <returns>The status of the task.</returns>
    /// <remarks>
    /// This method is used by the compiler to implement the await operator.
    /// It is not intended to be called directly by user code.
    /// </remarks>
    [DebuggerHidden]
    public GDTaskStatus GetStatus(short token) => (GDTaskStatus)Volatile.Read(ref _intStatus);

    /// <summary>
    /// Gets the status of the underlying <see cref="GDTask{T}" /> without
    /// </summary>
    /// <returns>The status of the task.</returns>
    /// <remarks>
    /// This method is used by the compiler to implement the await operator.
    /// It is not intended to be called directly by user code.
    /// </remarks>
    [DebuggerHidden]
    public GDTaskStatus UnsafeGetStatus() => (GDTaskStatus)Volatile.Read(ref _intStatus);

    /// <summary>
    /// Schedules the continuation action for this operation.
    /// </summary>
    /// <param name="continuation">The continuation action to schedule.</param>
    /// <param name="state">The state to pass to the continuation action.</param>
    /// <param name="token">The token to use for the operation.</param>
    /// <remarks>
    /// This method is used by the compiler to implement the await operator.
    /// It is not intended to be called directly by user code.
    /// </remarks>
    [DebuggerHidden]
    public void OnCompleted(Action<object> continuation, object state, short token)
    {
        if (_gate == null)
        {
#if NET9_0_OR_GREATER
                Interlocked.CompareExchange(ref _gate, new(), null);
#else
            Interlocked.CompareExchange(ref _gate, new(), null);
#endif
        }

        var lockGate = Volatile.Read(ref _gate);

        lock (lockGate!) // wait TrySignalCompletion, after status is not pending.
        {
            if ((GDTaskStatus)Volatile.Read(ref _intStatus) != GDTaskStatus.Pending)
            {
                continuation(state);
                return;
            }

            if (_singleContinuation == null)
            {
                _singleContinuation = continuation;
                _singleState = state;
            }
            else
            {
                if (_secondaryContinuationList == null) _secondaryContinuationList = new();
                _secondaryContinuationList.Add((continuation, state));
            }
        }
    }

    [DebuggerHidden]
    private bool TrySignalCompletion(GDTaskStatus status, Action storePayload)
    {
        if (_gate == null)
        {
#if NET9_0_OR_GREATER
            Interlocked.CompareExchange(ref _gate, new(), null);
#else
            Interlocked.CompareExchange(ref _gate, new(), null);
#endif
        }

        var lockGate = Volatile.Read(ref _gate);

        lock (lockGate!) // wait OnCompleted.
        {
            if ((GDTaskStatus)Volatile.Read(ref _intStatus) != GDTaskStatus.Pending)
            {
                return false;
            }

            storePayload();
            Volatile.Write(ref _intStatus, (int)status);

            if (_singleContinuation != null)
                try { _singleContinuation(_singleState); }
                catch (Exception ex) { GDTaskExceptionHandler.PublishUnobservedTaskException(ex); }

            if (_secondaryContinuationList != null)
                foreach (var (continuation, continuationState) in _secondaryContinuationList)
                    try { continuation(continuationState); }
                    catch (Exception ex) { GDTaskExceptionHandler.PublishUnobservedTaskException(ex); }

            _singleContinuation = null;
            _singleState = null;
            _secondaryContinuationList = null;

            return true;
        }
    }
}
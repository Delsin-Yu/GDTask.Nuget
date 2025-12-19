using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace GodotTask
{
    internal interface IResolvePromise
    {
        bool TrySetResult();
    }

    internal interface IResolvePromise<in T>
    {
        bool TrySetResult(T value);
    }

    internal interface IRejectPromise
    {
        bool TrySetException(Exception exception);
    }

    internal interface ICancelPromise
    {
        bool TrySetCanceled(CancellationToken cancellationToken = default);
    }

    internal interface IPromise<in T> : IResolvePromise<T>, IRejectPromise, ICancelPromise
    {
    }

    internal interface IPromise : IResolvePromise, IRejectPromise, ICancelPromise
    {
    }

    internal class ExceptionHolder
    {
        private readonly ExceptionDispatchInfo exception;
        private bool calledGet = false;

        public ExceptionHolder(ExceptionDispatchInfo exception)
        {
            this.exception = exception;
        }

        public ExceptionDispatchInfo GetException()
        {
            if (!calledGet)
            {
                calledGet = true;
                GC.SuppressFinalize(this);
            }
            return exception;
        }

        ~ExceptionHolder()
        {
            if (!calledGet)
            {
                GDTaskExceptionHandler.PublishUnobservedTaskException(exception.SourceException);
            }
        }
    }

    [StructLayout(LayoutKind.Auto)]
    internal struct GDTaskCompletionSourceCore<TResult>
    {
        // Struct Size: TResult + (8 + 2 + 1 + 1 + 8 + 8)

        private TResult result;
        private object error; // ExceptionHolder or OperationCanceledException
        private short version;
        private bool hasUnhandledError;
        private int completedCount; // 0: completed == false
        private Action<object> continuation;
        private object continuationState;

        [DebuggerHidden]
        public void Reset()
        {
            ReportUnhandledError();

            unchecked
            {
                version += 1; // incr version.
            }
            completedCount = 0;
            result = default;
            error = null;
            hasUnhandledError = false;
            continuation = null;
            continuationState = null;
        }

        private void ReportUnhandledError()
        {
            if (hasUnhandledError)
            {
                try
                {
                    if (error is OperationCanceledException oc)
                    {
                        GDTaskExceptionHandler.PublishUnobservedTaskException(oc);
                    }
                    else if (error is ExceptionHolder e)
                    {
                        GDTaskExceptionHandler.PublishUnobservedTaskException(e.GetException().SourceException);
                    }
                }
                catch
                {
                }
            }
        }

        internal void MarkHandled()
        {
            hasUnhandledError = false;
        }

        /// <summary>Completes with a successful result.</summary>
        /// <param name="result">The result.</param>
        [DebuggerHidden]
        public bool TrySetResult(TResult result)
        {
            if (Interlocked.Increment(ref completedCount) == 1)
            {
                // setup result
                this.result = result;

                if (continuation != null || Interlocked.CompareExchange(ref continuation, GDTaskCompletionSourceCoreShared.s_sentinel, null) != null)
                {
                    continuation(continuationState);
                    return true;
                }
            }

            return false;
        }

        /// <summary>Completes with an error.</summary>
        /// <param name="error">The exception.</param>
        [DebuggerHidden]
        public bool TrySetException(Exception error)
        {
            if (Interlocked.Increment(ref completedCount) == 1)
            {
                // setup result
                hasUnhandledError = true;
                if (error is OperationCanceledException)
                {
                    this.error = error;
                }
                else
                {
                    this.error = new ExceptionHolder(ExceptionDispatchInfo.Capture(error));
                }

                if (continuation != null || Interlocked.CompareExchange(ref continuation, GDTaskCompletionSourceCoreShared.s_sentinel, null) != null)
                {
                    continuation(continuationState);
                    return true;
                }
            }

            return false;
        }

        [DebuggerHidden]
        public bool TrySetCanceled(CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref completedCount) == 1)
            {
                // setup result
                hasUnhandledError = true;
                error = new OperationCanceledException(cancellationToken);

                if (continuation != null || Interlocked.CompareExchange(ref continuation, GDTaskCompletionSourceCoreShared.s_sentinel, null) != null)
                {
                    continuation(continuationState);
                    return true;
                }
            }

            return false;
        }

        /// <summary>Gets the operation version.</summary>
        [DebuggerHidden]
        public short Version => version;

        /// <summary>Gets the status of the operation.</summary>
        /// <param name="token">Opaque value that was provided to the <see cref="GDTask"/>'s constructor.</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GDTaskStatus GetStatus(short token)
        {
            ValidateToken(token);
            return (continuation == null || (completedCount == 0)) ? GDTaskStatus.Pending
                 : (error == null) ? GDTaskStatus.Succeeded
                 : (error is OperationCanceledException) ? GDTaskStatus.Canceled
                 : GDTaskStatus.Faulted;
        }

        /// <summary>Gets the status of the operation without token validation.</summary>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GDTaskStatus UnsafeGetStatus()
        {
            return (continuation == null || (completedCount == 0)) ? GDTaskStatus.Pending
                 : (error == null) ? GDTaskStatus.Succeeded
                 : (error is OperationCanceledException) ? GDTaskStatus.Canceled
                 : GDTaskStatus.Faulted;
        }

        /// <summary>Gets the result of the operation.</summary>
        /// <param name="token">Opaque value that was provided to the <see cref="GDTask"/>'s constructor.</param>
        // [StackTraceHidden]
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult GetResult(short token)
        {
            ValidateToken(token);
            if (completedCount == 0)
            {
                throw new InvalidOperationException("Not yet completed, GDTask only allow to use await.");
            }

            if (error != null)
            {
                hasUnhandledError = false;
                if (error is OperationCanceledException oce)
                {
                    throw oce;
                }
                else if (error is ExceptionHolder eh)
                {
                    eh.GetException().Throw();
                }

                throw new InvalidOperationException("Critical: invalid exception type was held.");
            }

            return result;
        }

        /// <summary>Schedules the continuation action for this operation.</summary>
        /// <param name="continuation">The continuation to invoke when the operation has completed.</param>
        /// <param name="state">The state object to pass to <paramref name="continuation"/> when it's invoked.</param>
        /// <param name="token">Opaque value that was provided to the <see cref="GDTask"/>'s constructor.</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action<object> continuation, object state, short token /*, ValueTaskSourceOnCompletedFlags flags */)
        {
            if (continuation == null)
            {
                throw new ArgumentNullException(nameof(continuation));
            }
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
            object oldContinuation = this.continuation;
            if (oldContinuation == null)
            {
                continuationState = state;
                oldContinuation = Interlocked.CompareExchange(ref this.continuation, continuation, null);
            }

            if (oldContinuation != null)
            {
                // already running continuation in TrySet.
                // It will cause call OnCompleted multiple time, invalid.
                if (!ReferenceEquals(oldContinuation, GDTaskCompletionSourceCoreShared.s_sentinel))
                {
                    throw new InvalidOperationException("Already continuation registered, can not await twice or get Status after await.");
                }

                continuation(state);
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateToken(short token)
        {
            if (token != version)
            {
                throw new InvalidOperationException("Token version is not matched, can not await twice or get Status after await.");
            }
        }
    }

    internal static class GDTaskCompletionSourceCoreShared // separated out of generic to avoid unnecessary duplication
    {
        internal static readonly Action<object> s_sentinel = CompletionSentinel;

        private static void CompletionSentinel(object _) // named method to aid debugging
        {
            throw new InvalidOperationException("The sentinel delegate should never be invoked.");
        }
    }

    internal class AutoResetGDTaskCompletionSource : IGDTaskSource, ITaskPoolNode<AutoResetGDTaskCompletionSource>, IPromise
    {
        private static TaskPool<AutoResetGDTaskCompletionSource> pool;
        private AutoResetGDTaskCompletionSource nextNode;
        public ref AutoResetGDTaskCompletionSource NextNode => ref nextNode;

        static AutoResetGDTaskCompletionSource()
        {
            TaskPool.RegisterSizeGetter(typeof(AutoResetGDTaskCompletionSource), () => pool.Size);
        }

        private GDTaskCompletionSourceCore<AsyncUnit> core;

        private AutoResetGDTaskCompletionSource()
        {
        }

        [DebuggerHidden]
        public static AutoResetGDTaskCompletionSource Create()
        {
            if (!pool.TryPop(out var result))
            {
                result = new AutoResetGDTaskCompletionSource();
            }
            TaskTracker.TrackActiveTask(result, 2);
            return result;
        }

        [DebuggerHidden]
        public static AutoResetGDTaskCompletionSource CreateFromCanceled(CancellationToken cancellationToken, out short token)
        {
            var source = Create();
            source.TrySetCanceled(cancellationToken);
            token = source.core.Version;
            return source;
        }

        [DebuggerHidden]
        public static AutoResetGDTaskCompletionSource CreateFromException(Exception exception, out short token)
        {
            var source = Create();
            source.TrySetException(exception);
            token = source.core.Version;
            return source;
        }

        [DebuggerHidden]
        public static AutoResetGDTaskCompletionSource CreateCompleted(out short token)
        {
            var source = Create();
            source.TrySetResult();
            token = source.core.Version;
            return source;
        }

        public GDTask Task
        {
            [DebuggerHidden]
            get => new(this, core.Version);
        }

        [DebuggerHidden]
        public bool TrySetResult()
        {
            return core.TrySetResult(AsyncUnit.Default);
        }

        [DebuggerHidden]
        public bool TrySetCanceled(CancellationToken cancellationToken = default)
        {
            return core.TrySetCanceled(cancellationToken);
        }

        [DebuggerHidden]
        public bool TrySetException(Exception exception)
        {
            return core.TrySetException(exception);
        }

        [DebuggerHidden]
        public void GetResult(short token)
        {
            try
            {
                core.GetResult(token);
            }
            finally
            {
                TryReturn();
            }

        }

        [DebuggerHidden]
        public GDTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        [DebuggerHidden]
        public GDTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        [DebuggerHidden]
        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        [DebuggerHidden]
        private bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            core.Reset();
            return pool.TryPush(this);
        }
    }

    internal class AutoResetGDTaskCompletionSource<T> : IGDTaskSource<T>, ITaskPoolNode<AutoResetGDTaskCompletionSource<T>>, IPromise<T>
    {
        private static TaskPool<AutoResetGDTaskCompletionSource<T>> pool;
        private AutoResetGDTaskCompletionSource<T> nextNode;
        public ref AutoResetGDTaskCompletionSource<T> NextNode => ref nextNode;

        static AutoResetGDTaskCompletionSource()
        {
            TaskPool.RegisterSizeGetter(typeof(AutoResetGDTaskCompletionSource<T>), () => pool.Size);
        }

        private GDTaskCompletionSourceCore<T> core;

        private AutoResetGDTaskCompletionSource()
        {
        }

        [DebuggerHidden]
        public static AutoResetGDTaskCompletionSource<T> Create()
        {
            if (!pool.TryPop(out var result))
            {
                result = new AutoResetGDTaskCompletionSource<T>();
            }
            TaskTracker.TrackActiveTask(result, 2);
            return result;
        }

        [DebuggerHidden]
        public static AutoResetGDTaskCompletionSource<T> CreateFromCanceled(CancellationToken cancellationToken, out short token)
        {
            var source = Create();
            source.TrySetCanceled(cancellationToken);
            token = source.core.Version;
            return source;
        }

        [DebuggerHidden]
        public static AutoResetGDTaskCompletionSource<T> CreateFromException(Exception exception, out short token)
        {
            var source = Create();
            source.TrySetException(exception);
            token = source.core.Version;
            return source;
        }

        [DebuggerHidden]
        public static AutoResetGDTaskCompletionSource<T> CreateFromResult(T result, out short token)
        {
            var source = Create();
            source.TrySetResult(result);
            token = source.core.Version;
            return source;
        }

        public GDTask<T> Task
        {
            [DebuggerHidden]
            get => new(this, core.Version);
        }

        [DebuggerHidden]
        public bool TrySetResult(T result)
        {
            return core.TrySetResult(result);
        }

        [DebuggerHidden]
        public bool TrySetCanceled(CancellationToken cancellationToken = default)
        {
            return core.TrySetCanceled(cancellationToken);
        }

        [DebuggerHidden]
        public bool TrySetException(Exception exception)
        {
            return core.TrySetException(exception);
        }

        [DebuggerHidden]
        public T GetResult(short token)
        {
            try
            {
                return core.GetResult(token);
            }
            finally
            {
                TryReturn();
            }
        }

        [DebuggerHidden]
        void IGDTaskSource.GetResult(short token)
        {
            GetResult(token);
        }

        [DebuggerHidden]
        public GDTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        [DebuggerHidden]
        public GDTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        [DebuggerHidden]
        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        [DebuggerHidden]
        private bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            core.Reset();
            return pool.TryPush(this);
        }
    }

    /// <summary>
    /// Represents the producer side of a <see cref="GDTask"/> unbound to a
    /// delegate, providing access to the consumer side through the <see cref="GDTask"/> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It is often the case that a <see cref="GDTask"/> is desired to
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
    /// All members of <see cref="GDTaskCompletionSource"/> are thread-safe
    /// and may be used from multiple threads concurrently.
    /// </para>
    /// </remarks>
    public class GDTaskCompletionSource : IGDTaskSource, IPromise
    {
        private CancellationToken cancellationToken;
        private ExceptionHolder exception;

#if NET9_0_OR_GREATER
        private Lock gate;
#else
        private object gate;
#endif

        private Action<object> singleContinuation;
        private object singleState;
        private List<(Action<object>, object)> secondaryContinuationList;

        private int intStatus; // GDTaskStatus
        private bool handled = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="GDTaskCompletionSource"/> class.
        /// </summary>
        public GDTaskCompletionSource()
        {
            TaskTracker.TrackActiveTask(this, 2);
        }

        [DebuggerHidden]
        internal void MarkHandled()
        {
            if (!handled)
            {
                handled = true;
                TaskTracker.RemoveTracking(this);
            }
        }

        /// <summary>
        /// Gets the <see cref="GDTask"/> created
        /// by this <see cref="GDTaskCompletionSource"/>.
        /// </summary>
        /// <remarks>
        /// This property enables a consumer access to the <see cref="GDTask"/> that is controlled by this instance.
        /// The <see cref="TrySetResult"/>, <see cref="TrySetException(Exception)"/>,
        /// and <see cref="TrySetCanceled"/> methods (and their "Try" variants) on this instance all result in the relevant state
        /// transitions on this underlying GDTask.
        /// </remarks>
        public GDTask Task
        {
            [DebuggerHidden]
            get => new(this, 0);
        }

        /// <summary>
        /// Attempts to transition the underlying <see cref="GDTask"/> into the <see cref="GDTaskStatus.Succeeded"/> state.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        [DebuggerHidden]
        public bool TrySetResult()
        {
            return TrySignalCompletion(GDTaskStatus.Succeeded);
        }

        /// <summary>
        /// Attempts to transition the underlying <see cref="GDTask"/> into the <see cref="GDTaskStatus.Canceled"/> state.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        [DebuggerHidden]
        public bool TrySetCanceled(CancellationToken cancellationToken = default)
        {
            if (UnsafeGetStatus() != GDTaskStatus.Pending) return false;

            this.cancellationToken = cancellationToken;
            return TrySignalCompletion(GDTaskStatus.Canceled);
        }

        /// <summary>
        /// Attempts to transition the underlying <see cref="GDTask"/> into the <see cref="GDTaskStatus.Faulted"/> state.
        /// </summary>
        /// <param name="exception">The exception to bind to this <see cref="GDTask"/>.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        [DebuggerHidden]
        public bool TrySetException(Exception exception)
        {
            if (exception is OperationCanceledException oce)
            {
                return TrySetCanceled(oce.CancellationToken);
            }

            if (UnsafeGetStatus() != GDTaskStatus.Pending) return false;

            this.exception = new ExceptionHolder(ExceptionDispatchInfo.Capture(exception));
            return TrySignalCompletion(GDTaskStatus.Faulted);
        }

        /// <summary>
        /// Gets the result of the underlying <see cref="GDTask"/>.
        /// </summary>
        /// <remarks>
        /// This method is used by the compiler to implement the await operator.
        /// It is not intended to be called directly by user code.
        /// </remarks>
        [DebuggerHidden]
        public void GetResult(short token)
        {
            MarkHandled();

            var status = (GDTaskStatus)intStatus;
            switch (status)
            {
                case GDTaskStatus.Succeeded:
                    return;
                case GDTaskStatus.Faulted:
                    exception.GetException().Throw();
                    return;
                case GDTaskStatus.Canceled:
                    throw new OperationCanceledException(cancellationToken);
                default:
                case GDTaskStatus.Pending:
                    throw new InvalidOperationException("not yet completed.");
            }
        }

        /// <summary>
        /// Gets the status of the underlying <see cref="GDTask"/>.
        /// </summary>
        /// <remarks>
        /// This method is used by the compiler to implement the await operator.
        /// It is not intended to be called directly by user code.
        /// </remarks>
        [DebuggerHidden]
        public GDTaskStatus GetStatus(short token)
        {
            return (GDTaskStatus)intStatus;
        }

        /// <summary>
        /// Gets the status of the underlying <see cref="GDTask"/> without validating the token
        /// or checking if the task is completed.
        /// </summary>
        /// <remarks>
        /// This method is used by the compiler to implement the await operator.
        /// It is not intended to be called directly by user code.
        /// </remarks>
        [DebuggerHidden]
        public GDTaskStatus UnsafeGetStatus()
        {
            return (GDTaskStatus)intStatus;
        }

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
            if (gate == null)
            {
#if NET9_0_OR_GREATER
                Interlocked.CompareExchange(ref gate, new Lock(), null);
#else
                Interlocked.CompareExchange(ref gate, new object(), null);
#endif
            }

            var lockGate = Volatile.Read(ref gate);
            lock (lockGate) // wait TrySignalCompletion, after status is not pending.
            {
                if ((GDTaskStatus)intStatus != GDTaskStatus.Pending)
                {
                    continuation(state);
                    return;
                }

                if (singleContinuation == null)
                {
                    singleContinuation = continuation;
                    singleState = state;
                }
                else
                {
                    if (secondaryContinuationList == null)
                    {
                        secondaryContinuationList = new List<(Action<object>, object)>();
                    }
                    secondaryContinuationList.Add((continuation, state));
                }
            }
        }

        [DebuggerHidden]
        private bool TrySignalCompletion(GDTaskStatus status)
        {
            if (Interlocked.CompareExchange(ref intStatus, (int)status, (int)GDTaskStatus.Pending) == (int)GDTaskStatus.Pending)
            {
                if (gate == null)
                {
#if NET9_0_OR_GREATER
                    Interlocked.CompareExchange(ref gate, new Lock(), null);
#else
                    Interlocked.CompareExchange(ref gate, new object(), null);
#endif
                }

                var lockGate = Volatile.Read(ref gate);
                lock (lockGate) // wait OnCompleted.
                {
                    if (singleContinuation != null)
                    {
                        try
                        {
                            singleContinuation(singleState);
                        }
                        catch (Exception ex)
                        {
                            GDTaskExceptionHandler.PublishUnobservedTaskException(ex);
                        }
                    }

                    if (secondaryContinuationList != null)
                    {
                        foreach (var (c, state) in secondaryContinuationList)
                        {
                            try
                            {
                                c(state);
                            }
                            catch (Exception ex)
                            {
                                GDTaskExceptionHandler.PublishUnobservedTaskException(ex);
                            }
                        }
                    }

                    singleContinuation = null;
                    singleState = null;
                    secondaryContinuationList = null;
                }
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Represents the producer side of a <see cref="GDTask{T}"/> unbound to a delegate,
    /// providing access to the consumer side through the <see cref="GDTask{T}"/> property.
    /// </summary>
    /// <remarks>
    /// It is often the case that a <see cref="GDTask{T}"/> is desired to represent another asynchronous operation.
    /// <see cref="GDTaskCompletionSource{T}">GDTaskCompletionSource{T}</see> is provided for this purpose. It enables
    /// the creation of a task that can be handed out to consumers, and those consumers can use the members
    /// of the task as they would any other. However, unlike most tasks, the state of a task created by a
    /// GDTaskCompletionSource{T} is controlled explicitly by the methods on GDTaskCompletionSource{T}. This enables the
    /// completion of the external asynchronous operation to be propagated to the underlying GDTask{T}. The
    /// separation also ensures that consumers are not able to transition the state without access to the
    /// corresponding GDTaskCompletionSource{T}.
    /// </remarks>
    /// <typeparam name="T">The type of the result value associated with this GDTask.</typeparam>
    public class GDTaskCompletionSource<T> : IGDTaskSource<T>, IPromise<T>
    {
        private CancellationToken cancellationToken;
        private T result;
        private ExceptionHolder exception;
        
#if NET9_0_OR_GREATER
        private Lock gate;
#else
        private object gate;
#endif

        private Action<object> singleContinuation;
        private object singleState;
        private List<(Action<object>, object)> secondaryContinuationList;

        private int intStatus; // GDTaskStatus
        private bool handled = false;

        /// <summary> Initializes a new instance of the <see cref="GDTaskCompletionSource{T}"/> class.</summary>
        public GDTaskCompletionSource()
        {
            TaskTracker.TrackActiveTask(this, 2);
        }

        [DebuggerHidden]
        internal void MarkHandled()
        {
            if (!handled)
            {
                handled = true;
                TaskTracker.RemoveTracking(this);
            }
        }

        /// <summary>
        /// Gets the <see cref="GDTask{T}"/> created
        /// by this <see cref="GDTaskCompletionSource{T}"/>.
        /// </summary>
        /// <remarks>
        /// This property enables a consumer access to the <see cref="GDTask{T}"/> that is controlled by this instance.
        /// The <see cref="TrySetResult"/>, <see cref="TrySetException
        /// (Exception)"/>, and <see cref="TrySetCanceled"/> methods (and their "Try" variants) on this instance
        /// all result in the relevant state transitions on this underlying GDTask{T}.
        /// </remarks>
        public GDTask<T> Task
        {
            [DebuggerHidden]
            get => new(this, 0);
        }

        /// <summary>
        /// Attempts to transition the underlying <see cref="GDTask{T}"/> into the <see cref="GDTaskStatus.Succeeded"/> state.
        /// </summary>
        /// <param name="result">The result to set.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        [DebuggerHidden]
        public bool TrySetResult(T result)
        {
            if (UnsafeGetStatus() != GDTaskStatus.Pending) return false;

            this.result = result;
            return TrySignalCompletion(GDTaskStatus.Succeeded);
        }

        /// <summary>
        /// Attempts to transition the underlying <see cref="GDTask{T}"/> into the <see cref="GDTaskStatus.Canceled"/> state.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to set.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        [DebuggerHidden]
        public bool TrySetCanceled(CancellationToken cancellationToken = default)
        {
            if (UnsafeGetStatus() != GDTaskStatus.Pending) return false;

            this.cancellationToken = cancellationToken;
            return TrySignalCompletion(GDTaskStatus.Canceled);
        }

        /// <summary>
        /// Attempts to transition the underlying <see cref="GDTask{T}"/> into the <see cref="GDTaskStatus.Faulted"/> state.
        /// </summary>
        /// <param name="exception">The exception to set.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        [DebuggerHidden]
        public bool TrySetException(Exception exception)
        {
            if (exception is OperationCanceledException oce)
            {
                return TrySetCanceled(oce.CancellationToken);
            }

            if (UnsafeGetStatus() != GDTaskStatus.Pending) return false;

            this.exception = new ExceptionHolder(ExceptionDispatchInfo.Capture(exception));
            return TrySignalCompletion(GDTaskStatus.Faulted);
        }

        /// <summary>
        /// Gets the result of the underlying <see cref="GDTask{T}"/>.
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

            var status = (GDTaskStatus)intStatus;
            switch (status)
            {
                case GDTaskStatus.Succeeded:
                    return result;
                case GDTaskStatus.Faulted:
                    exception.GetException().Throw();
                    return default;
                case GDTaskStatus.Canceled:
                    throw new OperationCanceledException(cancellationToken);
                default:
                case GDTaskStatus.Pending:
                    throw new InvalidOperationException("not yet completed.");
            }
        }

        [DebuggerHidden]
        void IGDTaskSource.GetResult(short token)
        {
            GetResult(token);
        }

        /// <summary>
        /// Gets the status of the underlying <see cref="GDTask{T}"/>.
        /// </summary>
        /// <returns>The status of the task.</returns>
        /// <remarks>
        /// This method is used by the compiler to implement the await operator.
        /// It is not intended to be called directly by user code.
        /// </remarks>
        [DebuggerHidden]
        public GDTaskStatus GetStatus(short token)
        {
            return (GDTaskStatus)intStatus;
        }

        /// <summary>
        /// Gets the status of the underlying <see cref="GDTask{T}"/> without
        /// </summary>
        /// <returns>The status of the task.</returns>
        /// <remarks>
        /// This method is used by the compiler to implement the await operator.
        /// It is not intended to be called directly by user code.
        /// </remarks>
        [DebuggerHidden]
        public GDTaskStatus UnsafeGetStatus()
        {
            return (GDTaskStatus)intStatus;
        }

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
            if (gate == null)
            {
#if NET9_0_OR_GREATER
                Interlocked.CompareExchange(ref gate, new Lock(), null);
#else
                Interlocked.CompareExchange(ref gate, new object(), null);
#endif
            }

            var lockGate = Volatile.Read(ref gate);
            lock (lockGate) // wait TrySignalCompletion, after status is not pending.
            {
                if ((GDTaskStatus)intStatus != GDTaskStatus.Pending)
                {
                    continuation(state);
                    return;
                }

                if (singleContinuation == null)
                {
                    singleContinuation = continuation;
                    singleState = state;
                }
                else
                {
                    if (secondaryContinuationList == null)
                    {
                        secondaryContinuationList = new List<(Action<object>, object)>();
                    }
                    secondaryContinuationList.Add((continuation, state));
                }
            }
        }

        [DebuggerHidden]
        private bool TrySignalCompletion(GDTaskStatus status)
        {
            if (Interlocked.CompareExchange(ref intStatus, (int)status, (int)GDTaskStatus.Pending) == (int)GDTaskStatus.Pending)
            {
                if (gate == null)
                {
#if NET9_0_OR_GREATER
                    Interlocked.CompareExchange(ref gate, new Lock(), null);
#else
                    Interlocked.CompareExchange(ref gate, new object(), null);
#endif
                }

                var lockGate = Volatile.Read(ref gate);
                lock (lockGate) // wait OnCompleted.
                {
                    if (singleContinuation != null)
                    {
                        try
                        {
                            singleContinuation(singleState);
                        }
                        catch (Exception ex)
                        {
                            GDTaskExceptionHandler.PublishUnobservedTaskException(ex);
                        }
                    }

                    if (secondaryContinuationList != null)
                    {
                        foreach (var (c, state) in secondaryContinuationList)
                        {
                            try
                            {
                                c(state);
                            }
                            catch (Exception ex)
                            {
                                GDTaskExceptionHandler.PublishUnobservedTaskException(ex);
                            }
                        }
                    }

                    singleContinuation = null;
                    singleState = null;
                    secondaryContinuationList = null;
                }
                return true;
            }
            return false;
        }
    }
}
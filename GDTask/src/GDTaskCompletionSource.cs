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

    internal class GDTaskCompletionSource : IGDTaskSource, IPromise
    {
        private CancellationToken cancellationToken;
        private ExceptionHolder exception;
        private object gate;
        private Action<object> singleContinuation;
        private object singleState;
        private List<(Action<object>, object)> secondaryContinuationList;

        private int intStatus; // GDTaskStatus
        private bool handled = false;

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

        public GDTask Task
        {
            [DebuggerHidden]
            get => new(this, 0);
        }

        [DebuggerHidden]
        public bool TrySetResult()
        {
            return TrySignalCompletion(GDTaskStatus.Succeeded);
        }

        [DebuggerHidden]
        public bool TrySetCanceled(CancellationToken cancellationToken = default)
        {
            if (UnsafeGetStatus() != GDTaskStatus.Pending) return false;

            this.cancellationToken = cancellationToken;
            return TrySignalCompletion(GDTaskStatus.Canceled);
        }

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

        [DebuggerHidden]
        public GDTaskStatus GetStatus(short token)
        {
            return (GDTaskStatus)intStatus;
        }

        [DebuggerHidden]
        public GDTaskStatus UnsafeGetStatus()
        {
            return (GDTaskStatus)intStatus;
        }

        [DebuggerHidden]
        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            if (gate == null)
            {
                Interlocked.CompareExchange(ref gate, new object(), null);
            }

            var lockGate = Thread.VolatileRead(ref gate);
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
                    Interlocked.CompareExchange(ref gate, new object(), null);
                }

                var lockGate = Thread.VolatileRead(ref gate);
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

    internal class GDTaskCompletionSource<T> : IGDTaskSource<T>, IPromise<T>
    {
        private CancellationToken cancellationToken;
        private T result;
        private ExceptionHolder exception;
        private object gate;
        private Action<object> singleContinuation;
        private object singleState;
        private List<(Action<object>, object)> secondaryContinuationList;

        private int intStatus; // GDTaskStatus
        private bool handled = false;

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

        public GDTask<T> Task
        {
            [DebuggerHidden]
            get => new(this, 0);
        }

        [DebuggerHidden]
        public bool TrySetResult(T result)
        {
            if (UnsafeGetStatus() != GDTaskStatus.Pending) return false;

            this.result = result;
            return TrySignalCompletion(GDTaskStatus.Succeeded);
        }

        [DebuggerHidden]
        public bool TrySetCanceled(CancellationToken cancellationToken = default)
        {
            if (UnsafeGetStatus() != GDTaskStatus.Pending) return false;

            this.cancellationToken = cancellationToken;
            return TrySignalCompletion(GDTaskStatus.Canceled);
        }

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

        [DebuggerHidden]
        public GDTaskStatus GetStatus(short token)
        {
            return (GDTaskStatus)intStatus;
        }

        [DebuggerHidden]
        public GDTaskStatus UnsafeGetStatus()
        {
            return (GDTaskStatus)intStatus;
        }

        [DebuggerHidden]
        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            if (gate == null)
            {
                Interlocked.CompareExchange(ref gate, new object(), null);
            }

            var lockGate = Thread.VolatileRead(ref gate);
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
                    Interlocked.CompareExchange(ref gate, new object(), null);
                }

                var lockGate = Thread.VolatileRead(ref gate);
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
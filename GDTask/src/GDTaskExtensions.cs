using System;
using System.Threading;
using System.Threading.Tasks;
using GodotTask.Internal;

namespace GodotTask;

/// <summary>
/// Provides extensions methods for <see cref="GDTask" />.
/// </summary>
public static partial class GDTaskExtensions
{
    /// <summary>
    /// Create a <see cref="GDTask" /> that wraps around this task.
    /// </summary>
    public static GDTask<T> AsGDTask<T>(this Task<T> task, bool useCurrentSynchronizationContext = true)
    {
        var promise = new GDTaskCompletionSource<T>();

        task.ContinueWith(
            (x, state) =>
            {
                var p = (GDTaskCompletionSource<T>)state;

                switch (x.Status)
                {
                    case TaskStatus.Canceled:
                        p.TrySetCanceled();
                        break;
                    case TaskStatus.Faulted:
                        p.TrySetException(x.Exception);
                        break;
                    case TaskStatus.RanToCompletion:
                        p.TrySetResult(x.Result);
                        break;
                    default:
                        throw new NotSupportedException();
                }
            },
            promise,
            useCurrentSynchronizationContext ? TaskScheduler.FromCurrentSynchronizationContext() : TaskScheduler.Current
        );

        return promise.Task;
    }

    /// <inheritdoc cref="AsGDTask{T}(Task{T},bool)" />
    public static GDTask AsGDTask(this Task task, bool useCurrentSynchronizationContext = true)
    {
        var promise = new GDTaskCompletionSource();

        task.ContinueWith(
            (x, state) =>
            {
                var p = (GDTaskCompletionSource)state;

                switch (x.Status)
                {
                    case TaskStatus.Canceled:
                        p.TrySetCanceled();
                        break;
                    case TaskStatus.Faulted:
                        p.TrySetException(x.Exception);
                        break;
                    case TaskStatus.RanToCompletion:
                        p.TrySetResult();
                        break;
                    default:
                        throw new NotSupportedException();
                }
            },
            promise,
            useCurrentSynchronizationContext ? TaskScheduler.FromCurrentSynchronizationContext() : TaskScheduler.Current
        );

        return promise.Task;
    }

    extension<T>(GDTask<T> task)
    {
        /// <summary>
        /// Create a <see cref="Task" /> that wraps around this task.
        /// </summary>
        public Task<T> AsTask()
        {
            try
            {
                GDTask<T>.Awaiter awaiter;

                try { awaiter = task.GetAwaiter(); }
                catch (Exception ex) { return Task.FromException<T>(ex); }

                if (awaiter.IsCompleted)
                    try
                    {
                        var result = awaiter.GetResult();
                        return Task.FromResult(result);
                    }
                    catch (Exception ex) { return Task.FromException<T>(ex); }

                var tcs = new TaskCompletionSource<T>();

                awaiter.SourceOnCompleted(
                    state =>
                    {
                        using var tuple = (StateTuple<TaskCompletionSource<T>, GDTask<T>.Awaiter>)state;
                        var (inTcs, inAwaiter) = tuple;

                        try
                        {
                            var result = inAwaiter.GetResult();
                            inTcs.SetResult(result);
                        }
                        catch (Exception ex) { inTcs.SetException(ex); }
                    },
                    StateTuple.Create(tcs, awaiter)
                );

                return tcs.Task;
            }
            catch (Exception ex) { return Task.FromException<T>(ex); }
        }

        /// <inheritdoc cref="ToAsyncLazy" />
        public IAsyncLazy<T> ToAsyncLazy() => new AsyncLazy<T>(task);

        /// <inheritdoc cref="AttachExternalCancellation" />
        public GDTask<T> AttachExternalCancellation(CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled) return task;

            if (cancellationToken.IsCancellationRequested) return GDTask.FromCanceled<T>(cancellationToken);

            if (task.Status.IsCompleted()) return task;

            return new(new AttachExternalCancellationSource<T>(task, cancellationToken), 0);
        }

        /// <inheritdoc cref="Timeout(GDTask, TimeSpan, DelayType, PlayerLoopTiming, CancellationTokenSource)" />
        public async GDTask<T> Timeout(TimeSpan timeout, DelayType delayType = DelayType.DeltaTime, PlayerLoopTiming timeoutCheckTiming = PlayerLoopTiming.Process, CancellationTokenSource taskCancellationTokenSource = null) => await task.Timeout(timeout, delayType, GDTaskScheduler.GetPlayerLoop(timeoutCheckTiming), taskCancellationTokenSource);

        /// <inheritdoc cref="Timeout(GDTask, TimeSpan, DelayType, IPlayerLoop, CancellationTokenSource)" />
        public async GDTask<T> Timeout(TimeSpan timeout, DelayType delayType, IPlayerLoop timeoutCheckLoop, CancellationTokenSource taskCancellationTokenSource = null)
        {
            Error.ThrowArgumentNullException(timeoutCheckLoop, nameof(timeoutCheckLoop));
            var delayCancellationTokenSource = new CancellationTokenSource();
            var timeoutTask = GDTask.Delay(timeout, delayType, timeoutCheckLoop, delayCancellationTokenSource.Token).SuppressCancellationThrow();

            int winArgIndex;
            (bool IsCanceled, T Result) taskResult;

            try { (winArgIndex, taskResult, _) = await GDTask.WhenAny(task.SuppressCancellationThrow(), timeoutTask); }
            catch
            {
                delayCancellationTokenSource.Cancel();
                delayCancellationTokenSource.Dispose();
                throw;
            }

            // timeout
            if (winArgIndex == 1)
            {
                if (taskCancellationTokenSource != null)
                {
                    taskCancellationTokenSource.Cancel();
                    taskCancellationTokenSource.Dispose();
                }

                throw new TimeoutException("Exceed Timeout:" + timeout);
            }

            delayCancellationTokenSource.Cancel();
            delayCancellationTokenSource.Dispose();

            if (taskResult.IsCanceled) Error.ThrowOperationCanceledException();

            return taskResult.Result;
        }

        /// <inheritdoc cref="TimeoutWithoutException(GDTask, TimeSpan, DelayType, PlayerLoopTiming, CancellationTokenSource)" />
        public async GDTask<(bool IsTimeout, T Result)> TimeoutWithoutException(TimeSpan timeout, DelayType delayType = DelayType.DeltaTime, PlayerLoopTiming timeoutCheckTiming = PlayerLoopTiming.Process, CancellationTokenSource taskCancellationTokenSource = null) => await task.TimeoutWithoutException(timeout, delayType, GDTaskScheduler.GetPlayerLoop(timeoutCheckTiming), taskCancellationTokenSource);

        /// <inheritdoc cref="TimeoutWithoutException(GDTask, TimeSpan, DelayType, IPlayerLoop, CancellationTokenSource)" />
        public async GDTask<(bool IsTimeout, T Result)> TimeoutWithoutException(TimeSpan timeout, DelayType delayType, IPlayerLoop timeoutCheckLoop, CancellationTokenSource taskCancellationTokenSource = null)
        {
            Error.ThrowArgumentNullException(timeoutCheckLoop, nameof(timeoutCheckLoop));
            var observedTask = task.Preserve();
            var delayCancellationTokenSource = new CancellationTokenSource();
            var timeoutTask = GDTask.Delay(timeout, delayType, timeoutCheckLoop, delayCancellationTokenSource.Token).SuppressCancellationThrow();

            bool hasTaskResult;

            try { (hasTaskResult, _) = await GDTask.WhenAny(observedTask, (GDTask)timeoutTask); }
            catch
            {
                delayCancellationTokenSource.Cancel();
                delayCancellationTokenSource.Dispose();
                throw;
            }

            // timeout
            if (!hasTaskResult)
            {
                if (taskCancellationTokenSource != null)
                {
                    taskCancellationTokenSource.Cancel();
                    taskCancellationTokenSource.Dispose();
                }

                return (true, default);
            }

            delayCancellationTokenSource.Cancel();
            delayCancellationTokenSource.Dispose();

            try
            {
                return (false, await observedTask);
            }
            catch (OperationCanceledException)
            {
                return (true, default);
            }
        }

        /// <inheritdoc cref="Forget(GDTask)" />
        public void Forget()
        {
            var awaiter = task.GetAwaiter();

            if (awaiter.IsCompleted)
                try { awaiter.GetResult(); }
                catch (Exception ex) { GDTaskExceptionHandler.PublishUnobservedTaskException(ex); }
            else
                awaiter.SourceOnCompleted(
                    state =>
                    {
                        using var t = (StateTuple<GDTask<T>.Awaiter>)state;

                        try { t.Item1.GetResult(); }
                        catch (Exception ex) { GDTaskExceptionHandler.PublishUnobservedTaskException(ex); }
                    },
                    StateTuple.Create(awaiter)
                );
        }

        /// <inheritdoc cref="Forget(GDTask)" />
        public void Forget(Action<Exception> exceptionHandler, bool handleExceptionOnMainThread = true)
        {
            if (exceptionHandler == null) task.Forget();
            else ForgetCoreWithCatch((GDTask)task, exceptionHandler, handleExceptionOnMainThread).Forget();
        }

        /// <summary>
        /// Creates a continuation that executes when the target <see cref="GDTask" /> completes.
        /// </summary>
        public async GDTask ContinueWith(Action<T> continuationFunction) => continuationFunction(await task);

        /// <inheritdoc cref="ContinueWith{T}(GDTask{T}, Action{T})" />
        public async GDTask ContinueWith(Func<T, GDTask> continuationFunction) => await continuationFunction(await task);

        /// <inheritdoc cref="ContinueWith{T}(GDTask{T}, Action{T})" />
        public async GDTask<TReturn> ContinueWith<TReturn>(Func<T, TReturn> continuationFunction) => continuationFunction(await task);

        /// <inheritdoc cref="ContinueWith{T}(GDTask{T}, Action{T})" />
        public async GDTask<TReturn> ContinueWith<TReturn>(Func<T, GDTask<TReturn>> continuationFunction) => await continuationFunction(await task);

        /// <inheritdoc cref="ContinueWith{T}(GDTask{T}, Action{T})" />
        public async GDTask ContinueWith(Action continuationFunction)
        {
            await task;
            continuationFunction();
        }

        /// <inheritdoc cref="ContinueWith{T}(GDTask{T}, Action{T})" />
        public async GDTask ContinueWith(Func<GDTask> continuationFunction)
        {
            await task;
            await continuationFunction();
        }

        /// <inheritdoc cref="ContinueWith{T}(GDTask{T}, Action{T})" />
        public async GDTask<TR> ContinueWith<TR>(Func<TR> continuationFunction)
        {
            await task;
            return continuationFunction();
        }

        /// <inheritdoc cref="ContinueWith{T}(GDTask{T}, Action{T})" />
        public async GDTask<TR> ContinueWith<TR>(Func<GDTask<TR>> continuationFunction)
        {
            await task;
            return await continuationFunction();
        }
    }

    /// <param name="task">The <see cref="GDTask" /> to associate the time out to</param>
    extension(GDTask task)
    {
        /// <inheritdoc cref="AsTask{T}" />
        public Task AsTask()
        {
            try
            {
                GDTask.Awaiter awaiter;

                try { awaiter = task.GetAwaiter(); }
                catch (Exception ex) { return Task.FromException(ex); }

                if (awaiter.IsCompleted)
                    try
                    {
                        awaiter.GetResult(); // check token valid on Succeeded
                        return Task.CompletedTask;
                    }
                    catch (Exception ex) { return Task.FromException(ex); }

                var tcs = new TaskCompletionSource<object>();

                awaiter.SourceOnCompleted(
                    state =>
                    {
                        using var tuple = (StateTuple<TaskCompletionSource<object>, GDTask.Awaiter>)state;
                        var (inTcs, inAwaiter) = tuple;

                        try
                        {
                            inAwaiter.GetResult();
                            inTcs.SetResult(null);
                        }
                        catch (Exception ex) { inTcs.SetException(ex); }
                    },
                    StateTuple.Create(tcs, awaiter)
                );

                return tcs.Task;
            }
            catch (Exception ex) { return Task.FromException(ex); }
        }

        /// <summary>
        /// Create a <see cref="AsyncLazy" /> that wraps around this task.
        /// </summary>
        public IAsyncLazy ToAsyncLazy() => new AsyncLazy(task);

        /// <summary>
        /// Attach a <see cref="CancellationToken" /> to the given task, result is ignored when cancel is raised first.
        /// </summary>
        public GDTask AttachExternalCancellation(CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled) return task;

            if (cancellationToken.IsCancellationRequested) return GDTask.FromCanceled(cancellationToken);

            if (task.Status.IsCompleted()) return task;

            return new(new AttachExternalCancellationSource(task, cancellationToken), 0);
        }

        /// <summary>
        /// Associate a time out to the current <see cref="GDTask" />
        /// </summary>
        /// <param name="timeout">The time out associate to the <see cref="GDTask" /></param>
        /// <param name="delayType">Timing provide used for calculating time out</param>
        /// <param name="timeoutCheckTiming">Update method used for checking time out</param>
        /// <param name="taskCancellationTokenSource">
        /// A <see cref="CancellationTokenSource" /> that get canceled when the task is
        /// completed by time out
        /// </param>
        /// <exception cref="TimeoutException">Thrown when the time allotted for this task has expired.</exception>
        public async GDTask Timeout(TimeSpan timeout, DelayType delayType = DelayType.DeltaTime, PlayerLoopTiming timeoutCheckTiming = PlayerLoopTiming.Process, CancellationTokenSource taskCancellationTokenSource = null) => await task.Timeout(timeout, delayType, GDTaskScheduler.GetPlayerLoop(timeoutCheckTiming), taskCancellationTokenSource);

        /// <summary>
        /// Associate a time out to the current <see cref="GDTask" />
        /// </summary>
        /// <param name="timeout">The time out associate to the <see cref="GDTask" /></param>
        /// <param name="delayType">Timing provide used for calculating time out</param>
        /// <param name="timeoutCheckLoop">Update loop used for checking time out</param>
        /// <param name="taskCancellationTokenSource">
        /// A <see cref="CancellationTokenSource" /> that get canceled when the task is
        /// completed by time out
        /// </param>
        /// <exception cref="TimeoutException">Thrown when the time allotted for this task has expired.</exception>
        public async GDTask Timeout(TimeSpan timeout, DelayType delayType, IPlayerLoop timeoutCheckLoop, CancellationTokenSource taskCancellationTokenSource = null)
        {
            Error.ThrowArgumentNullException(timeoutCheckLoop, nameof(timeoutCheckLoop));
            var delayCancellationTokenSource = new CancellationTokenSource();
            var timeoutTask = GDTask.Delay(timeout, delayType, timeoutCheckLoop, delayCancellationTokenSource.Token).SuppressCancellationThrow();

            int winArgIndex;
            bool taskResultIsCanceled;

            try { (winArgIndex, taskResultIsCanceled, _) = await GDTask.WhenAny(task.SuppressCancellationThrow(), timeoutTask); }
            catch
            {
                delayCancellationTokenSource.Cancel();
                delayCancellationTokenSource.Dispose();
                throw;
            }

            // timeout
            if (winArgIndex == 1)
            {
                if (taskCancellationTokenSource != null)
                {
                    taskCancellationTokenSource.Cancel();
                    taskCancellationTokenSource.Dispose();
                }

                throw new TimeoutException("Exceed Timeout:" + timeout);
            }

            delayCancellationTokenSource.Cancel();
            delayCancellationTokenSource.Dispose();

            if (taskResultIsCanceled) Error.ThrowOperationCanceledException();
        }

        /// <summary>
        /// Associate a time out to the current <see cref="GDTask" />, this overload does not raise <see cref="TimeoutException" />
        /// instead asynchronously returns a <see cref="bool" /> indicating if the operation has timed out.
        /// </summary>
        /// <param name="timeout">The time out associate to the <see cref="GDTask" /></param>
        /// <param name="delayType">Timing provide used for calculating time out</param>
        /// <param name="timeoutCheckTiming">Update method used for checking time out</param>
        /// <param name="taskCancellationTokenSource">
        /// A <see cref="CancellationTokenSource" /> that get canceled when the task is
        /// completed by time out
        /// </param>
        public async GDTask<bool> TimeoutWithoutException(TimeSpan timeout, DelayType delayType = DelayType.DeltaTime, PlayerLoopTiming timeoutCheckTiming = PlayerLoopTiming.Process, CancellationTokenSource taskCancellationTokenSource = null) => await task.TimeoutWithoutException(timeout, delayType, GDTaskScheduler.GetPlayerLoop(timeoutCheckTiming), taskCancellationTokenSource);

        /// <summary>
        /// Associate a time out to the current <see cref="GDTask" />, this overload does not raise <see cref="TimeoutException" />
        /// instead asynchronously returns a <see cref="bool" /> indicating if the operation has timed out.
        /// </summary>
        /// <param name="timeout">The time out associate to the <see cref="GDTask" /></param>
        /// <param name="delayType">Timing provide used for calculating time out</param>
        /// <param name="timeoutCheckLoop">Update loop used for checking time out</param>
        /// <param name="taskCancellationTokenSource">
        /// A <see cref="CancellationTokenSource" /> that get canceled when the task is
        /// completed by time out
        /// </param>
        public async GDTask<bool> TimeoutWithoutException(TimeSpan timeout, DelayType delayType, IPlayerLoop timeoutCheckLoop, CancellationTokenSource taskCancellationTokenSource = null)
        {
            Error.ThrowArgumentNullException(timeoutCheckLoop, nameof(timeoutCheckLoop));
            var observedTask = task.Preserve();
            var delayCancellationTokenSource = new CancellationTokenSource();
            var timeoutTask = GDTask.Delay(timeout, delayType, timeoutCheckLoop, delayCancellationTokenSource.Token).SuppressCancellationThrow();

            int winArgIndex;

            try { winArgIndex = await GDTask.WhenAny(observedTask, timeoutTask); }
            catch
            {
                delayCancellationTokenSource.Cancel();
                delayCancellationTokenSource.Dispose();
                throw;
            }

            // timeout
            if (winArgIndex == 1)
            {
                if (taskCancellationTokenSource != null)
                {
                    taskCancellationTokenSource.Cancel();
                    taskCancellationTokenSource.Dispose();
                }

                return true;
            }

            delayCancellationTokenSource.Cancel();
            delayCancellationTokenSource.Dispose();

            try
            {
                await observedTask;
                return false;
            }
            catch (OperationCanceledException)
            {
                return true;
            }
        }

        /// <summary>
        /// Run this task without asynchronously waiting for it to finish.
        /// </summary>
        public void Forget()
        {
            var awaiter = task.GetAwaiter();

            if (awaiter.IsCompleted)
                try { awaiter.GetResult(); }
                catch (Exception ex) { GDTaskExceptionHandler.PublishUnobservedTaskException(ex); }
            else
                awaiter.SourceOnCompleted(
                    state =>
                    {
                        using var t = (StateTuple<GDTask.Awaiter>)state;

                        try { t.Item1.GetResult(); }
                        catch (Exception ex) { GDTaskExceptionHandler.PublishUnobservedTaskException(ex); }
                    },
                    StateTuple.Create(awaiter)
                );
        }

        /// <inheritdoc cref="Forget(GDTask)" />
        public void Forget(Action<Exception> exceptionHandler, bool handleExceptionOnMainThread = true)
        {
            if (exceptionHandler == null) task.Forget();
            else ForgetCoreWithCatch(task, exceptionHandler, handleExceptionOnMainThread).Forget();
        }

        /// <inheritdoc cref="ContinueWith{T}(GDTask{T}, Action{T})" />
        public async GDTask ContinueWith(Action continuationFunction)
        {
            await task;
            continuationFunction();
        }

        /// <inheritdoc cref="ContinueWith{T}(GDTask{T}, Action{T})" />
        public async GDTask ContinueWith(Func<GDTask> continuationFunction)
        {
            await task;
            await continuationFunction();
        }

        /// <inheritdoc cref="ContinueWith{T}(GDTask{T}, Action{T})" />
        public async GDTask<T> ContinueWith<T>(Func<T> continuationFunction)
        {
            await task;
            return continuationFunction();
        }

        /// <inheritdoc cref="ContinueWith{T}(GDTask{T}, Action{T})" />
        public async GDTask<T> ContinueWith<T>(Func<GDTask<T>> continuationFunction)
        {
            await task;
            return await continuationFunction();
        }
    }

    private static async GDTaskVoid ForgetCoreWithCatch(GDTask task, Action<Exception> exceptionHandler, bool handleExceptionOnMainThread)
    {
        try { await task; }
        catch (Exception ex)
        {
            try
            {
                if (handleExceptionOnMainThread) await GDTask.SwitchToMainThread();
                exceptionHandler(ex);
            }
            catch (Exception ex2) { GDTaskExceptionHandler.PublishUnobservedTaskException(ex2); }
        }
    }

    private static async GDTaskVoid ForgetCoreWithCatch<T>(GDTask<T> task, Action<Exception> exceptionHandler, bool handleExceptionOnMainThread)
    {
        try { await task; }
        catch (Exception ex)
        {
            try
            {
                if (handleExceptionOnMainThread) await GDTask.SwitchToMainThread();
                exceptionHandler(ex);
            }
            catch (Exception ex2) { GDTaskExceptionHandler.PublishUnobservedTaskException(ex2); }
        }
    }

    /// <summary>
    /// Creates a proxy <see cref="GDTask" /> that represents the asynchronous operation of a wrapped <see cref="GDTask" />.
    /// </summary>
    public static async GDTask<T> Unwrap<T>(this GDTask<GDTask<T>> task) => await await task;

    /// <inheritdoc cref="Unwrap{T}(GDTask{GDTask{T}})" />
    public static async GDTask Unwrap(this GDTask<GDTask> task) => await await task;

    /// <inheritdoc cref="Unwrap{T}(GDTask{GDTask{T}})" />
    public static async GDTask<T> Unwrap<T>(this Task<GDTask<T>> task) => await await task;

    /// <inheritdoc cref="Unwrap{T}(GDTask{GDTask{T}})" />
    public static async GDTask<T> Unwrap<T>(this Task<GDTask<T>> task, bool continueOnCapturedContext) => await await task.ConfigureAwait(continueOnCapturedContext);

    /// <inheritdoc cref="Unwrap{T}(GDTask{GDTask{T}})" />
    public static async GDTask Unwrap(this Task<GDTask> task) => await await task;

    /// <inheritdoc cref="Unwrap{T}(GDTask{GDTask{T}})" />
    public static async GDTask Unwrap(this Task<GDTask> task, bool continueOnCapturedContext) => await await task.ConfigureAwait(continueOnCapturedContext);

    /// <inheritdoc cref="Unwrap{T}(GDTask{GDTask{T}})" />
    public static async GDTask<T> Unwrap<T>(this GDTask<Task<T>> task) => await await task;

    /// <inheritdoc cref="Unwrap{T}(GDTask{GDTask{T}})" />
    public static async GDTask<T> Unwrap<T>(this GDTask<Task<T>> task, bool continueOnCapturedContext) => await (await task).ConfigureAwait(continueOnCapturedContext);

    /// <inheritdoc cref="Unwrap{T}(GDTask{GDTask{T}})" />
    public static async GDTask Unwrap(this GDTask<Task> task) => await await task;

    /// <inheritdoc cref="Unwrap{T}(GDTask{GDTask{T}})" />
    public static async GDTask Unwrap(this GDTask<Task> task, bool continueOnCapturedContext) => await (await task).ConfigureAwait(continueOnCapturedContext);

    private sealed class AttachExternalCancellationSource : IGDTaskSource
    {
        private static readonly Action<object> CancellationCallbackDelegate = CancellationCallback;

        private readonly CancellationToken _cancellationToken;
        private readonly CancellationTokenRegistration _tokenRegistration;
        private GDTaskCompletionSourceCore<AsyncUnit> _core;

        public AttachExternalCancellationSource(GDTask task, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _tokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(CancellationCallbackDelegate, this);
            RunTask(task).Forget();
        }

        public void GetResult(short token) => _core.GetResult(token);

        public GDTaskStatus GetStatus(short token) => _core.GetStatus(token);

        public void OnCompleted(Action<object> continuation, object state, short token) => _core.OnCompleted(continuation, state, token);

        public GDTaskStatus UnsafeGetStatus() => _core.UnsafeGetStatus();

        private async GDTaskVoid RunTask(GDTask task)
        {
            try
            {
                await task;
                _core.TrySetResult(AsyncUnit.Default);
            }
            catch (Exception ex) { _core.TrySetException(ex); }
            finally { _tokenRegistration.Dispose(); }
        }

        private static void CancellationCallback(object state)
        {
            var self = (AttachExternalCancellationSource)state;
            self._core.TrySetCanceled(self._cancellationToken);
        }
    }

    private sealed class AttachExternalCancellationSource<T> : IGDTaskSource<T>
    {
        private readonly CancellationToken _cancellationToken;
        private readonly CancellationTokenRegistration _tokenRegistration;
        private GDTaskCompletionSourceCore<T> _core;

        public AttachExternalCancellationSource(GDTask<T> task, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _tokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(CancellationCallback, this);
            RunTask(task).Forget();
        }

        void IGDTaskSource.GetResult(short token) => _core.GetResult(token);

        public T GetResult(short token) => _core.GetResult(token);

        public GDTaskStatus GetStatus(short token) => _core.GetStatus(token);

        public void OnCompleted(Action<object> continuation, object state, short token) => _core.OnCompleted(continuation, state, token);

        public GDTaskStatus UnsafeGetStatus() => _core.UnsafeGetStatus();

        private async GDTaskVoid RunTask(GDTask<T> task)
        {
            try { _core.TrySetResult(await task); }
            catch (Exception ex) { _core.TrySetException(ex); }
            finally { _tokenRegistration.Dispose(); }
        }

        private static void CancellationCallback(object state)
        {
            var self = (AttachExternalCancellationSource<T>)state;
            self._core.TrySetCanceled(self._cancellationToken);
        }
    }
}
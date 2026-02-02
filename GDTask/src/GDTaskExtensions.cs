using System;
using System.Threading;
using System.Threading.Tasks;
using GodotTask.Internal;

namespace GodotTask
{
    /// <summary>
    /// Provides extensions methods for <see cref="GDTask"/>.
    /// </summary>
    public static partial class GDTaskExtensions
    {
        /// <summary>
        /// Create a <see cref="GDTask"/> that wraps around this task.
        /// </summary>
        public static GDTask<T> AsGDTask<T>(this Task<T> task, bool useCurrentSynchronizationContext = true)
        {
            var promise = new GDTaskCompletionSource<T>();

            task.ContinueWith((x, state) =>
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
            }, promise, useCurrentSynchronizationContext ? TaskScheduler.FromCurrentSynchronizationContext() : TaskScheduler.Current);

            return promise.Task;
        }

        /// <inheritdoc cref="AsGDTask{T}(Task{T},bool)"/>
        public static GDTask AsGDTask(this Task task, bool useCurrentSynchronizationContext = true)
        {
            var promise = new GDTaskCompletionSource();

            task.ContinueWith((x, state) =>
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
            }, promise, useCurrentSynchronizationContext ? TaskScheduler.FromCurrentSynchronizationContext() : TaskScheduler.Current);

            return promise.Task;
        }

        /// <summary>
        /// Create a <see cref="Task"/> that wraps around this task.
        /// </summary>
        public static Task<T> AsTask<T>(this GDTask<T> task)
        {
            try
            {
                GDTask<T>.Awaiter awaiter;
                try
                {
                    awaiter = task.GetAwaiter();
                }
                catch (Exception ex)
                {
                    return Task.FromException<T>(ex);
                }

                if (awaiter.IsCompleted)
                {
                    try
                    {
                        var result = awaiter.GetResult();
                        return Task.FromResult(result);
                    }
                    catch (Exception ex)
                    {
                        return Task.FromException<T>(ex);
                    }
                }

                var tcs = new TaskCompletionSource<T>();

                awaiter.SourceOnCompleted(state =>
                {
                    using var tuple = (StateTuple<TaskCompletionSource<T>, GDTask<T>.Awaiter>)state;
                    var (inTcs, inAwaiter) = tuple;
                    try
                    {
                        var result = inAwaiter.GetResult();
                        inTcs.SetResult(result);
                    }
                    catch (Exception ex)
                    {
                        inTcs.SetException(ex);
                    }
                }, StateTuple.Create(tcs, awaiter));

                return tcs.Task;
            }
            catch (Exception ex)
            {
                return Task.FromException<T>(ex);
            }
        }

        /// <inheritdoc cref="AsTask{T}"/>
        public static Task AsTask(this GDTask task)
        {
            try
            {
                GDTask.Awaiter awaiter;
                try
                {
                    awaiter = task.GetAwaiter();
                }
                catch (Exception ex)
                {
                    return Task.FromException(ex);
                }

                if (awaiter.IsCompleted)
                {
                    try
                    {
                        awaiter.GetResult(); // check token valid on Succeeded
                        return Task.CompletedTask;
                    }
                    catch (Exception ex)
                    {
                        return Task.FromException(ex);
                    }
                }

                var tcs = new TaskCompletionSource<object>();

                awaiter.SourceOnCompleted(state =>
                {
                    using var tuple = (StateTuple<TaskCompletionSource<object>, GDTask.Awaiter>)state;
                    var (inTcs, inAwaiter) = tuple;
                    try
                    {
                        inAwaiter.GetResult();
                        inTcs.SetResult(null);
                    }
                    catch (Exception ex)
                    {
                        inTcs.SetException(ex);
                    }
                }, StateTuple.Create(tcs, awaiter));

                return tcs.Task;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }

        /// <summary>
        /// Create a <see cref="AsyncLazy"/> that wraps around this task.
        /// </summary>
        public static IAsyncLazy ToAsyncLazy(this GDTask task)
        {
            return new AsyncLazy(task);
        }

        /// <inheritdoc cref="ToAsyncLazy"/>
        public static IAsyncLazy<T> ToAsyncLazy<T>(this GDTask<T> task)
        {
            return new AsyncLazy<T>(task);
        }

        /// <summary>
        /// Attach a <see cref="CancellationToken"/> to the given task, result is ignored when cancel is raised first.
        /// </summary>
        public static GDTask AttachExternalCancellation(this GDTask task, CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
            {
                return task;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return GDTask.FromCanceled(cancellationToken);
            }

            if (task.Status.IsCompleted())
            {
                return task;
            }

            return new GDTask(AttachExternalCancellationSource.Create(task, cancellationToken, out var token), token);
        }

        /// <inheritdoc cref="AttachExternalCancellation"/>
        public static GDTask<T> AttachExternalCancellation<T>(this GDTask<T> task, CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
            {
                return task;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return GDTask.FromCanceled<T>(cancellationToken);
            }

            if (task.Status.IsCompleted())
            {
                return task;
            }

            return new GDTask<T>(AttachExternalCancellationSource<T>.Create(task, cancellationToken, out var token), token);
        }

        private sealed class AttachExternalCancellationSource : IGDTaskSource, ITaskPoolNode<AttachExternalCancellationSource>
        {
            private static TaskPool<AttachExternalCancellationSource> pool;
            private AttachExternalCancellationSource nextNode;
            public ref AttachExternalCancellationSource NextNode => ref nextNode;

            private CancellationToken cancellationToken;
            private CancellationTokenRegistration tokenRegistration;
            private GDTaskCompletionSourceCore<AsyncUnit> core;

            static AttachExternalCancellationSource()
            {
                TaskPool.RegisterSizeGetter(typeof(AttachExternalCancellationSource), () => pool.Size);
            }

            private AttachExternalCancellationSource()
            {
            }

            public static IGDTaskSource Create(GDTask task, CancellationToken cancellationToken, out short token)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!pool.TryPop(out var result))
                {
                    result = new AttachExternalCancellationSource();
                }

                result.cancellationToken = cancellationToken;
                result.tokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(CancellationCallback, result);

                TaskTracker.TrackActiveTask(result, 3);

                result.RunTask(task).Forget();

                token = result.core.Version;
                return result;
            }

            private async GDTaskVoid RunTask(GDTask task)
            {
                try
                {
                    await task;
                    core.TrySetResult(AsyncUnit.Default);
                }
                catch (Exception ex)
                {
                    core.TrySetException(ex);
                }
                finally
                {
                    tokenRegistration.Dispose();
                }
            }

            private static void CancellationCallback(object state)
            {
                var self = (AttachExternalCancellationSource)state;
                self.core.TrySetCanceled(self.cancellationToken);
            }

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

            public GDTaskStatus GetStatus(short token)
            {
                return core.GetStatus(token);
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                core.OnCompleted(continuation, state, token);
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                return core.UnsafeGetStatus();
            }

            private bool TryReturn()
            {
                TaskTracker.RemoveTracking(this);
                core.Reset();
                cancellationToken = default;
                return pool.TryPush(this);
            }
        }

        private sealed class AttachExternalCancellationSource<T> : IGDTaskSource<T>, ITaskPoolNode<AttachExternalCancellationSource<T>>
        {
            private static TaskPool<AttachExternalCancellationSource<T>> pool;
            private AttachExternalCancellationSource<T> nextNode;
            public ref AttachExternalCancellationSource<T> NextNode => ref nextNode;

            private CancellationToken cancellationToken;
            private CancellationTokenRegistration tokenRegistration;
            private GDTaskCompletionSourceCore<T> core;
            
            static AttachExternalCancellationSource()
            {
                TaskPool.RegisterSizeGetter(typeof(AttachExternalCancellationSource<T>), () => pool.Size);
            }

            private AttachExternalCancellationSource()
            {
            }

            public static IGDTaskSource<T> Create(GDTask<T> task, CancellationToken cancellationToken, out short token)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return AutoResetGDTaskCompletionSource<T>.CreateFromCanceled(cancellationToken, out token);
                }

                if (!pool.TryPop(out var result))
                {
                    result = new AttachExternalCancellationSource<T>();
                }

                result.cancellationToken = cancellationToken;
                result.tokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(CancellationCallback, result);

                TaskTracker.TrackActiveTask(result, 3);

                result.RunTask(task).Forget();

                token = result.core.Version;
                return result;
            }

            private async GDTaskVoid RunTask(GDTask<T> task)
            {
                try
                {
                    core.TrySetResult(await task);
                }
                catch (Exception ex)
                {
                    core.TrySetException(ex);
                }
                finally
                {
                    tokenRegistration.Dispose();
                }
            }

            private static void CancellationCallback(object state)
            {
                var self = (AttachExternalCancellationSource<T>)state;
                self.core.TrySetCanceled(self.cancellationToken);
            }

            void IGDTaskSource.GetResult(short token)
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

            public GDTaskStatus GetStatus(short token)
            {
                return core.GetStatus(token);
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                core.OnCompleted(continuation, state, token);
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                return core.UnsafeGetStatus();
            }

            private bool TryReturn()
            {
                TaskTracker.RemoveTracking(this);
                core.Reset();
                cancellationToken = default;
                return pool.TryPush(this);
            }
        }

        /// <summary>
        /// Associate a time out to the current <see cref="GDTask"/>
        /// </summary>
        /// <param name="task">The <see cref="GDTask"/> to associate the time out to</param>
        /// <param name="timeout">The time out associate to the <see cref="GDTask"/></param>
        /// <param name="delayType">Timing provide used for calculating time out</param>
        /// <param name="timeoutCheckTiming">Update method used for checking time out</param>
        /// <param name="taskCancellationTokenSource">A <see cref="CancellationTokenSource"/> that get canceled when the task is completed by time out</param>
        /// <exception cref="TimeoutException">Thrown when the time allotted for this task has expired.</exception>
        public static async GDTask Timeout(this GDTask task, TimeSpan timeout, DelayType delayType = DelayType.DeltaTime, PlayerLoopTiming timeoutCheckTiming = PlayerLoopTiming.Process, CancellationTokenSource taskCancellationTokenSource = null)
        {
            var delayCancellationTokenSource = new CancellationTokenSource();
            var timeoutTask = GDTask.Delay(timeout, delayType, timeoutCheckTiming, delayCancellationTokenSource.Token).SuppressCancellationThrow();

            int winArgIndex;
            bool taskResultIsCanceled;
            try
            {
                (winArgIndex, taskResultIsCanceled, _) = await GDTask.WhenAny(task.SuppressCancellationThrow(), timeoutTask);
            }
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
            else
            {
                delayCancellationTokenSource.Cancel();
                delayCancellationTokenSource.Dispose();
            }

            if (taskResultIsCanceled)
            {
                Error.ThrowOperationCanceledException();
            }
        }

        /// <inheritdoc cref="Timeout"/>
        public static async GDTask<T> Timeout<T>(this GDTask<T> task, TimeSpan timeout, DelayType delayType = DelayType.DeltaTime, PlayerLoopTiming timeoutCheckTiming = PlayerLoopTiming.Process, CancellationTokenSource taskCancellationTokenSource = null)
        {
            var delayCancellationTokenSource = new CancellationTokenSource();
            var timeoutTask = GDTask.Delay(timeout, delayType, timeoutCheckTiming, delayCancellationTokenSource.Token).SuppressCancellationThrow();

            int winArgIndex;
            (bool IsCanceled, T Result) taskResult;
            try
            {
                (winArgIndex, taskResult, _) = await GDTask.WhenAny(task.SuppressCancellationThrow(), timeoutTask);
            }
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
            else
            {
                delayCancellationTokenSource.Cancel();
                delayCancellationTokenSource.Dispose();
            }

            if (taskResult.IsCanceled)
            {
                Error.ThrowOperationCanceledException();
            }

            return taskResult.Result;
        }

        /// <summary>
        /// Associate a time out to the current <see cref="GDTask"/>, this overload does not raise <see cref="TimeoutException"/> instead asynchronously returns a <see cref="bool"/> indicating if the operation has timed out.
        /// </summary>
        /// <param name="task">The <see cref="GDTask"/> to associate the time out to</param>
        /// <param name="timeout">The time out associate to the <see cref="GDTask"/></param>
        /// <param name="delayType">Timing provide used for calculating time out</param>
        /// <param name="timeoutCheckTiming">Update method used for checking time out</param>
        /// <param name="taskCancellationTokenSource">A <see cref="CancellationTokenSource"/> that get canceled when the task is completed by time out</param>
        public static async GDTask<bool> TimeoutWithoutException(this GDTask task, TimeSpan timeout, DelayType delayType = DelayType.DeltaTime, PlayerLoopTiming timeoutCheckTiming = PlayerLoopTiming.Process, CancellationTokenSource taskCancellationTokenSource = null)
        {
            var delayCancellationTokenSource = new CancellationTokenSource();
            var timeoutTask = GDTask.Delay(timeout, delayType, timeoutCheckTiming, delayCancellationTokenSource.Token).SuppressCancellationThrow();

            int winArgIndex;
            bool taskResultIsCanceled;
            try
            {
                (winArgIndex, taskResultIsCanceled, _) = await GDTask.WhenAny(task.SuppressCancellationThrow(), timeoutTask);
            }
            catch
            {
                delayCancellationTokenSource.Cancel();
                delayCancellationTokenSource.Dispose();
                return true;
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
            else
            {
                delayCancellationTokenSource.Cancel();
                delayCancellationTokenSource.Dispose();
            }

            if (taskResultIsCanceled)
            {
                return true;
            }

            return false;
        }

        /// <inheritdoc cref="TimeoutWithoutException"/>
        public static async GDTask<(bool IsTimeout, T Result)> TimeoutWithoutException<T>(this GDTask<T> task, TimeSpan timeout, DelayType delayType = DelayType.DeltaTime, PlayerLoopTiming timeoutCheckTiming = PlayerLoopTiming.Process, CancellationTokenSource taskCancellationTokenSource = null)
        {
            var delayCancellationTokenSource = new CancellationTokenSource();
            var timeoutTask = GDTask.Delay(timeout, delayType, timeoutCheckTiming, delayCancellationTokenSource.Token).SuppressCancellationThrow();

            int winArgIndex;
            (bool IsCanceled, T Result) taskResult;
            try
            {
                (winArgIndex, taskResult, _) = await GDTask.WhenAny(task.SuppressCancellationThrow(), timeoutTask);
            }
            catch
            {
                delayCancellationTokenSource.Cancel();
                delayCancellationTokenSource.Dispose();
                return (true, default);
            }

            // timeout
            if (winArgIndex == 1)
            {
                if (taskCancellationTokenSource != null)
                {
                    taskCancellationTokenSource.Cancel();
                    taskCancellationTokenSource.Dispose();
                }

                return (true, default);
            }
            else
            {
                delayCancellationTokenSource.Cancel();
                delayCancellationTokenSource.Dispose();
            }

            if (taskResult.IsCanceled)
            {
                return (true, default);
            }

            return (false, taskResult.Result);
        }

        /// <summary>
        /// Run this task without asynchronously waiting for it to finish.
        /// </summary>
        public static void Forget(this GDTask task)
        {
            var awaiter = task.GetAwaiter();
            if (awaiter.IsCompleted)
            {
                try
                {
                    awaiter.GetResult();
                }
                catch (Exception ex)
                {
                    GDTaskExceptionHandler.PublishUnobservedTaskException(ex);
                }
            }
            else
            {
                awaiter.SourceOnCompleted(state =>
                {
                    using var t = (StateTuple<GDTask.Awaiter>)state;
                    try
                    {
                        t.Item1.GetResult();
                    }
                    catch (Exception ex)
                    {
                        GDTaskExceptionHandler.PublishUnobservedTaskException(ex);
                    }
                }, StateTuple.Create(awaiter));
            }
        }

        /// <inheritdoc cref="Forget(GDTask)"/>
        public static void Forget(this GDTask task, Action<Exception> exceptionHandler, bool handleExceptionOnMainThread = true)
        {
            if (exceptionHandler == null)
            {
                Forget(task);
            }
            else
            {
                ForgetCoreWithCatch(task, exceptionHandler, handleExceptionOnMainThread).Forget();
            }
        }

        private static async GDTaskVoid ForgetCoreWithCatch(GDTask task, Action<Exception> exceptionHandler, bool handleExceptionOnMainThread)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                try
                {
                    if (handleExceptionOnMainThread)
                    {
                        await GDTask.SwitchToMainThread();
                    }
                    exceptionHandler(ex);
                }
                catch (Exception ex2)
                {
                    GDTaskExceptionHandler.PublishUnobservedTaskException(ex2);
                }
            }
        }

        /// <inheritdoc cref="Forget(GDTask)"/>
        public static void Forget<T>(this GDTask<T> task)
        {
            var awaiter = task.GetAwaiter();
            if (awaiter.IsCompleted)
            {
                try
                {
                    awaiter.GetResult();
                }
                catch (Exception ex)
                {
                    GDTaskExceptionHandler.PublishUnobservedTaskException(ex);
                }
            }
            else
            {
                awaiter.SourceOnCompleted(state =>
                {
                    using var t = (StateTuple<GDTask<T>.Awaiter>)state;
                    try
                    {
                        t.Item1.GetResult();
                    }
                    catch (Exception ex)
                    {
                        GDTaskExceptionHandler.PublishUnobservedTaskException(ex);
                    }
                }, StateTuple.Create(awaiter));
            }
        }

        /// <inheritdoc cref="Forget(GDTask)"/>
        public static void Forget<T>(this GDTask<T> task, Action<Exception> exceptionHandler, bool handleExceptionOnMainThread = true)
        {
            if (exceptionHandler == null)
            {
                task.Forget();
            }
            else
            {
                ForgetCoreWithCatch(task, exceptionHandler, handleExceptionOnMainThread).Forget();
            }
        }

        private static async GDTaskVoid ForgetCoreWithCatch<T>(GDTask<T> task, Action<Exception> exceptionHandler, bool handleExceptionOnMainThread)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                try
                {
                    if (handleExceptionOnMainThread)
                    {
                        await GDTask.SwitchToMainThread();
                    }
                    exceptionHandler(ex);
                }
                catch (Exception ex2)
                {
                    GDTaskExceptionHandler.PublishUnobservedTaskException(ex2);
                }
            }
        }

        /// <summary>
        /// Creates a continuation that executes when the target <see cref="GDTask"/> completes.
        /// </summary>
        public static async GDTask ContinueWith<T>(this GDTask<T> task, Action<T> continuationFunction)
        {
            continuationFunction(await task);
        }

        /// <inheritdoc cref="ContinueWith{T}(GDTask{T},System.Action{T})"/>
        public static async GDTask ContinueWith<T>(this GDTask<T> task, Func<T, GDTask> continuationFunction)
        {
            await continuationFunction(await task);
        }

        /// <inheritdoc cref="ContinueWith{T}(GDTask{T},System.Action{T})"/>
        public static async GDTask<TReturn> ContinueWith<T, TReturn>(this GDTask<T> task, Func<T, TReturn> continuationFunction)
        {
            return continuationFunction(await task);
        }

        /// <inheritdoc cref="ContinueWith{T}(GDTask{T},System.Action{T})"/>
        public static async GDTask<TReturn> ContinueWith<T, TReturn>(this GDTask<T> task, Func<T, GDTask<TReturn>> continuationFunction)
        {
            return await continuationFunction(await task);
        }

        /// <inheritdoc cref="ContinueWith{T}(GDTask{T},System.Action{T})"/>
        public static async GDTask ContinueWith<T>(this GDTask<T> task, Action continuationFunction) {
            await task;
            continuationFunction();
        }

        /// <inheritdoc cref="ContinueWith{T}(GDTask{T},System.Action{T})"/>
        public static async GDTask ContinueWith<T>(this GDTask<T> task, Func<GDTask> continuationFunction) {
            await task;
            await continuationFunction();
        }

        /// <inheritdoc cref="ContinueWith{T}(GDTask{T},System.Action{T})"/>
        public static async GDTask<TR> ContinueWith<T, TR>(this GDTask<T> task, Func<TR> continuationFunction) {
            await task;
            return continuationFunction();
        }

        /// <inheritdoc cref="ContinueWith{T}(GDTask{T},System.Action{T})"/>
        public static async GDTask<TR> ContinueWith<T, TR>(this GDTask<T> task, Func<GDTask<TR>> continuationFunction) {
            await task;
            return await continuationFunction();
        }

        /// <inheritdoc cref="ContinueWith{T}(GDTask{T},System.Action{T})"/>
        public static async GDTask ContinueWith(this GDTask task, Action continuationFunction)
        {
            await task;
            continuationFunction();
        }

        /// <inheritdoc cref="ContinueWith{T}(GDTask{T},System.Action{T})"/>
        public static async GDTask ContinueWith(this GDTask task, Func<GDTask> continuationFunction)
        {
            await task;
            await continuationFunction();
        }

        /// <inheritdoc cref="ContinueWith{T}(GDTask{T},System.Action{T})"/>
        public static async GDTask<T> ContinueWith<T>(this GDTask task, Func<T> continuationFunction)
        {
            await task;
            return continuationFunction();
        }

        /// <inheritdoc cref="ContinueWith{T}(GDTask{T},System.Action{T})"/>
        public static async GDTask<T> ContinueWith<T>(this GDTask task, Func<GDTask<T>> continuationFunction)
        {
            await task;
            return await continuationFunction();
        }

        /// <summary>
        /// Creates a proxy <see cref="GDTask"/> that represents the asynchronous operation of a wrapped <see cref="GDTask"/>.
        /// </summary>
        public static async GDTask<T> Unwrap<T>(this GDTask<GDTask<T>> task)
        {
            return await await task;
        }

        /// <inheritdoc cref="Unwrap{T}(GDTask{GDTask{T}})"/>
        public static async GDTask Unwrap(this GDTask<GDTask> task)
        {
            await await task;
        }

        /// <inheritdoc cref="Unwrap{T}(GDTask{GDTask{T}})"/>
        public static async GDTask<T> Unwrap<T>(this Task<GDTask<T>> task)
        {
            return await await task;
        }

        /// <inheritdoc cref="Unwrap{T}(GDTask{GDTask{T}})"/>
        public static async GDTask<T> Unwrap<T>(this Task<GDTask<T>> task, bool continueOnCapturedContext)
        {
            return await await task.ConfigureAwait(continueOnCapturedContext);
        }

        /// <inheritdoc cref="Unwrap{T}(GDTask{GDTask{T}})"/>
        public static async GDTask Unwrap(this Task<GDTask> task)
        {
            await await task;
        }

        /// <inheritdoc cref="Unwrap{T}(GDTask{GDTask{T}})"/>
        public static async GDTask Unwrap(this Task<GDTask> task, bool continueOnCapturedContext)
        {
            await await task.ConfigureAwait(continueOnCapturedContext);
        }

        /// <inheritdoc cref="Unwrap{T}(GDTask{GDTask{T}})"/>
        public static async GDTask<T> Unwrap<T>(this GDTask<Task<T>> task)
        {
            return await await task;
        }

        /// <inheritdoc cref="Unwrap{T}(GDTask{GDTask{T}})"/>
        public static async GDTask<T> Unwrap<T>(this GDTask<Task<T>> task, bool continueOnCapturedContext)
        {
            return await (await task).ConfigureAwait(continueOnCapturedContext);
        }

        /// <inheritdoc cref="Unwrap{T}(GDTask{GDTask{T}})"/>
        public static async GDTask Unwrap(this GDTask<Task> task)
        {
            await await task;
        }

        /// <inheritdoc cref="Unwrap{T}(GDTask{GDTask{T}})"/>
        public static async GDTask Unwrap(this GDTask<Task> task, bool continueOnCapturedContext)
        {
            await (await task).ConfigureAwait(continueOnCapturedContext);
        }
    }
}


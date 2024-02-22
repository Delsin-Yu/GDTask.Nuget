using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace GodotTask
{
    public partial struct GDTask
    {
        private static readonly GDTask CanceledGDTask = new Func<GDTask>(() => new GDTask(new CanceledResultSource(CancellationToken.None), 0))();

        private static class CanceledGDTaskCache<T>
        {
            public static readonly GDTask<T> Task;

            static CanceledGDTaskCache()
            {
                Task = new GDTask<T>(new CanceledResultSource<T>(CancellationToken.None), 0);
            }
        }

        /// <summary>
        /// Gets a <see cref="GDTask"/> that has already completed successfully.
        /// </summary>
        public static readonly GDTask CompletedTask = new GDTask();

        /// <summary>
        /// Creates a <see cref="GDTask"/> that has completed with the specified exception.
        /// </summary>
        /// <param name="ex">The exception with which to fault the task.</param>
        /// <returns>The faulted task.</returns>
        public static GDTask FromException(Exception ex)
        {
            if (ex is OperationCanceledException oce)
            {
                return FromCanceled(oce.CancellationToken);
            }

            return new GDTask(new ExceptionResultSource(ex), 0);
        }

        /// <inheritdoc cref="FromException"/>
        public static GDTask<T> FromException<T>(Exception ex)
        {
            if (ex is OperationCanceledException oce)
            {
                return FromCanceled<T>(oce.CancellationToken);
            }

            return new GDTask<T>(new ExceptionResultSource<T>(ex), 0);
        }

        /// <summary>
        /// Creates a <see cref="GDTask{TResult}"/> that's completed successfully with the specified result.
        /// </summary>
        /// <typeparam name="T">The type of the result returned by the task.</typeparam>
        /// <param name="value">The result to store into the completed task.</param>
        /// <returns>The successfully completed task.</returns>
        public static GDTask<T> FromResult<T>(T value)
        {
            return new GDTask<T>(value);
        }


        /// <summary>
        /// Creates a <see cref="GDTask"/> that has completed due to cancellation with the specified cancellation token.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token with which to cancel the task.</param>
        /// <returns>The canceled task.</returns>
        public static GDTask FromCanceled(CancellationToken cancellationToken = default)
        {
            if (cancellationToken == CancellationToken.None)
            {
                return CanceledGDTask;
            }
            else
            {
                return new GDTask(new CanceledResultSource(cancellationToken), 0);
            }
        }

        /// <inheritdoc cref="FromCanceled"/>
        public static GDTask<T> FromCanceled<T>(CancellationToken cancellationToken = default)
        {
            if (cancellationToken == CancellationToken.None)
            {
                return CanceledGDTaskCache<T>.Task;
            }
            else
            {
                return new GDTask<T>(new CanceledResultSource<T>(cancellationToken), 0);
            }
        }

        /// <summary>
        /// Create the specified asynchronous work to run and returns a <see cref="GDTask"/> handle for that work.
        /// </summary>
        /// <param name="factory">The work to execute asynchronously</param>
        /// <returns>A <see cref="GDTask"/> that represents the work to execute.</returns>
        public static GDTask Create(Func<GDTask> factory)
        {
            return factory();
        }

        /// <inheritdoc cref="Create"/>
        public static GDTask<T> Create<T>(Func<GDTask<T>> factory)
        {
            return factory();
        }

        /// <summary>
        /// Defers the creation of a specified asynchronous work until it's acquired.
        /// </summary>
        /// <param name="factory">The work to execute asynchronously</param>
        /// <returns>An <see cref="AsyncLazy"/> that represents the work for lazy initialization.</returns>
        public static IAsyncLazy Lazy(Func<GDTask> factory)
        {
            return new AsyncLazy(factory);
        }

        /// <inheritdoc cref="Lazy"/>
        public static IAsyncLazy<T> Lazy<T>(Func<GDTask<T>> factory)
        {
            return new AsyncLazy<T>(factory);
        }

        /// <summary>
        /// Execute a lightweight task that does not have awaitable completion.
        /// </summary>
        public static void Void(Func<GDTaskVoid> asyncAction)
        {
            asyncAction().Forget();
        }

        /// <summary>
        /// Execute a lightweight task that does not have awaitable completion, with specified <see cref="CancellationToken"/>.
        /// </summary>
        public static void Void(Func<CancellationToken, GDTaskVoid> asyncAction, CancellationToken cancellationToken)
        {
            asyncAction(cancellationToken).Forget();
        }

        /// <summary>
        /// Execute a lightweight task that does not have awaitable completion, with specified input value <typeparamref name="T"/>.
        /// </summary>
        public static void Void<T>(Func<T, GDTaskVoid> asyncAction, T state)
        {
            asyncAction(state).Forget();
        }

        /// <summary>
        /// Creates a delegate that execute a lightweight task that does not have awaitable completion.
        /// </summary>
        public static Action Action(Func<GDTaskVoid> asyncAction)
        {
            return () => asyncAction().Forget();
        }

        /// <summary>
        /// Creates a delegate that execute a lightweight task that does not have awaitable completion, with specified <see cref="CancellationToken"/>.
        /// </summary>
        public static Action Action(Func<CancellationToken, GDTaskVoid> asyncAction, CancellationToken cancellationToken)
        {
            return () => asyncAction(cancellationToken).Forget();
        }

        /// <summary>
        /// Defers the creation of a specified asynchronous work when it's awaited.
        /// </summary>
        public static GDTask Defer(Func<GDTask> factory)
        {
            return new GDTask(new DeferPromise(factory), 0);
        }

        /// <inheritdoc cref="Defer"/>
        public static GDTask<T> Defer<T>(Func<GDTask<T>> factory)
        {
            return new GDTask<T>(new DeferPromise<T>(factory), 0);
        }

        /// <summary>
        /// Creates a task that never completes, with specified <see cref="CancellationToken"/>.
        /// </summary>
        public static GDTask Never(CancellationToken cancellationToken)
        {
            return new GDTask<AsyncUnit>(new NeverPromise<AsyncUnit>(cancellationToken), 0);
        }

        /// <inheritdoc cref="Never"/>
        public static GDTask<T> Never<T>(CancellationToken cancellationToken)
        {
            return new GDTask<T>(new NeverPromise<T>(cancellationToken), 0);
        }

        private sealed class ExceptionResultSource : IGDTaskSource
        {
            private readonly ExceptionDispatchInfo exception;
            private bool calledGet;

            public ExceptionResultSource(Exception exception)
            {
                this.exception = ExceptionDispatchInfo.Capture(exception);
            }

            public void GetResult(short token)
            {
                if (!calledGet)
                {
                    calledGet = true;
                    GC.SuppressFinalize(this);
                }
                exception.Throw();
            }

            public GDTaskStatus GetStatus(short token)
            {
                return GDTaskStatus.Faulted;
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                return GDTaskStatus.Faulted;
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                continuation(state);
            }

            ~ExceptionResultSource()
            {
                if (!calledGet)
                {
                    GDTaskExceptionHandler.PublishUnobservedTaskException(exception.SourceException);
                }
            }
        }

        private sealed class ExceptionResultSource<T> : IGDTaskSource<T>
        {
            private readonly ExceptionDispatchInfo exception;
            private bool calledGet;

            public ExceptionResultSource(Exception exception)
            {
                this.exception = ExceptionDispatchInfo.Capture(exception);
            }

            public T GetResult(short token)
            {
                if (!calledGet)
                {
                    calledGet = true;
                    GC.SuppressFinalize(this);
                }
                exception.Throw();
                return default;
            }

            void IGDTaskSource.GetResult(short token)
            {
                if (!calledGet)
                {
                    calledGet = true;
                    GC.SuppressFinalize(this);
                }
                exception.Throw();
            }

            public GDTaskStatus GetStatus(short token)
            {
                return GDTaskStatus.Faulted;
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                return GDTaskStatus.Faulted;
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                continuation(state);
            }

            ~ExceptionResultSource()
            {
                if (!calledGet)
                {
                    GDTaskExceptionHandler.PublishUnobservedTaskException(exception.SourceException);
                }
            }
        }

        private sealed class CanceledResultSource : IGDTaskSource
        {
            private readonly CancellationToken cancellationToken;

            public CanceledResultSource(CancellationToken cancellationToken)
            {
                this.cancellationToken = cancellationToken;
            }

            public void GetResult(short token)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            public GDTaskStatus GetStatus(short token)
            {
                return GDTaskStatus.Canceled;
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                return GDTaskStatus.Canceled;
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                continuation(state);
            }
        }

        private sealed class CanceledResultSource<T> : IGDTaskSource<T>
        {
            private readonly CancellationToken cancellationToken;

            public CanceledResultSource(CancellationToken cancellationToken)
            {
                this.cancellationToken = cancellationToken;
            }

            public T GetResult(short token)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            void IGDTaskSource.GetResult(short token)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            public GDTaskStatus GetStatus(short token)
            {
                return GDTaskStatus.Canceled;
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                return GDTaskStatus.Canceled;
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                continuation(state);
            }
        }

        private sealed class DeferPromise : IGDTaskSource
        {
            private Func<GDTask> factory;
            private GDTask task;
            private Awaiter awaiter;

            public DeferPromise(Func<GDTask> factory)
            {
                this.factory = factory;
            }

            public void GetResult(short token)
            {
                awaiter.GetResult();
            }

            public GDTaskStatus GetStatus(short token)
            {
                var f = Interlocked.Exchange(ref factory, null);
                if (f != null)
                {
                    task = f();
                    awaiter = task.GetAwaiter();
                }

                return task.Status;
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                awaiter.SourceOnCompleted(continuation, state);
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                return task.Status;
            }
        }

        private sealed class DeferPromise<T> : IGDTaskSource<T>
        {
            private Func<GDTask<T>> factory;
            private GDTask<T> task;
            private GDTask<T>.Awaiter awaiter;

            public DeferPromise(Func<GDTask<T>> factory)
            {
                this.factory = factory;
            }

            public T GetResult(short token)
            {
                return awaiter.GetResult();
            }

            void IGDTaskSource.GetResult(short token)
            {
                awaiter.GetResult();
            }

            public GDTaskStatus GetStatus(short token)
            {
                var f = Interlocked.Exchange(ref factory, null);
                if (f != null)
                {
                    task = f();
                    awaiter = task.GetAwaiter();
                }

                return task.Status;
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                awaiter.SourceOnCompleted(continuation, state);
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                return task.Status;
            }
        }

        private sealed class NeverPromise<T> : IGDTaskSource<T>
        {
            private readonly CancellationToken cancellationToken;
            private GDTaskCompletionSourceCore<T> core;

            public NeverPromise(CancellationToken cancellationToken)
            {
                this.cancellationToken = cancellationToken;
                if (this.cancellationToken.CanBeCanceled)
                {
                    this.cancellationToken.RegisterWithoutCaptureExecutionContext(CancellationCallback, this);
                }
            }

            private static void CancellationCallback(object state)
            {
                var self = (NeverPromise<T>)state;
                self.core.TrySetCanceled(self.cancellationToken);
            }

            public T GetResult(short token)
            {
                return core.GetResult(token);
            }

            public GDTaskStatus GetStatus(short token)
            {
                return core.GetStatus(token);
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                return core.UnsafeGetStatus();
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                core.OnCompleted(continuation, state, token);
            }

            void IGDTaskSource.GetResult(short token)
            {
                core.GetResult(token);
            }
        }
    }

    internal static class CompletedTasks
    {
        public static readonly GDTask<AsyncUnit> AsyncUnit = GDTask.FromResult(GodotTask.AsyncUnit.Default);
        public static readonly GDTask<bool> True = GDTask.FromResult(true);
        public static readonly GDTask<bool> False = GDTask.FromResult(false);
        public static readonly GDTask<int> Zero = GDTask.FromResult(0);
        public static readonly GDTask<int> MinusOne = GDTask.FromResult(-1);
        public static readonly GDTask<int> One = GDTask.FromResult(1);
    }
}

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using GodotTask.Internal;

namespace GodotTask
{
    public partial struct GDTask
    {

        /// <summary>
        /// Creates an awaitable that asynchronously yields back to the next <see cref="PlayerLoopTiming.Process"/> from the main thread when awaited, with specified <see cref="CancellationToken"/>.
        /// </summary>
        /// <returns>
        /// A context that, when awaited, will asynchronously transition back into the next <see cref="PlayerLoopTiming.Process"/> from the main thread at the time of the await. This awaitable behaves identically as <see cref="Yield(CancellationToken)"/> in case the call site is from the main thread. 
        /// </returns>
        public static SwitchToMainThreadAwaitable SwitchToMainThread(CancellationToken cancellationToken = default)
        {
            return new SwitchToMainThreadAwaitable(CreateTarget(PlayerLoopTiming.Process), cancellationToken);
        }

        /// <summary>
        /// Creates an awaitable that asynchronously yields back to the next provided <see cref="PlayerLoopTiming"/> from the main thread when awaited, with specified <see cref="CancellationToken"/>.
        /// </summary>
        /// <returns>
        /// A context that, when awaited, will asynchronously transition back into the next provided <see cref="PlayerLoopTiming"/> from the main thread at the time of the await. This awaitable behaves identically as <see cref="Yield(PlayerLoopTiming, CancellationToken)"/> in case the call site is from the main thread. 
        /// </returns>
        public static SwitchToMainThreadAwaitable SwitchToMainThread(PlayerLoopTiming timing, CancellationToken cancellationToken = default)
        {
            return new SwitchToMainThreadAwaitable(CreateTarget(timing), cancellationToken);
        }

        /// <summary>
        /// Creates an awaitable that asynchronously yields back to the next <see cref="PlayerLoopTiming.Process"/> of the provided custom player loop when awaited, with specified <see cref="CancellationToken"/>.
        /// </summary>
        public static SwitchToMainThreadAwaitable SwitchToMainThread(ICustomPlayerLoop customPlayerLoop, CancellationToken cancellationToken = default)
        {
            return new SwitchToMainThreadAwaitable(CreateTarget(customPlayerLoop, PlayerLoopTiming.Process), cancellationToken);
        }

        /// <summary>
        /// Creates an awaitable that asynchronously yields back to the next provided <see cref="PlayerLoopTiming"/> of the provided custom player loop when awaited, with specified <see cref="CancellationToken"/>.
        /// </summary>
        public static SwitchToMainThreadAwaitable SwitchToMainThread(ICustomPlayerLoop customPlayerLoop, PlayerLoopTiming timing, CancellationToken cancellationToken = default)
        {
            return new SwitchToMainThreadAwaitable(CreateTarget(customPlayerLoop, timing), cancellationToken);
        }

        /// <summary>
        /// Creates an asynchronously disposable that asynchronously yields back to the next <see cref="PlayerLoopTiming.Process"/> from the main thread after using scope is closed, with specified <see cref="CancellationToken"/>.
        /// </summary>
        /// <returns>
        /// A context that, when disposed, will asynchronously transition back into the next <see cref="PlayerLoopTiming.Process"/> from the main thread at the time of the dispose. This behaves identically as <see cref="Yield(CancellationToken)"/> in case the call site is from the main thread. 
        /// </returns>
        public static ReturnToMainThread ReturnToMainThread(CancellationToken cancellationToken = default)
        {
            return new ReturnToMainThread(CreateTarget(PlayerLoopTiming.Process), cancellationToken);
        }

        /// <summary>
        /// Creates an asynchronously disposable that asynchronously yields back to the next provided <see cref="PlayerLoopTiming"/> from the main thread after using scope is closed, with specified <see cref="CancellationToken"/>.
        /// </summary>
        /// <returns>
        /// A context that, when disposed, will asynchronously transition back into the next provided <see cref="PlayerLoopTiming"/> from the main thread at the time of the dispose. This behaves identically as <see cref="Yield(PlayerLoopTiming, CancellationToken)"/> in case the call site is from the main thread. 
        /// </returns>
        public static ReturnToMainThread ReturnToMainThread(PlayerLoopTiming timing, CancellationToken cancellationToken = default)
        {
            return new ReturnToMainThread(CreateTarget(timing), cancellationToken);
        }

        /// <summary>
        /// Creates an asynchronously disposable that asynchronously yields back to the next <see cref="PlayerLoopTiming.Process"/> of the provided custom player loop after using scope is closed, with specified <see cref="CancellationToken"/>.
        /// </summary>
        public static ReturnToMainThread ReturnToMainThread(ICustomPlayerLoop customPlayerLoop, CancellationToken cancellationToken = default)
        {
            return new ReturnToMainThread(CreateTarget(customPlayerLoop, PlayerLoopTiming.Process), cancellationToken);
        }

        /// <summary>
        /// Creates an asynchronously disposable that asynchronously yields back to the next provided <see cref="PlayerLoopTiming"/> of the provided custom player loop after using scope is closed, with specified <see cref="CancellationToken"/>.
        /// </summary>
        public static ReturnToMainThread ReturnToMainThread(ICustomPlayerLoop customPlayerLoop, PlayerLoopTiming timing, CancellationToken cancellationToken = default)
        {
            return new ReturnToMainThread(CreateTarget(customPlayerLoop, timing), cancellationToken);
        }

        /// <summary>
        /// Queue the action execution to the next specified <see cref="PlayerLoopTiming"/>.
        /// </summary>
        public static void Post(Action action, PlayerLoopTiming timing = PlayerLoopTiming.Process)
        {
            CreateTarget(timing).AddContinuation(action);
        }

        /// <summary>
        /// Queue the action execution to the next specified <see cref="PlayerLoopTiming"/> of the provided custom player loop.
        /// </summary>
        public static void Post(Action action, ICustomPlayerLoop customPlayerLoop, PlayerLoopTiming timing = PlayerLoopTiming.Process)
        {
            CreateTarget(customPlayerLoop, timing).AddContinuation(action);
        }

        /// <summary>
        /// Creates an awaitable that asynchronously yields to <see cref="ThreadPool"/> when awaited.
        /// </summary>
        /// <returns>
        /// A context that, when awaited, will asynchronously transition to <see cref="ThreadPool"/> at the time of the await.
        /// </returns>
        public static SwitchToThreadPoolAwaitable SwitchToThreadPool()
        {
            return new SwitchToThreadPoolAwaitable();
        }

        /// <summary>
        /// Creates an awaitable that asynchronously yields to the provided <see cref="SynchronizationContext"/> when awaited, with specified <see cref="CancellationToken"/>.
        /// </summary>
        /// <returns>
        /// A context that, when awaited, will asynchronously transition to the provided <see cref="SynchronizationContext"/> at the time of the await.
        /// </returns>
        public static SwitchToSynchronizationContextAwaitable SwitchToSynchronizationContext(SynchronizationContext synchronizationContext, CancellationToken cancellationToken = default)
        {
            Error.ThrowArgumentNullException(synchronizationContext, nameof(synchronizationContext));
            return new SwitchToSynchronizationContextAwaitable(synchronizationContext, cancellationToken);
        }

        /// <summary>
        /// Creates an asynchronously disposable that asynchronously yields back to the provided <see cref="SynchronizationContext"/> after using scope is closed, with specified <see cref="CancellationToken"/>.
        /// </summary>
        /// <returns>
        /// A context that, when disposed, will asynchronously transition back into the provided <see cref="SynchronizationContext"/> at the time of the dispose.
        /// </returns>
        public static ReturnToSynchronizationContext ReturnToSynchronizationContext(SynchronizationContext synchronizationContext, CancellationToken cancellationToken = default)
        {
            return new ReturnToSynchronizationContext(synchronizationContext, false, cancellationToken);
        }

        /// <summary>
        /// Creates an asynchronously disposable that asynchronously yields back to the <see cref="SynchronizationContext.Current"/> after using scope is closed, with specified <see cref="CancellationToken"/>.
        /// </summary>
        /// <returns>
        /// A context that, when disposed, will asynchronously transition back into the provided <see cref="SynchronizationContext.Current"/> at the time of the dispose.
        /// </returns>
        public static ReturnToSynchronizationContext ReturnToCurrentSynchronizationContext(bool dontPostWhenSameContext = true, CancellationToken cancellationToken = default)
        {
            return new ReturnToSynchronizationContext(SynchronizationContext.Current, dontPostWhenSameContext, cancellationToken);
        }
    }

    /// <summary>
    /// An awaitable that, when awaited, will asynchronously yields back to the next <see cref="PlayerLoopTiming"/>.
    /// </summary>
    public readonly struct SwitchToMainThreadAwaitable
    {
        internal readonly PlayerLoopRunnerTarget target;
        internal readonly CancellationToken cancellationToken;

        internal SwitchToMainThreadAwaitable(PlayerLoopRunnerTarget target, CancellationToken cancellationToken)
        {
            this.target = target;
            this.cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Gets an awaiter used to await this <see cref="SwitchToMainThreadAwaitable"/>.
        /// </summary>
        public Awaiter GetAwaiter() => new Awaiter(target, cancellationToken);

        /// <summary>
        /// Provides an awaiter for awaiting a <see cref="SwitchToMainThreadAwaitable"/>.
        /// </summary>
        public readonly struct Awaiter : ICriticalNotifyCompletion
        {
            private readonly PlayerLoopRunnerTarget target;
            private readonly CancellationToken cancellationToken;

            internal Awaiter(PlayerLoopRunnerTarget target, CancellationToken cancellationToken)
            {
                this.target = target;
                this.cancellationToken = cancellationToken;
            }

            /// <summary>
            /// Gets whether this <see cref="SwitchToMainThreadAwaitable">Task</see> has completed.
            /// </summary>
            public bool IsCompleted
            {
                get
                {
                    var currentThreadId = Environment.CurrentManagedThreadId;
                    var mainThreadId = target.MainThreadId;
                    if (mainThreadId != 0 && mainThreadId == currentThreadId)
                    {
                        return true; // run immediate.
                    }
                    else
                    {
                        return false; // register continuation.
                    }
                }
            }

            /// <summary>
            /// Ends the awaiting on the completed <see cref="SwitchToMainThreadAwaitable"/>.
            /// </summary>
            public void GetResult() {
                cancellationToken.ThrowIfCancellationRequested();
            }

            /// <summary>
            /// Schedules the continuation onto the <see cref="SwitchToMainThreadAwaitable"/> associated with this <see cref="Awaiter"/>.
            /// </summary>
            public void OnCompleted(Action continuation)
            {
                target.AddContinuation(continuation);
            }

            /// <summary>
            /// Schedules the continuation onto the <see cref="SwitchToMainThreadAwaitable"/> associated with this <see cref="Awaiter"/>.
            /// </summary>
            public void UnsafeOnCompleted(Action continuation)
            {
                target.AddContinuation(continuation);
            }
        }
    }

    /// <summary>
    /// An context that, when disposed, will asynchronously yields back to the next specified <see cref="PlayerLoopTiming"/> on the main thread.
    /// </summary>
    public readonly struct ReturnToMainThread
    {
        internal readonly PlayerLoopRunnerTarget target;
        internal readonly CancellationToken cancellationToken;

        internal ReturnToMainThread(PlayerLoopRunnerTarget target, CancellationToken cancellationToken)
        {
            this.target = target;
            this.cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Dispose this context and asynchronously yields back to the next specified <see cref="PlayerLoopTiming"/> on the main thread.
        /// </summary>
        public Awaiter DisposeAsync()
        {
            return new Awaiter(target, cancellationToken); // run immediate.
        }

        /// <summary>
        /// Provides an awaiter for awaiting a <see cref="ReturnToMainThread"/>.
        /// </summary>
        public readonly struct Awaiter : ICriticalNotifyCompletion
        {
            private readonly PlayerLoopRunnerTarget target;
            private readonly CancellationToken cancellationToken;

            internal Awaiter(PlayerLoopRunnerTarget target, CancellationToken cancellationToken)
            {
                this.target = target;
                this.cancellationToken = cancellationToken;
            }

            /// <summary>
            /// Return self
            /// </summary>
            public Awaiter GetAwaiter() => this;

            /// <summary>
            /// Gets whether the current <see cref="GDTaskPlayerLoopRunner.MainThreadId"/> is <see cref="Environment.CurrentManagedThreadId"/>.
            /// </summary>
            public bool IsCompleted
            {
                get
                {
                    var mainThreadId = target.MainThreadId;
                    return mainThreadId != 0 && mainThreadId == Environment.CurrentManagedThreadId;
                }
            }

            /// <summary>
            /// Ends the awaiting on the completed <see cref="ReturnToMainThread"/>.
            /// </summary>
            public void GetResult() {
                cancellationToken.ThrowIfCancellationRequested();
            }

            /// <summary>
            /// Schedules the continuation onto the <see cref="ReturnToMainThread"/> associated with this <see cref="Awaiter"/>.
            /// </summary>
            public void OnCompleted(Action continuation)
            {
                target.AddContinuation(continuation);
            }

            /// <summary>
            /// Schedules the continuation onto the <see cref="ReturnToMainThread"/> associated with this <see cref="Awaiter"/>.
            /// </summary>
            public void UnsafeOnCompleted(Action continuation)
            {
                target.AddContinuation(continuation);
            }
        }
    }

    /// <summary>
    /// An context that, when disposed, will asynchronously yields to the thread pool.
    /// </summary>
    public struct SwitchToThreadPoolAwaitable
    {
        /// <summary>
        /// Initializes the <see cref="SwitchToThreadPoolAwaitable"/>.
        /// </summary>
        public SwitchToThreadPoolAwaitable() {
        }

        /// <summary>
        /// Gets an awaiter used to await this <see cref="SwitchToThreadPoolAwaitable"/>.
        /// </summary>
        public Awaiter GetAwaiter() => new Awaiter();

        /// <summary>
        /// Provides an awaiter for awaiting a <see cref="SwitchToThreadPoolAwaitable"/>.
        /// </summary>
        public readonly struct Awaiter : ICriticalNotifyCompletion
        {
            private static readonly WaitCallback switchToCallback = Callback;

            /// <summary>
            /// Initializes the <see cref="Awaiter"/>.
            /// </summary>
            public Awaiter() {
            }

            /// <summary>
            /// Gets whether this <see cref="SwitchToThreadPoolAwaitable"/> has completed, always returns false.
            /// </summary>
            public bool IsCompleted => false;
            
            /// <summary>
            /// Do nothing
            /// </summary>
            public void GetResult() {
            }

            /// <summary>
            /// Schedules the continuation onto the <see cref="SwitchToThreadPoolAwaitable"/> associated with this <see cref="Awaiter"/>.
            /// </summary>
            public void OnCompleted(Action continuation)
            {
                ThreadPool.QueueUserWorkItem(switchToCallback, continuation);
            }

            /// <summary>
            /// Schedules the continuation onto the <see cref="SwitchToThreadPoolAwaitable"/> associated with this <see cref="Awaiter"/>.
            /// </summary>
            public void UnsafeOnCompleted(Action continuation)
            {
                ThreadPool.UnsafeQueueUserWorkItem(switchToCallback, continuation);
            }

            private static void Callback(object state)
            {
                var continuation = (Action)state;
                continuation();
            }
        }
    }

    /// <summary>
    /// An awaitable that asynchronously yields to the provided <see cref="SynchronizationContext"/> when awaited.
    /// </summary>
    public readonly struct SwitchToSynchronizationContextAwaitable
    {
        internal readonly SynchronizationContext synchronizationContext;
        internal readonly CancellationToken cancellationToken;

        internal SwitchToSynchronizationContextAwaitable(SynchronizationContext synchronizationContext, CancellationToken cancellationToken)
        {
            this.synchronizationContext = synchronizationContext;
            this.cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Gets an awaiter used to await this <see cref="SwitchToSynchronizationContextAwaitable"/>.
        /// </summary>
        public Awaiter GetAwaiter() => new Awaiter(synchronizationContext, cancellationToken);

        /// <summary>
        /// Provides an awaiter for awaiting a <see cref="SwitchToSynchronizationContextAwaitable"/>.
        /// </summary>
        public readonly struct Awaiter : ICriticalNotifyCompletion
        {
            private static readonly SendOrPostCallback switchToCallback = Callback;

            private readonly SynchronizationContext synchronizationContext;
            private readonly CancellationToken cancellationToken;

            /// <summary>
            /// Initializes the <see cref="Awaiter"/>.
            /// </summary>
            public Awaiter(SynchronizationContext synchronizationContext, CancellationToken cancellationToken)
            {
                this.synchronizationContext = synchronizationContext;
                this.cancellationToken = cancellationToken;
            }

            /// <summary>
            /// Gets whether this <see cref="SwitchToSynchronizationContextAwaitable"/> has completed, always returns false.
            /// </summary>
            public bool IsCompleted => false;
            
            /// <summary>
            /// Ends the awaiting on the completed <see cref="SwitchToSynchronizationContextAwaitable"/>.
            /// </summary>
            public void GetResult() {
                cancellationToken.ThrowIfCancellationRequested();
            }

            /// <summary>
            /// Schedules the continuation onto the <see cref="SwitchToSynchronizationContextAwaitable"/> associated with this <see cref="Awaiter"/>.
            /// </summary>
            public void OnCompleted(Action continuation)
            {
                synchronizationContext.Post(switchToCallback, continuation);
            }

            /// <summary>
            /// Schedules the continuation onto the <see cref="SwitchToSynchronizationContextAwaitable"/> associated with this <see cref="Awaiter"/>.
            /// </summary>
            public void UnsafeOnCompleted(Action continuation)
            {
                synchronizationContext.Post(switchToCallback, continuation);
            }

            private static void Callback(object state)
            {
                var continuation = (Action)state;
                continuation();
            }
        }
    }

    /// <summary>
    /// An context that, when disposed, will asynchronously yields back to the previous <see cref="SynchronizationContext"/>.
    /// </summary>
    public readonly struct ReturnToSynchronizationContext
    {
        internal readonly SynchronizationContext syncContext;
        internal readonly bool dontPostWhenSameContext;
        internal readonly CancellationToken cancellationToken;

        internal ReturnToSynchronizationContext(SynchronizationContext syncContext, bool dontPostWhenSameContext, CancellationToken cancellationToken)
        {
            this.syncContext = syncContext;
            this.dontPostWhenSameContext = dontPostWhenSameContext;
            this.cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Dispose this context and asynchronously yields back to the previous <see cref="SynchronizationContext"/>.
        /// </summary>
        public Awaiter DisposeAsync()
        {
            return new Awaiter(syncContext, dontPostWhenSameContext, cancellationToken);
        }

        /// <summary>
        /// Provides an awaiter for awaiting a <see cref="ReturnToSynchronizationContext"/>.
        /// </summary>
        public readonly struct Awaiter : ICriticalNotifyCompletion
        {
            private static readonly SendOrPostCallback switchToCallback = Callback;

            private readonly SynchronizationContext synchronizationContext;
            private readonly bool dontPostWhenSameContext;
            private readonly CancellationToken cancellationToken;

            /// <summary>
            /// Initializes the <see cref="Awaiter"/>.
            /// </summary>
            public Awaiter(SynchronizationContext synchronizationContext, bool dontPostWhenSameContext, CancellationToken cancellationToken)
            {
                this.synchronizationContext = synchronizationContext;
                this.dontPostWhenSameContext = dontPostWhenSameContext;
                this.cancellationToken = cancellationToken;
            }

            /// <summary>
            /// Return self
            /// </summary>
            public Awaiter GetAwaiter() => this;

            /// <summary>
            /// Gets whether the <see cref="SynchronizationContext.Current"/> synchronizationContext is the captured one.
            /// </summary>
            public bool IsCompleted
            {
                get
                {
                    if (!dontPostWhenSameContext) return false;

                    var current = SynchronizationContext.Current;
                    if (current == synchronizationContext)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            /// <summary>
            /// Ends the awaiting on the completed <see cref="ReturnToSynchronizationContext"/>.
            /// </summary>
            public void GetResult() {
                cancellationToken.ThrowIfCancellationRequested();
            }

            /// <summary>
            /// Schedules the continuation onto the <see cref="ReturnToSynchronizationContext"/> associated with this <see cref="Awaiter"/>.
            /// </summary>
            public void OnCompleted(Action continuation)
            {
                synchronizationContext.Post(switchToCallback, continuation);
            }

            /// <summary>
            /// Schedules the continuation onto the <see cref="ReturnToSynchronizationContext"/> associated with this <see cref="Awaiter"/>.
            /// </summary>
            public void UnsafeOnCompleted(Action continuation)
            {
                synchronizationContext.Post(switchToCallback, continuation);
            }

            private static void Callback(object state)
            {
                var continuation = (Action)state;
                continuation();
            }
        }
    }
}

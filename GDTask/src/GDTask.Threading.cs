using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fractural.Tasks.Internal;

namespace Fractural.Tasks
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
            return new SwitchToMainThreadAwaitable(PlayerLoopTiming.Process, cancellationToken);
        }

        /// <summary>
        /// Creates an awaitable that asynchronously yields back to the next provided <see cref="PlayerLoopTiming"/> from the main thread when awaited, with specified <see cref="CancellationToken"/>.
        /// </summary>
        /// <returns>
        /// A context that, when awaited, will asynchronously transition back into the next provided <see cref="PlayerLoopTiming"/> from the main thread at the time of the await. This awaitable behaves identically as <see cref="Yield(PlayerLoopTiming, CancellationToken)"/> in case the call site is from the main thread. 
        /// </returns>
        public static SwitchToMainThreadAwaitable SwitchToMainThread(PlayerLoopTiming timing, CancellationToken cancellationToken = default)
        {
            return new SwitchToMainThreadAwaitable(timing, cancellationToken);
        }

        /// <summary>
        /// Creates an asynchronously disposable that asynchronously yields back to the next <see cref="PlayerLoopTiming.Process"/> from the main thread after using scope is closed, with specified <see cref="CancellationToken"/>.
        /// </summary>
        /// <returns>
        /// A context that, when disposed, will asynchronously transition back into the next <see cref="PlayerLoopTiming.Process"/> from the main thread at the time of the dispose. This behaves identically as <see cref="Yield(CancellationToken)"/> in case the call site is from the main thread. 
        /// </returns>
        public static ReturnToMainThread ReturnToMainThread(CancellationToken cancellationToken = default)
        {
            return new ReturnToMainThread(PlayerLoopTiming.Process, cancellationToken);
        }

        /// <summary>
        /// Creates an asynchronously disposable that asynchronously yields back to the next provided <see cref="PlayerLoopTiming"/> from the main thread after using scope is closed, with specified <see cref="CancellationToken"/>.
        /// </summary>
        /// <returns>
        /// A context that, when disposed, will asynchronously transition back into the next provided <see cref="PlayerLoopTiming"/> from the main thread at the time of the dispose. This behaves identically as <see cref="Yield(PlayerLoopTiming, CancellationToken)"/> in case the call site is from the main thread. 
        /// </returns>
        public static ReturnToMainThread ReturnToMainThread(PlayerLoopTiming timing, CancellationToken cancellationToken = default)
        {
            return new ReturnToMainThread(timing, cancellationToken);
        }

        /// <summary>
        /// Queue the action execution to the next specified <see cref="PlayerLoopTiming"/>.
        /// </summary>
        public static void Post(Action action, PlayerLoopTiming timing = PlayerLoopTiming.Process)
        {
            GDTaskPlayerLoopAutoload.AddContinuation(timing, action);
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

    public struct SwitchToMainThreadAwaitable
    {
        readonly PlayerLoopTiming playerLoopTiming;
        readonly CancellationToken cancellationToken;

        public SwitchToMainThreadAwaitable(PlayerLoopTiming playerLoopTiming, CancellationToken cancellationToken)
        {
            this.playerLoopTiming = playerLoopTiming;
            this.cancellationToken = cancellationToken;
        }

        public Awaiter GetAwaiter() => new Awaiter(playerLoopTiming, cancellationToken);

        public struct Awaiter : ICriticalNotifyCompletion
        {
            readonly PlayerLoopTiming playerLoopTiming;
            readonly CancellationToken cancellationToken;

            public Awaiter(PlayerLoopTiming playerLoopTiming, CancellationToken cancellationToken)
            {
                this.playerLoopTiming = playerLoopTiming;
                this.cancellationToken = cancellationToken;
            }

            public bool IsCompleted
            {
                get
                {
                    var currentThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                    if (GDTaskPlayerLoopAutoload.MainThreadId == currentThreadId)
                    {
                        return true; // run immediate.
                    }
                    else
                    {
                        return false; // register continuation.
                    }
                }
            }

            public void GetResult() { cancellationToken.ThrowIfCancellationRequested(); }

            public void OnCompleted(Action continuation)
            {
                GDTaskPlayerLoopAutoload.AddContinuation(playerLoopTiming, continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                GDTaskPlayerLoopAutoload.AddContinuation(playerLoopTiming, continuation);
            }
        }
    }

    public struct ReturnToMainThread
    {
        readonly PlayerLoopTiming playerLoopTiming;
        readonly CancellationToken cancellationToken;

        public ReturnToMainThread(PlayerLoopTiming playerLoopTiming, CancellationToken cancellationToken)
        {
            this.playerLoopTiming = playerLoopTiming;
            this.cancellationToken = cancellationToken;
        }

        public Awaiter DisposeAsync()
        {
            return new Awaiter(playerLoopTiming, cancellationToken); // run immediate.
        }

        public readonly struct Awaiter : ICriticalNotifyCompletion
        {
            readonly PlayerLoopTiming timing;
            readonly CancellationToken cancellationToken;

            public Awaiter(PlayerLoopTiming timing, CancellationToken cancellationToken)
            {
                this.timing = timing;
                this.cancellationToken = cancellationToken;
            }

            public Awaiter GetAwaiter() => this;

            public bool IsCompleted => GDTaskPlayerLoopAutoload.MainThreadId == System.Threading.Thread.CurrentThread.ManagedThreadId;

            public void GetResult() { cancellationToken.ThrowIfCancellationRequested(); }

            public void OnCompleted(Action continuation)
            {
                GDTaskPlayerLoopAutoload.AddContinuation(timing, continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                GDTaskPlayerLoopAutoload.AddContinuation(timing, continuation);
            }
        }
    }


    public struct SwitchToThreadPoolAwaitable
    {
        public Awaiter GetAwaiter() => new Awaiter();

        public struct Awaiter : ICriticalNotifyCompletion
        {
            static readonly WaitCallback switchToCallback = Callback;

            public bool IsCompleted => false;
            public void GetResult() { }

            public void OnCompleted(Action continuation)
            {
                ThreadPool.QueueUserWorkItem(switchToCallback, continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                ThreadPool.UnsafeQueueUserWorkItem(switchToCallback, continuation);
            }

            static void Callback(object state)
            {
                var continuation = (Action)state;
                continuation();
            }
        }
    }

    public struct SwitchToSynchronizationContextAwaitable
    {
        readonly SynchronizationContext synchronizationContext;
        readonly CancellationToken cancellationToken;

        public SwitchToSynchronizationContextAwaitable(SynchronizationContext synchronizationContext, CancellationToken cancellationToken)
        {
            this.synchronizationContext = synchronizationContext;
            this.cancellationToken = cancellationToken;
        }

        public Awaiter GetAwaiter() => new Awaiter(synchronizationContext, cancellationToken);

        public struct Awaiter : ICriticalNotifyCompletion
        {
            static readonly SendOrPostCallback switchToCallback = Callback;
            readonly SynchronizationContext synchronizationContext;
            readonly CancellationToken cancellationToken;

            public Awaiter(SynchronizationContext synchronizationContext, CancellationToken cancellationToken)
            {
                this.synchronizationContext = synchronizationContext;
                this.cancellationToken = cancellationToken;
            }

            public bool IsCompleted => false;
            public void GetResult() { cancellationToken.ThrowIfCancellationRequested(); }

            public void OnCompleted(Action continuation)
            {
                synchronizationContext.Post(switchToCallback, continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                synchronizationContext.Post(switchToCallback, continuation);
            }

            static void Callback(object state)
            {
                var continuation = (Action)state;
                continuation();
            }
        }
    }

    public struct ReturnToSynchronizationContext
    {
        readonly SynchronizationContext syncContext;
        readonly bool dontPostWhenSameContext;
        readonly CancellationToken cancellationToken;

        public ReturnToSynchronizationContext(SynchronizationContext syncContext, bool dontPostWhenSameContext, CancellationToken cancellationToken)
        {
            this.syncContext = syncContext;
            this.dontPostWhenSameContext = dontPostWhenSameContext;
            this.cancellationToken = cancellationToken;
        }

        public Awaiter DisposeAsync()
        {
            return new Awaiter(syncContext, dontPostWhenSameContext, cancellationToken);
        }

        public struct Awaiter : ICriticalNotifyCompletion
        {
            static readonly SendOrPostCallback switchToCallback = Callback;

            readonly SynchronizationContext synchronizationContext;
            readonly bool dontPostWhenSameContext;
            readonly CancellationToken cancellationToken;

            public Awaiter(SynchronizationContext synchronizationContext, bool dontPostWhenSameContext, CancellationToken cancellationToken)
            {
                this.synchronizationContext = synchronizationContext;
                this.dontPostWhenSameContext = dontPostWhenSameContext;
                this.cancellationToken = cancellationToken;
            }

            public Awaiter GetAwaiter() => this;

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

            public void GetResult() { cancellationToken.ThrowIfCancellationRequested(); }

            public void OnCompleted(Action continuation)
            {
                synchronizationContext.Post(switchToCallback, continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                synchronizationContext.Post(switchToCallback, continuation);
            }

            static void Callback(object state)
            {
                var continuation = (Action)state;
                continuation();
            }
        }
    }
}

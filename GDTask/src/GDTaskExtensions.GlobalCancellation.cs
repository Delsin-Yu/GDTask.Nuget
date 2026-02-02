using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading;

namespace GodotTask
{
    public static partial class GDTaskExtensions
    {
        /// <summary>
        /// Attaches the global <see cref="CancellationToken"/> to the given <see cref="GDTask"/>.
        /// </summary>
        public static GDTask AttachGlobalCancellation(this GDTask task)
        {
            return AttachExternalCancellation(task, GDTaskPlayerLoopRunner.GetGlobalCancellationToken());
        }

        /// <summary>
        /// Attaches the global <see cref="CancellationToken"/> to the given <see cref="GDTask{T}"/>.
        /// </summary>
        public static GDTask<T> AttachGlobalCancellation<T>(this GDTask<T> task)
        {
            return AttachExternalCancellation(task, GDTaskPlayerLoopRunner.GetGlobalCancellationToken());
        }

        /// <summary>
        /// Attaches the global <see cref="CancellationToken"/> to the given <see cref="GDTask.DeferredAwaitable"/>.
        /// </summary>
        public static DeferredWithGlobalCancellationAwaitable AttachGlobalCancellation(this GDTask.DeferredAwaitable deferredAwaitable)
        {
            return new DeferredWithGlobalCancellationAwaitable();
        }

        /// <summary>
        /// An awaitable that when awaited, asynchronously continues the execution at the end of the current frame (idle time).
        /// Equivalent to <see cref="GDTask.DeferredAwaitable"/> with the global cancellation token attached.
        /// </summary>
        public readonly struct DeferredWithGlobalCancellationAwaitable
        {
            private readonly CancellationToken globalCancellationToken;

            /// <summary>
            /// Initializes the <see cref="DeferredWithGlobalCancellationAwaitable"/>.
            /// </summary>
            public DeferredWithGlobalCancellationAwaitable() {
                globalCancellationToken = GDTaskPlayerLoopRunner.GetGlobalCancellationToken();
            }

            /// <summary>
            /// Gets an awaiter used to await this <see cref="DeferredWithGlobalCancellationAwaitable"/>.
            /// </summary>
            public Awaiter GetAwaiter() => new(globalCancellationToken);

            /// <summary>
            /// Creates a <see cref="GDTask"/> that represents this <see cref="DeferredWithGlobalCancellationAwaitable"/>.
            /// </summary>
            public GDTask ToGDTask()
            {
                return GDTask.Deferred(globalCancellationToken);
            }

            /// <summary>
            /// Provides an awaiter for awaiting a <see cref="DeferredWithGlobalCancellationAwaitable"/>.
            /// </summary>
            public readonly struct Awaiter : ICriticalNotifyCompletion
            {
                private readonly CancellationToken globalCancellationToken;

                /// <summary>
                /// Initializes the <see cref="Awaiter"/>.
                /// </summary>
                internal Awaiter(CancellationToken globalCancellationToken) {
                    this.globalCancellationToken = globalCancellationToken;
                }

                /// <summary>
                /// Ends the awaiting on the completed <see cref="DeferredWithGlobalCancellationAwaitable"/>.
                /// </summary>
                public void GetResult() {
                    globalCancellationToken.ThrowIfCancellationRequested();
                }
            
                /// <summary>
                /// Gets whether this <see cref="YieldAwaitable">Task</see> has completed, always returns false.
                /// </summary>
                public bool IsCompleted => false;
          
                /// <summary>
                /// Schedules the continuation onto the <see cref="YieldAwaitable"/> associated with this <see cref="Awaiter"/>.
                /// </summary>
                public void OnCompleted(Action continuation)
                {
                    GDTaskPlayerLoopRunner.AddDeferredContinuation(continuation);
                }

                /// <summary>
                /// Schedules the continuation onto the <see cref="YieldAwaitable"/> associated with this <see cref="Awaiter"/>.
                /// </summary>
                public void UnsafeOnCompleted(Action continuation)
                {
                    GDTaskPlayerLoopRunner.AddDeferredContinuation(continuation);
                }
            }
        }

        /// <summary>
        /// Attaches the global <see cref="CancellationToken"/> to the given <see cref="YieldAwaitable"/>.
        /// </summary>
        public static YieldWithGlobalCancellationAwaitable AttachGlobalCancellation(this YieldAwaitable yieldAwaitable)
        {
            return new YieldWithGlobalCancellationAwaitable(yieldAwaitable.timing);
        }

        /// <summary>
        /// An awaitable that when awaited, asynchronously yields back to the next specified <see cref="PlayerLoopTiming"/>.
        /// Equivalent to <see cref="YieldAwaitable"/> with the global cancellation token attached.
        /// </summary>
        public readonly struct YieldWithGlobalCancellationAwaitable
        {
            private readonly PlayerLoopTiming timing;
            private readonly CancellationToken globalCancellationToken;

            /// <summary>
            /// Initializes the <see cref="YieldWithGlobalCancellationAwaitable"/>.
            /// </summary>
            internal YieldWithGlobalCancellationAwaitable(PlayerLoopTiming timing)
            {
                this.timing = timing;
                globalCancellationToken = GDTaskPlayerLoopRunner.GetGlobalCancellationToken();
            }

            /// <summary>
            /// Gets an awaiter used to await this <see cref="YieldWithGlobalCancellationAwaitable"/>.
            /// </summary>
            public Awaiter GetAwaiter() => new(timing, globalCancellationToken);

            /// <summary>
            /// Creates a <see cref="GDTask"/> that represents this <see cref="YieldWithGlobalCancellationAwaitable"/>.
            /// </summary>
            public GDTask ToGDTask()
            {
                return GDTask.Yield(timing, globalCancellationToken);
            }

            /// <summary>
            /// Provides an awaiter for awaiting a <see cref="YieldWithGlobalCancellationAwaitable"/>.
            /// </summary>
            public readonly struct Awaiter : ICriticalNotifyCompletion
            {
                private readonly PlayerLoopTiming timing;
                private readonly CancellationToken globalCancellationToken;

                /// <summary>
                /// Initializes the <see cref="Awaiter"/>.
                /// </summary>
                internal Awaiter(PlayerLoopTiming timing, CancellationToken globalCancellationToken)
                {
                    this.timing = timing;
                    this.globalCancellationToken = globalCancellationToken;
                }

                /// <summary>
                /// Gets whether this <see cref="YieldWithGlobalCancellationAwaitable">Task</see> has completed, always returns false.
                /// </summary>
                public bool IsCompleted => false;

                /// <summary>
                /// Ends the awaiting on the completed <see cref="YieldWithGlobalCancellationAwaitable"/>.
                /// </summary>
                public void GetResult() {
                    globalCancellationToken.ThrowIfCancellationRequested();
                }

                /// <summary>
                /// Schedules the continuation onto the <see cref="YieldWithGlobalCancellationAwaitable"/> associated with this <see cref="Awaiter"/>.
                /// </summary>
                public void OnCompleted(Action continuation)
                {
                    GDTaskPlayerLoopRunner.AddContinuation(timing, continuation);
                }

                /// <summary>
                /// Schedules the continuation onto the <see cref="YieldWithGlobalCancellationAwaitable"/> associated with this <see cref="Awaiter"/>.
                /// </summary>
                public void UnsafeOnCompleted(Action continuation)
                {
                    GDTaskPlayerLoopRunner.AddContinuation(timing, continuation);
                }
            }
        }

        /// <summary>
        /// Attaches the global <see cref="CancellationToken"/> to the given <see cref="SwitchToMainThreadAwaitable"/>.
        /// </summary>
        public static SwitchToMainThreadWithGlobalCancellationAwaitable AttachGlobalCancellation(this SwitchToMainThreadAwaitable switchToMainThreadAwaitable)
        {
            return new SwitchToMainThreadWithGlobalCancellationAwaitable(switchToMainThreadAwaitable.playerLoopTiming, switchToMainThreadAwaitable.cancellationToken);
        }

        /// <summary>
        /// An awaitable that, when awaited, will asynchronously yields back to the next <see cref="PlayerLoopTiming"/>.
        /// Equivalent to <see cref="SwitchToMainThreadAwaitable"/> with the global cancellation token attached.
        /// </summary>
        public readonly struct SwitchToMainThreadWithGlobalCancellationAwaitable
        {
            private readonly PlayerLoopTiming playerLoopTiming;
            private readonly CancellationToken cancellationToken;
            private readonly CancellationToken globalCancellationToken;

            internal SwitchToMainThreadWithGlobalCancellationAwaitable(PlayerLoopTiming playerLoopTiming, CancellationToken cancellationToken)
            {
                this.playerLoopTiming = playerLoopTiming;
                this.cancellationToken = cancellationToken;
                globalCancellationToken = GDTaskPlayerLoopRunner.GetGlobalCancellationToken();
            }

            /// <summary>
            /// Gets an awaiter used to await this <see cref="SwitchToMainThreadAwaitable"/>.
            /// </summary>
            public Awaiter GetAwaiter() => new(playerLoopTiming, cancellationToken, globalCancellationToken);

            /// <summary>
            /// Provides an awaiter for awaiting a <see cref="SwitchToMainThreadAwaitable"/>.
            /// </summary>
            public readonly struct Awaiter : ICriticalNotifyCompletion
            {
                private readonly PlayerLoopTiming playerLoopTiming;
                private readonly CancellationToken cancellationToken;
                private readonly CancellationToken globalCancellationToken;

                internal Awaiter(PlayerLoopTiming playerLoopTiming, CancellationToken cancellationToken, CancellationToken globalCancellationToken)
                {
                    this.playerLoopTiming = playerLoopTiming;
                    this.cancellationToken = cancellationToken;
                    this.globalCancellationToken = globalCancellationToken;
                }

                /// <summary>
                /// Gets whether this <see cref="SwitchToMainThreadAwaitable">Task</see> has completed.
                /// </summary>
                public bool IsCompleted
                {
                    get
                    {
                        var currentThreadId = Environment.CurrentManagedThreadId;
                        if (GDTaskPlayerLoopRunner.MainThreadId == currentThreadId)
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
                public void GetResult()
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    globalCancellationToken.ThrowIfCancellationRequested();
                }

                /// <summary>
                /// Schedules the continuation onto the <see cref="SwitchToMainThreadAwaitable"/> associated with this <see cref="Awaiter"/>.
                /// </summary>
                public void OnCompleted(Action continuation)
                {
                    GDTaskPlayerLoopRunner.AddContinuation(playerLoopTiming, continuation);
                }

                /// <summary>
                /// Schedules the continuation onto the <see cref="SwitchToMainThreadAwaitable"/> associated with this <see cref="Awaiter"/>.
                /// </summary>
                public void UnsafeOnCompleted(Action continuation)
                {
                    GDTaskPlayerLoopRunner.AddContinuation(playerLoopTiming, continuation);
                }
            }
        }

        /// <summary>
        /// Attaches the global <see cref="CancellationToken"/> to the given <see cref="ReturnToMainThread"/>.
        /// </summary>
        public static ReturnToMainThreadWithGlobalCancellation AttachGlobalCancellation(this ReturnToMainThread returnToMainThread)
        {
            return new ReturnToMainThreadWithGlobalCancellation(returnToMainThread.playerLoopTiming, returnToMainThread.cancellationToken);
        }

        /// <summary>
        /// An context that, when disposed, will asynchronously yields back to the next specified <see cref="PlayerLoopTiming"/> on the main thread.
        /// Equivalent to <see cref="ReturnToMainThread"/> with the global cancellation token attached.
        /// </summary>
        public readonly struct ReturnToMainThreadWithGlobalCancellation
        {
            private readonly PlayerLoopTiming playerLoopTiming;
            private readonly CancellationToken cancellationToken;
            private readonly CancellationToken globalCancellationToken;

            internal ReturnToMainThreadWithGlobalCancellation(PlayerLoopTiming playerLoopTiming, CancellationToken cancellationToken)
            {
                this.playerLoopTiming = playerLoopTiming;
                this.cancellationToken = cancellationToken;
                globalCancellationToken = GDTaskPlayerLoopRunner.GetGlobalCancellationToken();
            }

            /// <summary>
            /// Dispose this context and asynchronously yields back to the next specified <see cref="PlayerLoopTiming"/> on the main thread.
            /// </summary>
            public Awaiter DisposeAsync()
            {
                return new Awaiter(playerLoopTiming, cancellationToken, globalCancellationToken); // run immediate.
            }

            /// <summary>
            /// Provides an awaiter for awaiting a <see cref="ReturnToMainThreadWithGlobalCancellation"/>.
            /// </summary>
            public readonly struct Awaiter : ICriticalNotifyCompletion
            {
                private readonly PlayerLoopTiming timing;
                private readonly CancellationToken cancellationToken;
                private readonly CancellationToken globalCancellationToken;

                internal Awaiter(PlayerLoopTiming timing, CancellationToken cancellationToken, CancellationToken globalCancellationToken)
                {
                    this.timing = timing;
                    this.cancellationToken = cancellationToken;
                    this.globalCancellationToken = globalCancellationToken;
                }

                /// <summary>
                /// Return self
                /// </summary>
                public Awaiter GetAwaiter() => this;

                /// <summary>
                /// Gets whether the current <see cref="GDTaskPlayerLoopRunner.MainThreadId"/> is <see cref="Environment.CurrentManagedThreadId"/>.
                /// </summary>
                public bool IsCompleted => GDTaskPlayerLoopRunner.MainThreadId == Environment.CurrentManagedThreadId;

                /// <summary>
                /// Ends the awaiting on the completed <see cref="ReturnToMainThreadWithGlobalCancellation"/>.
                /// </summary>
                public void GetResult() {
                    cancellationToken.ThrowIfCancellationRequested();
                    globalCancellationToken.ThrowIfCancellationRequested();
                }

                /// <summary>
                /// Schedules the continuation onto the <see cref="ReturnToMainThreadWithGlobalCancellation"/> associated with this <see cref="Awaiter"/>.
                /// </summary>
                public void OnCompleted(Action continuation)
                {
                    GDTaskPlayerLoopRunner.AddContinuation(timing, continuation);
                }

                /// <summary>
                /// Schedules the continuation onto the <see cref="ReturnToMainThreadWithGlobalCancellation"/> associated with this <see cref="Awaiter"/>.
                /// </summary>
                public void UnsafeOnCompleted(Action continuation)
                {
                    GDTaskPlayerLoopRunner.AddContinuation(timing, continuation);
                }
            }
        }

        /// <summary>
        /// Attaches the global <see cref="CancellationToken"/> to the given <see cref="SwitchToThreadPoolAwaitable"/>.
        /// </summary>
        public static SwitchToThreadPoolWithGlobalCancellationAwaitable AttachGlobalCancellation(this SwitchToThreadPoolAwaitable switchToThreadPoolAwaitable)
        {
            return new SwitchToThreadPoolWithGlobalCancellationAwaitable();
        }

        /// <summary>
        /// An context that, when disposed, will asynchronously yields to the thread pool.
        /// Equivalent to <see cref="SwitchToThreadPoolAwaitable"/> with the global cancellation token attached.
        /// </summary>
        public readonly struct SwitchToThreadPoolWithGlobalCancellationAwaitable
        {
            private readonly CancellationToken globalCancellationToken;

            /// <summary>
            /// Initializes the <see cref="SwitchToThreadPoolAwaitable"/>.
            /// </summary>
            public SwitchToThreadPoolWithGlobalCancellationAwaitable() {
                globalCancellationToken = GDTaskPlayerLoopRunner.GetGlobalCancellationToken();
            }

            /// <summary>
            /// Gets an awaiter used to await this <see cref="SwitchToThreadPoolWithGlobalCancellationAwaitable"/>.
            /// </summary>
            public Awaiter GetAwaiter() => new(globalCancellationToken);

            /// <summary>
            /// Provides an awaiter for awaiting a <see cref="SwitchToThreadPoolWithGlobalCancellationAwaitable"/>.
            /// </summary>
            public readonly struct Awaiter : ICriticalNotifyCompletion
            {
                private static readonly WaitCallback switchToCallback = Callback;

                private readonly CancellationToken globalCancellationToken;

                /// <summary>
                /// Initializes the <see cref="Awaiter"/>.
                /// </summary>
                internal Awaiter(CancellationToken globalCancellationToken) {
                    this.globalCancellationToken = globalCancellationToken;
                }

                /// <summary>
                /// Gets whether this <see cref="SwitchToThreadPoolWithGlobalCancellationAwaitable"/> has completed, always returns false.
                /// </summary>
                public bool IsCompleted => false;
            
                /// <summary>
                /// Do nothing
                /// </summary>
                public void GetResult() {
                    globalCancellationToken.ThrowIfCancellationRequested();
                }

                /// <summary>
                /// Schedules the continuation onto the <see cref="SwitchToThreadPoolWithGlobalCancellationAwaitable"/> associated with this <see cref="Awaiter"/>.
                /// </summary>
                public void OnCompleted(Action continuation)
                {
                    ThreadPool.QueueUserWorkItem(switchToCallback, continuation);
                }

                /// <summary>
                /// Schedules the continuation onto the <see cref="SwitchToThreadPoolWithGlobalCancellationAwaitable"/> associated with this <see cref="Awaiter"/>.
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
        /// Attaches the global <see cref="CancellationToken"/> to the given <see cref="SwitchToSynchronizationContextAwaitable"/>.
        /// </summary>
        public static SwitchToSynchronizationContextWithGlobalCancellationAwaitable AttachGlobalCancellation(this SwitchToSynchronizationContextAwaitable switchToSynchronizationContextAwaitable)
        {
            return new SwitchToSynchronizationContextWithGlobalCancellationAwaitable(switchToSynchronizationContextAwaitable.synchronizationContext, switchToSynchronizationContextAwaitable.cancellationToken);
        }

        /// <summary>
        /// An awaitable that asynchronously yields to the provided <see cref="SynchronizationContext"/> when awaited.
        /// Equivalent to <see cref="SwitchToSynchronizationContextAwaitable"/> with the global cancellation token attached.
        /// </summary>
        public readonly struct SwitchToSynchronizationContextWithGlobalCancellationAwaitable
        {
            private readonly SynchronizationContext synchronizationContext;
            private readonly CancellationToken cancellationToken;
            private readonly CancellationToken globalCancellationToken;

            internal SwitchToSynchronizationContextWithGlobalCancellationAwaitable(SynchronizationContext synchronizationContext, CancellationToken cancellationToken)
            {
                this.synchronizationContext = synchronizationContext;
                this.cancellationToken = cancellationToken;
                globalCancellationToken = GDTaskPlayerLoopRunner.GetGlobalCancellationToken();
            }

            /// <summary>
            /// Gets an awaiter used to await this <see cref="SwitchToSynchronizationContextWithGlobalCancellationAwaitable"/>.
            /// </summary>
            public Awaiter GetAwaiter() => new(synchronizationContext, cancellationToken, globalCancellationToken);

            /// <summary>
            /// Provides an awaiter for awaiting a <see cref="SwitchToSynchronizationContextWithGlobalCancellationAwaitable"/>.
            /// </summary>
            public readonly struct Awaiter : ICriticalNotifyCompletion
            {
                private static readonly SendOrPostCallback switchToCallback = Callback;

                private readonly SynchronizationContext synchronizationContext;
                private readonly CancellationToken cancellationToken;
                private readonly CancellationToken globalCancellationToken;

                /// <summary>
                /// Initializes the <see cref="Awaiter"/>.
                /// </summary>
                public Awaiter(SynchronizationContext synchronizationContext, CancellationToken cancellationToken, CancellationToken globalCancellationToken)
                {
                    this.synchronizationContext = synchronizationContext;
                    this.cancellationToken = cancellationToken;
                    this.globalCancellationToken = globalCancellationToken;
                }

                /// <summary>
                /// Gets whether this <see cref="SwitchToSynchronizationContextWithGlobalCancellationAwaitable"/> has completed, always returns false.
                /// </summary>
                public bool IsCompleted => false;

                /// <summary>
                /// Ends the awaiting on the completed <see cref="SwitchToSynchronizationContextWithGlobalCancellationAwaitable"/>.
                /// </summary>
                public void GetResult() {
                    cancellationToken.ThrowIfCancellationRequested();
                    globalCancellationToken.ThrowIfCancellationRequested();
                }

                /// <summary>
                /// Schedules the continuation onto the <see cref="SwitchToSynchronizationContextWithGlobalCancellationAwaitable"/> associated with this <see cref="Awaiter"/>.
                /// </summary>
                public void OnCompleted(Action continuation)
                {
                    synchronizationContext.Post(switchToCallback, continuation);
                }

                /// <summary>
                /// Schedules the continuation onto the <see cref="SwitchToSynchronizationContextWithGlobalCancellationAwaitable"/> associated with this <see cref="Awaiter"/>.
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
        /// Attaches the global <see cref="CancellationToken"/> to the given <see cref="ReturnToSynchronizationContext"/>.
        /// </summary>
        public static ReturnToSynchronizationWithGlobalCancellationContext AttachGlobalCancellation(this ReturnToSynchronizationContext returnToSynchronizationContext)
        {
            return new ReturnToSynchronizationWithGlobalCancellationContext(returnToSynchronizationContext.syncContext, returnToSynchronizationContext.dontPostWhenSameContext, returnToSynchronizationContext.cancellationToken);
        }

        /// <summary>
        /// An context that, when disposed, will asynchronously yields back to the previous <see cref="SynchronizationContext"/>.
        /// Equivalent to <see cref="ReturnToSynchronizationContext"/> with the global cancellation token attached.
        /// </summary>
        public readonly struct ReturnToSynchronizationWithGlobalCancellationContext
        {
            private readonly SynchronizationContext syncContext;
            private readonly bool dontPostWhenSameContext;
            private readonly CancellationToken cancellationToken;
            private readonly CancellationToken globalCancellationToken;

            internal ReturnToSynchronizationWithGlobalCancellationContext(SynchronizationContext syncContext, bool dontPostWhenSameContext, CancellationToken cancellationToken)
            {
                this.syncContext = syncContext;
                this.dontPostWhenSameContext = dontPostWhenSameContext;
                this.cancellationToken = cancellationToken;
                globalCancellationToken = GDTaskPlayerLoopRunner.GetGlobalCancellationToken();
            }

            /// <summary>
            /// Dispose this context and asynchronously yields back to the previous <see cref="SynchronizationContext"/>.
            /// </summary>
            public Awaiter DisposeAsync()
            {
                return new Awaiter(syncContext, dontPostWhenSameContext, cancellationToken, globalCancellationToken);
            }

            /// <summary>
            /// Provides an awaiter for awaiting a <see cref="ReturnToSynchronizationWithGlobalCancellationContext"/>.
            /// </summary>
            public readonly struct Awaiter : ICriticalNotifyCompletion
            {
                private static readonly SendOrPostCallback switchToCallback = Callback;

                private readonly SynchronizationContext synchronizationContext;
                private readonly bool dontPostWhenSameContext;
                private readonly CancellationToken cancellationToken;
                private readonly CancellationToken globalCancellationToken;

                /// <summary>
                /// Initializes the <see cref="Awaiter"/>.
                /// </summary>
                public Awaiter(SynchronizationContext synchronizationContext, bool dontPostWhenSameContext, CancellationToken cancellationToken, CancellationToken globalCancellationToken)
                {
                    this.synchronizationContext = synchronizationContext;
                    this.dontPostWhenSameContext = dontPostWhenSameContext;
                    this.cancellationToken = cancellationToken;
                    this.globalCancellationToken = globalCancellationToken;
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
                /// Ends the awaiting on the completed <see cref="ReturnToSynchronizationWithGlobalCancellationContext"/>.
                /// </summary>
                public void GetResult() {
                    cancellationToken.ThrowIfCancellationRequested();
                    globalCancellationToken.ThrowIfCancellationRequested();
                }

                /// <summary>
                /// Schedules the continuation onto the <see cref="ReturnToSynchronizationWithGlobalCancellationContext"/> associated with this <see cref="Awaiter"/>.
                /// </summary>
                public void OnCompleted(Action continuation)
                {
                    synchronizationContext.Post(switchToCallback, continuation);
                }

                /// <summary>
                /// Schedules the continuation onto the <see cref="ReturnToSynchronizationWithGlobalCancellationContext"/> associated with this <see cref="Awaiter"/>.
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
        /// Attaches the global <see cref="CancellationToken"/> to the given <see cref="IGDTaskAsyncEnumerable{T}"/>.
        /// </summary>
        public static IGDTaskAsyncEnumerable<T> AttachGlobalCancellation<T>(this IGDTaskAsyncEnumerable<T> enumerable)
#if NET9_0_OR_GREATER
        where T : allows ref struct
#endif
        {
            return new WithGlobalCancellationEnumerable<T>(enumerable);
        }

        /// <summary>
        /// Attaches the global <see cref="CancellationToken"/> to the given <see cref="IGDTaskAsyncEnumerator{T}"/>.
        /// </summary>
        public static IGDTaskAsyncEnumerator<T> AttachGlobalCancellation<T>(this IGDTaskAsyncEnumerator<T> enumerator)
#if NET9_0_OR_GREATER
        where T : allows ref struct
#endif
        {
            return new WithGlobalCancellationEnumerable<T>.Enumerator(enumerator, GDTaskPlayerLoopRunner.GetGlobalCancellationToken());
        }

        internal sealed class WithGlobalCancellationEnumerable<T> : IGDTaskAsyncEnumerable<T>
#if NET9_0_OR_GREATER
        where T : allows ref struct
#endif
        {
            private readonly IGDTaskAsyncEnumerable<T> source;
            private readonly CancellationToken globalCancellationToken;

            public WithGlobalCancellationEnumerable(IGDTaskAsyncEnumerable<T> source)
            {
                this.source = source;
                globalCancellationToken = GDTaskPlayerLoopRunner.GetGlobalCancellationToken();
            }

            public IGDTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                return new Enumerator(source.GetAsyncEnumerator(cancellationToken), globalCancellationToken);
            }

            public sealed class Enumerator : IGDTaskAsyncEnumerator<T>
            {
                private readonly IGDTaskAsyncEnumerator<T> source;
                private readonly CancellationToken globalCancellationToken;

                public Enumerator(IGDTaskAsyncEnumerator<T> source, CancellationToken globalCancellationToken)
                {
                    this.source = source;
                    this.globalCancellationToken = globalCancellationToken;
                }

                public T Current => source.Current;

                public GDTask<bool> MoveNextAsync()
                {
                    globalCancellationToken.ThrowIfCancellationRequested();

                    return source.MoveNextAsync();
                }

                public GDTask DisposeAsync()
                {
                    return source.DisposeAsync();
                }
            }
        }
    }
}

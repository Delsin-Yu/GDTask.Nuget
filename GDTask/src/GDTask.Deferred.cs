using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace GodotTask;

public partial struct GDTask
{
    /// <summary>
    /// Delay the execution until the end of the current frame (idle time).
    /// </summary>
    public static DeferredAwaitable Deferred() => new();

    /// <summary>
    /// Delay the execution until the end of the next dispatch cycle of the provided custom player loop.
    /// </summary>
    public static DeferredAwaitable Deferred(ICustomPlayerLoop customPlayerLoop) => new(GetDeferredScheduler(customPlayerLoop));

    /// <summary>
    /// Delay the execution until the end of the current frame (idle time), with specified <see cref="CancellationToken"/>.
    /// </summary>
    public static GDTask Deferred(CancellationToken cancellationToken)
    {
        return new GDTask(DeferredPromise.Create(GetDefaultDeferredScheduler(), cancellationToken, out var token), token);
    }

    /// <summary>
    /// Delay the execution until the end of the next dispatch cycle of the provided custom player loop, with specified <see cref="CancellationToken"/>.
    /// </summary>
    public static GDTask Deferred(ICustomPlayerLoop customPlayerLoop, CancellationToken cancellationToken)
    {
        return new GDTask(DeferredPromise.Create(GetDeferredScheduler(customPlayerLoop), cancellationToken, out var token), token);
    }

    private sealed class DeferredPromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<DeferredPromise>
    {
        private static TaskPool<DeferredPromise> pool;
        private DeferredPromise nextNode;
        public ref DeferredPromise NextNode => ref nextNode;

        static DeferredPromise()
        {
            TaskPool.RegisterSizeGetter(typeof(DeferredPromise), () => pool.Size);
        }

        private CancellationToken cancellationToken;
        private GDTaskCompletionSourceCore<object> core;

        private DeferredPromise()
        {
            
        }
        
        public static IGDTaskSource Create(IPlayerLoopScheduler scheduler, CancellationToken cancellationToken, out short token)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
            }
            
            if (!pool.TryPop(out var result))
            {
                result = new DeferredPromise();
            }
            
            result.cancellationToken = cancellationToken;
            
            TaskTracker.TrackActiveTask(result, 3);
            
            scheduler.AddDeferredAction(result);
            
            token = result.core.Version;
            return result;
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

        public GDTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public bool MoveNext()
        {
            if (cancellationToken.IsCancellationRequested)
            {
                core.TrySetCanceled(cancellationToken);
                return false;
            }

            core.TrySetResult(null);
            return false;
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
    /// An awaitable that when awaited, asynchronously continues the execution at the end of the current frame (idle time).
    /// </summary>
    public readonly struct DeferredAwaitable
    {
        private readonly IPlayerLoopScheduler scheduler;

        /// <summary>
        /// Initializes the <see cref="DeferredAwaitable"/>.
        /// </summary>
        public DeferredAwaitable() {
        }

        internal DeferredAwaitable(IPlayerLoopScheduler scheduler)
        {
            this.scheduler = scheduler;
        }

        /// <summary>
        /// Gets an awaiter used to await this <see cref="DeferredAwaitable"/>.
        /// </summary>
        public Awaiter GetAwaiter() => new Awaiter(scheduler ?? GetDefaultDeferredScheduler());

        /// <summary>
        /// Creates a <see cref="GDTask"/> that represents this <see cref="DeferredAwaitable"/>.
        /// </summary>
        public GDTask ToGDTask()
        {
            return new GDTask(DeferredPromise.Create(scheduler ?? GetDefaultDeferredScheduler(), CancellationToken.None, out var token), token);
        }
      
        /// <summary>
        /// Provides an awaiter for awaiting a <see cref="DeferredAwaitable"/>.
        /// </summary>
        public readonly struct Awaiter : ICriticalNotifyCompletion
        {
            private readonly IPlayerLoopScheduler scheduler;

            /// <summary>
            /// Initializes the <see cref="Awaiter"/>.
            /// </summary>
            public Awaiter() {
            }

            internal Awaiter(IPlayerLoopScheduler scheduler)
            {
                this.scheduler = scheduler;
            }

            /// <summary>
            /// Ends the awaiting on the completed <see cref="DeferredAwaitable"/>.
            /// </summary>
            public void GetResult() {
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
                scheduler.AddDeferredContinuation(continuation);
            }

            /// <summary>
            /// Schedules the continuation onto the <see cref="YieldAwaitable"/> associated with this <see cref="Awaiter"/>.
            /// </summary>
            public void UnsafeOnCompleted(Action continuation)
            {
                scheduler.AddDeferredContinuation(continuation);
            }
        }
    }
}
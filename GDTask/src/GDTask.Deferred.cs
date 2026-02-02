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
    /// Delay the execution until the end of the current frame (idle time), with specified <see cref="CancellationToken"/>.
    /// </summary>
    public static GDTask Deferred(CancellationToken cancellationToken)
    {
        return new GDTask(DeferredPromise.Create(cancellationToken, out var token), token);
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
        
        public static IGDTaskSource Create(CancellationToken cancellationToken, out short token)
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
            
            GDTaskPlayerLoopRunner.AddDeferredAction(result);
            
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
        /// <summary>
        /// Initializes the <see cref="DeferredAwaitable"/>.
        /// </summary>
        public DeferredAwaitable() {
        }

        /// <summary>
        /// Gets an awaiter used to await this <see cref="DeferredAwaitable"/>.
        /// </summary>
        public Awaiter GetAwaiter() => new Awaiter();

        /// <summary>
        /// Creates a <see cref="GDTask"/> that represents this <see cref="DeferredAwaitable"/>.
        /// </summary>
        public GDTask ToGDTask()
        {
            return GDTask.Deferred(CancellationToken.None);
        }
      
        /// <summary>
        /// Provides an awaiter for awaiting a <see cref="DeferredAwaitable"/>.
        /// </summary>
        public readonly struct Awaiter : ICriticalNotifyCompletion
        {
            /// <summary>
            /// Initializes the <see cref="Awaiter"/>.
            /// </summary>
            public Awaiter() {
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
}
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
    /// Delay the execution until the end of the current frame (idle time), with specified <see cref="CancellationToken" />.
    /// </summary>
    public static GDTask Deferred(CancellationToken cancellationToken) => new(DeferredPromise.Create(cancellationToken, out var token), token);

    private sealed class DeferredPromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<DeferredPromise>
    {
        private static TaskPool<DeferredPromise> Pool;

        private CancellationToken _cancellationToken;
        private GDTaskCompletionSourceCore<object> _core;
        private DeferredPromise _nextNode;

        private DeferredPromise() { }

        public void GetResult(short token)
        {
            try { _core.GetResult(token); }
            finally { TryReturn(); }
        }

        public GDTaskStatus GetStatus(short token) => _core.GetStatus(token);

        public GDTaskStatus UnsafeGetStatus() => _core.UnsafeGetStatus();

        public void OnCompleted(Action<object> continuation, object state, short token) => _core.OnCompleted(continuation, state, token);

        public bool MoveNext(double deltaTime)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _core.TrySetCanceled(_cancellationToken);
                return false;
            }

            _core.TrySetResult(null);
            return false;
        }

        public ref DeferredPromise NextNode => ref _nextNode;

        public static IGDTaskSource Create(CancellationToken cancellationToken, out short token)
        {
            if (cancellationToken.IsCancellationRequested) return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);

            if (!Pool.TryPop(out var result)) result = new();

            result._cancellationToken = cancellationToken;

            TaskTracker.TrackActiveTask(result, 3);

            GDTaskScheduler.AddAction(PlayerLoopTiming.DeferredProcess, result);

            token = result._core.Version;
            return result;
        }

        private bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            _core.Reset();
            _cancellationToken = default;
            return Pool.TryPush(this);
        }
    }

    /// <summary>
    /// An awaitable that when awaited, asynchronously continues the execution at the end of the current frame (idle time).
    /// </summary>
    public readonly struct DeferredAwaitable
    {
        /// <summary>
        /// Initializes the <see cref="DeferredAwaitable" />.
        /// </summary>
        public DeferredAwaitable() { }

        /// <summary>
        /// Gets an awaiter used to await this <see cref="DeferredAwaitable" />.
        /// </summary>
        public Awaiter GetAwaiter() => new();

        /// <summary>
        /// Creates a <see cref="GDTask" /> that represents this <see cref="DeferredAwaitable" />.
        /// </summary>
        public GDTask ToGDTask() => Deferred(CancellationToken.None);

        /// <summary>
        /// Provides an awaiter for awaiting a <see cref="DeferredAwaitable" />.
        /// </summary>
        public readonly struct Awaiter : ICriticalNotifyCompletion
        {
            /// <summary>
            /// Initializes the <see cref="Awaiter" />.
            /// </summary>
            public Awaiter() { }

            /// <summary>
            /// Ends the awaiting on the completed <see cref="DeferredAwaitable" />.
            /// </summary>
            public void GetResult() { }

            /// <summary>
            /// Gets whether this <see cref="YieldAwaitable">Task</see> has completed, always returns false.
            /// </summary>
            public bool IsCompleted => false;

            /// <summary>
            /// Schedules the continuation onto the <see cref="YieldAwaitable" /> associated with this <see cref="Awaiter" />.
            /// </summary>
            public void OnCompleted(Action continuation) => GDTaskScheduler.AddContinuation(PlayerLoopTiming.DeferredProcess, continuation);

            /// <summary>
            /// Schedules the continuation onto the <see cref="YieldAwaitable" /> associated with this <see cref="Awaiter" />.
            /// </summary>
            public void UnsafeOnCompleted(Action continuation) => GDTaskScheduler.AddContinuation(PlayerLoopTiming.DeferredProcess, continuation);
        }
    }
}
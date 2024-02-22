#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace GodotTask.CompilerServices
{
    internal interface IStateMachineRunner
    {
        Action MoveNext { get; }
        void Return();
    }

    internal interface IStateMachineRunnerPromise : IGDTaskSource
    {
        Action MoveNext { get; }
        GDTask Task { get; }
        void SetResult();
        void SetException(Exception exception);
    }

    internal interface IStateMachineRunnerPromise<T> : IGDTaskSource<T>
    {
        Action MoveNext { get; }
        GDTask<T> Task { get; }
        void SetResult(T result);
        void SetException(Exception exception);
    }

    internal sealed class AsyncGDTaskVoid<TStateMachine> : IStateMachineRunner, ITaskPoolNode<AsyncGDTaskVoid<TStateMachine>>, IGDTaskSource
        where TStateMachine : IAsyncStateMachine
    {
        private static TaskPool<AsyncGDTaskVoid<TStateMachine>> pool;

        private TStateMachine stateMachine;

        public Action MoveNext { get; }

        public AsyncGDTaskVoid()
        {
            MoveNext = Run;
        }

        public static void SetStateMachine(ref TStateMachine stateMachine, ref IStateMachineRunner runnerFieldRef)
        {
            if (!pool.TryPop(out var result))
            {
                result = new AsyncGDTaskVoid<TStateMachine>();
            }
            TaskTracker.TrackActiveTask(result, 3);

            runnerFieldRef = result; // set runner before copied.
            result.stateMachine = stateMachine; // copy struct StateMachine(in release build).
        }

        static AsyncGDTaskVoid()
        {
            TaskPool.RegisterSizeGetter(typeof(AsyncGDTaskVoid<TStateMachine>), () => pool.Size);
        }

        private AsyncGDTaskVoid<TStateMachine> nextNode;
        public ref AsyncGDTaskVoid<TStateMachine> NextNode => ref nextNode;

        public void Return()
        {
            TaskTracker.RemoveTracking(this);
            stateMachine = default;
            pool.TryPush(this);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Run()
        {
            stateMachine.MoveNext();
        }

        // dummy interface implementation for TaskTracker.

        GDTaskStatus IGDTaskSource.GetStatus(short token)
        {
            return GDTaskStatus.Pending;
        }

        GDTaskStatus IGDTaskSource.UnsafeGetStatus()
        {
            return GDTaskStatus.Pending;
        }

        void IGDTaskSource.OnCompleted(Action<object> continuation, object state, short token)
        {
        }

        void IGDTaskSource.GetResult(short token)
        {
        }
    }

    internal sealed class AsyncGDTask<TStateMachine> : IStateMachineRunnerPromise, ITaskPoolNode<AsyncGDTask<TStateMachine>>
        where TStateMachine : IAsyncStateMachine
    {
        private static TaskPool<AsyncGDTask<TStateMachine>> pool;

        public Action MoveNext { get; }

        private TStateMachine stateMachine;
        private GDTaskCompletionSourceCore<AsyncUnit> core;

        private AsyncGDTask()
        {
            MoveNext = Run;
        }

        public static void SetStateMachine(ref TStateMachine stateMachine, ref IStateMachineRunnerPromise runnerPromiseFieldRef)
        {
            if (!pool.TryPop(out var result))
            {
                result = new AsyncGDTask<TStateMachine>();
            }
            TaskTracker.TrackActiveTask(result, 3);

            runnerPromiseFieldRef = result; // set runner before copied.
            result.stateMachine = stateMachine; // copy struct StateMachine(in release build).
        }

        private AsyncGDTask<TStateMachine> nextNode;
        public ref AsyncGDTask<TStateMachine> NextNode => ref nextNode;

        static AsyncGDTask()
        {
            TaskPool.RegisterSizeGetter(typeof(AsyncGDTask<TStateMachine>), () => pool.Size);
        }

        private void Return()
        {
            TaskTracker.RemoveTracking(this);
            core.Reset();
            stateMachine = default;
            pool.TryPush(this);
        }

        private bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            core.Reset();
            stateMachine = default;
            return pool.TryPush(this);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Run()
        {
            stateMachine.MoveNext();
        }

        public GDTask Task
        {
            [DebuggerHidden]
            get => new(this, core.Version);
        }

        [DebuggerHidden]
        public void SetResult()
        {
            core.TrySetResult(AsyncUnit.Default);
        }

        [DebuggerHidden]
        public void SetException(Exception exception)
        {
            core.TrySetException(exception);
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
    }

    internal sealed class AsyncGDTask<TStateMachine, T> : IStateMachineRunnerPromise<T>, ITaskPoolNode<AsyncGDTask<TStateMachine, T>>
        where TStateMachine : IAsyncStateMachine
    {
        private static TaskPool<AsyncGDTask<TStateMachine, T>> pool;

        public Action MoveNext { get; }

        private TStateMachine stateMachine;
        private GDTaskCompletionSourceCore<T> core;

        private AsyncGDTask()
        {
            MoveNext = Run;
        }

        public static void SetStateMachine(ref TStateMachine stateMachine, ref IStateMachineRunnerPromise<T> runnerPromiseFieldRef)
        {
            if (!pool.TryPop(out var result))
            {
                result = new AsyncGDTask<TStateMachine, T>();
            }
            TaskTracker.TrackActiveTask(result, 3);

            runnerPromiseFieldRef = result; // set runner before copied.
            result.stateMachine = stateMachine; // copy struct StateMachine(in release build).
        }

        private AsyncGDTask<TStateMachine, T> nextNode;
        public ref AsyncGDTask<TStateMachine, T> NextNode => ref nextNode;

        static AsyncGDTask()
        {
            TaskPool.RegisterSizeGetter(typeof(AsyncGDTask<TStateMachine, T>), () => pool.Size);
        }

        private void Return()
        {
            TaskTracker.RemoveTracking(this);
            core.Reset();
            stateMachine = default;
            pool.TryPush(this);
        }

        private bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            core.Reset();
            stateMachine = default;
            return pool.TryPush(this);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Run()
        {
            stateMachine.MoveNext();
        }

        public GDTask<T> Task
        {
            [DebuggerHidden]
            get => new(this, core.Version);
        }

        [DebuggerHidden]
        public void SetResult(T result)
        {
            core.TrySetResult(result);
        }

        [DebuggerHidden]
        public void SetException(Exception exception)
        {
            core.TrySetException(exception);
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
    }
}


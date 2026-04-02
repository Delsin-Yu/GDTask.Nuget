#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

﻿
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace GodotTask.CompilerServices;

interface IStateMachineRunner
{
    Action MoveNext { get; }
    void Return();
}

interface IStateMachineRunnerPromise : IGDTaskSource
{
    Action MoveNext { get; }
    GDTask Task { get; }
    void SetResult();
    void SetException(Exception exception);
}

interface IStateMachineRunnerPromise<T> : IGDTaskSource<T>
{
    Action MoveNext { get; }
    GDTask<T> Task { get; }
    void SetResult(T result);
    void SetException(Exception exception);
}

sealed class AsyncGDTaskVoid<TStateMachine> : IStateMachineRunner, ITaskPoolNode<AsyncGDTaskVoid<TStateMachine>>, IGDTaskSource
    where TStateMachine : IAsyncStateMachine
{
    private static TaskPool<AsyncGDTaskVoid<TStateMachine>> Pool;

    private AsyncGDTaskVoid<TStateMachine> _nextNode;

    private TStateMachine _stateMachine;

    private AsyncGDTaskVoid()
    {
        MoveNext = Run;
    }

    // dummy interface implementation for TaskTracker.

    GDTaskStatus IGDTaskSource.GetStatus(short token) => GDTaskStatus.Pending;

    GDTaskStatus IGDTaskSource.UnsafeGetStatus() => GDTaskStatus.Pending;

    void IGDTaskSource.OnCompleted(Action<object> continuation, object state, short token) { }

    void IGDTaskSource.GetResult(short token) { }

    public Action MoveNext { get; }

    public void Return()
    {
        TaskTracker.RemoveTracking(this);
        _stateMachine = default;
        Pool.TryPush(this);
    }

    public ref AsyncGDTaskVoid<TStateMachine> NextNode => ref _nextNode;

    public static void SetStateMachine(ref TStateMachine stateMachine, ref IStateMachineRunner runnerFieldRef)
    {
        if (!Pool.TryPop(out var result)) result = new();
        TaskTracker.TrackActiveTask(result, 3);

        runnerFieldRef = result; // set runner before copied.
        result._stateMachine = stateMachine; // copy struct StateMachine(in release build).
    }

    [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Run() => _stateMachine.MoveNext();
}

sealed class AsyncGDTask<TStateMachine> : IStateMachineRunnerPromise, ITaskPoolNode<AsyncGDTask<TStateMachine>>
    where TStateMachine : IAsyncStateMachine
{
    private static TaskPool<AsyncGDTask<TStateMachine>> Pool;
    private GDTaskCompletionSourceCore<AsyncUnit> _core;

    private AsyncGDTask<TStateMachine> _nextNode;

    private TStateMachine _stateMachine;

    private AsyncGDTask()
    {
        MoveNext = Run;
    }

    public Action MoveNext { get; }

    public GDTask Task
    {
        [DebuggerHidden]
        get => new(this, _core.Version);
    }

    [DebuggerHidden]
    public void SetResult() => _core.TrySetResult(AsyncUnit.Default);

    [DebuggerHidden]
    public void SetException(Exception exception) => _core.TrySetException(exception);

    [DebuggerHidden]
    public void GetResult(short token)
    {
        try { _core.GetResult(token); }
        finally { TryReturn(); }
    }

    [DebuggerHidden]
    public GDTaskStatus GetStatus(short token) => _core.GetStatus(token);

    [DebuggerHidden]
    public GDTaskStatus UnsafeGetStatus() => _core.UnsafeGetStatus();

    [DebuggerHidden]
    public void OnCompleted(Action<object> continuation, object state, short token) => _core.OnCompleted(continuation, state, token);

    public ref AsyncGDTask<TStateMachine> NextNode => ref _nextNode;

    public static void SetStateMachine(ref TStateMachine stateMachine, ref IStateMachineRunnerPromise runnerPromiseFieldRef)
    {
        if (!Pool.TryPop(out var result)) result = new();
        TaskTracker.TrackActiveTask(result, 3);

        runnerPromiseFieldRef = result; // set runner before copied.
        result._stateMachine = stateMachine; // copy struct StateMachine(in release build).
    }

    private void Return()
    {
        TaskTracker.RemoveTracking(this);
        _core.Reset();
        _stateMachine = default;
        Pool.TryPush(this);
    }

    private bool TryReturn()
    {
        TaskTracker.RemoveTracking(this);
        _core.Reset();
        _stateMachine = default;
        return Pool.TryPush(this);
    }

    [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Run() => _stateMachine.MoveNext();
}

sealed class AsyncGDTask<TStateMachine, T> : IStateMachineRunnerPromise<T>, ITaskPoolNode<AsyncGDTask<TStateMachine, T>>
    where TStateMachine : IAsyncStateMachine
{
    private static TaskPool<AsyncGDTask<TStateMachine, T>> Pool;
    private GDTaskCompletionSourceCore<T> _core;

    private AsyncGDTask<TStateMachine, T> _nextNode;

    private TStateMachine _stateMachine;

    private AsyncGDTask()
    {
        MoveNext = Run;
    }

    public Action MoveNext { get; }

    public GDTask<T> Task
    {
        [DebuggerHidden]
        get => new(this, _core.Version);
    }

    [DebuggerHidden]
    public void SetResult(T result) => _core.TrySetResult(result);

    [DebuggerHidden]
    public void SetException(Exception exception) => _core.TrySetException(exception);

    [DebuggerHidden]
    public T GetResult(short token)
    {
        try { return _core.GetResult(token); }
        finally { TryReturn(); }
    }

    [DebuggerHidden]
    void IGDTaskSource.GetResult(short token) => GetResult(token);

    [DebuggerHidden]
    public GDTaskStatus GetStatus(short token) => _core.GetStatus(token);

    [DebuggerHidden]
    public GDTaskStatus UnsafeGetStatus() => _core.UnsafeGetStatus();

    [DebuggerHidden]
    public void OnCompleted(Action<object> continuation, object state, short token) => _core.OnCompleted(continuation, state, token);

    public ref AsyncGDTask<TStateMachine, T> NextNode => ref _nextNode;

    public static void SetStateMachine(ref TStateMachine stateMachine, ref IStateMachineRunnerPromise<T> runnerPromiseFieldRef)
    {
        if (!Pool.TryPop(out var result)) result = new();
        TaskTracker.TrackActiveTask(result, 3);

        runnerPromiseFieldRef = result; // set runner before copied.
        result._stateMachine = stateMachine; // copy struct StateMachine(in release build).
    }

    private void Return()
    {
        TaskTracker.RemoveTracking(this);
        _core.Reset();
        _stateMachine = default;
        Pool.TryPush(this);
    }

    private bool TryReturn()
    {
        TaskTracker.RemoveTracking(this);
        _core.Reset();
        _stateMachine = default;
        return Pool.TryPush(this);
    }

    [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Run() => _stateMachine.MoveNext();
}
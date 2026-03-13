using System;
using System.Collections.Concurrent;
using Godot;
using GodotTask.Internal;
using Environment = System.Environment;

namespace GodotTask;

internal static class GDTaskScheduler
{
    public static bool IsMainThread => Environment.CurrentManagedThreadId == MainThreadId;

    public static int MainThreadId
    {
        get
        {
            if (field == -1) 
                Dispatcher.SynchronizationContext.Send(_ => MainThreadId = Environment.CurrentManagedThreadId, null);
            return field;
        }
        private set;
    } = -1;

    internal static IPlayerLoop GetPlayerLoop(PlayerLoopTiming timing)
    {
        return timing switch
        {
            PlayerLoopTiming.Process => PlayerLoopRunnerProvider.Process,
            PlayerLoopTiming.PhysicsProcess => PlayerLoopRunnerProvider.PhysicsProcess,
            PlayerLoopTiming.IsolatedProcess => PlayerLoopRunnerProvider.IsolatedProcess,
            PlayerLoopTiming.IsolatedPhysicsProcess => PlayerLoopRunnerProvider.IsolatedPhysicsProcess,
            PlayerLoopTiming.DeferredProcess => PlayerLoopRunnerProvider.Deferred,
            _ => throw new ArgumentOutOfRangeException(nameof(timing), timing, null)
        };
    }

    internal static bool UsesEngineFrameBoundary(IPlayerLoop playerLoop)
    {
        // Only the built-in Godot-backed loops have a meaningful Engine frame boundary.
        // Custom IPlayerLoop implementations may drive ticks manually and should use every tick they receive.
        return ReferenceEquals(playerLoop, PlayerLoopRunnerProvider.Process)
            || ReferenceEquals(playerLoop, PlayerLoopRunnerProvider.PhysicsProcess)
            || ReferenceEquals(playerLoop, PlayerLoopRunnerProvider.IsolatedProcess)
            || ReferenceEquals(playerLoop, PlayerLoopRunnerProvider.IsolatedPhysicsProcess)
            || ReferenceEquals(playerLoop, PlayerLoopRunnerProvider.Deferred);
    }
    
    private static readonly ConcurrentDictionary<IPlayerLoop, PlayerLoopRunner> Runners = [];
    private static readonly ConcurrentDictionary<IPlayerLoop, ContinuationQueue> Yielders = [];
    
    public static void AddAction(PlayerLoopTiming timing, IPlayerLoopItem action) => 
        AddAction(GetPlayerLoop(timing), action);

    public static void AddAction(IPlayerLoop runner, IPlayerLoopItem action)
    {
        Runners.GetOrAdd(runner, state =>
        {
            var associatedRunner = new PlayerLoopRunner();
            state.OnProcess += associatedRunner.Run;
            state.OnPredelete += () =>
            {
                associatedRunner.Clear();
                Runners.TryRemove(state, out _);
            };
            return associatedRunner;
        }).AddAction(action);
    }
    
    public static void AddContinuation(PlayerLoopTiming timing, Action continuation) => 
        AddContinuation(GetPlayerLoop(timing), continuation);

    public static void AddContinuation(IPlayerLoop runner, Action continuation)
    {
        Yielders.GetOrAdd(runner, state =>
        {
            var associatedQueue = new ContinuationQueue();
            state.OnProcess += associatedQueue.Run;
            state.OnPredelete += () =>
            {
                associatedQueue.Clear();
                Yielders.TryRemove(state, out _);
            };
            return associatedQueue;
        }).Enqueue(continuation);
    }

}
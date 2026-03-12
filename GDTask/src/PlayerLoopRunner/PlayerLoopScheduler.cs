using System;
using Godot;
using GodotTask.Internal;

namespace GodotTask;

internal abstract class PlayerLoopSchedulerBase : IPlayerLoopScheduler
{
    private readonly ContinuationQueue[] yielders = [new(), new(), new(), new()];
    private readonly PlayerLoopRunner[] runners = [new(), new(), new(), new()];
    private readonly ContinuationQueue deferredYielder = new();
    private readonly PlayerLoopRunner deferredRunner = new();

    private int mainThreadId;
    private double processDeltaTime;
    private double physicsDeltaTime;
    private ulong processFrameCount;
    private ulong physicsFrameCount;

    public int MainThreadId => mainThreadId;
    public bool IsMainThread => mainThreadId != 0 && System.Environment.CurrentManagedThreadId == mainThreadId;
    public double DeltaTime => processDeltaTime;
    public double PhysicsDeltaTime => physicsDeltaTime;

    public virtual double GetDeltaTime(PlayerLoopTiming timing)
    {
        return timing switch
        {
            PlayerLoopTiming.Process or PlayerLoopTiming.IsolatedProcess => processDeltaTime,
            PlayerLoopTiming.PhysicsProcess or PlayerLoopTiming.IsolatedPhysicsProcess => physicsDeltaTime,
            _ => throw new ArgumentOutOfRangeException(nameof(timing), timing, null)
        };
    }

    public virtual ulong GetFrameCount(PlayerLoopTiming timing)
    {
        return timing switch
        {
            PlayerLoopTiming.Process or PlayerLoopTiming.IsolatedProcess => processFrameCount,
            PlayerLoopTiming.PhysicsProcess or PlayerLoopTiming.IsolatedPhysicsProcess => physicsFrameCount,
            _ => throw new ArgumentOutOfRangeException(nameof(timing), timing, null)
        };
    }

    public virtual void AddAction(PlayerLoopTiming timing, IPlayerLoopItem action)
    {
        var runner = GetRunner(timing);
        runner.AddAction(action);
    }

    public virtual void AddContinuation(PlayerLoopTiming timing, Action continuation)
    {
        var queue = GetYielder(timing);
        queue.Enqueue(continuation);
    }

    public virtual void AddDeferredAction(IPlayerLoopItem action)
    {
        deferredRunner.AddAction(action);
    }

    public virtual void AddDeferredContinuation(Action continuation)
    {
        deferredYielder.Enqueue(continuation);
    }

    public int Clear()
    {
        var rest = deferredYielder.Clear() + deferredRunner.Clear();
        for (var i = 0; i < yielders.Length; i++)
        {
            rest += yielders[i].Clear();
            rest += runners[i].Clear();
        }

        return rest;
    }

    internal bool HasPendingWork()
    {
        if (deferredYielder.HasItems || deferredRunner.HasItems)
        {
            return true;
        }

        for (var i = 0; i < yielders.Length; i++)
        {
            var timing = (PlayerLoopTiming)i;
            if (!SupportsTiming(timing))
            {
                continue;
            }

            if (yielders[i].HasItems || runners[i].HasItems)
            {
                return true;
            }
        }

        return false;
    }

    protected void SetMainThreadId(int threadId)
    {
        mainThreadId = threadId;
    }

    protected virtual bool SupportsTiming(PlayerLoopTiming timing)
    {
        return timing is PlayerLoopTiming.Process or PlayerLoopTiming.PhysicsProcess or PlayerLoopTiming.IsolatedProcess or PlayerLoopTiming.IsolatedPhysicsProcess;
    }

    protected void DispatchLoop(PlayerLoopTiming timing, double delta)
    {
        SetMainThreadId(System.Environment.CurrentManagedThreadId);

        switch (timing)
        {
            case PlayerLoopTiming.Process:
            case PlayerLoopTiming.IsolatedProcess:
                processDeltaTime = delta;
                processFrameCount++;
                break;
            case PlayerLoopTiming.PhysicsProcess:
            case PlayerLoopTiming.IsolatedPhysicsProcess:
                physicsDeltaTime = delta;
                physicsFrameCount++;
                break;
            default:
                GDTaskPlayerLoopRunner.ThrowInvalidLoopTiming(timing);
                break;
        }

        GetYielder(timing).Run();
        GetRunner(timing).Run();
    }

    protected void DispatchDeferred()
    {
        deferredYielder.Run();
        deferredRunner.Run();
    }

    private ContinuationQueue GetYielder(PlayerLoopTiming timing)
    {
        ValidateTiming(timing);
        return yielders[(int)timing];
    }

    private PlayerLoopRunner GetRunner(PlayerLoopTiming timing)
    {
        ValidateTiming(timing);
        return runners[(int)timing];
    }

    private void ValidateTiming(PlayerLoopTiming timing)
    {
        if (!SupportsTiming(timing))
        {
            GDTaskPlayerLoopRunner.ThrowInvalidLoopTiming(timing);
        }
    }
}

internal sealed class DefaultPlayerLoopScheduler(int mainThreadId) : PlayerLoopSchedulerBase
{
    public override ulong GetFrameCount(PlayerLoopTiming timing)
    {
        return timing switch
        {
            PlayerLoopTiming.Process or PlayerLoopTiming.IsolatedProcess => Engine.GetProcessFrames(),
            PlayerLoopTiming.PhysicsProcess or PlayerLoopTiming.IsolatedPhysicsProcess => Engine.GetPhysicsFrames(),
            _ => throw new ArgumentOutOfRangeException(nameof(timing), timing, null)
        };
    }

    public void Initialize()
    {
        SetMainThreadId(mainThreadId);
    }

    public void DispatchProcess(double delta) => DispatchLoop(PlayerLoopTiming.Process, delta);

    public void DispatchPhysicsProcess(double delta) => DispatchLoop(PlayerLoopTiming.PhysicsProcess, delta);

    public void DispatchIsolatedProcess(double delta) => DispatchLoop(PlayerLoopTiming.IsolatedProcess, delta);

    public void DispatchIsolatedPhysicsProcess(double delta) => DispatchLoop(PlayerLoopTiming.IsolatedPhysicsProcess, delta);

    public void RunDeferred() => DispatchDeferred();
}

internal sealed class CustomPlayerLoopScheduler(ICustomPlayerLoop customPlayerLoop) : PlayerLoopSchedulerBase, IDisposable
{
    private readonly object subscriptionGate = new();
    private bool isSubscribed;

    protected override bool SupportsTiming(PlayerLoopTiming timing)
    {
        return timing is PlayerLoopTiming.Process or PlayerLoopTiming.PhysicsProcess;
    }

    public override void AddAction(PlayerLoopTiming timing, IPlayerLoopItem action)
    {
        EnsureSubscribed();
        base.AddAction(timing, action);
    }

    public override void AddContinuation(PlayerLoopTiming timing, Action continuation)
    {
        EnsureSubscribed();
        base.AddContinuation(timing, continuation);
    }

    public override void AddDeferredAction(IPlayerLoopItem action)
    {
        EnsureSubscribed();
        base.AddDeferredAction(action);
    }

    public override void AddDeferredContinuation(Action continuation)
    {
        EnsureSubscribed();
        base.AddDeferredContinuation(continuation);
    }

    public void Dispose()
    {
        lock (subscriptionGate)
        {
            if (!isSubscribed)
            {
                return;
            }

            customPlayerLoop.Process -= OnProcess;
            customPlayerLoop.PhysicsProcess -= OnPhysicsProcess;
            isSubscribed = false;
        }

        Clear();
    }

    private void EnsureSubscribed()
    {
        lock (subscriptionGate)
        {
            if (isSubscribed)
            {
                return;
            }

            customPlayerLoop.Process += OnProcess;
            customPlayerLoop.PhysicsProcess += OnPhysicsProcess;
            isSubscribed = true;
        }
    }

    private void OnProcess(double delta)
    {
        DispatchLoop(PlayerLoopTiming.Process, delta);
        DispatchDeferred();
        TryUnsubscribe();
    }

    private void OnPhysicsProcess(double delta)
    {
        DispatchLoop(PlayerLoopTiming.PhysicsProcess, delta);
        DispatchDeferred();
        TryUnsubscribe();
    }

    private void TryUnsubscribe()
    {
        lock (subscriptionGate)
        {
            if (!isSubscribed || HasPendingWork())
            {
                return;
            }

            customPlayerLoop.Process -= OnProcess;
            customPlayerLoop.PhysicsProcess -= OnPhysicsProcess;
            isSubscribed = false;
        }
    }
}
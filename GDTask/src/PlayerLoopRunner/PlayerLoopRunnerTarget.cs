using System;

namespace GodotTask;

internal readonly struct PlayerLoopRunnerTarget
{
    public PlayerLoopRunnerTarget(IPlayerLoopScheduler scheduler, PlayerLoopTiming timing)
    {
        Scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        Timing = timing;
    }

    public IPlayerLoopScheduler Scheduler { get; }
    public PlayerLoopTiming Timing { get; }

    public static PlayerLoopRunnerTarget Default(PlayerLoopTiming timing)
    {
        return new PlayerLoopRunnerTarget(GDTaskPlayerLoopRunner.DefaultScheduler, timing);
    }

    public static PlayerLoopRunnerTarget Custom(ICustomPlayerLoop customPlayerLoop, PlayerLoopTiming timing)
    {
        return new PlayerLoopRunnerTarget(GDTaskPlayerLoopRunner.GetScheduler(customPlayerLoop), timing);
    }

    public int MainThreadId => Scheduler.MainThreadId;
    public bool IsMainThread => Scheduler.IsMainThread;
    public double DeltaTime => Scheduler.GetDeltaTime(Timing);
    public ulong FrameCount => Scheduler.GetFrameCount(Timing);

    public void AddAction(IPlayerLoopItem action)
    {
        Scheduler.AddAction(Timing, action);
    }

    public void AddContinuation(Action continuation)
    {
        Scheduler.AddContinuation(Timing, continuation);
    }
}
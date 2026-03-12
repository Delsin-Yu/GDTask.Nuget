namespace GodotTask;

public partial struct GDTask
{
    internal static PlayerLoopRunnerTarget CreateTarget(PlayerLoopTiming timing)
    {
        return PlayerLoopRunnerTarget.Default(timing);
    }

    internal static PlayerLoopRunnerTarget CreateTarget(ICustomPlayerLoop customPlayerLoop, PlayerLoopTiming timing)
    {
        return PlayerLoopRunnerTarget.Custom(customPlayerLoop, timing);
    }

    internal static IPlayerLoopScheduler GetDefaultDeferredScheduler()
    {
        return GDTaskPlayerLoopRunner.DefaultScheduler;
    }

    internal static IPlayerLoopScheduler GetDeferredScheduler(ICustomPlayerLoop customPlayerLoop)
    {
        return GDTaskPlayerLoopRunner.GetScheduler(customPlayerLoop);
    }
}
using System;
using System.Threading;
using System.Threading.Tasks;
using GdUnit4;

namespace GodotTask.Tests;

[TestSuite]
public class GDTaskTest_CustomPlayerLoop
{
    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Yield_CustomPlayerLoops_Are_Isolated()
    {
        var firstLoop = new ManualCustomPlayerLoop();
        var secondLoop = new ManualCustomPlayerLoop();

        var firstTask = GDTask.Yield(firstLoop, CancellationToken.None);
        var secondTask = GDTask.Yield(secondLoop, CancellationToken.None);

        firstLoop.Process(0.016);

        Assertions.AssertThat(firstTask.Status).IsEqual(GDTaskStatus.Succeeded);
        Assertions.AssertThat(secondTask.Status).IsEqual(GDTaskStatus.Pending);

        await firstTask;

        secondLoop.Process(0.016);
        await secondTask;
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Delay_CustomPhysicsProcess_Uses_PhysicsTicks()
    {
        var customLoop = new ManualCustomPlayerLoop();
        var delayTask = GDTask.Delay(TimeSpan.FromMilliseconds(30), DelayType.DeltaTime, customLoop, PlayerLoopTiming.PhysicsProcess);

        customLoop.Process(1.0);
        Assertions.AssertThat(delayTask.Status).IsEqual(GDTaskStatus.Pending);

        customLoop.PhysicsProcess(0.01);
        Assertions.AssertThat(delayTask.Status).IsEqual(GDTaskStatus.Pending);

        customLoop.PhysicsProcess(0.02);
        await delayTask;
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_WaitUntil_CustomPlayerLoop_Completes_WhenPredicateTurnsTrue()
    {
        var customLoop = new ManualCustomPlayerLoop();
        var ready = false;
        var waitTask = GDTask.WaitUntil(() => ready, customLoop);

        customLoop.Process(0.016);
        Assertions.AssertThat(waitTask.Status).IsEqual(GDTaskStatus.Pending);

        ready = true;
        customLoop.Process(0.016);
        await waitTask;
    }

    private sealed class ManualCustomPlayerLoop : ICustomPlayerLoop
    {
        public event Action<double>? OnProcess;
        public event Action<double>? OnPhysicsProcess;

        public void Process(double delta)
        {
            OnProcess?.Invoke(delta);
        }

        public void PhysicsProcess(double delta)
        {
            OnPhysicsProcess?.Invoke(delta);
        }
    }
}

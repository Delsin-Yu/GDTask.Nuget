using System;
using System.Threading;
using System.Threading.Tasks;
using GdUnit4;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace GodotTask.Tests;

[TestSuite]
public class GDTaskTest_Delay
{


    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Yield_Process()
    {
        await Constants.WaitForTaskReadyAsync();
        await GDTask.Yield();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Yield_PhysicsProcess()
    {
        await Constants.WaitForTaskReadyAsync();
        await GDTask.Yield(PlayerLoopTiming.PhysicsProcess);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Yield_Process_Token()
    {
        await Constants.WaitForTaskReadyAsync();
        var source = new CancellationTokenSource();
        await source.CancelAsync();
        try
        {
            await GDTask.Yield(PlayerLoopTiming.Process, source.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        throw new TestFailedException("Yield Instructions not canceled");
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Yield_PhysicsProcess_Token()
    {
        await Constants.WaitForTaskReadyAsync();
        var source = new CancellationTokenSource();
        await source.CancelAsync();
        try
        {
            await GDTask.Yield(PlayerLoopTiming.PhysicsProcess, source.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        throw new TestFailedException("Yield Instructions not canceled");
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_NextFrame_Process()
    {
        await Constants.WaitForTaskReadyAsync();
        await GDTask.NextFrame(PlayerLoopTiming.Process);
        using (new ScopedFrameCount(1, PlayerLoopTiming.Process))
        {
            await GDTask.NextFrame(PlayerLoopTiming.Process);
        }
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_NextFrame_PhysicsProcess()
    {
        await Constants.WaitForTaskReadyAsync();
        await GDTask.NextFrame(PlayerLoopTiming.PhysicsProcess);
        using (new ScopedFrameCount(1, PlayerLoopTiming.PhysicsProcess))
        {
            await GDTask.NextFrame(PlayerLoopTiming.PhysicsProcess);
        }
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_NextFrame_Process_Token()
    {
        await Constants.WaitForTaskReadyAsync();
        var source = new CancellationTokenSource();
        await source.CancelAsync();
        try
        {
            await GDTask.NextFrame(PlayerLoopTiming.Process, source.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        throw new TestFailedException("NextFrame Instructions not canceled");
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_NextFrame_PhysicsProcess_Token()
    {
        await Constants.WaitForTaskReadyAsync();
        var source = new CancellationTokenSource();
        await source.CancelAsync();
        try
        {
            await GDTask.NextFrame(PlayerLoopTiming.PhysicsProcess, source.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        throw new TestFailedException("NextFrame Instructions not canceled");
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_DelayFrame_Process()
    {
        await Constants.WaitForTaskReadyAsync();
        await GDTask.NextFrame(PlayerLoopTiming.Process);
        using (new ScopedFrameCount(Constants.DelayFrames, PlayerLoopTiming.Process))
        {
            await GDTask.DelayFrame(Constants.DelayFrames);
        }
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_DelayFrame_PhysicsProcess()
    {
        await Constants.WaitForTaskReadyAsync();
        await GDTask.NextFrame(PlayerLoopTiming.PhysicsProcess);
        using (new ScopedFrameCount(Constants.DelayFrames, PlayerLoopTiming.PhysicsProcess))
        {
            await GDTask.DelayFrame(Constants.DelayFrames, PlayerLoopTiming.PhysicsProcess);
        }
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_DelayFrame_Process_Token()
    {
        await Constants.WaitForTaskReadyAsync();
        var source = new CancellationTokenSource();
        await source.CancelAsync();
        try
        {
            await GDTask.DelayFrame(Constants.DelayFrames, PlayerLoopTiming.Process, source.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        throw new TestFailedException("DelayFrame Instructions not canceled");
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_DelayFrame_PhysicsProcess_Token()
    {
        await Constants.WaitForTaskReadyAsync();
        var source = new CancellationTokenSource();
        await source.CancelAsync();
        try
        {
            await GDTask.DelayFrame(Constants.DelayFrames, PlayerLoopTiming.Process, source.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        throw new TestFailedException("DelayFrame Instructions not canceled");
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Delay_DeltaTime_Process()
    {
        await Constants.WaitForTaskReadyAsync();
        await GDTask.NextFrame(PlayerLoopTiming.Process);
        using (new ScopedStopwatch()) await GDTask.Delay(Constants.DelayTimeSpan, DelayType.DeltaTime);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Delay_DeltaTime_PhysicsProcess()
    {
        await Constants.WaitForTaskReadyAsync();
        await GDTask.NextFrame(PlayerLoopTiming.PhysicsProcess);
        using (new ScopedStopwatch()) await GDTask.Delay(Constants.DelayTimeSpan, DelayType.DeltaTime, PlayerLoopTiming.PhysicsProcess);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Delay_DeltaTime_Isolated_Process()
    {
        await Constants.WaitForTaskReadyAsync();
        await GDTask.NextFrame(PlayerLoopTiming.Process);
        var tree = GDTaskPlayerLoopRunner.Global.GetTree();
        tree.Paused = true;
        using (new ScopedStopwatch()) await GDTask.Delay(Constants.DelayTimeSpan, DelayType.DeltaTime, PlayerLoopTiming.IsolatedProcess);
        tree.Paused = false;
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Delay_DeltaTime_Isolated_PhysicsProcess()
    {
        await Constants.WaitForTaskReadyAsync();
        await GDTask.NextFrame(PlayerLoopTiming.PhysicsProcess);
        var tree = GDTaskPlayerLoopRunner.Global.GetTree();
        tree.Paused = true;
        using (new ScopedStopwatch()) await GDTask.Delay(Constants.DelayTimeSpan, DelayType.DeltaTime, PlayerLoopTiming.IsolatedPhysicsProcess);
        tree.Paused = false;
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Delay_DeltaTime_Process_Token()
    {
        await Constants.WaitForTaskReadyAsync();
        await GDTask.NextFrame(PlayerLoopTiming.Process);
        var source = new CancellationTokenSource();
        await source.CancelAsync();
        try
        {
            await GDTask.Delay(Constants.DelayTimeSpan, DelayType.DeltaTime, cancellationToken: source.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        throw new TestFailedException("Delay Instructions not canceled");
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Delay_DeltaTime_PhysicsProcess_Token()
    {
        await Constants.WaitForTaskReadyAsync();
        await GDTask.NextFrame(PlayerLoopTiming.PhysicsProcess);
        var source = new CancellationTokenSource();
        await source.CancelAsync();
        try
        {
            await GDTask.Delay(Constants.DelayTimeSpan, DelayType.DeltaTime, PlayerLoopTiming.PhysicsProcess, source.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        throw new TestFailedException("Delay Instructions not canceled");
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Delay_Realtime_Process()
    {
        await Constants.WaitForTaskReadyAsync();
        await GDTask.NextFrame(PlayerLoopTiming.Process);
        using (new ScopedStopwatch()) await GDTask.Delay(Constants.DelayTimeSpan, DelayType.Realtime);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Delay_Realtime_PhysicsProcess()
    {
        await Constants.WaitForTaskReadyAsync();
        await GDTask.NextFrame(PlayerLoopTiming.PhysicsProcess);
        using (new ScopedStopwatch()) await GDTask.Delay(Constants.DelayTimeSpan, DelayType.Realtime, PlayerLoopTiming.PhysicsProcess);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Delay_RealTime_Process_Token()
    {
        await Constants.WaitForTaskReadyAsync();
        await GDTask.NextFrame(PlayerLoopTiming.Process);
        var source = new CancellationTokenSource();
        await source.CancelAsync();
        try
        {
            await GDTask.Delay(Constants.DelayTimeSpan, DelayType.Realtime, cancellationToken: source.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        throw new TestFailedException("Delay Instructions not canceled");
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Delay_RealTime_PhysicsProcess_Token()
    {
        await Constants.WaitForTaskReadyAsync();
        await GDTask.NextFrame(PlayerLoopTiming.PhysicsProcess);
        var source = new CancellationTokenSource();
        await source.CancelAsync();
        try
        {
            await GDTask.Delay(Constants.DelayTimeSpan, DelayType.Realtime, PlayerLoopTiming.PhysicsProcess, source.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        throw new TestFailedException("Delay Instructions not canceled");
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Yield_CustomLoop_UsesMatchingLoop_AndUnsubscribes()
    {
        await Constants.WaitForTaskReadyAsync();
        var firstLoop = new ManualCustomPlayerLoop();
        var secondLoop = new ManualCustomPlayerLoop();
        var completed = false;

        GDTask.Yield(firstLoop).ToGDTask().ContinueWith(() => completed = true).Forget();

        Assertions.AssertThat(firstLoop.ProcessSubscriberCount).IsEqual(1);

        secondLoop.RaiseProcess();
        Assertions.AssertThat(completed).IsFalse();

        firstLoop.RaiseProcess();
        await GDTask.WaitUntil(() => completed);

        Assertions.AssertThat(firstLoop.ProcessSubscriberCount).IsEqual(0);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_CustomLoop_SharedDispatch_CompletesYieldAndNextFrame()
    {
        await Constants.WaitForTaskReadyAsync();
        var loop = new ManualCustomPlayerLoop();
        var yieldCompleted = false;
        var nextFrameCompleted = false;

        GDTask.Yield(loop).ToGDTask().ContinueWith(() => yieldCompleted = true).Forget();
        GDTask.NextFrame(loop).ContinueWith(() => nextFrameCompleted = true).Forget();

        loop.RaiseProcess();

        await GDTask.WaitUntil(() => yieldCompleted && nextFrameCompleted);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_DelayFrame_CustomLoop_Process()
    {
        await Constants.WaitForTaskReadyAsync();
        var loop = new ManualCustomPlayerLoop();
        var completed = false;

        GDTask.DelayFrame(2, loop).ContinueWith(() => completed = true).Forget();

        loop.RaiseProcess();
        Assertions.AssertThat(completed).IsFalse();

        loop.RaiseProcess();
        await GDTask.WaitUntil(() => completed);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Delay_CustomLoop_PhysicsProcess()
    {
        await Constants.WaitForTaskReadyAsync();
        var loop = new ManualCustomPlayerLoop();
        var completed = false;

        GDTask.Delay(TimeSpan.FromSeconds(0.5), loop, PlayerLoopTiming.PhysicsProcess).ContinueWith(() => completed = true).Forget();

        loop.RaisePhysicsProcess(0.2);
        Assertions.AssertThat(completed).IsFalse();

        loop.RaisePhysicsProcess(0.2);
        Assertions.AssertThat(completed).IsFalse();

        loop.RaisePhysicsProcess(0.2);
        await GDTask.WaitUntil(() => completed);
    }
}
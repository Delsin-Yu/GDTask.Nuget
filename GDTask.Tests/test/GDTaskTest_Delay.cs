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
}
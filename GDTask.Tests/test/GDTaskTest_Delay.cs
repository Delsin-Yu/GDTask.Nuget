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


    [TestCase]
    public static async Task GDTask_Yield_Process()
    {
        await GDTask.Yield();
    }

    [TestCase]
    public static async Task GDTask_Yield_PhysicsProcess()
    {
        await GDTask.Yield(PlayerLoopTiming.PhysicsProcess);
    }

    [TestCase]
    public static async Task GDTask_Yield_Process_Token()
    {
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

        throw new GdUnit4.Exceptions.TestFailedException("Yield Instructions not canceled");
    }

    [TestCase]
    public static async Task GDTask_Yield_PhysicsProcess_Token()
    {
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

        throw new GdUnit4.Exceptions.TestFailedException("Yield Instructions not canceled");
    }

    [TestCase]
    public static async Task GDTask_NextFrame_Process()
    {
        await GDTask.NextFrame(PlayerLoopTiming.Process);
        using (new ScopedFrameCount(1, PlayerLoopTiming.Process))
        {
            await GDTask.NextFrame(PlayerLoopTiming.Process);
        }
    }

    [TestCase]
    public static async Task GDTask_NextFrame_PhysicsProcess()
    {
        await GDTask.NextFrame(PlayerLoopTiming.PhysicsProcess);
        using (new ScopedFrameCount(1, PlayerLoopTiming.PhysicsProcess))
        {
            await GDTask.NextFrame(PlayerLoopTiming.PhysicsProcess);
        }
    }

    [TestCase]
    public static async Task GDTask_NextFrame_Process_Token()
    {
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

        throw new GdUnit4.Exceptions.TestFailedException("NextFrame Instructions not canceled");
    }

    [TestCase]
    public static async Task GDTask_NextFrame_PhysicsProcess_Token()
    {
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

        throw new GdUnit4.Exceptions.TestFailedException("NextFrame Instructions not canceled");
    }

    [TestCase]
    public static async Task GDTask_DelayFrame_Process()
    {
        await GDTask.NextFrame(PlayerLoopTiming.Process);
        using (new ScopedFrameCount(Constants.DelayFrames, PlayerLoopTiming.Process))
        {
            await GDTask.DelayFrame(Constants.DelayFrames);
        }
    }

    [TestCase]
    public static async Task GDTask_DelayFrame_PhysicsProcess()
    {
        await GDTask.NextFrame(PlayerLoopTiming.PhysicsProcess);
        using (new ScopedFrameCount(Constants.DelayFrames, PlayerLoopTiming.PhysicsProcess))
        {
            await GDTask.DelayFrame(Constants.DelayFrames, PlayerLoopTiming.PhysicsProcess);
        }
    }

    [TestCase]
    public static async Task GDTask_DelayFrame_Process_Token()
    {
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

        throw new GdUnit4.Exceptions.TestFailedException("DelayFrame Instructions not canceled");
    }

    [TestCase]
    public static async Task GDTask_DelayFrame_PhysicsProcess_Token()
    {
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

        throw new GdUnit4.Exceptions.TestFailedException("DelayFrame Instructions not canceled");
    }

    [TestCase]
    public static async Task GDTask_Delay_DeltaTime_Process()
    {
        await GDTask.NextFrame(PlayerLoopTiming.Process);
        using (new ScopedStopwatch()) await GDTask.Delay(Constants.DelayTimeSpan, DelayType.DeltaTime);
    }

    [TestCase]
    public static async Task GDTask_Delay_DeltaTime_PhysicsProcess()
    {
        await GDTask.NextFrame(PlayerLoopTiming.PhysicsProcess);
        using (new ScopedStopwatch()) await GDTask.Delay(Constants.DelayTimeSpan, DelayType.DeltaTime, PlayerLoopTiming.PhysicsProcess);
    }

    [TestCase]
    public static async Task GDTask_Delay_DeltaTime_Process_Token()
    {
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

        throw new GdUnit4.Exceptions.TestFailedException("Delay Instructions not canceled");
    }

    [TestCase]
    public static async Task GDTask_Delay_DeltaTime_PhysicsProcess_Token()
    {
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

        throw new GdUnit4.Exceptions.TestFailedException("Delay Instructions not canceled");
    }

    [TestCase]
    public static async Task GDTask_Delay_Realtime_Process()
    {
        await GDTask.NextFrame(PlayerLoopTiming.Process);
        using (new ScopedStopwatch()) await GDTask.Delay(Constants.DelayTimeSpan, DelayType.Realtime);
    }

    [TestCase]
    public static async Task GDTask_Delay_Realtime_PhysicsProcess()
    {
        await GDTask.NextFrame(PlayerLoopTiming.PhysicsProcess);
        using (new ScopedStopwatch()) await GDTask.Delay(Constants.DelayTimeSpan, DelayType.Realtime, PlayerLoopTiming.PhysicsProcess);
    }

    [TestCase]
    public static async Task GDTask_Delay_RealTime_Process_Token()
    {
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

        throw new GdUnit4.Exceptions.TestFailedException("Delay Instructions not canceled");
    }

    [TestCase]
    public static async Task GDTask_Delay_RealTime_PhysicsProcess_Token()
    {
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

        throw new GdUnit4.Exceptions.TestFailedException("Delay Instructions not canceled");
    }
}
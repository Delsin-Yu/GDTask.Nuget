using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GdUnit4;
using Godot;
using Timer = Godot.Timer;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace Fractural.Tasks.Tests;

[TestSuite]
public class GDTaskTest_Delay
{
    private const int DelayFrames = 5;
    private static readonly TimeSpan DelayTimeSpan = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan DelayDownToleranceSpan = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan DelayUpToleranceSpan = TimeSpan.FromSeconds(3);

    [TestCase]
    public static async Task GDTask_Yield()
    {
        await GDTask.Yield();
    }

    [TestCase]
    public static async Task GDTask_Yield_WithParam()
    {
        await GDTask.Yield(PlayerLoopTiming.PhysicsProcess);
    }

    [TestCase]
    public static async Task GDTask_Yield_WithToken()
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
        var processFrames = Engine.GetProcessFrames();
        await GDTask.NextFrame(PlayerLoopTiming.Process);
        Assertions.AssertThat(Engine.GetProcessFrames()).IsEqual(processFrames + 1);
    }

    [TestCase]
    public static async Task GDTask_NextFrame_PhysicsProcess()
    {
        await GDTask.NextFrame(PlayerLoopTiming.PhysicsProcess);
        var processFrames = Engine.GetPhysicsFrames();
        await GDTask.NextFrame(PlayerLoopTiming.PhysicsProcess);
        Assertions.AssertThat(Engine.GetPhysicsFrames()).IsEqual(processFrames + 1);
    }

    [TestCase]
    public static async Task GDTask_NextFrame_Process_CancellationToken()
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
    public static async Task GDTask_NextFrame_PhysicsProcess_CancellationToken()
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
        var processFrames = Engine.GetProcessFrames();
        await GDTask.DelayFrame(DelayFrames);
        Assertions.AssertThat(processFrames + DelayFrames).IsEqual(Engine.GetProcessFrames());
    }

    [TestCase]
    public static async Task GDTask_DelayFrame_PhysicsProcess()
    {
        await GDTask.NextFrame(PlayerLoopTiming.PhysicsProcess);
        var physicsFrames = Engine.GetPhysicsFrames();
        await GDTask.DelayFrame(DelayFrames, PlayerLoopTiming.PhysicsProcess);
        Assertions.AssertThat(physicsFrames + DelayFrames).IsEqual(Engine.GetPhysicsFrames());
    }


    [TestCase]
    public static async Task GDTask_DelayFrame_Process_CancellationToken()
    {
        var source = new CancellationTokenSource();
        await source.CancelAsync();
        try
        {
            await GDTask.DelayFrame(DelayFrames, PlayerLoopTiming.Process, source.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        throw new GdUnit4.Exceptions.TestFailedException("DelayFrame Instructions not canceled");
    }

    [TestCase]
    public static async Task GDTask_DelayFrame_PhysicsProcess_CancellationToken()
    {
        var source = new CancellationTokenSource();
        await source.CancelAsync();
        try
        {
            await GDTask.DelayFrame(DelayFrames, PlayerLoopTiming.Process, source.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        throw new GdUnit4.Exceptions.TestFailedException("DelayFrame Instructions not canceled");
    }

    [TestCase]
    public static async Task GDTask_Delay_Process()
    {
        await GDTask.NextFrame(PlayerLoopTiming.Process);
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        await GDTask.Delay(DelayTimeSpan);
        var elapsed = stopwatch.Elapsed;
        Assertions.AssertThat(elapsed.TotalSeconds)
            .IsBetween(
                DelayDownToleranceSpan.TotalSeconds,
                DelayUpToleranceSpan.TotalSeconds
            );
    }

    [TestCase]
    public static async Task GDTask_Delay_PhysicsProcess()
    {
        await GDTask.NextFrame(PlayerLoopTiming.PhysicsProcess);
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        await GDTask.Delay(DelayTimeSpan, PlayerLoopTiming.PhysicsProcess);
        var elapsed = stopwatch.Elapsed;
        Assertions.AssertThat(elapsed.TotalSeconds)
            .IsBetween(
                DelayDownToleranceSpan.TotalSeconds,
                DelayUpToleranceSpan.TotalSeconds
            );
    }

    [TestCase]
    public static async Task GDTask_Delay_Realtime_Process()
    {
        await GDTask.NextFrame(PlayerLoopTiming.Process);
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        await GDTask.Delay(DelayTimeSpan, DelayType.Realtime);
        var elapsed = stopwatch.Elapsed;
        Assertions.AssertThat(elapsed.TotalSeconds)
            .IsBetween(
                DelayDownToleranceSpan.TotalSeconds,
                DelayUpToleranceSpan.TotalSeconds
            );
    }

    [TestCase]
    public static async Task GDTask_Delay_Realtime_PhysicsProcess()
    {
        await GDTask.NextFrame(PlayerLoopTiming.PhysicsProcess);
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        await GDTask.Delay(DelayTimeSpan, DelayType.Realtime, PlayerLoopTiming.PhysicsProcess);
        var elapsed = stopwatch.Elapsed;
        Assertions.AssertThat(elapsed.TotalSeconds)
            .IsBetween(
                DelayDownToleranceSpan.TotalSeconds,
                DelayUpToleranceSpan.TotalSeconds
            );
    }
}
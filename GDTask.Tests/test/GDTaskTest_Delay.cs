using System;
using System.Threading;
using System.Threading.Tasks;
using GdUnit4;
using Godot;

namespace Fractural.Tasks.Tests;

[TestSuite]
public class GDTaskTest_Delay
{
    [TestCase]
    public async Task GDTask_Yield()
    {
        await GDTask.Yield();
    }

    [TestCase]
    public async Task GDTask_Yield_WithParam()
    {
        await GDTask.Yield(PlayerLoopTiming.PhysicsProcess);
    }

    [TestCase]
    public async Task GDTask_Yield_WithToken()
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
    public async Task GDTask_NextFrame_Process()
    {
        await GDTask.NextFrame(PlayerLoopTiming.Process);
        var processFrames = Engine.GetProcessFrames();
        await GDTask.NextFrame(PlayerLoopTiming.Process);
        Assertions.AssertThat(Engine.GetProcessFrames()).IsEqual(processFrames + 1);
    }

    [TestCase]
    public async Task GDTask_NextFrame_PhysicsProcess()
    {
        await GDTask.NextFrame(PlayerLoopTiming.PhysicsProcess);
        var processFrames = Engine.GetPhysicsFrames();
        await GDTask.NextFrame(PlayerLoopTiming.PhysicsProcess);
        Assertions.AssertThat(Engine.GetPhysicsFrames()).IsEqual(processFrames + 1);
    }

    [TestCase]
    public async Task GDTask_NextFrame_Process_CancellationToken()
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
    public async Task GDTask_NextFrame_PhysicsProcess_CancellationToken()
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
}
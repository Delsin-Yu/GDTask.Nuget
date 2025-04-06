using System;
using System.Diagnostics;
using System.Threading;
using GdUnit4;
using Godot;

namespace GodotTask.Tests;

internal static class Constants
{
    internal const int ReturnValue = 5;
    internal const int DelayTime = 100;
    internal const int DelayFrames = 5;

    internal static readonly TimeSpan DelayTimeSpan = TimeSpan.FromMilliseconds(DelayTime);
    internal static readonly TimeSpan DelayDownToleranceSpan = TimeSpan.FromMilliseconds(DelayTime - DelayTime / 2f);
    internal static readonly TimeSpan DelayUpToleranceSpan = TimeSpan.FromMilliseconds(DelayTime + DelayTime / 2f);

    internal static async GDTask Throw()
    {
        await GDTask.Yield();
        throw new ExpectedException();
    }
    
    internal static async GDTask<int> ThrowT()
    {
        await GDTask.Yield();
        throw new ExpectedException();
    }

    internal static GDTask DelayRealtime(int multiplier = 1, CancellationToken? cancellationToken = default) => GDTask.Delay(DelayTimeSpan * multiplier, DelayType.Realtime, cancellationToken: cancellationToken ?? CancellationToken.None);
    internal static GDTask Delay(CancellationToken? cancellationToken = default) => GDTask.Delay(DelayTimeSpan, cancellationToken: cancellationToken ?? CancellationToken.None);

    internal static GDTask DelayRealtimeWithReturn(int multiplier = 1, CancellationToken? cancellationToken = default) => GDTask.Delay(DelayTimeSpan * multiplier, DelayType.Realtime, cancellationToken: cancellationToken ?? CancellationToken.None).ContinueWith(() => ReturnValue);
    internal static GDTask<int> DelayWithReturn(CancellationToken? cancellationToken = default) => GDTask.Delay(DelayTimeSpan, cancellationToken: cancellationToken ?? CancellationToken.None).ContinueWith(() => ReturnValue);

    internal static Node CreateTestNode(string nodeName)
    {
        var node = new Node { Name = nodeName };
        var root = ((SceneTree)Engine.GetMainLoop()).Root;
        root.CallDeferred(Node.MethodName.AddChild, node);
        return node;
    }
    
    internal static CancellationToken CreateCanceledToken() => new(true);
}

internal readonly struct ScopedStopwatch : IDisposable
{
    private readonly Stopwatch _stopwatch;

    public ScopedStopwatch()
    {
        _stopwatch = new();
        _stopwatch.Start();
    }

    public void Dispose()
    {
        Assertions.AssertThat(_stopwatch.Elapsed.TotalSeconds)
            .IsBetween(
                Constants.DelayDownToleranceSpan.TotalSeconds,
                Constants.DelayUpToleranceSpan.TotalSeconds
            );
    }
}

internal readonly struct ScopedFrameCount : IDisposable
{
    private readonly ulong _targetFrame;
    private readonly PlayerLoopTiming _timing;

    private static ulong GetCurrentFrame(PlayerLoopTiming timing)
    {
        return timing switch
        {
            PlayerLoopTiming.Process => Engine.GetProcessFrames(),
            PlayerLoopTiming.PhysicsProcess => Engine.GetPhysicsFrames(),
            _ => throw new ArgumentOutOfRangeException(nameof(timing), timing, null)
        };
    }

    public ScopedFrameCount(ulong offset, PlayerLoopTiming timing)
    {
        _targetFrame = GetCurrentFrame(timing) + offset;
        _timing = timing;
    }

    public void Dispose()
    {
        Assertions.AssertThat(_targetFrame)
            .IsEqual(GetCurrentFrame(_timing));
    }
}
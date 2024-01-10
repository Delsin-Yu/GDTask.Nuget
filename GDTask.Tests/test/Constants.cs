using System;
using System.Diagnostics;
using System.Threading;
using GdUnit4;
using Godot;

namespace Fractural.Tasks.Tests;

internal static class Constants
{
    internal const int ReturnValue = 5;
    internal const int DelayTime = 100;
    internal const int DelayFrames = 5;
    
    internal static readonly TimeSpan DelayTimeSpan = TimeSpan.FromSeconds(1);
    internal static readonly TimeSpan DelayDownToleranceSpan = TimeSpan.FromSeconds(0);
    internal static readonly TimeSpan DelayUpToleranceSpan = TimeSpan.FromSeconds(2);
    
    internal static GDTask Delay(CancellationToken? cancellationToken = default) => 
        GDTask.Delay(DelayTime, cancellationToken: cancellationToken ?? CancellationToken.None);
    
    internal static GDTask<int> DelayWithReturn(CancellationToken? cancellationToken = default) => 
        GDTask.Delay(DelayTime, cancellationToken: cancellationToken ?? CancellationToken.None).ContinueWith(() => ReturnValue);
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
using System;

namespace GodotTask;

internal interface IPlayerLoopScheduler
{
    int MainThreadId { get; }
    bool IsMainThread { get; }
    double DeltaTime { get; }
    double PhysicsDeltaTime { get; }
    double GetDeltaTime(PlayerLoopTiming timing);
    ulong GetFrameCount(PlayerLoopTiming timing);
    void AddAction(PlayerLoopTiming timing, IPlayerLoopItem action);
    void AddContinuation(PlayerLoopTiming timing, Action continuation);
    void AddDeferredAction(IPlayerLoopItem action);
    void AddDeferredContinuation(Action continuation);
}
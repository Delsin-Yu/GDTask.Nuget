# GDTask.Nuget

[![GitHub Release](https://img.shields.io/github/v/release/Delsin-Yu/GDTask.Nuget)](https://github.com/Delsin-Yu/GDTask.Nuget/releases/latest)
[![NuGet Version](https://img.shields.io/nuget/v/GDTask)](https://www.nuget.org/packages/GDTask)
![NuGet Downloads](https://img.shields.io/nuget/dt/GDtask)
[![Stars](https://img.shields.io/github/stars/Delsin-Yu/GDTask.Nuget?color=brightgreen)](https://github.com/Delsin-Yu/GDTask.Nuget/stargazers)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/Delsin-Yu/GDTask.Nuget/blob/main/LICENSE)

- Targets .NET 6 through .NET 10 with a GodotSharp 4.1.0+ dependency floor.
- This is the Nuget Package version based on code from:
  - **[Atlinx's GDTask addon for Godot](https://github.com/Fractural/GDTask)**
  - **[Cysharp's UniTask library for Unity](https://github.com/Cysharp/UniTask)**

---

<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table of Contents

- [Abstract](#abstract)
    - [Lightweight async/await integration for Godot](#lightweight-asyncawait-integration-for-godot)
  - [GDTask Under the hood](#gdtask-under-the-hood)
- [Installation via Nuget](#installation-via-nuget)
- [Basic API usage](#basic-api-usage)
- [Extended Feature Packages](#extended-feature-packages)
- [Task Profiling](#task-profiling)
- [Object Pool Configuration](#object-pool-configuration)
- [Compare with Standard .Net Task API](#compare-with-standard-net-task-api)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

---

## Abstract

> Clarification: Contents in the abstract section are mostly migrated from the [Cysharp's UniTask library for Unity](https://github.com/Cysharp/UniTask)

### Lightweight async/await integration for Godot

- Struct-based `GDTask<T>` with custom AsyncMethodBuilder: synchronously completed tasks require no heap allocation; asynchronous suspensions use pooled, reusable state-machine runners and promise objects instead of allocating a new `Task` each time.
- Bypasses `SynchronizationContext` and `ExecutionContext` capture by default, eliminating the per-`await` context-save overhead of the standard `Task` async model.
- Continuations are dispatched directly onto the Godot player loop (`Process`, `PhysicsProcess`, deferred, and isolated timings), removing the need for `TaskScheduler` or timer-based bridging.
- Provides Godot-specific awaitable primitives - frame delays, signal awaiting, `WaitUntil`/`WaitWhile` with automatic `GodotObject` lifetime tracking, and explicit thread-switching APIs (`SwitchToThreadPool`, `SwitchToMainThread`).
- Highly compatible surface with `Task`/`ValueTask`/`IValueTaskSource` (including direct conversion via `AsGDTask` / `AsTask`).

### GDTask Under the hood

- Built on the [task-like custom async method builder feature](https://github.com/dotnet/roslyn/blob/main/docs/features/task-types.md) of C# 7.0. `GDTask` / `GDTask<T>` are `readonly struct` types holding only a poolable `IGDTaskSource` reference and a version token. When a task completes synchronously the result is inlined in the struct with zero heap traffic; when it truly suspends, the async state machine is copied into a pooled runner object, not a new `Task`, and the runner's `MoveNext` delegate is pre-allocated and reused.
- The core completion primitive (`GDTaskCompletionSourceCore<T>`) explicitly never captures [ExecutionContext](https://learn.microsoft.com/en-us/dotnet/api/system.threading.executioncontext) or [SynchronizationContext](https://learn.microsoft.com/en-us/dotnet/api/system.threading.synchronizationcontext). Returning to the main thread is handled by enqueuing continuations into the player-loop-driven `ContinuationQueue` on the singleton node [GDTaskPlayerLoopRunner](https://github.com/Delsin-Yu/GDTask.Nuget/blob/main/GDTask/src/PlayerLoopRunner/PlayerLoopRunnerProvider.cs), not by posting back through a synchronization context.

## Installation via Nuget

For .Net CLI

```
dotnet add package GDTask
```

For Package Manager Console:

```
NuGet\Install-Package GDTask
```

## Basic API usage

For more detailed usage, see **[Unit Tests](https://github.com/Delsin-Yu/GDTask.Nuget/tree/main/GDTask.Tests/test)**.

```csharp
using GodotTask;

// Use GDTaskVoid if this task is only used with ApiUsage().Forget();
public async GDTask ApiUsage()
{
    // Delay the execution after frame(s).
    await GDTask.DelayFrame(100); 
    
    // Delay the execution after delayTimeSpan.
    await GDTask.Delay(TimeSpan.FromSeconds(1), DelayType.Realtime);

    // Delay the execution until the next Process.
    await GDTask.Yield(PlayerLoopTiming.Process);

    // The same APIs also accept any IPlayerLoop implementation.
    IPlayerLoop processLoop = PlayerLoopRunnerProvider.Process;
    await GDTask.Delay(TimeSpan.FromMilliseconds(10), DelayType.DeltaTime, processLoop);

    // Delay the execution until the next PhysicsProcess.
    await GDTask.WaitForPhysicsProcess();

    // Creates a task that will complete at the next provided PlayerLoopTiming when the supplied predicate evaluates to true
    await GDTask.WaitUntil(() => Time.GetTimeDictFromSystem()["second"].AsInt32() % 2 == 0);
    
    // Creates a task that will complete at the next provided PlayerLoopTiming when the provided monitorFunction returns a different value.
    await GDTask.WaitUntilValueChanged(Time.Singleton, timeInstance => timeInstance.GetTimeDictFromSystem()["second"]);
    
    // Creates an awaitable that asynchronously yields to ThreadPool when awaited.
    await GDTask.SwitchToThreadPool();
    
    /* Threaded work */
    
    // Creates an awaitable that yields back to the current or next requested main-thread player-loop update.
    await GDTask.SwitchToMainThread();

    // If you're already on the main thread but not yet in the requested update phase,
    // the continuation resumes on that next requested update instead of completing immediately.
    await GDTask.SwitchToMainThread(PlayerLoopTiming.PhysicsProcess);
    
    /* Main thread work */
    
    await GDTask.NextFrame();

    // Creates a continuation that executes when the target GDTask completes.
    int taskResult = await GDTask.Delay(10).ContinueWith(() => 5);
    
    GDTask<int> task1 = GDTask.Delay(10).ContinueWith(() => 5);
    GDTask<string> task2 = GDTask.Delay(20).ContinueWith(() => "Task Result");
    GDTask<bool> task3 = GDTask.Delay(30).ContinueWith(() => true);

    // Creates a task that will complete when all of the supplied tasks have completed.
    var (task1Result, task2Result, task3Result) = await GDTask.WhenAll(task1, task2, task3);

    try
    {
        // Creates a GDTask that has completed with the specified exception.
        await GDTask.FromException(new ExpectedException());
    }
    catch (ExpectedException) { }
    
    try
    {
        // Creates a GDTask that has completed due to cancellation with the specified cancellation token.
        await GDTask.FromCanceled(CancellationToken.None);
    }
    catch (OperationCanceledException) { }
    
    // Or use an alternative pattern to handle cancellation without exception:
    var isCanceled = await GDTask.FromCanceled(CancellationToken.None).SuppressCancellationThrow();
    
    try
    {
        var source = new CancellationTokenSource();
        source.CancelAfter(100);
        // Creates a task that never completes, with specified CancellationToken.
        await GDTask.Never(source.Token);
    }
    catch (OperationCanceledException) { }
    
    // Queues the specified work to run on the ThreadPool and returns a GDTask handle for that work.
    await GDTask.RunOnThreadPool(
        () => GD.Print(Environment.CurrentManagedThreadId.ToString()),
        configureAwait: true,
        cancellationToken: CancellationToken.None
    );

    // Create a GDTask that wraps around this task.
    await Task.Delay(5).AsGDTask(useCurrentSynchronizationContext: true);

    // Associate a time out to the current GDTask.
    try
    {
        await GDTask.Never(CancellationToken.None).Timeout(TimeSpan.FromMilliseconds(5));
    }
    catch (TimeoutException) { }

}
```

## Extended Feature Packages

The following packages extend the functionality of GDTask; they are optional components for projects where applicable.

AsyncTriggers are no longer included in the main GDTask package. If there is concrete demand for them again, they are intended to live in a separate community package instead.

---

### [GDTask.GlobalCancellation](https://github.com/Joy-less/GDTask.Nuget.GlobalCancellation)

[![GitHub Release](https://img.shields.io/github/v/release/Joy-less/GDTask.Nuget.GlobalCancellation)](https://github.com/Joy-less/GDTask.Nuget.GlobalCancellation/releases/latest) [![NuGet Version](https://img.shields.io/nuget/v/GDTask.GlobalCancellation)](https://www.nuget.org/packages/GDTask.GlobalCancellation) ![NuGet Downloads](https://img.shields.io/nuget/dt/GDTask.GlobalCancellation) [![Stars](https://img.shields.io/github/stars/Joy-less/GDTask.Nuget.GlobalCancellation?color=brightgreen)](https://github.com/Joy-less/GDTask.Nuget.GlobalCancellation/stargazers) [![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/Joy-less/GDTask.Nuget.GlobalCancellation/blob/main/LICENSE)

Authored by: [@Joy-less](https://github.com/Joy-less)

This package adds support for attaching a global cancellation token to GDTasks with the `AttachGlobalCancellation` extension method and cancelling it with the `GDTaskGlobalCancellation.Cancel` method. This is useful for cancelling certain tasks in bulk.

Package: [GDTask.GlobalCancellation on NuGet](https://www.nuget.org/packages/GDTask.GlobalCancellation)


Install:

```bash
dotnet add package GDTask.GlobalCancellation
```

Example usage:

```csharp
GDTask.Delay(TimeSpan.FromSeconds(3.0), GDTaskGlobalCancellation.GetToken());
GDTask.Delay(TimeSpan.FromSeconds(2.0), GDTaskGlobalCancellation.GetToken());
GDTask.Delay(TimeSpan.FromSeconds(5.0)).AttachGlobalCancellation();
GDTask.Delay(TimeSpan.FromSeconds(1.0)).AttachGlobalCancellation();

GDTaskGlobalCancellation.Cancel();
```

---

## Task Profiling

> Clarification: Contents in the task profiling section are mostly migrated from the [Cysharp's UniTask library for Unity](https://github.com/Cysharp/UniTask)

When calling `TaskTracker.ShowTrackerWindow()` in your code base, the GDTask system will create(or reuse) a `GDTask Tracker` window for inspecting/diagnosing (leaked) `GDTasks`.

![Image](https://github.com/Delsin-Yu/GDTask.Nuget/blob/main/Readme-Images/GDTaskTracker.png)

| Name              | Description                                                                                                                                                                                                              |
|:------------------|:-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Enable Tracking   | Enable the tracking system for collecting status for future started `GDTasks`, this is on by default when calling `TaskTracker.ShowTrackerWindow()`, you may also alter this value through `TaskTracker.EnableTracking`. |
| Enable StackTrace | Records and show stack traces for the active `GDTasks`, you may also alter this value through `TaskTracker.EnableStackTrace`.                                                                                            |
| GC Collect        | Invokes `GC.Collect()` manually.                                                                                                                                                                                         |

> - Do keep in mind this feature is for debugging purposes and it has performance penalties, so stay cautious when calling `TaskTracker.ShowTrackerWindow()` under the production environment.
> - The background status collection system does not start if you have never called `TaskTracker.ShowTrackerWindow()`.
> - Closing an active `GDTask Tracker` window does not stop the background status collection system, remember to toggle off `Enable Tracking` or set `TaskTracker.EnableTracking` to `false` in your code.
> - Godot Games embeds sub-windows by default, you can disable the `Embed Subwindows` option located in `ProjectSettings (Advanced Settings enabled)Display/Window/Subwindows/Embed Subwindows` for them to become Standalone Windows.
> - This window reacts to the `window closing command` (`NotificationWMCloseRequest`) correctly so it closes itself when you click the close button, to relaunch this window simply call `TaskTracker.ShowTrackerWindow()` again.

## Object Pool Configuration

GDTask internally pools and reuses the promise and state-machine objects behind operations such as `Delay`, `WaitUntil`, completion sources, and async runners. `TaskPool.MaxPoolSize` controls how many objects each individual internal pool is allowed to retain, with a default of `int.MaxValue`. The value is checked only when a completed object is returned to its pool, so changing it affects future returns only: lowering it does not trim objects that are already pooled, and once a pool is at or above the new limit, additional returns are simply skipped until its size drops below that cap.

> **Tip:** In most projects the default (unbounded) is fine. Consider lowering `MaxPoolSize` only if you observe unexpectedly high retained memory from pooled GDTask objects — for example, after a burst of thousands of concurrent tasks that are unlikely to recur.

---

## Compare with Standard .Net Task API

> Clarification: Contents in the compare section are mostly migrated from the [Cysharp's UniTask library for Unity](https://github.com/Cysharp/UniTask)

Same as the `Standard .Net Task APIs`, `CancellationToken` and `CancellationTokenSource` are widely used by the GDTask APIs as well.<br>
Otherwise, the following table shows the GDTask APIs provided that are meant to replace the usage of standard .Net Task APIs.

| .NET Type                                    | GDTask Type                                                      |
|----------------------------------------------|------------------------------------------------------------------|
| `Task`/`ValueTask`                           | `GDTask`                                                         |
| `Task<T>`/`ValueTask<T>`                     | `GDTask<T>`                                                      |
| `async void`                                 | `async GDTaskVoid`                                               |
| `+= async () => { }`                         | `GDTask.Void`, `GDTask.Action`                                   |
| ---                                          | `GDTaskCompletionSource`                                         |
| `TaskCompletionSource<T>`                    | `GDTaskCompletionSource<T>`/`AutoResetGDTaskCompletionSource<T>` |
| `ManualResetValueTaskSourceCore<T>`          | `GDTaskCompletionSourceCore<T>`                                  |
| `IValueTaskSource`                           | `IGDTaskSource`                                                  |
| `IValueTaskSource<T>`                        | `IGDTaskSource<T>`                                               |
| `ValueTask.IsCompleted`                      | `GDTask.Status.IsCompleted()`                                    |
| `ValueTask<T>.IsCompleted`                   | `GDTask<T>.Status.IsCompleted()`                                 |
| `CancellationToken.Register(UnsafeRegister)` | `CancellationToken.RegisterWithoutCaptureExecutionContext`       |
| `CancellationTokenSource.CancelAfter`        | `CancellationTokenSource.CancelAfterSlim`                        |
| `Task.Delay`                                 | `GDTask.Delay`                                                   |
| `Task.Yield`                                 | `GDTask.Yield`                                                   |
| `Task.Run`                                   | `GDTask.RunOnThreadPool`                                         |
| `Task.WhenAll`                               | `GDTask.WhenAll`                                                 |
| `Task.WhenAny`                               | `GDTask.WhenAny`                                                 |
| `Task.WhenEach`                              | `GDTask.WhenEach`                                                |
| `Task.CompletedTask`                         | `GDTask.CompletedTask`                                           |
| `Task.FromException`                         | `GDTask.FromException`                                           |
| `Task.FromResult`                            | `GDTask.FromResult`                                              |
| `Task.FromCanceled`                          | `GDTask.FromCanceled`                                            |
| `Task.ContinueWith`                          | `GDTask.ContinueWith`                                            |
| `TaskScheduler.UnobservedTaskException`      | `GDTaskExceptionHandler.UnobservedTaskException`                 |
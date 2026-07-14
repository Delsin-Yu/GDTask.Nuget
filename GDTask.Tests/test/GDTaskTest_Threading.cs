// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using GdUnit4;

namespace GodotTask.Tests;

[TestSuite]
public class GDTaskTest_Threading
{
    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_SwitchToThreadPool()
    {
        await Constants.WaitForTaskReadyAsync();
        await GDTask.SwitchToThreadPool();
        Assertions
            .AssertThat(Thread.CurrentThread.IsThreadPoolThread)
            .IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_SwitchToMainThread_Process()
    {
        await Constants.WaitForTaskReadyAsync();
        await GDTask.SwitchToMainThread();
        Assertions
            .AssertThat(GDTaskScheduler.IsMainThread)
            .IsTrue();
    }
    
    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_SwitchToMainThread_Process_Token()
    {
        await Constants.WaitForTaskReadyAsync();
        try
        {
            await GDTask.SwitchToMainThread(Constants.CreateCanceledToken());
        }
        catch (OperationCanceledException)
        {
            return;
        }

        throw new TestFailedException("SwitchToMainThread not canceled");
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_SwitchToMainThread_WrongMainThreadTiming_DoesNotCompleteSynchronously()
    {
        await Constants.WaitForTaskReadyAsync();
        var completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        GDTask.Post(
            () => completionSource.SetResult(GDTask.SwitchToMainThread(PlayerLoopTiming.PhysicsProcess).GetAwaiter().IsCompleted),
            PlayerLoopTiming.Process
        );

        var isCompleted = await completionSource.Task;

        Assertions.AssertThat(isCompleted).IsFalse();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_SwitchToMainThread_CurrentMainThreadTiming_CompletesSynchronously()
    {
        await Constants.WaitForTaskReadyAsync();
        var completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        GDTask.Post(
            () => completionSource.SetResult(GDTask.SwitchToMainThread(PlayerLoopTiming.Process).GetAwaiter().IsCompleted),
            PlayerLoopTiming.Process
        );

        var isCompleted = await completionSource.Task;

        Assertions.AssertThat(isCompleted).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_SwitchToMainThread_CustomPlayerLoop()
    {
        await Constants.WaitForTaskReadyAsync();
        var playerLoop = new ManualPlayerLoop();

        await GDTask.SwitchToThreadPool();
        
        var task = GDTask.Create(async () =>
        {
            await GDTask.SwitchToMainThread(playerLoop);
            return GDTaskScheduler.IsMainThread;
        });

        await GDTask.SwitchToMainThread();
        playerLoop.Tick();
        var resumedOnMainThread = await task;

        playerLoop.Dispose();
        Assertions.AssertThat(resumedOnMainThread).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_SwitchToMainThread_CustomPlayerLoop_RespectsCurrentTickContext()
    {
        await Constants.WaitForTaskReadyAsync();
        using var playerLoop = new ManualPlayerLoop();

        var queuedTask = GDTask.Create(async () =>
        {
            await GDTask.SwitchToMainThread(playerLoop);
            return Thread.CurrentThread.ManagedThreadId;
        }).AsTask();

        Assertions.AssertThat(queuedTask.IsCompleted).IsFalse();

        var completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var probeTask = GDTask.Create(async () =>
        {
            await GDTask.Yield(playerLoop);
            completionSource.SetResult(GDTask.SwitchToMainThread(playerLoop).GetAwaiter().IsCompleted);
        }).AsTask();

        playerLoop.Tick();

        var resumedThreadId = await queuedTask;
        var isCompletedInsideLoop = await completionSource.Task;
        await probeTask;

        Assertions.AssertThat(resumedThreadId).IsEqual(Thread.CurrentThread.ManagedThreadId);
        Assertions.AssertThat(isCompletedInsideLoop).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_SwitchToMainThread_BackgroundFirstAccess_InitializesSafely()
    {
        var mainThreadId = Thread.CurrentThread.ManagedThreadId;
        var backgroundLoopSource = new TaskCompletionSource<IPlayerLoop>(TaskCreationOptions.RunContinuationsAsynchronously);

        var resumeTask = Task.Run(async () =>
        {
            var playerLoop = PlayerLoopRunnerProvider.Process;
            backgroundLoopSource.SetResult(playerLoop);
            await GDTask.SwitchToMainThread();
            return Thread.CurrentThread.ManagedThreadId;
        });

        var backgroundLoop = await backgroundLoopSource.Task;

        await GDTask.Yield();

        var resumedThreadId = await resumeTask;

        Assertions.AssertThat(ReferenceEquals(backgroundLoop, PlayerLoopRunnerProvider.Process)).IsTrue();
        Assertions.AssertThat(resumedThreadId).IsEqual(mainThreadId);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Post_CustomPlayerLoop()
    {
        await Constants.WaitForTaskReadyAsync();
        using var playerLoop = new ManualPlayerLoop();
        var postedThreadId = -1;

        GDTask.Post(() => postedThreadId = Thread.CurrentThread.ManagedThreadId, playerLoop);

        Assertions.AssertThat(postedThreadId).IsEqual(-1);

        playerLoop.Tick();

        Assertions.AssertThat(postedThreadId).IsEqual(Thread.CurrentThread.ManagedThreadId);
    }

    
    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_RunOnThreadPool_Delegate()
    {
        await Constants.WaitForTaskReadyAsync();
        await GDTask.RunOnThreadPool(
            (Action)(() => Assertions
                .AssertThat(Thread.CurrentThread.IsThreadPoolThread)
                .IsTrue()),
            false,
            CancellationToken.None
        );
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_RunOnThreadPool_Delegate_ConfigureAwait()
    {
        await Constants.WaitForTaskReadyAsync();
        await GDTask.RunOnThreadPool(
            (Action)(() => Assertions
                .AssertThat(Thread.CurrentThread.IsThreadPoolThread)
                .IsTrue()),
            true,
            CancellationToken.None
        );

        Assertions
            .AssertThat(GDTaskScheduler.IsMainThread)
            .IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_RunOnThreadPool_Delegate_Token()
    {
        await Constants.WaitForTaskReadyAsync();
        try
        {
            await GDTask.RunOnThreadPool(
                () => { },
                false,
                Constants.CreateCanceledToken()
            );
            throw new TestFailedException("Operation not canceled");
        }
        catch (OperationCanceledException)
        {
        }
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_RunOnThreadPool_Delegate_ConfigureAwait_Token()
    {
        await Constants.WaitForTaskReadyAsync();
        try
        {
            await GDTask.RunOnThreadPool(
                () => { },
                true,
                Constants.CreateCanceledToken()
            );

            throw new TestFailedException("Operation not canceled");
        }
        catch (OperationCanceledException)
        {
        }
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_RunOnThreadPool_DelegateT()
    {
        await Constants.WaitForTaskReadyAsync();
        var result = await GDTask.RunOnThreadPool(
            () =>
            {
                Assertions
                    .AssertThat(Thread.CurrentThread.IsThreadPoolThread)
                    .IsTrue();
                return Constants.ReturnValue;
            },
            false,
            CancellationToken.None
        );

        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_RunOnThreadPool_DelegateT_ConfigureAwait()
    {
        await Constants.WaitForTaskReadyAsync();
        var result = await GDTask.RunOnThreadPool(
            () =>
            {
                Assertions
                    .AssertThat(Thread.CurrentThread.IsThreadPoolThread)
                    .IsTrue();
                return Constants.ReturnValue;
            },
            true,
            CancellationToken.None
        );

        Assertions
            .AssertThat(GDTaskScheduler.IsMainThread)
            .IsTrue();

        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_RunOnThreadPool_DelegateT_Token()
    {
        await Constants.WaitForTaskReadyAsync();

        try
        {
            await GDTask.RunOnThreadPool(
                () => Constants.ReturnValue,
                false,
                Constants.CreateCanceledToken()
            );
            throw new TestFailedException("Operation not canceled");
        }
        catch (OperationCanceledException)
        {
        }
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_RunOnThreadPool_DelegateT_ConfigureAwait_Token()
    {
        await Constants.WaitForTaskReadyAsync();

        Constants.CreateCanceledToken();

        try
        {
            await GDTask.RunOnThreadPool(
                () => Constants.ReturnValue,
                true,
                Constants.CreateCanceledToken()
            );

            throw new TestFailedException("Operation not canceled");
        }
        catch (OperationCanceledException)
        {
        }
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_RunOnThreadPool_GDTask()
    {
        await Constants.WaitForTaskReadyAsync();
        await GDTask.RunOnThreadPool(
            () =>
            {
                Assertions
                    .AssertThat(Thread.CurrentThread.IsThreadPoolThread)
                    .IsTrue();
                return GDTask.CompletedTask;
            },
            false,
            CancellationToken.None
        );
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_RunOnThreadPool_GDTask_ConfigureAwait()
    {
        await Constants.WaitForTaskReadyAsync();
        await GDTask.RunOnThreadPool(
            () =>
            {
                Assertions
                    .AssertThat(Thread.CurrentThread.IsThreadPoolThread)
                    .IsTrue();
                return GDTask.CompletedTask;
            },
            true,
            CancellationToken.None
        );

        Assertions
            .AssertThat(GDTaskScheduler.IsMainThread)
            .IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_RunOnThreadPool_GDTask_Token()
    {
        await Constants.WaitForTaskReadyAsync();

        try
        {
            await GDTask.RunOnThreadPool(
                () => GDTask.CompletedTask,
                false,
                Constants.CreateCanceledToken()
            );
            throw new TestFailedException("Operation not canceled");
        }
        catch (OperationCanceledException)
        {
        }
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_RunOnThreadPool_GDTask_ConfigureAwait_Token()
    {
        await Constants.WaitForTaskReadyAsync();
        
        try
        {
            await GDTask.RunOnThreadPool(
                () => GDTask.CompletedTask,
                true,
                Constants.CreateCanceledToken()
            );

            throw new TestFailedException("Operation not canceled");
        }
        catch (OperationCanceledException)
        {
        }
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_RunOnThreadPool_GDTaskT()
    {
        await Constants.WaitForTaskReadyAsync();
        var result = await GDTask.RunOnThreadPool(
            () =>
            {
                Assertions
                    .AssertThat(Thread.CurrentThread.IsThreadPoolThread)
                    .IsTrue();
                return GDTask.FromResult(Constants.ReturnValue);
            },
            false,
            CancellationToken.None
        );
        
        Assertions
            .AssertThat(result)
            .IsEqual(Constants.ReturnValue);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_RunOnThreadPool_GDTaskT_ConfigureAwait()
    {
        await Constants.WaitForTaskReadyAsync();
        var result = await GDTask.RunOnThreadPool(
            () =>
            {
                Assertions
                    .AssertThat(Thread.CurrentThread.IsThreadPoolThread)
                    .IsTrue();
                return GDTask.FromResult(Constants.ReturnValue);
            },
            true,
            CancellationToken.None
        );

        Assertions
            .AssertThat(GDTaskScheduler.IsMainThread)
            .IsTrue();

        Assertions
            .AssertThat(result)
            .IsEqual(Constants.ReturnValue);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_RunOnThreadPool_GDTaskT_Token()
    {
        await Constants.WaitForTaskReadyAsync();
        
        try
        {
            await GDTask.RunOnThreadPool(
                () => GDTask.FromResult(Constants.ReturnValue),
                false,
                Constants.CreateCanceledToken()
            );
            throw new TestFailedException("Operation not canceled");
        }
        catch (OperationCanceledException)
        {
        }
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_RunOnThreadPool_GDTaskT_ConfigureAwait_Token()
    {
        await Constants.WaitForTaskReadyAsync();

        try
        {
            await GDTask.RunOnThreadPool(
                () => GDTask.FromResult(Constants.ReturnValue),
                true,
                Constants.CreateCanceledToken()
            );

            throw new TestFailedException("Operation not canceled");
        }
        catch (OperationCanceledException)
        {
        }
    }
    

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_ReturnToMainThread()
    {
        await Constants.WaitForTaskReadyAsync();
        await using (GDTask.ReturnToMainThread())
        {
            await GDTask.SwitchToThreadPool();
            Assertions
                .AssertThat(Thread.CurrentThread.IsThreadPoolThread)
                .IsTrue();
        }
        
        Assertions
            .AssertThat(GDTaskScheduler.IsMainThread)
            .IsTrue();
    }
    
    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_ReturnToSynchronizationContext()
    {
        await Constants.WaitForTaskReadyAsync();
        
        var context = SynchronizationContext.Current;
        
        await using (GDTask.ReturnToSynchronizationContext(context))
        {
            await GDTask.SwitchToThreadPool();
            Assertions
                .AssertThat(context != SynchronizationContext.Current);
        }
        
        Assertions
            .AssertThat(context == SynchronizationContext.Current);
    }
    
    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_ReturnToCurrentSynchronizationContext()
    {
        await Constants.WaitForTaskReadyAsync();
        
        var context = SynchronizationContext.Current;
        
        await using (GDTask.ReturnToCurrentSynchronizationContext())
        {
            await GDTask.SwitchToThreadPool();
            Assertions
                .AssertThat(context != SynchronizationContext.Current);
        }
        
        Assertions
            .AssertThat(context == SynchronizationContext.Current);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_ReturnToMainThread_Token()
    {
        await GDTask.SwitchToThreadPool();
        
        try
        {
            await using var handler = GDTask.ReturnToMainThread(Constants.CreateCanceledToken());
        }
        catch (OperationCanceledException)
        {
            return;
        }
        throw new TestFailedException("Operation not canceled");
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_ReturnToSynchronizationContext_Token()
    {
        await Constants.WaitForTaskReadyAsync();

        var context = SynchronizationContext.Current;
        
        try
        {
            await using var handler = GDTask.ReturnToSynchronizationContext(context, Constants.CreateCanceledToken());
        }
        catch (OperationCanceledException)
        {
            return;
        }
        throw new TestFailedException("Operation not canceled");
    }
    
    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_ReturnToCurrentSynchronizationContext_Token()
    {
        await Constants.WaitForTaskReadyAsync();

        try
        {
            await using var handler = GDTask.ReturnToCurrentSynchronizationContext(cancellationToken: Constants.CreateCanceledToken());
        }
        catch (OperationCanceledException)
        {
            return;
        }
        throw new TestFailedException("Operation not canceled");
    }
}
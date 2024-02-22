// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

using System;
using System.Threading;
using System.Threading.Tasks;
using GdUnit4;
using GdUnit4.Exceptions;

namespace GodotTask.Tests;

[TestSuite]
public class GDTaskTest_Threading
{
    [TestCase]
    public static async Task GDTask_SwitchToThreadPool()
    {
        await GDTask.SwitchToThreadPool();
        Assertions
            .AssertThat(Thread.CurrentThread.IsThreadPoolThread)
            .IsTrue();
    }

    [TestCase]
    public static async Task GDTask_SwitchToMainThread_Process()
    {
        await GDTask.SwitchToMainThread();
        Assertions
            .AssertThat(GDTaskPlayerLoopRunner.IsMainThread)
            .IsTrue();
    }
    
    [TestCase]
    public static async Task GDTask_SwitchToMainThread_Process_Token()
    {
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

    
    [TestCase]
    public static async Task GDTask_RunOnThreadPool_Delegate()
    {
        await GDTask.SwitchToMainThread();
        await GDTask.RunOnThreadPool(
            (Action)(() => Assertions
                .AssertThat(Thread.CurrentThread.IsThreadPoolThread)
                .IsTrue()),
            false,
            CancellationToken.None
        );
    }

    [TestCase]
    public static async Task GDTask_RunOnThreadPool_Delegate_ConfigureAwait()
    {
        await GDTask.SwitchToMainThread();
        await GDTask.RunOnThreadPool(
            (Action)(() => Assertions
                .AssertThat(Thread.CurrentThread.IsThreadPoolThread)
                .IsTrue()),
            true,
            CancellationToken.None
        );

        Assertions
            .AssertThat(GDTaskPlayerLoopRunner.IsMainThread)
            .IsTrue();
    }

    [TestCase]
    public static async Task GDTask_RunOnThreadPool_Delegate_Token()
    {
        await GDTask.SwitchToMainThread();
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

    [TestCase]
    public static async Task GDTask_RunOnThreadPool_Delegate_ConfigureAwait_Token()
    {
        await GDTask.SwitchToMainThread();

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


    [TestCase]
    public static async Task GDTask_RunOnThreadPool_DelegateT()
    {
        await GDTask.SwitchToMainThread();
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

    [TestCase]
    public static async Task GDTask_RunOnThreadPool_DelegateT_ConfigureAwait()
    {
        await GDTask.SwitchToMainThread();
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
            .AssertThat(GDTaskPlayerLoopRunner.IsMainThread)
            .IsTrue();

        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
    }

    [TestCase]
    public static async Task GDTask_RunOnThreadPool_DelegateT_Token()
    {
        await GDTask.SwitchToMainThread();

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

    [TestCase]
    public static async Task GDTask_RunOnThreadPool_DelegateT_ConfigureAwait_Token()
    {
        await GDTask.SwitchToMainThread();

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


    [TestCase]
    public static async Task GDTask_RunOnThreadPool_GDTask()
    {
        await GDTask.SwitchToMainThread();
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

    [TestCase]
    public static async Task GDTask_RunOnThreadPool_GDTask_ConfigureAwait()
    {
        await GDTask.SwitchToMainThread();
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
            .AssertThat(GDTaskPlayerLoopRunner.IsMainThread)
            .IsTrue();
    }

    [TestCase]
    public static async Task GDTask_RunOnThreadPool_GDTask_Token()
    {
        await GDTask.SwitchToMainThread();

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

    [TestCase]
    public static async Task GDTask_RunOnThreadPool_GDTask_ConfigureAwait_Token()
    {
        await GDTask.SwitchToMainThread();
        
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


    [TestCase]
    public static async Task GDTask_RunOnThreadPool_GDTaskT()
    {
        await GDTask.SwitchToMainThread();
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

    [TestCase]
    public static async Task GDTask_RunOnThreadPool_GDTaskT_ConfigureAwait()
    {
        await GDTask.SwitchToMainThread();
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
            .AssertThat(GDTaskPlayerLoopRunner.IsMainThread)
            .IsTrue();

        Assertions
            .AssertThat(result)
            .IsEqual(Constants.ReturnValue);
    }

    [TestCase]
    public static async Task GDTask_RunOnThreadPool_GDTaskT_Token()
    {
        await GDTask.SwitchToMainThread();
        
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

    [TestCase]
    public static async Task GDTask_RunOnThreadPool_GDTaskT_ConfigureAwait_Token()
    {
        await GDTask.SwitchToMainThread();

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
    

    [TestCase]
    public static async Task GDTask_ReturnToMainThread()
    {
        await using (GDTask.ReturnToMainThread())
        {
            await GDTask.SwitchToThreadPool();
            Assertions
                .AssertThat(Thread.CurrentThread.IsThreadPoolThread)
                .IsTrue();
        }
        
        Assertions
            .AssertThat(GDTaskPlayerLoopRunner.IsMainThread)
            .IsTrue();
    }
    
    [TestCase]
    public static async Task GDTask_ReturnToSynchronizationContext()
    {
        await GDTask.SwitchToMainThread();
        
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
    
    [TestCase]
    public static async Task GDTask_ReturnToCurrentSynchronizationContext()
    {
        await GDTask.SwitchToMainThread();
        
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


    [TestCase]
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

    [TestCase]
    public static async Task GDTask_ReturnToSynchronizationContext_Token()
    {
        await GDTask.SwitchToMainThread();
        
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
    
    [TestCase]
    public static async Task GDTask_ReturnToCurrentSynchronizationContext_Token()
    {
        await GDTask.SwitchToMainThread();

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
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

using System;
using System.Threading;
using System.Threading.Tasks;
using GdUnit4;
using GdUnit4.Exceptions;
using Godot;

namespace Fractural.Tasks.Tests;

[TestSuite]
public class GDTaskText_Threading
{
    [TestCase]
    public static async Task GDTask_IsMainThread()
    {
        await GDTask.NextFrame();
        Assertions
            .AssertThat(GDTaskPlayerLoopAutoload.IsMainThread)
            .IsTrue();
    }

    [TestCase]
    public static async Task GDTask_RunOnThreadPool_Delegate()
    {
        await GDTask.NextFrame();
        await GDTask.RunOnThreadPool(
            (Action)(() => Assertions
                .AssertThat(Thread.CurrentThread.IsThreadPoolThread)
                .IsTrue()),
            false,
            CancellationToken.None
        );

        Assertions
            .AssertThat(Thread.CurrentThread.IsThreadPoolThread)
            .IsTrue();
    }

    [TestCase]
    public static async Task GDTask_RunOnThreadPool_Delegate_ConfigureAwait()
    {
        await GDTask.NextFrame();
        await GDTask.RunOnThreadPool(
            (Action)(() => Assertions
                .AssertThat(Thread.CurrentThread.IsThreadPoolThread)
                .IsTrue()),
            true,
            CancellationToken.None
        );

        Assertions
            .AssertThat(GDTaskPlayerLoopAutoload.IsMainThread)
            .IsTrue();
    }

    [TestCase]
    public static async Task GDTask_RunOnThreadPool_Delegate_CancellationToken()
    {
        await GDTask.NextFrame();
        var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        try
        {
            await GDTask.RunOnThreadPool(
                () => { },
                false,
                cancellationTokenSource.Token
            );
            throw new TestFailedException("Operation not canceled");
        }
        catch (OperationCanceledException)
        {
        }
    }

    [TestCase]
    public static async Task GDTask_RunOnThreadPool_Delegate_ConfigureAwait_CancellationToken()
    {
        await GDTask.NextFrame();
        var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        try
        {
            await GDTask.RunOnThreadPool(
                () => { },
                true,
                cancellationTokenSource.Token
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
        await GDTask.NextFrame();
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

        Assertions
            .AssertThat(Thread.CurrentThread.IsThreadPoolThread)
            .IsTrue();

        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
    }

    [TestCase]
    public static async Task GDTask_RunOnThreadPool_DelegateT_ConfigureAwait()
    {
        await GDTask.NextFrame();
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
            .AssertThat(GDTaskPlayerLoopAutoload.IsMainThread)
            .IsTrue();

        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
    }

    [TestCase]
    public static async Task GDTask_RunOnThreadPool_DelegateT_CancellationToken()
    {
        await GDTask.NextFrame();
        var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        try
        {
            await GDTask.RunOnThreadPool(
                () => Constants.ReturnValue,
                false,
                cancellationTokenSource.Token
            );
            throw new TestFailedException("Operation not canceled");
        }
        catch (OperationCanceledException)
        {
        }
    }

    [TestCase]
    public static async Task GDTask_RunOnThreadPool_DelegateT_ConfigureAwait_CancellationToken()
    {
        await GDTask.NextFrame();
        var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        try
        {
            await GDTask.RunOnThreadPool(
                () => Constants.ReturnValue,
                true,
                cancellationTokenSource.Token
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
        await GDTask.NextFrame();
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

        Assertions
            .AssertThat(Thread.CurrentThread.IsThreadPoolThread)
            .IsTrue();
    }

    [TestCase]
    public static async Task GDTask_RunOnThreadPool_GDTask_ConfigureAwait()
    {
        await GDTask.NextFrame();
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
            .AssertThat(GDTaskPlayerLoopAutoload.IsMainThread)
            .IsTrue();
    }

    [TestCase]
    public static async Task GDTask_RunOnThreadPool_GDTask_CancellationToken()
    {
        await GDTask.NextFrame();
        var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        try
        {
            await GDTask.RunOnThreadPool(
                () => GDTask.CompletedTask,
                false,
                cancellationTokenSource.Token
            );
            throw new TestFailedException("Operation not canceled");
        }
        catch (OperationCanceledException)
        {
        }
    }

    [TestCase]
    public static async Task GDTask_RunOnThreadPool_GDTask_ConfigureAwait_CancellationToken()
    {
        await GDTask.NextFrame();
        var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        try
        {
            await GDTask.RunOnThreadPool(
                () => GDTask.CompletedTask,
                true,
                cancellationTokenSource.Token
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
        await GDTask.NextFrame();
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
            .AssertThat(Thread.CurrentThread.IsThreadPoolThread)
            .IsTrue();

        Assertions
            .AssertThat(result)
            .IsEqual(Constants.ReturnValue);
    }

    [TestCase]
    public static async Task GDTask_RunOnThreadPool_GDTaskT_ConfigureAwait()
    {
        await GDTask.NextFrame();
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
            .AssertThat(GDTaskPlayerLoopAutoload.IsMainThread)
            .IsTrue();

        Assertions
            .AssertThat(result)
            .IsEqual(Constants.ReturnValue);
    }

    [TestCase]
    public static async Task GDTask_RunOnThreadPool_GDTaskT_CancellationToken()
    {
        await GDTask.NextFrame();
        var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        try
        {
            await GDTask.RunOnThreadPool(
                () => GDTask.FromResult(Constants.ReturnValue),
                false,
                cancellationTokenSource.Token
            );
            throw new TestFailedException("Operation not canceled");
        }
        catch (OperationCanceledException)
        {
        }
    }

    [TestCase]
    public static async Task GDTask_RunOnThreadPool_GDTaskT_ConfigureAwait_CancellationToken()
    {
        await GDTask.NextFrame();
        var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        try
        {
            await GDTask.RunOnThreadPool(
                () => GDTask.FromResult(Constants.ReturnValue),
                true,
                cancellationTokenSource.Token
            );

            throw new TestFailedException("Operation not canceled");
        }
        catch (OperationCanceledException)
        {
        }
    }
}
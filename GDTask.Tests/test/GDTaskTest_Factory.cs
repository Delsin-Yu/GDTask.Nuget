// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

using System;
using System.Threading;
using System.Threading.Tasks;
using GdUnit4;
using GdUnit4.Exceptions;

namespace GodotTask.Tests;

[TestSuite]
public partial class GDTaskTest_Factory
{
    [TestCase]
    public static async Task GDTask_FromException()
    {
        try
        {
            await GDTask.FromException(new ExpectedException());
        }
        catch (ExpectedException)
        {
            return;
        }

        throw new TestFailedException("Exception not thrown");
    }

    [TestCase]
    public static async Task GDTask_FromExceptionT()
    {
        try
        {
            await GDTask.FromException<int>(new ExpectedException());
        }
        catch (ExpectedException)
        {
            return;
        }

        throw new TestFailedException("Exception not thrown");
    }

    [TestCase]
    public static async Task GDTask_FromResult()
    {
        Assertions
            .AssertThat(await GDTask.FromResult(Constants.ReturnValue))
            .IsEqual(Constants.ReturnValue);
    }

    [TestCase]
    public static async Task GDTask_FromCanceled()
    {
        try
        {
            await GDTask.FromCanceled();
        }
        catch (OperationCanceledException e)
        {
            Assertions
                .AssertThat(e.CancellationToken)
                .Equals(CancellationToken.None);
        }
    }

    [TestCase]
    public static async Task GDTask_FromCanceledT()
    {
        try
        {
            await GDTask.FromCanceled<int>();
        }
        catch (OperationCanceledException e)
        {
            Assertions
                .AssertThat(e.CancellationToken)
                .Equals(CancellationToken.None);
        }
    }

    [TestCase]
    public static async Task GDTask_FromCanceled_Token()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        try
        {
            await GDTask.FromCanceled(cancellationTokenSource.Token);
        }
        catch (OperationCanceledException e)
        {
            Assertions
                .AssertThat(e.CancellationToken)
                .Equals(cancellationTokenSource.Token);
        }
    }

    [TestCase]
    public static async Task GDTask_FromCanceledT_Token()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        try
        {
            await GDTask.FromCanceled<int>(cancellationTokenSource.Token);
        }
        catch (OperationCanceledException e)
        {
            Assertions
                .AssertThat(e.CancellationToken)
                .Equals(cancellationTokenSource.Token);
        }
    }

    [TestCase]
    public static async Task GDTask_Create()
    {
        await GDTask.Create(() => Constants.Delay());
    }

    [TestCase]
    public static async Task GDTask_CreateT()
    {
        Assertions
            .AssertThat(await GDTask.Create(() => Constants.DelayWithReturn()))
            .IsEqual(Constants.ReturnValue);
    }

    [TestCase]
    public static async Task GDTask_Lazy()
    {
        var started = false;
        var task = GDTask.Lazy(
            async () =>
            {
                started = true;
                await Constants.Delay();
            }
        );
        Assertions.AssertThat(started).IsFalse();
        await task;
    }

    [TestCase]
    public static async Task GDTask_LazyT()
    {
        var started = false;
        var task = GDTask.Lazy(
            async () =>
            {
                started = true;
                return await Constants.DelayWithReturn();
            }
        );
        Assertions.AssertThat(started).IsFalse();
        Assertions.AssertThat(await task).IsEqual(Constants.ReturnValue);
    }

    [TestCase]
    public static async Task GDTask_Void()
    {
        var finished = false;
        using (new ScopedStopwatch())
        {
            GDTask.Void(
                async () =>
                {
                    await Constants.Delay();
                    finished = true;
                }
            );
            await GDTask.WaitUntil(() => finished);
        }
    }

    [TestCase]
    public static async Task GDTask_Void_Token()
    {
        var finished = false;
        OperationCanceledException exception = null;
        GDTask.Void(
            async cancellationToken =>
            {
                try
                {
                    await Constants.Delay(cancellationToken);
                }
                catch (OperationCanceledException e)
                {
                    exception = e;
                    return;
                }

                finished = true;
            },
            Constants.CreateCanceledToken()
        );
        await GDTask.WaitUntil(() => finished || exception != null);
        Assertions.AssertThat(finished).IsFalse();
        Assertions.AssertThat(exception != null).IsTrue();
    }

    [TestCase]
    public static async Task GDTask_Action()
    {
        var finished = false;
        using (new ScopedStopwatch())
        {
            var call = GDTask.Action(
                async () =>
                {
                    await Constants.Delay();
                    finished = true;
                }
            );
            Assertions.AssertThat(finished).IsFalse();
            call();
            await GDTask.WaitUntil(() => finished);
        }
    }


    [TestCase]
    public static async Task GDTask_Action_Token()
    {
        var finished = false;
        OperationCanceledException exception = null;
        var call = GDTask.Action(
            async cancellationToken =>
            {
                try
                {
                    await Constants.Delay(cancellationToken);
                }
                catch (OperationCanceledException e)
                {
                    exception = e;
                    return;
                }

                finished = true;
            },
            Constants.CreateCanceledToken()
        );
        call();
        await GDTask.WaitUntil(() => finished || exception != null);
        Assertions.AssertThat(finished).IsFalse();
        Assertions.AssertThat(exception != null).IsTrue();
    }

    [TestCase]
    public static async Task GDTask_Defer()
    {
        var started = false;
        var deferredTask = GDTask.Defer(
            () =>
            {
                started = true;
                return Constants.Delay();
            }
        );

        Assertions.AssertThat(started).IsFalse();
        
        using (new ScopedStopwatch())
        {
            await deferredTask;
        }
    }
    
    [TestCase]
    public static async Task GDTask_DeferT()
    {
        var started = false;
        var deferredTask = GDTask.Defer(
            () =>
            {
                started = true;
                return Constants.DelayWithReturn();
            }
        );

        Assertions.AssertThat(started).IsFalse();
        
        using (new ScopedStopwatch())
        {
            Assertions
                .AssertThat(Constants.ReturnValue)
                .IsEqual(await deferredTask);
        }
    }

    [TestCase]
    public static async Task GDTask_Never()
    {
        try
        {
            await GDTask.Never(Constants.CreateCanceledToken());
        }
        catch (OperationCanceledException)
        {
            return;
        }

        throw new TestFailedException("OperationCanceledException not thrown");
    }

    [TestCase]
    public static async Task GDTask_NeverT()
    {
        try
        {
            await GDTask.Never<int>(Constants.CreateCanceledToken());
        }
        catch (OperationCanceledException)
        {
            return;
        }

        throw new TestFailedException("OperationCanceledException not thrown");
    }
}
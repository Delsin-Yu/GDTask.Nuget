// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

using System;
using System.Threading;
using System.Threading.Tasks;
using GdUnit4;

namespace GodotTask.Tests;

[TestSuite]
public partial class GDTaskTest_Factory
{
    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_FromException()
    {
        await Constants.WaitForTaskReadyAsync();
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

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_FromExceptionT()
    {
        await Constants.WaitForTaskReadyAsync();
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

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_FromResult()
    {
        await Constants.WaitForTaskReadyAsync();
        Assertions
            .AssertThat(await GDTask.FromResult(Constants.ReturnValue))
            .IsEqual(Constants.ReturnValue);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Lazy_FactoryThrowOnFirstCall_RetriesAndCompletes()
    {
        await Constants.WaitForTaskReadyAsync();
        var attempts = 0;
        var lazy = GDTask.Lazy(() =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw new ExpectedException();
            }

            return GDTask.CompletedTask;
        });

        try
        {
            await lazy.Task;
            throw new TestFailedException("Exception not thrown");
        }
        catch (ExpectedException)
        {
        }

        await lazy.Task.AsTask().WaitAsync(TimeSpan.FromSeconds(1));

        Assertions.AssertThat(attempts).IsEqual(2);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_LazyT_FactoryThrowOnFirstCall_RetriesAndCompletes()
    {
        await Constants.WaitForTaskReadyAsync();
        var attempts = 0;
        var lazy = GDTask.Lazy(() =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw new ExpectedException();
            }

            return GDTask.FromResult(Constants.ReturnValue);
        });

        try
        {
            _ = await lazy.Task;
            throw new TestFailedException("Exception not thrown");
        }
        catch (ExpectedException)
        {
        }

        Assertions.AssertThat(await lazy.Task.AsTask().WaitAsync(TimeSpan.FromSeconds(1)))
            .IsEqual(Constants.ReturnValue);
        Assertions.AssertThat(attempts).IsEqual(2);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_FromCanceled()
    {
        await Constants.WaitForTaskReadyAsync();
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

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_FromCanceledT()
    {
        await Constants.WaitForTaskReadyAsync();
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

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_FromCanceled_Token()
    {
        await Constants.WaitForTaskReadyAsync();
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

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_FromCanceledT_Token()
    {
        await Constants.WaitForTaskReadyAsync();
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

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Create()
    {
        await Constants.WaitForTaskReadyAsync();
        await GDTask.Create(() => Constants.Delay());
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_CreateT()
    {
        await Constants.WaitForTaskReadyAsync();
        Assertions
            .AssertThat(await GDTask.Create(() => Constants.DelayWithReturn()))
            .IsEqual(Constants.ReturnValue);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Lazy()
    {
        await Constants.WaitForTaskReadyAsync();
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

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_LazyT()
    {
        await Constants.WaitForTaskReadyAsync();
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

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Void()
    {
        await Constants.WaitForTaskReadyAsync();
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

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Void_Token()
    {
        await Constants.WaitForTaskReadyAsync();
        var finished = false;
        OperationCanceledException? exception = null;
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

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Action()
    {
        await Constants.WaitForTaskReadyAsync();
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

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Action_Token()
    {
        await Constants.WaitForTaskReadyAsync();
        var finished = false;
        OperationCanceledException? exception = null;
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

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Defer()
    {
        await Constants.WaitForTaskReadyAsync();
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
    
    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_DeferT()
    {
        await Constants.WaitForTaskReadyAsync();
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

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Never()
    {
        await Constants.WaitForTaskReadyAsync();
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

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_NeverT()
    {
        await Constants.WaitForTaskReadyAsync();
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
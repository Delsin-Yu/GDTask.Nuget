using System;
using System.Threading;
using System.Threading.Tasks;
using GdUnit4;

namespace GodotTask.Tests;

[TestSuite]
public class GDTaskTest_Utils
{
    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_AttachExternalCancellation()
    {
        await Constants.WaitForTaskReadyAsync();
        try
        {
            await Constants.Delay().AttachExternalCancellation(Constants.CreateCanceledToken());
        }
        catch (OperationCanceledException)
        {
            return;
        }

        throw new TestFailedException("Operation not cancelled");
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTaskT_AttachExternalCancellation()
    {
        await Constants.WaitForTaskReadyAsync();
        try
        {
            await Constants.DelayWithReturn().AttachExternalCancellation(Constants.CreateCanceledToken());
        }
        catch (OperationCanceledException)
        {
            return;
        }

        throw new TestFailedException("Operation not cancelled");
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Timeout()
    {
        await Constants.WaitForTaskReadyAsync();
        using (new ScopedStopwatch())
        {
            try
            {
                await GDTask.Never(CancellationToken.None).Timeout(Constants.DelayTimeSpan);
            }
            catch (TimeoutException)
            {
                return;
            }
        }

        throw new TestFailedException("Operation not cancelled");
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTaskT_Timeout()
    {
        await Constants.WaitForTaskReadyAsync();
        using (new ScopedStopwatch())
        {
            try
            {
                await GDTask.Never<int>(CancellationToken.None).Timeout(Constants.DelayTimeSpan);
            }
            catch (TimeoutException)
            {
                return;
            }
        }

        throw new TestFailedException("Operation not cancelled");
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_TimeoutWithoutException()
    {
        await Constants.WaitForTaskReadyAsync();
        using (new ScopedStopwatch())
        {
            var isTimeout = await GDTask.Never(CancellationToken.None).TimeoutWithoutException(Constants.DelayTimeSpan);
            Assertions.AssertThat(isTimeout);
        }
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTaskT_TimeoutWithoutException()
    {
        await Constants.WaitForTaskReadyAsync();
        using (new ScopedStopwatch())
        {
            var (isTimeout, _) = await GDTask.Never<int>(CancellationToken.None).TimeoutWithoutException(Constants.DelayTimeSpan);
            Assertions.AssertThat(isTimeout).IsTrue();
        }
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Forget()
    {
        await Constants.WaitForTaskReadyAsync();

        var finished = false;
        Constants.Delay().ContinueWith(() => finished = true).Forget();
        using (new ScopedStopwatch()) await GDTask.WaitUntil(() => finished);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_ForgetT()
    {
        await Constants.WaitForTaskReadyAsync();

        var finished = false;
        var result = 0;
        Constants.DelayWithReturn().ContinueWith(
            returnValue =>
            {
                result = returnValue;
                finished = true;
            }
        ).Forget();
        using (new ScopedStopwatch())
        {
            await GDTask.WaitUntil(() => finished);
        }

        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Forget_Exception()
    {
        await Constants.WaitForTaskReadyAsync();

        Exception? exception = null;
        Constants.Throw().Forget(exp => exception = exp);
        await GDTask.WaitUntil(() => exception != null);
        Assertions.AssertThat(exception is ExpectedException);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_ForgetT_Exception()
    {
        await Constants.WaitForTaskReadyAsync();

        Exception? exception = null;
        Constants.ThrowT().Forget(exp => exception = exp);
        await GDTask.WaitUntil(() => exception != null);
        Assertions.AssertThat(exception is ExpectedException);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_ContinueWith()
    {
        await Constants.WaitForTaskReadyAsync();
        var finished = false;
        await Constants.Delay().ContinueWith((Action)(() => finished = true));
        Assertions.AssertThat(finished).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_ContinueWithT()
    {
        await Constants.WaitForTaskReadyAsync();
        var result = await Constants.Delay().ContinueWith(() => Constants.ReturnValue);
        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_ContinueWith_GDTask()
    {
        await Constants.WaitForTaskReadyAsync();
        await Constants.Delay().ContinueWith(() => Constants.Delay());
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_ContinueWith_GDTaskT()
    {
        await Constants.WaitForTaskReadyAsync();
        var result = await Constants.Delay().ContinueWith(() => Constants.DelayWithReturn());
        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTaskT_ContinueWith()
    {
        await Constants.WaitForTaskReadyAsync();
        var result = -1;
        await Constants.DelayWithReturn().ContinueWith((Action<int>)(value => result = value));
        await Constants.DelayWithReturn().ContinueWith(() => { });
        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTaskT_ContinueWithT()
    {
        await Constants.WaitForTaskReadyAsync();
        var result1 = await Constants.DelayWithReturn().ContinueWith(value => value);
        var result2 = await Constants.DelayWithReturn().ContinueWith(() => Constants.ReturnValue);
        Assertions.AssertThat(result1).IsEqual(Constants.ReturnValue);
        Assertions.AssertThat(result2).IsEqual(Constants.ReturnValue);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTaskT_ContinueWith_GDTask()
    {
        await Constants.WaitForTaskReadyAsync();
        var result = -1;
        await Constants.DelayWithReturn().ContinueWith(
            value =>
            {
                result = value;
                return Constants.Delay();
            }
        );
        await Constants.DelayWithReturn().ContinueWith(() => Constants.Delay());
        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTaskT_ContinueWith_GDTaskT()
    {
        await Constants.WaitForTaskReadyAsync();
        var result1 = -1;
        var result2 = await Constants.DelayWithReturn().ContinueWith(
            value =>
            {
                result1 = value;
                return Constants.DelayWithReturn();
            }
        );
        Assertions.AssertThat(result1).IsEqual(result2);
        Assertions.AssertThat(result1).IsEqual(Constants.ReturnValue);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Unwrap_GDTask_GDTask()
    {
        await Constants.WaitForTaskReadyAsync();
        await GDTask.FromResult(GDTask.CompletedTask).Unwrap();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Unwrap_GDTask_GDTaskT()
    {
        await Constants.WaitForTaskReadyAsync();
        var result = await GDTask.FromResult(GDTask.FromResult(Constants.ReturnValue)).Unwrap();
        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTaskT_Unwrap_Task_GDTask()
    {
        await Constants.WaitForTaskReadyAsync();
        await Task.FromResult(GDTask.CompletedTask).Unwrap();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTaskT_Unwrap_Task_GDTaskT()
    {
        await Constants.WaitForTaskReadyAsync();
        var result = await Task.FromResult(GDTask.FromResult(Constants.ReturnValue)).Unwrap();
        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTaskT_Unwrap_Task_GDTask_Captured()
    {
        await GDTask.SwitchToThreadPool();
        await Task.FromResult(GDTask.CompletedTask).Unwrap(true);
        Assertions.AssertThat(Thread.CurrentThread.IsThreadPoolThread).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTaskT_Unwrap_Task_GDTaskT_Captured()
    {
        await GDTask.SwitchToThreadPool();
        var result = await Task.FromResult(GDTask.FromResult(Constants.ReturnValue)).Unwrap(true);
        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
        Assertions.AssertThat(Thread.CurrentThread.IsThreadPoolThread).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task Task_Unwrap_GDTask_Task()
    {
        await Constants.WaitForTaskReadyAsync();
        await GDTask.FromResult(Task.CompletedTask).Unwrap();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task Task_Unwrap_GDTask_Task_Captured()
    {
        await GDTask.SwitchToThreadPool();
        await GDTask.FromResult(Task.CompletedTask).Unwrap(true);
        Assertions.AssertThat(Thread.CurrentThread.IsThreadPoolThread).IsTrue();
    }
    
    [TestCase, RequireGodotRuntime]
    public static async Task Task_Unwrap_GDTask_TaskT()
    {
        await Constants.WaitForTaskReadyAsync();
        var result = await GDTask.FromResult(Task.FromResult(Constants.ReturnValue)).Unwrap();
        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task Task_Unwrap_GDTask_TaskT_Captured()
    {
        await GDTask.SwitchToThreadPool();
        var result = await GDTask.FromResult(Task.FromResult(Constants.ReturnValue)).Unwrap(true);
        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
        Assertions.AssertThat(Thread.CurrentThread.IsThreadPoolThread).IsTrue();
    }
}
using System;
using System.Threading;
using System.Threading.Tasks;
using GdUnit4;
using GdUnit4.Exceptions;

namespace Fractural.Tasks.Tests;

[TestSuite]
public class GDTaskTest_Interop
{
    [TestCase]
    public static async Task Task_AsGDTask_CurrentContext()
    {
        await GDTask.SwitchToMainThread();
        using (new ScopedStopwatch())
        {
            await Task.Delay(Constants.DelayTimeSpan).AsGDTask();
        }
    }

    [TestCase]
    public static async Task Task_AsGDTask_SchedulerContext()
    {
        await GDTask.SwitchToThreadPool();
        using (new ScopedStopwatch())
        {
            await Task.Delay(Constants.DelayTimeSpan).AsGDTask(false);
        }

        Assertions.AssertThat(Thread.CurrentThread.IsThreadPoolThread);
    }

    [TestCase]
    public static async Task TaskT_AsGDTask_CurrentContext()
    {
        await GDTask.SwitchToMainThread();
        int result;
        using (new ScopedStopwatch())
        {
            result = await Task.Delay(Constants.DelayTimeSpan).ContinueWith(task => Constants.ReturnValue).AsGDTask();
        }

        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
    }

    [TestCase]
    public static async Task TaskT_AsGDTask_SchedulerContext()
    {
        await GDTask.SwitchToThreadPool();
        int result;
        using (new ScopedStopwatch())
        {
            result = await Task.Delay(Constants.DelayTimeSpan).ContinueWith(task => Constants.ReturnValue).AsGDTask(false);
        }

        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);

        Assertions.AssertThat(Thread.CurrentThread.IsThreadPoolThread);
    }

    [TestCase]
    public static async Task GDTask_AsTask()
    {
        await GDTask.SwitchToMainThread();
        using (new ScopedStopwatch()) await Constants.Delay().AsTask();
    }

    [TestCase]
    public static async Task GDTaskT_AsTask()
    {
        await GDTask.SwitchToMainThread();
        int result;
        using (new ScopedStopwatch()) result = await Constants.DelayWithReturn().AsTask();
        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
    }

    [TestCase]
    public static async Task GDTask_ToAsyncLazy()
    {
        await GDTask.SwitchToMainThread();
        using (new ScopedStopwatch()) await Constants.Delay().ToAsyncLazy();
    }

    [TestCase]
    public static async Task GDTaskT_ToAsyncLazy()
    {
        await GDTask.SwitchToMainThread();
        int result;
        using (new ScopedStopwatch()) result = await Constants.DelayWithReturn().ToAsyncLazy();
        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
    }

    [TestCase]
    public static async Task GDTask_AttachExternalCancellation()
    {
        await GDTask.SwitchToMainThread();
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

    [TestCase]
    public static async Task GDTaskT_AttachExternalCancellation()
    {
        await GDTask.SwitchToMainThread();
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

    [TestCase]
    public static async Task GDTask_Timeout()
    {
        await GDTask.SwitchToMainThread();
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

    [TestCase]
    public static async Task GDTaskT_Timeout()
    {
        await GDTask.SwitchToMainThread();
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

    [TestCase]
    public static async Task GDTask_TimeoutWithoutException()
    {
        await GDTask.SwitchToMainThread();
        using (new ScopedStopwatch())
        {
            var isTimeout = await GDTask.Never(CancellationToken.None).TimeoutWithoutException(Constants.DelayTimeSpan);
            Assertions.AssertThat(isTimeout);
        }
    }

    [TestCase]
    public static async Task GDTaskT_TimeoutWithoutException()
    {
        await GDTask.SwitchToMainThread();
        using (new ScopedStopwatch())
        {
            var (isTimeout, result) = await GDTask.Never<int>(CancellationToken.None).TimeoutWithoutException(Constants.DelayTimeSpan);
            Assertions.AssertThat(isTimeout);
        }
    }

    [TestCase]
    public static async Task GDTask_Forget()
    {
        await GDTask.SwitchToMainThread();

        var finished = false;
        Constants.Delay().ContinueWith(() => finished = true).Forget();
        using (new ScopedStopwatch()) await GDTask.WaitUntil(() => finished);
    }

    [TestCase]
    public static async Task GDTask_ForgetT()
    {
        await GDTask.SwitchToMainThread();

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

    [TestCase]
    public static async Task GDTask_Forget_Exception()
    {
        await GDTask.SwitchToMainThread();

        Exception exception = null;
        Constants.Throw().Forget(exp => exception = exp);
        await GDTask.WaitUntil(() => exception != null);
        Assertions.AssertThat(exception is ExpectedException);
    }

    [TestCase]
    public static async Task GDTask_ForgetT_Exception()
    {
        await GDTask.SwitchToMainThread();

        Exception exception = null;
        Constants.ThrowT().Forget(exp => exception = exp);
        await GDTask.WaitUntil(() => exception != null);
        Assertions.AssertThat(exception is ExpectedException);
    }
}
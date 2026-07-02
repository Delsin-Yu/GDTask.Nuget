using System;
using System.Threading;
using System.Threading.Tasks;
using GdUnit4;

namespace GodotTask.Tests;

[TestSuite]
public class GDTaskTest_AutoResetCompletionSource
{
    [TestCase, RequireGodotRuntime]
    public static void AutoResetCompletionSource_Create_ReturnsPendingTask()
    {
        var source = AutoResetGDTaskCompletionSource.Create();
        Assertions.AssertThat(source).IsNotNull();
        Assertions.AssertThat(source.Task).IsNotNull();
        Assertions.AssertThat(source.Task.Status == GDTaskStatus.Pending).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static void AutoResetCompletionSourceT_Create_ReturnsPendingTask()
    {
        var source = AutoResetGDTaskCompletionSource<int>.Create();
        Assertions.AssertThat(source).IsNotNull();
        Assertions.AssertThat(source.Task).IsNotNull();
        Assertions.AssertThat(source.Task.Status == GDTaskStatus.Pending).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static void AutoResetCompletionSource_TrySetResult()
    {
        var source = AutoResetGDTaskCompletionSource.Create();
        source.TrySetResult();
        Assertions.AssertThat(source.Task.Status == GDTaskStatus.Succeeded).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static void AutoResetCompletionSource_TrySetException()
    {
        var source = AutoResetGDTaskCompletionSource.Create();
        source.TrySetException(new ExpectedException());
        Assertions.AssertThat(source.Task.Status == GDTaskStatus.Faulted).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static void AutoResetCompletionSource_TrySetCanceled()
    {
        var source = AutoResetGDTaskCompletionSource.Create();
        source.TrySetCanceled();
        Assertions.AssertThat(source.Task.Status == GDTaskStatus.Canceled).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static void AutoResetCompletionSourceT_TrySetResult()
    {
        var source = AutoResetGDTaskCompletionSource<int>.Create();
        source.TrySetResult(Constants.ReturnValue);
        Assertions.AssertThat(source.Task.Status == GDTaskStatus.Succeeded).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static void AutoResetCompletionSource_SecondTrySetResult_ReturnsFalse()
    {
        var source = AutoResetGDTaskCompletionSource.Create();
        Assertions.AssertThat(source.TrySetResult()).IsTrue();
        Assertions.AssertThat(source.TrySetResult()).IsFalse();
    }

    [TestCase, RequireGodotRuntime]
    public static void AutoResetCompletionSourceT_SecondTrySetResult_ReturnsFalse()
    {
        var source = AutoResetGDTaskCompletionSource<int>.Create();
        Assertions.AssertThat(source.TrySetResult(Constants.ReturnValue)).IsTrue();
        Assertions.AssertThat(source.TrySetResult(Constants.ReturnValue)).IsFalse();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task AutoResetCompletionSource_Async()
    {
        await Constants.WaitForTaskReadyAsync();
        var source = AutoResetGDTaskCompletionSource.Create();
        Constants.Delay().ContinueWith(() => source.TrySetResult()).Forget();
        await source.Task;
    }

    [TestCase, RequireGodotRuntime]
    public static async Task AutoResetCompletionSourceT_Async()
    {
        await Constants.WaitForTaskReadyAsync();
        var source = AutoResetGDTaskCompletionSource<int>.Create();
        Constants.Delay().ContinueWith(() => source.TrySetResult(Constants.ReturnValue)).Forget();
        var result = await source.Task;
        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task AutoResetCompletionSource_ReuseCycle()
    {
        await Constants.WaitForTaskReadyAsync();

        var first = AutoResetGDTaskCompletionSource.Create();
        first.TrySetResult();
        await first.Task;

        var second = AutoResetGDTaskCompletionSource.Create();
        Assertions.AssertThat(second.Task.Status == GDTaskStatus.Pending).IsTrue();
        second.TrySetResult();
        await second.Task;
    }

    [TestCase, RequireGodotRuntime]
    public static async Task AutoResetCompletionSourceT_ReuseCycle()
    {
        await Constants.WaitForTaskReadyAsync();

        var first = AutoResetGDTaskCompletionSource<int>.Create();
        first.TrySetResult(Constants.ReturnValue);
        Assertions.AssertThat(await first.Task).IsEqual(Constants.ReturnValue);

        var second = AutoResetGDTaskCompletionSource<int>.Create();
        Assertions.AssertThat(second.Task.Status == GDTaskStatus.Pending).IsTrue();
        second.TrySetResult(Constants.ReturnValue + 1);
        Assertions.AssertThat(await second.Task).IsEqual(Constants.ReturnValue + 1);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task AutoResetCompletionSource_AwaitTwice_Throws()
    {
        await Constants.WaitForTaskReadyAsync();
        var source = AutoResetGDTaskCompletionSource.Create();
        source.TrySetResult();
        var task = source.Task;
        await task;

        try
        {
            await task;
        }
        catch (InvalidOperationException)
        {
            return;
        }

        throw new TestFailedException("InvalidOperationException not thrown");
    }

    [TestCase, RequireGodotRuntime]
    public static async Task AutoResetCompletionSourceT_AwaitTwice_Throws()
    {
        await Constants.WaitForTaskReadyAsync();
        var source = AutoResetGDTaskCompletionSource<int>.Create();
        source.TrySetResult(Constants.ReturnValue);
        var task = source.Task;
        await task;

        try
        {
            await task;
        }
        catch (InvalidOperationException)
        {
            return;
        }

        throw new TestFailedException("InvalidOperationException not thrown");
    }

    [TestCase, RequireGodotRuntime]
    public static void AutoResetCompletionSource_CreateFromCanceled()
    {
        using var cts = new CancellationTokenSource();
        var source = AutoResetGDTaskCompletionSource.CreateFromCanceled(cts.Token, out var token);
        Assertions.AssertThat(source.Task.Status == GDTaskStatus.Canceled).IsTrue();

        try
        {
            source.GetResult(token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        throw new TestFailedException("OperationCanceledException not thrown");
    }

    [TestCase, RequireGodotRuntime]
    public static void AutoResetCompletionSource_CreateFromException()
    {
        var source = AutoResetGDTaskCompletionSource.CreateFromException(new ExpectedException(), out var token);
        Assertions.AssertThat(source.Task.Status == GDTaskStatus.Faulted).IsTrue();

        try
        {
            source.GetResult(token);
        }
        catch (ExpectedException)
        {
            return;
        }

        throw new TestFailedException("ExpectedException not thrown");
    }

    [TestCase, RequireGodotRuntime]
    public static void AutoResetCompletionSourceT_CreateFromResult()
    {
        var source = AutoResetGDTaskCompletionSource<int>.CreateFromResult(Constants.ReturnValue, out var token);
        Assertions.AssertThat(source.Task.Status == GDTaskStatus.Succeeded).IsTrue();
        Assertions.AssertThat(source.GetResult(token)).IsEqual(Constants.ReturnValue);
    }

    [TestCase, RequireGodotRuntime]
    public static void AutoResetCompletionSourceT_CreateFromCanceled()
    {
        using var cts = new CancellationTokenSource();
        var source = AutoResetGDTaskCompletionSource<int>.CreateFromCanceled(cts.Token, out var token);
        Assertions.AssertThat(source.Task.Status == GDTaskStatus.Canceled).IsTrue();

        try
        {
            source.GetResult(token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        throw new TestFailedException("OperationCanceledException not thrown");
    }

    [TestCase, RequireGodotRuntime]
    public static void AutoResetCompletionSourceT_CreateFromException()
    {
        var source = AutoResetGDTaskCompletionSource<int>.CreateFromException(new ExpectedException(), out var token);
        Assertions.AssertThat(source.Task.Status == GDTaskStatus.Faulted).IsTrue();

        try
        {
            source.GetResult(token);
        }
        catch (ExpectedException)
        {
            return;
        }

        throw new TestFailedException("ExpectedException not thrown");
    }

    [TestCase, RequireGodotRuntime]
    public static void AutoResetCompletionSource_CreateCompleted()
    {
        var source = AutoResetGDTaskCompletionSource.CreateCompleted(out var token);
        Assertions.AssertThat(source.Task.Status == GDTaskStatus.Succeeded).IsTrue();
        source.GetResult(token);
    }
}

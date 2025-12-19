using System;
using System.Threading.Tasks;
using GdUnit4;

namespace GodotTask.Tests;

[TestSuite]
public class GDTaskTest_CompletionSource
{
    [TestCase, RequireGodotRuntime]
    public static void CompletionSource_Constructor()
    {
        var source = new GDTaskCompletionSource();
        Assertions.AssertThat(source).IsNotNull();
        Assertions.AssertThat(source.Task).IsNotNull();
        Assertions.AssertThat(source.Task.Status == GDTaskStatus.Pending).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static void CompletionSource_TrySetResult()
    {
        var source = new GDTaskCompletionSource();
        source.TrySetResult();
        Assertions.AssertThat(source.Task.Status == GDTaskStatus.Succeeded).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static void CompletionSource_TrySetException()
    {
        var source = new GDTaskCompletionSource();
        source.TrySetException(new ExpectedException());
        Assertions.AssertThat(source.Task.Status == GDTaskStatus.Faulted).IsTrue();

        try
        {
            source.GetResult(0);
        }
        catch (ExpectedException)
        {
            return;
        }

        throw new TestFailedException("ExpectedException not thrown");
    }

    [TestCase, RequireGodotRuntime]
    public static void CompletionSource_TrySetCanceled()
    {
        var source = new GDTaskCompletionSource();
        source.TrySetCanceled();
        Assertions.AssertThat(source.Task.Status == GDTaskStatus.Canceled).IsTrue();

        try
        {
            source.GetResult(0);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        throw new TestFailedException("OperationCanceledException not thrown");
    }

    [TestCase, RequireGodotRuntime]
    public static async Task CompletionSource_Async()
    {
        await Constants.WaitForTaskReadyAsync();
        var source = new GDTaskCompletionSource();
        Constants.Delay().ContinueWith(() => source.TrySetResult()).Forget();

        // Await the task
        await source.Task;
    }

    [TestCase, RequireGodotRuntime]
    public static void CompletionSourceT_Constructor()
    {
        var source = new GDTaskCompletionSource<int>();
        Assertions.AssertThat(source).IsNotNull();
        Assertions.AssertThat(source.Task).IsNotNull();
        Assertions.AssertThat(source.Task.Status == GDTaskStatus.Pending).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static void CompletionSourceT_TrySetResult()
    {
        var source = new GDTaskCompletionSource<int>();
        source.TrySetResult(Constants.ReturnValue);
        Assertions.AssertThat(source.Task.Status == GDTaskStatus.Succeeded).IsTrue();
        Assertions.AssertThat(source.GetResult(0)).IsEqual(Constants.ReturnValue);
    }

    [TestCase, RequireGodotRuntime]
    public static void CompletionSourceT_TrySetException()
    {
        var source = new GDTaskCompletionSource<int>();
        source.TrySetException(new ExpectedException());
        Assertions.AssertThat(source.Task.Status == GDTaskStatus.Faulted).IsTrue();

        try
        {
            source.GetResult(0);
        }
        catch (ExpectedException)
        {
            return;
        }

        throw new TestFailedException("ExpectedException not thrown");
    }

    [TestCase, RequireGodotRuntime]
    public static void CompletionSourceT_TrySetCanceled()
    {
        var source = new GDTaskCompletionSource<int>();
        source.TrySetCanceled();
        Assertions.AssertThat(source.Task.Status == GDTaskStatus.Canceled).IsTrue();

        try
        {
            source.GetResult(0);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        throw new TestFailedException("OperationCanceledException not thrown");
    }

    [TestCase, RequireGodotRuntime]
    public static async Task CompletionSourceT_Async()
    {
        await Constants.WaitForTaskReadyAsync();
        var source = new GDTaskCompletionSource<int>();
        Constants.Delay().ContinueWith(() => source.TrySetResult(Constants.ReturnValue)).Forget();

        // Await the task
        var result = await source.Task;
        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
    }
}
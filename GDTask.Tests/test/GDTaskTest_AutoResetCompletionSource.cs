using System;
using System.Threading;
using System.Threading.Tasks;
using GdUnit4;

namespace GodotTask.Tests;

[TestSuite]
public class GDTaskTest_AutoResetCompletionSource
{
    private const int ConcurrentCompletionIterations = 200;
    private const int ConcurrentCompletionWriters = 12;
    private const int PoolReuseCycles = 8;

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
    public static void AutoResetCompletionSourceT_TrySetException()
    {
        var source = AutoResetGDTaskCompletionSource<int>.Create();
        source.TrySetException(new ExpectedException());
        Assertions.AssertThat(source.Task.Status == GDTaskStatus.Faulted).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static void AutoResetCompletionSourceT_TrySetCanceled()
    {
        var source = AutoResetGDTaskCompletionSource<int>.Create();
        source.TrySetCanceled();
        Assertions.AssertThat(source.Task.Status == GDTaskStatus.Canceled).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static void AutoResetCompletionSource_SecondTrySetResult_ReturnsFalse()
    {
        var source = AutoResetGDTaskCompletionSource.Create();
        Assertions.AssertThat(source.TrySetResult()).IsTrue();
        Assertions.AssertThat(source.TrySetResult()).IsFalse();
        Assertions.AssertThat(source.TrySetException(new ExpectedException())).IsFalse();
        Assertions.AssertThat(source.TrySetCanceled()).IsFalse();
    }

    [TestCase, RequireGodotRuntime]
    public static void AutoResetCompletionSourceT_SecondTrySetResult_ReturnsFalse()
    {
        var source = AutoResetGDTaskCompletionSource<int>.Create();
        Assertions.AssertThat(source.TrySetResult(Constants.ReturnValue)).IsTrue();
        Assertions.AssertThat(source.TrySetResult(Constants.ReturnValue + 1)).IsFalse();
        Assertions.AssertThat(source.TrySetException(new ExpectedException())).IsFalse();
        Assertions.AssertThat(source.TrySetCanceled()).IsFalse();
    }

    [TestCase, RequireGodotRuntime]
    public static void AutoResetCompletionSource_TrySetExceptionThenOther_ReturnsFalse()
    {
        var source = AutoResetGDTaskCompletionSource.Create();
        Assertions.AssertThat(source.TrySetException(new ExpectedException())).IsTrue();
        Assertions.AssertThat(source.TrySetResult()).IsFalse();
        Assertions.AssertThat(source.TrySetCanceled()).IsFalse();
        Assertions.AssertThat(source.Task.Status == GDTaskStatus.Faulted).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static void AutoResetCompletionSource_TrySetCanceledThenOther_ReturnsFalse()
    {
        var source = AutoResetGDTaskCompletionSource.Create();
        Assertions.AssertThat(source.TrySetCanceled()).IsTrue();
        Assertions.AssertThat(source.TrySetResult()).IsFalse();
        Assertions.AssertThat(source.TrySetException(new ExpectedException())).IsFalse();
        Assertions.AssertThat(source.Task.Status == GDTaskStatus.Canceled).IsTrue();
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
    public static async Task AutoResetCompletionSource_AwaitAlreadyCompleted()
    {
        await Constants.WaitForTaskReadyAsync();
        var source = AutoResetGDTaskCompletionSource.Create();
        source.TrySetResult();
        await source.Task;
    }

    [TestCase, RequireGodotRuntime]
    public static async Task AutoResetCompletionSourceT_AwaitAlreadyCompleted()
    {
        await Constants.WaitForTaskReadyAsync();
        var source = AutoResetGDTaskCompletionSource<int>.Create();
        source.TrySetResult(Constants.ReturnValue);
        Assertions.AssertThat(await source.Task).IsEqual(Constants.ReturnValue);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task AutoResetCompletionSource_AwaitException_Throws()
    {
        await Constants.WaitForTaskReadyAsync();
        var source = AutoResetGDTaskCompletionSource.Create();
        source.TrySetException(new ExpectedException());

        try
        {
            await source.Task;
        }
        catch (ExpectedException)
        {
            return;
        }

        throw new TestFailedException("ExpectedException not thrown");
    }

    [TestCase, RequireGodotRuntime]
    public static async Task AutoResetCompletionSourceT_AwaitException_Throws()
    {
        await Constants.WaitForTaskReadyAsync();
        var source = AutoResetGDTaskCompletionSource<int>.Create();
        source.TrySetException(new ExpectedException());

        try
        {
            await source.Task;
        }
        catch (ExpectedException)
        {
            return;
        }

        throw new TestFailedException("ExpectedException not thrown");
    }

    [TestCase, RequireGodotRuntime]
    public static async Task AutoResetCompletionSource_AwaitCanceled_Throws()
    {
        await Constants.WaitForTaskReadyAsync();
        using var cts = new CancellationTokenSource();
        var source = AutoResetGDTaskCompletionSource.Create();
        source.TrySetCanceled(cts.Token);

        try
        {
            await source.Task;
        }
        catch (OperationCanceledException exception)
        {
            Assertions.AssertThat(exception.CancellationToken.Equals(cts.Token)).IsTrue();
            return;
        }

        throw new TestFailedException("OperationCanceledException not thrown");
    }

    [TestCase, RequireGodotRuntime]
    public static async Task AutoResetCompletionSourceT_AwaitCanceled_Throws()
    {
        await Constants.WaitForTaskReadyAsync();
        using var cts = new CancellationTokenSource();
        var source = AutoResetGDTaskCompletionSource<int>.Create();
        source.TrySetCanceled(cts.Token);

        try
        {
            await source.Task;
        }
        catch (OperationCanceledException exception)
        {
            Assertions.AssertThat(exception.CancellationToken.Equals(cts.Token)).IsTrue();
            return;
        }

        throw new TestFailedException("OperationCanceledException not thrown");
    }

    [TestCase, RequireGodotRuntime]
    public static async Task AutoResetCompletionSource_AwaitExceptionSetLater_Throws()
    {
        await Constants.WaitForTaskReadyAsync();
        var source = AutoResetGDTaskCompletionSource.Create();
        Constants.Delay().ContinueWith(() => source.TrySetException(new ExpectedException())).Forget();

        try
        {
            await source.Task;
        }
        catch (ExpectedException)
        {
            return;
        }

        throw new TestFailedException("ExpectedException not thrown");
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
    public static async Task AutoResetCompletionSource_Reuse_ReturnsSameInstance()
    {
        await Constants.WaitForTaskReadyAsync();

        var first = AutoResetGDTaskCompletionSource.Create();
        first.TrySetResult();
        await first.Task;

        var second = AutoResetGDTaskCompletionSource.Create();
        Assertions.AssertThat(ReferenceEquals(first, second)).IsTrue();
        Assertions.AssertThat(second.Task.Status == GDTaskStatus.Pending).IsTrue();
        second.TrySetResult();
        await second.Task;
    }

    [TestCase, RequireGodotRuntime]
    public static async Task AutoResetCompletionSourceT_Reuse_ReturnsSameInstance()
    {
        await Constants.WaitForTaskReadyAsync();

        var first = AutoResetGDTaskCompletionSource<int>.Create();
        first.TrySetResult(Constants.ReturnValue);
        Assertions.AssertThat(await first.Task).IsEqual(Constants.ReturnValue);

        var second = AutoResetGDTaskCompletionSource<int>.Create();
        Assertions.AssertThat(ReferenceEquals(first, second)).IsTrue();
        Assertions.AssertThat(second.Task.Status == GDTaskStatus.Pending).IsTrue();
        second.TrySetResult(Constants.ReturnValue + 1);
        Assertions.AssertThat(await second.Task).IsEqual(Constants.ReturnValue + 1);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task AutoResetCompletionSource_Reuse_MultipleCycles()
    {
        await Constants.WaitForTaskReadyAsync();

        AutoResetGDTaskCompletionSource previous = null!;
        for (var i = 0; i < PoolReuseCycles; i++)
        {
            var source = AutoResetGDTaskCompletionSource.Create();
            if (i > 0)
                Assertions.AssertThat(ReferenceEquals(previous, source)).IsTrue();

            Assertions.AssertThat(source.Task.Status == GDTaskStatus.Pending).IsTrue();
            source.TrySetResult();
            await source.Task;
            previous = source;
        }
    }

    [TestCase, RequireGodotRuntime]
    public static async Task AutoResetCompletionSourceT_Reuse_MultipleCycles_ClearsPreviousResult()
    {
        await Constants.WaitForTaskReadyAsync();

        AutoResetGDTaskCompletionSource<int> previous = null!;
        for (var i = 0; i < PoolReuseCycles; i++)
        {
            var source = AutoResetGDTaskCompletionSource<int>.Create();
            if (i > 0)
                Assertions.AssertThat(ReferenceEquals(previous, source)).IsTrue();

            Assertions.AssertThat(source.Task.Status == GDTaskStatus.Pending).IsTrue();
            source.TrySetResult(i);
            Assertions.AssertThat(await source.Task).IsEqual(i);
            previous = source;
        }
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
    public static async Task AutoResetCompletionSource_WhenAll_MultipleSources()
    {
        await Constants.WaitForTaskReadyAsync();

        var first = AutoResetGDTaskCompletionSource.Create();
        var second = AutoResetGDTaskCompletionSource.Create();

        Constants.Delay().ContinueWith(() =>
        {
            first.TrySetResult();
            second.TrySetResult();
        }).Forget();

        await GDTask.WhenAll(first.Task, second.Task);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task AutoResetCompletionSourceT_WhenAll_PreservesResults()
    {
        await Constants.WaitForTaskReadyAsync();

        var first = AutoResetGDTaskCompletionSource<int>.Create();
        var second = AutoResetGDTaskCompletionSource<int>.Create();

        Constants.Delay().ContinueWith(() =>
        {
            first.TrySetResult(Constants.ReturnValue);
            second.TrySetResult(Constants.ReturnValue + 1);
        }).Forget();

        var (resultA, resultB) = await GDTask.WhenAll(first.Task, second.Task);
        Assertions.AssertThat(resultA).IsEqual(Constants.ReturnValue);
        Assertions.AssertThat(resultB).IsEqual(Constants.ReturnValue + 1);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task AutoResetCompletionSource_CompleteFromBackgroundThread()
    {
        await Constants.WaitForTaskReadyAsync();
        var source = AutoResetGDTaskCompletionSource.Create();

        GDTask.RunOnThreadPool(() =>
        {
            Thread.Sleep(50);
            source.TrySetResult();
        }).Forget();

        await source.Task;
    }

    [TestCase, RequireGodotRuntime]
    public static async Task AutoResetCompletionSourceT_CompleteFromBackgroundThread()
    {
        await Constants.WaitForTaskReadyAsync();
        var source = AutoResetGDTaskCompletionSource<int>.Create();

        GDTask.RunOnThreadPool(() =>
        {
            Thread.Sleep(50);
            source.TrySetResult(Constants.ReturnValue);
        }).Forget();

        Assertions.AssertThat(await source.Task).IsEqual(Constants.ReturnValue);
    }

    [TestCase, RequireGodotRuntime]
    public static void AutoResetCompletionSource_CreateFromCanceled()
    {
        using var cts = new CancellationTokenSource();
        var source = AutoResetGDTaskCompletionSource.CreateFromCanceled(cts.Token, out var token);
        Assertions.AssertThat(source.Task.Status == GDTaskStatus.Canceled).IsTrue();

        try
        {
            ((IGDTaskSource)source).GetResult(token);
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
            ((IGDTaskSource)source).GetResult(token);
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
        Assertions.AssertThat(((IGDTaskSource<int>)source).GetResult(token)).IsEqual(Constants.ReturnValue);
    }

    [TestCase, RequireGodotRuntime]
    public static void AutoResetCompletionSourceT_CreateFromCanceled()
    {
        using var cts = new CancellationTokenSource();
        var source = AutoResetGDTaskCompletionSource<int>.CreateFromCanceled(cts.Token, out var token);
        Assertions.AssertThat(source.Task.Status == GDTaskStatus.Canceled).IsTrue();

        try
        {
            ((IGDTaskSource<int>)source).GetResult(token);
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
            ((IGDTaskSource<int>)source).GetResult(token);
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
        ((IGDTaskSource)source).GetResult(token);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task AutoResetCompletionSource_CreateCompleted_AwaitTask()
    {
        await Constants.WaitForTaskReadyAsync();
        var source = AutoResetGDTaskCompletionSource.CreateCompleted(out _);
        await source.Task;
    }

    [TestCase, RequireGodotRuntime]
    public static async Task AutoResetCompletionSourceT_CreateFromResult_AwaitTask()
    {
        await Constants.WaitForTaskReadyAsync();
        var source = AutoResetGDTaskCompletionSource<int>.CreateFromResult(Constants.ReturnValue, out _);
        Assertions.AssertThat(await source.Task).IsEqual(Constants.ReturnValue);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task AutoResetCompletionSource_CreateFromCanceled_AwaitTask_Throws()
    {
        await Constants.WaitForTaskReadyAsync();
        using var cts = new CancellationTokenSource();
        var source = AutoResetGDTaskCompletionSource.CreateFromCanceled(cts.Token, out _);

        try
        {
            await source.Task;
        }
        catch (OperationCanceledException)
        {
            return;
        }

        throw new TestFailedException("OperationCanceledException not thrown");
    }

    [TestCase, RequireGodotRuntime]
    public static async Task AutoResetCompletionSource_CreateFromException_AwaitTask_Throws()
    {
        await Constants.WaitForTaskReadyAsync();
        var source = AutoResetGDTaskCompletionSource.CreateFromException(new ExpectedException(), out _);

        try
        {
            await source.Task;
        }
        catch (ExpectedException)
        {
            return;
        }

        throw new TestFailedException("ExpectedException not thrown");
    }

    [TestCase, RequireGodotRuntime]
    public static async Task AutoResetCompletionSource_ConcurrentTrySetResult_ObservedWinnerOnly()
    {
        await Constants.WaitForTaskReadyAsync();

        for (var iteration = 0; iteration < ConcurrentCompletionIterations; iteration++)
        {
            var source = AutoResetGDTaskCompletionSource.Create();
            var winner = RunConcurrentCompletion(index =>
                index == 0
                    ? source.TrySetResult()
                    : source.TrySetException(new ConcurrentCompletionException(index)));

            if (winner == 0)
            {
                Assertions.AssertThat(source.Task.Status == GDTaskStatus.Succeeded).IsTrue();
                await source.Task;
                continue;
            }

            Assertions.AssertThat(source.Task.Status == GDTaskStatus.Faulted).IsTrue();

            try
            {
                await source.Task;
            }
            catch (ConcurrentCompletionException exception)
            {
                Assertions.AssertThat(exception.Id).IsEqual(winner);
                continue;
            }

            throw new TestFailedException("Expected ConcurrentCompletionException was not thrown.");
        }
    }

    [TestCase, RequireGodotRuntime]
    public static async Task AutoResetCompletionSourceT_ConcurrentTrySetResult_ObservedWinnerOnly()
    {
        await Constants.WaitForTaskReadyAsync();

        for (var iteration = 0; iteration < ConcurrentCompletionIterations; iteration++)
        {
            var source = AutoResetGDTaskCompletionSource<int>.Create();
            var winner = RunConcurrentCompletion(index => source.TrySetResult(index));

            Assertions.AssertThat(source.Task.Status == GDTaskStatus.Succeeded).IsTrue();
            Assertions.AssertThat(await source.Task).IsEqual(winner);
        }
    }

    [TestCase, RequireGodotRuntime]
    public static async Task AutoResetCompletionSourceT_ConcurrentTrySetResultOrException_ObservedWinnerOnly()
    {
        await Constants.WaitForTaskReadyAsync();

        for (var iteration = 0; iteration < ConcurrentCompletionIterations; iteration++)
        {
            var source = AutoResetGDTaskCompletionSource<int>.Create();
            var winner = RunConcurrentCompletion(index =>
                index == 0
                    ? source.TrySetResult(index)
                    : source.TrySetException(new ConcurrentCompletionException(index)));

            if (winner == 0)
            {
                Assertions.AssertThat(source.Task.Status == GDTaskStatus.Succeeded).IsTrue();
                Assertions.AssertThat(await source.Task).IsEqual(0);
                continue;
            }

            Assertions.AssertThat(source.Task.Status == GDTaskStatus.Faulted).IsTrue();

            try
            {
                await source.Task;
            }
            catch (ConcurrentCompletionException exception)
            {
                Assertions.AssertThat(exception.Id).IsEqual(winner);
                continue;
            }

            throw new TestFailedException("Expected ConcurrentCompletionException was not thrown.");
        }
    }

    [TestCase, RequireGodotRuntime]
    public static async Task AutoResetCompletionSource_ConcurrentTrySetCanceled_ObservedWinnerOnly()
    {
        await Constants.WaitForTaskReadyAsync();

        for (var iteration = 0; iteration < ConcurrentCompletionIterations; iteration++)
        {
            var source = AutoResetGDTaskCompletionSource.Create();
            var tokenSources = CreateCancellationTokenSources();

            try
            {
                var winner = RunConcurrentCompletion(index => source.TrySetCanceled(tokenSources[index].Token));

                Assertions.AssertThat(source.Task.Status == GDTaskStatus.Canceled).IsTrue();

                try
                {
                    await source.Task;
                }
                catch (OperationCanceledException exception)
                {
                    Assertions.AssertThat(exception.CancellationToken.Equals(tokenSources[winner].Token)).IsTrue();
                    continue;
                }

                throw new TestFailedException("Expected OperationCanceledException was not thrown.");
            }
            finally
            {
                DisposeCancellationTokenSources(tokenSources);
            }
        }
    }

    private static int RunConcurrentCompletion(Func<int, bool> tryComplete)
    {
        var barrier = new Barrier(ConcurrentCompletionWriters + 1);
        var threads = new Thread[ConcurrentCompletionWriters];
        var winners = new bool[ConcurrentCompletionWriters];

        for (var index = 0; index < ConcurrentCompletionWriters; index++)
        {
            var capturedIndex = index;
            threads[index] = new Thread(() =>
            {
                barrier.SignalAndWait();
                winners[capturedIndex] = tryComplete(capturedIndex);
            });
            threads[index].Start();
        }

        barrier.SignalAndWait();

        foreach (var thread in threads)
            thread.Join();

        var winner = -1;
        for (var index = 0; index < winners.Length; index++)
        {
            if (!winners[index]) continue;

            if (winner != -1)
                throw new TestFailedException("More than one completion attempt reported success.");

            winner = index;
        }

        if (winner == -1)
            throw new TestFailedException("No completion attempt reported success.");

        return winner;
    }

    private static CancellationTokenSource[] CreateCancellationTokenSources()
    {
        var sources = new CancellationTokenSource[ConcurrentCompletionWriters];
        for (var index = 0; index < sources.Length; index++)
            sources[index] = new CancellationTokenSource();

        return sources;
    }

    private static void DisposeCancellationTokenSources(CancellationTokenSource[] sources)
    {
        foreach (var source in sources)
            source.Dispose();
    }

    private sealed class ConcurrentCompletionException(int id) : Exception($"Concurrent completion {id}")
    {
        public int Id { get; } = id;
    }
}

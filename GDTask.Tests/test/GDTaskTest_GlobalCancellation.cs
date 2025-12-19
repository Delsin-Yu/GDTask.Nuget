using System;
using System.Threading.Tasks;
using GdUnit4;

namespace GodotTask.Tests;

[TestSuite]
public class GDTaskTest_GlobalCancellation
{
    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_CancelAllTasks_Create()
    {
        var number = 0;
        var canceled = false;
        try
        {
            var task = GDTask.Create(async () =>
            {
                await GDTask.Delay(TimeSpan.FromSeconds(0.2));
                number++;
                await GDTask.Delay(TimeSpan.FromSeconds(0.2));
                number++;
            });
            await GDTask.Delay(TimeSpan.FromSeconds(0.3));
            GDTask.CancelAllTasks();
            await task;
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }

        Assertions.AssertThat(canceled).IsTrue();
        Assertions.AssertThat(number).IsEqual(1);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_CancelAllTasks_Delay()
    {
        await Constants.WaitForTaskReadyAsync();
        var canceled = false;
        try
        {
            var task = GDTask.Delay(TimeSpan.FromSeconds(1.0));
            await GDTask.Delay(TimeSpan.FromSeconds(0.1));
            GDTask.CancelAllTasks();
            await task;
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }

        Assertions.AssertThat(canceled).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_CancelAllTasks_DelayFrame()
    {
        await Constants.WaitForTaskReadyAsync();
        var canceled = false;
        try
        {
            var task = GDTask.DelayFrame(100);
            await GDTask.DelayFrame(2);
            GDTask.CancelAllTasks();
            await task;
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }

        Assertions.AssertThat(canceled).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_CancelAllTasks_Yield()
    {
        await Constants.WaitForTaskReadyAsync();
        var canceled = false;
        try
        {
            var task = GDTask.Create(async () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    await GDTask.Yield();
                }
            });
            await GDTask.Yield();
            GDTask.CancelAllTasks();
            await task;
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }

        Assertions.AssertThat(canceled).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_CancelAllTasks_NextFrame()
    {
        await Constants.WaitForTaskReadyAsync();
        var canceled = false;
        try
        {
            var task = GDTask.Create(async () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    await GDTask.NextFrame();
                }
            });
            await GDTask.NextFrame();
            GDTask.CancelAllTasks();
            await task;
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }

        Assertions.AssertThat(canceled).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_CancelAllTasks_WaitUntil()
    {
        await Constants.WaitForTaskReadyAsync();
        var canceled = false;
        var condition = false;
        try
        {
            var task = GDTask.WaitUntil(() => condition);
            await GDTask.Yield();
            GDTask.CancelAllTasks();
            await task;
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }

        Assertions.AssertThat(canceled).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_CancelAllTasks_WaitWhile()
    {
        await Constants.WaitForTaskReadyAsync();
        var canceled = false;
        var condition = true;
        try
        {
            var task = GDTask.WaitWhile(() => condition);
            await GDTask.Yield();
            GDTask.CancelAllTasks();
            await task;
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }

        Assertions.AssertThat(canceled).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_CancelAllTasks_WaitUntilValueChanged()
    {
        await Constants.WaitForTaskReadyAsync();
        var canceled = false;
        var testValue = new TestValue { Value = 1 };
        try
        {
            var task = GDTask.WaitUntilValueChanged(testValue, x => x.Value);
            await GDTask.Yield();
            GDTask.CancelAllTasks();
            await task;
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }

        Assertions.AssertThat(canceled).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_CancelAllTasks_WhenAll()
    {
        await Constants.WaitForTaskReadyAsync();
        var canceled = false;
        try
        {
            var task1 = GDTask.Delay(TimeSpan.FromSeconds(1.0));
            var task2 = GDTask.Delay(TimeSpan.FromSeconds(1.0));
            var task3 = GDTask.Delay(TimeSpan.FromSeconds(1.0));
            var whenAllTask = GDTask.WhenAll(task1, task2, task3);
            await GDTask.Delay(TimeSpan.FromSeconds(0.1));
            GDTask.CancelAllTasks();
            await whenAllTask;
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }

        Assertions.AssertThat(canceled).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_CancelAllTasks_WhenAllT()
    {
        await Constants.WaitForTaskReadyAsync();
        var canceled = false;
        try
        {
            var task1 = GDTask.Create(async () =>
            {
                await GDTask.Delay(TimeSpan.FromSeconds(1.0));
                return 1;
            });
            var task2 = GDTask.Create(async () =>
            {
                await GDTask.Delay(TimeSpan.FromSeconds(1.0));
                return 2;
            });
            var whenAllTask = GDTask.WhenAll(task1, task2);
            await GDTask.Delay(TimeSpan.FromSeconds(0.1));
            GDTask.CancelAllTasks();
            await whenAllTask;
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }

        Assertions.AssertThat(canceled).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_CancelAllTasks_WhenAny()
    {
        await Constants.WaitForTaskReadyAsync();
        var canceled = false;
        try
        {
            var task1 = GDTask.Delay(TimeSpan.FromSeconds(1.0));
            var task2 = GDTask.Delay(TimeSpan.FromSeconds(1.0));
            var whenAnyTask = GDTask.WhenAny(task1, task2);
            await GDTask.Yield();
            GDTask.CancelAllTasks();
            await whenAnyTask;
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }

        Assertions.AssertThat(canceled).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_CancelAllTasks_WhenAnyT()
    {
        await Constants.WaitForTaskReadyAsync();
        var canceled = false;
        try
        {
            var task1 = GDTask.Create(async () =>
            {
                await GDTask.Delay(TimeSpan.FromSeconds(1.0));
                return 1;
            });
            var task2 = GDTask.Create(async () =>
            {
                await GDTask.Delay(TimeSpan.FromSeconds(1.0));
                return 2;
            });
            var whenAnyTask = GDTask.WhenAny(task1, task2);
            await GDTask.Yield();
            GDTask.CancelAllTasks();
            await whenAnyTask;
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }

        Assertions.AssertThat(canceled).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_CancelAllTasks_CompletionSource()
    {
        await Constants.WaitForTaskReadyAsync();
        var canceled = false;
        try
        {
            var source = new GDTaskCompletionSource();
            var task = source.Task;
            await GDTask.Yield();
            GDTask.CancelAllTasks();
            await task;
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }

        Assertions.AssertThat(canceled).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_CancelAllTasks_CompletionSourceT()
    {
        await Constants.WaitForTaskReadyAsync();
        var canceled = false;
        try
        {
            var source = new GDTaskCompletionSource<int>();
            var task = source.Task;
            await GDTask.Yield();
            GDTask.CancelAllTasks();
            await task;
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }

        Assertions.AssertThat(canceled).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_CancelAllTasks_Run()
    {
        await Constants.WaitForTaskReadyAsync();
        var canceled = false;
        try
        {
            var task = GDTask.Create(async () =>
            {
                await GDTask.Delay(TimeSpan.FromSeconds(1.0));
            });
            await GDTask.Delay(TimeSpan.FromSeconds(0.1));
            GDTask.CancelAllTasks();
            await task;
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }

        Assertions.AssertThat(canceled).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_CancelAllTasks_RunT()
    {
        await Constants.WaitForTaskReadyAsync();
        var canceled = false;
        try
        {
            var task = GDTask.Create(async () =>
            {
                await GDTask.Delay(TimeSpan.FromSeconds(1.0));
                return 42;
            });
            await GDTask.Delay(TimeSpan.FromSeconds(0.1));
            GDTask.CancelAllTasks();
            await task;
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }

        Assertions.AssertThat(canceled).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_CancelAllTasks_Lazy()
    {
        await Constants.WaitForTaskReadyAsync();
        var canceled = false;
        try
        {
            var lazyTask = GDTask.Lazy(async () =>
            {
                await GDTask.Delay(TimeSpan.FromSeconds(1.0));
            });
            var task = lazyTask.Task;
            await GDTask.Delay(TimeSpan.FromSeconds(0.1));
            GDTask.CancelAllTasks();
            await task;
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }

        Assertions.AssertThat(canceled).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_CancelAllTasks_LazyT()
    {
        await Constants.WaitForTaskReadyAsync();
        var canceled = false;
        try
        {
            var lazyTask = GDTask.Lazy(async () =>
            {
                await GDTask.Delay(TimeSpan.FromSeconds(1.0));
                return 42;
            });
            var task = lazyTask.Task;
            await GDTask.Delay(TimeSpan.FromSeconds(0.1));
            GDTask.CancelAllTasks();
            await task;
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }

        Assertions.AssertThat(canceled).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_CancelAllTasks_Void()
    {
        await Constants.WaitForTaskReadyAsync();
        var canceled = false;
        var completed = false;

        GDTask.Void(async () =>
        {
            try
            {
                await GDTask.Delay(TimeSpan.FromSeconds(1.0));
                completed = true;
            }
            catch (OperationCanceledException)
            {
                canceled = true;
            }
        });

        await GDTask.Delay(TimeSpan.FromSeconds(0.1));
        GDTask.CancelAllTasks();
        await GDTask.Delay(TimeSpan.FromSeconds(0.2));

        Assertions.AssertThat(canceled).IsTrue();
        Assertions.AssertThat(completed).IsFalse();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_CancelAllTasks_ContinueWith()
    {
        await Constants.WaitForTaskReadyAsync();
        var canceled = false;
        var firstCompleted = false;
        var secondCompleted = false;

        try
        {
            var task = GDTask.Delay(TimeSpan.FromSeconds(0.2))
                .ContinueWith(() =>
                {
                    firstCompleted = true;
                    return GDTask.Delay(TimeSpan.FromSeconds(1.0));
                })
                .ContinueWith(() => secondCompleted = true);

            await GDTask.Delay(TimeSpan.FromSeconds(0.3));
            GDTask.CancelAllTasks();
            await task;
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }

        Assertions.AssertThat(canceled).IsTrue();
        Assertions.AssertThat(firstCompleted).IsTrue();
        Assertions.AssertThat(secondCompleted).IsFalse();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_CancelAllTasks_MultipleTasksPartialComplete()
    {
        await Constants.WaitForTaskReadyAsync();
        var task1Completed = false;
        var task2Completed = false;
        var task3Completed = false;
        var canceled1 = false;
        var canceled2 = false;
        var canceled3 = false;

        try
        {
            var task1 = GDTask.Create(async () =>
            {
                try
                {
                    await GDTask.Delay(TimeSpan.FromSeconds(0.1));
                    task1Completed = true;
                }
                catch (OperationCanceledException)
                {
                    canceled1 = true;
                    throw;
                }
            });

            var task2 = GDTask.Create(async () =>
            {
                try
                {
                    await GDTask.Delay(TimeSpan.FromSeconds(0.5));
                    task2Completed = true;
                }
                catch (OperationCanceledException)
                {
                    canceled2 = true;
                    throw;
                }
            });

            var task3 = GDTask.Create(async () =>
            {
                try
                {
                    await GDTask.Delay(TimeSpan.FromSeconds(1.0));
                    task3Completed = true;
                }
                catch (OperationCanceledException)
                {
                    canceled3 = true;
                    throw;
                }
            });

            await GDTask.Delay(TimeSpan.FromSeconds(0.2));
            GDTask.CancelAllTasks();

            await GDTask.WhenAll(
                task1.SuppressCancellationThrow(),
                task2.SuppressCancellationThrow(),
                task3.SuppressCancellationThrow()
            );
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        Assertions.AssertThat(task1Completed).IsTrue();
        Assertions.AssertThat(task2Completed).IsFalse();
        Assertions.AssertThat(task3Completed).IsFalse();
        Assertions.AssertThat(canceled1).IsFalse();
        Assertions.AssertThat(canceled2).IsTrue();
        Assertions.AssertThat(canceled3).IsTrue();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_CancelAllTasks_SwitchToThreadPool()
    {
        await Constants.WaitForTaskReadyAsync();
        var canceled = false;
        var switched = false;

        try
        {
            var task = GDTask.Create(async () =>
            {
                await GDTask.SwitchToThreadPool();
                switched = true;
                await GDTask.Delay(TimeSpan.FromSeconds(1.0));
            });

            await GDTask.Delay(TimeSpan.FromSeconds(0.1));
            GDTask.CancelAllTasks();
            await task;
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }

        Assertions.AssertThat(canceled).IsTrue();
        Assertions.AssertThat(switched).IsTrue();
    }

    private class TestValue
    {
        public int Value { get; set; }
    }
}
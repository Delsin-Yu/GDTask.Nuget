using System.Threading;
using System.Threading.Tasks;
using GdUnit4;

namespace GodotTask.Tests;

[TestSuite]
public class GDTaskTest_Interop
{
    [TestCase, RequireGodotRuntime]
    public static async Task Task_AsGDTask_CurrentContext()
    {
        await Constants.WaitForTaskReadyAsync();
        using (new ScopedStopwatch())
        {
            await Task.Delay(Constants.DelayTimeSpan).AsGDTask();
        }
    }

    [TestCase, RequireGodotRuntime]
    public static async Task Task_AsGDTask_SchedulerContext()
    {
        await Constants.WaitForTaskReadyAsync();
        await GDTask.SwitchToThreadPool();
        using (new ScopedStopwatch())
        {
            await Task.Delay(Constants.DelayTimeSpan).AsGDTask(false);
        }

        Assertions.AssertThat(Thread.CurrentThread.IsThreadPoolThread);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task TaskT_AsGDTask_CurrentContext()
    {
        await Constants.WaitForTaskReadyAsync();
        int result;
        using (new ScopedStopwatch())
        {
            result = await Task.Delay(Constants.DelayTimeSpan).ContinueWith(task => Constants.ReturnValue).AsGDTask();
        }

        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
    }

    [TestCase, RequireGodotRuntime]
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

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_AsTask()
    {
        await Constants.WaitForTaskReadyAsync();
        using (new ScopedStopwatch()) await Constants.Delay().AsTask();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTaskT_AsTask()
    {
        await Constants.WaitForTaskReadyAsync();
        int result;
        using (new ScopedStopwatch()) result = await Constants.DelayWithReturn().AsTask();
        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_AsValueTask()
    {
        await Constants.WaitForTaskReadyAsync();
        using (new ScopedStopwatch()) await Constants.Delay().AsValueTask();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTaskT_AsValueTask()
    {
        await Constants.WaitForTaskReadyAsync();
        int result;
        using (new ScopedStopwatch()) result = await Constants.DelayWithReturn().AsValueTask();
        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_ToAsyncLazy()
    {
        await Constants.WaitForTaskReadyAsync();
        using (new ScopedStopwatch()) await Constants.Delay().ToAsyncLazy();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTaskT_ToAsyncLazy()
    {
        await Constants.WaitForTaskReadyAsync();
        int result;
        using (new ScopedStopwatch()) result = await Constants.DelayWithReturn().ToAsyncLazy();
        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
    }
}
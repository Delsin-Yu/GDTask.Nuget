using System.Threading;
using System.Threading.Tasks;
using GdUnit4;

namespace Fractural.Tasks.Tests;

[TestSuite]
public class GDTaskTest_Core
{
    private const int ReturnValue = 5;
    private static GDTask Delay(CancellationToken? cancellationToken = default) => 
        GDTask.Delay(100, cancellationToken: cancellationToken ?? CancellationToken.None);
    private static GDTask<int> DelayWithReturn(CancellationToken? cancellationToken = default) => 
        GDTask.Delay(100, cancellationToken: cancellationToken ?? CancellationToken.None).ContinueWith(() => ReturnValue);

    [TestCase]
    public async Task GDTask_Status()
    {
        var gdTask = Delay();
        Assertions.AssertThat(gdTask.Status).Equals(GDTaskStatus.Pending);
        await gdTask;
    }

    [TestCase]
    public async Task GDTask_ToString()
    {
        var gdTask = Delay();
        Assertions.AssertThat(gdTask.ToString()).IsEqual($"({GDTaskStatus.Pending})");
        await gdTask;
    }

    [TestCase]
    public async Task GDTask_GetAwaiter_OnCompleted()
    {
        var gdTask = Delay();
        var completed = false;
        gdTask.GetAwaiter().OnCompleted(() => completed = true);
        await GDTask.WaitUntil(() => completed);
    }

    [TestCase]
    public async Task GDTask_SuppressCancellationThrow()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(10);
        var gdTask = Delay(cancellationTokenSource.Token).SuppressCancellationThrow();
        await gdTask;
    }

    [TestCase]
    public async Task GDTask_Preserve()
    {
        var gdTask = Delay();
        gdTask = gdTask.Preserve();
        await gdTask;
        await gdTask;
        await gdTask;
        await gdTask;
    }

    [TestCase]
    public async Task GDTask_AsAsyncUnitGDTask()
    {
        var asyncUnitGDTask = Delay().AsAsyncUnitGDTask();
        var result = await asyncUnitGDTask;
        Assertions.AssertThat(result).Equals(AsyncUnit.Default);
    }

    [TestCase]
    public async Task GDTaskT_Status()
    {
        var gdTask = DelayWithReturn();
        Assertions.AssertThat(gdTask.Status).Equals(GDTaskStatus.Pending);
        await gdTask;
    }

    [TestCase]
    public async Task GDTaskT_ToString()
    {
        var gdTask = DelayWithReturn();
        Assertions.AssertThat(gdTask.ToString()).IsEqual($"({GDTaskStatus.Pending})");
        await gdTask;
    }
    
    [TestCase]
    public async Task GDTaskT_Result()
    {
        Assertions
            .AssertThat(await DelayWithReturn())
            .IsEqual(ReturnValue);
    }

    [TestCase]
    public async Task GDTaskT_GetAwaiter_OnCompleted()
    {
        var gdTask = DelayWithReturn();
        var completed = false;
        gdTask.GetAwaiter().OnCompleted(() => completed = true);
        await GDTask.WaitUntil(() => completed);
    }
    
    
    [TestCase]
    public async Task GDTaskT_SuppressCancellationThrow()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(10);
        var gdTask = DelayWithReturn(cancellationTokenSource.Token).SuppressCancellationThrow();
        await gdTask;
    }

    [TestCase]
    public async Task GDTaskT_Preserve()
    {
        var gdTask = DelayWithReturn();
        gdTask = gdTask.Preserve();
        Assertions.AssertThat(await gdTask).IsEqual(ReturnValue);
        Assertions.AssertThat(await gdTask).IsEqual(ReturnValue);
        Assertions.AssertThat(await gdTask).IsEqual(ReturnValue);
        Assertions.AssertThat(await gdTask).IsEqual(ReturnValue);
    }
}
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global
using System.Threading;
using System.Threading.Tasks;
using GdUnit4;
using Godot;

namespace GodotTask.Tests;

[TestSuite]
public class GDTaskTest_Core
{

    [TestCase]
    public static async Task GDTask_Status()
    {
        var gdTask = Constants.Delay();
        Assertions.AssertThat(gdTask.Status).Equals(GDTaskStatus.Pending);
        await gdTask;
    }

    [TestCase]
    public static async Task GDTask_ToString()
    {
        var gdTask = Constants.Delay();
        Assertions.AssertThat(gdTask.ToString()).IsEqual($"({GDTaskStatus.Pending})");
        await gdTask;
    }

    [TestCase]
    public static async Task GDTask_GetAwaiter_OnCompleted()
    {
        var gdTask = Constants.Delay();
        var completed = false;
        gdTask.GetAwaiter().OnCompleted(() => completed = true);
        await GDTask.WaitUntil(() => completed);
    }

    public static async Task GDTask_GetAwaiter_Target() {
        var godotObject = new GodotObject();
        var gdTask = Constants.Delay();
        gdTask.GetAwaiter().OnCompleted(() => godotObject.Free());
        await GDTask.WaitUntil(godotObject, () => false);
        await GDTask.WaitWhile(godotObject, () => true);
    }

    [TestCase]
    public static async Task GDTask_SuppressCancellationThrow()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(10);
        var gdTask = Constants.Delay(cancellationTokenSource.Token).SuppressCancellationThrow();
        await gdTask;
    }

    [TestCase]
    public static async Task GDTask_Preserve()
    {
        var gdTask = Constants.Delay();
        gdTask = gdTask.Preserve();
        await gdTask;
        await gdTask;
        await gdTask;
        await gdTask;
    }

    [TestCase]
    public static async Task GDTask_AsAsyncUnitGDTask()
    {
        var asyncUnitGDTask = Constants.Delay().AsAsyncUnitGDTask();
        var result = await asyncUnitGDTask;
        Assertions.AssertThat(result).Equals(AsyncUnit.Default);
    }

    [TestCase]
    public static async Task GDTaskT_Status()
    {
        var gdTask = Constants.DelayWithReturn();
        Assertions.AssertThat(gdTask.Status).Equals(GDTaskStatus.Pending);
        await gdTask;
    }

    [TestCase]
    public static async Task GDTaskT_ToString()
    {
        var gdTask = Constants.DelayWithReturn();
        Assertions.AssertThat(gdTask.ToString()).IsEqual($"({GDTaskStatus.Pending})");
        await gdTask;
    }
    
    [TestCase]
    public static async Task GDTaskT_Result()
    {
        Assertions
            .AssertThat(await Constants.DelayWithReturn())
            .IsEqual(Constants.ReturnValue);
    }

    [TestCase]
    public static async Task GDTaskT_GetAwaiter_OnCompleted()
    {
        var gdTask = Constants.DelayWithReturn();
        var completed = false;
        gdTask.GetAwaiter().OnCompleted(() => completed = true);
        await GDTask.WaitUntil(() => completed);
    }
    
    
    [TestCase]
    public static async Task GDTaskT_SuppressCancellationThrow()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(10);
        var gdTask = Constants.DelayWithReturn(cancellationTokenSource.Token).SuppressCancellationThrow();
        await gdTask;
    }

    [TestCase]
    public static async Task GDTaskT_Preserve()
    {
        var gdTask = Constants.DelayWithReturn();
        gdTask = gdTask.Preserve();
        Assertions.AssertThat(await gdTask).IsEqual(Constants.ReturnValue);
        Assertions.AssertThat(await gdTask).IsEqual(Constants.ReturnValue);
        Assertions.AssertThat(await gdTask).IsEqual(Constants.ReturnValue);
        Assertions.AssertThat(await gdTask).IsEqual(Constants.ReturnValue);
    }
}
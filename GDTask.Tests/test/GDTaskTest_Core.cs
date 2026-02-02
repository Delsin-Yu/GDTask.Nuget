// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global
using System;
using System.Threading;
using System.Threading.Tasks;
using GdUnit4;
using Godot;

namespace GodotTask.Tests;

[TestSuite]
public class GDTaskTest_Core
{
    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Status()
    {
        await Constants.WaitForTaskReadyAsync();
        var gdTask = Constants.Delay();
        Assertions.AssertThat(gdTask.Status).Equals(GDTaskStatus.Pending);
        await gdTask;
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_ToString()
    {
        await Constants.WaitForTaskReadyAsync();
        var gdTask = Constants.Delay();
        Assertions.AssertThat(gdTask.ToString()).IsEqual($"({GDTaskStatus.Pending})");
        await gdTask;
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_GetAwaiter_OnCompleted()
    {
        await Constants.WaitForTaskReadyAsync();
        var gdTask = Constants.Delay();
        var completed = false;
        gdTask.GetAwaiter().OnCompleted(() => completed = true);
        await GDTask.WaitUntil(() => completed);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_SuppressCancellationThrow()
    {
        await Constants.WaitForTaskReadyAsync();
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(10);
        var gdTask = Constants.Delay(cancellationTokenSource.Token).SuppressCancellationThrow();
        await gdTask;
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_Preserve()
    {
        await Constants.WaitForTaskReadyAsync();
        var gdTask = Constants.Delay();
        gdTask = gdTask.Preserve();
        await gdTask;
        await gdTask;
        await gdTask;
        await gdTask;
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_AsAsyncUnitGDTask()
    {
        await Constants.WaitForTaskReadyAsync();
        var asyncUnitGDTask = Constants.Delay().AsAsyncUnitGDTask();
        var result = await asyncUnitGDTask;
        Assertions.AssertThat(result).Equals(AsyncUnit.Default);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTaskT_Status()
    {
        await Constants.WaitForTaskReadyAsync();
        var gdTask = Constants.DelayWithReturn();
        Assertions.AssertThat(gdTask.Status).Equals(GDTaskStatus.Pending);
        await gdTask;
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTaskT_ToString()
    {
        await Constants.WaitForTaskReadyAsync();
        var gdTask = Constants.DelayWithReturn();
        Assertions.AssertThat(gdTask.ToString()).IsEqual($"({GDTaskStatus.Pending})");
        await gdTask;
    }

    [TestCase]
    public static async Task GDTaskT_Result()
    {
        await Constants.WaitForTaskReadyAsync();
        Assertions
            .AssertThat(await Constants.DelayWithReturn())
            .IsEqual(Constants.ReturnValue);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTaskT_GetAwaiter_OnCompleted()
    {
        await Constants.WaitForTaskReadyAsync();
        var gdTask = Constants.DelayWithReturn();
        var completed = false;
        gdTask.GetAwaiter().OnCompleted(() => completed = true);
        await GDTask.WaitUntil(() => completed);
    }


    [TestCase, RequireGodotRuntime]
    public static async Task GDTaskT_SuppressCancellationThrow()
    {
        await Constants.WaitForTaskReadyAsync();
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(10);
        var gdTask = Constants.DelayWithReturn(cancellationTokenSource.Token).SuppressCancellationThrow();
        await gdTask;
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTaskT_Preserve()
    {
        await Constants.WaitForTaskReadyAsync();
        var gdTask = Constants.DelayWithReturn();
        gdTask = gdTask.Preserve();
        Assertions.AssertThat(await gdTask).IsEqual(Constants.ReturnValue);
        Assertions.AssertThat(await gdTask).IsEqual(Constants.ReturnValue);
        Assertions.AssertThat(await gdTask).IsEqual(Constants.ReturnValue);
        Assertions.AssertThat(await gdTask).IsEqual(Constants.ReturnValue);
    }
}
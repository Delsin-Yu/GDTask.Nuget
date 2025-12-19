using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GdUnit4;

namespace GodotTask.Tests;

[TestSuite]
public class GDTaskTest_WhenAll_WhenAny
{
    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_WhenAll_Params()
    {
        await Constants.WaitForTaskReadyAsync();
        await GDTask.WhenAll(new[] { GDTask.CompletedTask, Constants.Delay() });
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_WhenAllT_Params()
    {
        await Constants.WaitForTaskReadyAsync();
        var results = await GDTask.WhenAll(new[] { Constants.DelayWithReturn(), GDTask.FromResult(Constants.ReturnValue) });
        Assertions.AssertThat(results.All(value => value == Constants.ReturnValue));
    }
    
    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_WhenAll_ParamsSpan()
    {
        await Constants.WaitForTaskReadyAsync();
        await GDTask.WhenAll((ReadOnlySpan<GDTask>) [GDTask.CompletedTask, Constants.Delay()]);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_WhenAllT_ParamsSpan()
    {
        await Constants.WaitForTaskReadyAsync();
        var results = await GDTask.WhenAll((ReadOnlySpan<GDTask<int>>) [Constants.DelayWithReturn(), GDTask.FromResult(Constants.ReturnValue)]);
        Assertions.AssertThat(results.All(value => value == Constants.ReturnValue));
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_WhenAll_Enumerable()
    {
        await Constants.WaitForTaskReadyAsync();
        await GDTask.WhenAll((IEnumerable<GDTask>)new[] { GDTask.CompletedTask, Constants.Delay() });
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_WhenAllT_Enumerable()
    {
        await Constants.WaitForTaskReadyAsync();
        var results = await GDTask.WhenAll((IEnumerable<GDTask<int>>)new[] { Constants.DelayWithReturn(), GDTask.FromResult(Constants.ReturnValue) });
        Assertions.AssertThat(results.All(value => value == Constants.ReturnValue));
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_WhenAllT_Tuple()
    {
        await Constants.WaitForTaskReadyAsync();
        var (resultA, resultB) = await GDTask.WhenAll(Constants.DelayWithReturn(), GDTask.FromResult(Constants.ReturnValue));
        Assertions.AssertThat(resultA).IsEqual(Constants.ReturnValue);
        Assertions.AssertThat(resultB).IsEqual(Constants.ReturnValue);
    }   
    
    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_WhenAny_Params()
    {
        await Constants.WaitForTaskReadyAsync();
        var winArgumentIndex = await GDTask.WhenAny(new[] { GDTask.Never(CancellationToken.None), Constants.Delay() });
        Assertions.AssertThat(winArgumentIndex).IsEqual(1);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_WhenAnyT_Params()
    {
        await Constants.WaitForTaskReadyAsync();
        var (winArgumentIndex, result) = await GDTask.WhenAny(new[] { Constants.DelayWithReturn(), GDTask.Never<int>(CancellationToken.None) });
        Assertions.AssertThat(winArgumentIndex).IsEqual(0);
        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
    }
    
    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_WhenAny_ParamsSpan()
    {
        await Constants.WaitForTaskReadyAsync();
        var winArgumentIndex = await GDTask.WhenAny((ReadOnlySpan<GDTask>) [GDTask.Never(CancellationToken.None), Constants.Delay()]);
        Assertions.AssertThat(winArgumentIndex).IsEqual(1);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_WhenAnyT_ParamsSpan()
    {
        await Constants.WaitForTaskReadyAsync();
        var (winArgumentIndex, result) = await GDTask.WhenAny((ReadOnlySpan<GDTask<int>>) [Constants.DelayWithReturn(), GDTask.Never<int>(CancellationToken.None)]);
        Assertions.AssertThat(winArgumentIndex).IsEqual(0);
        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_WhenAny_Enumerable()
    {
        await Constants.WaitForTaskReadyAsync();
        var winArgumentIndex = await GDTask.WhenAny((IEnumerable<GDTask>)new[] { GDTask.Never(CancellationToken.None), Constants.Delay() });
        Assertions.AssertThat(winArgumentIndex).IsEqual(1);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_WhenAnyT_Enumerable()
    {
        await Constants.WaitForTaskReadyAsync();
        var (winArgumentIndex, result) = await GDTask.WhenAny((IEnumerable<GDTask<int>>)new[] { Constants.DelayWithReturn(), GDTask.Never<int>(CancellationToken.None) });
        Assertions.AssertThat(winArgumentIndex).IsEqual(0);
        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
    }

    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_WhenAnyT_Tuple()
    {
        await Constants.WaitForTaskReadyAsync();
        var (winArgumentIndex, result1, _) = await GDTask.WhenAny(Constants.DelayWithReturn(), GDTask.Never<int>(CancellationToken.None));
        Assertions.AssertThat(winArgumentIndex).IsEqual(0);
        Assertions.AssertThat(result1).IsEqual(Constants.ReturnValue);
    }
}
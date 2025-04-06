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
    [TestCase]
    public static async Task GDTask_WhenAll_Params()
    {
        await GDTask.WhenAll(new[] { GDTask.CompletedTask, Constants.Delay() });
    }

    [TestCase]
    public static async Task GDTask_WhenAllT_Params()
    {
        var results = await GDTask.WhenAll(new[] { Constants.DelayWithReturn(), GDTask.FromResult(Constants.ReturnValue) });
        Assertions.AssertThat(results.All(value => value == Constants.ReturnValue));
    }
    
    [TestCase]
    public static async Task GDTask_WhenAll_ParamsSpan()
    {
        await GDTask.WhenAll((ReadOnlySpan<GDTask>) [GDTask.CompletedTask, Constants.Delay()]);
    }

    [TestCase]
    public static async Task GDTask_WhenAllT_ParamsSpan()
    {
        var results = await GDTask.WhenAll((ReadOnlySpan<GDTask<int>>) [Constants.DelayWithReturn(), GDTask.FromResult(Constants.ReturnValue)]);
        Assertions.AssertThat(results.All(value => value == Constants.ReturnValue));
    }

    [TestCase]
    public static async Task GDTask_WhenAll_Enumerable()
    {
        await GDTask.WhenAll((IEnumerable<GDTask>)new[] { GDTask.CompletedTask, Constants.Delay() });
    }

    [TestCase]
    public static async Task GDTask_WhenAllT_Enumerable()
    {
        var results = await GDTask.WhenAll((IEnumerable<GDTask<int>>)new[] { Constants.DelayWithReturn(), GDTask.FromResult(Constants.ReturnValue) });
        Assertions.AssertThat(results.All(value => value == Constants.ReturnValue));
    }

    [TestCase]
    public static async Task GDTask_WhenAllT_Tuple()
    {
        var (resultA, resultB) = await GDTask.WhenAll(Constants.DelayWithReturn(), GDTask.FromResult(Constants.ReturnValue));
        Assertions.AssertThat(resultA).IsEqual(Constants.ReturnValue);
        Assertions.AssertThat(resultB).IsEqual(Constants.ReturnValue);
    }   
    
    [TestCase]
    public static async Task GDTask_WhenAny_Params()
    {
        var winArgumentIndex = await GDTask.WhenAny(new[] { GDTask.Never(CancellationToken.None), Constants.Delay() });
        Assertions.AssertThat(winArgumentIndex).IsEqual(1);
    }

    [TestCase]
    public static async Task GDTask_WhenAnyT_Params()
    {
        var (winArgumentIndex, result) = await GDTask.WhenAny(new[] { Constants.DelayWithReturn(), GDTask.Never<int>(CancellationToken.None) });
        Assertions.AssertThat(winArgumentIndex).IsEqual(0);
        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
    }
    
    [TestCase]
    public static async Task GDTask_WhenAny_ParamsSpan()
    {
        var winArgumentIndex = await GDTask.WhenAny((ReadOnlySpan<GDTask>) [GDTask.Never(CancellationToken.None), Constants.Delay()]);
        Assertions.AssertThat(winArgumentIndex).IsEqual(1);
    }

    [TestCase]
    public static async Task GDTask_WhenAnyT_ParamsSpan()
    {
        var (winArgumentIndex, result) = await GDTask.WhenAny((ReadOnlySpan<GDTask<int>>) [Constants.DelayWithReturn(), GDTask.Never<int>(CancellationToken.None)]);
        Assertions.AssertThat(winArgumentIndex).IsEqual(0);
        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
    }

    [TestCase]
    public static async Task GDTask_WhenAny_Enumerable()
    {
        var winArgumentIndex = await GDTask.WhenAny((IEnumerable<GDTask>)new[] { GDTask.Never(CancellationToken.None), Constants.Delay() });
        Assertions.AssertThat(winArgumentIndex).IsEqual(1);
    }

    [TestCase]
    public static async Task GDTask_WhenAnyT_Enumerable()
    {
        var (winArgumentIndex, result) = await GDTask.WhenAny((IEnumerable<GDTask<int>>)new[] { Constants.DelayWithReturn(), GDTask.Never<int>(CancellationToken.None) });
        Assertions.AssertThat(winArgumentIndex).IsEqual(0);
        Assertions.AssertThat(result).IsEqual(Constants.ReturnValue);
    }

    [TestCase]
    public static async Task GDTask_WhenAnyT_Tuple()
    {
        var (winArgumentIndex, result1, _) = await GDTask.WhenAny(Constants.DelayWithReturn(), GDTask.Never<int>(CancellationToken.None));
        Assertions.AssertThat(winArgumentIndex).IsEqual(0);
        Assertions.AssertThat(result1).IsEqual(Constants.ReturnValue);
    }
}
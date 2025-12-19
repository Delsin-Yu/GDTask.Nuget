using System.Collections.Generic;
using System.Threading.Tasks;
using GdUnit4;

namespace GodotTask.Tests;

[TestSuite]
public class GDTaskTest_Shorthand
{
    [TestCase, RequireGodotRuntime]
    public static async Task GetAwaiter_GDTaskArray()
    {
        await Constants.WaitForTaskReadyAsync();
        await new[] { Constants.Delay(), Constants.Delay() };
    }
    
    [TestCase, RequireGodotRuntime]
    public static async Task GetAwaiter_GDTaskIEnumerable()
    {
        await Constants.WaitForTaskReadyAsync();
        await RepeatedEnumerable();
        return;

        static IEnumerable<GDTask> RepeatedEnumerable()
        {
            for (var i = 0; i < 20; i++)
                yield return Constants.Delay();
        }
    }
    
    [TestCase, RequireGodotRuntime]
    public static async Task GetAwaiter_GDTaskTArray()
    {
        await Constants.WaitForTaskReadyAsync();
        var result = await new[] { Constants.DelayWithReturn(), Constants.DelayWithReturn() };
        Assertions.AssertThat(result[0]).IsEqual(Constants.ReturnValue);
        Assertions.AssertThat(result[1]).IsEqual(Constants.ReturnValue);
    }
    
    [TestCase, RequireGodotRuntime]
    public static async Task GetAwaiter_GDTaskTIEnumerable()
    {
        await Constants.WaitForTaskReadyAsync();
        await RepeatedEnumerable();
        return;

        static IEnumerable<GDTask<int>> RepeatedEnumerable()
        {
            for (var i = 0; i < 20; i++)
                yield return Constants.DelayWithReturn();
        }
    }
    
    [TestCase, RequireGodotRuntime]
    public static async Task GetAwaiter_GDTaskTuple()
    {
        var (result1, result2) = await (Constants.DelayWithReturn(), Constants.DelayWithReturn());
        Assertions.AssertThat(result1).IsEqual(Constants.ReturnValue);
        Assertions.AssertThat(result2).IsEqual(Constants.ReturnValue);
    }

}
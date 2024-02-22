using System.Collections.Generic;
using System.Threading.Tasks;
using GdUnit4;

namespace GodotTask.Tests;

[TestSuite]
public class GDTaskTest_Shorthand
{
    [TestCase]
    public static async Task GetAwaiter_GDTaskArray()
    {
        await new[] { Constants.Delay(), Constants.Delay() };
    }
    
    [TestCase]
    public static async Task GetAwaiter_GDTaskIEnumerable()
    {
        await RepeatedEnumerable();
        return;

        static IEnumerable<GDTask> RepeatedEnumerable()
        {
            for (var i = 0; i < 20; i++)
                yield return Constants.Delay();
        }
    }
    
    [TestCase]
    public static async Task GetAwaiter_GDTaskTArray()
    {
        var result = await new[] { Constants.DelayWithReturn(), Constants.DelayWithReturn() };
        Assertions.AssertThat(result[0]).IsEqual(Constants.ReturnValue);
        Assertions.AssertThat(result[1]).IsEqual(Constants.ReturnValue);
    }
    
    [TestCase]
    public static async Task GetAwaiter_GDTaskTIEnumerable()
    {
        await RepeatedEnumerable();
        return;

        static IEnumerable<GDTask<int>> RepeatedEnumerable()
        {
            for (var i = 0; i < 20; i++)
                yield return Constants.DelayWithReturn();
        }
    }
    
    [TestCase]
    public static async Task GetAwaiter_GDTaskTuple()
    {
        var (result1, result2) = await (Constants.DelayWithReturn(), Constants.DelayWithReturn());
        Assertions.AssertThat(result1).IsEqual(Constants.ReturnValue);
        Assertions.AssertThat(result2).IsEqual(Constants.ReturnValue);
    }

}
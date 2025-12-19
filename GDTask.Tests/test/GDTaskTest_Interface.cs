using System.Threading.Tasks;
using GdUnit4;

namespace GodotTask.Tests;

[TestSuite]
public class GDTaskTest_Interface
{
    [TestCase, RequireGodotRuntime]
    public static async Task IGDTask_Void()
    {
        await Constants.WaitForTaskReadyAsync();
        await (IGDTask)Constants.Delay();
    }
    
    [TestCase, RequireGodotRuntime]
    public static async Task IGDTask_T()
    {
        await Constants.WaitForTaskReadyAsync();
        var value = await (IGDTask)Constants.DelayWithReturn();
        Assertions.AssertThat((int)value).IsEqual(Constants.ReturnValue);
    }
}
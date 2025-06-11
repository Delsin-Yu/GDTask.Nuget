using System.Threading.Tasks;
using GdUnit4;

namespace GodotTask.Tests;

[TestSuite]
public class GDTaskTest_Interface
{
    [TestCase]
    public static async Task IGDTask_Void()
    {
        await (IGDTask)Constants.Delay();
    }
    
    [TestCase]
    public static async Task IGDTask_T()
    {
        var value = await (IGDTask)Constants.DelayWithReturn();
        Assertions.AssertThat((int)value).IsEqual(Constants.ReturnValue);
    }
}
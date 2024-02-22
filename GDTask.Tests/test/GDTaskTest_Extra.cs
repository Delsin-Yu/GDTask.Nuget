using System.Threading;
using System.Threading.Tasks;
using GdUnit4;

namespace GodotTask.Tests;

[TestSuite]
public class GDTaskTest_Extra
{
    [TestCase]
    public static async Task FirstCallIsSwitchToThreadIssue()
    {
        var mainThreadId = System.Environment.CurrentManagedThreadId;
        Assertions.AssertThat(Thread.CurrentThread.IsThreadPoolThread).IsFalse();

        await GDTask.SwitchToThreadPool();

        Assertions.AssertThat(System.Environment.CurrentManagedThreadId).IsNotEqual(mainThreadId);
        Assertions.AssertThat(Thread.CurrentThread.IsThreadPoolThread).IsTrue();

        await GDTask.SwitchToMainThread();
		
        Assertions.AssertThat(System.Environment.CurrentManagedThreadId).IsEqual(mainThreadId);
        Assertions.AssertThat(Thread.CurrentThread.IsThreadPoolThread).IsFalse();
    }
}
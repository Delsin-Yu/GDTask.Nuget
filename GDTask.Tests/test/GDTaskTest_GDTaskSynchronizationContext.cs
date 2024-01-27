using System.Threading;
using System.Threading.Tasks;
using GdUnit4;

namespace GodotTasks.Tasks.Tests;

[TestSuite]
public class GDTaskTest_GDTaskSynchronizationContext
{
    [TestCase]
    public static async Task GDTask_Context()
    {
        _ = GDTaskPlayerLoopAutoload.Global;
        await GDTask.SwitchToMainThread();
        var currentContext = GDTaskSynchronizationContext.Current;
        await GDTask.SwitchToThreadPool();
        currentContext.Post(
            _ => Assertions.AssertThat(GDTaskPlayerLoopAutoload.IsMainThread).IsTrue(),
            null
        );
    }
}
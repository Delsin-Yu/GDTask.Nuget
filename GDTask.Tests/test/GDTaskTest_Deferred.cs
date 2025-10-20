using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GdUnit4;

namespace GodotTask.Tests;

[TestSuite]
public class GDTaskTest_Deferred
{
    [TestCase]
    public static async Task GDTask_Deferred()
    {
        await GDTask.NextFrame(PlayerLoopTiming.Process);
        using (new ScopedFrameCount(0, PlayerLoopTiming.Process))
        {
            await GDTask.Deferred();
        }
    }
    
    [TestCase]
    public static async Task GDTask_Deferred_Order()
    {
        await GDTask.NextFrame(PlayerLoopTiming.Process);

        var list = new List<string>();
        
        var nextFrameTask = GDTask.NextFrame(PlayerLoopTiming.Process).ContinueWith(() => list.Add("NextFrame"));
        var deferredTask = GDTask.Deferred().ToGDTask().ContinueWith(() => list.Add("Deferred"));
        
        await GDTask.WhenAll(nextFrameTask, deferredTask);

        Assertions.AssertThat(list.SequenceEqual(["Deferred", "NextFrame"])).IsTrue();
    }
    
    [TestCase]
    public static async Task GDTask_Deferred_Token()
    {
        await GDTask.NextFrame(PlayerLoopTiming.Process);
        var source = new CancellationTokenSource();
        await source.CancelAsync();
        try
        {
            await GDTask.Deferred(source.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        
        throw new TestFailedException("Deferred Instructions not canceled");
    }


}
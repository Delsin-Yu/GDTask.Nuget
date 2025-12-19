using System;
using System.Threading;
using System.Threading.Tasks;
using GdUnit4;
using Godot;

namespace GodotTask.Tests;

[TestSuite]
public class GDTaskTest_Wait
{
    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_WaitUntil()
    {
        await Constants.WaitForTaskReadyAsync();
        var finished = false;
        Constants.Delay().ContinueWith(() => finished = true).Forget();
        await GDTask.WaitUntil(() => finished);
    }
    
    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_WaitWhile()
    {
        await Constants.WaitForTaskReadyAsync();
        var finished = true;
        Constants.Delay().ContinueWith(() => finished = false).Forget();
        await GDTask.WaitWhile(() => finished);
    }
    
    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_WaitUntil_With_GodotObject()
    {
        await Constants.WaitForTaskReadyAsync();
        var godotObject = new GodotObject();
        godotObject.Free();
        try
        {
            await GDTask.WaitUntil(godotObject, () => true);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        
        throw new TestFailedException("Operation not Canceled");
    }
    
    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_WaitWhile_With_GodotObject()
    {
        await Constants.WaitForTaskReadyAsync();
        var godotObject = new GodotObject();
        godotObject.Free();
        try
        {
            await GDTask.WaitWhile(godotObject, () => true);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        
        throw new TestFailedException("Operation not Canceled");
    }
    
    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_WaitUntilCanceled()
    {
        await Constants.WaitForTaskReadyAsync();
        var source = new CancellationTokenSource();
        source.CancelAfter(Constants.DelayTimeSpan);
        await GDTask.WaitUntilCanceled(source.Token);
    }
    
    [TestCase, RequireGodotRuntime]
    public static async Task GDTask_WaitUntilValueChanged()
    {
        await Constants.WaitForTaskReadyAsync();
        var value = new InternalValue();
        Constants.Delay().ContinueWith(() => value.Value = 0).Forget();
        await GDTask.WaitUntilValueChanged(value, data => data.Value);
    }

    private class InternalValue
    {
        public int Value { get; set; } = Constants.ReturnValue;
    }

}
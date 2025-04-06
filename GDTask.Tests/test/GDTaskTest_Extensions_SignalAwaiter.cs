using System.Threading.Tasks;
using GdUnit4;
using Godot;

namespace GodotTask.Tests;

[TestSuite]
public partial class GDTaskTest_SignalAwaiter
{
    [TestCase]
    public static async Task SignalAwaiter_FromSignal_0Arg()
    {
        await GDTask.SwitchToMainThread();
        var node = Constants.CreateTestNode<SignalTestNode>("SignalTestNode");
        
        Constants.Delay()
            .ContinueWith(node.EmitParam0)
            .Forget();
        
        var result = await GDTask.FromSignal(node, SignalTestNode.SignalName.Param0);
      
        node.Free();

        Assertions.AssertThat(result.Length).IsEqual(0);
    }

    [TestCase]
    public static async Task SignalAwaiter_FromSignal_2Args()
    {
        await GDTask.SwitchToMainThread();
        var node = Constants.CreateTestNode<SignalTestNode>("SignalTestNode");
        
        Constants.Delay()
            .ContinueWith(() => node.EmitParam2(Constants.ReturnValue, Constants.ReturnValue))
            .Forget();
        
        var (result1, result2) = await GDTask.FromSignal<int, int>(node, SignalTestNode.SignalName.Param2);
        
        node.Free();

        Assertions.AssertThat(result1).IsEqual(Constants.ReturnValue);
        Assertions.AssertThat(result2).IsEqual(Constants.ReturnValue);
    }
    
    [TestCase]
    public static async Task SignalAwaiter_FromSignal_0Arg_WithCtx()
    {
        await GDTask.SwitchToMainThread();
        var node = Constants.CreateTestNode<SignalTestNode>("SignalTestNode");
        
        Constants.Delay()
            .ContinueWith(node.EmitParam0)
            .Forget();
        
        var (isCanceled, _) = await GDTask.FromSignal(node, SignalTestNode.SignalName.Param0, Constants.CreateCanceledToken()).SuppressCancellationThrow();
      
        node.Free();

        Assertions.AssertThat(isCanceled).IsTrue();
    }

    [TestCase]
    public static async Task SignalAwaiter_FromSignal_2Args_WithCtx()
    {
        await GDTask.SwitchToMainThread();
        var node = Constants.CreateTestNode<SignalTestNode>("SignalTestNode");
        
        Constants.Delay()
            .ContinueWith(() => node.EmitParam2(Constants.ReturnValue, Constants.ReturnValue))
            .Forget();
        
        var (isCanceled, _) = await GDTask.FromSignal<int, int>(node, SignalTestNode.SignalName.Param2, Constants.CreateCanceledToken()).SuppressCancellationThrow();
        
        node.Free();

        Assertions.AssertThat(isCanceled).IsTrue();
    }    
    
    [TestCase]
    public static async Task SignalAwaiter_AsGDTask_0Arg()
    {
        await GDTask.SwitchToMainThread();
        var node = Constants.CreateTestNode<SignalTestNode>("SignalTestNode");
        
        Constants.Delay()
            .ContinueWith(node.EmitParam0)
            .Forget();
        
        var result = await node.ToSignal(node, SignalTestNode.SignalName.Param0).AsGDTask();
      
        node.Free();

        Assertions.AssertThat(result.Length).IsEqual(0);
    }

    [TestCase]
    public static async Task SignalAwaiter_AsGDTask_2Args()
    {
        await GDTask.SwitchToMainThread();
        var node = Constants.CreateTestNode<SignalTestNode>("SignalTestNode");
        
        Constants.Delay()
            .ContinueWith(() => node.EmitParam2(Constants.ReturnValue, Constants.ReturnValue))
            .Forget();
        
        var (result1, result2) = await node.ToSignal(node, SignalTestNode.SignalName.Param2).AsGDTask<int, int>();
        
        node.Free();

        Assertions.AssertThat(result1).IsEqual(Constants.ReturnValue);
        Assertions.AssertThat(result2).IsEqual(Constants.ReturnValue);
    }

    public partial class SignalTestNode : Node
    {
        [Signal] private delegate void Param0EventHandler();
        [Signal] private delegate void Param2EventHandler(Variant param1, Variant param2);
        
        public void EmitParam0() => EmitSignalParam0();
        public void EmitParam2(Variant param1, Variant param2) => EmitSignalParam2(param1, param2);
    }
    
}
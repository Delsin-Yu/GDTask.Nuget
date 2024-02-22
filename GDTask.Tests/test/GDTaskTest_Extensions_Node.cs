using System.Threading.Tasks;
using GodotTask.Triggers;
using GdUnit4;
using Godot;

namespace GodotTask.Tests;

public class GDTaskTest_Extensions_Node
{
    [TestCase]
    public static async Task Node_OnEnterTreeAsync()
    {
        var node = Constants.CreateTestNode("OnEnterTreeAsync");
        await node.OnEnterTreeAsync();
        node.QueueFree();
    }

    [TestCase]
    public static async Task Node_OnReadyAsync()
    {
        var node = Constants.CreateTestNode("OnReadyAsync");
        await node.OnReadyAsync();
        node.QueueFree();
    }

    [TestCase]
    public static async Task Node_OnProcessAsync()
    {
        var node = Constants.CreateTestNode("OnProcessAsync");
        var trigger = node.GetAsyncProcessTrigger();
        await trigger.OnProcessAsync();
        var frames = Engine.GetProcessFrames();
        await trigger.OnProcessAsync();
        var newFrames = Engine.GetProcessFrames();
        node.QueueFree();
        Assertions.AssertThat(frames + 1).IsEqual(newFrames);
    }

    [TestCase]
    public static async Task Node_OnPhysicsProcessAsync()
    {
        var node = Constants.CreateTestNode("OnPhysicsProcessAsync");
        var trigger = node.GetAsyncPhysicsProcessTrigger();
        await trigger.OnPhysicsProcessAsync();
        var frames = Engine.GetPhysicsFrames();
        await trigger.OnPhysicsProcessAsync();
        var newFrames = Engine.GetPhysicsFrames();
        node.QueueFree();
        Assertions.AssertThat(frames + 1).IsEqual(newFrames);
    }

    [TestCase]
    public static async Task Node_OnPredeleteAsync()
    {
        var node = Constants.CreateTestNode("OnPredeleteAsync");
        node.QueueFree();
        await node.OnPredeleteAsync();
    }
}
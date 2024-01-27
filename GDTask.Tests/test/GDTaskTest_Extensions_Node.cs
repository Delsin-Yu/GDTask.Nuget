using System.Threading.Tasks;
using Fractural.Tasks.Triggers;
using GdUnit4;
using Godot;

namespace Fractural.Tasks.Tests;

[TestSuite]
public class GDTaskTest_Extensions_Node
{
    [TestCase]
    public static async Task Node_OnEnterTreeAsync()
    {
        var node = CreateTestNode("OnEnterTreeAsync");
        await node.OnEnterTreeAsync();
        node.QueueFree();
    }

    [TestCase]
    public static async Task Node_OnReadyAsync()
    {
        var node = CreateTestNode("OnReadyAsync");
        await node.OnReadyAsync();
        node.QueueFree();
    }

    [TestCase]
    public static async Task Node_OnProcessAsync()
    {
        var node = CreateTestNode("OnProcessAsync");
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
        var node = CreateTestNode("OnPhysicsProcessAsync");
        var trigger = node.GetAsyncPhysicsProcessTrigger();
        await trigger.OnPhysicsProcessAsync();
        var frames = Engine.GetPhysicsFrames();
        await trigger.OnPhysicsProcessAsync();
        var newFrames = Engine.GetPhysicsFrames();
        node.QueueFree();
        Assertions.AssertThat(frames + 1).IsEqual(newFrames);
    }

    private static Node CreateTestNode(string nodeName)
    {
        GD.Print($"Create Test Node: {nodeName}");
        var node = new Node { Name = nodeName };
        var root = ((SceneTree)Engine.GetMainLoop()).Root;
        GD.Print($"CallDeferred: AddChild");
        root.CallDeferred(Node.MethodName.AddChild, node);
        return node;
    }
}
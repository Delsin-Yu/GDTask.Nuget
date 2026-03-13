using System;
using System.Runtime.CompilerServices;
using Godot;
using GodotTask.Internal;

[assembly: InternalsVisibleTo("GDTask.Tests")]
namespace GodotTask;

#nullable enable
internal partial class PlayerLoopRunnerProvider : Node
{
    private static PlayerLoopRunnerProvider? _global;

    internal static PlayerLoopRunnerProvider GlobalInstance
    {
        get
        {
            RuntimeChecker.ThrowIfEditor();
            if (_global != null) return _global;
            var newInstance = new PlayerLoopRunnerProvider();
            var root = ((SceneTree)Engine.GetMainLoop()).Root;
            root.CallDeferred(Node.MethodName.AddChild, newInstance, false, Variant.From(InternalMode.Front));
            newInstance.Name = "GDTaskPlayerLoopRunner";
            _global = newInstance;
            return _global;
        }
    }

    public override void _Ready()
    {
        if (_global == null)
        {
            _global = this;
            return;
        }

        if (_global == this) return;
        QueueFree();
    }

    private PlayerLoopRunnerProvider()
    {
        _processProxy = new();
        _physicsProcessProxy = new();
        _isolatedProcessProxy = new();
        _isolatedPhysicsProcessProxy = new();
        _deferredProxy = new();
        var isolatedPlayerLoopRunner = new IsolatedGDTaskPlayerLoopRunner(_isolatedProcessProxy, _isolatedPhysicsProcessProxy);
        AddChild(isolatedPlayerLoopRunner);
        isolatedPlayerLoopRunner.Name = "IsolatedGDTaskPlayerLoopRunner";
    }

    private readonly PlayerLoopProxy _processProxy;
    private readonly PlayerLoopProxy _physicsProcessProxy;
    private readonly PlayerLoopProxy _deferredProxy;
    private readonly PlayerLoopProxy _isolatedProcessProxy;
    private readonly PlayerLoopProxy _isolatedPhysicsProcessProxy;

    public static IPlayerLoop Process => GlobalInstance._processProxy;
    public static IPlayerLoop PhysicsProcess => GlobalInstance._physicsProcessProxy;
    public static IPlayerLoop IsolatedProcess => GlobalInstance._isolatedProcessProxy;
    public static IPlayerLoop IsolatedPhysicsProcess => GlobalInstance._isolatedPhysicsProcessProxy;
    public static IPlayerLoop Deferred => GlobalInstance._deferredProxy;

    public override void _Notification(int what)
    {
        if (what != NotificationPredelete) return;
        _processProxy.NotifyPredelete();
        _physicsProcessProxy.NotifyPredelete();
        _deferredProxy.NotifyPredelete();
        if (_global != this) return;
        _global = null;
    }

    private readonly Variant[] _deferredArgs = new Variant[1];
    public override void _Process(double delta)
    {
        _processProxy.NotifyProcess(delta);
        _deferredArgs[0] = delta;
        CallDeferred(MethodName.DeferredProcess, _deferredArgs);
    }
    
    public override void _PhysicsProcess(double delta)
    {
        _physicsProcessProxy.NotifyProcess(delta);
    }
    
    private void DeferredProcess(double delta) => 
        _deferredProxy.NotifyProcess(delta);
}

internal class PlayerLoopProxy : IPlayerLoop
{
    public void NotifyProcess(double delta) => OnProcess?.Invoke(delta);
    public void NotifyPredelete() => OnPredelete?.Invoke();

    public event Action<double>? OnProcess;
    public event Action? OnPredelete;
}
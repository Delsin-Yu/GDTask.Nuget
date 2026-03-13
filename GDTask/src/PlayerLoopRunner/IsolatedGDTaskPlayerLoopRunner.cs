using Godot;

namespace GodotTask;

internal partial class IsolatedGDTaskPlayerLoopRunner : Node
{
    private readonly PlayerLoopProxy _isolatedProcessProxy;
    private readonly PlayerLoopProxy _isolatedPhysicsProcessProxy;

    public IsolatedGDTaskPlayerLoopRunner(PlayerLoopProxy isolatedProcessProxy, PlayerLoopProxy isolatedPhysicsProcessProxy)
    {
        ProcessMode = ProcessModeEnum.Always;
        _isolatedProcessProxy = isolatedProcessProxy;
        _isolatedPhysicsProcessProxy = isolatedPhysicsProcessProxy;
    }

    public sealed override void _Process(double delta) =>
        _isolatedProcessProxy.NotifyProcess(delta);

    public sealed override void _PhysicsProcess(double delta) => 
        _isolatedPhysicsProcessProxy.NotifyProcess(delta);
    
    public override void _Notification(int what)
    {
        if (what != NotificationPredelete) return;
        _isolatedProcessProxy.NotifyPredelete();
        _isolatedPhysicsProcessProxy.NotifyPredelete();
    }
}
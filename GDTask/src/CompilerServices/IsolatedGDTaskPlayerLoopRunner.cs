using Godot;

namespace GodotTask.CompilerServices;

internal partial class IsolatedGDTaskPlayerLoopRunner : Node
{
    private readonly GDTaskPlayerLoopRunner _playerLoopRunner;

    public IsolatedGDTaskPlayerLoopRunner(GDTaskPlayerLoopRunner playerLoopRunner)
    {
        ProcessMode = ProcessModeEnum.Always;
        _playerLoopRunner = playerLoopRunner;
    }

    public sealed override void _Process(double delta) => 
        _playerLoopRunner.PauseProcess();

    public sealed override void _PhysicsProcess(double delta) => 
        _playerLoopRunner.PausePhysicsProcess();
}
using Godot;

namespace GodotTask.Triggers
{
    
    public static partial class AsyncTriggerExtensions
    {
        /// <summary>
        /// Gets an instance of <see cref="IAsyncPhysicsProcessHandler"/> for making repeatedly calls on <see cref="IAsyncPhysicsProcessHandler.OnPhysicsProcessAsync()"/>
        /// </summary>
        public static IAsyncPhysicsProcessHandler GetAsyncPhysicsProcessTrigger(this Node node)
        {
            return node.GetOrCreateChild<AsyncPhysicsProcessTrigger>();
        }
    }

    /// <summary>
    /// Provide access to <see cref="OnPhysicsProcessAsync()"/>/>
    /// </summary>
    public interface IAsyncPhysicsProcessHandler
    {
        /// <summary>
        /// Creates a task that will complete when the next <see cref="Node._PhysicsProcess"/> is called
        /// </summary>
        GDTask OnPhysicsProcessAsync();
    }
    
    internal sealed partial class AsyncPhysicsProcessTrigger : AsyncTriggerBase<AsyncUnit>, IAsyncPhysicsProcessHandler
    {
        public override void _PhysicsProcess(double delta)
        {
            RaiseEvent(AsyncUnit.Default);
        }

        private IAsyncPhysicsProcessHandler GetPhysicsProcessAsyncHandler()
        {
            return new AsyncTriggerHandler<AsyncUnit>(this, false);
        }

        public GDTask OnPhysicsProcessAsync()
        {
            return GetPhysicsProcessAsyncHandler().OnPhysicsProcessAsync();
        }
    }
    
    internal partial class AsyncTriggerHandler<T> : IAsyncPhysicsProcessHandler
    {
        GDTask IAsyncPhysicsProcessHandler.OnPhysicsProcessAsync()
        {
            core.Reset();
            return new GDTask(this, core.Version);
        }      
    }
    
}
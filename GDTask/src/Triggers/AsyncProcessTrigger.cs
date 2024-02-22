using Godot;

namespace GodotTask.Triggers
{
    public static partial class AsyncTriggerExtensions
    {
        /// <summary>
        /// Gets an instance of <see cref="IAsyncProcessHandler"/> for making repeatedly calls on <see cref="IAsyncProcessHandler.OnProcessAsync()"/>
        /// </summary>
        public static IAsyncProcessHandler GetAsyncProcessTrigger(this Node node)
        {
            return node.GetOrCreateChild<AsyncProcessTrigger>();
        }
    }

    /// <summary>
    /// Provide access to <see cref="OnProcessAsync()"/>/>
    /// </summary>
    public interface IAsyncProcessHandler
    {
        /// <summary>
        /// Creates a task that will complete when the next <see cref="Node._Process"/> is called
        /// </summary>
        GDTask OnProcessAsync();
    }

    internal sealed partial class AsyncProcessTrigger : AsyncTriggerBase<AsyncUnit>, IAsyncProcessHandler
    {
        public override void _Process(double delta)
        {
            RaiseEvent(AsyncUnit.Default);
        }

        public IAsyncProcessHandler GetProcessAsyncHandler()
        {
            return new AsyncTriggerHandler<AsyncUnit>(this, false);
        }

        public GDTask OnProcessAsync()
        {
            return ((IAsyncProcessHandler)new AsyncTriggerHandler<AsyncUnit>(this, true)).OnProcessAsync();
        }
    }

    internal partial class AsyncTriggerHandler<T> : IAsyncProcessHandler
    {
        GDTask IAsyncProcessHandler.OnProcessAsync()
        {
            core.Reset();
            return new GDTask(this, core.Version);
        }
    }
}
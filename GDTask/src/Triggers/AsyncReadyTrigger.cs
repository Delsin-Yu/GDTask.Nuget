using Godot;

namespace GodotTask.Triggers
{
    public static partial class AsyncTriggerExtensions
    {
        /// <inheritdoc cref="IAsyncReadyHandler.OnReadyAsync"/>
        public static GDTask OnReadyAsync(this Node node)
        {
            return node.GetAsyncReadyTrigger().OnReadyAsync();
        }

        /// <summary>
        /// Gets an instance of <see cref="IAsyncReadyHandler"/> for making repeatedly calls on <see cref="IAsyncReadyHandler.OnReadyAsync"/>
        /// </summary>
        public static IAsyncReadyHandler GetAsyncReadyTrigger(this Node node)
        {
            return node.GetOrCreateChild<AsyncReadyTrigger>();
        }
    }

    /// <summary>
    /// Provide access to <see cref="OnReadyAsync"/>
    /// </summary>
    public interface IAsyncReadyHandler
    {
        /// <summary>
        /// Creates a task that will complete when the <see cref="Node._Ready"/> is called
        /// </summary>
        GDTask OnReadyAsync();
    }
    
    internal sealed partial class AsyncReadyTrigger : AsyncTriggerBase<AsyncUnit>, IAsyncReadyHandler
    {
        private bool called;

        public override void _Ready()
        {
            base._Ready();
            called = true;
            RaiseEvent(AsyncUnit.Default);
        }

        public GDTask OnReadyAsync()
        {
            if (called) return GDTask.CompletedTask;

            return ((IAsyncOneShotTrigger)new AsyncTriggerHandler<AsyncUnit>(this, true)).OneShotAsync();
        }
    }
}
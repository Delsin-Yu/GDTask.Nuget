using Godot;

namespace GodotTask.Triggers
{
    /// Provides async extensions methods for <see cref="Node"/>.
    public static partial class AsyncTriggerExtensions
    {
        /// <inheritdoc cref="IAsyncEnterTreeHandler.OnEnterTreeAsync"/>
        public static GDTask OnEnterTreeAsync(this Node node)
        {
            return node.GetAsyncEnterTreeTrigger().OnEnterTreeAsync();
        }

        /// <summary>
        /// Gets an instance of <see cref="IAsyncEnterTreeHandler"/> for making repeatedly calls on <see cref="IAsyncEnterTreeHandler.OnEnterTreeAsync"/>
        /// </summary>
        public static IAsyncEnterTreeHandler GetAsyncEnterTreeTrigger(this Node node)
        {
            return node.GetOrCreateChild<AsyncEnterTreeTrigger>();
        }
    }

    /// <summary>
    /// Provide access to <see cref="OnEnterTreeAsync"/>
    /// </summary>
    public interface IAsyncEnterTreeHandler
    {
        /// <summary>
        /// Creates a task that will complete when the <see cref="Node._EnterTree"/> is called
        /// </summary>
        /// <returns></returns>
        GDTask OnEnterTreeAsync();
    }

    internal sealed partial class AsyncEnterTreeTrigger : AsyncTriggerBase<AsyncUnit>, IAsyncEnterTreeHandler
    {

        public override void _EnterTree()
        {
            base._EnterTree();
            RaiseEvent(AsyncUnit.Default);
        }
        
        public GDTask OnEnterTreeAsync()
        {
            if (calledEnterTree)
            {
                return GDTask.CompletedTask;
            }

            return ((IAsyncOneShotTrigger)new AsyncTriggerHandler<AsyncUnit>(this, true)).OneShotAsync();
        }
    }
}
using System.Threading;
using Godot;

namespace Fractural.Tasks.Triggers
{
    /// Provides async extensions methods for <see cref="Node"/>.
    public static partial class AsyncTriggerExtensions
    {
        public static AsyncEnterTreeTrigger GetAsyncEnterTreeTrigger(this Node node)
        {
            return node.GetOrAddImmediateChild<AsyncEnterTreeTrigger>();
        }
    }

    public sealed partial class AsyncEnterTreeTrigger : AsyncTriggerBase<AsyncUnit>
    {
        public GDTask OnEnterTreeAsync()
        {
            if (calledEnterTree) return GDTask.CompletedTask;

            return ((IAsyncOneShotTrigger)new AsyncTriggerHandler<AsyncUnit>(this, true)).OneShotAsync();
        }
    }
}


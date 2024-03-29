using System.Threading;
using Godot;

namespace GodotTask.Triggers
{
    public static partial class AsyncTriggerExtensions
    {
        /// <inheritdoc cref="IAsyncPredeleteHandler.OnPredeleteAsync"/>
        public static GDTask OnPredeleteAsync(this Node node)
        {
            return node.GetAsyncPredeleteTrigger().OnPredeleteAsync();
        }
        
        /// <summary>
        /// Gets a <see cref="CancellationToken"/> that will cancel when the <see cref="Node"/> is receiving <see cref="GodotObject.NotificationPredelete"/>
        /// </summary>
        public static CancellationToken GetAsyncPredeleteCancellationToken(this Node node)
        {
            return node.GetAsyncPredeleteTrigger().CancellationToken;
        }

        /// <summary>
        /// Gets an instance of <see cref="IAsyncPredeleteHandler"/> for making repeatedly calls on <see cref="IAsyncPredeleteHandler.OnPredeleteAsync"/>
        /// </summary>
        public static IAsyncPredeleteHandler GetAsyncPredeleteTrigger(this Node node)
        {
            return node.GetOrCreateChild<AsyncPredeleteTrigger>();
        }
        
    }

    /// <summary>
    /// Provide access to <see cref="OnPredeleteAsync"/>
    /// </summary>
    public interface IAsyncPredeleteHandler
    {
        /// <summary>
        /// Creates a task that will complete when the <see cref="Node"/> is receiving <see cref="GodotObject.NotificationPredelete"/>
        /// </summary>
        /// <returns></returns>
        GDTask OnPredeleteAsync();
        
        /// <summary>
        /// The <see cref="CancellationToken"/> associate with this handler.
        /// </summary>
        CancellationToken CancellationToken { get; }
    }

    internal sealed partial class AsyncPredeleteTrigger : Node, IAsyncPredeleteHandler
    {
        private bool enterTreeCalled = false;
        private bool predeleteCalled = false;
        private CancellationTokenSource cancellationTokenSource;

        public CancellationToken CancellationToken
        {
            get
            {
                if (cancellationTokenSource == null)
                {
                    cancellationTokenSource = new CancellationTokenSource();
                }

                return cancellationTokenSource.Token;
            }
        }

        public override void _EnterTree()
        {
            enterTreeCalled = true;
        }

        public override void _Notification(int what)
        {
            if (what == NotificationPredelete)
                OnPredelete();
        }

        private void OnPredelete()
        {
            predeleteCalled = true;

            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
        }

        public GDTask OnPredeleteAsync()
        {
            if (predeleteCalled) return GDTask.CompletedTask;

            var tcs = new GDTaskCompletionSource();

            // OnPredelete = Called Cancel.
            CancellationToken.RegisterWithoutCaptureExecutionContext(state =>
            {
                var tcs2 = (GDTaskCompletionSource)state;
                tcs2.TrySetResult();
            }, tcs);

            return tcs.Task;
        }
    }
}


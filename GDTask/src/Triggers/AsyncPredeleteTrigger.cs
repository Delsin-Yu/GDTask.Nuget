using System.Threading;
using Godot;

namespace Fractural.Tasks.Triggers
{
    public static partial class AsyncTriggerExtensions
    {
        public static AsyncPredeleteTrigger GetAsyncPredeleteTrigger(this Node node)
        {
            return node.GetOrAddImmediateChild<AsyncPredeleteTrigger>();
        }
    }

    public sealed partial class AsyncPredeleteTrigger : Node
    {
        bool awakeCalled = false;
        bool called = false;
        CancellationTokenSource cancellationTokenSource;

        public CancellationToken CancellationToken
        {
            get
            {
                if (cancellationTokenSource == null)
                {
                    cancellationTokenSource = new CancellationTokenSource();
                }

                if (!awakeCalled)
                {
                    GDTaskPlayerLoopAutoload.AddAction(PlayerLoopTiming.Process, new AwakeMonitor(this));
                }

                return cancellationTokenSource.Token;
            }
        }

        public override void _EnterTree()
        {
            awakeCalled = true;
        }

        public override void _Notification(int what)
        {
            if (what == NotificationPredelete)
                OnPredelete();
        }

        void OnPredelete()
        {
            called = true;

            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
        }

        public GDTask OnPredeleteAsync()
        {
            if (called) return GDTask.CompletedTask;

            var tcs = new GDTaskCompletionSource();

            // OnPredelete = Called Cancel.
            CancellationToken.RegisterWithoutCaptureExecutionContext(state =>
            {
                var tcs2 = (GDTaskCompletionSource)state;
                tcs2.TrySetResult();
            }, tcs);

            return tcs.Task;
        }

        class AwakeMonitor : IPlayerLoopItem
        {
            readonly AsyncPredeleteTrigger trigger;

            public AwakeMonitor(AsyncPredeleteTrigger trigger)
            {
                this.trigger = trigger;
            }

            public bool MoveNext()
            {
                if (trigger.called) return false;
                if (trigger == null)
                {
                    trigger.OnPredelete();
                    return false;
                }
                return true;
            }
        }
    }
}


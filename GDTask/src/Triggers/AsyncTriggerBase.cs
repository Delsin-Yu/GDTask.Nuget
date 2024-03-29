using System;
using System.Threading;
using Godot;

namespace GodotTask.Triggers
{
    internal abstract partial class AsyncTriggerBase<T> : Node
    {
        private TriggerEvent<T> triggerEvent;

        internal protected bool calledEnterTree;
        internal protected bool calledPredelete;

        public override void _EnterTree()
        {
            calledEnterTree = true;
        }

        public override void _Notification(int what)
        {
            if (what == NotificationPredelete)
                OnPredelete();
        }

        private void OnPredelete()
        {
            if (calledPredelete) return;
            calledPredelete = true;

            triggerEvent.SetCompleted();
        }

        internal void AddHandler(ITriggerHandler<T> handler)
        {
            triggerEvent.Add(handler);
        }

        internal void RemoveHandler(ITriggerHandler<T> handler)
        {
            triggerEvent.Remove(handler);
        }

        protected void RaiseEvent(T value)
        {
            triggerEvent.SetResult(value);
        }
    }

    internal interface IAsyncOneShotTrigger
    {
        GDTask OneShotAsync();
    }

    internal partial class AsyncTriggerHandler<T> : IAsyncOneShotTrigger
    {
        GDTask IAsyncOneShotTrigger.OneShotAsync()
        {
            core.Reset();
            return new GDTask(this, core.Version);
        }
    }

    internal sealed partial class AsyncTriggerHandler<T> : IGDTaskSource<T>, ITriggerHandler<T>, IDisposable
    {
        private readonly AsyncTriggerBase<T> trigger;

        private CancellationToken cancellationToken;
        private CancellationTokenRegistration registration;
        private bool isDisposed;
        private bool callOnce;

        private GDTaskCompletionSourceCore<T> core;

        internal CancellationToken CancellationToken => cancellationToken;

        ITriggerHandler<T> ITriggerHandler<T>.Prev { get; set; }
        ITriggerHandler<T> ITriggerHandler<T>.Next { get; set; }

        internal AsyncTriggerHandler(AsyncTriggerBase<T> trigger, bool callOnce)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                isDisposed = true;
                return;
            }

            this.trigger = trigger;
            cancellationToken = default;
            registration = default;
            this.callOnce = callOnce;

            trigger.AddHandler(this);

            TaskTracker.TrackActiveTask(this, 3);
        }

        internal AsyncTriggerHandler(AsyncTriggerBase<T> trigger, CancellationToken cancellationToken, bool callOnce)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                isDisposed = true;
                return;
            }

            this.trigger = trigger;
            this.cancellationToken = cancellationToken;
            this.callOnce = callOnce;

            trigger.AddHandler(this);

            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.RegisterWithoutCaptureExecutionContext(CancellationCallback, this);
            }

            TaskTracker.TrackActiveTask(this, 3);
        }

        private static void CancellationCallback(object state)
        {
            var self = (AsyncTriggerHandler<T>)state;
            self.Dispose();

            self.core.TrySetCanceled(self.cancellationToken);
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                TaskTracker.RemoveTracking(this);
                registration.Dispose();
                trigger.RemoveHandler(this);
            }
        }

        T IGDTaskSource<T>.GetResult(short token)
        {
            try
            {
                return core.GetResult(token);
            }
            finally
            {
                if (callOnce)
                {
                    Dispose();
                }
            }
        }

        void ITriggerHandler<T>.OnNext(T value)
        {
            core.TrySetResult(value);
        }

        void ITriggerHandler<T>.OnCanceled(CancellationToken cancellationToken)
        {
            core.TrySetCanceled(cancellationToken);
        }

        void ITriggerHandler<T>.OnCompleted()
        {
            core.TrySetCanceled(CancellationToken.None);
        }

        void ITriggerHandler<T>.OnError(Exception ex)
        {
            core.TrySetException(ex);
        }

        void IGDTaskSource.GetResult(short token)
        {
            ((IGDTaskSource<T>)this).GetResult(token);
        }

        GDTaskStatus IGDTaskSource.GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        GDTaskStatus IGDTaskSource.UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        void IGDTaskSource.OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }
    }
}
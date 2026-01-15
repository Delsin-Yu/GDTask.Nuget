using System;
using System.Collections.Generic;
using System.Threading;

namespace GodotTask.Internal
{
    internal static class Channel
    {
        public static Channel<T> CreateSingleConsumerUnbounded<T>()
        {
            return new SingleConsumerUnboundedChannel<T>();
        }
    }

    internal abstract class Channel<TWrite, TRead>
    {
        public ChannelReader<TRead> Reader { get; protected set; }
        public ChannelWriter<TWrite> Writer { get; protected set; }

        public static implicit operator ChannelReader<TRead>(Channel<TWrite, TRead> channel) => channel.Reader;
        public static implicit operator ChannelWriter<TWrite>(Channel<TWrite, TRead> channel) => channel.Writer;
    }

    internal abstract class Channel<T> : Channel<T, T>
    {
    }

    internal abstract class ChannelReader<T>
    {
        public abstract bool TryRead(out T item);
        public abstract GDTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default);

        public abstract GDTask Completion { get; }

        public virtual GDTask<T> ReadAsync(CancellationToken cancellationToken = default)
        {
            if (this.TryRead(out var item))
            {
                return GDTask.FromResult(item);
            }

            return ReadAsyncCore(cancellationToken);
        }

        private async GDTask<T> ReadAsyncCore(CancellationToken cancellationToken = default)
        {
            if (await WaitToReadAsync(cancellationToken))
            {
                if (TryRead(out var item))
                {
                    return item;
                }
            }

            throw new ChannelClosedException();
        }

        public abstract IGDTaskAsyncEnumerable<T> ReadAllAsync(CancellationToken cancellationToken = default);
    }

    internal abstract class ChannelWriter<T>
    {
        public abstract bool TryWrite(T item);
        public abstract bool TryComplete(Exception error = null);

        public void Complete(Exception error = null)
        {
            if (!TryComplete(error))
            {
                throw new ChannelClosedException();
            }
        }
    }

    internal partial class ChannelClosedException : InvalidOperationException
    {
        public ChannelClosedException() :
            base("Channel is already closed.")
        { }

        public ChannelClosedException(string message) : base(message) { }

        public ChannelClosedException(Exception innerException) :
            base("Channel is already closed", innerException)
        { }

        public ChannelClosedException(string message, Exception innerException) : base(message, innerException) { }
    }

    internal class SingleConsumerUnboundedChannel<T> : Channel<T>
    {
        private readonly Queue<T> items;
        private readonly SingleConsumerUnboundedChannelReader readerSource;
        private GDTaskCompletionSource completedTaskSource;
        private GDTask completedTask;

        private Exception completionError;
        private bool closed;

        public SingleConsumerUnboundedChannel()
        {
            items = new Queue<T>();
            Writer = new SingleConsumerUnboundedChannelWriter(this);
            readerSource = new SingleConsumerUnboundedChannelReader(this);
            Reader = readerSource;
        }

        private sealed class SingleConsumerUnboundedChannelWriter : ChannelWriter<T>
        {
            private readonly SingleConsumerUnboundedChannel<T> parent;

            public SingleConsumerUnboundedChannelWriter(SingleConsumerUnboundedChannel<T> parent)
            {
                this.parent = parent;
            }

            public override bool TryWrite(T item)
            {
                bool waiting;
                lock (parent.items)
                {
                    if (parent.closed)
                        return false;

                    parent.items.Enqueue(item);
                    waiting = parent.readerSource.isWaiting;
                }

                if (waiting)
                {
                    parent.readerSource.SingalContinuation();
                }

                return true;
            }

            public override bool TryComplete(Exception error = null)
            {
                bool waiting;
                lock (parent.items)
                {
                    if (parent.closed)
                        return false;
                    parent.closed = true;
                    waiting = parent.readerSource.isWaiting;

                    if (parent.items.Count == 0)
                    {
                        if (error == null)
                        {
                            if (parent.completedTaskSource != null)
                            {
                                parent.completedTaskSource.TrySetResult();
                            }
                            else
                            {
                                parent.completedTask = GDTask.CompletedTask;
                            }
                        }
                        else
                        {
                            if (parent.completedTaskSource != null)
                            {
                                parent.completedTaskSource.TrySetException(error);
                            }
                            else
                            {
                                parent.completedTask = GDTask.FromException(error);
                            }
                        }

                        if (waiting)
                        {
                            parent.readerSource.SingalCompleted(error);
                        }
                    }

                    parent.completionError = error;
                }

                return true;
            }
        }

        private sealed class SingleConsumerUnboundedChannelReader : ChannelReader<T>, IGDTaskSource<bool>
        {
            private readonly Action<object> CancellationCallbackDelegate = CancellationCallback;
            private readonly SingleConsumerUnboundedChannel<T> parent;

            private CancellationToken cancellationToken;
            private CancellationTokenRegistration cancellationTokenRegistration;
            private GDTaskCompletionSourceCore<bool> core;
            internal bool isWaiting;

            public SingleConsumerUnboundedChannelReader(SingleConsumerUnboundedChannel<T> parent)
            {
                this.parent = parent;

                TaskTracker.TrackActiveTask(this, 4);
            }

            public override GDTask Completion
            {
                get
                {
                    if (parent.completedTaskSource != null)
                        return parent.completedTaskSource.Task;

                    if (parent.closed)
                    {
                        return parent.completedTask;
                    }

                    parent.completedTaskSource = new GDTaskCompletionSource();
                    return parent.completedTaskSource.Task;
                }
            }

            public override bool TryRead(out T item)
            {
                lock (parent.items)
                {
                    if (parent.items.Count != 0)
                    {
                        item = parent.items.Dequeue();

                        // complete when all value was consumed.
                        if (parent.closed && parent.items.Count == 0)
                        {
                            if (parent.completionError != null)
                            {
                                if (parent.completedTaskSource != null)
                                {
                                    parent.completedTaskSource.TrySetException(parent.completionError);
                                }
                                else
                                {
                                    parent.completedTask = GDTask.FromException(parent.completionError);
                                }
                            }
                            else
                            {
                                if (parent.completedTaskSource != null)
                                {
                                    parent.completedTaskSource.TrySetResult();
                                }
                                else
                                {
                                    parent.completedTask = GDTask.CompletedTask;
                                }
                            }
                        }
                    }
                    else
                    {
                        item = default;
                        return false;
                    }
                }

                return true;
            }

            public override GDTask<bool> WaitToReadAsync(CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return GDTask.FromCanceled<bool>(cancellationToken);
                }

                lock (parent.items)
                {
                    if (parent.items.Count != 0)
                    {
                        return CompletedTasks.True;
                    }

                    if (parent.closed)
                    {
                        if (parent.completionError == null)
                        {
                            return CompletedTasks.False;
                        }
                        else
                        {
                            return GDTask.FromException<bool>(parent.completionError);
                        }
                    }

                    cancellationTokenRegistration.Dispose();

                    core.Reset();
                    isWaiting = true;

                    this.cancellationToken = cancellationToken;
                    if (this.cancellationToken.CanBeCanceled)
                    {
                        cancellationTokenRegistration = this.cancellationToken.RegisterWithoutCaptureExecutionContext(CancellationCallbackDelegate, this);
                    }

                    return new GDTask<bool>(this, core.Version);
                }
            }

            public void SingalContinuation()
            {
                core.TrySetResult(true);
            }

            public void SingalCancellation(CancellationToken cancellationToken)
            {
                TaskTracker.RemoveTracking(this);
                core.TrySetCanceled(cancellationToken);
            }

            public void SingalCompleted(Exception error)
            {
                if (error != null)
                {
                    TaskTracker.RemoveTracking(this);
                    core.TrySetException(error);
                }
                else
                {
                    TaskTracker.RemoveTracking(this);
                    core.TrySetResult(false);
                }
            }

            public override IGDTaskAsyncEnumerable<T> ReadAllAsync(CancellationToken cancellationToken = default)
            {
                return new ReadAllAsyncEnumerable(this, cancellationToken);
            }

            bool IGDTaskSource<bool>.GetResult(short token)
            {
                return core.GetResult(token);
            }

            void IGDTaskSource.GetResult(short token)
            {
                core.GetResult(token);
            }

            GDTaskStatus IGDTaskSource.GetStatus(short token)
            {
                return core.GetStatus(token);
            }

            void IGDTaskSource.OnCompleted(Action<object> continuation, object state, short token)
            {
                core.OnCompleted(continuation, state, token);
            }

            GDTaskStatus IGDTaskSource.UnsafeGetStatus()
            {
                return core.UnsafeGetStatus();
            }

            private static void CancellationCallback(object state)
            {
                var self = (SingleConsumerUnboundedChannelReader)state;
                self.SingalCancellation(self.cancellationToken);
            }

            private sealed class ReadAllAsyncEnumerable : IGDTaskAsyncEnumerable<T>, IGDTaskAsyncEnumerator<T>
            {
                private readonly Action<object> CancellationCallback1Delegate = CancellationCallback1;
                private readonly Action<object> CancellationCallback2Delegate = CancellationCallback2;

                private readonly SingleConsumerUnboundedChannelReader parent;
                private readonly CancellationToken cancellationToken1;
                private CancellationToken cancellationToken2;
                private CancellationTokenRegistration cancellationTokenRegistration1;
                private CancellationTokenRegistration cancellationTokenRegistration2;

                private T current;
                private bool cacheValue;
                private bool running;

                public ReadAllAsyncEnumerable(SingleConsumerUnboundedChannelReader parent, CancellationToken cancellationToken)
                {
                    this.parent = parent;
                    this.cancellationToken1 = cancellationToken;
                }

                public IGDTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
                {
                    if (running)
                    {
                        throw new InvalidOperationException("Enumerator is already running, does not allow call GetAsyncEnumerator twice.");
                    }

                    if (this.cancellationToken1 != cancellationToken)
                    {
                        this.cancellationToken2 = cancellationToken;
                    }

                    if (this.cancellationToken1.CanBeCanceled)
                    {
                        this.cancellationTokenRegistration1 = this.cancellationToken1.RegisterWithoutCaptureExecutionContext(CancellationCallback1Delegate, this);
                    }

                    if (this.cancellationToken2.CanBeCanceled)
                    {
                        this.cancellationTokenRegistration2 = this.cancellationToken2.RegisterWithoutCaptureExecutionContext(CancellationCallback2Delegate, this);
                    }

                    running = true;
                    return this;
                }

                public T Current
                {
                    get
                    {
                        if (cacheValue)
                        {
                            return current;
                        }
                        parent.TryRead(out current);
                        return current;
                    }
                }

                public GDTask<bool> MoveNextAsync()
                {
                    cacheValue = false;
                    return parent.WaitToReadAsync(CancellationToken.None); // ok to use None, registered in ctor.
                }

                public GDTask DisposeAsync()
                {
                    cancellationTokenRegistration1.Dispose();
                    cancellationTokenRegistration2.Dispose();
                    return default;
                }

                private static void CancellationCallback1(object state)
                {
                    var self = (ReadAllAsyncEnumerable)state;
                    self.parent.SingalCancellation(self.cancellationToken1);
                }

                private static void CancellationCallback2(object state)
                {
                    var self = (ReadAllAsyncEnumerable)state;
                    self.parent.SingalCancellation(self.cancellationToken2);
                }
            }
        }
    }
}

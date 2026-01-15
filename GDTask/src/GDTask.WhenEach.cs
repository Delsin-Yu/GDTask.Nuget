using System;
using System.Collections.Generic;
using System.Threading;
using GodotTask.Internal;

namespace GodotTask
{
    public partial struct GDTask
    {
        /// <summary>
        /// Creates an <see cref="IGDTaskAsyncEnumerable{T}"/> that will yield the supplied tasks as those tasks complete.
        /// </summary>
        /// <param name="tasks">The task to iterate through when completed.</param>
        /// <returns>An <see cref="IGDTaskAsyncEnumerable{T}"/> for iterating through the supplied tasks.</returns>
        /// <remarks>
        /// The supplied tasks will become available to be output via the enumerable once they've completed. The exact order
        /// in which the tasks will become available is not defined.
        /// </remarks>
        public static IGDTaskAsyncEnumerable<GDTask<T>> WhenEach<T>(params GDTask<T>[] tasks)
        {
            return new WhenEachEnumerable<T>(tasks);
        }

        /// <inheritdoc cref="WhenEach{T}(GDTask{T}[])"/>
        public static IGDTaskAsyncEnumerable<GDTask<T>> WhenEach<T>(IEnumerable<GDTask<T>> tasks)
        {
            return new WhenEachEnumerable<T>(tasks);
        }

        /// <inheritdoc cref="WhenEach{T}(GDTask{T}[])"/>
        public static IGDTaskAsyncEnumerable<GDTask> WhenEach(params GDTask[] tasks)
        {
            return new WhenEachEnumerable(tasks);
        }

        /// <inheritdoc cref="WhenEach(GDTask[])"/>
        public static IGDTaskAsyncEnumerable<GDTask> WhenEach(IEnumerable<GDTask> tasks)
        {
            return new WhenEachEnumerable(tasks);
        }
    }

    internal sealed class WhenEachEnumerable<T> : IGDTaskAsyncEnumerable<GDTask<T>>
    {
        private readonly IEnumerable<GDTask<T>> source;

        public WhenEachEnumerable(IEnumerable<GDTask<T>> source)
        {
            this.source = source;
        }

        public IGDTaskAsyncEnumerator<GDTask<T>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new Enumerator(source, cancellationToken);
        }

        private sealed class Enumerator : IGDTaskAsyncEnumerator<GDTask<T>>
        {
            private readonly IEnumerable<GDTask<T>> source;
            private readonly CancellationToken cancellationToken;

            private Channel<GDTask<T>> channel;
            private IGDTaskAsyncEnumerator<GDTask<T>> channelEnumerator;
            private int completeCount;
            private WhenEachState state;

            public Enumerator(IEnumerable<GDTask<T>> source, CancellationToken cancellationToken)
            {
                this.source = source;
                this.cancellationToken = cancellationToken;
            }

            public GDTask<T> Current => channelEnumerator.Current;

            public GDTask<bool> MoveNextAsync()
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (state == WhenEachState.NotRunning)
                {
                    state = WhenEachState.Running;
                    channel = Channel.CreateSingleConsumerUnbounded<GDTask<T>>();
                    channelEnumerator = channel.Reader.ReadAllAsync().GetAsyncEnumerator(cancellationToken);

                    using var usage = EnumerableUtils.ToSpan(source, out ReadOnlySpan<GDTask<T>> span);
                    foreach (GDTask<T> task in span)
                    {
                        RunWhenEachTask(this, task, span.Length).Forget();
                    }
                }

                return channelEnumerator.MoveNextAsync();
            }

            private static async GDTaskVoid RunWhenEachTask(Enumerator self, GDTask<T> task, int length)
            {
                try
                {
                    var result = await task;
                    self.channel.Writer.TryWrite(GDTask.FromResult(result));
                }
                catch (Exception ex)
                {
                    self.channel.Writer.TryWrite(GDTask.FromException<T>(ex));
                }

                if (Interlocked.Increment(ref self.completeCount) == length)
                {
                    self.state = WhenEachState.Completed;
                    self.channel.Writer.TryComplete();
                }
            }

            public async GDTask DisposeAsync()
            {
                if (channelEnumerator != null)
                {
                    await channelEnumerator.DisposeAsync();
                }

                if (state != WhenEachState.Completed)
                {
                    state = WhenEachState.Completed;
                    channel.Writer.TryComplete(new OperationCanceledException());
                }
            }

            private enum WhenEachState : byte
            {
                NotRunning,
                Running,
                Completed
            }
        }
    }

    internal sealed class WhenEachEnumerable : IGDTaskAsyncEnumerable<GDTask>
    {
        private readonly IEnumerable<GDTask> source;

        public WhenEachEnumerable(IEnumerable<GDTask> source)
        {
            this.source = source;
        }

        public IGDTaskAsyncEnumerator<GDTask> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new Enumerator(source, cancellationToken);
        }

        private sealed class Enumerator : IGDTaskAsyncEnumerator<GDTask>
        {
            private readonly IEnumerable<GDTask> source;
            private readonly CancellationToken cancellationToken;

            private Channel<GDTask> channel;
            private IGDTaskAsyncEnumerator<GDTask> channelEnumerator;
            private int completeCount;
            private WhenEachState state;

            public Enumerator(IEnumerable<GDTask> source, CancellationToken cancellationToken)
            {
                this.source = source;
                this.cancellationToken = cancellationToken;
            }

            public GDTask Current => channelEnumerator.Current;

            public GDTask<bool> MoveNextAsync()
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (state == WhenEachState.NotRunning)
                {
                    state = WhenEachState.Running;
                    channel = Channel.CreateSingleConsumerUnbounded<GDTask>();
                    channelEnumerator = channel.Reader.ReadAllAsync().GetAsyncEnumerator(cancellationToken);

                    using var usage = EnumerableUtils.ToSpan(source, out ReadOnlySpan<GDTask> span);
                    foreach (GDTask task in span) {
                        RunWhenEachTask(this, task, span.Length).Forget();
                    }
                }

                return channelEnumerator.MoveNextAsync();
            }

            private static async GDTaskVoid RunWhenEachTask(Enumerator self, GDTask task, int length)
            {
                try
                {
                    await task;
                    self.channel.Writer.TryWrite(GDTask.CompletedTask);
                }
                catch (Exception ex)
                {
                    self.channel.Writer.TryWrite(GDTask.FromException(ex));
                }

                if (Interlocked.Increment(ref self.completeCount) == length)
                {
                    self.state = WhenEachState.Completed;
                    self.channel.Writer.TryComplete();
                }
            }

            public async GDTask DisposeAsync()
            {
                if (channelEnumerator != null)
                {
                    await channelEnumerator.DisposeAsync();
                }

                if (state != WhenEachState.Completed)
                {
                    state = WhenEachState.Completed;
                    channel.Writer.TryComplete(new OperationCanceledException());
                }
            }

            private enum WhenEachState : byte
            {
                NotRunning,
                Running,
                Completed
            }
        }
    }
}

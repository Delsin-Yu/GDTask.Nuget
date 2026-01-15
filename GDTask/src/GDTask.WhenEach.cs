#if false
#if NET10_0_OR_GREATER
using GodotTask.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace GodotTask
{
    public partial struct GDTask
    {
        /// <summary>
        /// Creates a task that will complete when all of the supplied tasks have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait on for completion.</param>
        /// <param name="cancellationToken">The cancellation token with which to cancel the task.</param>
        /// <typeparam name="T">The type of the result returned by the task.</typeparam>
        /// <returns>A task that represents the completion of all of the supplied tasks.</returns>
        public static IAsyncEnumerable<GDTask> WhenEach<T>(scoped ReadOnlySpan<GDTask<T>> tasks, CancellationToken cancellationToken = default)
        {
            return WhenEachEnumerable.Create(tasks, cancellationToken);
        }
        
        /// <inheritdoc cref="WhenEach{T}(ReadOnlySpan{GDTask{T}}, CancellationToken)"/>
        public static IAsyncEnumerable<GDTask> WhenEach<T>(IEnumerable<GDTask<T>> tasks, CancellationToken cancellationToken = default)
        {
            using var usage = EnumerableUtils.ToSpan(tasks, out var span);
            return WhenEachEnumerable.Create(span, cancellationToken);
        }

        /// <summary>
        /// Creates a task that will complete when all of the supplied tasks have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait on for completion.</param>
        /// <param name="cancellationToken">The cancellation token with which to cancel the task.</param>
        /// <returns>A task that represents the completion of all of the supplied tasks.</returns>
        public static IAsyncEnumerable<GDTask> WhenEach(scoped ReadOnlySpan<GDTask> tasks, CancellationToken cancellationToken = default)
        {
            return WhenEachEnumerable.Create(tasks, cancellationToken);
        }

        /// <inheritdoc cref="WhenEach(ReadOnlySpan{GDTask}, CancellationToken)"/>
        public static IAsyncEnumerable<GDTask> WhenEach(IEnumerable<GDTask> tasks, CancellationToken cancellationToken = default)
        {
            using var usage = EnumerableUtils.ToSpan(tasks, out var span);
            return WhenEachEnumerable.Create(span, cancellationToken);
        }

        private sealed class WhenEachEnumerable : IAsyncEnumerable<GDTask>, ITaskPoolNode<WhenEachEnumerable>
        {
            private static TaskPool<WhenEachEnumerable> pool;
            private WhenEachEnumerable nextNode;
            public ref WhenEachEnumerable NextNode => ref nextNode;

            static WhenEachEnumerable() {
                TaskPool.RegisterSizeGetter(typeof(WhenEachEnumerable), () => pool.Size);
            }

            private List<GDTask> tasks;
            private GDTask[] completedTasks;
            private int totalCount;
            private int completedCount;
            private CancellationToken cancellationToken;

            private WhenEachEnumerable()
            {
            }

            public static IAsyncEnumerable<GDTask> Create(scoped ReadOnlySpan<GDTask> tasks, CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return AsyncEnumerable.Empty<GDTask>();
                }

                if (!pool.TryPop(out var result))
                {
                    result = new WhenEachEnumerable();
                }

                result.tasks = [.. tasks];
                result.totalCount = tasks.Length;
                result.completedTasks = new GDTask[result.totalCount];
                result.completedCount = 0;
                result.cancellationToken = cancellationToken;

                foreach (GDTask task in tasks)
                {
                    task.GetAwaiter().SourceOnCompleted(AddToQueue, StateTuple.Create(task, result));
                }

                return result;
            }

            public static IAsyncEnumerable<GDTask> Create<T>(scoped ReadOnlySpan<GDTask<T>> tasks, CancellationToken cancellationToken = default)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return AsyncEnumerable.Empty<GDTask>();
                }

                if (!pool.TryPop(out var result))
                {
                    result = new WhenEachEnumerable();
                }

                result.tasks = [.. tasks];
                result.totalCount = tasks.Length;
                result.completedTasks = new GDTask[result.totalCount];
                result.completedCount = 0;
                result.cancellationToken = cancellationToken;

                foreach (GDTask task in tasks)
                {
                    task.GetAwaiter().SourceOnCompleted(AddToQueue, StateTuple.Create(task, result));
                }

                return result;
            }

            public IAsyncEnumerator<GDTask> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                return new WhenEachEnumerator(this);
            }

            private static void AddToQueue(object state)
            {
                using var tuple = (StateTuple<GDTask, WhenEachEnumerable>)state;
                (GDTask task, WhenEachEnumerable promise) = tuple;

                int newCompletedCount = Interlocked.Increment(ref promise.completedCount);
                promise.completedTasks[newCompletedCount - 1] = task;

                promise.tasks.Remove(task);
            }

            public sealed class WhenEachEnumerator : IAsyncEnumerator<GDTask>
            {
                private readonly WhenEachEnumerable promise;
                private int currentIndex;

                public WhenEachEnumerator(WhenEachEnumerable promise)
                {
                    this.promise = promise;
                    this.currentIndex = -1;
                }

                public GDTask Current => (currentIndex < 0) ? default : promise.completedTasks[currentIndex];

                public ValueTask DisposeAsync() => ValueTask.CompletedTask;

                public async ValueTask<bool> MoveNextAsync()
                {
                    if (currentIndex == promise.totalCount - 1)
                    {
                        return false;
                    }

                    if (currentIndex == promise.completedCount)
                    {
                        await Yield(promise.cancellationToken);
                        await WhenAny(promise.tasks)/*.AttachExternalCancellation(promise.cancellationToken)*/;
                    }

                    Interlocked.Increment(ref currentIndex);

                    return true;
                }
            }
        }
    }
}
#endif
#endif

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

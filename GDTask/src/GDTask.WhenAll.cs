using System;
using System.Collections.Generic;
using System.Threading;
using GodotTask.Internal;

namespace GodotTask
{
    public partial struct GDTask
    {
        /// <summary>
        /// Creates a task that will complete when all of the supplied tasks have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait on for completion.</param>
        /// <typeparam name="T">The type of the result returned by the task.</typeparam>
        /// <returns>A task that represents the completion of all of the supplied tasks.</returns>
        public static GDTask<T[]> WhenAll<T>(params GDTask<T>[] tasks)
        {
            if (tasks.Length == 0)
            {
                return FromResult(Array.Empty<T>());
            }

            return new GDTask<T[]>(new WhenAllPromise<T>(tasks, tasks.Length), 0);
        }

        /// <inheritdoc cref="WhenAll{T}(GDTask{T}[])"/>
        public static GDTask<T[]> WhenAll<T>(IEnumerable<GDTask<T>> tasks)
        {
            using var span = ArrayPoolUtil.Materialize(tasks);
            var promise = new WhenAllPromise<T>(span.Array, span.Length); // consumed array in constructor.
            return new GDTask<T[]>(promise, 0);
        }

        /// <summary>
        /// Creates a task that will complete when all of the supplied tasks have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait on for completion.</param>
        /// <returns>A task that represents the completion of all of the supplied tasks.</returns>
        public static GDTask WhenAll(params GDTask[] tasks)
        {
            if (tasks.Length == 0)
            {
                return CompletedTask;
            }

            return new GDTask(new WhenAllPromise(tasks, tasks.Length), 0);
        }

        /// <inheritdoc cref="WhenAll(GDTask[])"/>
        public static GDTask WhenAll(IEnumerable<GDTask> tasks)
        {
            using var span = ArrayPoolUtil.Materialize(tasks);
            var promise = new WhenAllPromise(span.Array, span.Length); // consumed array in constructor.
            return new GDTask(promise, 0);
        }

        private sealed class WhenAllPromise<T> : IGDTaskSource<T[]>
        {
            private readonly T[] result;
            private int completeCount;
            private GDTaskCompletionSourceCore<T[]> core; // don't reset(called after GetResult, will invoke TrySetException.)

            public WhenAllPromise(GDTask<T>[] tasks, int tasksLength)
            {
                TaskTracker.TrackActiveTask(this, 3);

                completeCount = 0;

                if (tasksLength == 0)
                {
                    result = Array.Empty<T>();
                    core.TrySetResult(result);
                    return;
                }

                result = new T[tasksLength];

                for (int i = 0; i < tasksLength; i++)
                {
                    GDTask<T>.Awaiter awaiter;
                    try
                    {
                        awaiter = tasks[i].GetAwaiter();
                    }
                    catch (Exception ex)
                    {
                        core.TrySetException(ex);
                        continue;
                    }

                    if (awaiter.IsCompleted)
                    {
                        TryInvokeContinuation(this, awaiter, i);
                    }
                    else
                    {
                        awaiter.SourceOnCompleted(state =>
                        {
                            using var t = (StateTuple<WhenAllPromise<T>, GDTask<T>.Awaiter, int>)state;
                            TryInvokeContinuation(t.Item1, t.Item2, t.Item3);
                        }, StateTuple.Create(this, awaiter, i));
                    }
                }
            }

            private static void TryInvokeContinuation(WhenAllPromise<T> self, in GDTask<T>.Awaiter awaiter, int i)
            {
                try
                {
                    self.result[i] = awaiter.GetResult();
                }
                catch (Exception ex)
                {
                    self.core.TrySetException(ex);
                    return;
                }

                if (Interlocked.Increment(ref self.completeCount) == self.result.Length)
                {
                    self.core.TrySetResult(self.result);
                }
            }

            public T[] GetResult(short token)
            {
                TaskTracker.RemoveTracking(this);
                GC.SuppressFinalize(this);
                return core.GetResult(token);
            }

            void IGDTaskSource.GetResult(short token)
            {
                GetResult(token);
            }

            public GDTaskStatus GetStatus(short token)
            {
                return core.GetStatus(token);
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                return core.UnsafeGetStatus();
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                core.OnCompleted(continuation, state, token);
            }
        }

        private sealed class WhenAllPromise : IGDTaskSource
        {
            private int completeCount;
            private readonly int tasksLength;
            private GDTaskCompletionSourceCore<AsyncUnit> core; // don't reset(called after GetResult, will invoke TrySetException.)

            public WhenAllPromise(GDTask[] tasks, int tasksLength)
            {
                TaskTracker.TrackActiveTask(this, 3);

                this.tasksLength = tasksLength;
                completeCount = 0;

                if (tasksLength == 0)
                {
                    core.TrySetResult(AsyncUnit.Default);
                    return;
                }

                for (int i = 0; i < tasksLength; i++)
                {
                    Awaiter awaiter;
                    try
                    {
                        awaiter = tasks[i].GetAwaiter();
                    }
                    catch (Exception ex)
                    {
                        core.TrySetException(ex);
                        continue;
                    }

                    if (awaiter.IsCompleted)
                    {
                        TryInvokeContinuation(this, awaiter);
                    }
                    else
                    {
                        awaiter.SourceOnCompleted(state =>
                        {
                            using var t = (StateTuple<WhenAllPromise, Awaiter>)state;
                            TryInvokeContinuation(t.Item1, t.Item2);
                        }, StateTuple.Create(this, awaiter));
                    }
                }
            }

            private static void TryInvokeContinuation(WhenAllPromise self, in Awaiter awaiter)
            {
                try
                {
                    awaiter.GetResult();
                }
                catch (Exception ex)
                {
                    self.core.TrySetException(ex);
                    return;
                }

                if (Interlocked.Increment(ref self.completeCount) == self.tasksLength)
                {
                    self.core.TrySetResult(AsyncUnit.Default);
                }
            }

            public void GetResult(short token)
            {
                TaskTracker.RemoveTracking(this);
                GC.SuppressFinalize(this);
                core.GetResult(token);
            }

            public GDTaskStatus GetStatus(short token)
            {
                return core.GetStatus(token);
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                return core.UnsafeGetStatus();
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                core.OnCompleted(continuation, state, token);
            }
        }
    }
}

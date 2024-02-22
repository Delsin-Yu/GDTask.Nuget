using System;
using System.Collections.Generic;
using System.Threading;
using GodotTask.Internal;

namespace GodotTask
{
    public partial struct GDTask
    {
        /// <summary>
        /// Creates a task that will complete when any of the supplied tasks have completed.
        /// </summary>
        /// <typeparam name="T">The type of the result returned by the task.</typeparam>
        /// <returns>A task that represents the info for the first completed task.</returns>
        public static GDTask<(bool hasResultLeft, T result)> WhenAny<T>(GDTask<T> leftTask, GDTask rightTask)
        {
            return new GDTask<(bool, T)>(new WhenAnyLRPromise<T>(leftTask, rightTask), 0);
        }

        /// <inheritdoc cref="WhenAny{T}(GDTask{T},GDTask)"/>
        public static GDTask<(int winArgumentIndex, T result)> WhenAny<T>(params GDTask<T>[] tasks)
        {
            return new GDTask<(int, T)>(new WhenAnyPromise<T>(tasks, tasks.Length), 0);
        }

        /// <inheritdoc cref="WhenAny{T}(GDTask{T},GDTask)"/>
        public static GDTask<(int winArgumentIndex, T result)> WhenAny<T>(IEnumerable<GDTask<T>> tasks)
        {
            using var span = ArrayPoolUtil.Materialize(tasks);
            return new GDTask<(int, T)>(new WhenAnyPromise<T>(span.Array, span.Length), 0);
        }

        /// <summary>
        /// Creates a task that will complete when any of the supplied tasks have completed.
        /// </summary>
        /// <returns>A task that evaluates the index of the first completed task.</returns>
        public static GDTask<int> WhenAny(params GDTask[] tasks)
        {
            return new GDTask<int>(new WhenAnyPromise(tasks, tasks.Length), 0);
        }

        /// <inheritdoc cref="WhenAny(GDTask[])"/>
        public static GDTask<int> WhenAny(IEnumerable<GDTask> tasks)
        {
            using var span = ArrayPoolUtil.Materialize(tasks);
            return new GDTask<int>(new WhenAnyPromise(span.Array, span.Length), 0);
        }

        private sealed class WhenAnyLRPromise<T> : IGDTaskSource<(bool, T)>
        {
            private int completedCount;
            private GDTaskCompletionSourceCore<(bool, T)> core;

            public WhenAnyLRPromise(GDTask<T> leftTask, GDTask rightTask)
            {
                TaskTracker.TrackActiveTask(this, 3);

                {
                    GDTask<T>.Awaiter awaiter;
                    try
                    {
                        awaiter = leftTask.GetAwaiter();
                    }
                    catch (Exception ex)
                    {
                        core.TrySetException(ex);
                        goto RIGHT;
                    }

                    if (awaiter.IsCompleted)
                    {
                        TryLeftInvokeContinuation(this, awaiter);
                    }
                    else
                    {
                        awaiter.SourceOnCompleted(state =>
                        {
                            using var t = (StateTuple<WhenAnyLRPromise<T>, GDTask<T>.Awaiter>)state;
                            TryLeftInvokeContinuation(t.Item1, t.Item2);
                        }, StateTuple.Create(this, awaiter));
                    }
                }
                RIGHT:
                {
                    Awaiter awaiter;
                    try
                    {
                        awaiter = rightTask.GetAwaiter();
                    }
                    catch (Exception ex)
                    {
                        core.TrySetException(ex);
                        return;
                    }

                    if (awaiter.IsCompleted)
                    {
                        TryRightInvokeContinuation(this, awaiter);
                    }
                    else
                    {
                        awaiter.SourceOnCompleted(state =>
                        {
                            using var t = (StateTuple<WhenAnyLRPromise<T>, Awaiter>)state;
                            TryRightInvokeContinuation(t.Item1, t.Item2);
                        }, StateTuple.Create(this, awaiter));
                    }
                }
            }

            private static void TryLeftInvokeContinuation(WhenAnyLRPromise<T> self, in GDTask<T>.Awaiter awaiter)
            {
                T result;
                try
                {
                    result = awaiter.GetResult();
                }
                catch (Exception ex)
                {
                    self.core.TrySetException(ex);
                    return;
                }

                if (Interlocked.Increment(ref self.completedCount) == 1)
                {
                    self.core.TrySetResult((true, result));
                }
            }

            private static void TryRightInvokeContinuation(WhenAnyLRPromise<T> self, in Awaiter awaiter)
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

                if (Interlocked.Increment(ref self.completedCount) == 1)
                {
                    self.core.TrySetResult((false, default));
                }
            }

            public (bool, T) GetResult(short token)
            {
                TaskTracker.RemoveTracking(this);
                GC.SuppressFinalize(this);
                return core.GetResult(token);
            }

            public GDTaskStatus GetStatus(short token)
            {
                return core.GetStatus(token);
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                core.OnCompleted(continuation, state, token);
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                return core.UnsafeGetStatus();
            }

            void IGDTaskSource.GetResult(short token)
            {
                GetResult(token);
            }
        }


        private sealed class WhenAnyPromise<T> : IGDTaskSource<(int, T)>
        {
            private int completedCount;
            private GDTaskCompletionSourceCore<(int, T)> core;

            public WhenAnyPromise(GDTask<T>[] tasks, int tasksLength)
            {
                if (tasksLength == 0)
                {
                    throw new ArgumentException("The tasks argument contains no tasks.");
                }

                TaskTracker.TrackActiveTask(this, 3);

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
                        continue; // consume others.
                    }

                    if (awaiter.IsCompleted)
                    {
                        TryInvokeContinuation(this, awaiter, i);
                    }
                    else
                    {
                        awaiter.SourceOnCompleted(state =>
                        {
                            using var t = (StateTuple<WhenAnyPromise<T>, GDTask<T>.Awaiter, int>)state;
                            TryInvokeContinuation(t.Item1, t.Item2, t.Item3);
                        }, StateTuple.Create(this, awaiter, i));
                    }
                }
            }

            private static void TryInvokeContinuation(WhenAnyPromise<T> self, in GDTask<T>.Awaiter awaiter, int i)
            {
                T result;
                try
                {
                    result = awaiter.GetResult();
                }
                catch (Exception ex)
                {
                    self.core.TrySetException(ex);
                    return;
                }

                if (Interlocked.Increment(ref self.completedCount) == 1)
                {
                    self.core.TrySetResult((i, result));
                }
            }

            public (int, T) GetResult(short token)
            {
                TaskTracker.RemoveTracking(this);
                GC.SuppressFinalize(this);
                return core.GetResult(token);
            }

            public GDTaskStatus GetStatus(short token)
            {
                return core.GetStatus(token);
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                core.OnCompleted(continuation, state, token);
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                return core.UnsafeGetStatus();
            }

            void IGDTaskSource.GetResult(short token)
            {
                GetResult(token);
            }
        }

        private sealed class WhenAnyPromise : IGDTaskSource<int>
        {
            private int completedCount;
            private GDTaskCompletionSourceCore<int> core;

            public WhenAnyPromise(GDTask[] tasks, int tasksLength)
            {
                if (tasksLength == 0)
                {
                    throw new ArgumentException("The tasks argument contains no tasks.");
                }

                TaskTracker.TrackActiveTask(this, 3);

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
                        continue; // consume others.
                    }

                    if (awaiter.IsCompleted)
                    {
                        TryInvokeContinuation(this, awaiter, i);
                    }
                    else
                    {
                        awaiter.SourceOnCompleted(state =>
                        {
                            using var t = (StateTuple<WhenAnyPromise, Awaiter, int>)state;
                            TryInvokeContinuation(t.Item1, t.Item2, t.Item3);
                        }, StateTuple.Create(this, awaiter, i));
                    }
                }
            }

            private static void TryInvokeContinuation(WhenAnyPromise self, in Awaiter awaiter, int i)
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

                if (Interlocked.Increment(ref self.completedCount) == 1)
                {
                    self.core.TrySetResult(i);
                }
            }

            public int GetResult(short token)
            {
                TaskTracker.RemoveTracking(this);
                GC.SuppressFinalize(this);
                return core.GetResult(token);
            }

            public GDTaskStatus GetStatus(short token)
            {
                return core.GetStatus(token);
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                core.OnCompleted(continuation, state, token);
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                return core.UnsafeGetStatus();
            }

            void IGDTaskSource.GetResult(short token)
            {
                GetResult(token);
            }
        }
    }
}


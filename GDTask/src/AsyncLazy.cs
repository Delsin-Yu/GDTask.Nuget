﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace GodotTasks.Tasks
{
    /// <summary>
    /// Provides access a lazy initialized of an asynchronous work.
    /// </summary>
    public interface IAsyncLazy
    {
        /// <summary>
        /// Access the initialized task.
        /// </summary>
        GDTask Task { get; }
        
        /// <summary>
        /// Gets an awaiter used to await this <see cref="GDTask" />.
        /// </summary>
        GDTask.Awaiter GetAwaiter();
    }

    /// <inheritdoc cref="IAsyncLazy"/>
    public interface IAsyncLazy<T>
    {
        /// <inheritdoc cref="IAsyncLazy.Task"/>
        GDTask<T> Task { get; }
        
        /// <inheritdoc cref="IAsyncLazy.GetAwaiter"/>
        GDTask<T>.Awaiter GetAwaiter();
    }

    internal class AsyncLazy : IAsyncLazy
    {
        static Action<object> continuation = SetCompletionSource;

        Func<GDTask> taskFactory;
        GDTaskCompletionSource completionSource;
        GDTask.Awaiter awaiter;

        object syncLock;
        bool initialized;

        public AsyncLazy(Func<GDTask> taskFactory)
        {
            this.taskFactory = taskFactory;
            this.completionSource = new GDTaskCompletionSource();
            this.syncLock = new object();
            this.initialized = false;
        }

        internal AsyncLazy(GDTask task)
        {
            this.taskFactory = null;
            this.completionSource = new GDTaskCompletionSource();
            this.syncLock = null;
            this.initialized = true;

            var awaiter = task.GetAwaiter();
            if (awaiter.IsCompleted)
            {
                SetCompletionSource(awaiter);
            }
            else
            {
                this.awaiter = awaiter;
                awaiter.SourceOnCompleted(continuation, this);
            }
        }

        public GDTask Task
        {
            get
            {
                EnsureInitialized();
                return completionSource.Task;
            }
        }


        public GDTask.Awaiter GetAwaiter() => Task.GetAwaiter();

        void EnsureInitialized()
        {
            if (Volatile.Read(ref initialized))
            {
                return;
            }

            EnsureInitializedCore();
        }

        void EnsureInitializedCore()
        {
            lock (syncLock)
            {
                if (!Volatile.Read(ref initialized))
                {
                    var f = Interlocked.Exchange(ref taskFactory, null);
                    if (f != null)
                    {
                        var task = f();
                        var awaiter = task.GetAwaiter();
                        if (awaiter.IsCompleted)
                        {
                            SetCompletionSource(awaiter);
                        }
                        else
                        {
                            this.awaiter = awaiter;
                            awaiter.SourceOnCompleted(continuation, this);
                        }

                        Volatile.Write(ref initialized, true);
                    }
                }
            }
        }

        void SetCompletionSource(in GDTask.Awaiter awaiter)
        {
            try
            {
                awaiter.GetResult();
                completionSource.TrySetResult();
            }
            catch (Exception ex)
            {
                completionSource.TrySetException(ex);
            }
        }

        static void SetCompletionSource(object state)
        {
            var self = (AsyncLazy)state;
            try
            {
                self.awaiter.GetResult();
                self.completionSource.TrySetResult();
            }
            catch (Exception ex)
            {
                self.completionSource.TrySetException(ex);
            }
            finally
            {
                self.awaiter = default;
            }
        }
    }

    internal class AsyncLazy<T> : IAsyncLazy<T>
    {
        static Action<object> continuation = SetCompletionSource;

        Func<GDTask<T>> taskFactory;
        GDTaskCompletionSource<T> completionSource;
        GDTask<T>.Awaiter awaiter;

        object syncLock;
        bool initialized;

        public AsyncLazy(Func<GDTask<T>> taskFactory)
        {
            this.taskFactory = taskFactory;
            this.completionSource = new GDTaskCompletionSource<T>();
            this.syncLock = new object();
            this.initialized = false;
        }

        internal AsyncLazy(GDTask<T> task)
        {
            this.taskFactory = null;
            this.completionSource = new GDTaskCompletionSource<T>();
            this.syncLock = null;
            this.initialized = true;

            var awaiter = task.GetAwaiter();
            if (awaiter.IsCompleted)
            {
                SetCompletionSource(awaiter);
            }
            else
            {
                this.awaiter = awaiter;
                awaiter.SourceOnCompleted(continuation, this);
            }
        }

        public GDTask<T> Task
        {
            get
            {
                EnsureInitialized();
                return completionSource.Task;
            }
        }


        public GDTask<T>.Awaiter GetAwaiter() => Task.GetAwaiter();

        void EnsureInitialized()
        {
            if (Volatile.Read(ref initialized))
            {
                return;
            }

            EnsureInitializedCore();
        }

        void EnsureInitializedCore()
        {
            lock (syncLock)
            {
                if (!Volatile.Read(ref initialized))
                {
                    var f = Interlocked.Exchange(ref taskFactory, null);
                    if (f != null)
                    {
                        var task = f();
                        var awaiter = task.GetAwaiter();
                        if (awaiter.IsCompleted)
                        {
                            SetCompletionSource(awaiter);
                        }
                        else
                        {
                            this.awaiter = awaiter;
                            awaiter.SourceOnCompleted(continuation, this);
                        }

                        Volatile.Write(ref initialized, true);
                    }
                }
            }
        }

        void SetCompletionSource(in GDTask<T>.Awaiter awaiter)
        {
            try
            {
                var result = awaiter.GetResult();
                completionSource.TrySetResult(result);
            }
            catch (Exception ex)
            {
                completionSource.TrySetException(ex);
            }
        }

        static void SetCompletionSource(object state)
        {
            var self = (AsyncLazy<T>)state;
            try
            {
                var result = self.awaiter.GetResult();
                self.completionSource.TrySetResult(result);
            }
            catch (Exception ex)
            {
                self.completionSource.TrySetException(ex);
            }
            finally
            {
                self.awaiter = default;
            }
        }
    }
}

using System;
using System.Threading;

namespace GodotTask
{
    public partial struct GDTask
    {
        /// <summary>
        /// Queues the specified work to run on the ThreadPool and returns a <see cref="GDTask"/> handle for that work.
        /// </summary>
        /// <param name="action">The work to execute asynchronously</param>
        /// <param name="configureAwait">Returns to the main thread after await if set to true, otherwise, the executing thread is undefined</param>
        /// <param name="cancellationToken">A cancellation token that should be used to cancel the work</param>
        public static async GDTask RunOnThreadPool(Action action, bool configureAwait = true, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await SwitchToThreadPool();

            cancellationToken.ThrowIfCancellationRequested();

            if (configureAwait)
            {
                try
                {
                    action();
                }
                finally
                {
                    await Yield();
                }
            }
            else
            {
                action();
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Queues the specified work to run on the ThreadPool and returns a <see cref="GDTask"/> handle for that work.
        /// </summary>
        /// <param name="action">The work to execute asynchronously</param>
        /// <param name="state">Object to which to pass to action (may be null)</param>
        /// <param name="configureAwait">Returns to the main thread after await if set to true, otherwise, the executing thread is undefined</param>
        /// <param name="cancellationToken">A cancellation token that should be used to cancel the work</param>
        public static async GDTask RunOnThreadPool(Action<object> action, object state, bool configureAwait = true, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await SwitchToThreadPool();

            cancellationToken.ThrowIfCancellationRequested();

            if (configureAwait)
            {
                try
                {
                    action(state);
                }
                finally
                {
                    await Yield();
                }
            }
            else
            {
                action(state);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Create and queues the specified task to run on the ThreadPool and returns a <see cref="GDTask"/> handle for that work.
        /// </summary>
        /// <param name="action">The delegate which create the task</param>
        /// <param name="configureAwait">Returns to the main thread after await if set to true, otherwise, the executing thread is undefined</param>
        /// <param name="cancellationToken">A cancellation token that should be used to cancel the work</param>
        public static async GDTask RunOnThreadPool(Func<GDTask> action, bool configureAwait = true, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await SwitchToThreadPool();

            cancellationToken.ThrowIfCancellationRequested();

            if (configureAwait)
            {
                try
                {
                    await action();
                }
                finally
                {
                    await Yield();
                }
            }
            else
            {
                await action();
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Create and queues the specified task to run on the ThreadPool and returns a <see cref="GDTask"/> handle for that work.
        /// </summary>
        /// <param name="action">The delegate which create the task</param>
        /// <param name="state">Object to which to pass to action (may be null)</param>
        /// <param name="configureAwait">Returns to the main thread after await if set to true, otherwise, the executing thread is undefined</param>
        /// <param name="cancellationToken">A cancellation token that should be used to cancel the work</param>
        public static async GDTask RunOnThreadPool(Func<object, GDTask> action, object state, bool configureAwait = true, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await SwitchToThreadPool();

            cancellationToken.ThrowIfCancellationRequested();

            if (configureAwait)
            {
                try
                {
                    await action(state);
                }
                finally
                {
                    await Yield();
                }
            }
            else
            {
                await action(state);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Queues the specified work to run on the ThreadPool and returns a <see cref="GDTask"/> handle for that work.
        /// </summary>
        /// <typeparam name="T">The type of the result returned by the task.</typeparam>
        /// <param name="func">The work to execute asynchronously</param>
        /// <param name="configureAwait">Returns to the main thread after await if set to true, otherwise, the executing thread is undefined</param>
        /// <param name="cancellationToken">A cancellation token that should be used to cancel the work</param>
        public static async GDTask<T> RunOnThreadPool<T>(Func<T> func, bool configureAwait = true, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await SwitchToThreadPool();

            cancellationToken.ThrowIfCancellationRequested();

            if (configureAwait)
            {
                try
                {
                    return func();
                }
                finally
                {
                    await Yield();
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            else
            {
                return func();
            }
        }

        /// <summary>
        /// Create and queues the specified task to run on the ThreadPool and returns a <see cref="GDTask"/> handle for that work.
        /// </summary>
        /// <typeparam name="T">The type of the result returned by the task.</typeparam>
        /// <param name="func">The delegate which create the task</param>
        /// <param name="configureAwait">Returns to the main thread after await if set to true, otherwise, the executing thread is undefined</param>
        /// <param name="cancellationToken">A cancellation token that should be used to cancel the work</param>
        public static async GDTask<T> RunOnThreadPool<T>(Func<GDTask<T>> func, bool configureAwait = true, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await SwitchToThreadPool();

            cancellationToken.ThrowIfCancellationRequested();

            if (configureAwait)
            {
                try
                {
                    return await func();
                }
                finally
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Yield();
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            else
            {
                var result = await func();
                cancellationToken.ThrowIfCancellationRequested();
                return result;
            }
        }

        /// <summary>
        /// Create and queues the specified task to run on the ThreadPool and returns a <see cref="GDTask"/> handle for that work.
        /// </summary>
        /// <typeparam name="T">The type of the result returned by the task.</typeparam>
        /// <param name="func">The work to execute asynchronously</param>
        /// <param name="state">Object to which to pass to action (may be null)</param>
        /// <param name="configureAwait">Returns to the main thread after await if set to true, otherwise, the executing thread is undefined</param>
        /// <param name="cancellationToken">A cancellation token that should be used to cancel the work</param>
        public static async GDTask<T> RunOnThreadPool<T>(Func<object, T> func, object state, bool configureAwait = true, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await SwitchToThreadPool();

            cancellationToken.ThrowIfCancellationRequested();

            if (configureAwait)
            {
                try
                {
                    return func(state);
                }
                finally
                {
                    await Yield();
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            else
            {
                return func(state);
            }
        }

        /// <summary>
        /// Create and queues the specified task to run on the ThreadPool and returns a <see cref="GDTask"/> handle for that work.
        /// </summary>
        /// <typeparam name="T">The type of the result returned by the task.</typeparam>
        /// <param name="func">The delegate which create the task</param>
        /// <param name="state">Object to which to pass to action (may be null)</param>
        /// <param name="configureAwait">Returns to the main thread after await if set to true, otherwise, the executing thread is undefined</param>
        /// <param name="cancellationToken">A cancellation token that should be used to cancel the work</param>
        public static async GDTask<T> RunOnThreadPool<T>(Func<object, GDTask<T>> func, object state, bool configureAwait = true, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await SwitchToThreadPool();

            cancellationToken.ThrowIfCancellationRequested();

            if (configureAwait)
            {
                try
                {
                    return await func(state);
                }
                finally
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Yield();
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            else
            {
                var result = await func(state);
                cancellationToken.ThrowIfCancellationRequested();
                return result;
            }
        }
    }
}


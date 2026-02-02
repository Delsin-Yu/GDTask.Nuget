using System;
using System.Collections.Generic;
using System.Threading;
using Godot;
using GodotTask.Internal;

namespace GodotTask
{
    public partial struct GDTask
    {
        /// <summary>
        /// Creates a task that will complete at the next provided <see cref="PlayerLoopTiming"/> when the supplied <paramref name="predicate"/> evaluates to true, with specified <see cref="CancellationToken"/>
        /// </summary>
        /// <exception cref="OperationCanceledException">Throws when <paramref name="target"/> GodotObject has been freed.</exception>
        public static GDTask WaitUntil(GodotObject target, Func<bool> predicate, PlayerLoopTiming timing = PlayerLoopTiming.Process, CancellationToken cancellationToken = default)
        {
            return new GDTask(WaitUntilPromise.Create(target, predicate, timing, cancellationToken, out var token), token);
        }
        /// <inheritdoc cref="WaitUntil(GodotObject, Func{bool}, PlayerLoopTiming, CancellationToken)"/>
        public static GDTask WaitUntil(Func<bool> predicate, PlayerLoopTiming timing = PlayerLoopTiming.Process, CancellationToken cancellationToken = default)
        {
            return WaitUntil(null, predicate, timing, cancellationToken);
        }

        /// <summary>
        /// Creates a task that will complete at the next provided <see cref="PlayerLoopTiming"/> when the supplied <paramref name="predicate"/> evaluates to false, with specified <see cref="CancellationToken"/>.
        /// </summary>
        /// <exception cref="OperationCanceledException">Throws when <paramref name="target"/> GodotObject has been freed.</exception>
        public static GDTask WaitWhile(GodotObject target, Func<bool> predicate, PlayerLoopTiming timing = PlayerLoopTiming.Process, CancellationToken cancellationToken = default)
        {
            return new GDTask(WaitWhilePromise.Create(target, predicate, timing, cancellationToken, out var token), token);
        }
        /// <inheritdoc cref="WaitWhile(GodotObject, Func{bool}, PlayerLoopTiming, CancellationToken)"/>
        public static GDTask WaitWhile(Func<bool> predicate, PlayerLoopTiming timing = PlayerLoopTiming.Process, CancellationToken cancellationToken = default)
        {
            return WaitWhile(null, predicate, timing, cancellationToken);
        }

        /// <summary>
        /// Creates a task that will complete at the next provided <see cref="PlayerLoopTiming"/> when the supplied <see cref="CancellationToken"/> is canceled.
        /// </summary>
        public static GDTask WaitUntilCanceled(GodotObject target, CancellationToken cancellationToken, PlayerLoopTiming timing = PlayerLoopTiming.Process)
        {
            return new GDTask(WaitUntilCanceledPromise.Create(target, cancellationToken, timing, out var token), token);
        }
        /// <inheritdoc cref="WaitUntilCanceled(GodotObject, CancellationToken, PlayerLoopTiming)"/>
        public static GDTask WaitUntilCanceled(CancellationToken cancellationToken, PlayerLoopTiming timing = PlayerLoopTiming.Process)
        {
            return WaitUntilCanceled(null, cancellationToken, timing);
        }

        /// <summary>
        /// Creates a task that will complete at the next provided <see cref="PlayerLoopTiming"/> when the provided <paramref name="monitorFunction"/> returns a different value, with specified <see cref="CancellationToken"/>.
        /// </summary>
        public static GDTask<U> WaitUntilValueChanged<T, U>(T target, Func<T, U> monitorFunction, PlayerLoopTiming monitorTiming = PlayerLoopTiming.Process, IEqualityComparer<U> equalityComparer = null, CancellationToken cancellationToken = default)
          where T : class
        {
            return new GDTask<U>(target is GodotObject
                ? WaitUntilValueChangedGodotObjectPromise<T, U>.Create(target, monitorFunction, equalityComparer, monitorTiming, cancellationToken, out var token)
                : WaitUntilValueChangedStandardObjectPromise<T, U>.Create(target, monitorFunction, equalityComparer, monitorTiming, cancellationToken, out token), token);
        }

        private sealed class WaitUntilPromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<WaitUntilPromise>
        {
            private static TaskPool<WaitUntilPromise> pool;
            private WaitUntilPromise nextNode;
            public ref WaitUntilPromise NextNode => ref nextNode;

            static WaitUntilPromise()
            {
                TaskPool.RegisterSizeGetter(typeof(WaitUntilPromise), () => pool.Size);
            }

            private GodotObject target;
            private Func<bool> predicate;
            private CancellationToken cancellationToken;

            private GDTaskCompletionSourceCore<object> core;

            private WaitUntilPromise()
            {
            }

            public static IGDTaskSource Create(GodotObject target, Func<bool> predicate, PlayerLoopTiming timing, CancellationToken cancellationToken, out short token)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!pool.TryPop(out var result))
                {
                    result = new WaitUntilPromise();
                }

                result.target = target;
                result.predicate = predicate;
                result.cancellationToken = cancellationToken;

                TaskTracker.TrackActiveTask(result, 3);

                GDTaskPlayerLoopRunner.AddAction(timing, result);

                token = result.core.Version;
                return result;
            }

            public void GetResult(short token)
            {
                try
                {
                    core.GetResult(token);
                }
                finally
                {
                    TryReturn();
                }
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

            public bool MoveNext()
            {
                if (cancellationToken.IsCancellationRequested || (target is not null && !GodotObject.IsInstanceValid(target))) // Cancel when destroyed
                {
                    core.TrySetCanceled(cancellationToken);
                    return false;
                }

                try
                {
                    if (!predicate())
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    core.TrySetException(ex);
                    return false;
                }

                core.TrySetResult(null);
                return false;
            }

            private bool TryReturn()
            {
                TaskTracker.RemoveTracking(this);
                core.Reset();
                predicate = default;
                cancellationToken = default;
                return pool.TryPush(this);
            }
        }

        private sealed class WaitWhilePromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<WaitWhilePromise>
        {
            private static TaskPool<WaitWhilePromise> pool;
            private WaitWhilePromise nextNode;
            public ref WaitWhilePromise NextNode => ref nextNode;

            static WaitWhilePromise()
            {
                TaskPool.RegisterSizeGetter(typeof(WaitWhilePromise), () => pool.Size);
            }

            private GodotObject target;
            private Func<bool> predicate;
            private CancellationToken cancellationToken;

            private GDTaskCompletionSourceCore<object> core;

            private WaitWhilePromise()
            {
            }

            public static IGDTaskSource Create(GodotObject target, Func<bool> predicate, PlayerLoopTiming timing, CancellationToken cancellationToken, out short token)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!pool.TryPop(out var result))
                {
                    result = new WaitWhilePromise();
                }

                result.target = target;
                result.predicate = predicate;
                result.cancellationToken = cancellationToken;

                TaskTracker.TrackActiveTask(result, 3);

                GDTaskPlayerLoopRunner.AddAction(timing, result);

                token = result.core.Version;
                return result;
            }

            public void GetResult(short token)
            {
                try
                {
                    core.GetResult(token);
                }
                finally
                {
                    TryReturn();
                }
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

            public bool MoveNext()
            {
                if (cancellationToken.IsCancellationRequested || (target is not null && !GodotObject.IsInstanceValid(target))) // Cancel when destroyed
                {
                    core.TrySetCanceled(cancellationToken);
                    return false;
                }

                try
                {
                    if (predicate())
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    core.TrySetException(ex);
                    return false;
                }

                core.TrySetResult(null);
                return false;
            }

            private bool TryReturn()
            {
                TaskTracker.RemoveTracking(this);
                core.Reset();
                predicate = default;
                cancellationToken = default;
                return pool.TryPush(this);
            }
        }

        private sealed class WaitUntilCanceledPromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<WaitUntilCanceledPromise>
        {
            private static TaskPool<WaitUntilCanceledPromise> pool;
            private WaitUntilCanceledPromise nextNode;
            public ref WaitUntilCanceledPromise NextNode => ref nextNode;

            static WaitUntilCanceledPromise()
            {
                TaskPool.RegisterSizeGetter(typeof(WaitUntilCanceledPromise), () => pool.Size);
            }

            private GodotObject target;
            private CancellationToken cancellationToken;

            private GDTaskCompletionSourceCore<object> core;

            private WaitUntilCanceledPromise()
            {
            }

            public static IGDTaskSource Create(GodotObject target, CancellationToken cancellationToken, PlayerLoopTiming timing, out short token)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!pool.TryPop(out var result))
                {
                    result = new WaitUntilCanceledPromise();
                }

                result.target = target;
                result.cancellationToken = cancellationToken;

                TaskTracker.TrackActiveTask(result, 3);

                GDTaskPlayerLoopRunner.AddAction(timing, result);

                token = result.core.Version;
                return result;
            }

            public void GetResult(short token)
            {
                try
                {
                    core.GetResult(token);
                }
                finally
                {
                    TryReturn();
                }
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

            public bool MoveNext()
            {
                if (cancellationToken.IsCancellationRequested || (target is not null && !GodotObject.IsInstanceValid(target))) // Cancel when destroyed
                {
                    core.TrySetResult(null);
                    return false;
                }

                return true;
            }

            private bool TryReturn()
            {
                TaskTracker.RemoveTracking(this);
                core.Reset();
                cancellationToken = default;
                return pool.TryPush(this);
            }
        }

        // Cannot add `where T : GodotObject` because `WaitUntilValueChanged` doesn't have the constraint.
        private sealed class WaitUntilValueChangedGodotObjectPromise<T, U> : IGDTaskSource<U>, IPlayerLoopItem, ITaskPoolNode<WaitUntilValueChangedGodotObjectPromise<T, U>>
        {
            private static TaskPool<WaitUntilValueChangedGodotObjectPromise<T, U>> pool;
            private WaitUntilValueChangedGodotObjectPromise<T, U> nextNode;
            public ref WaitUntilValueChangedGodotObjectPromise<T, U> NextNode => ref nextNode;

            static WaitUntilValueChangedGodotObjectPromise()
            {
                TaskPool.RegisterSizeGetter(typeof(WaitUntilValueChangedGodotObjectPromise<T, U>), () => pool.Size);
            }

            private T target;
            private GodotObject targetGodotObject;
            private U currentValue;
            private Func<T, U> monitorFunction;
            private IEqualityComparer<U> equalityComparer;
            private CancellationToken cancellationToken;

            private GDTaskCompletionSourceCore<U> core;

            private WaitUntilValueChangedGodotObjectPromise()
            {
            }

            public static IGDTaskSource<U> Create(T target, Func<T, U> monitorFunction, IEqualityComparer<U> equalityComparer, PlayerLoopTiming timing, CancellationToken cancellationToken, out short token)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return AutoResetGDTaskCompletionSource<U>.CreateFromCanceled(cancellationToken, out token);
                }

                if (!pool.TryPop(out var result))
                {
                    result = new WaitUntilValueChangedGodotObjectPromise<T, U>();
                }

                result.target = target;
                result.targetGodotObject = target as GodotObject;
                result.monitorFunction = monitorFunction;
                result.currentValue = monitorFunction(target);
                result.equalityComparer = equalityComparer ?? EqualityComparer<U>.Default;
                result.cancellationToken = cancellationToken;

                TaskTracker.TrackActiveTask(result, 3);

                GDTaskPlayerLoopRunner.AddAction(timing, result);

                token = result.core.Version;
                return result;
            }

            public U GetResult(short token)
            {
                try
                {
                    return core.GetResult(token);
                }
                finally
                {
                    TryReturn();
                }
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

            public bool MoveNext()
            {
                if (cancellationToken.IsCancellationRequested || (target is not null && !GodotObject.IsInstanceValid(targetGodotObject))) // Cancel when destroyed
                {
                    core.TrySetCanceled(cancellationToken);
                    return false;
                }

                U nextValue;
                try
                {
                    nextValue = monitorFunction(target);
                    if (equalityComparer.Equals(currentValue, nextValue))
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    core.TrySetException(ex);
                    return false;
                }

                core.TrySetResult(nextValue);
                return false;
            }

            private bool TryReturn()
            {
                TaskTracker.RemoveTracking(this);
                core.Reset();
                target = default;
                currentValue = default;
                monitorFunction = default;
                equalityComparer = default;
                cancellationToken = default;
                return pool.TryPush(this);
            }
        }

        private sealed class WaitUntilValueChangedStandardObjectPromise<T, U> : IGDTaskSource<U>, IPlayerLoopItem, ITaskPoolNode<WaitUntilValueChangedStandardObjectPromise<T, U>>
            where T : class
        {
            private static TaskPool<WaitUntilValueChangedStandardObjectPromise<T, U>> pool;
            private WaitUntilValueChangedStandardObjectPromise<T, U> nextNode;
            public ref WaitUntilValueChangedStandardObjectPromise<T, U> NextNode => ref nextNode;

            static WaitUntilValueChangedStandardObjectPromise()
            {
                TaskPool.RegisterSizeGetter(typeof(WaitUntilValueChangedStandardObjectPromise<T, U>), () => pool.Size);
            }

            private WeakReference<T> target;
            private U currentValue;
            private Func<T, U> monitorFunction;
            private IEqualityComparer<U> equalityComparer;
            private CancellationToken cancellationToken;

            private GDTaskCompletionSourceCore<U> core;

            private WaitUntilValueChangedStandardObjectPromise()
            {
            }

            public static IGDTaskSource<U> Create(T target, Func<T, U> monitorFunction, IEqualityComparer<U> equalityComparer, PlayerLoopTiming timing, CancellationToken cancellationToken, out short token)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return AutoResetGDTaskCompletionSource<U>.CreateFromCanceled(cancellationToken, out token);
                }

                if (!pool.TryPop(out var result))
                {
                    result = new WaitUntilValueChangedStandardObjectPromise<T, U>();
                }

                result.target = new WeakReference<T>(target, false); // wrap in WeakReference.
                result.monitorFunction = monitorFunction;
                result.currentValue = monitorFunction(target);
                result.equalityComparer = equalityComparer ?? EqualityComparer<U>.Default;
                result.cancellationToken = cancellationToken;

                TaskTracker.TrackActiveTask(result, 3);

                GDTaskPlayerLoopRunner.AddAction(timing, result);

                token = result.core.Version;
                return result;
            }

            public U GetResult(short token)
            {
                try
                {
                    return core.GetResult(token);
                }
                finally
                {
                    TryReturn();
                }
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

            public bool MoveNext()
            {
                if (cancellationToken.IsCancellationRequested || !target.TryGetTarget(out var t)) // doesn't find = cancel.
                {
                    core.TrySetCanceled(cancellationToken);
                    return false;
                }

                U nextValue;
                try
                {
                    nextValue = monitorFunction(t);
                    if (equalityComparer.Equals(currentValue, nextValue))
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    core.TrySetException(ex);
                    return false;
                }

                core.TrySetResult(nextValue);
                return false;
            }

            private bool TryReturn()
            {
                TaskTracker.RemoveTracking(this);
                core.Reset();
                target = default;
                currentValue = default;
                monitorFunction = default;
                equalityComparer = default;
                cancellationToken = default;
                return pool.TryPush(this);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using Godot;
using Error = GodotTask.Internal.Error;

namespace GodotTask;

public partial struct GDTask
{
    /// <summary>
    /// Creates a task that will complete at the next provided <see cref="PlayerLoopTiming" /> when the supplied
    /// <paramref name="predicate" /> evaluates to true, with specified <see cref="CancellationToken" />
    /// </summary>
    /// <exception cref="OperationCanceledException">Throws when <paramref name="target" /> GodotObject has been freed.</exception>
    public static GDTask WaitUntil(GodotObject target, Func<bool> predicate, PlayerLoopTiming timing = PlayerLoopTiming.Process, CancellationToken cancellationToken = default) => new(WaitUntilPromise.Create(target, predicate, GDTaskScheduler.GetPlayerLoop(timing), cancellationToken, out var token), token);

    /// <inheritdoc cref="WaitUntil(GodotObject, Func{bool}, PlayerLoopTiming, CancellationToken)" />
    public static GDTask WaitUntil(Func<bool> predicate, PlayerLoopTiming timing = PlayerLoopTiming.Process, CancellationToken cancellationToken = default) => WaitUntil(null, predicate, timing, cancellationToken);

    /// <inheritdoc cref="WaitUntil(Func{bool}, PlayerLoopTiming, CancellationToken)" />
    public static GDTask WaitUntil(Func<bool> predicate, CancellationToken cancellationToken) => WaitUntil(predicate, PlayerLoopTiming.Process, cancellationToken);

    /// <summary>
    /// Creates a task that will complete at the next provided <see cref="IPlayerLoop" /> when the supplied
    /// <paramref name="predicate" /> evaluates to true, with specified <see cref="CancellationToken" />
    /// </summary>
    /// <exception cref="OperationCanceledException">Throws when <paramref name="target" /> GodotObject has been freed.</exception>
    public static GDTask WaitUntil(GodotObject target, Func<bool> predicate, IPlayerLoop playerLoop, CancellationToken cancellationToken = default)
    {
        Error.ThrowArgumentNullException(playerLoop, nameof(playerLoop));
        return new(WaitUntilPromise.Create(target, predicate, playerLoop, cancellationToken, out var token), token);
    }

    /// <inheritdoc cref="WaitUntil(GodotObject, Func{bool}, IPlayerLoop, CancellationToken)" />
    public static GDTask WaitUntil(Func<bool> predicate, IPlayerLoop playerLoop, CancellationToken cancellationToken = default) => WaitUntil(null, predicate, playerLoop, cancellationToken);

    /// <summary>
    /// Creates a task that will complete at the next provided <see cref="PlayerLoopTiming" /> when the supplied
    /// <paramref name="predicate" /> evaluates to false, with specified <see cref="CancellationToken" />.
    /// </summary>
    /// <exception cref="OperationCanceledException">Throws when <paramref name="target" /> GodotObject has been freed.</exception>
    public static GDTask WaitWhile(GodotObject target, Func<bool> predicate, PlayerLoopTiming timing = PlayerLoopTiming.Process, CancellationToken cancellationToken = default) => new(WaitWhilePromise.Create(target, predicate, GDTaskScheduler.GetPlayerLoop(timing), cancellationToken, out var token), token);

    /// <inheritdoc cref="WaitWhile(GodotObject, Func{bool}, PlayerLoopTiming, CancellationToken)" />
    public static GDTask WaitWhile(Func<bool> predicate, PlayerLoopTiming timing = PlayerLoopTiming.Process, CancellationToken cancellationToken = default) => WaitWhile(null, predicate, timing, cancellationToken);

    /// <inheritdoc cref="WaitWhile(Func{bool}, PlayerLoopTiming, CancellationToken)" />
    public static GDTask WaitWhile(Func<bool> predicate, CancellationToken cancellationToken) => WaitWhile(predicate, PlayerLoopTiming.Process, cancellationToken);

    /// <summary>
    /// Creates a task that will complete at the next provided <see cref="IPlayerLoop" /> when the supplied
    /// <paramref name="predicate" /> evaluates to false, with specified <see cref="CancellationToken" />.
    /// </summary>
    /// <exception cref="OperationCanceledException">Throws when <paramref name="target" /> GodotObject has been freed.</exception>
    public static GDTask WaitWhile(GodotObject target, Func<bool> predicate, IPlayerLoop playerLoop, CancellationToken cancellationToken = default)
    {
        Error.ThrowArgumentNullException(playerLoop, nameof(playerLoop));
        return new(WaitWhilePromise.Create(target, predicate, playerLoop, cancellationToken, out var token), token);
    }

    /// <inheritdoc cref="WaitWhile(GodotObject, Func{bool}, IPlayerLoop, CancellationToken)" />
    public static GDTask WaitWhile(Func<bool> predicate, IPlayerLoop playerLoop, CancellationToken cancellationToken = default) => WaitWhile(null, predicate, playerLoop, cancellationToken);

    /// <summary>
    /// Creates a task that will complete at the next provided <see cref="PlayerLoopTiming" /> when the supplied
    /// <see cref="CancellationToken" /> is canceled.
    /// </summary>
    public static GDTask WaitUntilCanceled(GodotObject target, CancellationToken cancellationToken, PlayerLoopTiming timing = PlayerLoopTiming.Process) => new(WaitUntilCanceledPromise.Create(target, cancellationToken, GDTaskScheduler.GetPlayerLoop(timing), out var token), token);

    /// <inheritdoc cref="WaitUntilCanceled(GodotObject, CancellationToken, PlayerLoopTiming)" />
    public static GDTask WaitUntilCanceled(CancellationToken cancellationToken, PlayerLoopTiming timing = PlayerLoopTiming.Process) => WaitUntilCanceled(null, cancellationToken, timing);

    /// <summary>
    /// Creates a task that will complete at the next provided <see cref="IPlayerLoop" /> when the supplied
    /// <see cref="CancellationToken" /> is canceled.
    /// </summary>
    public static GDTask WaitUntilCanceled(GodotObject target, CancellationToken cancellationToken, IPlayerLoop playerLoop)
    {
        Error.ThrowArgumentNullException(playerLoop, nameof(playerLoop));
        return new(WaitUntilCanceledPromise.Create(target, cancellationToken, playerLoop, out var token), token);
    }

    /// <inheritdoc cref="WaitUntilCanceled(GodotObject, CancellationToken, IPlayerLoop)" />
    public static GDTask WaitUntilCanceled(CancellationToken cancellationToken, IPlayerLoop playerLoop) => WaitUntilCanceled(null, cancellationToken, playerLoop);

    /// <summary>
    /// Creates a task that will complete at the next provided <see cref="PlayerLoopTiming" /> when the provided
    /// <paramref name="monitorFunction" /> returns a different value, with specified <see cref="CancellationToken" />.
    /// </summary>
    public static GDTask<TU> WaitUntilValueChanged<T, TU>(T target, Func<T, TU> monitorFunction, PlayerLoopTiming monitorTiming = PlayerLoopTiming.Process, IEqualityComparer<TU> equalityComparer = null, CancellationToken cancellationToken = default)
        where T : class =>
        new(
            target is GodotObject
                ? WaitUntilValueChangedGodotObjectPromise<T, TU>.Create(target, monitorFunction, equalityComparer, GDTaskScheduler.GetPlayerLoop(monitorTiming), cancellationToken, out var token)
                : WaitUntilValueChangedStandardObjectPromise<T, TU>.Create(target, monitorFunction, equalityComparer, GDTaskScheduler.GetPlayerLoop(monitorTiming), cancellationToken, out token),
            token
        );

    /// <summary>
    /// Creates a task that will complete at the next provided <see cref="IPlayerLoop" /> when the provided
    /// <paramref name="monitorFunction" /> returns a different value, with specified <see cref="CancellationToken" />.
    /// </summary>
    public static GDTask<TU> WaitUntilValueChanged<T, TU>(T target, Func<T, TU> monitorFunction, IPlayerLoop monitorLoop, IEqualityComparer<TU> equalityComparer = null, CancellationToken cancellationToken = default)
        where T : class
    {
        Error.ThrowArgumentNullException(monitorLoop, nameof(monitorLoop));
        return new(
            target is GodotObject
                ? WaitUntilValueChangedGodotObjectPromise<T, TU>.Create(target, monitorFunction, equalityComparer, monitorLoop, cancellationToken, out var token)
                : WaitUntilValueChangedStandardObjectPromise<T, TU>.Create(target, monitorFunction, equalityComparer, monitorLoop, cancellationToken, out token),
            token
        );
    }

    private sealed class WaitUntilPromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<WaitUntilPromise>
    {
        private static TaskPool<WaitUntilPromise> Pool;
        private CancellationToken _cancellationToken;

        private GDTaskCompletionSourceCore<object> _core;
        private WaitUntilPromise _nextNode;
        private Func<bool> _predicate;

        private GodotObject _target;

        private WaitUntilPromise() { }

        public void GetResult(short token)
        {
            try { _core.GetResult(token); }
            finally { TryReturn(); }
        }

        public GDTaskStatus GetStatus(short token) => _core.GetStatus(token);

        public GDTaskStatus UnsafeGetStatus() => _core.UnsafeGetStatus();

        public void OnCompleted(Action<object> continuation, object state, short token) => _core.OnCompleted(continuation, state, token);

        public bool MoveNext(double deltaTime)
        {
            if (_cancellationToken.IsCancellationRequested || _target is not null && !GodotObject.IsInstanceValid(_target)) // Cancel when destroyed
            {
                _core.TrySetCanceled(_cancellationToken);
                return false;
            }

            try
            {
                if (!_predicate()) return true;
            }
            catch (Exception ex)
            {
                _core.TrySetException(ex);
                return false;
            }

            _core.TrySetResult(null);
            return false;
        }

        public ref WaitUntilPromise NextNode => ref _nextNode;

        public static IGDTaskSource Create(GodotObject target, Func<bool> predicate, IPlayerLoop playerLoop, CancellationToken cancellationToken, out short token)
        {
            if (cancellationToken.IsCancellationRequested) return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);

            if (!Pool.TryPop(out var result)) result = new();

            result._target = target;
            result._predicate = predicate;
            result._cancellationToken = cancellationToken;

            TaskTracker.TrackActiveTask(result, 3);

            GDTaskScheduler.AddAction(playerLoop, result);

            token = result._core.Version;
            return result;
        }

        private bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            _core.Reset();
            _predicate = default;
            _cancellationToken = default;
            return Pool.TryPush(this);
        }
    }

    private sealed class WaitWhilePromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<WaitWhilePromise>
    {
        private static TaskPool<WaitWhilePromise> Pool;
        private CancellationToken _cancellationToken;

        private GDTaskCompletionSourceCore<object> _core;
        private WaitWhilePromise _nextNode;
        private Func<bool> _predicate;

        private GodotObject _target;

        private WaitWhilePromise() { }

        public void GetResult(short token)
        {
            try { _core.GetResult(token); }
            finally { TryReturn(); }
        }

        public GDTaskStatus GetStatus(short token) => _core.GetStatus(token);

        public GDTaskStatus UnsafeGetStatus() => _core.UnsafeGetStatus();

        public void OnCompleted(Action<object> continuation, object state, short token) => _core.OnCompleted(continuation, state, token);

        public bool MoveNext(double deltaTime)
        {
            if (_cancellationToken.IsCancellationRequested || _target is not null && !GodotObject.IsInstanceValid(_target)) // Cancel when destroyed
            {
                _core.TrySetCanceled(_cancellationToken);
                return false;
            }

            try
            {
                if (_predicate()) return true;
            }
            catch (Exception ex)
            {
                _core.TrySetException(ex);
                return false;
            }

            _core.TrySetResult(null);
            return false;
        }

        public ref WaitWhilePromise NextNode => ref _nextNode;

        public static IGDTaskSource Create(GodotObject target, Func<bool> predicate, IPlayerLoop playerLoop, CancellationToken cancellationToken, out short token)
        {
            if (cancellationToken.IsCancellationRequested) return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);

            if (!Pool.TryPop(out var result)) result = new();

            result._target = target;
            result._predicate = predicate;
            result._cancellationToken = cancellationToken;

            TaskTracker.TrackActiveTask(result, 3);

            GDTaskScheduler.AddAction(playerLoop, result);

            token = result._core.Version;
            return result;
        }

        private bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            _core.Reset();
            _predicate = default;
            _cancellationToken = default;
            return Pool.TryPush(this);
        }
    }

    private sealed class WaitUntilCanceledPromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<WaitUntilCanceledPromise>
    {
        private static TaskPool<WaitUntilCanceledPromise> Pool;
        private CancellationToken _cancellationToken;

        private GDTaskCompletionSourceCore<object> _core;
        private WaitUntilCanceledPromise _nextNode;

        private GodotObject _target;

        private WaitUntilCanceledPromise() { }

        public void GetResult(short token)
        {
            try { _core.GetResult(token); }
            finally { TryReturn(); }
        }

        public GDTaskStatus GetStatus(short token) => _core.GetStatus(token);

        public GDTaskStatus UnsafeGetStatus() => _core.UnsafeGetStatus();

        public void OnCompleted(Action<object> continuation, object state, short token) => _core.OnCompleted(continuation, state, token);

        public bool MoveNext(double deltaTime)
        {
            if (_cancellationToken.IsCancellationRequested || _target is not null && !GodotObject.IsInstanceValid(_target)) // Cancel when destroyed
            {
                _core.TrySetResult(null);
                return false;
            }

            return true;
        }

        public ref WaitUntilCanceledPromise NextNode => ref _nextNode;

        public static IGDTaskSource Create(GodotObject target, CancellationToken cancellationToken, IPlayerLoop playerLoop, out short token)
        {
            if (cancellationToken.IsCancellationRequested) return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);

            if (!Pool.TryPop(out var result)) result = new();

            result._target = target;
            result._cancellationToken = cancellationToken;

            TaskTracker.TrackActiveTask(result, 3);

            GDTaskScheduler.AddAction(playerLoop, result);

            token = result._core.Version;
            return result;
        }

        private bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            _core.Reset();
            _cancellationToken = default;
            return Pool.TryPush(this);
        }
    }

    // Cannot add `where T : GodotObject` because `WaitUntilValueChanged` doesn't have the constraint.
    private sealed class WaitUntilValueChangedGodotObjectPromise<T, TU> : IGDTaskSource<TU>, IPlayerLoopItem, ITaskPoolNode<WaitUntilValueChangedGodotObjectPromise<T, TU>>
    {
        private static TaskPool<WaitUntilValueChangedGodotObjectPromise<T, TU>> Pool;
        private CancellationToken _cancellationToken;

        private GDTaskCompletionSourceCore<TU> _core;
        private TU _currentValue;
        private IEqualityComparer<TU> _equalityComparer;
        private Func<T, TU> _monitorFunction;
        private WaitUntilValueChangedGodotObjectPromise<T, TU> _nextNode;

        private T _target;
        private GodotObject _targetGodotObject;

        private WaitUntilValueChangedGodotObjectPromise() { }

        public TU GetResult(short token)
        {
            try { return _core.GetResult(token); }
            finally { TryReturn(); }
        }

        void IGDTaskSource.GetResult(short token) => GetResult(token);

        public GDTaskStatus GetStatus(short token) => _core.GetStatus(token);

        public GDTaskStatus UnsafeGetStatus() => _core.UnsafeGetStatus();

        public void OnCompleted(Action<object> continuation, object state, short token) => _core.OnCompleted(continuation, state, token);

        public bool MoveNext(double deltaTime)
        {
            if (_cancellationToken.IsCancellationRequested || _target is not null && !GodotObject.IsInstanceValid(_targetGodotObject)) // Cancel when destroyed
            {
                _core.TrySetCanceled(_cancellationToken);
                return false;
            }

            TU nextValue;

            try
            {
                nextValue = _monitorFunction(_target);
                if (_equalityComparer.Equals(_currentValue, nextValue)) return true;
            }
            catch (Exception ex)
            {
                _core.TrySetException(ex);
                return false;
            }

            _core.TrySetResult(nextValue);
            return false;
        }

        public ref WaitUntilValueChangedGodotObjectPromise<T, TU> NextNode => ref _nextNode;

        public static IGDTaskSource<TU> Create(T target, Func<T, TU> monitorFunction, IEqualityComparer<TU> equalityComparer, IPlayerLoop playerLoop, CancellationToken cancellationToken, out short token)
        {
            if (cancellationToken.IsCancellationRequested) return AutoResetGDTaskCompletionSource<TU>.CreateFromCanceled(cancellationToken, out token);

            if (!Pool.TryPop(out var result)) result = new();

            result._target = target;
            result._targetGodotObject = target as GodotObject;
            result._monitorFunction = monitorFunction;
            result._currentValue = monitorFunction(target);
            result._equalityComparer = equalityComparer ?? EqualityComparer<TU>.Default;
            result._cancellationToken = cancellationToken;

            TaskTracker.TrackActiveTask(result, 3);

            GDTaskScheduler.AddAction(playerLoop, result);

            token = result._core.Version;
            return result;
        }

        private bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            _core.Reset();
            _target = default;
            _currentValue = default;
            _monitorFunction = default;
            _equalityComparer = default;
            _cancellationToken = default;
            return Pool.TryPush(this);
        }
    }

    private sealed class WaitUntilValueChangedStandardObjectPromise<T, TU> : IGDTaskSource<TU>, IPlayerLoopItem, ITaskPoolNode<WaitUntilValueChangedStandardObjectPromise<T, TU>>
        where T : class
    {
        private static TaskPool<WaitUntilValueChangedStandardObjectPromise<T, TU>> Pool;
        private CancellationToken _cancellationToken;

        private GDTaskCompletionSourceCore<TU> _core;
        private TU _currentValue;
        private IEqualityComparer<TU> _equalityComparer;
        private Func<T, TU> _monitorFunction;
        private WaitUntilValueChangedStandardObjectPromise<T, TU> _nextNode;

        private WeakReference<T> _target;

        private WaitUntilValueChangedStandardObjectPromise() { }

        public TU GetResult(short token)
        {
            try { return _core.GetResult(token); }
            finally { TryReturn(); }
        }

        void IGDTaskSource.GetResult(short token) => GetResult(token);

        public GDTaskStatus GetStatus(short token) => _core.GetStatus(token);

        public GDTaskStatus UnsafeGetStatus() => _core.UnsafeGetStatus();

        public void OnCompleted(Action<object> continuation, object state, short token) => _core.OnCompleted(continuation, state, token);

        public bool MoveNext(double deltaTime)
        {
            if (_cancellationToken.IsCancellationRequested || !_target.TryGetTarget(out var t)) // doesn't find = cancel.
            {
                _core.TrySetCanceled(_cancellationToken);
                return false;
            }

            TU nextValue;

            try
            {
                nextValue = _monitorFunction(t);
                if (_equalityComparer.Equals(_currentValue, nextValue)) return true;
            }
            catch (Exception ex)
            {
                _core.TrySetException(ex);
                return false;
            }

            _core.TrySetResult(nextValue);
            return false;
        }

        public ref WaitUntilValueChangedStandardObjectPromise<T, TU> NextNode => ref _nextNode;

        public static IGDTaskSource<TU> Create(T target, Func<T, TU> monitorFunction, IEqualityComparer<TU> equalityComparer, IPlayerLoop playerLoop, CancellationToken cancellationToken, out short token)
        {
            if (cancellationToken.IsCancellationRequested) return AutoResetGDTaskCompletionSource<TU>.CreateFromCanceled(cancellationToken, out token);

            if (!Pool.TryPop(out var result)) result = new();

            result._target = new(target, false); // wrap in WeakReference.
            result._monitorFunction = monitorFunction;
            result._currentValue = monitorFunction(target);
            result._equalityComparer = equalityComparer ?? EqualityComparer<TU>.Default;
            result._cancellationToken = cancellationToken;

            TaskTracker.TrackActiveTask(result, 3);

            GDTaskScheduler.AddAction(playerLoop, result);

            token = result._core.Version;
            return result;
        }

        private bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            _core.Reset();
            _target = default;
            _currentValue = default;
            _monitorFunction = default;
            _equalityComparer = default;
            _cancellationToken = default;
            return Pool.TryPush(this);
        }
    }
}
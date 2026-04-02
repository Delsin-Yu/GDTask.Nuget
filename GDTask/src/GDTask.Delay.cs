using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Godot;
using GodotTask.Internal;
using Error = GodotTask.Internal.Error;

namespace GodotTask;

/// <summary>
/// Indicates the time provider used for Delaying
/// </summary>
public enum DelayType
{
    /// <summary>Use scaled delta time provided from <see cref="Node._Process" /></summary>
    DeltaTime,
    /// <summary>Use time provided from <see cref="System.Diagnostics.Stopwatch.GetTimestamp()" /></summary>
    Realtime,
}

public partial struct GDTask
{
    /// <summary>
    /// Delay the execution until the next <see cref="PlayerLoopTiming.Process" />.
    /// </summary>
    public static YieldAwaitable Yield() =>
        // optimized for single continuation
        new(GDTaskScheduler.GetPlayerLoop(PlayerLoopTiming.Process));

    /// <summary>
    /// Delay the execution until the next provided <see cref="PlayerLoopTiming" />.
    /// </summary>
    public static YieldAwaitable Yield(PlayerLoopTiming timing) =>
        // optimized for single continuation
        new(GDTaskScheduler.GetPlayerLoop(timing));

    /// <summary>
    /// Delay the execution until the next provided <see cref="IPlayerLoop" />.
    /// </summary>
    public static YieldAwaitable Yield(IPlayerLoop playerLoop)
    {
        Error.ThrowArgumentNullException(playerLoop, nameof(playerLoop));
        return new(playerLoop);
    }

    /// <summary>
    /// Delay the execution until the next <see cref="PlayerLoopTiming.Process" />, with specified
    /// <see cref="CancellationToken" />.
    /// </summary>
    public static GDTask Yield(CancellationToken cancellationToken) => new(YieldPromise.Create(GDTaskScheduler.GetPlayerLoop(PlayerLoopTiming.Process), cancellationToken, out var token), token);

    /// <summary>
    /// Delay the execution until the next provided <see cref="PlayerLoopTiming" />, with specified
    /// <see cref="CancellationToken" />.
    /// </summary>
    public static GDTask Yield(PlayerLoopTiming timing, CancellationToken cancellationToken) => new(YieldPromise.Create(GDTaskScheduler.GetPlayerLoop(timing), cancellationToken, out var token), token);

    /// <summary>
    /// Delay the execution until the next provided <see cref="IPlayerLoop" />, with specified <see cref="CancellationToken" />
    /// .
    /// </summary>
    public static GDTask Yield(IPlayerLoop playerLoop, CancellationToken cancellationToken)
    {
        Error.ThrowArgumentNullException(playerLoop, nameof(playerLoop));
        return new(YieldPromise.Create(playerLoop, cancellationToken, out var token), token);
    }

    /// <summary>
    /// Delay the execution until the next frame of <see cref="PlayerLoopTiming.Process" />.
    /// </summary>
    public static GDTask NextFrame() => new(NextFramePromise.Create(GDTaskScheduler.GetPlayerLoop(PlayerLoopTiming.Process), CancellationToken.None, out var token), token);

    /// <summary>
    /// Delay the execution until the next frame of the provided <see cref="PlayerLoopTiming" />.
    /// </summary>
    public static GDTask NextFrame(PlayerLoopTiming timing) => new(NextFramePromise.Create(GDTaskScheduler.GetPlayerLoop(timing), CancellationToken.None, out var token), token);

    /// <summary>
    /// Delay the execution until the next frame of the provided <see cref="IPlayerLoop" />.
    /// </summary>
    public static GDTask NextFrame(IPlayerLoop playerLoop)
    {
        Error.ThrowArgumentNullException(playerLoop, nameof(playerLoop));
        return new(NextFramePromise.Create(playerLoop, CancellationToken.None, out var token), token);
    }

    /// <summary>
    /// Delay the execution until the next frame of <see cref="PlayerLoopTiming.Process" />, with specified
    /// <see cref="CancellationToken" />.
    /// </summary>
    public static GDTask NextFrame(CancellationToken cancellationToken) => new(NextFramePromise.Create(GDTaskScheduler.GetPlayerLoop(PlayerLoopTiming.Process), cancellationToken, out var token), token);

    /// <summary>
    /// Delay the execution until the next frame of the provided <see cref="PlayerLoopTiming" />, with specified
    /// <see cref="CancellationToken" />.
    /// </summary>
    public static GDTask NextFrame(PlayerLoopTiming timing, CancellationToken cancellationToken) => new(NextFramePromise.Create(GDTaskScheduler.GetPlayerLoop(timing), cancellationToken, out var token), token);

    /// <summary>
    /// Delay the execution until the next frame of the provided <see cref="IPlayerLoop" />, with specified
    /// <see cref="CancellationToken" />.
    /// </summary>
    public static GDTask NextFrame(IPlayerLoop playerLoop, CancellationToken cancellationToken)
    {
        Error.ThrowArgumentNullException(playerLoop, nameof(playerLoop));
        return new(NextFramePromise.Create(playerLoop, cancellationToken, out var token), token);
    }

    /// <inheritdoc cref="Yield()" />
    public static YieldAwaitable WaitForEndOfFrame() => Yield(PlayerLoopTiming.Process);

    /// <inheritdoc cref="Yield(CancellationToken)" />
    public static GDTask WaitForEndOfFrame(CancellationToken cancellationToken) => Yield(PlayerLoopTiming.Process, cancellationToken);

    /// <summary>
    /// Delay the execution until the next <see cref="PlayerLoopTiming.PhysicsProcess" />.
    /// </summary>
    public static YieldAwaitable WaitForPhysicsProcess() => Yield(PlayerLoopTiming.PhysicsProcess);

    /// <summary>
    /// Delay the execution until the next <see cref="PlayerLoopTiming.PhysicsProcess" />, with specified
    /// <see cref="CancellationToken" />.
    /// </summary>
    public static GDTask WaitForPhysicsProcess(CancellationToken cancellationToken) => Yield(PlayerLoopTiming.PhysicsProcess, cancellationToken);

    /// <summary>
    /// Delay the execution after frame(s) of the provided <see cref="PlayerLoopTiming" />, with specified
    /// <see cref="CancellationToken" />.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="delayFrameCount" /> is less than 0.</exception>
    public static GDTask DelayFrame(int delayFrameCount, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process, CancellationToken cancellationToken = default)
    {
        if (delayFrameCount < 0) throw new ArgumentOutOfRangeException("Delay does not allow minus delayFrameCount. delayFrameCount:" + delayFrameCount);

        return new(DelayFramePromise.Create(delayFrameCount, GDTaskScheduler.GetPlayerLoop(delayTiming), cancellationToken, out var token), token);
    }

    /// <inheritdoc cref="DelayFrame(int, PlayerLoopTiming, CancellationToken)" />
    public static GDTask DelayFrame(int delayFrameCount, CancellationToken cancellationToken) => DelayFrame(delayFrameCount, PlayerLoopTiming.Process, cancellationToken);

    /// <summary>
    /// Delay the execution after frame(s) of the provided <see cref="IPlayerLoop" />, with specified
    /// <see cref="CancellationToken" />.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="delayFrameCount" /> is less than 0.</exception>
    public static GDTask DelayFrame(int delayFrameCount, IPlayerLoop delayLoop, CancellationToken cancellationToken = default)
    {
        if (delayFrameCount < 0) throw new ArgumentOutOfRangeException("Delay does not allow minus delayFrameCount. delayFrameCount:" + delayFrameCount);

        Error.ThrowArgumentNullException(delayLoop, nameof(delayLoop));
        return new(DelayFramePromise.Create(delayFrameCount, delayLoop, cancellationToken, out var token), token);
    }

    /// <summary>
    /// Delay the execution after <paramref name="millisecondsDelay" /> on provided <see cref="PlayerLoopTiming" /> with
    /// <see cref="DelayType.DeltaTime" /> provider, with specified <see cref="CancellationToken" />.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="millisecondsDelay" /> is less than 0.</exception>
    public static GDTask Delay(int millisecondsDelay, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process, CancellationToken cancellationToken = default)
    {
        var delayTimeSpan = TimeSpan.FromMilliseconds(millisecondsDelay);
        return Delay(delayTimeSpan, delayTiming, cancellationToken);
    }

    /// <inheritdoc cref="Delay(int, PlayerLoopTiming, CancellationToken)" />
    public static GDTask Delay(int millisecondsDelay, CancellationToken cancellationToken) => Delay(millisecondsDelay, PlayerLoopTiming.Process, cancellationToken);

    /// <summary>
    /// Delay the execution after <paramref name="millisecondsDelay" /> on provided <see cref="IPlayerLoop" /> with
    /// <see cref="DelayType.DeltaTime" /> provider, with specified <see cref="CancellationToken" />.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="millisecondsDelay" /> is less than 0.</exception>
    public static GDTask Delay(int millisecondsDelay, IPlayerLoop delayLoop, CancellationToken cancellationToken = default)
    {
        var delayTimeSpan = TimeSpan.FromMilliseconds(millisecondsDelay);
        return Delay(delayTimeSpan, delayLoop, cancellationToken);
    }

    /// <summary>
    /// Delay the execution after <paramref name="delayTimeSpan" /> on provided <see cref="PlayerLoopTiming" /> with
    /// <see cref="DelayType.DeltaTime" /> provider, with specified <see cref="CancellationToken" />.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="delayTimeSpan" /> is less than 0.</exception>
    public static GDTask Delay(TimeSpan delayTimeSpan, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process, CancellationToken cancellationToken = default) => Delay(delayTimeSpan, DelayType.DeltaTime, delayTiming, cancellationToken);

    /// <inheritdoc cref="Delay(TimeSpan, PlayerLoopTiming, CancellationToken)" />
    public static GDTask Delay(TimeSpan delayTimeSpan, CancellationToken cancellationToken) => Delay(delayTimeSpan, PlayerLoopTiming.Process, cancellationToken);

    /// <summary>
    /// Delay the execution after <paramref name="delayTimeSpan" /> on provided <see cref="IPlayerLoop" /> with
    /// <see cref="DelayType.DeltaTime" /> provider, with specified <see cref="CancellationToken" />.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="delayTimeSpan" /> is less than 0.</exception>
    public static GDTask Delay(TimeSpan delayTimeSpan, IPlayerLoop delayLoop, CancellationToken cancellationToken = default) => Delay(delayTimeSpan, DelayType.DeltaTime, delayLoop, cancellationToken);

    /// <summary>
    /// Delay the execution after <paramref name="millisecondsDelay" /> on provided <see cref="PlayerLoopTiming" /> with
    /// <see cref="DelayType.DeltaTime" /> provider, with specified <see cref="CancellationToken" />.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="millisecondsDelay" /> is less than 0.</exception>
    public static GDTask Delay(int millisecondsDelay, DelayType delayType, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process, CancellationToken cancellationToken = default)
    {
        var delayTimeSpan = TimeSpan.FromMilliseconds(millisecondsDelay);
        return Delay(delayTimeSpan, delayType, delayTiming, cancellationToken);
    }

    /// <inheritdoc cref="Delay(int, DelayType, PlayerLoopTiming, CancellationToken)" />
    public static GDTask Delay(int millisecondsDelay, DelayType delayType, CancellationToken cancellationToken) => Delay(millisecondsDelay, delayType, PlayerLoopTiming.Process, cancellationToken);

    /// <summary>
    /// Delay the execution after <paramref name="millisecondsDelay" /> on provided <see cref="IPlayerLoop" /> with
    /// <see cref="DelayType.DeltaTime" /> provider, with specified <see cref="CancellationToken" />.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="millisecondsDelay" /> is less than 0.</exception>
    public static GDTask Delay(int millisecondsDelay, DelayType delayType, IPlayerLoop delayLoop, CancellationToken cancellationToken = default)
    {
        var delayTimeSpan = TimeSpan.FromMilliseconds(millisecondsDelay);
        return Delay(delayTimeSpan, delayType, delayLoop, cancellationToken);
    }

    /// <summary>
    /// Delay the execution after <paramref name="delayTimeSpan" /> on provided <see cref="PlayerLoopTiming" /> with
    /// <see cref="DelayType.DeltaTime" /> provider, with specified <see cref="CancellationToken" />.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="delayTimeSpan" /> is less than 0.</exception>
    public static GDTask Delay(TimeSpan delayTimeSpan, DelayType delayType, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process, CancellationToken cancellationToken = default) => Delay(delayTimeSpan, delayType, GDTaskScheduler.GetPlayerLoop(delayTiming), cancellationToken);

    /// <inheritdoc cref="Delay(TimeSpan, DelayType, PlayerLoopTiming, CancellationToken)" />
    public static GDTask Delay(TimeSpan delayTimeSpan, DelayType delayType, CancellationToken cancellationToken) => Delay(delayTimeSpan, delayType, PlayerLoopTiming.Process, cancellationToken);

    /// <summary>
    /// Delay the execution after <paramref name="delayTimeSpan" /> on provided <see cref="IPlayerLoop" /> with
    /// <see cref="DelayType.DeltaTime" /> provider, with specified <see cref="CancellationToken" />.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="delayTimeSpan" /> is less than 0.</exception>
    public static GDTask Delay(TimeSpan delayTimeSpan, DelayType delayType, IPlayerLoop delayLoop, CancellationToken cancellationToken = default)
    {
        if (delayTimeSpan < TimeSpan.Zero) throw new ArgumentOutOfRangeException("Delay does not allow minus delayTimeSpan. delayTimeSpan:" + delayTimeSpan);

        Error.ThrowArgumentNullException(delayLoop, nameof(delayLoop));

        // Force use Realtime in editor.
        if (GDTaskScheduler.IsMainThread && Engine.IsEditorHint()) delayType = DelayType.Realtime;

        switch (delayType)
        {
            case DelayType.Realtime: { return new(DelayRealtimePromise.Create(delayTimeSpan, delayLoop, cancellationToken, out var token), token); }
            case DelayType.DeltaTime:
            default:
            {
                return new(DelayPromise.Create(delayTimeSpan, delayLoop, cancellationToken, out var token), token);
            }
        }
    }

    private sealed class YieldPromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<YieldPromise>
    {
        private static TaskPool<YieldPromise> Pool;

        private CancellationToken _cancellationToken;
        private GDTaskCompletionSourceCore<object> _core;
        private YieldPromise _nextNode;

        private YieldPromise() { }

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
            if (_cancellationToken.IsCancellationRequested)
            {
                _core.TrySetCanceled(_cancellationToken);
                return false;
            }

            _core.TrySetResult(null);
            return false;
        }

        public ref YieldPromise NextNode => ref _nextNode;

        public static IGDTaskSource Create(IPlayerLoop playerLoop, CancellationToken cancellationToken, out short token)
        {
            if (cancellationToken.IsCancellationRequested) return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);

            if (!Pool.TryPop(out var result)) result = new();

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

    private sealed class NextFramePromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<NextFramePromise>
    {
        private static TaskPool<NextFramePromise> Pool;
        private CancellationToken _cancellationToken;
        private GDTaskCompletionSourceCore<AsyncUnit> _core;
        private ulong _frameCount;

        private bool _isMainThread;
        private NextFramePromise _nextNode;
        private bool _usesEngineFrameBoundary;

        private NextFramePromise() { }

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
            if (_cancellationToken.IsCancellationRequested)
            {
                _core.TrySetCanceled(_cancellationToken);
                return false;
            }

            if (_isMainThread && _frameCount == Engine.GetProcessFrames()) return true;

            _core.TrySetResult(AsyncUnit.Default);
            return false;
        }

        public ref NextFramePromise NextNode => ref _nextNode;

        public static IGDTaskSource Create(IPlayerLoop playerLoop, CancellationToken cancellationToken, out short token)
        {
            if (cancellationToken.IsCancellationRequested) return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);

            if (!Pool.TryPop(out var result)) result = new();

            result._usesEngineFrameBoundary = GDTaskScheduler.UsesEngineFrameBoundary(playerLoop);
            result._isMainThread = result._usesEngineFrameBoundary && GDTaskScheduler.IsMainThread;
            if (result._isMainThread)
                result._frameCount = Engine.GetProcessFrames();
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
            _usesEngineFrameBoundary = default;
            _cancellationToken = default;
            return Pool.TryPush(this);
        }
    }

    private sealed class DelayFramePromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<DelayFramePromise>
    {
        private static TaskPool<DelayFramePromise> Pool;
        private CancellationToken _cancellationToken;
        private GDTaskCompletionSourceCore<AsyncUnit> _core;

        private int _currentFrameCount;
        private int _delayFrameCount;
        private ulong _initialFrame;

        private bool _isMainThread;
        private DelayFramePromise _nextNode;
        private bool _usesEngineFrameBoundary;

        private DelayFramePromise() { }

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
            if (_cancellationToken.IsCancellationRequested)
            {
                _core.TrySetCanceled(_cancellationToken);
                return false;
            }

            if (_currentFrameCount == 0)
            {
                if (_delayFrameCount == 0) // same as Yield
                {
                    _core.TrySetResult(AsyncUnit.Default);
                    return false;
                }

                // Skip in initial frame.
                if (_isMainThread && _initialFrame == Engine.GetProcessFrames()) return true;
            }

            if (++_currentFrameCount >= _delayFrameCount)
            {
                _core.TrySetResult(AsyncUnit.Default);
                return false;
            }

            return true;
        }

        public ref DelayFramePromise NextNode => ref _nextNode;

        public static IGDTaskSource Create(int delayFrameCount, IPlayerLoop playerLoop, CancellationToken cancellationToken, out short token)
        {
            if (cancellationToken.IsCancellationRequested) return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);

            if (!Pool.TryPop(out var result)) result = new();

            result._delayFrameCount = delayFrameCount;
            result._cancellationToken = cancellationToken;
            result._usesEngineFrameBoundary = GDTaskScheduler.UsesEngineFrameBoundary(playerLoop);
            result._isMainThread = result._usesEngineFrameBoundary && GDTaskScheduler.IsMainThread;
            if (result._isMainThread)
                result._initialFrame = Engine.GetProcessFrames();

            TaskTracker.TrackActiveTask(result, 3);

            GDTaskScheduler.AddAction(playerLoop, result);

            token = result._core.Version;
            return result;
        }

        private bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            _core.Reset();
            _currentFrameCount = default;
            _delayFrameCount = default;
            _usesEngineFrameBoundary = default;
            _cancellationToken = default;
            return Pool.TryPush(this);
        }
    }

    private sealed class DelayPromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<DelayPromise>
    {
        private static TaskPool<DelayPromise> Pool;
        private CancellationToken _cancellationToken;
        private GDTaskCompletionSourceCore<object> _core;
        private double _delayTimeSpan;
        private double _elapsed;
        private ulong _initialFrame;

        private bool _isMainThread;
        private DelayPromise _nextNode;
        private bool _usesEngineFrameBoundary;

        private DelayPromise() { }

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
            if (_cancellationToken.IsCancellationRequested)
            {
                _core.TrySetCanceled(_cancellationToken);
                return false;
            }

            if (_elapsed == 0.0f)
                // Built-in loops skip completion on the same engine frame they were scheduled on.
                // Custom loops may be ticked manually, so their first tick must still count elapsed time.
                if (_isMainThread && _initialFrame == Engine.GetProcessFrames())
                    return true;

            _elapsed += deltaTime;

            if (_elapsed >= _delayTimeSpan)
            {
                _core.TrySetResult(null);
                return false;
            }

            return true;
        }

        public ref DelayPromise NextNode => ref _nextNode;

        public static IGDTaskSource Create(TimeSpan delayTimeSpan, IPlayerLoop playerLoop, CancellationToken cancellationToken, out short token)
        {
            if (cancellationToken.IsCancellationRequested) return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);

            if (!Pool.TryPop(out var result)) result = new();

            result._elapsed = 0.0f;
            result._delayTimeSpan = (float)delayTimeSpan.TotalSeconds;
            result._cancellationToken = cancellationToken;
            result._usesEngineFrameBoundary = GDTaskScheduler.UsesEngineFrameBoundary(playerLoop);
            result._isMainThread = result._usesEngineFrameBoundary && GDTaskScheduler.IsMainThread;
            if (result._isMainThread)
                result._initialFrame = Engine.GetProcessFrames();

            TaskTracker.TrackActiveTask(result, 3);

            GDTaskScheduler.AddAction(playerLoop, result);

            token = result._core.Version;
            return result;
        }

        private bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            _core.Reset();
            _delayTimeSpan = default;
            _elapsed = default;
            _usesEngineFrameBoundary = default;
            _cancellationToken = default;
            return Pool.TryPush(this);
        }
    }

    private sealed class DelayRealtimePromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<DelayRealtimePromise>
    {
        private static TaskPool<DelayRealtimePromise> Pool;
        private CancellationToken _cancellationToken;

        private GDTaskCompletionSourceCore<AsyncUnit> _core;

        private long _delayTimeSpanTicks;
        private DelayRealtimePromise _nextNode;
        private ValueStopwatch _stopwatch;

        private DelayRealtimePromise() { }

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
            if (_cancellationToken.IsCancellationRequested)
            {
                _core.TrySetCanceled(_cancellationToken);
                return false;
            }

            if (_stopwatch.IsInvalid)
            {
                _core.TrySetResult(AsyncUnit.Default);
                return false;
            }

            if (_stopwatch.ElapsedTicks >= _delayTimeSpanTicks)
            {
                _core.TrySetResult(AsyncUnit.Default);
                return false;
            }

            return true;
        }

        public ref DelayRealtimePromise NextNode => ref _nextNode;

        public static IGDTaskSource Create(TimeSpan delayTimeSpan, IPlayerLoop playerLoop, CancellationToken cancellationToken, out short token)
        {
            if (cancellationToken.IsCancellationRequested) return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);

            if (!Pool.TryPop(out var result)) result = new();

            result._stopwatch = ValueStopwatch.StartNew();
            result._delayTimeSpanTicks = delayTimeSpan.Ticks;
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
            _stopwatch = default;
            _cancellationToken = default;
            return Pool.TryPush(this);
        }
    }
}

/// <summary>
/// An awaitable that when awaited, asynchronously yields back to the next specified <see cref="IPlayerLoop" />.
/// </summary>
public readonly struct YieldAwaitable
{
    internal readonly IPlayerLoop PlayerLoop;

    /// <summary>
    /// Initializes the <see cref="YieldAwaitable" />.
    /// </summary>
    internal YieldAwaitable(IPlayerLoop playerLoop)
    {
        PlayerLoop = playerLoop;
    }

    /// <summary>
    /// Gets an awaiter used to await this <see cref="YieldAwaitable" />.
    /// </summary>
    public Awaiter GetAwaiter() => new(PlayerLoop);

    /// <summary>
    /// Creates a <see cref="GDTask" /> that represents this <see cref="YieldAwaitable" />.
    /// </summary>
    public GDTask ToGDTask() => GDTask.Yield(PlayerLoop, CancellationToken.None);

    /// <summary>
    /// Provides an awaiter for awaiting a <see cref="YieldAwaitable" />.
    /// </summary>
    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        private readonly IPlayerLoop _playerLoop;

        /// <summary>
        /// Initializes the <see cref="Awaiter" />.
        /// </summary>
        internal Awaiter(IPlayerLoop playerLoop)
        {
            _playerLoop = playerLoop;
        }

        /// <summary>
        /// Gets whether this <see cref="YieldAwaitable">Task</see> has completed, always returns false.
        /// </summary>
        public bool IsCompleted => false;

        /// <summary>
        /// Ends the awaiting on the completed <see cref="YieldAwaitable" />.
        /// </summary>
        public void GetResult() { }

        /// <summary>
        /// Schedules the continuation onto the <see cref="YieldAwaitable" /> associated with this <see cref="Awaiter" />.
        /// </summary>
        public void OnCompleted(Action continuation) => GDTaskScheduler.AddContinuation(_playerLoop, continuation);

        /// <summary>
        /// Schedules the continuation onto the <see cref="YieldAwaitable" /> associated with this <see cref="Awaiter" />.
        /// </summary>
        public void UnsafeOnCompleted(Action continuation) => GDTaskScheduler.AddContinuation(_playerLoop, continuation);
    }
}
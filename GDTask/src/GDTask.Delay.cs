using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Godot;
using GodotTask.Internal;

namespace GodotTask
{
    /// <summary>
    /// Indicates the time provider used for Delaying
    /// </summary>
    public enum DelayType
    {
        /// <summary>Use scaled delta time provided from <see cref="Node._Process"/></summary>
        DeltaTime,
        /// <summary>Use time provided from <see cref="System.Diagnostics.Stopwatch.GetTimestamp()"/></summary>
        Realtime
    }

    public partial struct GDTask
    {
        /// <summary>
        /// Delay the execution until the next <see cref="PlayerLoopTiming.Process"/>.
        /// </summary>
        public static YieldAwaitable Yield()
        {
            // optimized for single continuation
            return new YieldAwaitable(GDTaskScheduler.GetPlayerLoop(PlayerLoopTiming.Process));
        }

        /// <summary>
        /// Delay the execution until the next provided <see cref="PlayerLoopTiming"/>.
        /// </summary>
        public static YieldAwaitable Yield(PlayerLoopTiming timing)
        {
            // optimized for single continuation
            return new YieldAwaitable(GDTaskScheduler.GetPlayerLoop(timing));
        }

        /// <summary>
        /// Delay the execution until the next provided <see cref="IPlayerLoop"/>.
        /// </summary>
        public static YieldAwaitable Yield(IPlayerLoop playerLoop)
        {
            GodotTask.Internal.Error.ThrowArgumentNullException(playerLoop, nameof(playerLoop));
            return new YieldAwaitable(playerLoop);
        }

        /// <summary>
        /// Delay the execution until the next <see cref="PlayerLoopTiming.Process"/>, with specified <see cref="CancellationToken"/>.
        /// </summary>
        public static GDTask Yield(CancellationToken cancellationToken)
        {
            return new GDTask(YieldPromise.Create(GDTaskScheduler.GetPlayerLoop(PlayerLoopTiming.Process), cancellationToken, out var token), token);
        }

        /// <summary>
        /// Delay the execution until the next provided <see cref="PlayerLoopTiming"/>, with specified <see cref="CancellationToken"/>.
        /// </summary>   
        public static GDTask Yield(PlayerLoopTiming timing, CancellationToken cancellationToken)
        {
            return new GDTask(YieldPromise.Create(GDTaskScheduler.GetPlayerLoop(timing), cancellationToken, out var token), token);
        }

        /// <summary>
        /// Delay the execution until the next provided <see cref="IPlayerLoop"/>, with specified <see cref="CancellationToken"/>.
        /// </summary>
        public static GDTask Yield(IPlayerLoop playerLoop, CancellationToken cancellationToken)
        {
            GodotTask.Internal.Error.ThrowArgumentNullException(playerLoop, nameof(playerLoop));
            return new GDTask(YieldPromise.Create(playerLoop, cancellationToken, out var token), token);
        }

        /// <summary>
        /// Delay the execution until the next frame of <see cref="PlayerLoopTiming.Process"/>.
        /// </summary>
        public static GDTask NextFrame()
        {
            return new GDTask(NextFramePromise.Create(GDTaskScheduler.GetPlayerLoop(PlayerLoopTiming.Process), CancellationToken.None, out var token), token);
        }

        /// <summary>
        /// Delay the execution until the next frame of the provided <see cref="PlayerLoopTiming"/>.
        /// </summary>
        public static GDTask NextFrame(PlayerLoopTiming timing)
        {
            return new GDTask(NextFramePromise.Create(GDTaskScheduler.GetPlayerLoop(timing), CancellationToken.None, out var token), token);
        }

        /// <summary>
        /// Delay the execution until the next frame of the provided <see cref="IPlayerLoop"/>.
        /// </summary>
        public static GDTask NextFrame(IPlayerLoop playerLoop)
        {
            GodotTask.Internal.Error.ThrowArgumentNullException(playerLoop, nameof(playerLoop));
            return new GDTask(NextFramePromise.Create(playerLoop, CancellationToken.None, out var token), token);
        }

        /// <summary>
        /// Delay the execution until the next frame of <see cref="PlayerLoopTiming.Process"/>, with specified <see cref="CancellationToken"/>.
        /// </summary>
        public static GDTask NextFrame(CancellationToken cancellationToken)
        {
            return new GDTask(NextFramePromise.Create(GDTaskScheduler.GetPlayerLoop(PlayerLoopTiming.Process), cancellationToken, out var token), token);
        }

        /// <summary>
        /// Delay the execution until the next frame of the provided <see cref="PlayerLoopTiming"/>, with specified <see cref="CancellationToken"/>.
        /// </summary>
        public static GDTask NextFrame(PlayerLoopTiming timing, CancellationToken cancellationToken)
        {
            return new GDTask(NextFramePromise.Create(GDTaskScheduler.GetPlayerLoop(timing), cancellationToken, out var token), token);
        }

        /// <summary>
        /// Delay the execution until the next frame of the provided <see cref="IPlayerLoop"/>, with specified <see cref="CancellationToken"/>.
        /// </summary>
        public static GDTask NextFrame(IPlayerLoop playerLoop, CancellationToken cancellationToken)
        {
            GodotTask.Internal.Error.ThrowArgumentNullException(playerLoop, nameof(playerLoop));
            return new GDTask(NextFramePromise.Create(playerLoop, cancellationToken, out var token), token);
        }

        /// <inheritdoc cref="Yield()"/>
        public static YieldAwaitable WaitForEndOfFrame()
        {
            return Yield(PlayerLoopTiming.Process);
        }

        /// <inheritdoc cref="Yield(CancellationToken)"/>
        public static GDTask WaitForEndOfFrame(CancellationToken cancellationToken)
        {
            return Yield(PlayerLoopTiming.Process, cancellationToken);
        }

        /// <summary>
        /// Delay the execution until the next <see cref="PlayerLoopTiming.PhysicsProcess"/>.
        /// </summary>
        public static YieldAwaitable WaitForPhysicsProcess()
        {
            return Yield(PlayerLoopTiming.PhysicsProcess);
        }

        /// <summary>
        /// Delay the execution until the next <see cref="PlayerLoopTiming.PhysicsProcess"/>, with specified <see cref="CancellationToken"/>.
        /// </summary>
        public static GDTask WaitForPhysicsProcess(CancellationToken cancellationToken)
        {
            return Yield(PlayerLoopTiming.PhysicsProcess, cancellationToken);
        }

        /// <summary>
        /// Delay the execution after frame(s) of the provided <see cref="PlayerLoopTiming"/>, with specified <see cref="CancellationToken"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="delayFrameCount"/> is less than 0.</exception>
        public static GDTask DelayFrame(int delayFrameCount, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process, CancellationToken cancellationToken = default)
        {
            if (delayFrameCount < 0)
            {
                throw new ArgumentOutOfRangeException("Delay does not allow minus delayFrameCount. delayFrameCount:" + delayFrameCount);
            }

            return new GDTask(DelayFramePromise.Create(delayFrameCount, GDTaskScheduler.GetPlayerLoop(delayTiming), cancellationToken, out var token), token);
        }

        /// <summary>
        /// Delay the execution after frame(s) of the provided <see cref="IPlayerLoop"/>, with specified <see cref="CancellationToken"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="delayFrameCount"/> is less than 0.</exception>
        public static GDTask DelayFrame(int delayFrameCount, IPlayerLoop delayLoop, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (delayFrameCount < 0)
            {
                throw new ArgumentOutOfRangeException("Delay does not allow minus delayFrameCount. delayFrameCount:" + delayFrameCount);
            }

            GodotTask.Internal.Error.ThrowArgumentNullException(delayLoop, nameof(delayLoop));
            return new GDTask(DelayFramePromise.Create(delayFrameCount, delayLoop, cancellationToken, out var token), token);
        }

        /// <summary>
        /// Delay the execution after <paramref name="millisecondsDelay"/> on provided <see cref="PlayerLoopTiming"/> with <see cref="DelayType.DeltaTime"/> provider, with specified <see cref="CancellationToken"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="millisecondsDelay"/> is less than 0.</exception>
        public static GDTask Delay(int millisecondsDelay, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process, CancellationToken cancellationToken = default)
        {
            var delayTimeSpan = TimeSpan.FromMilliseconds(millisecondsDelay);
            return Delay(delayTimeSpan, delayTiming, cancellationToken);
        }

        /// <summary>
        /// Delay the execution after <paramref name="millisecondsDelay"/> on provided <see cref="IPlayerLoop"/> with <see cref="DelayType.DeltaTime"/> provider, with specified <see cref="CancellationToken"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="millisecondsDelay"/> is less than 0.</exception>
        public static GDTask Delay(int millisecondsDelay, IPlayerLoop delayLoop, CancellationToken cancellationToken = default(CancellationToken))
        {
            var delayTimeSpan = TimeSpan.FromMilliseconds(millisecondsDelay);
            return Delay(delayTimeSpan, delayLoop, cancellationToken);
        }

        /// <summary>
        /// Delay the execution after <paramref name="delayTimeSpan"/> on provided <see cref="PlayerLoopTiming"/> with <see cref="DelayType.DeltaTime"/> provider, with specified <see cref="CancellationToken"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="delayTimeSpan"/> is less than 0.</exception>
        public static GDTask Delay(TimeSpan delayTimeSpan, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process, CancellationToken cancellationToken = default)
        {
            return Delay(delayTimeSpan, DelayType.DeltaTime, delayTiming, cancellationToken);
        }

        /// <summary>
        /// Delay the execution after <paramref name="delayTimeSpan"/> on provided <see cref="IPlayerLoop"/> with <see cref="DelayType.DeltaTime"/> provider, with specified <see cref="CancellationToken"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="delayTimeSpan"/> is less than 0.</exception>
        public static GDTask Delay(TimeSpan delayTimeSpan, IPlayerLoop delayLoop, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Delay(delayTimeSpan, DelayType.DeltaTime, delayLoop, cancellationToken);
        }

        /// <summary>
        /// Delay the execution after <paramref name="millisecondsDelay"/> on provided <see cref="PlayerLoopTiming"/> with <see cref="DelayType.DeltaTime"/> provider, with specified <see cref="CancellationToken"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="millisecondsDelay"/> is less than 0.</exception>
        public static GDTask Delay(int millisecondsDelay, DelayType delayType, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process, CancellationToken cancellationToken = default)
        {
            var delayTimeSpan = TimeSpan.FromMilliseconds(millisecondsDelay);
            return Delay(delayTimeSpan, delayType, delayTiming, cancellationToken);
        }

        /// <summary>
        /// Delay the execution after <paramref name="millisecondsDelay"/> on provided <see cref="IPlayerLoop"/> with <see cref="DelayType.DeltaTime"/> provider, with specified <see cref="CancellationToken"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="millisecondsDelay"/> is less than 0.</exception>
        public static GDTask Delay(int millisecondsDelay, DelayType delayType, IPlayerLoop delayLoop, CancellationToken cancellationToken = default(CancellationToken))
        {
            var delayTimeSpan = TimeSpan.FromMilliseconds(millisecondsDelay);
            return Delay(delayTimeSpan, delayType, delayLoop, cancellationToken);
        }

        /// <summary>
        /// Delay the execution after <paramref name="delayTimeSpan"/> on provided <see cref="PlayerLoopTiming"/> with <see cref="DelayType.DeltaTime"/> provider, with specified <see cref="CancellationToken"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="delayTimeSpan"/> is less than 0.</exception>
        public static GDTask Delay(TimeSpan delayTimeSpan, DelayType delayType, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process, CancellationToken cancellationToken = default)
        {
            return Delay(delayTimeSpan, delayType, GDTaskScheduler.GetPlayerLoop(delayTiming), cancellationToken);
        }

        /// <summary>
        /// Delay the execution after <paramref name="delayTimeSpan"/> on provided <see cref="IPlayerLoop"/> with <see cref="DelayType.DeltaTime"/> provider, with specified <see cref="CancellationToken"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="delayTimeSpan"/> is less than 0.</exception>
        public static GDTask Delay(TimeSpan delayTimeSpan, DelayType delayType, IPlayerLoop delayLoop, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (delayTimeSpan < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException("Delay does not allow minus delayTimeSpan. delayTimeSpan:" + delayTimeSpan);
            }

            GodotTask.Internal.Error.ThrowArgumentNullException(delayLoop, nameof(delayLoop));

            // Force use Realtime in editor.
            if (GDTaskScheduler.IsMainThread && Engine.IsEditorHint())
            {
                delayType = DelayType.Realtime;
            }

            switch (delayType)
            {
                case DelayType.Realtime:
                    {
                        return new GDTask(DelayRealtimePromise.Create(delayTimeSpan, delayLoop, cancellationToken, out var token), token);
                    }
                case DelayType.DeltaTime:
                default:
                    {
                        return new GDTask(DelayPromise.Create(delayTimeSpan, delayLoop, cancellationToken, out var token), token);
                    }
            }
        }

        private sealed class YieldPromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<YieldPromise>
        {
            private static TaskPool<YieldPromise> pool;
            private YieldPromise nextNode;
            public ref YieldPromise NextNode => ref nextNode;

            static YieldPromise()
            {
                TaskPool.RegisterSizeGetter(typeof(YieldPromise), () => pool.Size);
            }

            private CancellationToken cancellationToken;
            private GDTaskCompletionSourceCore<object> core;

            private YieldPromise()
            {
            }

            public static IGDTaskSource Create(IPlayerLoop playerLoop, CancellationToken cancellationToken, out short token)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!pool.TryPop(out var result))
                {
                    result = new YieldPromise();
                }

                result.cancellationToken = cancellationToken;

                TaskTracker.TrackActiveTask(result, 3);

                GDTaskScheduler.AddAction(playerLoop, result);

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

            public bool MoveNext(double deltaTime)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    core.TrySetCanceled(cancellationToken);
                    return false;
                }

                core.TrySetResult(null);
                return false;
            }

            private bool TryReturn()
            {
                TaskTracker.RemoveTracking(this);
                core.Reset();
                cancellationToken = default;
                return pool.TryPush(this);
            }
        }

        private sealed class NextFramePromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<NextFramePromise>
        {
            private static TaskPool<NextFramePromise> pool;
            private NextFramePromise nextNode;
            public ref NextFramePromise NextNode => ref nextNode;

            static NextFramePromise()
            {
                TaskPool.RegisterSizeGetter(typeof(NextFramePromise), () => pool.Size);
            }

            private bool isMainThread;
            private bool usesEngineFrameBoundary;
            private ulong frameCount;
            private CancellationToken cancellationToken;
            private GDTaskCompletionSourceCore<AsyncUnit> core;

            private NextFramePromise()
            {
            }

            public static IGDTaskSource Create(IPlayerLoop playerLoop, CancellationToken cancellationToken, out short token)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!pool.TryPop(out var result))
                {
                    result = new NextFramePromise();
                }

                result.usesEngineFrameBoundary = GDTaskScheduler.UsesEngineFrameBoundary(playerLoop);
                result.isMainThread = result.usesEngineFrameBoundary && GDTaskScheduler.IsMainThread;
                if (result.isMainThread)
                    result.frameCount = Engine.GetProcessFrames();
                result.cancellationToken = cancellationToken;

                TaskTracker.TrackActiveTask(result, 3);

                GDTaskScheduler.AddAction(playerLoop, result);

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

            public bool MoveNext(double deltaTime)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    core.TrySetCanceled(cancellationToken);
                    return false;
                }

                if (isMainThread && frameCount == Engine.GetProcessFrames())
                {
                    return true;
                }

                core.TrySetResult(AsyncUnit.Default);
                return false;
            }

            private bool TryReturn()
            {
                TaskTracker.RemoveTracking(this);
                core.Reset();
                usesEngineFrameBoundary = default;
                cancellationToken = default;
                return pool.TryPush(this);
            }
        }

        private sealed class DelayFramePromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<DelayFramePromise>
        {
            private static TaskPool<DelayFramePromise> pool;
            private DelayFramePromise nextNode;
            public ref DelayFramePromise NextNode => ref nextNode;

            static DelayFramePromise()
            {
                TaskPool.RegisterSizeGetter(typeof(DelayFramePromise), () => pool.Size);
            }

            private bool isMainThread;
            private bool usesEngineFrameBoundary;
            private ulong initialFrame;
            private int delayFrameCount;
            private CancellationToken cancellationToken;

            private int currentFrameCount;
            private GDTaskCompletionSourceCore<AsyncUnit> core;

            private DelayFramePromise()
            {
            }

            public static IGDTaskSource Create(int delayFrameCount, IPlayerLoop playerLoop, CancellationToken cancellationToken, out short token)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!pool.TryPop(out var result))
                {
                    result = new DelayFramePromise();
                }

                result.delayFrameCount = delayFrameCount;
                result.cancellationToken = cancellationToken;
                result.usesEngineFrameBoundary = GDTaskScheduler.UsesEngineFrameBoundary(playerLoop);
                result.isMainThread = result.usesEngineFrameBoundary && GDTaskScheduler.IsMainThread;
                if (result.isMainThread)
                    result.initialFrame = Engine.GetProcessFrames();

                TaskTracker.TrackActiveTask(result, 3);

                GDTaskScheduler.AddAction(playerLoop, result);

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

            public bool MoveNext(double deltaTime)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    core.TrySetCanceled(cancellationToken);
                    return false;
                }

                if (currentFrameCount == 0)
                {
                    if (delayFrameCount == 0) // same as Yield
                    {
                        core.TrySetResult(AsyncUnit.Default);
                        return false;
                    }

                    // Skip in initial frame.
                    if (isMainThread && initialFrame == Engine.GetProcessFrames())
                    {
                        return true;
                    }
                }

                if (++currentFrameCount >= delayFrameCount)
                {
                    core.TrySetResult(AsyncUnit.Default);
                    return false;
                }

                return true;
            }

            private bool TryReturn()
            {
                TaskTracker.RemoveTracking(this);
                core.Reset();
                currentFrameCount = default;
                delayFrameCount = default;
                usesEngineFrameBoundary = default;
                cancellationToken = default;
                return pool.TryPush(this);
            }
        }

        private sealed class DelayPromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<DelayPromise>
        {
            private static TaskPool<DelayPromise> pool;
            private DelayPromise nextNode;
            public ref DelayPromise NextNode => ref nextNode;

            static DelayPromise()
            {
                TaskPool.RegisterSizeGetter(typeof(DelayPromise), () => pool.Size);
            }

            private bool isMainThread;
            private bool usesEngineFrameBoundary;
            private ulong initialFrame;
            private double delayTimeSpan;
            private double elapsed;
            private CancellationToken cancellationToken;
            private GDTaskCompletionSourceCore<object> core;

            private DelayPromise()
            {
            }

            public static IGDTaskSource Create(TimeSpan delayTimeSpan, IPlayerLoop playerLoop, CancellationToken cancellationToken, out short token)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!pool.TryPop(out var result))
                {
                    result = new DelayPromise();
                }

                result.elapsed = 0.0f;
                result.delayTimeSpan = (float)delayTimeSpan.TotalSeconds;
                result.cancellationToken = cancellationToken;
                result.usesEngineFrameBoundary = GDTaskScheduler.UsesEngineFrameBoundary(playerLoop);
                result.isMainThread = result.usesEngineFrameBoundary && GDTaskScheduler.IsMainThread;
                if (result.isMainThread)
                    result.initialFrame = Engine.GetProcessFrames();

                TaskTracker.TrackActiveTask(result, 3);

                GDTaskScheduler.AddAction(playerLoop, result);

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

            public bool MoveNext(double deltaTime)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    core.TrySetCanceled(cancellationToken);
                    return false;
                }

                if (elapsed == 0.0f)
                {
                    // Built-in loops skip completion on the same engine frame they were scheduled on.
                    // Custom loops may be ticked manually, so their first tick must still count elapsed time.
                    if (isMainThread && initialFrame == Engine.GetProcessFrames())
                    {
                        return true;
                    }
                }

                elapsed += deltaTime;
                
                if (elapsed >= delayTimeSpan)
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
                delayTimeSpan = default;
                elapsed = default;
                usesEngineFrameBoundary = default;
                cancellationToken = default;
                return pool.TryPush(this);
            }
        }

        private sealed class DelayRealtimePromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<DelayRealtimePromise>
        {
            private static TaskPool<DelayRealtimePromise> pool;
            private DelayRealtimePromise nextNode;
            public ref DelayRealtimePromise NextNode => ref nextNode;

            static DelayRealtimePromise()
            {
                TaskPool.RegisterSizeGetter(typeof(DelayRealtimePromise), () => pool.Size);
            }

            private long delayTimeSpanTicks;
            private ValueStopwatch stopwatch;
            private CancellationToken cancellationToken;

            private GDTaskCompletionSourceCore<AsyncUnit> core;

            private DelayRealtimePromise()
            {
            }

            public static IGDTaskSource Create(TimeSpan delayTimeSpan, IPlayerLoop playerLoop, CancellationToken cancellationToken, out short token)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!pool.TryPop(out var result))
                {
                    result = new DelayRealtimePromise();
                }

                result.stopwatch = ValueStopwatch.StartNew();
                result.delayTimeSpanTicks = delayTimeSpan.Ticks;
                result.cancellationToken = cancellationToken;

                TaskTracker.TrackActiveTask(result, 3);

                GDTaskScheduler.AddAction(playerLoop, result);

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

            public bool MoveNext(double deltaTime)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    core.TrySetCanceled(cancellationToken);
                    return false;
                }

                if (stopwatch.IsInvalid)
                {
                    core.TrySetResult(AsyncUnit.Default);
                    return false;
                }

                if (stopwatch.ElapsedTicks >= delayTimeSpanTicks)
                {
                    core.TrySetResult(AsyncUnit.Default);
                    return false;
                }

                return true;
            }

            private bool TryReturn()
            {
                TaskTracker.RemoveTracking(this);
                core.Reset();
                stopwatch = default;
                cancellationToken = default;
                return pool.TryPush(this);
            }
        }
    }

    /// <summary>
    /// An awaitable that when awaited, asynchronously yields back to the next specified <see cref="IPlayerLoop"/>.
    /// </summary>
    public readonly struct YieldAwaitable
    {
        internal readonly IPlayerLoop playerLoop;

        /// <summary>
        /// Initializes the <see cref="YieldAwaitable"/>.
        /// </summary>
        internal YieldAwaitable(IPlayerLoop playerLoop)
        {
            this.playerLoop = playerLoop;
        }

        /// <summary>
        /// Gets an awaiter used to await this <see cref="YieldAwaitable"/>.
        /// </summary>
        public Awaiter GetAwaiter()
        {
            return new Awaiter(playerLoop);
        }

        /// <summary>
        /// Creates a <see cref="GDTask"/> that represents this <see cref="YieldAwaitable"/>.
        /// </summary>
        public GDTask ToGDTask()
        {
            return GDTask.Yield(playerLoop, CancellationToken.None);
        }

        /// <summary>
        /// Provides an awaiter for awaiting a <see cref="YieldAwaitable"/>.
        /// </summary>
        public readonly struct Awaiter : ICriticalNotifyCompletion
        {
            private readonly IPlayerLoop playerLoop;

            /// <summary>
            /// Initializes the <see cref="Awaiter"/>.
            /// </summary>
            internal Awaiter(IPlayerLoop playerLoop)
            {
                this.playerLoop = playerLoop;
            }

            /// <summary>
            /// Gets whether this <see cref="YieldAwaitable">Task</see> has completed, always returns false.
            /// </summary>
            public bool IsCompleted => false;

            /// <summary>
            /// Ends the awaiting on the completed <see cref="YieldAwaitable"/>.
            /// </summary>
            public void GetResult() {
            }

            /// <summary>
            /// Schedules the continuation onto the <see cref="YieldAwaitable"/> associated with this <see cref="Awaiter"/>.
            /// </summary>
            public void OnCompleted(Action continuation)
            {
                GDTaskScheduler.AddContinuation(playerLoop, continuation);
            }

            /// <summary>
            /// Schedules the continuation onto the <see cref="YieldAwaitable"/> associated with this <see cref="Awaiter"/>.
            /// </summary>
            public void UnsafeOnCompleted(Action continuation)
            {
                GDTaskScheduler.AddContinuation(playerLoop, continuation);
            }
        }
    }
}

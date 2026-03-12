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
            return new YieldAwaitable(CreateTarget(PlayerLoopTiming.Process));
        }

        /// <summary>
        /// Delay the execution until the next provided <see cref="PlayerLoopTiming"/>.
        /// </summary>
        public static YieldAwaitable Yield(PlayerLoopTiming timing)
        {
            // optimized for single continuation
            return new YieldAwaitable(CreateTarget(timing));
        }

        /// <summary>
        /// Delay the execution until the next <see cref="PlayerLoopTiming.Process"/> of the provided custom player loop.
        /// </summary>
        public static YieldAwaitable Yield(ICustomPlayerLoop customPlayerLoop)
        {
            return new YieldAwaitable(CreateTarget(customPlayerLoop, PlayerLoopTiming.Process));
        }

        /// <summary>
        /// Delay the execution until the next provided <see cref="PlayerLoopTiming"/> of the provided custom player loop.
        /// </summary>
        public static YieldAwaitable Yield(ICustomPlayerLoop customPlayerLoop, PlayerLoopTiming timing)
        {
            return new YieldAwaitable(CreateTarget(customPlayerLoop, timing));
        }

        /// <summary>
        /// Delay the execution until the next <see cref="PlayerLoopTiming.Process"/>, with specified <see cref="CancellationToken"/>.
        /// </summary>
        public static GDTask Yield(CancellationToken cancellationToken)
        {
            return new GDTask(YieldPromise.Create(CreateTarget(PlayerLoopTiming.Process), cancellationToken, out var token), token);
        }

        /// <summary>
        /// Delay the execution until the next provided <see cref="PlayerLoopTiming"/>, with specified <see cref="CancellationToken"/>.
        /// </summary>   
        public static GDTask Yield(PlayerLoopTiming timing, CancellationToken cancellationToken)
        {
            return new GDTask(YieldPromise.Create(CreateTarget(timing), cancellationToken, out var token), token);
        }

        /// <summary>
        /// Delay the execution until the next <see cref="PlayerLoopTiming.Process"/> of the provided custom player loop, with specified <see cref="CancellationToken"/>.
        /// </summary>
        public static GDTask Yield(ICustomPlayerLoop customPlayerLoop, CancellationToken cancellationToken)
        {
            return new GDTask(YieldPromise.Create(CreateTarget(customPlayerLoop, PlayerLoopTiming.Process), cancellationToken, out var token), token);
        }

        /// <summary>
        /// Delay the execution until the next provided <see cref="PlayerLoopTiming"/> of the provided custom player loop, with specified <see cref="CancellationToken"/>.
        /// </summary>
        public static GDTask Yield(ICustomPlayerLoop customPlayerLoop, PlayerLoopTiming timing, CancellationToken cancellationToken)
        {
            return new GDTask(YieldPromise.Create(CreateTarget(customPlayerLoop, timing), cancellationToken, out var token), token);
        }

        /// <summary>
        /// Delay the execution until the next frame of <see cref="PlayerLoopTiming.Process"/>.
        /// </summary>
        public static GDTask NextFrame()
        {
            return new GDTask(NextFramePromise.Create(CreateTarget(PlayerLoopTiming.Process), CancellationToken.None, out var token), token);
        }

        /// <summary>
        /// Delay the execution until the next frame of the provided <see cref="PlayerLoopTiming"/>.
        /// </summary>
        public static GDTask NextFrame(PlayerLoopTiming timing)
        {
            return new GDTask(NextFramePromise.Create(CreateTarget(timing), CancellationToken.None, out var token), token);
        }

        /// <summary>
        /// Delay the execution until the next frame of <see cref="PlayerLoopTiming.Process"/> on the provided custom player loop.
        /// </summary>
        public static GDTask NextFrame(ICustomPlayerLoop customPlayerLoop)
        {
            return new GDTask(NextFramePromise.Create(CreateTarget(customPlayerLoop, PlayerLoopTiming.Process), CancellationToken.None, out var token), token);
        }

        /// <summary>
        /// Delay the execution until the next frame of the provided <see cref="PlayerLoopTiming"/> on the provided custom player loop.
        /// </summary>
        public static GDTask NextFrame(ICustomPlayerLoop customPlayerLoop, PlayerLoopTiming timing)
        {
            return new GDTask(NextFramePromise.Create(CreateTarget(customPlayerLoop, timing), CancellationToken.None, out var token), token);
        }

        /// <summary>
        /// Delay the execution until the next frame of <see cref="PlayerLoopTiming.Process"/>, with specified <see cref="CancellationToken"/>.
        /// </summary>
        public static GDTask NextFrame(CancellationToken cancellationToken)
        {
            return new GDTask(NextFramePromise.Create(CreateTarget(PlayerLoopTiming.Process), cancellationToken, out var token), token);
        }

        /// <summary>
        /// Delay the execution until the next frame of the provided <see cref="PlayerLoopTiming"/>, with specified <see cref="CancellationToken"/>.
        /// </summary>
        public static GDTask NextFrame(PlayerLoopTiming timing, CancellationToken cancellationToken)
        {
            return new GDTask(NextFramePromise.Create(CreateTarget(timing), cancellationToken, out var token), token);
        }

        /// <summary>
        /// Delay the execution until the next frame of <see cref="PlayerLoopTiming.Process"/> on the provided custom player loop, with specified <see cref="CancellationToken"/>.
        /// </summary>
        public static GDTask NextFrame(ICustomPlayerLoop customPlayerLoop, CancellationToken cancellationToken)
        {
            return new GDTask(NextFramePromise.Create(CreateTarget(customPlayerLoop, PlayerLoopTiming.Process), cancellationToken, out var token), token);
        }

        /// <summary>
        /// Delay the execution until the next frame of the provided <see cref="PlayerLoopTiming"/> on the provided custom player loop, with specified <see cref="CancellationToken"/>.
        /// </summary>
        public static GDTask NextFrame(ICustomPlayerLoop customPlayerLoop, PlayerLoopTiming timing, CancellationToken cancellationToken)
        {
            return new GDTask(NextFramePromise.Create(CreateTarget(customPlayerLoop, timing), cancellationToken, out var token), token);
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
        public static GDTask DelayFrame(int delayFrameCount, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (delayFrameCount < 0)
            {
                throw new ArgumentOutOfRangeException("Delay does not allow minus delayFrameCount. delayFrameCount:" + delayFrameCount);
            }

            return new GDTask(DelayFramePromise.Create(delayFrameCount, CreateTarget(delayTiming), cancellationToken, out var token), token);
        }

        /// <summary>
        /// Delay the execution after frame(s) of the provided custom player loop, with specified <see cref="CancellationToken"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="delayFrameCount"/> is less than 0.</exception>
        public static GDTask DelayFrame(int delayFrameCount, ICustomPlayerLoop customPlayerLoop, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process, CancellationToken cancellationToken = default)
        {
            if (delayFrameCount < 0)
            {
                throw new ArgumentOutOfRangeException("Delay does not allow minus delayFrameCount. delayFrameCount:" + delayFrameCount);
            }

            return new GDTask(DelayFramePromise.Create(delayFrameCount, CreateTarget(customPlayerLoop, delayTiming), cancellationToken, out var token), token);
        }

        /// <summary>
        /// Delay the execution after <paramref name="millisecondsDelay"/> on provided <see cref="PlayerLoopTiming"/> with <see cref="DelayType.DeltaTime"/> provider, with specified <see cref="CancellationToken"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="millisecondsDelay"/> is less than 0.</exception>
        public static GDTask Delay(int millisecondsDelay, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process, CancellationToken cancellationToken = default(CancellationToken))
        {
            var delayTimeSpan = TimeSpan.FromMilliseconds(millisecondsDelay);
            return Delay(delayTimeSpan, delayTiming, cancellationToken);
        }

        /// <summary>
        /// Delay the execution after <paramref name="millisecondsDelay"/> on the provided custom player loop with <see cref="DelayType.DeltaTime"/> provider, with specified <see cref="CancellationToken"/>.
        /// </summary>
        public static GDTask Delay(int millisecondsDelay, ICustomPlayerLoop customPlayerLoop, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process, CancellationToken cancellationToken = default)
        {
            var delayTimeSpan = TimeSpan.FromMilliseconds(millisecondsDelay);
            return Delay(delayTimeSpan, customPlayerLoop, delayTiming, cancellationToken);
        }

        /// <summary>
        /// Delay the execution after <paramref name="delayTimeSpan"/> on provided <see cref="PlayerLoopTiming"/> with <see cref="DelayType.DeltaTime"/> provider, with specified <see cref="CancellationToken"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="delayTimeSpan"/> is less than 0.</exception>
        public static GDTask Delay(TimeSpan delayTimeSpan, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Delay(delayTimeSpan, DelayType.DeltaTime, CreateTarget(delayTiming), cancellationToken);
        }

        /// <summary>
        /// Delay the execution after <paramref name="delayTimeSpan"/> on the provided custom player loop with <see cref="DelayType.DeltaTime"/> provider, with specified <see cref="CancellationToken"/>.
        /// </summary>
        public static GDTask Delay(TimeSpan delayTimeSpan, ICustomPlayerLoop customPlayerLoop, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process, CancellationToken cancellationToken = default)
        {
            return Delay(delayTimeSpan, DelayType.DeltaTime, CreateTarget(customPlayerLoop, delayTiming), cancellationToken);
        }

        /// <summary>
        /// Delay the execution after <paramref name="millisecondsDelay"/> on provided <see cref="PlayerLoopTiming"/> with <see cref="DelayType.DeltaTime"/> provider, with specified <see cref="CancellationToken"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="millisecondsDelay"/> is less than 0.</exception>
        public static GDTask Delay(int millisecondsDelay, DelayType delayType, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process, CancellationToken cancellationToken = default(CancellationToken))
        {
            var delayTimeSpan = TimeSpan.FromMilliseconds(millisecondsDelay);
            return Delay(delayTimeSpan, delayType, delayTiming, cancellationToken);
        }

        /// <summary>
        /// Delay the execution after <paramref name="millisecondsDelay"/> on the provided custom player loop, with specified <see cref="CancellationToken"/>.
        /// </summary>
        public static GDTask Delay(int millisecondsDelay, DelayType delayType, ICustomPlayerLoop customPlayerLoop, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process, CancellationToken cancellationToken = default)
        {
            var delayTimeSpan = TimeSpan.FromMilliseconds(millisecondsDelay);
            return Delay(delayTimeSpan, delayType, customPlayerLoop, delayTiming, cancellationToken);
        }

        /// <summary>
        /// Delay the execution after <paramref name="delayTimeSpan"/> on provided <see cref="PlayerLoopTiming"/> with <see cref="DelayType.DeltaTime"/> provider, with specified <see cref="CancellationToken"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="delayTimeSpan"/> is less than 0.</exception>
        public static GDTask Delay(TimeSpan delayTimeSpan, DelayType delayType, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Delay(delayTimeSpan, delayType, CreateTarget(delayTiming), cancellationToken);
        }

        /// <summary>
        /// Delay the execution after <paramref name="delayTimeSpan"/> on the provided custom player loop, with specified <see cref="CancellationToken"/>.
        /// </summary>
        public static GDTask Delay(TimeSpan delayTimeSpan, DelayType delayType, ICustomPlayerLoop customPlayerLoop, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process, CancellationToken cancellationToken = default)
        {
            return Delay(delayTimeSpan, delayType, CreateTarget(customPlayerLoop, delayTiming), cancellationToken);
        }

        private static GDTask Delay(TimeSpan delayTimeSpan, DelayType delayType, PlayerLoopRunnerTarget target, CancellationToken cancellationToken)
        {
            if (delayTimeSpan < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException("Delay does not allow minus delayTimeSpan. delayTimeSpan:" + delayTimeSpan);
            }

            // Force use Realtime in editor.
            if (ReferenceEquals(target.Scheduler, GDTaskPlayerLoopRunner.DefaultScheduler) && target.IsMainThread && Engine.IsEditorHint())
            {
                delayType = DelayType.Realtime;
            }

            switch (delayType)
            {
                case DelayType.Realtime:
                    {
                        return new GDTask(DelayRealtimePromise.Create(delayTimeSpan, target, cancellationToken, out var token), token);
                    }
                case DelayType.DeltaTime:
                default:
                    {
                        return new GDTask(DelayPromise.Create(delayTimeSpan, target, cancellationToken, out var token), token);
                    }
            }
        }

            internal static GDTask CreateYieldTask(PlayerLoopRunnerTarget target, CancellationToken cancellationToken)
            {
                return new GDTask(YieldPromise.Create(target, cancellationToken, out var token), token);
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

            public static IGDTaskSource Create(PlayerLoopRunnerTarget target, CancellationToken cancellationToken, out short token)
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

                target.AddAction(result);

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
            private ulong frameCount;
            private PlayerLoopRunnerTarget target;
            private CancellationToken cancellationToken;
            private GDTaskCompletionSourceCore<AsyncUnit> core;

            private NextFramePromise()
            {
            }

            public static IGDTaskSource Create(PlayerLoopRunnerTarget target, CancellationToken cancellationToken, out short token)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!pool.TryPop(out var result))
                {
                    result = new NextFramePromise();
                }

                result.target = target;
                result.isMainThread = target.IsMainThread;
                if (result.isMainThread)
                    result.frameCount = target.FrameCount;
                result.cancellationToken = cancellationToken;

                TaskTracker.TrackActiveTask(result, 3);

                target.AddAction(result);

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
                if (cancellationToken.IsCancellationRequested)
                {
                    core.TrySetCanceled(cancellationToken);
                    return false;
                }

                if (isMainThread && frameCount == target.FrameCount)
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
                target = default;
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
            private ulong initialFrame;
            private PlayerLoopRunnerTarget target;
            private int delayFrameCount;
            private CancellationToken cancellationToken;

            private int currentFrameCount;
            private GDTaskCompletionSourceCore<AsyncUnit> core;

            private DelayFramePromise()
            {
            }

            public static IGDTaskSource Create(int delayFrameCount, PlayerLoopRunnerTarget target, CancellationToken cancellationToken, out short token)
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
                result.target = target;
                result.isMainThread = target.IsMainThread;
                if (result.isMainThread)
                    result.initialFrame = target.FrameCount;

                TaskTracker.TrackActiveTask(result, 3);

                target.AddAction(result);

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
                    if (isMainThread && initialFrame == target.FrameCount)
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
                target = default;
                currentFrameCount = default;
                delayFrameCount = default;
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
            private ulong initialFrame;
            private double delayTimeSpan;
            private double elapsed;
            private CancellationToken cancellationToken;
            private PlayerLoopRunnerTarget target;
            private GDTaskCompletionSourceCore<object> core;

            private DelayPromise()
            {
            }

            public static IGDTaskSource Create(TimeSpan delayTimeSpan, PlayerLoopRunnerTarget target, CancellationToken cancellationToken, out short token)
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
                result.target = target;
                result.isMainThread = target.IsMainThread;
                if (result.isMainThread)
                    result.initialFrame = target.FrameCount;

                TaskTracker.TrackActiveTask(result, 3);

                target.AddAction(result);

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
                if (cancellationToken.IsCancellationRequested)
                {
                    core.TrySetCanceled(cancellationToken);
                    return false;
                }

                if (elapsed == 0.0f)
                {
                    if (isMainThread && initialFrame == target.FrameCount)
                    {
                        return true;
                    }
                }

                elapsed += target.DeltaTime;
                
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
                target = default;
                delayTimeSpan = default;
                elapsed = default;
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

            public static IGDTaskSource Create(TimeSpan delayTimeSpan, PlayerLoopRunnerTarget target, CancellationToken cancellationToken, out short token)
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

                target.AddAction(result);

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
    /// An awaitable that when awaited, asynchronously yields back to the next specified <see cref="PlayerLoopTiming"/>.
    /// </summary>
    public readonly struct YieldAwaitable
    {
        internal readonly PlayerLoopRunnerTarget target;

        /// <summary>
        /// Initializes the <see cref="YieldAwaitable"/>.
        /// </summary>
        internal YieldAwaitable(PlayerLoopRunnerTarget target)
        {
            this.target = target;
        }

        /// <summary>
        /// Gets an awaiter used to await this <see cref="YieldAwaitable"/>.
        /// </summary>
        public Awaiter GetAwaiter()
        {
            return new Awaiter(target);
        }

        /// <summary>
        /// Creates a <see cref="GDTask"/> that represents this <see cref="YieldAwaitable"/>.
        /// </summary>
        public GDTask ToGDTask()
        {
            return GDTask.CreateYieldTask(target, CancellationToken.None);
        }

        /// <summary>
        /// Provides an awaiter for awaiting a <see cref="YieldAwaitable"/>.
        /// </summary>
        public readonly struct Awaiter : ICriticalNotifyCompletion
        {
            private readonly PlayerLoopRunnerTarget target;

            /// <summary>
            /// Initializes the <see cref="Awaiter"/>.
            /// </summary>
            internal Awaiter(PlayerLoopRunnerTarget target)
            {
                this.target = target;
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
                target.AddContinuation(continuation);
            }

            /// <summary>
            /// Schedules the continuation onto the <see cref="YieldAwaitable"/> associated with this <see cref="Awaiter"/>.
            /// </summary>
            public void UnsafeOnCompleted(Action continuation)
            {
                target.AddContinuation(continuation);
            }
        }
    }
}

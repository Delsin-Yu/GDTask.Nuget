using Godot;
using GodotTask.Internal;
using System;
using System.Threading;

namespace GodotTask
{
    internal abstract class PlayerLoopTimer : IDisposable, IPlayerLoopItem
    {
        private readonly CancellationToken cancellationToken;
        private readonly Action<object> timerCallback;
        private readonly object state;
        private readonly IPlayerLoop playerLoop;
        protected readonly bool UsesEngineFrameBoundary;
        private readonly bool periodic;

        private bool isRunning;
        private bool tryStop;
        private bool isDisposed;

        protected PlayerLoopTimer(bool periodic, IPlayerLoop playerLoop, CancellationToken cancellationToken, Action<object> timerCallback, object state)
        {
            this.periodic = periodic;
            this.playerLoop = playerLoop;
            UsesEngineFrameBoundary = GDTaskScheduler.UsesEngineFrameBoundary(playerLoop);
            this.cancellationToken = cancellationToken;
            this.timerCallback = timerCallback;
            this.state = state;
        }

        public static PlayerLoopTimer Create(TimeSpan interval, bool periodic, DelayType delayType, PlayerLoopTiming playerLoopTiming, CancellationToken cancellationToken, Action<object> timerCallback, object state)
        {
            return Create(interval, periodic, delayType, GDTaskScheduler.GetPlayerLoop(playerLoopTiming), cancellationToken, timerCallback, state);
        }

        public static PlayerLoopTimer Create(TimeSpan interval, bool periodic, DelayType delayType, IPlayerLoop playerLoop, CancellationToken cancellationToken, Action<object> timerCallback, object state)
        {
            // Force use Realtime.
            if (GDTaskScheduler.IsMainThread && Engine.IsEditorHint())
            {
                delayType = DelayType.Realtime;
            }

            switch (delayType)
            {
                case DelayType.Realtime:
                    return new RealtimePlayerLoopTimer(interval, periodic, playerLoop, cancellationToken, timerCallback, state);
                case DelayType.DeltaTime:
                default:
                    return new DeltaTimePlayerLoopTimer(interval, periodic, playerLoop, cancellationToken, timerCallback, state);
            }
        }

        public static PlayerLoopTimer StartNew(TimeSpan interval, bool periodic, DelayType delayType, PlayerLoopTiming playerLoopTiming, CancellationToken cancellationToken, Action<object> timerCallback, object state)
        {
            return StartNew(interval, periodic, delayType, GDTaskScheduler.GetPlayerLoop(playerLoopTiming), cancellationToken, timerCallback, state);
        }

        public static PlayerLoopTimer StartNew(TimeSpan interval, bool periodic, DelayType delayType, IPlayerLoop playerLoop, CancellationToken cancellationToken, Action<object> timerCallback, object state)
        {
            var timer = Create(interval, periodic, delayType, playerLoop, cancellationToken, timerCallback, state);
            timer.Restart();
            return timer;
        }

        /// <summary>
        /// Restart(Reset and Start) timer.
        /// </summary>
        public void Restart()
        {
            if (isDisposed) throw new ObjectDisposedException(null);

            ResetCore(null); // init state
            if (!isRunning)
            {
                isRunning = true;
                GDTaskScheduler.AddAction(playerLoop, this);
            }
            tryStop = false;
        }

        /// <summary>
        /// Restart(Reset and Start) and change interval.
        /// </summary>
        public void Restart(TimeSpan interval)
        {
            if (isDisposed) throw new ObjectDisposedException(null);

            ResetCore(interval); // init state
            if (!isRunning)
            {
                isRunning = true;
                GDTaskScheduler.AddAction(playerLoop, this);
            }
            tryStop = false;
        }

        /// <summary>
        /// Stop timer.
        /// </summary>
        public void Stop()
        {
            tryStop = true;
        }

        protected abstract void ResetCore(TimeSpan? newInterval);

        public void Dispose()
        {
            isDisposed = true;
        }

        bool IPlayerLoopItem.MoveNext(double deltaTime)
        {
            if (isDisposed)
            {
                isRunning = false;
                return false;
            }
            if (tryStop)
            {
                isRunning = false;
                return false;
            }
            if (cancellationToken.IsCancellationRequested)
            {
                isRunning = false;
                return false;
            }

            if (!MoveNextCore(deltaTime))
            {
                timerCallback(state);

                if (periodic)
                {
                    ResetCore(null);
                    return true;
                }
                else
                {
                    isRunning = false;
                    return false;
                }
            }

            return true;
        }

        protected abstract bool MoveNextCore(double deltaTime);
    }

    internal sealed class DeltaTimePlayerLoopTimer : PlayerLoopTimer
    {
        private bool isMainThread;
        private ulong initialFrame;
        private double elapsed;
        private double interval;

        public DeltaTimePlayerLoopTimer(TimeSpan interval, bool periodic, IPlayerLoop playerLoop, CancellationToken cancellationToken, Action<object> timerCallback, object state)
            : base(periodic, playerLoop, cancellationToken, timerCallback, state)
        {
            ResetCore(interval);
        }

        protected override bool MoveNextCore(double deltaTime)
        {
            if (elapsed == 0.0)
            {
                // Match built-in player loop behavior by waiting for the next engine frame,
                // but do not suppress the first manual tick of a custom IPlayerLoop.
                if (isMainThread && initialFrame == Engine.GetProcessFrames())
                {
                    return true;
                }
            }

            elapsed += deltaTime;
            if (elapsed >= interval)
            {
                return false;
            }

            return true;
        }

        protected override void ResetCore(TimeSpan? interval)
        {
            elapsed = 0.0;
            isMainThread = UsesEngineFrameBoundary && GDTaskScheduler.IsMainThread;
            if (isMainThread)
                initialFrame = Engine.GetProcessFrames();
            if (interval != null)
            {
                this.interval = (float)interval.Value.TotalSeconds;
            }
        }
    }

    internal sealed class RealtimePlayerLoopTimer : PlayerLoopTimer
    {
        private ValueStopwatch stopwatch;
        private long intervalTicks;

        public RealtimePlayerLoopTimer(TimeSpan interval, bool periodic, IPlayerLoop playerLoop, CancellationToken cancellationToken, Action<object> timerCallback, object state)
            : base(periodic, playerLoop, cancellationToken, timerCallback, state)
        {
            ResetCore(interval);
        }

        protected override bool MoveNextCore(double deltaTime)
        {
            if (stopwatch.ElapsedTicks >= intervalTicks)
            {
                return false;
            }

            return true;
        }

        protected override void ResetCore(TimeSpan? interval)
        {
            stopwatch = ValueStopwatch.StartNew();
            if (interval != null)
            {
                intervalTicks = interval.Value.Ticks;
            }
        }
    }
}


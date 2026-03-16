using System.Threading;
using System;
using Godot;

namespace GodotTask
{

    /// <summary>
    /// Provides extensions methods for <see cref="CancellationTokenSource"/>.
    /// </summary>
    public static class CancellationTokenSourceExtensions
    {
        private static void CancelCancellationTokenSourceState(object state)
        {
            var cts = (CancellationTokenSource)state;
            cts.Cancel();
        }

        /// <inheritdoc cref="CancelAfterSlim(CancellationTokenSource, int, DelayType, PlayerLoopTiming)"/>
        public static IDisposable CancelAfterSlim(this CancellationTokenSource cts, int millisecondsDelay, DelayType delayType = DelayType.DeltaTime, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process)
        {
            return CancelAfterSlim(cts, TimeSpan.FromMilliseconds(millisecondsDelay), delayType, delayTiming);
        }

        /// <summary>
        /// Cancel this <see cref="CancellationTokenSource"/> after a given <paramref name="millisecondsDelay"/>.
        /// </summary>
        /// <returns>A <see cref="PlayerLoopTimer"/> that, when disposed, aborts the timing session</returns>
        public static IDisposable CancelAfterSlim(this CancellationTokenSource cts, int millisecondsDelay, DelayType delayType, IPlayerLoop delayLoop)
        {
            return CancelAfterSlim(cts, TimeSpan.FromMilliseconds(millisecondsDelay), delayType, delayLoop);
        }

        /// <summary>
        /// Cancel this <see cref="CancellationTokenSource"/> after a given <paramref name="delayTimeSpan"/>.
        /// </summary>
        /// <returns>A <see cref="PlayerLoopTimer"/> that, when disposed, aborts the timing session</returns>
        public static IDisposable CancelAfterSlim(this CancellationTokenSource cts, TimeSpan delayTimeSpan, DelayType delayType = DelayType.DeltaTime, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process)
        {
            return PlayerLoopTimer.StartNew(delayTimeSpan, false, delayType, delayTiming, cts.Token, CancelCancellationTokenSourceState, cts);
        }

        /// <summary>
        /// Cancel this <see cref="CancellationTokenSource"/> after a given <paramref name="delayTimeSpan"/>.
        /// </summary>
        /// <returns>A <see cref="PlayerLoopTimer"/> that, when disposed, aborts the timing session</returns>
        public static IDisposable CancelAfterSlim(this CancellationTokenSource cts, TimeSpan delayTimeSpan, DelayType delayType, IPlayerLoop delayLoop)
        {
            GodotTask.Internal.Error.ThrowArgumentNullException(delayLoop, nameof(delayLoop));
            return PlayerLoopTimer.StartNew(delayTimeSpan, false, delayType, delayLoop, cts.Token, CancelCancellationTokenSourceState, cts);
        }

    }
}

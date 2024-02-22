using System.Threading;
using GodotTask.Triggers;
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

        /// <inheritdoc cref="CancelAfterSlim(System.Threading.CancellationTokenSource,int,DelayType,PlayerLoopTiming)"/>
        public static IDisposable CancelAfterSlim(this CancellationTokenSource cts, int millisecondsDelay, DelayType delayType = DelayType.DeltaTime, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process)
        {
            return CancelAfterSlim(cts, TimeSpan.FromMilliseconds(millisecondsDelay), delayType, delayTiming);
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
        /// Associate this <see cref="CancellationTokenSource"/> to a <see cref="Node"/> for it to be canceled when the node is receiving <see cref="GodotObject.NotificationPredelete"/>  
        /// </summary>
        public static void RegisterRaiseCancelOnPredelete(this CancellationTokenSource cts, Node node)
        {
            var trigger = node.GetAsyncPredeleteTrigger();
            trigger.CancellationToken.RegisterWithoutCaptureExecutionContext(CancelCancellationTokenSourceState, cts);
        }
    }
}


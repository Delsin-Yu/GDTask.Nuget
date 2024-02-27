using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using GodotTask.Internal;

namespace GodotTask
{
    /// <summary>
    /// A conditional component that tracks and logs active tasks.
    /// </summary>
    public static partial class TaskTracker
    {
        /// <summary>
        /// Enable tracking for active tasks.
        /// </summary>
        public static bool EnableTracking { get => _enableTracking.Value; set => _enableTracking.Value = value; }

        /// <summary>
        /// Record StackTrace for tracked tasks.
        /// </summary>
        public static bool EnableStackTrace { get => _enableStackTrace.Value; set => _enableStackTrace.Value = value; }

        /// <summary>
        /// Shows the task tracker window if not already.
        /// </summary>
        /// <remarks>
        /// This also sets <see cref="EnableTracking"/> to true.
        /// </remarks>
        public static void ShowTrackerWindow()
        {
            EnableTracking = true;
            TaskTrackerWindow.Launch();
        }

        private static int trackingId = 0;
        internal static readonly ObservableProperty _enableTracking = new(false);
        internal static readonly ObservableProperty _enableStackTrace = new(true);

        private static readonly ConditionalWeakTable<IGDTaskSource, TrackingData> tracking = new();

        internal static void TrackActiveTask(IGDTaskSource task, int skipFrame)
        {
            if (!_enableTracking.Value) return;
            var stackTrace = _enableStackTrace.Value ? new StackTrace(skipFrame, true).ToString()[6..] : "";

            string typeName;
            if (_enableStackTrace.Value) typeName = TypePrinter.ConstructTypeName(task.GetType());
            else typeName = task.GetType().Name;
            var trackingData = new TrackingData(typeName, Interlocked.Increment(ref trackingId), DateTime.UtcNow, stackTrace, task.UnsafeGetStatus);
            tracking.AddOrUpdate(task, trackingData);
            TaskTrackerWindow.TryAddItem(trackingData);
        }

        internal static void RemoveTracking(IGDTaskSource task)
        {
            if (!_enableTracking.Value) return;
            if (!tracking.TryGetValue(task, out var trackingData)) return;
            tracking.Remove(task);
            TaskTrackerWindow.TryRemoveItem(trackingData);
        }

        internal static IEnumerable<TrackingData> GetAllExistingTrackingData()
        {
            foreach (var (_, trackingData) in tracking)
            {
                yield return trackingData;
            }
        }
    }
}


using System;
using Godot;

namespace GodotTask
{

    /// <summary>
    /// Class for handling exceptions in tasks
    /// </summary>
    public static class GDTaskExceptionHandler
    {
        /// <summary>
        /// Subscribe to this event to get notified when a <see cref="GDTask"/> throws an unobserved exception,
        /// </summary>
        /// <remarks>
        /// This event is invoked when a <see cref="GDTask"/> completes with an exception that is not observed (i.e., not awaited or handled).
        /// If no handlers are subscribed to this event, the exception details will be logged using <see cref="GD.PushError(string)"/>,
        /// otherwise, all subscribed handlers will be invoked with the exception as an argument.
        /// </remarks>
        public static event Action<Exception> UnobservedTaskException;

        /// <summary>
        /// Propagate <see cref="OperationCanceledException"/> to <see cref="UnobservedTaskException"/> when true. Default is false.
        /// </summary>
        public static bool PropagateOperationCanceledException { get; set; } = false;

        internal static void PublishUnobservedTaskException(Exception ex)
        {
            if (ex == null) return;
            
            if (!PropagateOperationCanceledException && ex is OperationCanceledException)
            {
                return;
            }

            if (UnobservedTaskException == null)
            {
                GD.PushError($"UnobservedTaskException: \n{ex}");
                return;
            }

            UnobservedTaskException.Invoke(ex);
        }
    }
}


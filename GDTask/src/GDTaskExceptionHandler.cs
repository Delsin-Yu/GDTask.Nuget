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
        /// Occurs when a faulted <see cref="GDTask"/>'s unobserved exception is about to trigger exception escalation policy.
        /// </summary>
        public static event Action<Exception> UnobservedTaskException;

        /// <summary>
        /// Propagate <see cref="OperationCanceledException"/> to <see cref="UnobservedTaskException"/> when true. Default is false.
        /// </summary>
        public static bool PropagateOperationCanceledException { get; set; } = false;

        internal static void PublishUnobservedTaskException(Exception ex)
        {
            if (ex != null)
            {
                if (!PropagateOperationCanceledException && ex is OperationCanceledException)
                {
                    return;
                }

                if (UnobservedTaskException != null)
                {
                    UnobservedTaskException.Invoke(ex);
                }
                else
                {
                    GD.PrintErr("UnobservedTaskException: " + ex.ToString());
                }
            }
        }
    }
}


using System;

namespace GodotTask
{
    /// <summary>
    /// Provides controllable process loop events that GDTask can schedule work against.
    /// </summary>
    public interface ICustomPlayerLoop
    {
        /// <summary>
        /// Raised when the custom process loop should advance.
        /// </summary>
        event Action<double> OnProcess;

        /// <summary>
        /// Raised when the custom physics process loop should advance.
        /// </summary>
        event Action<double> OnPhysicsProcess;
    }
}

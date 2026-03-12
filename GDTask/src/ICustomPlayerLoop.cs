using System;

namespace GodotTask;

/// <summary>
/// Represents an external loop that can drive GDTask scheduling.
/// </summary>
public interface ICustomPlayerLoop
{
    /// <summary>
    /// Raised for process-style updates.
    /// </summary>
    event Action<double> Process;

    /// <summary>
    /// Raised for physics-process-style updates.
    /// </summary>
    event Action<double> PhysicsProcess;
}
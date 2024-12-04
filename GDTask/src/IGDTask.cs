using System.Runtime.CompilerServices;

namespace GodotTask;

/// <summary>
/// Represents an instance of GDTask; this interface is intended for advanced polymorphic and reflection usage only; please use <see cref="GDTask"/> or <see cref="GDTask{T}"/> for all other uses.
/// </summary>
public interface IGDTask
{
    /// <summary>
    /// Gets an awaiter used to await this <see cref="IGDTask"/>.
    /// </summary>
    IGDTaskAwaiter GetAwaiter();
}

/// <summary>
/// Represents an awaiter for awaiting an <see cref="IGDTask"/>; this interface is intended for advanced polymorphic and reflection usage only; please await <see cref="GDTask"/> or <see cref="GDTask{T}"/> for all other uses.
/// </summary>
public interface IGDTaskAwaiter : ICriticalNotifyCompletion
{
    /// <summary>
    /// Gets whether the associated <see cref="IGDTask">Task</see> has completed.
    /// </summary>
    bool IsCompleted { get; }
    
    /// <summary>
    /// Ends the awaitinging on the completed <see cref="IGDTask"/>.
    /// </summary>
    object GetResult();
}
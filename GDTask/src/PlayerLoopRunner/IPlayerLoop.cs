using System;
using Godot;

namespace GodotTask;

/// <summary>
/// A loop with events that are invoked on process and predelete.
/// </summary>
public interface IPlayerLoop
{
    /// <summary>
    /// An event that should be invoked each frame.
    /// </summary>
    event Action<double> OnProcess;
    /// <summary>
    /// An event that should be invoked just before the loop is deleted.
    /// </summary>
    event Action OnPredelete;
}

/// <summary>
/// Indicates one of the functions from the player loop.
/// </summary>
public enum PlayerLoopTiming
{
    /// <summary>
    /// The <see cref="Node._Process"/> from the player loop.
    /// </summary>
    Process = 0,
         
    /// <summary>
    /// The <see cref="Node._PhysicsProcess"/> from the player loop.
    /// </summary>
    PhysicsProcess = 1,
         
    /// <summary>
    /// The <see cref="Node._Process"/> from the player loop, but also runs when the scene tree has paused.
    /// </summary>
    IsolatedProcess = 2,
         
    /// <summary>
    /// The <see cref="Node._PhysicsProcess"/> from the player loop, but also runs when the scene tree has paused.
    /// </summary>
    IsolatedPhysicsProcess = 3,
         
    /// <summary>
    /// The <see cref="GodotObject.CallDeferred"/> invoked in the <see cref="Node._Process"/> from the player loop, which means it runs after all the <see cref="Node._Process"/> and <see cref="Node._PhysicsProcess"/>.
    /// </summary>
    DeferredProcess = 4,
}

internal interface IPlayerLoopItem
{
    bool MoveNext(double deltaTime);
}
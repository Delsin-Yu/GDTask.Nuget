using System;
using Godot;

namespace GodotTask.Internal;

internal static class RuntimeChecker
{
    internal static void ThrowIfEditor()
    {
        if(!Engine.IsEditorHint()) return;
        throw new InvalidOperationException("Calling any GDTask API under editor is not supported.");
    }
}
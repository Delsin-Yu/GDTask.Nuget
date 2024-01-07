using Godot;
using System;
using System.Runtime.CompilerServices;

namespace Fractural.Tasks.Internal
{
    internal static class RuntimeHelpersAbstraction
    {
        public static bool IsWellKnownNoReferenceContainsType<T>()
        {
            return RuntimeHelpers.IsReferenceOrContainsReferences<T>();
        }
    }
}


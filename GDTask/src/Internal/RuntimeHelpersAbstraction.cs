using System.Runtime.CompilerServices;

namespace GodotTask.Internal
{
    internal static class RuntimeHelpersAbstraction
    {
        public static bool IsWellKnownNoReferenceContainsType<T>()
        {
            return RuntimeHelpers.IsReferenceOrContainsReferences<T>();
        }
    }
}


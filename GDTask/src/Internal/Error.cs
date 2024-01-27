using System;
using System.Runtime.CompilerServices;

namespace GodotTasks.Tasks.Internal
{
    internal static class Error
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowArgumentNullException<T>(T value, string paramName)
          where T : class
        {
            if (value == null) ThrowArgumentNullExceptionCore(paramName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ThrowArgumentNullExceptionCore(string paramName)
        {
            throw new ArgumentNullException(paramName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowOperationCanceledException()
        {
            throw new OperationCanceledException();
        }
    }
}


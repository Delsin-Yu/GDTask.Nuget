using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Sources;

namespace GodotTask
{
    /// <summary>
    /// Represents the current status of a <see cref="GDTask"/>
    /// </summary>
    public enum GDTaskStatus
    {
        /// <summary>The operation has not yet completed.</summary>
        Pending = 0,
        /// <summary>The operation completed successfully.</summary>
        Succeeded = 1,
        /// <summary>The operation completed with an error.</summary>
        Faulted = 2,
        /// <summary>The operation completed due to cancellation.</summary>
        Canceled = 3
    }

    // General architecture:
    // Each GDTask holds a IGDTaskSource, which determines how the GDTask will run. This is basically a strategy pattern.
    // GDTask is a struct, so will be allocated on stack with no garbage collection. All IGDTaskSources will be pooled using
    // TaskPool<T>, so again, no garbage will be generated.
    //
    // Hence we achieve 0 memory allocation, making our tasks run really fast.

    /// <summary>
    /// GDTaskSource that has a void return (returns nothing).
    /// </summary>
    internal interface IGDTaskSource : IValueTaskSource
    {
        new GDTaskStatus GetStatus(short token);
        void OnCompleted(Action<object> continuation, object state, short token);
        new void GetResult(short token);

        GDTaskStatus UnsafeGetStatus(); // only for debug use.

        // ValueTask compatibility
        ValueTaskSourceStatus IValueTaskSource.GetStatus(short token) => (ValueTaskSourceStatus)((IGDTaskSource)this).GetStatus(token);
        void IValueTaskSource.GetResult(short token) => ((IGDTaskSource)this).GetResult(token);
        void IValueTaskSource.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) => ((IGDTaskSource)this).OnCompleted(continuation, state, token); // // ignore flags, always none.
    }

    /// <summary>
    /// GDTaskSource that has a typed return value
    /// </summary>
    /// <typeparam name="T">Return value of the task source</typeparam>
    internal interface IGDTaskSource<out T> : IGDTaskSource, IValueTaskSource<T>
    {
        // Hide the original void GetResult method
        new T GetResult(short token);
        
        // ValueTask compatibility
        new public GDTaskStatus GetStatus(short token) => ((IGDTaskSource)this).GetStatus(token);
        new public void OnCompleted(Action<object> continuation, object state, short token) => ((IGDTaskSource)this).OnCompleted(continuation, state, token);
        ValueTaskSourceStatus IValueTaskSource<T>.GetStatus(short token) => (ValueTaskSourceStatus)((IGDTaskSource)this).GetStatus(token);
        T IValueTaskSource<T>.GetResult(short token) => ((IGDTaskSource<T>)this).GetResult(token);
        void IValueTaskSource<T>.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) => ((IGDTaskSource)this).OnCompleted(continuation, state, token); // // ignore flags, always none.
    }

    /// <summary>
    /// Provides extensions methods for <see cref="GDTask"/>.
    /// </summary>
    /// <remarks>
    /// Extensions are all aggressive inlined so all calls are substituted with raw code for greatest performance.
    /// </remarks>
    public static class GDTaskStatusExtensions
    {
        /// <summary>status != Pending.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompleted(this GDTaskStatus status)
        {
            return status != GDTaskStatus.Pending;
        }

        /// <summary>status == Succeeded.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompletedSuccessfully(this GDTaskStatus status)
        {
            return status == GDTaskStatus.Succeeded;
        }

        /// <summary>status == Canceled.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCanceled(this GDTaskStatus status)
        {
            return status == GDTaskStatus.Canceled;
        }

        /// <summary>status == Faulted.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFaulted(this GDTaskStatus status)
        {
            return status == GDTaskStatus.Faulted;
        }
    }
}
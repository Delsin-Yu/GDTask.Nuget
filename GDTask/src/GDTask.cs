using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using GodotTask.CompilerServices;

namespace GodotTask
{
    internal static class AwaiterActions
    {
        internal static readonly Action<object> InvokeContinuationDelegate = Continuation;

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Continuation(object state)
        {
            ((Action)state).Invoke();
        }
    }

    /// <summary>
    /// Lightweight Godot specific task-like object with a void return value.
    /// </summary>
    [AsyncMethodBuilder(typeof(AsyncGDTaskMethodBuilder))]
    [StructLayout(LayoutKind.Auto)]
    public readonly partial struct GDTask
    {
        private readonly IGDTaskSource source;
        private readonly short token;

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal GDTask(IGDTaskSource source, short token)
        {
            this.source = source;
            this.token = token;
        }

        /// <summary>
        /// Gets the <see cref="GDTaskStatus"/> of this task.
        /// </summary>
        public GDTaskStatus Status
        {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (source == null) return GDTaskStatus.Succeeded;
                return source.GetStatus(token);
            }
        }

        /// <summary>
        /// Gets an awaiter used to await this <see cref="GDTask"/>.
        /// </summary>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Awaiter GetAwaiter()
        {
            return new Awaiter(this);
        }

        /// <summary>
        /// returns (bool IsCanceled) instead of throws OperationCanceledException.
        /// </summary>
        public GDTask<bool> SuppressCancellationThrow()
        {
            var status = Status;
            if (status == GDTaskStatus.Succeeded) return CompletedTasks.False;
            if (status == GDTaskStatus.Canceled) return CompletedTasks.True;
            return new GDTask<bool>(new IsCanceledSource(source), token);
        }
        
        /// <summary>
        /// Returns a string representation of the internal status for this <see cref="GDTask"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (source == null) return "()";
            return $"({source.UnsafeGetStatus()})";
        }

        /// <summary>
        /// Creates a <see cref="GDTask"/> allows to await multiple times.
        /// </summary>
        public GDTask Preserve()
        {
            if (source == null)
            {
                return this;
            }
            else
            {
                return new GDTask(new MemoizeSource(source), token);
            }
        }

        /// <summary>
        /// Creates a <see cref="GDTask{AsyncUnit}"/> that represents this <see cref="GDTask"/>.
        /// </summary>
        /// <returns></returns>
        public GDTask<AsyncUnit> AsAsyncUnitGDTask()
        {
            if (source == null) return CompletedTasks.AsyncUnit;

            var status = source.GetStatus(token);
            if (status.IsCompletedSuccessfully())
            {
                source.GetResult(token);
                return CompletedTasks.AsyncUnit;
            }
            else if (source is IGDTaskSource<AsyncUnit> asyncUnitSource)
            {
                return new GDTask<AsyncUnit>(asyncUnitSource, token);
            }

            return new GDTask<AsyncUnit>(new AsyncUnitSource(source), token);
        }

        private sealed class AsyncUnitSource : IGDTaskSource<AsyncUnit>
        {
            private readonly IGDTaskSource source;

            public AsyncUnitSource(IGDTaskSource source)
            {
                this.source = source;
            }

            public AsyncUnit GetResult(short token)
            {
                source.GetResult(token);
                return AsyncUnit.Default;
            }

            public GDTaskStatus GetStatus(short token)
            {
                return source.GetStatus(token);
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                source.OnCompleted(continuation, state, token);
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                return source.UnsafeGetStatus();
            }

            void IGDTaskSource.GetResult(short token)
            {
                GetResult(token);
            }
        }

        private sealed class IsCanceledSource : IGDTaskSource<bool>
        {
            private readonly IGDTaskSource source;

            public IsCanceledSource(IGDTaskSource source)
            {
                this.source = source;
            }

            public bool GetResult(short token)
            {
                if (source.GetStatus(token) == GDTaskStatus.Canceled)
                {
                    return true;
                }

                source.GetResult(token);
                return false;
            }

            void IGDTaskSource.GetResult(short token)
            {
                GetResult(token);
            }

            public GDTaskStatus GetStatus(short token)
            {
                return source.GetStatus(token);
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                return source.UnsafeGetStatus();
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                source.OnCompleted(continuation, state, token);
            }
        }

        private sealed class MemoizeSource : IGDTaskSource
        {
            private IGDTaskSource source;
            private ExceptionDispatchInfo exception;
            private GDTaskStatus status;

            public MemoizeSource(IGDTaskSource source)
            {
                this.source = source;
            }

            public void GetResult(short token)
            {
                if (source == null)
                {
                    if (exception != null)
                    {
                        exception.Throw();
                    }
                }
                else
                {
                    try
                    {
                        source.GetResult(token);
                        status = GDTaskStatus.Succeeded;
                    }
                    catch (Exception ex)
                    {
                        exception = ExceptionDispatchInfo.Capture(ex);
                        if (ex is OperationCanceledException)
                        {
                            status = GDTaskStatus.Canceled;
                        }
                        else
                        {
                            status = GDTaskStatus.Faulted;
                        }
                        throw;
                    }
                    finally
                    {
                        source = null;
                    }
                }
            }

            public GDTaskStatus GetStatus(short token)
            {
                if (source == null)
                {
                    return status;
                }

                return source.GetStatus(token);
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                if (source == null)
                {
                    continuation(state);
                }
                else
                {
                    source.OnCompleted(continuation, state, token);
                }
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                if (source == null)
                {
                    return status;
                }

                return source.UnsafeGetStatus();
            }
        }

        /// <summary>
        /// Provides an awaiter for awaiting a <see cref="GDTask"/>.
        /// </summary>
        public readonly struct Awaiter : ICriticalNotifyCompletion
        {
            private readonly GDTask task;

            /// <summary>
            /// Initializes the <see cref="Awaiter"/>.
            /// </summary>
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Awaiter(in GDTask task)
            {
                this.task = task;
            }

            /// <summary>
            /// Gets whether this <see cref="GDTask">Task</see> has completed.
            /// </summary>
            public bool IsCompleted
            {
                [DebuggerHidden]
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => task.Status.IsCompleted();
            }

            /// <summary>
            /// Ends the await on the completed <see cref="GDTask"/>.
            /// </summary>
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void GetResult()
            {
                if (task.source == null) return;
                task.source.GetResult(task.token);
            }

            /// <summary>
            /// Schedules the continuation onto the <see cref="GDTask"/> associated with this <see cref="Awaiter"/>.
            /// </summary>
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnCompleted(Action continuation)
            {
                if (task.source == null)
                {
                    continuation();
                }
                else
                {
                    task.source.OnCompleted(AwaiterActions.InvokeContinuationDelegate, continuation, task.token);
                }
            }

            /// <summary>
            /// Schedules the continuation onto the <see cref="GDTask"/> associated with this <see cref="Awaiter"/>.
            /// </summary>
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void UnsafeOnCompleted(Action continuation)
            {
                if (task.source == null)
                {
                    continuation();
                }
                else
                {
                    task.source.OnCompleted(AwaiterActions.InvokeContinuationDelegate, continuation, task.token);
                }
            }

            /// <summary>
            /// If register manually continuation, you can use it instead of for compiler OnCompleted methods.
            /// </summary>
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SourceOnCompleted(Action<object> continuation, object state)
            {
                if (task.source == null)
                {
                    continuation(state);
                }
                else
                {
                    task.source.OnCompleted(continuation, state, task.token);
                }
            }
        }
    }

    /// <summary>
    /// Lightweight Godot specified task-like object with a return value.
    /// </summary>
    /// <typeparam name="T">Return value of the task</typeparam>
    [AsyncMethodBuilder(typeof(AsyncGDTaskMethodBuilder<>))]
    [StructLayout(LayoutKind.Auto)]
    public readonly struct GDTask<T>
    {
        private readonly IGDTaskSource<T> source;
        private readonly T result;
        private readonly short token;

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal GDTask(T result)
        {
            source = default;
            token = default;
            this.result = result;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal GDTask(IGDTaskSource<T> source, short token)
        {
            this.source = source;
            this.token = token;
            result = default;
        }

        /// <summary>
        /// Gets the <see cref="GDTaskStatus"/> of this task.
        /// </summary>
        public GDTaskStatus Status
        {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (source == null) ? GDTaskStatus.Succeeded : source.GetStatus(token);
        }

        /// <summary>
        /// Gets an awaiter used to await this <see cref="GDTask{T}"/>.
        /// </summary>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Awaiter GetAwaiter()
        {
            return new Awaiter(this);
        }

        /// <summary>
        /// Creates a <see cref="GDTask{T}"/> allows to await multiple times.
        /// </summary>
        public GDTask<T> Preserve()
        {
            if (source == null)
            {
                return this;
            }
            else
            {
                return new GDTask<T>(new MemoizeSource(source), token);
            }
        }

        /// <summary>
        /// Creates a <see cref="GDTask"/> that represents this <see cref="GDTask{T}"/>.
        /// </summary>
        public GDTask AsGDTask()
        {
            if (source == null) return GDTask.CompletedTask;

            var status = source.GetStatus(token);
            if (status.IsCompletedSuccessfully())
            {
                source.GetResult(token);
                return GDTask.CompletedTask;
            }

            // Converting GDTask<T> -> GDTask is zero overhead.
            return new GDTask(source, token);
        }

        /// <summary>
        /// Implicit operator for covert from <see cref="GDTask{T}"/> to <see cref="GDTask"/>.
        /// </summary>
        public static implicit operator GDTask(GDTask<T> self)
        {
            return self.AsGDTask();
        }

        /// <summary>
        /// returns (bool IsCanceled, T Result) instead of throws OperationCanceledException.
        /// </summary>
        public GDTask<(bool IsCanceled, T Result)> SuppressCancellationThrow()
        {
            if (source == null)
            {
                return new GDTask<(bool IsCanceled, T Result)>((false, result));
            }

            return new GDTask<(bool, T)>(new IsCanceledSource(source), token);
        }

        /// <summary>
        /// Returns a string representation of the internal status for this <see cref="GDTask{T}"/>.
        /// </summary>
        public override string ToString()
        {
            return (source == null) ? result?.ToString()
                 : "(" + source.UnsafeGetStatus() + ")";
        }

        private sealed class IsCanceledSource : IGDTaskSource<(bool, T)>
        {
            private readonly IGDTaskSource<T> source;

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public IsCanceledSource(IGDTaskSource<T> source)
            {
                this.source = source;
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public (bool, T) GetResult(short token)
            {
                if (source.GetStatus(token) == GDTaskStatus.Canceled)
                {
                    return (true, default);
                }

                var result = source.GetResult(token);
                return (false, result);
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void IGDTaskSource.GetResult(short token)
            {
                GetResult(token);
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public GDTaskStatus GetStatus(short token)
            {
                return source.GetStatus(token);
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public GDTaskStatus UnsafeGetStatus()
            {
                return source.UnsafeGetStatus();
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                source.OnCompleted(continuation, state, token);
            }
        }

        private sealed class MemoizeSource : IGDTaskSource<T>
        {
            private IGDTaskSource<T> source;
            private T result;
            private ExceptionDispatchInfo exception;
            private GDTaskStatus status;

            public MemoizeSource(IGDTaskSource<T> source)
            {
                this.source = source;
            }

            public T GetResult(short token)
            {
                if (source == null)
                {
                    if (exception != null)
                    {
                        exception.Throw();
                    }
                    return result;
                }
                else
                {
                    try
                    {
                        result = source.GetResult(token);
                        status = GDTaskStatus.Succeeded;
                        return result;
                    }
                    catch (Exception ex)
                    {
                        exception = ExceptionDispatchInfo.Capture(ex);
                        if (ex is OperationCanceledException)
                        {
                            status = GDTaskStatus.Canceled;
                        }
                        else
                        {
                            status = GDTaskStatus.Faulted;
                        }
                        throw;
                    }
                    finally
                    {
                        source = null;
                    }
                }
            }

            void IGDTaskSource.GetResult(short token)
            {
                GetResult(token);
            }

            public GDTaskStatus GetStatus(short token)
            {
                if (source == null)
                {
                    return status;
                }

                return source.GetStatus(token);
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                if (source == null)
                {
                    continuation(state);
                }
                else
                {
                    source.OnCompleted(continuation, state, token);
                }
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                if (source == null)
                {
                    return status;
                }

                return source.UnsafeGetStatus();
            }
        }

        /// <summary>
        /// Provides an awaiter for awaiting a <see cref="GDTask{T}"/>.
        /// </summary>
        public readonly struct Awaiter : ICriticalNotifyCompletion
        {
            private readonly GDTask<T> task;

            /// <summary>
            /// Initializes the <see cref="Awaiter"/>.
            /// </summary>
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Awaiter(in GDTask<T> task)
            {
                this.task = task;
            }

            /// <summary>
            /// Gets whether this <see cref="GDTask{T}">Task</see> has completed.
            /// </summary>
            public bool IsCompleted
            {
                [DebuggerHidden]
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => task.Status.IsCompleted();
            }

            /// <summary>
            /// Ends the await on the completed <see cref="GDTask{T}"/>.
            /// </summary>
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T GetResult()
            {
                var s = task.source;
                if (s == null)
                {
                    return task.result;
                }
                else
                {
                    return s.GetResult(task.token);
                }
            }

            /// <summary>
            /// Schedules the continuation onto the <see cref="GDTask{T}"/> associated with this <see cref="Awaiter"/>.
            /// </summary>
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnCompleted(Action continuation)
            {
                var s = task.source;
                if (s == null)
                {
                    continuation();
                }
                else
                {
                    s.OnCompleted(AwaiterActions.InvokeContinuationDelegate, continuation, task.token);
                }
            }

            /// <summary>
            /// Schedules the continuation onto the <see cref="GDTask{T}"/> associated with this <see cref="Awaiter"/>.
            /// </summary>
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void UnsafeOnCompleted(Action continuation)
            {
                var s = task.source;
                if (s == null)
                {
                    continuation();
                }
                else
                {
                    s.OnCompleted(AwaiterActions.InvokeContinuationDelegate, continuation, task.token);
                }
            }

            /// <summary>
            /// If register manually continuation, you can use it instead of for compiler OnCompleted methods.
            /// </summary>
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SourceOnCompleted(Action<object> continuation, object state)
            {
                var s = task.source;
                if (s == null)
                {
                    continuation(state);
                }
                else
                {
                    s.OnCompleted(continuation, state, task.token);
                }
            }
        }
    }
}
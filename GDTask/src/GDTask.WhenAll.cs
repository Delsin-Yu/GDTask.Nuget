using System;
using System.Collections.Generic;
using System.Threading;
using GodotTask.Internal;

namespace GodotTask;

public partial struct GDTask
{
    private enum WhenAllCompletionKind : byte
    {
        Pending,
        Succeeded,
        Faulted,
        Canceled,
    }

    private readonly struct WhenAllCompletion(WhenAllCompletionKind kind, Exception exception, CancellationToken cancellationToken)
    {
        public WhenAllCompletionKind Kind { get; } = kind;

        public Exception Exception { get; } = exception;

        public CancellationToken CancellationToken { get; } = cancellationToken;
    }

    private sealed class WhenAllSharedState
    {
        private readonly object _gate = new();
        private readonly int _taskCount;
        private int _completedCount;
        private List<Exception> _exceptions;
        private CancellationToken _cancellationToken;
        private bool _hasCancellation;

        public WhenAllSharedState(int taskCount)
        {
            _taskCount = taskCount;
        }

        public WhenAllCompletion RecordCompletion(Exception exception = null)
        {
            lock (_gate)
            {
                if (exception != null)
                {
                    if (exception is OperationCanceledException canceledException)
                    {
                        if (!_hasCancellation)
                        {
                            _hasCancellation = true;
                            _cancellationToken = canceledException.CancellationToken;
                        }
                    }
                    else
                    {
                        (_exceptions ??= []).Add(exception);
                    }
                }

                _completedCount++;
                if (_completedCount != _taskCount)
                {
                    return new(WhenAllCompletionKind.Pending, null, default);
                }

                if (_exceptions != null)
                {
                    var finalException = _exceptions.Count == 1
                        ? _exceptions[0]
                        : new AggregateException(_exceptions);
                    return new(WhenAllCompletionKind.Faulted, finalException, default);
                }

                if (_hasCancellation)
                {
                    return new(WhenAllCompletionKind.Canceled, null, _cancellationToken);
                }

                return new(WhenAllCompletionKind.Succeeded, null, default);
            }
        }
    }

    private interface IWhenAllObservation
    {
        Exception Exception { get; }

        bool IsCanceled { get; }

        CancellationToken CancellationToken { get; }
    }

    private sealed class WhenAllObservation<T> : IWhenAllObservation
    {
        public T Result;
        public Exception Exception { get; private set; }
        public bool IsCanceled { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public void SetResult(T result)
        {
            Result = result;
        }

        public void SetCanceled(CancellationToken cancellationToken)
        {
            IsCanceled = true;
            CancellationToken = cancellationToken;
        }

        public void SetException(Exception exception)
        {
            Exception = exception;
        }
    }

    private static async GDTask ObserveWhenAll<T>(GDTask<T> task, WhenAllObservation<T> observation)
    {
        try
        {
            observation.SetResult(await task);
        }
        catch (OperationCanceledException ex)
        {
            observation.SetCanceled(ex.CancellationToken);
        }
        catch (Exception ex)
        {
            observation.SetException(ex);
        }
    }

    private static void CompleteObservedWhenAll(params IWhenAllObservation[] observations)
    {
        List<Exception> exceptions = null;
        var cancellationToken = default(CancellationToken);
        var hasCancellation = false;

        foreach (var observation in observations)
        {
            if (observation.Exception != null)
            {
                (exceptions ??= []).Add(observation.Exception);
                continue;
            }

            if (observation.IsCanceled && !hasCancellation)
            {
                hasCancellation = true;
                cancellationToken = observation.CancellationToken;
            }
        }

        if (exceptions != null)
        {
            if (exceptions.Count == 1)
            {
                throw exceptions[0];
            }

            throw new AggregateException(exceptions);
        }

        if (hasCancellation)
        {
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private static void TrySignalWhenAllCompletion<TResult>(in WhenAllCompletion completion, ref GDTaskCompletionSourceCore<TResult> core, TResult result)
    {
        switch (completion.Kind)
        {
            case WhenAllCompletionKind.Pending:
                return;
            case WhenAllCompletionKind.Succeeded:
                core.TrySetResult(result);
                return;
            case WhenAllCompletionKind.Faulted:
                core.TrySetException(completion.Exception);
                return;
            case WhenAllCompletionKind.Canceled:
                core.TrySetCanceled(completion.CancellationToken);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(completion));
        }
    }

    /// <summary>
    /// Creates a task that will complete when all of the supplied tasks have completed.
    /// </summary>
    /// <remarks>
    /// If any supplied task faults, the returned task faults after all inputs complete.
    /// If multiple supplied tasks fault, the returned task stores an <see cref="AggregateException" />.
    /// If none fault but at least one is canceled, the returned task is canceled after all inputs complete.
    /// </remarks>
    /// <param name="tasks">The tasks to wait on for completion.</param>
    /// <typeparam name="T">The type of the result returned by the task.</typeparam>
    /// <returns>A task that represents the completion of all of the supplied tasks.</returns>
    public static GDTask<T[]> WhenAll<T>(params GDTask<T>[] tasks)
    {
        if (tasks.Length == 0) return FromResult(Array.Empty<T>());
        return new(new WhenAllPromise<T>(tasks), 0);
    }

    /// <inheritdoc cref="WhenAll{T}(GDTask{T}[])" />
    public static GDTask<T[]> WhenAll<T>(params ReadOnlySpan<GDTask<T>> tasks)
    {
        if (tasks.Length == 0) return FromResult(Array.Empty<T>());
        return new(new WhenAllPromise<T>(tasks), 0);
    }

    /// <inheritdoc cref="WhenAll{T}(GDTask{T}[])" />
    public static GDTask<T[]> WhenAll<T>(IEnumerable<GDTask<T>> tasks)
    {
        using var usage = EnumerableExtensions.ToSpan(tasks, out var span);
        var promise = new WhenAllPromise<T>(span); // consumed array in constructor.
        return new(promise, 0);
    }

    /// <summary>
    /// Creates a task that will complete when all of the supplied tasks have completed.
    /// </summary>
    /// <remarks>
    /// If any supplied task faults, the returned task faults after all inputs complete.
    /// If multiple supplied tasks fault, the returned task stores an <see cref="AggregateException" />.
    /// If none fault but at least one is canceled, the returned task is canceled after all inputs complete.
    /// </remarks>
    /// <param name="tasks">The tasks to wait on for completion.</param>
    /// <returns>A task that represents the completion of all of the supplied tasks.</returns>
    public static GDTask WhenAll(params GDTask[] tasks)
    {
        if (tasks.Length == 0) return CompletedTask;

        if (tasks.Length == 1) return tasks[0];

        return new(new WhenAllPromise(tasks), 0);
    }

    /// <inheritdoc cref="WhenAll(GDTask[])" />
    public static GDTask WhenAll(params ReadOnlySpan<GDTask> tasks)
    {
        if (tasks.Length == 0) return CompletedTask;

        if (tasks.Length == 1) return tasks[0];

        return new(new WhenAllPromise(tasks), 0);
    }

    /// <inheritdoc cref="WhenAll(GDTask[])" />
    public static GDTask WhenAll(IEnumerable<GDTask> tasks)
    {
        using var usage = EnumerableExtensions.ToSpan(tasks, out var span);
        if (span.Length == 0) return CompletedTask;

        if (span.Length == 1) return span[0];
        var promise = new WhenAllPromise(span); // consumed array in constructor.
        return new(promise, 0);
    }

    private sealed class WhenAllPromise<T> : IGDTaskSource<T[]>
    {
        private readonly T[] _result;
        private readonly WhenAllSharedState _sharedState;
        private GDTaskCompletionSourceCore<T[]> _core; // don't reset(called after GetResult, will invoke TrySetException.)

        public WhenAllPromise(ReadOnlySpan<GDTask<T>> tasks)
        {
            TaskTracker.TrackActiveTask(this, 3);

            if (tasks.Length == 0)
            {
                _result = Array.Empty<T>();
                _core.TrySetResult(_result);
                return;
            }

            _result = new T[tasks.Length];
            _sharedState = new WhenAllSharedState(tasks.Length);

            for (var i = 0; i < tasks.Length; i++)
            {
                GDTask<T>.Awaiter awaiter;

                try { awaiter = tasks[i].GetAwaiter(); }
                catch (Exception ex)
                {
                    TrySignalWhenAllCompletion(_sharedState.RecordCompletion(ex), ref _core, _result);
                    continue;
                }

                if (awaiter.IsCompleted) TryInvokeContinuation(this, awaiter, i);
                else
                    awaiter.SourceOnCompleted(
                        state =>
                        {
                            using var t = (StateTuple<WhenAllPromise<T>, GDTask<T>.Awaiter, int>)state;
                            TryInvokeContinuation(t.Item1, t.Item2, t.Item3);
                        },
                        StateTuple.Create(this, awaiter, i)
                    );
            }
        }

        public T[] GetResult(short token)
        {
            TaskTracker.RemoveTracking(this);
            GC.SuppressFinalize(this);
            return _core.GetResult(token);
        }

        void IGDTaskSource.GetResult(short token) => GetResult(token);

        public GDTaskStatus GetStatus(short token) => _core.GetStatus(token);

        public GDTaskStatus UnsafeGetStatus() => _core.UnsafeGetStatus();

        public void OnCompleted(Action<object> continuation, object state, short token) => _core.OnCompleted(continuation, state, token);

        private static void TryInvokeContinuation(WhenAllPromise<T> self, in GDTask<T>.Awaiter awaiter, int i)
        {
            Exception exception = null;

            try { self._result[i] = awaiter.GetResult(); }
            catch (Exception ex) { exception = ex; }

            TrySignalWhenAllCompletion(self._sharedState.RecordCompletion(exception), ref self._core, self._result);
        }
    }

    private sealed class WhenAllPromise : IGDTaskSource
    {
        private readonly int _tasksLength;
        private readonly WhenAllSharedState _sharedState;
        private GDTaskCompletionSourceCore<AsyncUnit> _core; // don't reset(called after GetResult, will invoke TrySetException.)

        public WhenAllPromise(ReadOnlySpan<GDTask> tasks)
        {
            TaskTracker.TrackActiveTask(this, 3);

            _tasksLength = tasks.Length;

            if (_tasksLength == 0)
            {
                _core.TrySetResult(AsyncUnit.Default);
                return;
            }

            _sharedState = new WhenAllSharedState(_tasksLength);

            for (var i = 0; i < _tasksLength; i++)
            {
                Awaiter awaiter;

                try { awaiter = tasks[i].GetAwaiter(); }
                catch (Exception ex)
                {
                    TrySignalWhenAllCompletion(_sharedState.RecordCompletion(ex), ref _core, AsyncUnit.Default);
                    continue;
                }

                if (awaiter.IsCompleted) TryInvokeContinuation(this, awaiter);
                else
                    awaiter.SourceOnCompleted(
                        state =>
                        {
                            using var t = (StateTuple<WhenAllPromise, Awaiter>)state;
                            TryInvokeContinuation(t.Item1, t.Item2);
                        },
                        StateTuple.Create(this, awaiter)
                    );
            }
        }

        public void GetResult(short token)
        {
            TaskTracker.RemoveTracking(this);
            GC.SuppressFinalize(this);
            _core.GetResult(token);
        }

        public GDTaskStatus GetStatus(short token) => _core.GetStatus(token);

        public GDTaskStatus UnsafeGetStatus() => _core.UnsafeGetStatus();

        public void OnCompleted(Action<object> continuation, object state, short token) => _core.OnCompleted(continuation, state, token);

        private static void TryInvokeContinuation(WhenAllPromise self, in Awaiter awaiter)
        {
            Exception exception = null;

            try { awaiter.GetResult(); }
            catch (Exception ex) { exception = ex; }

            TrySignalWhenAllCompletion(self._sharedState.RecordCompletion(exception), ref self._core, AsyncUnit.Default);
        }
    }
}
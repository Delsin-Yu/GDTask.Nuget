using System;
using System.Threading;

namespace GodotTask;

/// <summary>
/// Provides access a lazy initialized of an asynchronous work.
/// </summary>
public interface IAsyncLazy
{
    /// <summary>
    /// Access the initialized task.
    /// </summary>
    GDTask Task { get; }

    /// <summary>
    /// Gets an awaiter used to await this <see cref="GDTask" />.
    /// </summary>
    GDTask.Awaiter GetAwaiter();
}

/// <inheritdoc cref="IAsyncLazy" />
public interface IAsyncLazy<T>
{
    /// <inheritdoc cref="IAsyncLazy.Task" />
    GDTask<T> Task { get; }

    /// <inheritdoc cref="IAsyncLazy.GetAwaiter" />
    GDTask<T>.Awaiter GetAwaiter();
}

class AsyncLazy : IAsyncLazy
{
    private Func<GDTask> _taskFactory;
    private readonly GDTaskCompletionSource _completionSource;
    private GDTask.Awaiter _awaiter;

#if NET9_0_OR_GREATER
    private readonly Lock _syncLock;
#else
    private readonly object _syncLock;
#endif
    private bool _initialized;

    public AsyncLazy(Func<GDTask> taskFactory)
    {
        _taskFactory = taskFactory;
        _completionSource = new();
#if NET9_0_OR_GREATER
            _syncLock = new();
#else
        _syncLock = new();
#endif
        _initialized = false;
    }

    internal AsyncLazy(GDTask task)
    {
        _taskFactory = null;
        _completionSource = new();
        _syncLock = null;
        _initialized = true;

        var awaiter = task.GetAwaiter();

        if (awaiter.IsCompleted) SetCompletionSource(awaiter);
        else
        {
            _awaiter = awaiter;
            awaiter.SourceOnCompleted(SetCompletionSource, this);
        }
    }

    public GDTask Task
    {
        get
        {
            EnsureInitialized();
            return _completionSource.Task;
        }
    }

    public GDTask.Awaiter GetAwaiter() => Task.GetAwaiter();

    private void EnsureInitialized()
    {
        if (Volatile.Read(ref _initialized)) return;

        EnsureInitializedCore();
    }

    private void EnsureInitializedCore()
    {
        lock (_syncLock)
        {
            if (!Volatile.Read(ref _initialized))
            {
                var f = _taskFactory;

                if (f != null)
                {
                    var task = f();
                    var awaiter = task.GetAwaiter();

                    if (awaiter.IsCompleted) SetCompletionSource(awaiter);
                    else
                    {
                        _awaiter = awaiter;
                        awaiter.SourceOnCompleted(SetCompletionSource, this);
                    }

                    _taskFactory = null;
                    Volatile.Write(ref _initialized, true);
                }
            }
        }
    }

    private void SetCompletionSource(in GDTask.Awaiter awaiter)
    {
        try
        {
            awaiter.GetResult();
            _completionSource.TrySetResult();
        }
        catch (Exception ex) { _completionSource.TrySetException(ex); }
    }

    private static void SetCompletionSource(object state)
    {
        var self = (AsyncLazy)state;

        try
        {
            self._awaiter.GetResult();
            self._completionSource.TrySetResult();
        }
        catch (Exception ex) { self._completionSource.TrySetException(ex); }
        finally { self._awaiter = default; }
    }
}

class AsyncLazy<T> : IAsyncLazy<T>
{
    private Func<GDTask<T>> _taskFactory;
    private readonly GDTaskCompletionSource<T> _completionSource;
    private GDTask<T>.Awaiter _awaiter;

#if NET9_0_OR_GREATER
    private readonly Lock _syncLock;
#else
    private readonly object _syncLock;
#endif
    private bool _initialized;

    public AsyncLazy(Func<GDTask<T>> taskFactory)
    {
        _taskFactory = taskFactory;
        _completionSource = new();
        _syncLock = new();
        _initialized = false;
    }

    internal AsyncLazy(GDTask<T> task)
    {
        _taskFactory = null;
        _completionSource = new();
        _syncLock = null;
        _initialized = true;

        var awaiter = task.GetAwaiter();

        if (awaiter.IsCompleted) SetCompletionSource(awaiter);
        else
        {
            _awaiter = awaiter;
            awaiter.SourceOnCompleted(SetCompletionSource, this);
        }
    }

    public GDTask<T> Task
    {
        get
        {
            EnsureInitialized();
            return _completionSource.Task;
        }
    }

    public GDTask<T>.Awaiter GetAwaiter() => Task.GetAwaiter();

    private void EnsureInitialized()
    {
        if (Volatile.Read(ref _initialized)) return;

        EnsureInitializedCore();
    }

    private void EnsureInitializedCore()
    {
        lock (_syncLock)
        {
            if (!Volatile.Read(ref _initialized))
            {
                var f = _taskFactory;

                if (f != null)
                {
                    var task = f();
                    var awaiter = task.GetAwaiter();

                    if (awaiter.IsCompleted) SetCompletionSource(awaiter);
                    else
                    {
                        _awaiter = awaiter;
                        awaiter.SourceOnCompleted(SetCompletionSource, this);
                    }

                    _taskFactory = null;
                    Volatile.Write(ref _initialized, true);
                }
            }
        }
    }

    private void SetCompletionSource(in GDTask<T>.Awaiter awaiter)
    {
        try
        {
            var result = awaiter.GetResult();
            _completionSource.TrySetResult(result);
        }
        catch (Exception ex) { _completionSource.TrySetException(ex); }
    }

    private static void SetCompletionSource(object state)
    {
        var self = (AsyncLazy<T>)state;

        try
        {
            var result = self._awaiter.GetResult();
            self._completionSource.TrySetResult(result);
        }
        catch (Exception ex) { self._completionSource.TrySetException(ex); }
        finally { self._awaiter = default; }
    }
}
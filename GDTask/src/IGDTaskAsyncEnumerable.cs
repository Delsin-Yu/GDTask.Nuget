using System;
using System.Collections.Generic;
using System.Threading;

namespace GodotTask
{
    /// <inheritdoc cref="IAsyncEnumerable{T}"/>
    public interface IGDTaskAsyncEnumerable<out T>
#if NET9_0_OR_GREATER
        where T : allows ref struct
#endif
    {
        /// <inheritdoc cref="IAsyncEnumerable{T}.GetAsyncEnumerator(CancellationToken)"/>
        public IGDTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default);
    }

    /// <inheritdoc cref="IAsyncEnumerator{T}"/>
    public interface IGDTaskAsyncEnumerator<out T> : IGDTaskAsyncDisposable
#if NET9_0_OR_GREATER
        where T : allows ref struct
#endif
    {
        /// <inheritdoc cref="IAsyncEnumerator{T}.Current"/>
        public T Current { get; }
        /// <inheritdoc cref="IAsyncEnumerator{T}.MoveNextAsync()"/>
        public GDTask<bool> MoveNextAsync();
    }

    /// <inheritdoc cref="IAsyncDisposable"/>
    public interface IGDTaskAsyncDisposable
    {
        /// <inheritdoc cref="IAsyncDisposable.DisposeAsync()"/>
        public GDTask DisposeAsync();
    }
}

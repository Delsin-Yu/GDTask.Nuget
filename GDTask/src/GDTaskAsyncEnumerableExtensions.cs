using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GodotTask {
    /// <summary>
    /// Provides extensions methods for <see cref="IGDTaskAsyncEnumerable{T}"/> and <see cref="IAsyncEnumerable{T}"/>.
    /// </summary>
    public static class GDTaskAsyncEnumerableExtensions {
        /// <summary>
        /// Converts the <see cref="IAsyncEnumerable{T}"/> to an <see cref="IGDTaskAsyncEnumerable{T}"/>.
        /// </summary>
        public static IGDTaskAsyncEnumerable<T> AsGDTaskAsyncEnumerable<T>(this IAsyncEnumerable<T> source)
        {
            return new AsyncEnumerableToGDTaskAsyncEnumerable<T>(source);
        }

        /// <summary>
        /// Converts the <see cref="IGDTaskAsyncEnumerable{T}"/> to an <see cref="IAsyncEnumerable{T}"/>.
        /// </summary>
        public static IAsyncEnumerable<T> AsAsyncEnumerable<T>(this IGDTaskAsyncEnumerable<T> source)
        {
            return new GDTaskAsyncEnumerableToAsyncEnumerable<T>(source);
        }

        private sealed class AsyncEnumerableToGDTaskAsyncEnumerable<T> : IGDTaskAsyncEnumerable<T>
        {
            private readonly IAsyncEnumerable<T> source;

            public AsyncEnumerableToGDTaskAsyncEnumerable(IAsyncEnumerable<T> source)
            {
                this.source = source;
            }

            public IGDTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                return new Enumerator(source.GetAsyncEnumerator(cancellationToken));
            }

            private sealed class Enumerator : IGDTaskAsyncEnumerator<T>
            {
                private readonly IAsyncEnumerator<T> enumerator;

                public Enumerator(IAsyncEnumerator<T> enumerator)
                {
                    this.enumerator = enumerator;
                }

                public T Current => enumerator.Current;

                public async GDTask DisposeAsync()
                {
                    await enumerator.DisposeAsync();
                }

                public async GDTask<bool> MoveNextAsync()
                {
                    return await enumerator.MoveNextAsync();
                }
            }
        }

        private sealed class GDTaskAsyncEnumerableToAsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly IGDTaskAsyncEnumerable<T> source;

            public GDTaskAsyncEnumerableToAsyncEnumerable(IGDTaskAsyncEnumerable<T> source)
            {
                this.source = source;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                return new Enumerator(source.GetAsyncEnumerator(cancellationToken));
            }

            private sealed class Enumerator : IAsyncEnumerator<T>
            {
                private readonly IGDTaskAsyncEnumerator<T> enumerator;

                public Enumerator(IGDTaskAsyncEnumerator<T> enumerator)
                {
                    this.enumerator = enumerator;
                }

                public T Current => enumerator.Current;

                public ValueTask DisposeAsync()
                {
                    return enumerator.DisposeAsync().AsValueTask();
                }

                public ValueTask<bool> MoveNextAsync()
                {
                    return enumerator.MoveNextAsync().AsValueTask();
                }
            }
        }
    }
}

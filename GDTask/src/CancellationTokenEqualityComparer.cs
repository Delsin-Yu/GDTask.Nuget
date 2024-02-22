using System.Collections.Generic;
using System.Threading;

namespace GodotTask
{
    /// <summary>
    /// <see cref="System.Collections.Generic.IEqualityComparer{CancellationToken}" /> to support the comparison of <see cref="CancellationToken"/> for equality.
    /// </summary>
    public class CancellationTokenEqualityComparer : IEqualityComparer<CancellationToken>
    {
        private CancellationTokenEqualityComparer() { }

        /// <summary>
        /// Returns the default equality comparer for <see cref="CancellationToken"/>.
        /// </summary>
        public static readonly IEqualityComparer<CancellationToken> Default = new CancellationTokenEqualityComparer();

        /// <inheritdoc cref="CancellationToken.Equals(CancellationToken)"/>
        public bool Equals(CancellationToken x, CancellationToken y)
        {
            return x.Equals(y);
        }

        /// <inheritdoc cref="CancellationToken.GetHashCode()"/>
        public int GetHashCode(CancellationToken obj)
        {
            return obj.GetHashCode();
        }
    }
}


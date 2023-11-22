using CK.Core;
using System;
using System.Threading.Tasks;

namespace CK.Object.Filter
{
    /// <summary>
    /// Simple adapter for asynchronous on synchronous filter.
    /// </summary>
    /// <typeparam name="T">The synchronous filter configuration.</typeparam>
    public class AsyncFilterAdapterConfiguration<T> : ObjectAsyncFilterConfiguration where T : ObjectFilterConfiguration
    {
        readonly T _sync;

        /// <summary>
        /// Initializes a new adapter.
        /// </summary>
        /// <param name="sync">The synchronous filter.</param>
        public AsyncFilterAdapterConfiguration( T sync )
            : base( sync.Configuration )
        {
            _sync = sync;
        }

        /// <summary>
        /// Creates an asynchronous predicate from a predicate created by <typeparamref name="T"/>.
        /// The resulting predicate simply uses <see cref="ValueTask.FromResult{TResult}(TResult)"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">The services.</param>
        /// <returns>A configured predicate or null for an empty predicate.</returns>
        public override Func<object, ValueTask<bool>>? CreatePredicate( IActivityMonitor monitor, IServiceProvider services )
        {
            Func<object, bool>? p = _sync.CreatePredicate( monitor, services );
            return p != null ? o => ValueTask.FromResult( p( o ) ) : null;
        }
    }
}

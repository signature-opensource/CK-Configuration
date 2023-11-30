using CK.Core;
using System;
using System.Threading.Tasks;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Simple adapter for asynchronous on synchronous predicate.
    /// </summary>
    /// <typeparam name="T">The synchronous predicate configuration.</typeparam>
    public class AsyncPredicateAdapterConfiguration<T> : ObjectAsyncPredicateConfiguration where T : ObjectPredicateConfiguration
    {
        readonly T _sync;

        /// <summary>
        /// Initializes a new adapter.
        /// </summary>
        /// <param name="sync">The synchronous predicate configuration.</param>
        public AsyncPredicateAdapterConfiguration( T sync )
            : base( sync.Configuration )
        {
            _sync = sync;
        }

        /// <summary>
        /// Creates an asynchronous predicate from a predicate created by <typeparamref name="T"/>.
        /// The resulting predicate simply uses <see cref="ValueTask.FromResult{TResult}(TResult)"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">Services that may be required for some (complex) predicates.</param>
        /// <returns>A configured predicate or null for an empty predicate.</returns>
        public override Func<object, ValueTask<bool>>? CreateAsyncPredicate( IActivityMonitor monitor, IServiceProvider services )
        {
            Func<object, bool>? p = _sync.CreatePredicate( monitor, services );
            return p != null ? o => ValueTask.FromResult( p( o ) ) : null;
        }
    }
}

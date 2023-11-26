using CK.Core;
using System;
using System.Threading.Tasks;

namespace CK.Object.Transform
{
    /// <summary>
    /// Simple adapter for asynchronous on synchronous transform function.
    /// </summary>
    /// <typeparam name="T">The synchronous transform function configuration.</typeparam>
    public class AsyncTransformAdapterConfiguration<T> : ObjectAsyncTransformConfiguration where T : ObjectTransformConfiguration
    {
        readonly T _sync;

        /// <summary>
        /// Initializes a new adapter.
        /// </summary>
        /// <param name="sync">The synchronous transform function.</param>
        public AsyncTransformAdapterConfiguration( T sync )
            : base( sync.Configuration )
        {
            _sync = sync;
        }

        /// <summary>
        /// Creates an asynchronous transform function from a synchronous one created by <typeparamref name="T"/>.
        /// The resulting function simply uses <see cref="ValueTask.FromResult{TResult}(TResult)"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">The services.</param>
        /// <returns>A configured transform function or null for the identity function.</returns>
        public override Func<object, ValueTask<object>>? CreateTransform( IActivityMonitor monitor, IServiceProvider services )
        {
            Func<object, object>? p = _sync.CreateTransform( monitor, services );
            return p != null ? o => ValueTask.FromResult( p( o ) ) : null;
        }
    }
}

//using CK.Core;
//using System;
//using System.Threading.Tasks;

//namespace CK.Object.Processor
//{
//    /// <summary>
//    /// Simple adapter for asynchronous on synchronous processor.
//    /// </summary>
//    /// <typeparam name="T">The synchronous processor configuration.</typeparam>
//    public class AsyncProcessorAdapterConfiguration<T> : ObjectAsyncProcessorConfiguration where T : ObjectProcessorConfiguration
//    {
//        readonly T _sync;

//        /// <summary>
//        /// Initializes a new adapter.
//        /// </summary>
//        /// <param name="sync">The synchronous processor configuration.</param>
//        public AsyncProcessorAdapterConfiguration( T sync )
//            : base( sync.Configuration )
//        {
//            _sync = sync;
//        }

//        /// <summary>
//        /// Creates an asynchronous processor from a synchronous one created by <typeparamref name="T"/>.
//        /// The resulting function simply uses <see cref="ValueTask.FromResult{TResult}(TResult?)"/>.
//        /// </summary>
//        /// <param name="monitor">The monitor that must be used to signal errors.</param>
//        /// <param name="services">The services.</param>
//        /// <returns>A configured processor or null for the void processor.</returns>
//        public override Func<object, ValueTask<object?>>? CreateProcessor( IActivityMonitor monitor, IServiceProvider services )
//        {
//            Func<object, object?>? p = _sync.CreateProcessor( monitor, services );
//            return p != null ? o => ValueTask.FromResult( p( o ) ) : null;
//        }
//    }
//}

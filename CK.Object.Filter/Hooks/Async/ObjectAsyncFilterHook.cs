using CK.Core;
using System;
using System.Threading.Tasks;

namespace CK.Object.Filter
{
    /// <summary>
    /// Hook implementation for asynchronous predicates.
    /// </summary>
    public class ObjectAsyncFilterHook : IObjectFilterHook
    {
        readonly EvaluationHook _hook;
        readonly IObjectFilterConfiguration _configuration;
        readonly Func<object, ValueTask<bool>> _predicate;

        /// <summary>
        /// Initializes a new hook.
        /// </summary>
        /// <param name="hook">The evaluation hook.</param>
        /// <param name="configuration">The filter configuration.</param>
        /// <param name="predicate">The predicate.</param>
        public ObjectAsyncFilterHook( EvaluationHook hook, IObjectFilterConfiguration configuration, Func<object, ValueTask<bool>> predicate )
        {
            Throw.CheckNotNullArgument( hook );
            Throw.CheckNotNullArgument( configuration );
            Throw.CheckNotNullArgument( predicate );
            _hook = hook;
            _configuration = configuration;
            _predicate = predicate;
        }

        // Constructor for GroupAsyncFilter.
        internal ObjectAsyncFilterHook( EvaluationHook hook, IObjectFilterConfiguration configuration )
        {
            Throw.CheckNotNullArgument( hook );
            Throw.CheckNotNullArgument( configuration );
            _hook = hook;
            _configuration = configuration;
            _predicate = null!;
        }

        /// <inheritdoc />
        public IObjectFilterConfiguration Configuration => _configuration;

        /// <summary>
        /// Evaluates the predicate.
        /// </summary>
        /// <param name="o">The object to evaluate.</param>
        /// <returns>The predicate result.</returns>
        public virtual async ValueTask<bool> EvaluateAsync( object o )
        {
            if( !_hook.OnBeforeEvaluate( this, o ) )
            {
                return false;
            }
            bool r = false;
            try
            {
                r = await DoEvaluateAsync( o ).ConfigureAwait( false );
            }
            catch( Exception ex )
            {
                if( _hook.OnEvaluationError( this, o, ex ) )
                {
                    throw;
                }
            }
            return _hook.OnAfterEvaluate( this, o, r );
        }

        protected virtual ValueTask<bool> DoEvaluateAsync( object o ) => _predicate( o );
    }

}

using CK.Core;
using System;

namespace CK.Object.Filter
{
    /// <summary>
    /// Hook implementation for synchronous predicate.
    /// </summary>
    public class ObjectFilterHook : IObjectFilterHook
    {
        readonly EvaluationHook _hook;
        readonly IObjectFilterConfiguration _configuration;
        readonly Func<object, bool> _predicate;

        /// <summary>
        /// Initializes a new hook.
        /// </summary>
        /// <param name="hook">The evaluation hook.</param>
        /// <param name="configuration">The filter configuration.</param>
        /// <param name="predicate">The predicate.</param>
        public ObjectFilterHook( EvaluationHook hook, IObjectFilterConfiguration configuration, Func<object, bool> predicate )
        {
            Throw.CheckNotNullArgument( hook );
            Throw.CheckNotNullArgument( configuration );
            Throw.CheckNotNullArgument( predicate );
            _hook = hook;
            _configuration = configuration;
            _predicate = predicate;
        }

        // Constructor for GroupFilterHook.
        internal ObjectFilterHook( EvaluationHook hook, IObjectFilterConfiguration configuration )
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
        public bool Evaluate( object o )
        {
            if( !_hook.OnBeforeEvaluate( this, o ) )
            {
                return false;
            }
            bool r = false;
            try
            {
                r = DoEvaluate( o );
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

        /// <summary>
        /// Actual evaluation of the predicate.
        /// </summary>
        /// <param name="o">The object to evaluate.</param>
        /// <returns>The predicate result.</returns>
        protected virtual bool DoEvaluate( object o ) => _predicate( o );
    }

}

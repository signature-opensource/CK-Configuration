using CK.Core;
using CK.Object.Predicate;
using CK.Object.Transform;
using System;

namespace CK.Object.Processor
{
    /// <summary>
    /// Hook implementation for synchronous processors.
    /// </summary>
    public class ObjectProcessorHook : IObjectProcessorHook
    {
        readonly IProcessorEvaluationHook _hook;
        readonly IObjectProcessorConfiguration _configuration;
        readonly ObjectPredicateHook? _condition;
        readonly ObjectTransformHook? _action;

        /// <summary>
        /// Initializes a new hook.
        /// </summary>
        /// <param name="hook">The evaluation hook.</param>
        /// <param name="configuration">The processor configuration.</param>
        public ObjectProcessorHook( IProcessorEvaluationHook hook,
                                    IObjectProcessorConfiguration configuration,
                                    ObjectPredicateHook? condition,
                                    ObjectTransformHook? action )
        {
            Throw.CheckNotNullArgument( hook );
            Throw.CheckNotNullArgument( configuration );
            _hook = hook;
            _configuration = configuration;
            _condition = condition;
            _action = action;
        }

        /// <inheritdoc />
        public IObjectProcessorConfiguration Configuration => _configuration;

        IObjectPredicateHook? IObjectProcessorHook.Condition => _condition;

        public ObjectPredicateHook? Condition => _condition;

        IObjectTransformHook? IObjectProcessorHook.Action => _action;

        public ObjectTransformHook? Action => _action;

        /// <summary>
        /// Process the input object.
        /// </summary>
        /// <param name="o">The object to process.</param>
        /// <returns>The processed object or null if this processor rejects this input.</returns>
        public object? Process( object o )
        {
            Throw.CheckNotNullArgument( o );
            object? r = _hook.OnBeforeProcessor( this, o );
            if( r != null ) return r;
            try
            {
                r = DoProcess( o );
                return _hook.OnAfterProcessor( this, o, r );
            }
            catch( Exception ex )
            {
                r = _hook.OnProcessorError( this, o, ex );
                if( r == null )
                {
                    throw;
                }
                return r;
            }
        }

        /// <summary>
        /// Actual process.
        /// </summary>
        /// <param name="o">The object to process (necessarily not null).</param>
        /// <returns>The process result.</returns>
        protected virtual object? DoProcess( object o )
        {
            if( _condition != null && !_condition.Evaluate( o ) )
            {
                return null;
            }
            return _action != null ? _action.Transform( o ) : o;
        }
    }

}

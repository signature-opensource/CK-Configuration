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
        readonly ProcessorHookContext _hook;
        readonly IObjectProcessorConfiguration _configuration;
        readonly ObjectPredicateHook? _condition;
        readonly ObjectTransformHook? _action;

        /// <summary>
        /// Initializes a new hook.
        /// </summary>
        /// <param name="hook">The hook context.</param>
        /// <param name="configuration">The processor configuration.</param>
        public ObjectProcessorHook( ProcessorHookContext hook,
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

        IObjectTransformHook? IObjectProcessorHook.Transform => _action;

        public ObjectTransformHook? Transform => _action;

        /// <summary>
        /// Process the input object.
        /// </summary>
        /// <param name="o">The object to process.</param>
        /// <returns>The processed object or null if this processor rejects this input.</returns>
        public virtual object? Process( object o )
        {
            Throw.CheckNotNullArgument( o );
            if( _condition != null && !_condition.Evaluate( o ) )
            {
                return null;
            }
            return _action != null ? _action.Transform( o ) : o;
        }

    }

}

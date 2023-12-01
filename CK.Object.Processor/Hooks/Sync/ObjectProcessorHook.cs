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
        readonly ProcessorHookContext _context;
        readonly IObjectProcessorConfiguration _configuration;
        readonly SyncObjectPredicateHook? _condition;
        readonly ObjectTransformHook? _action;

        /// <summary>
        /// Initializes a new hook.
        /// </summary>
        /// <param name="context">The hook context.</param>
        /// <param name="configuration">The processor configuration.</param>
        public ObjectProcessorHook( ProcessorHookContext context,
                                    IObjectProcessorConfiguration configuration,
                                    SyncObjectPredicateHook? condition,
                                    ObjectTransformHook? action )
        {
            Throw.CheckNotNullArgument( context );
            Throw.CheckNotNullArgument( configuration );
            _context = context;
            _configuration = configuration;
            _condition = condition;
            _action = action;
        }

        /// <inheritdoc />
        public IObjectProcessorConfiguration Configuration => _configuration;

        IObjectPredicateHook? IObjectProcessorHook.Condition => _condition;

        /// <inheritdoc />
        public SyncObjectPredicateHook? Condition => _condition;

        IObjectTransformHook? IObjectProcessorHook.Transform => _action;

        /// <inheritdoc />
        public ObjectTransformHook? Transform => _action;

        /// <inheritdoc />
        public ProcessorHookContext Context => _context;

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

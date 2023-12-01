using CK.Core;
using CK.Object.Predicate;
using CK.Object.Transform;
using System;
using System.Threading.Tasks;

namespace CK.Object.Processor
{
    /// <summary>
    /// Hook implementation for synchronous processors.
    /// </summary>
    public class ObjectAsyncProcessorHook : IObjectProcessorHook
    {
        readonly ProcessorHookContext _context;
        readonly IObjectProcessorConfiguration _configuration;
        readonly IObjectPredicateHook? _condition;
        readonly IObjectTransformHook? _action;

        /// <summary>
        /// Initializes a new hook.
        /// </summary>
        /// <param name="context">The hook context.</param>
        /// <param name="configuration">The processor configuration.</param>
        public ObjectAsyncProcessorHook( ProcessorHookContext context,
                                         IObjectProcessorConfiguration configuration,
                                         IObjectPredicateHook? condition,
                                         IObjectTransformHook? action )
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

        /// <inheritdoc />
        public ProcessorHookContext Context => _context;

        /// <inheritdoc />
        public IObjectPredicateHook? Condition => _condition;

        /// <inheritdoc />
        public IObjectTransformHook? Transform => _action;

        /// <summary>
        /// Process the input object.
        /// </summary>
        /// <param name="o">The object to process.</param>
        /// <returns>The processed object or null if this processor rejects this input.</returns>
        public virtual async ValueTask<object?> ProcessAsync( object o )
        {
            Throw.CheckNotNullArgument( o );
            if( _condition != null && !await _condition.EvaluateAsync( o ).ConfigureAwait( false ) )
            {
                return null;
            }
            return _action != null ? await _action.TransformAsync( o ).ConfigureAwait( false ) : o;
        }

    }

}

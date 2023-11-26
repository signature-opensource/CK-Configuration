using CK.Core;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CK.Object.Processor
{
    /// <summary>
    /// Hook implementation for sequence of synchronous object processors.
    /// </summary>
    public class SequenceProcessorHook : ObjectProcessorHook, ISequenceProcessorHook
    {
        readonly ImmutableArray<ObjectProcessorHook> _processors;

        /// <summary>
        /// Initializes a new hook.
        /// </summary>
        /// <param name="configuration">The processor configuration.</param>
        /// <param name="hook">The evaluation hook.</param>
        /// <param name="processors">The subordinated processors.</param>
        public SequenceProcessorHook( IProcessorEvaluationHook hook,
                                      ISequenceProcessorConfiguration configuration,
                                      Predicate.ObjectPredicateHook? condition,
                                      Transform.ObjectTransformHook? action,
                                      ImmutableArray<ObjectProcessorHook> processors )
            : base( hook, configuration, condition, action )
        {
            _processors = processors;
        }

        /// <inheritdoc />
        public new ISequenceProcessorConfiguration Configuration => Unsafe.As<ISequenceProcessorConfiguration>( base.Configuration );

        ImmutableArray<IObjectProcessorHook> ISequenceProcessorHook.Processors => ImmutableArray<IObjectProcessorHook>.CastUp( _processors );

        /// <inheritdoc cref="ISequenceProcessorHook.Processors" />
        public ImmutableArray<ObjectProcessorHook> Processors => _processors;

        /// <inheritdoc />
        protected override object? DoProcess( object o )
        {
            if( Condition != null && !Condition.Evaluate( o ) )
            {
                return null;
            }
            foreach( var i in _processors )
            {
                Throw.DebugAssert( o != null );
                o = i.Process( o )!;
                if( o != null ) break;
            }
            if( o != null && Action != null )
            {
                o = Action.Transform( o );
            }
            return o;
        }
    }

}

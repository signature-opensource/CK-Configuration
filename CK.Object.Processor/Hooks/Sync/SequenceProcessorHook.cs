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
        /// <param name="hook">The hook context.</param>
        /// <param name="condition">The condition.</param>
        /// <param name="transform">The transform action.</param>
        /// <param name="processors">The subordinated processors.</param>
        public SequenceProcessorHook( ProcessorHookContext hook,
                                      ISequenceProcessorConfiguration configuration,
                                      Predicate.SyncObjectPredicateHook? condition,
                                      Transform.ObjectTransformHook? transform,
                                      ImmutableArray<ObjectProcessorHook> processors )
            : base( hook, configuration, condition, transform )
        {
            _processors = processors;
        }

        /// <inheritdoc />
        public new ISequenceProcessorConfiguration Configuration => Unsafe.As<ISequenceProcessorConfiguration>( base.Configuration );

        ImmutableArray<IObjectProcessorHook> ISequenceProcessorHook.Processors => ImmutableArray<IObjectProcessorHook>.CastUp( _processors );

        /// <inheritdoc cref="ISequenceProcessorHook.Processors" />
        public ImmutableArray<ObjectProcessorHook> Processors => _processors;

        /// <inheritdoc />
        public override object? Process( object o )
        {
            if( Condition != null && !Condition.Evaluate( o ) )
            {
                return null;
            }
            object? o2 = null;
            foreach( var i in _processors )
            {
                Throw.DebugAssert( o != null );
                o2 = i.Process( o )!;
                if( o2 != null ) break;
            }
            if( o2 != null && Transform != null )
            {
                o2 = Transform.Transform( o2 );
            }
            return o2;
        }
    }

}

using CK.Core;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CK.Object.Processor
{
    /// <summary>
    /// Hook implementation for sequence of synchronous object processors.
    /// </summary>
    public class SequenceAsyncProcessorHook : ObjectAsyncProcessorHook, ISequenceProcessorHook
    {
        readonly ImmutableArray<ObjectAsyncProcessorHook> _processors;

        /// <summary>
        /// Initializes a new hook.
        /// </summary>
        /// <param name="configuration">The processor configuration.</param>
        /// <param name="hook">The hook context.</param>
        /// <param name="condition">The condition.</param>
        /// <param name="transform">The transform action.</param>
        /// <param name="processors">The subordinated processors.</param>
        public SequenceAsyncProcessorHook( ProcessorHookContext hook,
                                           ISequenceProcessorConfiguration configuration,
                                           Predicate.ObjectAsyncPredicateHook? condition,
                                           Transform.ObjectAsyncTransformHook? transform,
                                           ImmutableArray<ObjectAsyncProcessorHook> processors )
            : base( hook, configuration, condition, transform )
        {
            _processors = processors;
        }

        /// <inheritdoc />
        public new ISequenceProcessorConfiguration Configuration => Unsafe.As<ISequenceProcessorConfiguration>( base.Configuration );

        ImmutableArray<IObjectProcessorHook> ISequenceProcessorHook.Processors => ImmutableArray<IObjectProcessorHook>.CastUp( _processors );

        /// <inheritdoc cref="ISequenceProcessorHook.Processors" />
        public ImmutableArray<ObjectAsyncProcessorHook> Processors => _processors;

        /// <inheritdoc />
        public override async ValueTask<object?> ProcessAsync( object o )
        {
            if( Condition != null && !await Condition.EvaluateAsync( o ).ConfigureAwait( false ) )
            {
                return null;
            }
            object? o2 = null;
            foreach( var i in _processors )
            {
                Throw.DebugAssert( o != null );
                o2 = await i.ProcessAsync( o ).ConfigureAwait( false )!;
                if( o2 != null ) break;
            }
            if( o2 != null && Transform != null )
            {
                o2 = await Transform.TransformAsync( o2 ).ConfigureAwait( false );
            }
            return o2;
        }
    }

}

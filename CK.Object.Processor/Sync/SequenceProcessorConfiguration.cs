using CK.Core;
using CK.Object.Predicate;
using CK.Object.Transform;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace CK.Object.Processor
{
    /// <summary>
    /// Composite for synchronous processors: the subordinated <see cref="Processors"/> are
    /// called like a switch-case: the first one that doesn't return null is the winner.
    /// </summary>
    public sealed class SequenceProcessorConfiguration : ObjectProcessorConfiguration, ISequenceProcessorConfiguration
    {
        readonly ImmutableArray<ObjectProcessorConfiguration> _processors;

        /// <summary>
        /// Required constructor.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors and warnings.</param>
        /// <param name="builder">The builder.</param>
        /// <param name="configuration">The configuration for this object.</param>
        /// <param name="processors">The subordinated items.</param>
        public SequenceProcessorConfiguration( IActivityMonitor monitor,
                                               PolymorphicConfigurationTypeBuilder builder,
                                               ImmutableConfigurationSection configuration,
                                               IReadOnlyList<ObjectProcessorConfiguration> processors )
            : base( monitor, builder, configuration )
        {
            _processors = processors.ToImmutableArray();
        }

        /// <summary>
        /// Mutation constructor.
        /// </summary>
        /// <param name="source">The original configuration.</param>
        /// <param name="newCondition">Optional new <see cref="Condition"/>.</param>
        /// <param name="newAction">Optional new <see cref="Action"/>.</param>
        /// <param name="newProcessors">Optional new processors.</param>
        internal SequenceProcessorConfiguration( SequenceProcessorConfiguration source,
                                                 ObjectPredicateConfiguration? newCondition,
                                                 ObjectTransformConfiguration? newAction,
                                                 ImmutableArray<ObjectProcessorConfiguration>? newProcessors )
            : base( source, newCondition, newAction )
        {
            _processors = newProcessors ?? source._processors;
        }

        IReadOnlyList<IObjectProcessorConfiguration> ISequenceProcessorConfiguration.Processors => _processors;

        /// <inheritdoc cref="ISequenceProcessorConfiguration.Processors" />
        public IReadOnlyList<ObjectProcessorConfiguration> Processors => _processors;

        /// <inheritdoc />
        public override ObjectProcessorHook? CreateHook( IActivityMonitor monitor, IProcessorEvaluationHook hook, IServiceProvider services )
        {
            ImmutableArray<ObjectProcessorHook> processors = _processors.Select( c => c.CreateHook( monitor, hook, services ) )
                                                                        .Where( s => s != null )
                                                                        .ToImmutableArray()!;
            // Trivial case: not a composite. Base implementation handles the Condition and the Action.
            if( processors.Length == 0 ) return base.CreateHook( monitor, hook, services );

            var thisCondition = CreateConditionHook( monitor, hook.ConditionEvaluationHook, services );
            var thisAction = CreateActionHook( monitor, hook.ActionEvaluationHook, services );
            return new SequenceProcessorHook( hook, this, thisCondition, thisAction, processors );
        }

        /// <inheritdoc />
        public override Func<object, object?>? CreateProcessor( IActivityMonitor monitor, IServiceProvider services )
        {
            ImmutableArray<Func<object, object?>> processors = _processors.Select( c => c.CreateProcessor( monitor, services ) )
                                                                          .Where( f => f != null )
                                                                          .ToImmutableArray()!;
            // Trivial case: not a composite. Base implementation handles the Condition and the Action.
            if( processors.Length == 0 ) return base.CreateProcessor( monitor, services );
            // Regular case: we have one or more processors.
            var innerProcessor = processors.Length == 1 ? processors[0] : o => Apply( processors, o );
            var thisCondition = CreateCondition( monitor, services );
            var thisAction = CreateAction( monitor, services );
            if( thisCondition != null )
            {
                if( thisAction != null )
                {
                    // Full composite with its own condition and action.
                    // This action is applied as a "finalizer" or a "post processor" only if the inner processor
                    // processed the input object.
                    object? r = null;
                    return o => thisCondition(o)
                                ? ((r = innerProcessor(o)) != null ? thisAction(r) : null)
                                : null;
                }
                // No action at this level, only this condition must be challenged before submitting it to the inner processor.
                return o => thisCondition( o ) ? innerProcessor( o ) : null;
            }
            else
            {
                // No condition at this level: the inner processor rules.
                if( thisAction != null )
                {
                    // Apllies this "post processor" only if the inner processor processed the input object.
                    object? r = null;
                    return o => (r = innerProcessor( o )) != null ? thisAction( r ) : null;
                }
                // Nothing at this level. Inner processor does the job.
                return innerProcessor;
            }

            static object? Apply( ImmutableArray<Func<object, object?>> processors, object o )
            {
                foreach( var t in processors )
                {
                    Throw.DebugAssert( o != null );
                    o = t( o )!;
                    if( o != null ) break;
                }
                return o;
            }
        }

        /// <summary>
        /// Composite mutator.
        /// <para>
        /// Errors are emitted in the monitor. On error, this instance is returned. 
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use to signal errors.</param>
        /// <param name="configuration">Configuration of the replaced placeholder.</param>
        /// <returns>A new configuration or this instance if an error occurred or the placeholder has not been found.</returns>
        protected override ObjectProcessorConfiguration DoSetPlaceholder( IActivityMonitor monitor,
                                                                          IConfigurationSection configuration,
                                                                          ObjectPredicateConfiguration? condition,
                                                                          ObjectTransformConfiguration? action )
        {
            // Handles placeholder inside Processors.
            ImmutableArray<ObjectProcessorConfiguration>.Builder? newItems = null;
            for( int i = 0; i < _processors.Length; i++ )
            {
                var item = _processors[i];
                var r = item.SetPlaceholder( monitor, configuration );
                if( r != item )
                {
                    if( newItems == null )
                    {
                        newItems = ImmutableArray.CreateBuilder<ObjectProcessorConfiguration>( _processors.Length );
                        newItems.AddRange( _processors.Take( i ) );
                    }
                }
                newItems?.Add( r );
            }
            return condition != Condition || newItems != null || action != Action
                    ? new SequenceProcessorConfiguration( this, condition, action, newItems?.ToImmutable() ?? _processors )
                    : this;
        }

    }

}

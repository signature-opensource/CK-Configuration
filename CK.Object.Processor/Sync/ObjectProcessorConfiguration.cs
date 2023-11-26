using CK.Core;
using CK.Object.Predicate;
using CK.Object.Transform;
using Microsoft.Extensions.Configuration;
using System;
using System.Security;

namespace CK.Object.Processor
{
    /// <summary>
    /// Configuration base class for synchronous processor.
    /// <para>
    /// This is a concrete type that handles 
    /// </para>
    /// </summary>
    public partial class ObjectProcessorConfiguration : IObjectProcessorConfiguration
    {
        readonly ImmutableConfigurationSection _configuration;
        readonly ObjectPredicateConfiguration? _condition;
        readonly ObjectTransformConfiguration? _action;

        /// <summary>
        /// Handles "Condition" and "Action" from the configuration section.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors and warnings.</param>
        /// <param name="builder">The builder.</param>
        /// <param name="configuration">The configuration for this object.</param>
        protected ObjectProcessorConfiguration( IActivityMonitor monitor,
                                                PolymorphicConfigurationTypeBuilder builder,
                                                ImmutableConfigurationSection configuration )
        {
            _configuration = configuration;
            var cCond = configuration.TryGetSection( "Condition" );
            if( cCond != null )
            {
                _condition = builder.Create<ObjectPredicateConfiguration>( monitor, cCond );
            }
            var cAction = configuration.TryGetSection( "Action" );
            if( cAction != null )
            {
                _action = builder.Create<ObjectTransformConfiguration>( monitor, cAction );
            }
        }

        /// <summary>
        /// Mutation constructor.
        /// <para>
        /// As long as only the Placeholder is used to mutate configurations, <paramref name="condition"/> (and <paramref name="action"/>)
        /// can be null only if current <see cref="Condition"/> (and <see cref="Action"/>) is null. We don't check this here to allow
        /// other kind of mutations to be supported if needed.
        /// </para>
        /// </summary>
        /// <param name="source">The original configuration.</param>
        /// <param name="condition">The <see cref="Condition"/>.</param>
        /// <param name="action">The <see cref="Action"/>.</param>
        protected ObjectProcessorConfiguration( ObjectProcessorConfiguration source,
                                                ObjectPredicateConfiguration? condition,
                                                ObjectTransformConfiguration? action )
        {
            Throw.CheckNotNullArgument( source );
            _configuration = source._configuration;
            _condition = condition;
            _action = action;
        }

        /// <inheritdoc />
        public ImmutableConfigurationSection Configuration => _configuration;

        /// <inheritdoc />
        public ObjectPredicateConfiguration? Condition => _condition;

        /// <inheritdoc />
        public ObjectTransformConfiguration? Action => _action;

        /// <summary>
        /// Creates a synchronous processor function that requires external services to do its job.
        /// <para>
        /// This base implementation creates a function based on <see cref="Condition"/> and <see cref="Action"/>
        /// configurations.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">The services.</param>
        /// <returns>A configured processor function or null for a void processor.</returns>
        public virtual Func<object, object?>? CreateProcessor( IActivityMonitor monitor, IServiceProvider services )
        {
            return CreateFromConditionAndAction( CreateCondition( monitor, services ), CreateAction( monitor, services ) );
        }

        /// <summary>
        /// Creates the condition (an object predicate). 
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">The services.</param>
        /// <returns>The predicate or null for the empty predicate.</returns>
        protected virtual Func<object, bool>? CreateCondition( IActivityMonitor monitor, IServiceProvider services )
        {
            return _condition?.CreatePredicate( monitor, services );
        }

        /// <summary>
        /// Creates the action (an object transform function). 
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">The services.</param>
        /// <returns>The transform function or null for the identity function.</returns>
        protected virtual Func<object, object>? CreateAction( IActivityMonitor monitor, IServiceProvider services )
        {
            return _action?.CreateTransform( monitor, services );
        }

        /// <summary>
        /// Core helper that creates a processor function from optional predicate and transform function.
        /// <para>
        /// When predicate and transform function are both null, the (null) void processor is returned.
        /// </para>
        /// </summary>
        /// <param name="c">The predicate.</param>
        /// <param name="a">The transform function.</param>
        /// <returns>A configured processor function or null for a void processor.</returns>
        protected static Func<object, object?>? CreateFromConditionAndAction( Func<object, bool>? c, Func<object, object>? a )
        {
            if( c != null )
            {
                if( a != null )
                {
                    // "Full processor" with its conditonned action.
                    return o => c( o ) ? a( o ) : null;
                }
                // Condition only processor: the action is the identity function.
                return o => c( o ) ? o : null;
            }
            // No condition...
            if( a != null )
            {
                // The action is the process.
                return a;
            }
            // No condition, no action: void processor.
            return null;
        }

        /// <summary>
        /// Creates a <see cref="ObjectProcessorHook"/> with this configuration and a function obtained by
        /// calling <see cref="CreateProcessor(IActivityMonitor, IServiceProvider)"/>.
        /// <para>
        /// This should be overridden if this processor relies on other processors in order to hook all of them.
        /// Failing to do so will hide some processors to the evaluation hook.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="hook">The evaluation hook.</param>
        /// <param name="services">The services.</param>
        /// <returns>A wrapper bound to the evaluation hook or null for a void processor.</returns>
        public virtual ObjectProcessorHook? CreateHook( IActivityMonitor monitor, IProcessorEvaluationHook hook, IServiceProvider services )
        {
            var c = CreateConditionHook( monitor, hook.ConditionEvaluationHook, services );
            var a = CreateActionHook( monitor, hook.ActionEvaluationHook, services );
            return a != null || c != null ? new ObjectProcessorHook( hook, this, c, a ) : null;
        }

        /// <summary>
        /// Creates the condition hook object. 
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="hook">The evaluation hook.</param>
        /// <param name="services">The services.</param>
        /// <returns>The hook predicate or null for the empty predicate.</returns>
        protected virtual ObjectPredicateHook? CreateConditionHook( IActivityMonitor monitor,
                                                                    IPredicateEvaluationHook hook,
                                                                    IServiceProvider services )
        {
            return _condition?.CreateHook( monitor, hook, services );
        }

        /// <summary>
        /// Creates the action hook object. 
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="hook">The evaluation hook.</param>
        /// <param name="services">The services.</param>
        /// <returns>The transform hook or null for the identity function.</returns>
        protected virtual ObjectTransformHook? CreateActionHook( IActivityMonitor monitor,
                                                                 ITransformEvaluationHook hook,
                                                                 IServiceProvider services )
        {
            return _action?.CreateHook( monitor, hook, services );
        }

        /// <summary>
        /// Creates a synchronous processor that doesn't require any external service to do its job.
        /// <see cref="CreateProcessor(IActivityMonitor, IServiceProvider)"/> is called with an empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <returns>A configured processor or null for a void processor.</returns>
        public Func<object, object?>? CreateProcessor( IActivityMonitor monitor ) => CreateProcessor( monitor, EmptyServiceProvider.Instance );

        internal sealed class EmptyServiceProvider : IServiceProvider
        {
            public static readonly EmptyServiceProvider Instance = new EmptyServiceProvider();
            public object? GetService( Type serviceType ) => null;
        }

        /// <summary>
        /// Creates an <see cref="ObjectProcessorHook"/> that doesn't require any external service to do its job.
        /// <see cref="CreateHook(IActivityMonitor, IProcessorEvaluationHook, IServiceProvider)"/> is called with an
        /// empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="hook">The evaluation hook.</param>
        /// <returns>A wrapper bound to the evaluation hook or null for a void processor.</returns>
        public ObjectProcessorHook? CreateHook( IActivityMonitor monitor, IProcessorEvaluationHook hook ) => CreateHook( monitor, hook, EmptyServiceProvider.Instance );

        /// <summary>
        /// Adds a <see cref="PolymorphicConfigurationTypeBuilder.TypeResolver"/> for synchronous <see cref="ObjectProcessorConfiguration"/>.
        /// <list type="bullet">
        /// <item>The processors must be in the "CK.Object.Processor" namespace.</item>
        /// <item>Their name must end with "ProcessorConfiguration".</item>
        /// </list>
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="allowOtherNamespace">True to allow other namespaces than "CK.Object.Processor" to be specified.</param>
        /// <param name="compositeItemsFieldName">Name of the composite field.</param>
        public static void AddResolver( PolymorphicConfigurationTypeBuilder builder, bool allowOtherNamespace = false, string compositeItemsFieldName = "Processors" )
        {
            builder.AddResolver( new PolymorphicConfigurationTypeBuilder.StandardTypeResolver(
                                             baseType: typeof( ObjectProcessorConfiguration ),
                                             typeNamespace: "CK.Object.Processor",
                                             allowOtherNamespace: allowOtherNamespace,
                                             familyTypeNameSuffix: "Processor",
                                             compositeBaseType: typeof( SequenceProcessorConfiguration ),
                                             compositeItemsFieldName: compositeItemsFieldName ) );

        }

    }
}
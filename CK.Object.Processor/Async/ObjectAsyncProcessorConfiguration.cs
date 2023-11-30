using CK.Core;
using CK.Object.Predicate;
using CK.Object.Transform;
using Microsoft.Extensions.Configuration;
using System;
using System.Security;
using System.Threading.Tasks;

namespace CK.Object.Processor
{
    /// <summary>
    /// Configuration base class for synchronous processor.
    /// <para>
    /// This is a concrete type that handles an optional <see cref="Condition"/> and an optional <see cref="Transform"/>.
    /// </para>
    /// </summary>
    public partial class ObjectAsyncProcessorConfiguration : IObjectProcessorConfiguration
    {
        readonly ImmutableConfigurationSection _configuration;
        readonly ObjectAsyncPredicateConfiguration? _condition;
        readonly ObjectAsyncTransformConfiguration? _transform;

        /// <summary>
        /// Handles "Condition" and "Transform" from the configuration section.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors and warnings.</param>
        /// <param name="builder">The builder.</param>
        /// <param name="configuration">The configuration for this object.</param>
        protected ObjectAsyncProcessorConfiguration( IActivityMonitor monitor,
                                                     PolymorphicConfigurationTypeBuilder builder,
                                                     ImmutableConfigurationSection configuration )
        {
            _configuration = configuration;
            var cCond = configuration.TryGetSection( "Condition" );
            if( cCond != null )
            {
                _condition = builder.Create<ObjectAsyncPredicateConfiguration>( monitor, cCond );
            }
            var cTrans = configuration.TryGetSection( "Transform" );
            if( cTrans != null )
            {
                _transform = builder.Create<ObjectAsyncTransformConfiguration>( monitor, cTrans );
            }
        }

        /// <summary>
        /// Mutation constructor.
        /// <para>
        /// As long as only the Placeholder is used to mutate configurations, <paramref name="condition"/> (and <paramref name="transform"/>)
        /// can be null only if current <see cref="Condition"/> (and <see cref="Transform"/>) is null. We don't check this here to allow
        /// other kind of mutations to be supported if needed.
        /// </para>
        /// </summary>
        /// <param name="source">The original configuration.</param>
        /// <param name="condition">The <see cref="Condition"/>.</param>
        /// <param name="transform">The <see cref="Transform"/>.</param>
        protected ObjectAsyncProcessorConfiguration( ObjectAsyncProcessorConfiguration source,
                                                     ObjectAsyncPredicateConfiguration? condition,
                                                     ObjectAsyncTransformConfiguration? transform )
        {
            Throw.CheckNotNullArgument( source );
            _configuration = source._configuration;
            _condition = condition;
            _transform = transform;
        }

        /// <inheritdoc />
        public ImmutableConfigurationSection Configuration => _configuration;

        /// <summary>
        /// Creates a synchronous processor function.
        /// <para>
        /// This base implementation creates a function based on <see cref="Condition"/> and <see cref="Transform"/>
        /// configurations.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">Services that may be required for some (complex) transform functions.</param>
        /// <returns>A configured processor function or null for a void processor.</returns>
        public virtual Func<object, ValueTask<object?>>? CreateProcessor( IActivityMonitor monitor, IServiceProvider services )
        {
            Func<object, ValueTask<bool>>? c = CreateCondition( monitor, services );
            Func<object, ValueTask<object>>? t = CreateTransform( monitor, services );
            return CreateFromConditionAndTransform( c, t );
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
        protected static Func<object, ValueTask<object?>>? CreateFromConditionAndTransform( Func<object, ValueTask<bool>>? c, Func<object, ValueTask<object>>? a )
        {
            if( c != null )
            {
                if( a != null )
                {
                    // "Full processor" with its conditonned action.
                    return async o => await c( o ).ConfigureAwait( false ) ? await a( o ).ConfigureAwait( false ) : null;
                }
                // Condition only processor: the action is the identity function.
                return async o => await c( o ).ConfigureAwait( false ) ? o : null;
            }
            // No condition...
            if( a != null )
            {
                // The action is the process
                // (it returns a non null object, that is compatible with a object?: use the bang operator).
                return a!;
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
        /// <param name="hook">The hook context.</param>
        /// <param name="services">The services.</param>
        /// <returns>A wrapper bound to the hook context or null for a void processor.</returns>
        public virtual ObjectAsyncProcessorHook? CreateHook( IActivityMonitor monitor, ProcessorHookContext hook, IServiceProvider services )
        {
            var c = CreateConditionHook( monitor, hook.ConditionHookContext, services );
            var t = CreateTransformHook( monitor, hook.TransformHookContext, services );
            return t != null || c != null ? new ObjectAsyncProcessorHook( hook, this, c, t ) : null;
        }

        /// <summary>
        /// Creates a synchronous processor that doesn't require any external service to do its job.
        /// <see cref="CreateProcessor(IActivityMonitor, IServiceProvider)"/> is called with an empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <returns>A configured processor or null for a void processor.</returns>
        public Func<object, ValueTask<object?>>? CreateProcessor( IActivityMonitor monitor ) => CreateProcessor( monitor, EmptyServiceProvider.Instance );

        internal sealed class EmptyServiceProvider : IServiceProvider
        {
            public static readonly EmptyServiceProvider Instance = new EmptyServiceProvider();
            public object? GetService( Type serviceType ) => null;
        }

        /// <summary>
        /// Creates a <see cref="ObjectProcessorHook"/> that doesn't require any external service to do its job.
        /// <see cref="CreateHook(IActivityMonitor, ProcessorHookContext, IServiceProvider)"/> is called with an
        /// empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="context">The hook context.</param>
        /// <returns>A wrapper bound to the hook context or null for a void processor.</returns>
        public ObjectAsyncProcessorHook? CreateHook( IActivityMonitor monitor, ProcessorHookContext context ) => CreateHook( monitor, context, EmptyServiceProvider.Instance );

        /// <summary>
        /// Adds a <see cref="PolymorphicConfigurationTypeBuilder.TypeResolver"/> for synchronous <see cref="ObjectAsyncProcessorConfiguration"/>.
        /// <list type="bullet">
        /// <item>The processors must be in the "CK.Object.Processor" namespace.</item>
        /// <item>Their name must end with "AsyncProcessorConfiguration".</item>
        /// </list>
        /// This also calls <see cref="ObjectAsyncPredicateConfiguration.AddResolver(PolymorphicConfigurationTypeBuilder, bool, string)"/>
        /// and <see cref="ObjectAsyncTransformConfiguration.AddResolver(PolymorphicConfigurationTypeBuilder, bool, string)"/>.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="allowOtherNamespace">True to allow other namespaces than "CK.Object.Processor" to be specified.</param>
        /// <param name="compositeItemsFieldName">Name of the composite field.</param>
        public static void AddResolver( PolymorphicConfigurationTypeBuilder builder, bool allowOtherNamespace = false, string compositeItemsFieldName = "Processors" )
        {
            // Add the resolvers for Predicates and Transforms.
            ObjectAsyncPredicateConfiguration.AddResolver( builder, allowOtherNamespace );
            ObjectAsyncTransformConfiguration.AddResolver( builder, allowOtherNamespace );
            builder.AddResolver( new PolymorphicConfigurationTypeBuilder.StandardTypeResolver(
                                             baseType: typeof( ObjectAsyncProcessorConfiguration ),
                                             typeNamespace: "CK.Object.Processor",
                                             allowOtherNamespace: allowOtherNamespace,
                                             familyTypeNameSuffix: "AsyncProcessor",
                                             compositeBaseType: typeof( SequenceAsyncProcessorConfiguration ),
                                             compositeItemsFieldName: compositeItemsFieldName ) );
        }

    }
}

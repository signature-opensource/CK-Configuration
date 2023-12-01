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
        readonly string _configurationPath;
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
            _configurationPath = configuration.Path;
            var cCond = configuration.TryGetSection( "Condition" );
            if( cCond != null )
            {
                _condition = builder.HasBaseType<ObjectAsyncPredicateConfiguration>()
                                ? builder.Create<ObjectAsyncPredicateConfiguration>( monitor, cCond )
                                : builder.Create<ObjectPredicateConfiguration>( monitor, cCond );
            }
            var cTrans = configuration.TryGetSection( "Transform" );
            if( cTrans != null )
            {
                _transform = builder.HasBaseType<ObjectAsyncTransformConfiguration>()
                                ? builder.Create<ObjectAsyncTransformConfiguration>( monitor, cTrans )
                                : builder.Create<ObjectTransformConfiguration>( monitor, cTrans );
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
            _configurationPath = source._configurationPath;
            _condition = condition;
            _transform = transform;
        }

        /// <inheritdoc />
        public string ConfigurationPath => _configurationPath;

        /// <summary>
        /// Gets this processor as a synchronous one if it is a synchronous one.
        /// </summary>
        public ObjectProcessorConfiguration? Synchronous => this as ObjectProcessorConfiguration;

        /// <summary>
        /// Creates an asynchronous processor function.
        /// <para>
        /// This base implementation creates a function based on <see cref="Condition"/> and <see cref="Transform"/>
        /// configurations.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">Services that may be required for some (complex) processors.</param>
        /// <returns>A configured processor function or null for a void processor.</returns>
        public virtual Func<object, ValueTask<object?>>? CreateAsyncProcessor( IActivityMonitor monitor, IServiceProvider services )
        {
            Func<object, ValueTask<bool>>? c = CreateAsyncCondition( monitor, services );
            Func<object, ValueTask<object>>? t = CreateAsyncTransform( monitor, services );
            if( c != null )
            {
                if( t != null )
                {
                    // "Full processor" with its conditonned action.
                    return async o => await c( o ).ConfigureAwait( false ) ? await t( o ).ConfigureAwait( false ) : null;
                }
                // Condition only processor: the action is the identity function.
                return async o => await c( o ).ConfigureAwait( false ) ? o : null;
            }
            // No condition...
            if( t != null )
            {
                // The action is the process
                // (it returns a non null object, that is compatible with a object?: use the bang operator).
                return t!;
            }
            // No condition, no action: void processor.
            return null;
        }

        /// <summary>
        /// Creates a <see cref="IObjectProcessorHook"/> with this configuration and a function obtained by
        /// calling <see cref="CreateAsyncProcessor(IActivityMonitor, IServiceProvider)"/>.
        /// <para>
        /// This should be overridden if this processor relies on other processors in order to hook all of them.
        /// Failing to do so will hide some processors to the evaluation hook.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="hook">The hook context.</param>
        /// <param name="services">The services.</param>
        /// <returns>A wrapper bound to the hook context or null for a void processor.</returns>
        public virtual IObjectProcessorHook? CreateAsyncHook( IActivityMonitor monitor, ProcessorHookContext hook, IServiceProvider services )
        {
            var c = CreateAsyncConditionHook( monitor, hook.ConditionHookContext, services );
            var t = CreateAsyncTransformHook( monitor, hook.TransformHookContext, services );
            return t != null || c != null ? new ObjectAsyncProcessorHook( hook, this, c, t ) : null;
        }

        /// <summary>
        /// Adds a <see cref="PolymorphicConfigurationTypeBuilder.TypeResolver"/> for synchronous <see cref="ObjectAsyncProcessorConfiguration"/>.
        /// <list type="bullet">
        /// <item>The processors must be in the "CK.Object.Processor" namespace.</item>
        /// <item>Their name must end with "AsyncProcessorConfiguration".</item>
        /// </list>
        /// This also calls <see cref="ObjectPredicateConfiguration.AddResolver(PolymorphicConfigurationTypeBuilder, bool, string)"/>
        /// and <see cref="ObjectAsyncTransformConfiguration.AddResolver(PolymorphicConfigurationTypeBuilder, bool, string)"/>.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="allowOtherNamespace">True to allow other namespaces than "CK.Object.Processor" to be specified.</param>
        /// <param name="compositeItemsFieldName">Name of the composite field.</param>
        public static void AddResolver( PolymorphicConfigurationTypeBuilder builder, bool allowOtherNamespace = false, string compositeItemsFieldName = "Processors" )
        {
            // Add the resolvers for Predicates and Transforms.
            ObjectPredicateConfiguration.AddResolver( builder, allowOtherNamespace );
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

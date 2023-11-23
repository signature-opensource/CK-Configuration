using CK.Core;
using System;
using System.Security;
using System.Threading.Tasks;
using static CK.Object.Predicate.ObjectPredicateConfiguration;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Configuration base class for asynchronous predicates.
    /// </summary>
    public abstract class ObjectAsyncPredicateConfiguration : IObjectPredicateConfiguration
    {
        readonly ImmutableConfigurationSection _configuration;

        /// <summary>
        /// Captures the configuration section.
        /// </summary>
        /// <param name="configuration">The configuration serction.</param>
        protected ObjectAsyncPredicateConfiguration( ImmutableConfigurationSection configuration )
        {
            _configuration = configuration;
        }

        /// <inheritdoc />
        public ImmutableConfigurationSection Configuration => _configuration;

        /// <summary>
        /// Creates an asynchronous predicate that requires external services to do its job.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">The services.</param>
        /// <returns>A configured predicate or null for an empty predicate.</returns>
        public abstract Func<object, ValueTask<bool>>? CreatePredicate( IActivityMonitor monitor, IServiceProvider services );

        /// <summary>
        /// Creates a <see cref="ObjectPredicateHook"/> with this configuration and a predicate obtained by
        /// calling <see cref="CreatePredicate(IActivityMonitor, IServiceProvider)"/>.
        /// <para>
        /// This should be overridden if this predicate relies on other predicates in order to hook all of them.
        /// Failing to do so will hide some predicates to the evaluation hook.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="hook">The evaluation hook.</param>
        /// <param name="services">The services.</param>
        /// <returns>A wrapper bound to the evaluation hook or null for an empty predicate.</returns>
        public virtual ObjectAsyncPredicateHook? CreateHook( IActivityMonitor monitor, IPredicateEvaluationHook hook, IServiceProvider services )
        {
            var p = CreatePredicate( monitor, services );
            return p != null ? new ObjectAsyncPredicateHook( hook, this, p ) : null;
        }

        /// <summary>
        /// Creates an asynchronous predicate that doesn't require any external service to do its job.
        /// <see cref="CreatePredicate(IActivityMonitor, IServiceProvider)"/> is called with an empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <returns>A configured predicate or null for an empty predicate.</returns>
        public Func<object, ValueTask<bool>>? CreatePredicate( IActivityMonitor monitor ) => CreatePredicate( monitor, EmptyServiceProvider.Instance );

        /// <summary>
        /// Creates an <see cref="ObjectAsyncPredicateHook"/> that doesn't require any external service to do its job.
        /// <see cref="CreateHook(IActivityMonitor, IPredicateEvaluationHook, IServiceProvider)"/> is called with an
        /// empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="hook">The evaluation hook.</param>
        /// <returns>A configured wrapper bound to the evaluation hook or null for an empty predicate.</returns>
        public ObjectAsyncPredicateHook? CreateHook( IActivityMonitor monitor, IPredicateEvaluationHook hook ) => CreateHook( monitor, hook, EmptyServiceProvider.Instance );

        /// <summary>
        /// Adds a <see cref="PolymorphicConfigurationTypeBuilder.TypeResolver"/> for asynchronous <see cref="ObjectAsyncPredicateConfiguration"/>.
        /// <list type="bullet">
        /// <item>The predicates must be in the "CK.Object.Predicate" namespace (same as the synchronous <see cref="ObjectPredicateConfiguration"/>).</item>
        /// <item>Their name must end with "AsyncPredicateConfiguration".</item>
        /// </list>
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="allowOtherNamespace">True to allow other namespaces than "CK.Object.Predicate" to be specified.</param>
        /// <param name="compositeItemsFieldName">Name of the composite field.</param>
        public static void AddResolver( PolymorphicConfigurationTypeBuilder builder, bool allowOtherNamespace = false, string compositeItemsFieldName = "Predicates" )
        {
            builder.AddStandardTypeResolver( baseType: typeof( ObjectAsyncPredicateConfiguration ),
                                             typeNamespace: "CK.Object.Predicate",
                                             allowOtherNamespace: allowOtherNamespace,
                                             familyTypeNameSuffix: "AsyncPredicate",
                                             tryCreateFromTypeName: TryCreateFromTypeName,
                                             compositeBaseType: typeof( GroupAsyncPredicateConfiguration ),
                                             compositeItemsFieldName: compositeItemsFieldName );

        }


        static object? TryCreateFromTypeName( IActivityMonitor monitor,
                                              string typeName,
                                              PolymorphicConfigurationTypeBuilder builder,
                                              ImmutableConfigurationSection configuration )
        {
            if( typeName.Equals( "true", StringComparison.OrdinalIgnoreCase ) )
            {
                return new AlwaysTrueAsyncPredicateConfiguration( monitor, builder, configuration );
            }
            if( typeName.Equals( "false", StringComparison.OrdinalIgnoreCase ) )
            {
                return new AlwaysFalseAsyncPredicateConfiguration( monitor, builder, configuration );
            }
            if( typeName.Equals( "All", StringComparison.OrdinalIgnoreCase ) )
            {
                var predicates = builder.CreateItems<ObjectAsyncPredicateConfiguration>( monitor, configuration );
                if( predicates == null ) return null;
                WarnUnusedKeys( monitor, configuration );
                return new GroupAsyncPredicateConfiguration( monitor, 0, configuration, predicates );
            }
            if( typeName.Equals( "Any", StringComparison.OrdinalIgnoreCase ) )
            {
                var predicates = builder.CreateItems<ObjectAsyncPredicateConfiguration>( monitor, configuration );
                if( predicates == null ) return null;
                WarnUnusedKeys( monitor, configuration );
                return predicates != null ? new GroupAsyncPredicateConfiguration( monitor, 1, configuration, predicates ) : null;
            }
            return null;
        }

        internal static void WarnUnusedKeys( IActivityMonitor monitor, ImmutableConfigurationSection configuration )
        {
            if( configuration["Any"] != null )
            {
                monitor.Warn( $"Configuration '{configuration.Path}:Any' is ignored." );
            }
            if( configuration["AtLeast"] != null )
            {
                monitor.Warn( $"Configuration '{configuration.Path}:AtLeast' is ignored." );
            }
        }

    }
}

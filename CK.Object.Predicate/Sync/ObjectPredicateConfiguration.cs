using CK.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Immutable;
using System.Security;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Configuration base class for synchronous predicates.
    /// </summary>
    public abstract partial class ObjectPredicateConfiguration : IObjectPredicateConfiguration
    {
        readonly ImmutableConfigurationSection _configuration;

        /// <summary>
        /// Captures the configuration section.
        /// <para>
        /// The required signature constructor for specialized class is
        /// <c>( IActivityMonitor monitor, PolymorphicConfigurationTypeBuilder builder, ImmutableConfigurationSection configuration )</c>.
        /// </para>
        /// </summary>
        /// <param name="configuration">The configuration serction.</param>
        protected ObjectPredicateConfiguration( ImmutableConfigurationSection configuration )
        {
            _configuration = configuration;
        }

        /// <inheritdoc />
        public ImmutableConfigurationSection Configuration => _configuration;

        /// <summary>
        /// Creates a synchronous predicate that requires external services to do its job.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">The services.</param>
        /// <returns>A configured object predicate or null for an empty predicate.</returns>
        public abstract Func<object, bool>? CreatePredicate( IActivityMonitor monitor, IServiceProvider services );

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
        public virtual ObjectPredicateHook? CreateHook( IActivityMonitor monitor, IPredicateEvaluationHook hook, IServiceProvider services )
        {
            var p = CreatePredicate( monitor, services );
            return p != null ? new ObjectPredicateHook( hook, this, p ) : null;
        }

        /// <summary>
        /// Creates a synchronous predicate that doesn't require any external service to do its job.
        /// <see cref="CreatePredicate(IActivityMonitor, IServiceProvider)"/> is called with an empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <returns>A configured object predicate or null for an empty predicate.</returns>
        public Func<object, bool>? CreatePredicate( IActivityMonitor monitor ) => CreatePredicate( monitor, EmptyServiceProvider.Instance );

        internal sealed class EmptyServiceProvider : IServiceProvider
        {
            public static readonly EmptyServiceProvider Instance = new EmptyServiceProvider();
            public object? GetService( Type serviceType ) => null;
        }

        /// <summary>
        /// Creates an <see cref="ObjectPredicateHook"/> that doesn't require any external service to do its job.
        /// <see cref="CreateHook(IActivityMonitor, IPredicateEvaluationHook, IServiceProvider)"/> is called with an
        /// empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="hook">The evaluation hook.</param>
        /// <returns>A wrapper bound to the evaluation hook or null for an empty predicate.</returns>
        public ObjectPredicateHook? CreateHook( IActivityMonitor monitor, IPredicateEvaluationHook hook ) => CreateHook( monitor, hook, EmptyServiceProvider.Instance );

        /// <summary>
        /// Adds a <see cref="PolymorphicConfigurationTypeBuilder.TypeResolver"/> for synchronous <see cref="ObjectPredicateConfiguration"/>.
        /// <list type="bullet">
        /// <item>The predicates must be in the "CK.Object.Predicate" namespace.</item>
        /// <item>Their name must end with "PredicateConfiguration".</item>
        /// </list>
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="allowOtherNamespace">True to allow other namespaces than "CK.Object.Predicate" to be specified.</param>
        /// <param name="compositeItemsFieldName">Name of the composite field.</param>
        public static void AddResolver( PolymorphicConfigurationTypeBuilder builder, bool allowOtherNamespace = false, string compositeItemsFieldName = "Predicates" )
        {
            builder.AddResolver( new PolymorphicConfigurationTypeBuilder.StandardTypeResolver(
                                             baseType: typeof( ObjectPredicateConfiguration ),
                                             typeNamespace: "CK.Object.Predicate",
                                             allowOtherNamespace: allowOtherNamespace,
                                             familyTypeNameSuffix: "Predicate",
                                             tryCreateFromTypeName: TryCreateFromTypeName,
                                             compositeBaseType: typeof( GroupPredicateConfiguration ),
                                             compositeItemsFieldName: compositeItemsFieldName ) );

        }

        static object? TryCreateFromTypeName( IActivityMonitor monitor,
                                              string typeName,
                                              PolymorphicConfigurationTypeBuilder builder,
                                              ImmutableConfigurationSection configuration )
        {
            if( typeName.Equals( "true", StringComparison.OrdinalIgnoreCase ) )
            {
                return new AlwaysTruePredicateConfiguration( monitor, builder, configuration );
            }
            if( typeName.Equals( "false", StringComparison.OrdinalIgnoreCase ) )
            {
                return new AlwaysFalsePredicateConfiguration( monitor, builder, configuration );
            }
            if( typeName.Equals( "All", StringComparison.OrdinalIgnoreCase ) )
            {
                var items = builder.CreateItems<ObjectPredicateConfiguration>( monitor, configuration );
                if( items == null ) return null;
                WarnUnusedKeys( monitor, configuration );
                return new GroupPredicateConfiguration( 0, configuration, items.ToImmutableArray() );
            }
            if( typeName.Equals( "Any", StringComparison.OrdinalIgnoreCase ) )
            {
                var items = builder.CreateItems<ObjectPredicateConfiguration>( monitor, configuration );
                if( items == null ) return null;
                WarnUnusedKeys( monitor, configuration );
                return items != null ? new GroupPredicateConfiguration( 1, configuration, items.ToImmutableArray() ) : null;
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

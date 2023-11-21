using CK.Core;
using System;
using System.Security;

namespace CK.Object.Filter
{
    /// <summary>
    /// Filter configuration base class for synchronous filters.
    /// </summary>
    public abstract class ObjectFilterConfiguration : IObjectFilterConfiguration
    {
        readonly ImmutableConfigurationSection _configuration;

        /// <summary>
        /// Captures the configuration section. The monitor and builder are unused
        /// at this level but this is the standard signature that all configuration
        /// must support.
        /// </summary>
        /// <param name="monitor">The monitor that signals errors or warnings.</param>
        /// <param name="builder">The builder.</param>
        /// <param name="configuration">The configuration serction.</param>
        protected ObjectFilterConfiguration( IActivityMonitor monitor,
                                             PolymorphicConfigurationTypeBuilder builder,
                                             ImmutableConfigurationSection configuration )
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
        /// <returns>A configured object filter.</returns>
        public abstract Func<object, bool> CreatePredicate( IActivityMonitor monitor, IServiceProvider services );

        /// <summary>
        /// Creates a default <see cref="ObjectFilterHook"/> with this configuration and a predicate obtained by
        /// calling <see cref="CreatePredicate(IActivityMonitor, IServiceProvider)"/>.
        /// <para>
        /// This should be overridden if this filter relies on other filters in order to expose all the filters.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">The services.</param>
        /// <returns>A configured filter.</returns>
        public virtual ObjectFilterHook CreateHook( IActivityMonitor monitor, IServiceProvider services )
        {
            return new ObjectFilterHook( this, CreatePredicate( monitor, services ) );
        }

        /// <summary>
        /// Creates a synchronous predicate that doesn't require any external service to do its job.
        /// <see cref="CreatePredicate(IActivityMonitor, IServiceProvider)"/> is called with an empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <returns>A configured object filter.</returns>
        public Func<object, bool> CreatePredicate( IActivityMonitor monitor ) => CreatePredicate( monitor, EmptyServiceProvider.Instance );

        internal sealed class EmptyServiceProvider : IServiceProvider
        {
            public static readonly EmptyServiceProvider Instance = new EmptyServiceProvider();
            public object? GetService( Type serviceType ) => null;
        }

        /// <summary>
        /// Creates an <see cref="ObjectFilterHook"/> that doesn't require any external service to do its job.
        /// <see cref="CreateHook(IActivityMonitor, IServiceProvider)"/> is called with an empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <returns>A configured filter.</returns>
        public ObjectFilterHook CreateHook( IActivityMonitor monitor ) => CreateHook( monitor, EmptyServiceProvider.Instance );


        /// <summary>
        /// Adds a <see cref="PolymorphicConfigurationTypeBuilder.TypeResolver"/> for synchronous <see cref="ObjectFilterConfiguration"/>.
        /// <list type="bullet">
        /// <item>The filters must be in the "CK.Object.Filter" namespace.</item>
        /// <item>Their name must end with "FilterConfiguration".</item>
        /// </list>
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="allowOtherNamespace">True to allow other namespaces than "CK.Object.Filter" to be specified.</param>
        /// <param name="compositeItemsFieldName">Name of the composite field.</param>
        public static void AddResolver( PolymorphicConfigurationTypeBuilder builder, bool allowOtherNamespace = false, string compositeItemsFieldName = "Filters" )
        {
            builder.AddStandardTypeResolver( baseType: typeof( ObjectFilterConfiguration ),
                                             typeNamespace: "CK.Object.Filter",
                                             allowOtherNamespace: allowOtherNamespace,
                                             familyTypeNameSuffix: "Filter",
                                             tryCreateFromTypeName: TryCreateFromTypeName,
                                             compositeBaseType: typeof( GroupFilterConfiguration ),
                                             compositeItemsFieldName: compositeItemsFieldName );

        }

        static object? TryCreateFromTypeName( IActivityMonitor monitor,
                                              string typeName,
                                              PolymorphicConfigurationTypeBuilder builder,
                                              ImmutableConfigurationSection configuration )
        {
            if( typeName.Equals( "true", StringComparison.OrdinalIgnoreCase ) )
            {
                return new AlwaysTrueFilterConfiguration( monitor, builder, configuration );
            }
            if( typeName.Equals( "false", StringComparison.OrdinalIgnoreCase ) )
            {
                return new AlwaysFalseFilterConfiguration( monitor, builder, configuration );
            }
            if( typeName.Equals( "All", StringComparison.OrdinalIgnoreCase ) )
            {
                var items = builder.CreateItems<ObjectFilterConfiguration>( monitor, configuration );
                if( items == null ) return null;
                WarnUnusedKeys( monitor, configuration );
                if( items.Count == 0 ) return new AlwaysTrueFilterConfiguration( monitor, builder, configuration );
                if( items.Count == 1 ) return items[0];
                return new GroupFilterConfiguration( monitor, 0, builder, configuration, items );
            }
            if( typeName.Equals( "Any", StringComparison.OrdinalIgnoreCase ) )
            {
                var items = builder.CreateItems<ObjectFilterConfiguration>( monitor, configuration );
                if( items == null ) return null;
                WarnUnusedKeys( monitor, configuration );
                if( items.Count == 0 ) return new AlwaysFalseFilterConfiguration( monitor, builder, configuration );
                if( items.Count == 1 ) return items[0];
                return items != null ? new GroupFilterConfiguration( monitor, 1, builder, configuration, items ) : null;
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

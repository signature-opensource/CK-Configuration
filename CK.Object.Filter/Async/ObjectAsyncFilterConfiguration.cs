using CK.Core;
using System;
using System.Security;
using System.Threading.Tasks;
using static CK.Object.Filter.ObjectFilterConfiguration;

namespace CK.Object.Filter
{
    /// <summary>
    /// Filter configuration base class for asynchronous filters.
    /// </summary>
    public abstract class ObjectAsyncFilterConfiguration : IObjectFilterConfiguration
    {
        readonly ImmutableConfigurationSection _configuration;

        /// <summary>
        /// Captures the configuration section.
        /// </summary>
        /// <param name="configuration">The configuration serction.</param>
        protected ObjectAsyncFilterConfiguration( ImmutableConfigurationSection configuration )
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
        /// <returns>A configured predicate.</returns>
        public abstract Func<object, ValueTask<bool>> CreatePredicate( IActivityMonitor monitor, IServiceProvider services );

        /// <summary>
        /// Creates a <see cref="ObjectFilterHook"/> with this configuration and a predicate obtained by
        /// calling <see cref="CreatePredicate(IActivityMonitor, IServiceProvider)"/>.
        /// <para>
        /// This should be overridden if this filter relies on other filters in order to hook all the filters.
        /// Failing to do so will hide some predicates to the evaluation hook.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="hook">The evaluation hook.</param>
        /// <param name="services">The services.</param>
        /// <returns>A configured filter hook bound to the evaluation hook.</returns>
        public virtual ObjectAsyncFilterHook CreateHook( IActivityMonitor monitor, EvaluationHook hook, IServiceProvider services )
        {
            return new ObjectAsyncFilterHook( hook, this, CreatePredicate( monitor, services ) );
        }

        /// <summary>
        /// Creates an asynchronous predicate that doesn't require any external service to do its job.
        /// <see cref="CreatePredicate(IActivityMonitor, IServiceProvider)"/> is called with an empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <returns>A configured predicate.</returns>
        public Func<object, ValueTask<bool>> CreatePredicate( IActivityMonitor monitor ) => CreatePredicate( monitor, EmptyServiceProvider.Instance );

        /// <summary>
        /// Creates an <see cref="ObjectAsyncFilterHook"/> that doesn't require any external service to do its job.
        /// <see cref="CreateHook(IActivityMonitor, EvaluationHook, IServiceProvider)"/> is called with an
        /// empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="hook">The evaluation hook.</param>
        /// <returns>A configured filter hook bound to the evaluation hook.</returns>
        public ObjectAsyncFilterHook CreateHook( IActivityMonitor monitor, EvaluationHook hook ) => CreateHook( monitor, hook, EmptyServiceProvider.Instance );

        /// <summary>
        /// Adds a <see cref="PolymorphicConfigurationTypeBuilder.TypeResolver"/> for asynchronous <see cref="ObjectAsyncFilterConfiguration"/>.
        /// <list type="bullet">
        /// <item>The filters must be in the "CK.Object.Filter" namespace (same as the synchronous <see cref="ObjectFilterConfiguration"/>).</item>
        /// <item>Their name must end with "AsyncFilterConfiguration".</item>
        /// </list>
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="allowOtherNamespace">True to allow other namespaces than "CK.Object.Filter" to be specified.</param>
        /// <param name="compositeItemsFieldName">Name of the composite field.</param>
        public static void AddResolver( PolymorphicConfigurationTypeBuilder builder, bool allowOtherNamespace = false, string compositeItemsFieldName = "Filters" )
        {
            builder.AddStandardTypeResolver( baseType: typeof( ObjectAsyncFilterConfiguration ),
                                             typeNamespace: "CK.Object.Filter",
                                             allowOtherNamespace: allowOtherNamespace,
                                             familyTypeNameSuffix: "AsyncFilter",
                                             tryCreateFromTypeName: TryCreateFromTypeName,
                                             compositeBaseType: typeof( GroupAsyncFilterConfiguration ),
                                             compositeItemsFieldName: compositeItemsFieldName );

        }


        static object? TryCreateFromTypeName( IActivityMonitor monitor,
                                              string typeName,
                                              PolymorphicConfigurationTypeBuilder builder,
                                              ImmutableConfigurationSection configuration )
        {
            if( typeName.Equals( "true", StringComparison.OrdinalIgnoreCase ) )
            {
                return new AlwaysTrueAsyncFilterConfiguration( monitor, builder, configuration );
            }
            if( typeName.Equals( "false", StringComparison.OrdinalIgnoreCase ) )
            {
                return new AlwaysFalseAsyncFilterConfiguration( monitor, builder, configuration );
            }
            if( typeName.Equals( "All", StringComparison.OrdinalIgnoreCase ) )
            {
                var items = builder.CreateItems<ObjectAsyncFilterConfiguration>( monitor, configuration );
                if( items == null ) return null;
                WarnUnusedKeys( monitor, configuration );
                if( items.Count == 0 ) return new AlwaysTrueAsyncFilterConfiguration( monitor, builder, configuration );
                if( items.Count == 1 ) return items[0];
                return new GroupAsyncFilterConfiguration( monitor, 0, builder, configuration, items );
            }
            if( typeName.Equals( "Any", StringComparison.OrdinalIgnoreCase ) )
            {
                var items = builder.CreateItems<ObjectAsyncFilterConfiguration>( monitor, configuration );
                if( items == null ) return null;
                WarnUnusedKeys( monitor, configuration );
                if( items.Count == 0 ) return new AlwaysFalseAsyncFilterConfiguration( monitor, builder, configuration );
                if( items.Count == 1 ) return items[0];
                return items != null ? new GroupAsyncFilterConfiguration( monitor, 1, builder, configuration, items ) : null;
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
using CK.Core;
using Microsoft.Extensions.Configuration;

namespace StrategyPlugin
{
    /// <summary>
    /// While the <see cref="IStrategyConfiguration"/> is an interface, this one is
    /// an abstract class (it could also be an interface).
    /// An extensible configuration handles <see cref="PlaceholderStrategyConfiguration"/> configuration replacements.
    /// <para>
    /// This one doesn't capture and expose its <see cref="ImmutableConfigurationSection"/> by default: it is up to
    /// the concrete configurations to do this as needed.
    /// </para>
    /// <para>
    /// This base class does't need to be in the "plugin" namespace because, as an abstract class, it cannot be
    /// explicitely targeted.
    /// </para>
    /// </summary>
    public abstract class ExtensibleStrategyConfiguration
    {
        /// <inheritdoc cref="IStrategyConfiguration.CreateStrategy(IActivityMonitor)"/>
        public abstract IStrategy? CreateStrategy( IActivityMonitor monitor );

        public bool TrySetPlaceholder( IActivityMonitor monitor,
                                       IConfigurationSection configuration,
                                       out ExtensibleStrategyConfiguration? result )
        {
            bool success = true;
            using( monitor.OnError( () => success = false ) )
            {
                result = SetPlaceholder( monitor, configuration );
                if( result == this )
                {
                    monitor.Error( $"Unable to set placeholder: '{configuration.GetParentPath()}' doesn't exist or is not a placeholder." );
                }
            }
            if( !success ) result = null;
            return success;
        }

        /// <summary>
        /// Mutator default implementation: always returns this instance by default.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="configuration">Configuration of the replaced placeholder.</param>
        /// <returns>This, a new configuration, or null to remove this.</returns>
        protected internal virtual ExtensibleStrategyConfiguration? SetPlaceholder( IActivityMonitor monitor, IConfigurationSection configuration )
        {
            return this;
        }

        /// <summary>
        /// Configures a builder to handle this type family.
        /// </summary>
        /// <param name="builder">A builder to configure.</param>
        public static void Configure( PolymorphicConfigurationTypeBuilder builder )
        {
            builder.AddStandardTypeResolver( baseType: typeof( ExtensibleStrategyConfiguration ),
                                             fieldName: "Type",
                                             typeNamespace: "Plugin.Strategy",
                                             allowOtherNamespace: false,
                                             familyTypeNameSuffix: "Strategy",
                                             compositeBaseType: typeof( Plugin.Strategy.ExtensibleCompositeStrategyConfiguration ),
                                             compositeItemsFieldName: "Strategies" );
        }

    }
}

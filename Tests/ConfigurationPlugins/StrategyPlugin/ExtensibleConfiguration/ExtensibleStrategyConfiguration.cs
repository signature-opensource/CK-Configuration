using CK.Core;
using Microsoft.Extensions.Configuration;

namespace StrategyPlugin
{
    /// <summary>
    /// While the <see cref="IStrategyConfiguration"/> is an interface, this one is
    /// an abstract class (it could also be an interface).
    /// An extensible configuration handles <see cref="ExtensibleStrategyConfiguration"/> configuration replacements.
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

        /// <summary>
        /// Tries to replace a <see cref="Plugin.Strategy.PlaceholderStrategyConfiguration"/>.
        /// <para>
        /// The <paramref name="configuration"/>.Path must be a direct child of the placeholder to replace.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="configuration">The configuration that should replace a placeholder.</param>
        /// <returns>A new configuration or null if an error occurred or the placeholder was not found.</returns>
        public ExtensibleStrategyConfiguration? TrySetPlaceholder( IActivityMonitor monitor,
                                                                   IConfigurationSection configuration )
        {
            return TrySetPlaceholder( monitor, configuration, out var _ );
        }

        /// <summary>
        /// Tries to replace a <see cref="Plugin.Strategy.PlaceholderStrategyConfiguration"/>.
        /// <para>
        /// The <paramref name="configuration"/>.Path must be a direct child of the placeholder to replace.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="configuration">The configuration that should replace a placeholder.</param>
        /// <param name="builderError">True if an error occurred while building the configuration, false if the placeholder was not found.</param>
        /// <returns>A new configuration or null if a <paramref name="builderError"/> occurred or the placeholder was not found.</returns>
        public ExtensibleStrategyConfiguration? TrySetPlaceholder( IActivityMonitor monitor,
                                                                   IConfigurationSection configuration,
                                                                   out bool builderError )
        {
            builderError = false;
            ExtensibleStrategyConfiguration? result = null;
            var buildError = false;
            using( monitor.OnError( () => buildError = true ) )
            {
                result = SetPlaceholder( monitor, configuration );
            }
            if( !buildError && result == this )
            {
                monitor.Error( $"Unable to set placeholder: '{configuration.GetParentPath()}' " +
                               $"doesn't exist or is not a placeholder." );
                return null;
            }
            return (builderError = buildError) ? null : result;
        }

        /// <summary>
        /// Mutator default implementation: always returns this instance by default.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="configuration">Configuration of the replaced placeholder.</param>
        /// <returns>A new configuration or this instance if an error occurred or the placeholder has not been found.</returns>
        public virtual ExtensibleStrategyConfiguration SetPlaceholder( IActivityMonitor monitor, IConfigurationSection configuration )
        {
            return this;
        }

        /// <summary>
        /// Configures a builder to handle this type family.
        /// </summary>
        /// <param name="builder">A builder to configure.</param>
        public static void AddResolver( PolymorphicConfigurationTypeBuilder builder )
        {
            builder.AddResolver( new PolymorphicConfigurationTypeBuilder.StandardTypeResolver(
                                        baseType: typeof( ExtensibleStrategyConfiguration ),
                                        typeNamespace: "Plugin.Strategy",
                                        allowOtherNamespace: false,
                                        familyTypeNameSuffix: "Strategy",
                                        compositeBaseType: typeof( Plugin.Strategy.ExtensibleCompositeStrategyConfiguration ),
                                        compositeItemsFieldName: "Strategies" ) );
        }

    }
}

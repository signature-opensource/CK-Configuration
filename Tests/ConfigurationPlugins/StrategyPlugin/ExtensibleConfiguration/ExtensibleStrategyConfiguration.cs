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
    public abstract class ExtensibleStrategyConfiguration : ISupportConfigurationPlaceholder<ExtensibleStrategyConfiguration>
    {
        /// <inheritdoc cref="IStrategyConfiguration.CreateStrategy(IActivityMonitor)"/>
        public abstract IStrategy? CreateStrategy( IActivityMonitor monitor );

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
        public static void AddResolver( TypedConfigurationBuilder builder )
        {
            builder.AddResolver( new TypedConfigurationBuilder.StandardTypeResolver(
                                        baseType: typeof( ExtensibleStrategyConfiguration ),
                                        typeNamespace: "Plugin.Strategy",
                                        allowOtherNamespace: false,
                                        familyTypeNameSuffix: "Strategy",
                                        defaultCompositeBaseType: typeof( Plugin.Strategy.ExtensibleCompositeStrategyConfiguration ),
                                        compositeItemsFieldName: "Strategies" ) );
        }

    }
}

using CK.Core;

namespace StrategyPlugin
{
    /// <summary>
    /// Typical base pattern for a polymorphic configured components: the configuration is
    /// the factory of the configured component.
    /// <para>
    /// An instance constructor must exist with the following signature:
    /// <c>( IActivityMonitor monitor, PolymorphicConfigurationTypeBuilder builder, ImmutableConfigurationSection configuration )</c>.
    /// </para>
    /// </summary>
    public interface IStrategyConfiguration
    {
        /// <summary>
        /// Gets the configuration section.
        /// Exposing this is totally optional, only the factory method is required.
        /// </summary>
        ImmutableConfigurationSection Configuration { get; }

        /// <summary>
        /// Creates a strategy object. A null return is not necessarily an error (errors
        /// should be handled via the monitor - see <see cref="ActivityMonitorExtension.OnError(IActivityMonitor, Action)"/>
        /// for instance). A null return must be ignored: the configuration is "disabled", "non applicable", or is a "placeholder".
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <returns>A strategy or null on error or if, for any reason, no strategy must be created from this configuration.</returns>
        IStrategy? CreateStrategy( IActivityMonitor monitor );

        /// <summary>
        /// Configures a builder to handle this type family (without <see cref="CompositeStrategyConfiguration"/>).
        /// </summary>
        /// <param name="builder">A builder to configure.</param>
        public static void ConfigureWithoutComposite( PolymorphicConfigurationTypeBuilder builder )
        {
            builder.AddStandardTypeResolver( baseType: typeof( IStrategyConfiguration ),
                                             fieldName: "Type",
                                             typeNamespace: "Plugin.Strategy",
                                             allowOtherNamespace: false,
                                             familyTypeNameSuffix: "Strategy" );
        }

        /// <summary>
        /// Configures a builder to handle this type family (with composite).
        /// </summary>
        /// <param name="builder">A builder to configure.</param>
        public static void Configure( PolymorphicConfigurationTypeBuilder builder )
        {
            builder.AddStandardTypeResolver( baseType: typeof( IStrategyConfiguration ),
                                             fieldName: "Type",
                                             typeNamespace: "Plugin.Strategy",
                                             allowOtherNamespace: false,
                                             familyTypeNameSuffix: "Strategy",
                                             compositeBaseType: typeof( CompositeStrategyConfiguration ),
                                             compositeItemsFieldName: "Strategies" );
        }
    }
}

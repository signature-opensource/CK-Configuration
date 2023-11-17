using CK.Core;
using Microsoft.Extensions.Configuration;
using StrategyPlugin;

namespace Plugin.Strategy
{
    /// <summary>
    /// Confiration object that is here to be replaced by a real configuration.
    /// <para>
    /// This must be in the "plugin" namespace.
    /// </para>
    /// </summary>
    public class PlaceholderStrategyConfiguration : ExtensibleStrategyConfiguration
    {
        readonly AssemblyConfiguration _assemblies;
        readonly ImmutableConfigurationSection _configuration;

        public PlaceholderStrategyConfiguration( IActivityMonitor monitor,
                                                 PolymorphicConfigurationTypeBuilder builder,
                                                 ImmutableConfigurationSection configuration )
        {
            _assemblies = builder.AssemblyConfiguration;
            _configuration = configuration;
        }

        /// <summary>
        /// A placeholder obviously generates no strategy.
        /// </summary>
        public override IStrategy? CreateStrategy( IActivityMonitor monitor ) => null;


        protected internal override ExtensibleStrategyConfiguration? SetPlaceholder( IActivityMonitor monitor,
                                                                                     IConfigurationSection configuration )
        {
            if( configuration.HasExactParentPath( _configuration.Path ) )
            {
                var builder = new PolymorphicConfigurationTypeBuilder( _assemblies );
                ExtensibleStrategyConfiguration.Configure( builder );
                // Anchors the new configuration under this one.
                var config = new ImmutableConfigurationSection( configuration, _configuration );
                var newC = builder.Create<ExtensibleStrategyConfiguration>( monitor, config );
                // We choose here to keep the placeholder on error (by returning this).
                // Returning newC here removes the placeholder.
                if( newC != null ) return newC;
            }
            return this;
        }
    }
}

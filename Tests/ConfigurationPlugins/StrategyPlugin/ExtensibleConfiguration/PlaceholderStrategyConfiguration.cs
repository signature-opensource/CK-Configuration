using CK.Core;
using Microsoft.Extensions.Configuration;
using StrategyPlugin;

namespace Plugin.Strategy
{
    /// <summary>
    /// Configuration object that is here to be replaced by a real configuration.
    /// <para>
    /// This must be in the "plugin" namespace.
    /// </para>
    /// </summary>
    public sealed class PlaceholderStrategyConfiguration : ExtensibleStrategyConfiguration
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


        public override ExtensibleStrategyConfiguration SetPlaceholder( IActivityMonitor monitor,
                                                                        IConfigurationSection configuration )
        {
            if( configuration.GetParentPath().Equals( _configuration.Path, StringComparison.OrdinalIgnoreCase ) )
            {
                var builder = new PolymorphicConfigurationTypeBuilder( _assemblies );
                ExtensibleStrategyConfiguration.AddResolver( builder );
                if( configuration is not ImmutableConfigurationSection config )
                {
                    config = new ImmutableConfigurationSection( configuration, lookupParent: _configuration );
                }
                var newC = builder.Create<ExtensibleStrategyConfiguration>( monitor, config );
                if( newC != null ) return newC;
            }
            return this;
        }
    }
}

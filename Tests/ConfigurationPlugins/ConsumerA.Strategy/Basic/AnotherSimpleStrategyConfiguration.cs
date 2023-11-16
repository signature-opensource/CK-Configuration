using CK.Core;
using StrategyPlugin;

namespace Plugin.Strategy
{
    public class AnotherSimpleStrategyConfiguration : IStrategyConfiguration
    {
        readonly ImmutableConfigurationSection _configuration;

        public AnotherSimpleStrategyConfiguration( IActivityMonitor monitor,
                                                   PolymorphicConfigurationTypeBuilder builder,
                                                   ImmutableConfigurationSection configuration )
        {
            _configuration = configuration;
            switch( _configuration["ConfigurationAction"] )
            {
                case "Error": monitor.Error( "AnotherSimpleStrategyConfiguration emits an error." ); break;
                case "Warn": monitor.Error( "AnotherSimpleStrategyConfiguration emits a warning." ); break;
            }
        }

        public ImmutableConfigurationSection Configuration => _configuration;

        public IStrategy CreateStrategy( IActivityMonitor monitor )
        {
            return new AnotherSimpleStrategy( this );
        }
    }
}

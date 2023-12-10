using CK.Core;
using StrategyPlugin;

namespace Plugin.Strategy
{
    public class SimpleStrategyConfiguration : IStrategyConfiguration
    {
        readonly ImmutableConfigurationSection _configuration;
        readonly string _action;

        public SimpleStrategyConfiguration( IActivityMonitor monitor,
                                    TypedConfigurationBuilder builder,
                                    ImmutableConfigurationSection configuration )
        {
            _configuration = configuration;
            switch( _configuration["ConfigurationAction"] )
            {
                case "Trrow": Throw.Exception( "SimpleStrategyConfiguration throws." ); break;
                case "Error": monitor.Error( "SimpleStrategyConfiguration emits an error." ); break;
                case "Warn": monitor.Error( "SimpleStrategyConfiguration emits a warning." ); break;
            }
            _action = configuration["Action"] ?? "";
        }

        public ImmutableConfigurationSection Configuration => _configuration;

        public string Action => _action;

        public IStrategy? CreateStrategy( IActivityMonitor monitor )
        {
            return new ConsumerA.SimpleStrategy( this );
        }
    }
}

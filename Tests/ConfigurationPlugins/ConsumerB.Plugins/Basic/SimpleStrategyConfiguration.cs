using CK.Core;
using StrategyPlugin;

namespace Plugin.Strategy;


public class SimpleStrategyConfiguration : IStrategyConfiguration
{
    readonly ImmutableConfigurationSection _configuration;
    readonly string _action;

    public SimpleStrategyConfiguration( IActivityMonitor monitor,
                                        TypedConfigurationBuilder builder,
                                        ImmutableConfigurationSection configuration )
    {
        _configuration = configuration;
        _action = configuration["Action"] ?? "";
    }

    public ImmutableConfigurationSection Configuration => _configuration;

    public string Action => _action;

    public IStrategy CreateStrategy( IActivityMonitor monitor )
    {
        return new ConsumerB.SimpleStrategy( this );
    }
}

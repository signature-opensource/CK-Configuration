using CK.Core;
using ConsumerB;
using StrategyPlugin;

namespace Plugin.Strategy
{
    public class ESimpleStrategyConfiguration : ExtensibleStrategyConfiguration
    {
        public ESimpleStrategyConfiguration( IActivityMonitor monitor,
                                             TypedConfigurationBuilder builder,
                                             ImmutableConfigurationSection configuration )
        {
        }

        public override IStrategy? CreateStrategy( IActivityMonitor monitor )
        {
            return new ESimpleStrategy( this );
        }
    }
}

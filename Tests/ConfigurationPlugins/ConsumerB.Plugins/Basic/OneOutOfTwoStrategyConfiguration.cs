using CK.Core;
using StrategyPlugin;
using System.Collections.Generic;

namespace Plugin.Strategy
{
    public class OneOutOfTwoStrategyConfiguration : CompositeStrategyConfiguration
    {
        readonly bool _halfRun;

        public OneOutOfTwoStrategyConfiguration( IActivityMonitor monitor,
                                                 PolymorphicConfigurationTypeBuilder builder,
                                                 ImmutableConfigurationSection configuration,
                                                 IReadOnlyList<IStrategyConfiguration> items )
            : base( monitor, builder, configuration, items )
        {
            _halfRun = configuration.TryGetBooleanValue( monitor, "HalfRun" ) ?? true;
        }

        public override IStrategy? CreateStrategy( IActivityMonitor monitor )
        {
            var items = CreateStrategyItems( monitor );
            return items != null
                    ? new ConsumerB.OneOutOfTwoStrategy( Configuration.Path, items, _halfRun )
                    : null;
        }
    }
}

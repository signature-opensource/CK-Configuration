using CK.Core;
using StrategyPlugin;

namespace Plugin.Strategy
{
    public class AnotherSimpleStrategy : IStrategy
    {
        internal AnotherSimpleStrategy( AnotherSimpleStrategyConfiguration configuration )
        {
        }

        public int DoSomething( IActivityMonitor monitor, int payload )
        {
            monitor.Info( $"AnotherSimple processes {payload}." );
            return ++payload;
        }
    }
}

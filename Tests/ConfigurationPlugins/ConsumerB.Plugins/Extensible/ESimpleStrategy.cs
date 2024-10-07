using CK.Core;
using Plugin.Strategy;
using StrategyPlugin;

namespace ConsumerB;

public class ESimpleStrategy : IStrategy
{
    internal ESimpleStrategy( ESimpleStrategyConfiguration configuration )
    {
    }

    public int DoSomething( IActivityMonitor monitor, int payload )
    {
        return ++payload;
    }
}

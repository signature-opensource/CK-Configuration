using CK.Core;
using Plugin.Strategy;
using StrategyPlugin;

namespace ConsumerA;

public class SimpleStrategy : IStrategy
{
    readonly string _action;

    internal SimpleStrategy( SimpleStrategyConfiguration configuration )
    {
        _action = configuration.Action;
    }

    public int DoSomething( IActivityMonitor monitor, int payload )
    {
        monitor.Info( $"Simple processes: {_action}." );
        return ++payload;
    }
}

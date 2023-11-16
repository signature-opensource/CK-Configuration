using CK.Core;
using StrategyPlugin;
using System.Collections.Immutable;

namespace ConsumerB
{
    public class OneOutOfTwoStrategy : CompositeStrategy
    {
        readonly bool _halfRun;

        protected internal OneOutOfTwoStrategy( string path, ImmutableArray<IStrategy> items, bool halfRun )
            : base( path, items )
        {
            _halfRun = halfRun;
        }

        public override int DoSomething( IActivityMonitor monitor, int payload )
        {
            if( !_halfRun ) return base.DoSomething( monitor, payload );
            for( int i = 0; i < Strategies.Length; ++i )
            {
                if( (i % 2) == 0 )
                {
                    payload = Strategies[i].DoSomething( monitor, payload );
                }
            }
            return payload;
        }
    }
}

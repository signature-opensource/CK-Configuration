using CK.Core;
using System.Collections.Immutable;

namespace StrategyPlugin
{
    /// <summary>
    /// Base composite strategy implementationis a default composite.
    /// This can be sealed but making it specializable enables slightly different
    /// composite implementations to coexist with the default one.
    /// </summary>
    public class CompositeStrategy : IStrategy
    {
        readonly string _path;
        readonly ImmutableArray<IStrategy> _items;

        /// <summary>
        /// This implementation choose to capture the configuration's path for logging.
        /// Only the items are really required.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="items"></param>
        public CompositeStrategy( string path, ImmutableArray<IStrategy> items )
        {
            _path = path;
            _items = items;
        }

        /// <summary>
        /// Gives specializations an access to the items.
        /// </summary>
        protected ImmutableArray<IStrategy> Strategies => _items;

        public virtual int DoSomething( IActivityMonitor monitor, int payload )
        {
            using( monitor.OpenInfo( $"Executing '{_path}'." ) )
            {
                foreach( var item in _items )
                {
                    payload = item.DoSomething( monitor, payload );
                }
            }
            return payload;
        }
    }
}

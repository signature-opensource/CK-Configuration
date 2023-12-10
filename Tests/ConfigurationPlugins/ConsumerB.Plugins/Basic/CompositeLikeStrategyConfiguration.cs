using CK.Core;
using Microsoft.Extensions.Configuration;
using StrategyPlugin;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Plugin.Strategy
{
    public class CompositeLikeStrategyConfiguration : IStrategyConfiguration
    {
        readonly ImmutableConfigurationSection _configuration;
        readonly CompositeStrategyConfiguration _before;
        readonly CompositeStrategyConfiguration _after;

        public CompositeLikeStrategyConfiguration( IActivityMonitor monitor,
                                                   TypedConfigurationBuilder builder,
                                                   ImmutableConfigurationSection configuration )
        {
            _configuration = configuration;
            _before = builder.Create<CompositeStrategyConfiguration>( monitor, configuration.GetRequiredSection( "Before" ) )!;
            _after = builder.Create<CompositeStrategyConfiguration>( monitor, configuration.GetRequiredSection( "After" ) )!;
        }

        public ImmutableConfigurationSection Configuration => _configuration;

        public IStrategy? CreateStrategy( IActivityMonitor monitor )
        {
            var b = _before.CreateStrategy( monitor );
            var a = _after.CreateStrategy( monitor );
            IEnumerable<IStrategy>? items;
            if( a != null ) items = b != null ? new[] { a, b } : new[] { a };
            else if( b != null ) items = new[] { b };
            else return null;
            return new CompositeStrategy( _configuration.Path, items.ToImmutableArray() );
        }
    }
}

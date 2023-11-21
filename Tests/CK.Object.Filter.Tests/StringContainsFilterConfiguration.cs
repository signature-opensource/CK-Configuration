using CK.Core;
using System;

namespace CK.Object.Filter
{
    public sealed class StringContainsFilterConfiguration : ObjectFilterConfiguration
    {
        readonly string _content;

        public StringContainsFilterConfiguration( IActivityMonitor monitor, PolymorphicConfigurationTypeBuilder builder, ImmutableConfigurationSection configuration )
            : base( monitor, builder, configuration )
        {
            var c = configuration["Content"];
            if( c == null )
            {
                monitor.Error( $"Missing '{configuration.Path}:Content' value." );
            }
            _content = c!;
        }

        public override Func<object, bool> CreatePredicate( IActivityMonitor monitor, IServiceProvider services )
        {
            return o => o is string s && s.Contains( _content );
        }
    }
}

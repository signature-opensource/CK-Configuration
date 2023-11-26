using CK.Core;
using System;

namespace CK.Object.Predicate
{
    public sealed class StringContainsPredicateConfiguration : ObjectPredicateConfiguration
    {
        readonly string _content;

        public StringContainsPredicateConfiguration( IActivityMonitor monitor, PolymorphicConfigurationTypeBuilder builder, ImmutableConfigurationSection configuration )
            : base( configuration )
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
using CK.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.Object.Filter
{
    public sealed class EnumerableMaxCountFilterConfiguration : ObjectFilterConfiguration
    {
        readonly int _maxCount;

        public EnumerableMaxCountFilterConfiguration( IActivityMonitor monitor, PolymorphicConfigurationTypeBuilder builder, ImmutableConfigurationSection configuration )
            : base( monitor, builder, configuration )
        {
            var c = configuration.TryGetIntValue( monitor, "MaxCount" );
            if( !c.HasValue )
            {
                monitor.Error( $"Missing '{configuration.Path}:MaxCount' value." );
            }
            else _maxCount = c.Value;
        }

        public override Func<object, bool> CreatePredicate( IActivityMonitor monitor, IServiceProvider services )
        {
            return o => DoContains( o, _maxCount );
        }

        static bool DoContains( object o, int maxCount )
        {
            if( o is System.Collections.IEnumerable e )
            {
                int c = 0;
                foreach( var item in e )
                {
                    if( ++c == maxCount )
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }
}

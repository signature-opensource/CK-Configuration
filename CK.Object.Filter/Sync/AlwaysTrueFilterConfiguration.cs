using CK.Core;
using System;

namespace CK.Object.Filter
{
    /// <summary>
    /// Simple always true filter.
    /// </summary>
    public sealed class AlwaysTrueFilterConfiguration : ObjectFilterConfiguration
    {
        public AlwaysTrueFilterConfiguration( IActivityMonitor monitor,
                                              PolymorphicConfigurationTypeBuilder builder,
                                              ImmutableConfigurationSection configuration )
            : base( monitor, builder, configuration )
        {
        }

        public override Func<object, bool> CreatePredicate( IActivityMonitor monitor, IServiceProvider services )
        {
            return static _ => true;
        }

    }
}

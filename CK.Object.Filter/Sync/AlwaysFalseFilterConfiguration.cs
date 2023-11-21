using CK.Core;
using System;

namespace CK.Object.Filter
{
    /// <summary>
    /// Simple always false filter.
    /// </summary>
    public sealed class AlwaysFalseFilterConfiguration : ObjectFilterConfiguration
    {
        public AlwaysFalseFilterConfiguration( IActivityMonitor monitor,
                                              PolymorphicConfigurationTypeBuilder builder,
                                              ImmutableConfigurationSection configuration )
            : base( monitor, builder, configuration )
        {
        }

        public override Func<object, bool> CreatePredicate( IActivityMonitor monitor, IServiceProvider services )
        {
            return static _ => false;
        }
    }
}

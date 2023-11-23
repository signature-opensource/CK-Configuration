using CK.Core;
using System;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Simple always true object predicate.
    /// </summary>
    public sealed class AlwaysTruePredicateConfiguration : ObjectPredicateConfiguration
    {
        public AlwaysTruePredicateConfiguration( IActivityMonitor monitor,
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

using CK.Core;
using System;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Simple always false object predicate.
    /// </summary>
    public sealed class AlwaysFalsePredicateConfiguration : ObjectPredicateConfiguration
    {
        public AlwaysFalsePredicateConfiguration( IActivityMonitor monitor,
                                                  PolymorphicConfigurationTypeBuilder builder,
                                                  ImmutableConfigurationSection configuration )
            : base( configuration )
        {
        }

        public override Func<object, bool> CreatePredicate( IActivityMonitor monitor, IServiceProvider services )
        {
            return static _ => false;
        }
    }
}

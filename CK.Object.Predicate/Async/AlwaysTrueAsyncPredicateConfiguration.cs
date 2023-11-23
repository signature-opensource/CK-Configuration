using CK.Core;
using System;
using System.Threading.Tasks;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Simple always true asynchronous object predicate.
    /// </summary>
    public sealed class AlwaysTrueAsyncPredicateConfiguration : ObjectAsyncPredicateConfiguration
    {
        public AlwaysTrueAsyncPredicateConfiguration( IActivityMonitor monitor,
                                                      PolymorphicConfigurationTypeBuilder builder,
                                                      ImmutableConfigurationSection configuration )
            : base( configuration )
        {
        }

        public override Func<object, ValueTask<bool>> CreatePredicate( IActivityMonitor monitor, IServiceProvider services )
        {
            return static _ => ValueTask.FromResult( true );
        }
    }

}

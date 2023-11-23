using CK.Core;
using System;
using System.Threading.Tasks;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Simple always false asynchronous predicate.
    /// </summary>
    public sealed class AlwaysFalseAsyncPredicateConfiguration : ObjectAsyncPredicateConfiguration
    {
        public AlwaysFalseAsyncPredicateConfiguration( IActivityMonitor monitor,
                                                       PolymorphicConfigurationTypeBuilder builder,
                                                       ImmutableConfigurationSection configuration )
            : base( configuration )
        {
        }

        public override Func<object, ValueTask<bool>> CreatePredicate( IActivityMonitor monitor, IServiceProvider services )
        {
            return static _ => ValueTask.FromResult( false );
        }
    }
}

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
        /// <summary>
        /// Required constructor.
        /// </summary>
        /// <param name="monitor">Unused monitor.</param>
        /// <param name="builder">Unused builder.</param>
        /// <param name="configuration">Captured configuration.</param>
        public AlwaysFalseAsyncPredicateConfiguration( IActivityMonitor monitor,
                                                       PolymorphicConfigurationTypeBuilder builder,
                                                       ImmutableConfigurationSection configuration )
            : base( configuration )
        {
        }

        public override Func<object, ValueTask<bool>> CreateAsyncPredicate( IActivityMonitor monitor, IServiceProvider services )
        {
            return static _ => ValueTask.FromResult( false );
        }
    }
}

using CK.Core;
using System;
using System.Threading.Tasks;

namespace CK.Object.Filter
{
    /// <summary>
    /// Simple always true asynchronous filter.
    /// </summary>
    public sealed class AlwaysTrueAsyncFilterConfiguration : ObjectAsyncFilterConfiguration
    {
        public AlwaysTrueAsyncFilterConfiguration( IActivityMonitor monitor,
                                                   PolymorphicConfigurationTypeBuilder builder,
                                                   ImmutableConfigurationSection configuration )
            : base( monitor, builder, configuration )
        {
        }

        public override Func<object, ValueTask<bool>> CreatePredicate( IActivityMonitor monitor, IServiceProvider services )
        {
            return static _ => ValueTask.FromResult( true );
        }
    }

}

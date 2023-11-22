using CK.Core;

namespace CK.Object.Filter
{
    public sealed class EnumerableMaxCountAsyncFilterConfiguration : AsyncFilterAdapterConfiguration<EnumerableMaxCountFilterConfiguration>
    {
        public EnumerableMaxCountAsyncFilterConfiguration( IActivityMonitor monitor, PolymorphicConfigurationTypeBuilder builder, ImmutableConfigurationSection configuration )
            : base( new EnumerableMaxCountFilterConfiguration( monitor, builder, configuration ) )
        {
        }
    }
}

using CK.Core;

namespace CK.Object.Filter
{
    public sealed class StringContainsAsyncFilterConfiguration : AsyncFilterAdapterConfiguration<StringContainsFilterConfiguration>
    {
        public StringContainsAsyncFilterConfiguration( IActivityMonitor monitor, PolymorphicConfigurationTypeBuilder builder, ImmutableConfigurationSection configuration )
            : base( new StringContainsFilterConfiguration( monitor, builder, configuration ) )
        {
        }
    }
}

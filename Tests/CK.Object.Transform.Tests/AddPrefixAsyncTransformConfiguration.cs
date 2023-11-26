using CK.Core;

namespace CK.Object.Transform
{
    public sealed class AddPrefixAsyncTransformConfiguration : AsyncTransformAdapterConfiguration<AddPrefixTransformConfiguration>
    {
        public AddPrefixAsyncTransformConfiguration( IActivityMonitor monitor,
                                                     PolymorphicConfigurationTypeBuilder builder,
                                                     ImmutableConfigurationSection configuration )
            : base( new AddPrefixTransformConfiguration( monitor, builder, configuration ) )
        {
        }
    }
}

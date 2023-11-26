using CK.Core;

namespace CK.Object.Transform
{
    public sealed class AddSuffixAsyncTransformConfiguration : AsyncTransformAdapterConfiguration<AddSuffixTransformConfiguration>
    {
        public AddSuffixAsyncTransformConfiguration( IActivityMonitor monitor,
                                                     PolymorphicConfigurationTypeBuilder builder,
                                                     ImmutableConfigurationSection configuration )
            : base( new AddSuffixTransformConfiguration( monitor, builder, configuration ) )
        {
        }
    }
}

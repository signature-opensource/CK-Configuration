using CK.Core;

namespace CK.Object.Transform
{
    public sealed partial class ToStringAsyncTransformConfiguration : AsyncTransformAdapterConfiguration<ToStringTransformConfiguration>
    {
        public ToStringAsyncTransformConfiguration( IActivityMonitor monitor,
                                                    PolymorphicConfigurationTypeBuilder builder,
                                                    ImmutableConfigurationSection configuration )
            : base( new ToStringTransformConfiguration( monitor, builder, configuration ) )
        {
        }
    }

}


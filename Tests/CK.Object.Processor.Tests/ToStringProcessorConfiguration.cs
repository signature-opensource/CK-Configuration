using CK.Core;
using System;
using System.Globalization;

namespace CK.Object.Transform
{
    public sealed class ToStringTransformConfiguration : ObjectTransformConfiguration
    {
        public ToStringTransformConfiguration( IActivityMonitor monitor,
                                               PolymorphicConfigurationTypeBuilder builder,
                                               ImmutableConfigurationSection configuration )
            : base( configuration.Path )
        {
        }

        public override Func<object, object>? CreateTransform( IActivityMonitor monitor, IServiceProvider services )
        {
            return static o => Convert.ToString( o, CultureInfo.InvariantCulture ) ?? string.Empty;
        }

    }
}

using CK.Core;
using System;

namespace CK.Object.Transform
{
    public sealed partial class ToStringTransformConfiguration : ObjectTransformConfiguration
    {
        public ToStringTransformConfiguration( IActivityMonitor monitor,
                                               PolymorphicConfigurationTypeBuilder builder,
                                               ImmutableConfigurationSection configuration )
            : base( configuration )
        {
        }

        public override Func<object, object>? CreateTransform( IActivityMonitor monitor, IServiceProvider services )
        {
            return static o => o.ToString() ?? "<null>";
        }
    }

}


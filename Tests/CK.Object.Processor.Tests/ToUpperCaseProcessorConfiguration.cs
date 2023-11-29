using CK.Core;
using System;

namespace CK.Object.Processor
{
    public sealed class ToUpperCaseProcessorConfiguration : ObjectProcessorConfiguration
    {
        public ToUpperCaseProcessorConfiguration( IActivityMonitor monitor,
                                                  PolymorphicConfigurationTypeBuilder builder,
                                                  ImmutableConfigurationSection configuration )
            : base( monitor, builder, configuration )
        {
        }

        protected override Func<object, bool>? CreateIntrinsicCondition( IActivityMonitor monitor, IServiceProvider services )
        {
            return static o => o is string;
        }

        protected override Func<object, object>? CreateIntrinsicTransform( IActivityMonitor monitor, IServiceProvider services )
        {
            return static o => ((string)o).ToUpperInvariant();
        }
    }
}

using CK.Core;
using System;

namespace CK.Object.Processor
{
    public sealed class NegateDoubleProcessorConfiguration : ObjectProcessorConfiguration
    {
        public NegateDoubleProcessorConfiguration( IActivityMonitor monitor,
                                                   PolymorphicConfigurationTypeBuilder builder,
                                                   ImmutableConfigurationSection configuration )
            : base( monitor, builder, configuration )
        {
        }

        protected override Func<object, bool>? CreateIntrinsicCondition( IActivityMonitor monitor, IServiceProvider services )
        {
            return static o => o is double;
        }

        protected override Func<object, object>? CreateIntrinsicTransform( IActivityMonitor monitor, IServiceProvider services )
        {
            return static o => -((double)o);
        }
    }

}

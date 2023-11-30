using CK.Core;

namespace CK.Object.Predicate
{
    public sealed class IsDoublePredicateConfiguration : IsTypePredicateConfiguration<double>
    {
        public IsDoublePredicateConfiguration( IActivityMonitor monitor, PolymorphicConfigurationTypeBuilder builder, ImmutableConfigurationSection configuration )
            : base( monitor, builder, configuration )
        {
        }
    }

    public sealed class IsDoubleAsyncPredicateConfiguration : IsTypeAsyncPredicateConfiguration<double>
    {
        public IsDoubleAsyncPredicateConfiguration( IActivityMonitor monitor, PolymorphicConfigurationTypeBuilder builder, ImmutableConfigurationSection configuration )
            : base( monitor, builder, configuration )
        {
        }
    }
}
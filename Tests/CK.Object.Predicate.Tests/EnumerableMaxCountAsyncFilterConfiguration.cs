using CK.Core;

namespace CK.Object.Predicate
{
    public sealed class EnumerableMaxCountAsyncPredicateConfiguration : AsyncPredicateAdapterConfiguration<EnumerableMaxCountPredicateConfiguration>
    {
        public EnumerableMaxCountAsyncPredicateConfiguration( IActivityMonitor monitor, PolymorphicConfigurationTypeBuilder builder, ImmutableConfigurationSection configuration )
            : base( new EnumerableMaxCountPredicateConfiguration( monitor, builder, configuration ) )
        {
        }
    }
}

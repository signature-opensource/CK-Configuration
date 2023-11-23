using CK.Core;

namespace CK.Object.Predicate
{
    public sealed class StringContainsAsyncPredicateConfiguration : AsyncPredicateAdapterConfiguration<StringContainsPredicateConfiguration>
    {
        public StringContainsAsyncPredicateConfiguration( IActivityMonitor monitor, PolymorphicConfigurationTypeBuilder builder, ImmutableConfigurationSection configuration )
            : base( new StringContainsPredicateConfiguration( monitor, builder, configuration ) )
        {
        }
    }
}

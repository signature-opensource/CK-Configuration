using CK.Core;

namespace CK.Object.Predicate
{
    public sealed class IsStringPredicateConfiguration : IsTypePredicateConfiguration<string>
    {
        public IsStringPredicateConfiguration( IActivityMonitor monitor, PolymorphicConfigurationTypeBuilder builder, ImmutableConfigurationSection configuration )
            : base( monitor, builder, configuration )
        {
        }
    }

}

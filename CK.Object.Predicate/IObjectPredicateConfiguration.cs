using CK.Core;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Generalizes <see cref="ObjectPredicateConfiguration"/> and <see cref="ObjectAsyncPredicateConfiguration"/>.
    /// </summary>
    public interface IObjectPredicateConfiguration
    {
        /// <summary>
        /// Gets the configuration section.
        /// </summary>
        ImmutableConfigurationSection Configuration { get; }
    }
}

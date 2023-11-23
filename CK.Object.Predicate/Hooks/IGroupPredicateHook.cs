using System.Collections.Immutable;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Generalizes <see cref="GroupPredicateHook"/> and <see cref="GroupAsyncPredicateHook"/> wrappers.
    /// </summary>
    public interface IGroupPredicateHook : IObjectPredicateHook
    {
        /// <summary>
        /// Gets the subordinated predicates.
        /// </summary>
        ImmutableArray<IObjectPredicateHook> Predicates { get; }
    }

}

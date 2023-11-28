using System.Collections.Generic;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Generalizes <see cref="GroupPredicateConfiguration"/> and <see cref="GroupAsyncPredicateConfiguration"/>.
    /// </summary>
    public interface IGroupPredicateConfiguration : IObjectPredicateConfiguration
    {
        /// <summary>
        /// Gets whether this is a "And" group (the default: <see cref="AtLeast"/> == 0 and <see cref="AtMost"/> == 0).
        /// </summary>
        bool All { get; }

        /// <summary>
        /// Gets whether this is a "Or" group (<see cref="AtLeast"/> == 1 and <see cref="AtMost"/> == 0).
        /// </summary>
        bool Any { get; }

        /// <summary>
        /// Gets whether this is a "Exclusive Or" group (<see cref="AtLeast"/> == 1 and <see cref="AtMost"/> == 1).
        /// </summary>
        bool Single { get; }

        /// <summary>
        /// Gets the minimal number of predicates to satisfy among the <see cref="Predicates"/>.
        /// This can be 0 for "All" or if <see cref="AtMost"/> is positive.
        /// </summary>
        int AtLeast { get; }

        /// <summary>
        /// Gets the maximal number of predicates to satisfy among the <see cref="Predicates"/>.
        /// When 0 this is ignored.
        /// </summary>
        int AtMost { get; }

        /// <summary>
        /// Gets the subordinated predicates configurations.
        /// <para>
        /// When this is empty, this configuration generates the empty (null) predicate.
        /// </para>
        /// </summary>
        IReadOnlyList<IObjectPredicateConfiguration> Predicates { get; }
    }
}

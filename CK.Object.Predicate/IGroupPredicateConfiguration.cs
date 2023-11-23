using System.Collections.Generic;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Generalizes <see cref="GroupPredicateConfiguration"/> and <see cref="GroupAsyncPredicateConfiguration"/>.
    /// </summary>
    public interface IGroupPredicateConfiguration : IObjectPredicateConfiguration
    {
        /// <summary>
        /// Gets whether this is a "And" group (same as <see cref="AtLeast"/> == 0).
        /// </summary>
        bool All { get; }

        /// <summary>
        /// Gets whether this is a "Or" group (same as <see cref="AtLeast"/> == 1).
        /// </summary>
        bool Any { get; }

        /// <summary>
        /// Gets the number of predicates to satisfy.
        /// <list type="bullet">
        /// <item>
        /// 0 - For the default "All" ("And" connector): all condition must be met. When <see cref="Predicates"/> is empty, this always evaluates to true.
        /// </item>
        /// <item>
        /// 1 - For "Any" ("Or" connector): at least one condition must be met. When <see cref="Predicates"/> is empty, this always evaluates to false.
        /// </item>
        /// <item>
        /// Other - At least this number of conditions among the <see cref="Predicates"/> count conditions (note that there is necessarily
        /// at least 3 predicates otherwise this would be a "All" or a "Any".
        /// </item>
        /// </list>
        /// </summary>
        int AtLeast { get; }

        /// <summary>
        /// Gets the subordinated predicates configurations.
        /// <para>
        /// When this is empty, this configuration generates the empty (null) predicate.
        /// </para>
        /// </summary>
        IReadOnlyList<IObjectPredicateConfiguration> Predicates { get; }
    }
}

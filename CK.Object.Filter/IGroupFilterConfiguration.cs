using System.Collections.Generic;

namespace CK.Object.Filter
{
    /// <summary>
    /// Generalizes <see cref="GroupFilterConfiguration"/> and <see cref="GroupAsyncFilterConfiguration"/>.
    /// </summary>
    public interface IGroupFilterConfiguration : IObjectFilterConfiguration
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
        /// Gets the number of filters to satisfy.
        /// <list type="bullet">
        /// <item>
        /// 0 - For the default "All" ("And" connector): all condition must be met. When <see cref="Filters"/> is empty, this always evaluates to true.
        /// </item>
        /// <item>
        /// 1 - For "Any" ("Or" connector): at least one condition must be met. When <see cref="Filters"/> is empty, this always evaluates to false.
        /// </item>
        /// <item>
        /// Other - At least this number of conditions among the <see cref="Filters"/> count conditions (note that there is necessarily
        /// at least 3 filters otherwise this would be a "All" or a "Any".
        /// </item>
        /// </list>
        /// </summary>
        int AtLeast { get; }

        /// <summary>
        /// Gets the subordinated filter configurations.
        /// </summary>
        IReadOnlyList<IObjectFilterConfiguration> Filters { get; }
    }
}

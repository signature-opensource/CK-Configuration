using System.Collections.Immutable;

namespace CK.Object.Filter
{
    /// <summary>
    /// Generalizes <see cref="GroupFilterHook"/> and <see cref="GroupAsyncFilterHook"/> wrappers.
    /// </summary>
    public interface IGroupFilterHook : IObjectFilterHook
    {
        /// <summary>
        /// Gets the subordinated filters.
        /// </summary>
        ImmutableArray<IObjectFilterHook> Items { get; }
    }

}

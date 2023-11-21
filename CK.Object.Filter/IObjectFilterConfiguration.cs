using CK.Core;

namespace CK.Object.Filter
{
    /// <summary>
    /// Generalizes <see cref="ObjectFilterConfiguration"/> and <see cref="ObjectAsyncFilterConfiguration"/>.
    /// </summary>
    public interface IObjectFilterConfiguration
    {
        /// <summary>
        /// Gets the configuration section.
        /// </summary>
        ImmutableConfigurationSection Configuration { get; }
    }
}

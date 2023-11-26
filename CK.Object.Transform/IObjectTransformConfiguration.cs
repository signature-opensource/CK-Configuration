using CK.Core;

namespace CK.Object.Transform
{
    /// <summary>
    /// Generalizes <see cref="ObjectTransformConfiguration"/> and <see cref="ObjectAsyncTransformConfiguration"/>.
    /// </summary>
    public interface IObjectTransformConfiguration
    {
        /// <summary>
        /// Gets the configuration section.
        /// </summary>
        ImmutableConfigurationSection Configuration { get; }
    }
}

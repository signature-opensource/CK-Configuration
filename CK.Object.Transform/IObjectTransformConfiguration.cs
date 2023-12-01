using CK.Core;

namespace CK.Object.Transform
{
    /// <summary>
    /// Minimal view of a transform configuration.
    /// </summary>
    public interface IObjectTransformConfiguration
    {
        /// <summary>
        /// Gets the configuration path.
        /// </summary>
        string ConfigurationPath { get; }

        /// <summary>
        /// Gets this transformation as a synchronous one if it is a synchronous one.
        /// </summary>
        ObjectTransformConfiguration? Synchronous { get; }
    }
}

using System.Collections.Generic;

namespace CK.Object.Transform
{
    /// <summary>
    /// Generalizes <see cref="SequenceTransformConfiguration"/> and <see cref="SequenceAsyncTransformConfiguration"/>.
    /// </summary>
    public interface ISequenceTransformConfiguration : IObjectTransformConfiguration
    {
        /// <summary>
        /// Gets the subordinated transform configurations.
        /// <para>
        /// When this is empty, this configuration generates the empty (null) transform.
        /// </para>
        /// </summary>
        IReadOnlyList<IObjectTransformConfiguration> Transforms { get; }
    }
}

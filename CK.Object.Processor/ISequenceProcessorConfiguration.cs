using System.Collections.Generic;

namespace CK.Object.Processor
{
    /// <summary>
    /// Generalizes <see cref="SequenceProcessorConfiguration"/> and <see cref="SequenceAsyncProcessorConfiguration"/>.
    /// </summary>
    public interface ISequenceProcessorConfiguration : IObjectProcessorConfiguration
    {
        /// <summary>
        /// Gets the subordinated transform configurations.
        /// <para>
        /// When this is empty, this configuration generates the empty (null) transform.
        /// </para>
        /// </summary>
        IReadOnlyList<IObjectProcessorConfiguration> Processors { get; }
    }
}

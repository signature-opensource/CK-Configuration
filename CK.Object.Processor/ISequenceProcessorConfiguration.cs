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
        /// When this is empty, this configuration generates the (null) identity transform function.
        /// Note that this is only configurations. Each of them can generate a (null) identity transform:
        /// items in this list doens't guaranty anything about the eventual transform function. 
        /// </para>
        /// </summary>
        IReadOnlyList<IObjectProcessorConfiguration> Processors { get; }
    }
}

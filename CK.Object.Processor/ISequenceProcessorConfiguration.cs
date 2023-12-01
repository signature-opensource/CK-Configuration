using System.Collections.Generic;

namespace CK.Object.Processor
{
    /// <summary>
    /// The composite of processor is a sequence of <see cref="Processors"/> that act as
    /// a switch-case: the first that accepts the input wins.
    /// </summary>
    public interface ISequenceProcessorConfiguration : IObjectProcessorConfiguration
    {
        /// <summary>
        /// Gets the subordinated processor configurations.
        /// <para>
        /// When this is empty, this configuration generates the (null) void processor.
        /// Note that this is only configurations. Each of them can generate a (null) void processor:
        /// items in this list doens't guaranty anything about the eventual processor. 
        /// </para>
        /// </summary>
        IReadOnlyList<IObjectProcessorConfiguration> Processors { get; }
    }
}

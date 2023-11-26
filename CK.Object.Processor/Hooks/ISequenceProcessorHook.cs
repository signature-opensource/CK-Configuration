using System.Collections.Immutable;

namespace CK.Object.Processor
{
    /// <summary>
    /// Generalizes <see cref="SequenceProcessorHook"/> and <see cref="SequenceAsyncProcessorHook"/> wrappers.
    /// </summary>
    public interface ISequenceProcessorHook : IObjectProcessorHook
    {
        /// <summary>
        /// Gets the subordinated transformations.
        /// </summary>
        ImmutableArray<IObjectProcessorHook> Processors { get; }
    }

}

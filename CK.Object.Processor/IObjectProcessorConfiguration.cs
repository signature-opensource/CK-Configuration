using CK.Core;
using CK.Object.Predicate;
using CK.Object.Transform;

namespace CK.Object.Processor
{
    /// <summary>
    /// Generalizes <see cref="ObjectProcessorConfiguration"/> and <see cref="ObjectAsyncProcessorConfiguration"/>.
    /// </summary>
    public interface IObjectProcessorConfiguration
    {
        /// <summary>
        /// Gets the optional configured condition.
        /// </summary>
        IObjectPredicateConfiguration? Condition { get; }

        /// <summary>
        /// Gets the optional configured transformation.
        /// </summary>
        IObjectTransformConfiguration? Transform { get; }
    }
}

using CK.Core;
using CK.Object.Predicate;
using CK.Object.Transform;

namespace CK.Object.Processor
{
    /// <summary>
    /// Minimal view of a processor configuration.
    /// </summary>
    public interface IObjectProcessorConfiguration
    {
        /// <summary>
        /// Gets the configuration path.
        /// </summary>
        string ConfigurationPath { get; }

        /// <summary>
        /// Gets the optional configured condition.
        /// </summary>
        ObjectAsyncPredicateConfiguration? Condition { get; }

        /// <summary>
        /// Gets the optional configured transformation.
        /// </summary>
        ObjectAsyncTransformConfiguration? Transform { get; }
    }
}

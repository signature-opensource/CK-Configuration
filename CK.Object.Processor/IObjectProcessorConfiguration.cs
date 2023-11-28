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
        /// Gets the configuration section.
        /// </summary>
        ImmutableConfigurationSection Configuration { get; }

        /// <summary>
        /// Gets the optional condition configuration.
        /// </summary>
        ObjectPredicateConfiguration? Condition { get; }

        /// <summary>
        /// Gets the optional action configuration.
        /// </summary>
        ObjectTransformConfiguration? Transform { get; }
    }
}

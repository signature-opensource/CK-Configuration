using CK.Object.Predicate;
using CK.Object.Transform;

namespace CK.Object.Processor
{
    /// <summary>
    /// Generalizes <see cref="ObjectProcessorHook"/> and <see cref="ObjectAsyncProcessorHook"/>.
    /// </summary>
    public interface IObjectProcessorHook
    {
        /// <summary>
        /// Gets the configuration.
        /// </summary>
        IObjectProcessorConfiguration Configuration { get; }
        IObjectPredicateHook? Condition { get; }
        IObjectTransformHook? Action { get; }
    }

}

using System;

namespace CK.Object.Filter
{
    /// <summary>
    /// Generalizes <see cref="ObjectFilterHook"/> and <see cref="ObjectAsyncFilterHook"/>.
    /// </summary>
    public interface IObjectFilterHook
    {
        /// <summary>
        /// Gets the configuration.
        /// </summary>
        IObjectFilterConfiguration Configuration { get; }

        /// <summary>
        /// Raised before <see cref="ObjectFilterHook.Evaluate(object)"/> or <see cref="ObjectAsyncFilterHook.EvaluateAsync(object)"/>.
        /// </summary>
        event Action<IObjectFilterHook, object>? Before;

        /// <summary>
        /// Raised after <see cref="ObjectFilterHook.Evaluate(object)"/> or <see cref="ObjectAsyncFilterHook.EvaluateAsync(object)"/>
        /// with the predicate result.
        /// </summary>
        event Action<IObjectFilterHook, object, bool>? After;

    }
}

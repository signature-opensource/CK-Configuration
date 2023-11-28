namespace CK.Object.Predicate
{
    /// <summary>
    /// Generalizes <see cref="ObjectPredicateHook"/> and <see cref="ObjectAsyncPredicateHook"/>.
    /// </summary>
    public interface IObjectPredicateHook
    {
        /// <summary>
        /// Gets the configuration.
        /// </summary>
        IObjectPredicateConfiguration Configuration { get; }

        /// <summary>
        /// Gets the hook context to which this hook is bound.
        /// </summary>
        PredicateHookContext Context { get; }
    }

}

namespace CK.Object.Predicate
{
    /// <summary>
    /// Minimal view of a predicate configuration.
    /// </summary>
    public interface IObjectPredicateConfiguration
    {
        /// <summary>
        /// Gets the configuration path.
        /// </summary>
        string ConfigurationPath { get; }

        /// <summary>
        /// Gets this predicate as a synchronous one if it is a synchronous predicate.
        /// </summary>
        ObjectPredicateConfiguration? Synchronous { get; }
    }
}

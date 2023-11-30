using CK.Core;
using System.Threading.Tasks;
using System;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Generalizes <see cref="ObjectPredicateConfiguration"/> and <see cref="ObjectAsyncPredicateConfiguration"/>.
    /// </summary>
    public interface IObjectPredicateConfiguration
    {
        /// <summary>
        /// Gets the configuration section.
        /// </summary>
        ImmutableConfigurationSection Configuration { get; }

        /// <summary>
        /// Gets the <see cref="ISyncObjectPredicateConfiguration"/> if this is a synchronous predicate.
        /// </summary>
        ISyncObjectPredicateConfiguration? AsSync { get; }

        /// <summary>
        /// Creates an asynchronous predicate.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">Services that may be required for some (complex) predicates.</param>
        /// <returns>A configured predicate or null for an empty predicate.</returns>
        Func<object, ValueTask<bool>>? CreateAsyncPredicate( IActivityMonitor monitor, IServiceProvider services );
    }



}

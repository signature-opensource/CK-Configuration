using CK.Core;
using System;

namespace CK.Object.Predicate
{
    public interface ISyncObjectPredicateConfiguration : IObjectPredicateConfiguration
    {
        /// <summary>
        /// Creates a synchronous predicate.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">Services that may be required for some (complex) predicates.</param>
        /// <returns>A configured object predicate or null for an empty predicate.</returns>
        Func<object, bool>? CreatePredicate( IActivityMonitor monitor, IServiceProvider services );
    }



}

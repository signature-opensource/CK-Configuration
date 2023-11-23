using CK.Core;
using Microsoft.Extensions.Configuration;

namespace CK.Object.Predicate
{
    public abstract partial class ObjectPredicateConfiguration
    {
        /// <summary>
        /// Tries to replace a <see cref="PlaceholderPredicateConfiguration"/>.
        /// <para>
        /// The <paramref name="configuration"/>.Path must be a direct child of the placeholder to replace.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="configuration">The configuration that should replace a placeholder.</param>
        /// <returns>A new configuration or null if an error occurred or the placeholder was not found.</returns>
        public ObjectPredicateConfiguration? TrySetPlaceholder( IActivityMonitor monitor,
                                                                IConfigurationSection configuration )
        {
            return TrySetPlaceholder( monitor, configuration, out var _ );
        }

        /// <summary>
        /// Tries to replace a <see cref="PlaceholderPredicateConfiguration"/>.
        /// The <paramref name="configuration"/>.Path must be a direct child of the placeholder to replace.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="configuration">The configuration that should replace a placeholder.</param>
        /// <param name="builderError">True if an error occurred while building the configuration, false if the placeholder was not found.</param>
        /// <returns>A new configuration or null if a <paramref name="builderError"/> occurred or the placeholder was not found.</returns>
        public ObjectPredicateConfiguration? TrySetPlaceholder( IActivityMonitor monitor,
                                                                IConfigurationSection configuration,
                                                                out bool builderError )
        {
            builderError = false;
            ObjectPredicateConfiguration? result = null;
            var buildError = false;
            using( monitor.OnError( () => buildError = true ) )
            {
                result = SetPlaceholder( monitor, configuration );
            }
            if( !buildError && result == this )
            {
                monitor.Error( $"Unable to set placeholder: '{configuration.GetParentPath()}' " +
                               $"doesn't exist or is not a placeholder." );
                return null;
            }
            return (builderError = buildError) ? null : result;
        }

        /// <summary>
        /// Mutator default implementation: always returns this instance by default.
        /// <para>
        /// Errors are emitted in the monitor. On error, this instance is returned. 
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use to signal errors.</param>
        /// <param name="configuration">Configuration of the replaced placeholder.</param>
        /// <returns>A new configuration or this instance if an error occurred or the placeholder has not been found.</returns>
        public virtual ObjectPredicateConfiguration SetPlaceholder( IActivityMonitor monitor, IConfigurationSection configuration )
        {
            return this;
        }

    }
}

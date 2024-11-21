using Microsoft.Extensions.Configuration;

// Using CK.Core namespace to avoid using CK.Configuration.
namespace CK.Core;

/// <summary>
/// Helper interface to support placeholder replacement in configuration object.
/// <para>
/// This must be implemented on the base type of a configuration family objects and <typeparamref name="T"/> must be this type
/// (the `TSelf` pattern) and a typed Placeholder configuration object must exist that overrides
/// the <see cref="SetPlaceholder(IActivityMonitor, IConfigurationSection)"/> to replace itself if the configuration section path
/// is a direct child of it.
/// Of course, any composite must also override SetPlaceHolder to handle the replacement.
/// </para>
/// </summary>
/// <typeparam name="T">The base type of the configuration family objects.</typeparam>
public interface ISupportConfigurationPlaceholder<T> where T : class
{
    /// <summary>
    /// Tries to replace a "Placeholder" child.
    /// This is a mutator (configuration objects are immutable) that must be virtual (actual placeholder type
    /// and composites must be able to override it).
    /// <para>
    /// The <paramref name="configuration"/>.Path must be a direct child of the placeholder to replace.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="configuration">The configuration that should replace a placeholder.</param>
    /// <returns>
    /// A new configuration (or this object if nothing changed). Should be null only if an error occurred.
    /// </returns>
    T? SetPlaceholder( IActivityMonitor monitor, IConfigurationSection configuration );
}

/// <summary>
/// Provides once for all TrySetPlaceholder helpers to any <see cref="ISupportConfigurationPlaceholder{T}"/>.
/// </summary>
public static class SupportConfigurationPlaceholderExtension
{
    /// <summary>
    /// Tries to replace a "Placeholder" in this configuration object.
    /// This logs an error and return null if the placeholder was not found.
    /// <para>
    /// The <paramref name="configuration"/>.Path must be a direct child of the placeholder to replace.
    /// </para>
    /// </summary>
    /// <param name="this">This configured object.</param>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="configuration">The configuration that should replace a placeholder.</param>
    /// <returns>
    /// A new configuration (or this object if nothing changed) or null if an error occurred or the placeholder was not found.
    /// </returns>
    public static T? TrySetPlaceholder<T>( this ISupportConfigurationPlaceholder<T> @this,
                                           IActivityMonitor monitor,
                                           IConfigurationSection configuration ) where T : class
    {
        return TrySetPlaceholder( @this, monitor, configuration, out var _ );
    }

    /// <summary>
    /// Tries to replace a "Placeholder" in this configuration object.
    /// This logs an error and return null if the placeholder was not found.
    /// <para>
    /// The <paramref name="configuration"/>.Path must be a direct child of the placeholder to replace.
    /// </para>
    /// </summary>
    /// <param name="this">This configured object.</param>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="configuration">The configuration that should replace a placeholder.</param>
    /// <param name="builderError">
    /// On error (when null is returned), this indicates whether the error occurred while building the configuration
    /// or false if the placeholder was not found.
    /// </param>
    /// <returns>
    /// A new configuration (or this object if nothing changed) or null if a <paramref name="builderError"/> occurred or the placeholder was not found.
    /// </returns>
    public static T? TrySetPlaceholder<T>( this ISupportConfigurationPlaceholder<T> @this,
                                           IActivityMonitor monitor,
                                           IConfigurationSection configuration,
                                           out bool builderError ) where T : class
    {
        builderError = false;
        T? result = null;
        var buildError = false;
        using( monitor.OnError( () => buildError = true ) )
        {
            result = @this.SetPlaceholder( monitor, configuration );
            // Security:
            if( result == null && !buildError )
            {
                monitor.Error( ActivityMonitor.Tags.ToBeInvestigated, $"SetPlaceholder returns null but no error was logged." );
                Throw.DebugAssert( "Now an error has been emitted.", buildError );
            }
        }
        if( !buildError && result == @this )
        {
            monitor.Error( $"Unable to set placeholder: '{configuration.GetParentPath()}' " +
                           $"doesn't exist or is not a placeholder." );
            return null;
        }
        return (builderError = buildError) ? null : result;
    }

}

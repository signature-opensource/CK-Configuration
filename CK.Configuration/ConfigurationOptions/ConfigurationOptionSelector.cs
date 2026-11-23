using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.Core;

/// <summary>
/// Selects the enabled <see cref="IConfigurationOption"/> of an owner from its configuration. In the section of the owner,
/// the key of an option is its name:
/// <list type="bullet">
///   <item>a section (<c>"Name": { ... }</c>) or <c>true</c> enables it explicitly;</item>
///   <item><c>false</c> disables it (an error for a <see cref="IConfigurationOption.IsRequired"/> option);</item>
///   <item>nothing: its default (<see cref="IConfigurationOption.IsEnabledByDefault"/>).</item>
/// </list>
/// A redundant <c>true</c> (an option enabled by default) or <c>false</c> (an opt-in one) is a warning. A required
/// option is always enabled. An option enabled explicitly (or required) that requires a disabled one is an error. An
/// option enabled by default that requires a disabled one is disabled with it (an information is logged). This is a pure
/// function of the options and of the configuration.
/// </summary>
public static class ConfigurationOptionSelector
{
    /// <summary>
    /// Selects options from the configuration section of their owner.
    /// </summary>
    /// <typeparam name="T">The option type.</typeparam>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="options">The ordered options of the owner.</param>
    /// <param name="ownerSection">The configuration section of the owner. Null when the owner has none.</param>
    /// <param name="selected">The selected options, in their order.</param>
    /// <returns>True on success, false on error (errors are logged).</returns>
    public static bool TrySelect<T>( IActivityMonitor monitor,
                                     IReadOnlyList<T> options,
                                     ImmutableConfigurationSection? ownerSection,
                                     out IReadOnlyList<SelectedConfigurationOption<T>> selected )
        where T : IConfigurationOption
    {
        return TrySelect( monitor, options, name => ownerSection?.TryGetSection( name ), out selected );
    }

    /// <summary>
    /// Selects options: see <see cref="TrySelect{T}(IActivityMonitor, IReadOnlyList{T}, ImmutableConfigurationSection?, out IReadOnlyList{SelectedConfigurationOption{T}})"/>.
    /// The configuration of each option is found by <paramref name="findOption"/> (the configuration of an owner may be
    /// looked up in several sections, a "Defaults" section for instance).
    /// </summary>
    /// <typeparam name="T">The option type.</typeparam>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="options">The ordered options of the owner.</param>
    /// <param name="findOption">Finds the configuration of an option by its name. Null when not configured.</param>
    /// <param name="selected">The selected options, in their order.</param>
    /// <returns>True on success, false on error (errors are logged).</returns>
    public static bool TrySelect<T>( IActivityMonitor monitor,
                                     IReadOnlyList<T> options,
                                     Func<string, ImmutableConfigurationSection?> findOption,
                                     out IReadOnlyList<SelectedConfigurationOption<T>> selected )
        where T : IConfigurationOption
    {
        Throw.CheckNotNullArgument( monitor );
        Throw.CheckNotNullArgument( options );
        Throw.CheckNotNullArgument( findOption );
        selected = Array.Empty<SelectedConfigurationOption<T>>();
        bool success = true;
        var enabled = new Dictionary<string, (T Option, bool IsExplicit, ImmutableConfigurationSection? Section)>( StringComparer.OrdinalIgnoreCase );
        foreach( var o in options )
        {
            var s = findOption( o.Name );
            // A required option is always enabled: it is "explicit" (a disabled requirement is an error).
            bool required = o.IsRequired;
            bool byDefault = required || o.IsEnabledByDefault;
            if( s == null )
            {
                if( byDefault ) enabled.Add( o.Name, (o, required, null) );
            }
            else if( s.Value != null )
            {
                if( bool.TryParse( s.Value, out var b ) )
                {
                    if( b )
                    {
                        if( byDefault ) monitor.Warn( $"Redundant '{s.Path}' = true: the option is {(required ? "required" : "enabled by default")}." );
                        enabled.Add( o.Name, (o, true, null) );
                    }
                    else if( required )
                    {
                        monitor.Error( $"The option '{s.Path}' is required: it can't be turned off." );
                        success = false;
                    }
                    else if( !byDefault )
                    {
                        monitor.Warn( $"Redundant '{s.Path}' = false: the option is optional, it is off unless enabled." );
                    }
                }
                else
                {
                    monitor.Error( $"Invalid option '{s.Path}' = '{s.Value}': true, false or a section is expected." );
                    success = false;
                }
            }
            else
            {
                enabled.Add( o.Name, (o, true, s) );
            }
        }
        if( !success ) return false;
        // The closure: an option needs the options it requires.
        bool changed;
        do
        {
            changed = false;
            foreach( var (name, e) in enabled.ToList() )
            {
                var missing = e.Option.Requires.FirstOrDefault( r => !enabled.ContainsKey( r ) );
                if( missing == null ) continue;
                if( e.IsExplicit )
                {
                    monitor.Error( $"The option '{name}' requires the option '{missing}' that is disabled." );
                    success = false;
                }
                else
                {
                    monitor.Info( $"The option '{name}' (enabled by default) is disabled: it requires the option '{missing}' that is disabled." );
                }
                enabled.Remove( name );
                changed = true;
            }
        }
        while( changed );
        if( !success ) return false;
        selected = options.Where( o => enabled.ContainsKey( o.Name ) )
                          .Select( o => new SelectedConfigurationOption<T>( o, enabled[o.Name].Section ) )
                          .ToArray();
        return true;
    }
}

/// <summary>
/// A selected option of an owner and its configuration section (null when it is enabled by default or by <c>true</c>).
/// </summary>
/// <typeparam name="T">The option type.</typeparam>
/// <param name="Option">The option.</param>
/// <param name="Configuration">Its configuration section, if any.</param>
public readonly record struct SelectedConfigurationOption<T>( T Option, ImmutableConfigurationSection? Configuration ) where T : IConfigurationOption;

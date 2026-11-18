using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace CK.Core;

/// <summary>
/// A configuration template is a section with <c>"InstanceCount": n</c> or <c>"Instances": [ "Top", "Red" ]</c>: it is
/// described once and instantiated for each of its instances (<see cref="ConfigurationInstance"/>). Its values may
/// contain the placeholders of the instance:
/// <list type="bullet">
///   <item><c>{Index}</c> (1-based), <c>{Name}</c> and <c>{n+Index}</c> (an integer plus the index: <c>{5400+Index}</c>);</item>
///   <item>qualified by the name of an enclosing template, in an inner template: <c>{Trolley.Index}</c>,
///   <c>{Trolley.Name}</c>, <c>{n+Trolley.Index}</c>. The innermost template wins: in an inner template, <c>{Index}</c> is
///   its own.</item>
/// </list>
/// A <c>{...}</c> that is none of these forms is left as is. The placeholders are in the values only (never in the keys).
/// </summary>
public static class ConfigurationTemplate
{
    /// <summary>
    /// The key of the number of instances: "InstanceCount".
    /// </summary>
    public const string InstanceCountKey = "InstanceCount";

    /// <summary>
    /// The key of the names of the instances: "Instances".
    /// </summary>
    public const string InstancesKey = "Instances";

    static readonly Regex _placeholder = new Regex( @"\{(?:(?<n>-?\d+)\+)?(?:(?<q>[A-Za-z_][\w.\-]*)\.)?(?<k>Index|Name)\}",
                                                    RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled );

    /// <summary>
    /// Gets whether a section is a template: it has "InstanceCount" or "Instances".
    /// </summary>
    /// <param name="section">The section.</param>
    /// <returns>True if the section is a template.</returns>
    public static bool IsTemplate( IConfigurationSection section )
    {
        Throw.CheckNotNullArgument( section );
        return section.GetSection( InstanceCountKey ).Exists() || section.GetSection( InstancesKey ).Exists();
    }

    /// <summary>
    /// Reads the instances of a template section.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="section">The section.</param>
    /// <param name="templateName">The name of the template (the qualifier of its placeholders), or null.</param>
    /// <param name="instances">The instances, null if the section is not a template (or on error).</param>
    /// <returns>True on success (a template or not), false on error.</returns>
    public static bool TryReadInstances( IActivityMonitor monitor,
                                         IConfigurationSection section,
                                         string? templateName,
                                         out IReadOnlyList<ConfigurationInstance>? instances )
    {
        Throw.CheckNotNullArgument( monitor );
        Throw.CheckNotNullArgument( section );
        instances = null;
        var count = section.GetSection( InstanceCountKey );
        var names = section.GetSection( InstancesKey );
        if( !count.Exists() && !names.Exists() ) return true;
        if( count.Exists() && names.Exists() )
        {
            monitor.Error( $"\"{section.Path}\" has \"{InstanceCountKey}\" and \"{InstancesKey}\": a template has one of them." );
            return false;
        }
        var result = new List<ConfigurationInstance>();
        if( count.Exists() )
        {
            if( count.Value == null || !int.TryParse( count.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var n ) || n <= 0 )
            {
                monitor.Error( $"Invalid \"{count.Path}\": '{count.Value}'. A positive integer is expected." );
                return false;
            }
            for( int i = 1; i <= n; ++i ) result.Add( new ConfigurationInstance( templateName, i, i.ToString( CultureInfo.InvariantCulture ) ) );
        }
        else
        {
            var items = names.GetChildren().ToList();
            if( names.Value != null || items.Count == 0 || items.Any( c => c.Value == null ) )
            {
                monitor.Error( $"Invalid \"{names.Path}\": a non empty array of instance names is expected." );
                return false;
            }
            bool success = true;
            var set = new HashSet<string>( StringComparer.OrdinalIgnoreCase );
            // The configuration keys of an array are its indices: they are ordered numerically.
            int index = 0;
            foreach( var c in items.OrderBy( c => int.TryParse( c.Key, out var k ) ? k : int.MaxValue ) )
            {
                var name = c.Value!.Trim();
                ++index;
                if( !CoreApplicationIdentity.IsValidPartyName( name ) || name.StartsWith( '$' ) )
                {
                    monitor.Error( $"Invalid instance name '{c.Value}' in \"{names.Path}\": a name is made of letters, digits, '-' and '_' (and doesn't start with a digit, '-' or '_')." );
                    success = false;
                }
                else if( !set.Add( name ) )
                {
                    monitor.Error( $"Duplicate instance name '{name}' in \"{names.Path}\" (the names are case insensitive)." );
                    success = false;
                }
                else result.Add( new ConfigurationInstance( templateName, index, name ) );
            }
            if( !success ) return false;
        }
        instances = result;
        return true;
    }

    /// <summary>
    /// Finds an instance by its name (case insensitive) or by its index.
    /// </summary>
    /// <param name="instances">The instances.</param>
    /// <param name="key">The name or the index.</param>
    /// <returns>The instance or null.</returns>
    public static ConfigurationInstance? FindInstance( IReadOnlyList<ConfigurationInstance> instances, string key )
    {
        Throw.CheckNotNullArgument( instances );
        Throw.CheckNotNullArgument( key );
        key = key.Trim();
        return instances.FirstOrDefault( i => i.Name.Equals( key, StringComparison.OrdinalIgnoreCase ) )
               ?? (int.TryParse( key, NumberStyles.None, CultureInfo.InvariantCulture, out var index )
                    ? instances.FirstOrDefault( i => i.Index == index )
                    : null);
    }

    /// <summary>
    /// Resolves the placeholders of a value.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="value">The value.</param>
    /// <param name="path">The path of the value (for the errors).</param>
    /// <param name="current">
    /// The instance of the unqualified placeholders. Null when the value is in an inner template that is not resolved
    /// by this pass: its unqualified placeholders are left as is.
    /// </param>
    /// <param name="enclosing">The instances of the enclosing templates, for the qualified placeholders.</param>
    /// <param name="resolved">The resolved value (the value itself when nothing changed).</param>
    /// <returns>True on success, false on error (a qualified placeholder that names no known template).</returns>
    public static bool TryResolve( IActivityMonitor monitor,
                                   string value,
                                   string path,
                                   ConfigurationInstance? current,
                                   IReadOnlyList<ConfigurationInstance> enclosing,
                                   out string resolved )
    {
        Throw.CheckNotNullArgument( value );
        Throw.CheckNotNullArgument( enclosing );
        bool success = true;
        resolved = _placeholder.Replace( value, m =>
        {
            ConfigurationInstance? i;
            var q = m.Groups["q"];
            if( q.Success )
            {
                i = enclosing.Append( current ).LastOrDefault( e => e != null && q.Value.Equals( e.TemplateName, StringComparison.OrdinalIgnoreCase ) );
                if( i == null )
                {
                    // A qualified placeholder of an inner template that this pass doesn't resolve is left as is.
                    if( current == null ) return m.Value;
                    monitor.Error( $"The placeholder '{m.Value}' of \"{path}\" names no enclosing template ('{q.Value}')." );
                    success = false;
                    return m.Value;
                }
            }
            else
            {
                i = current;
                if( i == null ) return m.Value;
            }
            var isIndex = m.Groups["k"].Value.Equals( "Index", StringComparison.OrdinalIgnoreCase );
            var n = m.Groups["n"];
            if( !isIndex )
            {
                if( n.Success )
                {
                    monitor.Error( $"Invalid placeholder '{m.Value}' of \"{path}\": only the Index can be offset." );
                    success = false;
                    return m.Value;
                }
                return i.Name;
            }
            return n.Success
                    ? (long.Parse( n.Value, CultureInfo.InvariantCulture ) + i.Index).ToString( CultureInfo.InvariantCulture )
                    : i.Index.ToString( CultureInfo.InvariantCulture );
        } );
        return success;
    }

    /// <summary>
    /// Resolves the placeholders of all the values below a section for an instance: the unqualified ones with the
    /// <paramref name="instance"/>, except in an inner template (a sub section that is a template: only its qualified
    /// placeholders are resolved). The "InstanceCount" and "Instances" of the section itself are not values to
    /// resolve.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="section">The template section (or the root of the configuration).</param>
    /// <param name="instance">The instance.</param>
    /// <param name="enclosing">The instances of the enclosing templates, if any.</param>
    /// <returns>The changed values (their absolute path and their resolved value), or null on error.</returns>
    public static List<KeyValuePair<string, string?>>? ResolveSection( IActivityMonitor monitor,
                                                                       IConfiguration section,
                                                                       ConfigurationInstance instance,
                                                                       IReadOnlyList<ConfigurationInstance>? enclosing = null )
    {
        Throw.CheckNotNullArgument( monitor );
        Throw.CheckNotNullArgument( section );
        Throw.CheckNotNullArgument( instance );
        enclosing ??= Array.Empty<ConfigurationInstance>();
        var result = new List<KeyValuePair<string, string?>>();
        bool success = true;
        foreach( var c in section.GetChildren() )
        {
            if( c.Key.Equals( InstanceCountKey, StringComparison.OrdinalIgnoreCase )
                || c.Key.Equals( InstancesKey, StringComparison.OrdinalIgnoreCase ) )
            {
                continue;
            }
            success &= Walk( monitor, c, instance, enclosing, result );
        }
        return success ? result : null;

        static bool Walk( IActivityMonitor monitor,
                          IConfigurationSection s,
                          ConfigurationInstance? current,
                          IReadOnlyList<ConfigurationInstance> enclosing,
                          List<KeyValuePair<string, string?>> result )
        {
            if( s.Value != null )
            {
                if( !TryResolve( monitor, s.Value, s.Path, current, enclosing, out var r ) ) return false;
                if( !ReferenceEquals( r, s.Value ) && r != s.Value ) result.Add( new KeyValuePair<string, string?>( s.Path, r ) );
                return true;
            }
            if( current != null && IsTemplate( s ) )
            {
                // An inner template: its own placeholders are its instances', the qualified ones are resolved here.
                enclosing = enclosing.Append( current ).ToArray();
                current = null;
            }
            bool success = true;
            foreach( var c in s.GetChildren() ) success &= Walk( monitor, c, current, enclosing, result );
            return success;
        }
    }
}

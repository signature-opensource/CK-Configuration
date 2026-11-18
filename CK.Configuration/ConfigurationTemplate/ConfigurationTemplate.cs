using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace CK.Core;

/// <summary>
/// A configuration template is a section with <c>"InstanceCount": n</c> or <c>"Instances": [ "Top", "Red" ]</c>: it is
/// described once and instantiated for each of its instances (<see cref="ConfigurationInstance"/>). With
/// <c>"MaxInstanceCount": m</c>, it is Opened: dynamic instances may exist after the configured ones, up to <c>m</c>
/// (<see cref="ConfigurationTemplateInstances"/>); <c>"InstanceCount"</c> may then be 0 (or missing). Its values may
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

    /// <summary>
    /// The key of the maximal number of instances of an Opened template: "MaxInstanceCount".
    /// </summary>
    public const string MaxInstanceCountKey = "MaxInstanceCount";

    static readonly Regex _placeholder = new Regex( @"\{(?:(?<n>-?\d+)\+)?(?:(?<q>[A-Za-z_][\w.\-]*)\.)?(?<k>Index|Name)\}",
                                                    RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled );

    /// <summary>
    /// Gets whether a section is a template: it has "InstanceCount", "Instances" or "MaxInstanceCount".
    /// </summary>
    /// <param name="section">The section.</param>
    /// <returns>True if the section is a template.</returns>
    public static bool IsTemplate( IConfigurationSection section )
    {
        Throw.CheckNotNullArgument( section );
        return section.GetSection( InstanceCountKey ).Exists()
               || section.GetSection( InstancesKey ).Exists()
               || section.GetSection( MaxInstanceCountKey ).Exists();
    }

    static bool IsTemplateKey( string key ) => key.Equals( InstanceCountKey, StringComparison.OrdinalIgnoreCase )
                                               || key.Equals( InstancesKey, StringComparison.OrdinalIgnoreCase )
                                               || key.Equals( MaxInstanceCountKey, StringComparison.OrdinalIgnoreCase );

    /// <summary>
    /// Reads the instances of a template section.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="section">The section.</param>
    /// <param name="templateName">The name of the template (the qualifier of its placeholders), or null.</param>
    /// <param name="instances">
    /// The configured instances (empty for an Opened template without them), null if the section is not a template (or on error).
    /// </param>
    /// <returns>True on success (a template or not), false on error.</returns>
    public static bool TryReadInstances( IActivityMonitor monitor,
                                         IConfigurationSection section,
                                         string? templateName,
                                         out ConfigurationTemplateInstances? instances )
    {
        Throw.CheckNotNullArgument( monitor );
        Throw.CheckNotNullArgument( section );
        instances = null;
        var count = section.GetSection( InstanceCountKey );
        var names = section.GetSection( InstancesKey );
        var max = section.GetSection( MaxInstanceCountKey );
        if( !count.Exists() && !names.Exists() && !max.Exists() ) return true;
        if( count.Exists() && names.Exists() )
        {
            monitor.Error( $"\"{section.Path}\" has \"{InstanceCountKey}\" and \"{InstancesKey}\": a template has one of them." );
            return false;
        }
        int? maxInstanceCount = null;
        if( max.Exists() )
        {
            if( max.Value == null || !int.TryParse( max.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var m ) || m <= 0 )
            {
                monitor.Error( $"Invalid \"{max.Path}\": '{max.Value}'. A positive integer is expected." );
                return false;
            }
            maxInstanceCount = m;
        }
        var result = new List<ConfigurationInstance>();
        if( count.Exists() )
        {
            // An Opened template may have no configured instance.
            if( count.Value == null
                || !int.TryParse( count.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var n )
                || (n == 0 && maxInstanceCount == null) )
            {
                monitor.Error( maxInstanceCount == null
                                ? $"Invalid \"{count.Path}\": '{count.Value}'. A positive integer is expected."
                                : $"Invalid \"{count.Path}\": '{count.Value}'. A positive integer or 0 is expected." );
                return false;
            }
            for( int i = 1; i <= n; ++i ) result.Add( new ConfigurationInstance( templateName, i, i.ToString( CultureInfo.InvariantCulture ) ) );
        }
        else if( names.Exists() )
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
        if( maxInstanceCount < result.Count )
        {
            monitor.Error( $"Invalid \"{max.Path}\": {maxInstanceCount} is lower than the {result.Count} configured instance(s)." );
            return false;
        }
        instances = new ConfigurationTemplateInstances( result, templateName, maxInstanceCount );
        return true;
    }

    /// <summary>
    /// Gets whether a value has an unqualified placeholder (<c>{Index}</c>, <c>{Name}</c> or <c>{n+Index}</c>): its
    /// instances have distinct values.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>True if the value has an unqualified placeholder.</returns>
    public static bool HasPlaceholder( string value )
    {
        Throw.CheckNotNullArgument( value );
        return _placeholder.Matches( value ).Any( m => !m.Groups["q"].Success );
    }

    /// <summary>
    /// Finds the index of the dynamic instance whose resolved <paramref name="pattern"/> is <paramref name="value"/>
    /// (case insensitive). A dynamic instance is named by its index: <c>{Name}</c> and <c>{Index}</c> match the index,
    /// <c>{n+Index}</c> the index plus n. The pattern must have at least one placeholder and no qualified one (the
    /// enclosing templates are resolved), and all its placeholders must give the same index.
    /// </summary>
    /// <param name="pattern">The value of the template, with its placeholders.</param>
    /// <param name="value">The resolved value.</param>
    /// <param name="index">The index (positive) on success.</param>
    /// <returns>True if the value is the pattern resolved by a dynamic instance.</returns>
    public static bool TryMatchIndex( string pattern, string value, out int index )
    {
        Throw.CheckNotNullArgument( pattern );
        Throw.CheckNotNullArgument( value );
        index = 0;
        var placeholders = _placeholder.Matches( pattern );
        if( placeholders.Count == 0 || placeholders.Any( m => m.Groups["q"].Success ) ) return false;
        var regex = new System.Text.StringBuilder( "^" );
        int last = 0;
        foreach( Match p in placeholders )
        {
            regex.Append( Regex.Escape( pattern.Substring( last, p.Index - last ) ) ).Append( @"(-?\d+)" );
            last = p.Index + p.Length;
        }
        regex.Append( Regex.Escape( pattern.Substring( last ) ) ).Append( '$' );
        var match = Regex.Match( value, regex.ToString(), RegexOptions.CultureInvariant | RegexOptions.IgnoreCase );
        if( !match.Success ) return false;
        int found = 0;
        for( int i = 0; i < placeholders.Count; ++i )
        {
            var text = match.Groups[i + 1].Value;
            if( !long.TryParse( text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var v ) ) return false;
            var n = placeholders[i].Groups["n"];
            long candidate = v - (n.Success ? long.Parse( n.Value, CultureInfo.InvariantCulture ) : 0);
            // The value must be exactly what the resolution gives (no leading zeros, no explicit '+').
            if( candidate <= 0 || candidate > int.MaxValue || v.ToString( CultureInfo.InvariantCulture ) != text ) return false;
            if( found != 0 && found != candidate ) return false;
            found = (int)candidate;
        }
        index = found;
        return true;
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
    /// Creates the section of an instance of a template: the same path and content, without its "InstanceCount",
    /// "Instances" and "MaxInstanceCount", its placeholders resolved (see <see cref="ResolveSection"/>).
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="template">The template section.</param>
    /// <param name="instance">The instance.</param>
    /// <param name="lookupParent">The lookup parent of the new section (see <see cref="ImmutableConfigurationSection(IConfigurationSection, ImmutableConfigurationSection?)"/>).</param>
    /// <param name="enclosing">The instances of the enclosing templates, if any.</param>
    /// <returns>The section of the instance, or null on error.</returns>
    public static ImmutableConfigurationSection? Instantiate( IActivityMonitor monitor,
                                                              IConfigurationSection template,
                                                              ConfigurationInstance instance,
                                                              ImmutableConfigurationSection? lookupParent = null,
                                                              IReadOnlyList<ConfigurationInstance>? enclosing = null )
    {
        var changes = ResolveSection( monitor, template, instance, enclosing );
        if( changes == null ) return null;
        var resolved = changes.ToDictionary( kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase );
        var m = new MutableConfigurationSection( template.Path );
        foreach( var c in template.GetChildren() )
        {
            if( IsTemplateKey( c.Key ) ) continue;
            Copy( m, c );
        }
        return new ImmutableConfigurationSection( m, lookupParent );

        void Copy( MutableConfigurationSection target, IConfigurationSection s )
        {
            var t = target.GetMutableSection( s.Key );
            if( s.Value != null ) t.Value = resolved.TryGetValue( s.Path, out var r ) ? r : s.Value;
            foreach( var c in s.GetChildren() ) Copy( t, c );
        }
    }

    /// <summary>
    /// Resolves the placeholders of all the values below a section for an instance: the unqualified ones with the
    /// <paramref name="instance"/>, except in an inner template (a sub section that is a template: only its qualified
    /// placeholders are resolved). The "InstanceCount", "Instances" and "MaxInstanceCount" of the section itself are not
    /// values to resolve.
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
            if( IsTemplateKey( c.Key ) ) continue;
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

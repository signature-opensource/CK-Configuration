using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace CK.Core
{
    /// <summary>
    /// Captures "DefaultAssembly" and "Assemblies" configurations hierarchically.
    /// This is an immutable instance that can be obtained by <see cref="Create(IActivityMonitor, ImmutableConfigurationSection?)"/>
    /// or by applying a subordinate configuration with <see cref="Apply(IActivityMonitor, ImmutableConfigurationSection?)"/>.
    /// </summary>
    public sealed class AssemblyConfiguration
    {
        readonly string? _defaultAssemblyName;
        readonly IReadOnlyDictionary<string, string> _assemblies;
        readonly AssemblyConfiguration _parent;
        readonly bool _isLocked;

        internal AssemblyConfiguration( AssemblyConfiguration? parent, string? defaultAssemblyName, IReadOnlyDictionary<string, string> assemblies, bool isLocked )
        {
            _defaultAssemblyName = defaultAssemblyName;
            _assemblies = assemblies;
            _parent = parent ?? this;
            _isLocked = isLocked;
        }

        /// <summary>
        /// The empty configuration singleton.
        /// </summary>
        public readonly static AssemblyConfiguration Empty = new AssemblyConfiguration( null, null, ImmutableDictionary<string, string>.Empty, false );

        /// <summary>
        /// Gets whether this assembly configuration is locked.
        /// Once locked, subordinated configurations cannot alter the <see cref="DefaultAssembly"/> nor <see cref="Assemblies"/>.
        /// </summary>
        public bool IsLocked => _isLocked;

        /// <summary>
        /// Get the default assembly name.
        /// <para>
        /// This can be redefined by a subordinated section and resets with a "null" value. 
        /// </para>
        /// </summary>
        public string? DefaultAssembly => _defaultAssemblyName;

        /// <summary>
        /// Gets the registered assemblies and their respective alias.
        /// <para>
        /// New assemblies can always be added and aliases can be redefined by subordinated sections.
        /// </para>
        /// </summary>
        public IReadOnlyDictionary<string, string> Assemblies => _assemblies;

        /// <summary>
        /// Gets the parent configuration. The root is always the <see cref="Empty"/> one.
        /// </summary>
        public AssemblyConfiguration ParentConfiguration => _parent;

        /// <summary>
        /// Tries to resolve a type name based on this assembly configuration.
        /// <para>
        /// This never throws: errors are emitted to the monitor.
        /// </para>
        /// <para>
        /// The <paramref name="typeName"/> is specified as:
        /// <list type="bullet">
        /// <item>
        /// A simple "nameOrFullName" or "nameOrFullName, assemblyName" (with a comma). When the "assemblyName" is specified,
        /// it must be the <see cref="DefaultAssembly"/> or appear in the <see cref="Assemblies"/>' keys.
        /// When the "assemblyName" is not specified the <see cref="DefaultAssembly"/> is used and must exist.
        /// </item>
        /// <item>
        /// If a dot '.' appears in the nameOrFullName, it is considered a full name.
        /// If <paramref name="allowOtherNamespace"/> is false (the default), the namespace MUST be the same as the <paramref name="typeNamespace"/>.
        /// </item>
        /// </list>
        /// If no dot '.' appears, then <paramref name="typeNamespace"/> will be prepended to obtain the full name.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="typeName">The type name to resolve. When empty or whitespace, an error is logged and null is returned.</param>
        /// <param name="fallbackDefaultAssembly">
        /// Optional assembly used when a type name without specified assembly is not found in the <see cref="DefaultAssembly"/>
        /// (or there is no default assembly). 
        /// </param>
        /// <param name="typeNamespace">
        /// Required namespace that will be prepended to the <paramref name="typeName"/> if there is no '.' in it.
        /// This must not be empty or whitespace.
        /// </param>
        /// <param name="isOptional">True to not emit an error if the type is not found. Must be used when an alternative is possible.</param>
        /// <param name="allowOtherNamespace">
        /// True to allow type names in other namespaces than <paramref name="typeNamespace"/>.
        /// </param>
        /// <param name="familyTypeNameSuffix">
        /// Type suffix that will be appended to the <paramref name="typeName"/> if it doesn't
        /// already end with it.
        /// <para>
        /// Example: with "Strategy", a "Simple" type name will be "SimpleStrategyConfiguration"
        /// (the default <paramref name="typeNameSuffix"/> being "Configuration").
        /// </para>
        /// </param>
        /// <param name="typeNameSuffix">
        /// Required type name suffix. This is automatically appended to <paramref name="typeName"/> if missing.
        /// </param>
        /// <param name="errorPrefix">Optional string that will be prepended to the logged error if any.</param>
        /// <param name="errorSuffix">Optional string that will appendend to the logged error if any.</param>
        /// <returns>The resolved type or null on error.</returns>
        public Type? TryResolveType( IActivityMonitor monitor,
                                     string typeName,
                                     string typeNamespace,
                                     bool isOptional = false,
                                     Assembly? fallbackDefaultAssembly = null,
                                     bool allowOtherNamespace = false,
                                     string? familyTypeNameSuffix = null,
                                     string typeNameSuffix = "Configuration",
                                     Func<string>? errorPrefix = null,
                                     Func<string>? errorSuffix = null )
        {
            Throw.CheckNotNullArgument( monitor );
            Throw.CheckArgument( string.IsNullOrWhiteSpace( typeNamespace ) is false );
            Throw.CheckNotNullArgument( typeNameSuffix );

            ReadOnlySpan<char> tName = typeName.AsSpan().Trim();
            if( tName.Length == 0 )
            {
                monitor.Error( $"{errorPrefix?.Invoke()}Specified type name is invalid ('{typeName}').{errorSuffix?.Invoke()}" );
                return null;
            }

            #region Resolving assembly
            Assembly? assembly = null;
            int idxComma = tName.IndexOf( ',' );
            bool isDefaultAssembly = idxComma < 0;
            if( isDefaultAssembly )
            {
                if( _defaultAssemblyName == null )
                {
                    if( fallbackDefaultAssembly == null )
                    {
                        monitor.Error( $"{errorPrefix?.Invoke()}Type '{typeName}' doesn't specify any assembly and no DefaultAssembly exists.{errorSuffix?.Invoke()}" );
                        return null;
                    }
                }
                else
                {
                    assembly = LoadAssembly( monitor, _defaultAssemblyName, errorPrefix, errorSuffix );
                    if( assembly == null ) return null;
                    if( assembly == fallbackDefaultAssembly ) fallbackDefaultAssembly = null;
                }
            }
            else
            {
                var alias = tName.Slice( idxComma + 1 ).TrimStart().ToString();
                string? assemblyName;
                if( alias == _defaultAssemblyName ) assemblyName = alias;
                else if( !_assemblies.TryGetValue( alias, out assemblyName ) )
                {
                    monitor.Error( $"{errorPrefix?.Invoke()}Type '{typeName}' specifies assembly '{alias}' that doesn't exist. " +
                                   $"Defined Assemblies are '{_assemblies.Values.Concatenate( "', '" )}'" +
                                   $"{(_defaultAssemblyName == null ? "" : $", DefaultAssembly is '{_defaultAssemblyName}'.")}{errorSuffix?.Invoke()}" );
                    return null;
                }
                tName = tName.Slice( 0, idxComma ).TrimEnd();
                assembly = LoadAssembly( monitor, assemblyName, errorPrefix, errorSuffix );
                if( assembly == null ) return null;
            }
            #endregion

            Throw.DebugAssert( "assembly == null ==> isDefaultAssembly (and there is no default but we have a fallbackDefaultAssembly).",
                               assembly != null || isDefaultAssembly );

            bool hasNamespace = tName.Contains( '.' );
            if( hasNamespace && !allowOtherNamespace
                && (tName.Length < typeNamespace.Length || tName[typeNamespace.Length] != '.' || !tName.StartsWith( typeNamespace )) )
            {
                monitor.Error( $"{errorPrefix?.Invoke()}Type '{typeName}' must not specify a namespace or be in '{typeNamespace}' namespace.{errorSuffix?.Invoke()}" );
                return null;
            }
            bool hasSuffix = tName.EndsWith( typeNameSuffix );
            if( !hasSuffix && !string.IsNullOrEmpty( familyTypeNameSuffix ) )
            {
                bool hasFamilySuffix = tName.EndsWith( familyTypeNameSuffix );
                if( !hasFamilySuffix ) typeNameSuffix = $"{familyTypeNameSuffix}{typeNameSuffix}";
            }
            string finalTypeName = hasNamespace
                                    ? (hasSuffix ? tName.ToString() : $"{tName}{typeNameSuffix}")
                                    : (hasSuffix ? $"{typeNamespace}.{tName}" : $"{typeNamespace}.{tName}{typeNameSuffix}");

            if( assembly == null )
            {
                Throw.DebugAssert( isDefaultAssembly && _defaultAssemblyName == null && fallbackDefaultAssembly != null );
                return LoadType( monitor, fallbackDefaultAssembly, null, finalTypeName, isOptional, errorPrefix, errorSuffix );
            }
            if( isDefaultAssembly && fallbackDefaultAssembly != null )
            {
                return LoadType( monitor, assembly, fallbackDefaultAssembly, finalTypeName, isOptional, errorPrefix, errorSuffix );
            }
            return LoadType( monitor, assembly, null, finalTypeName, isOptional, errorPrefix, errorSuffix );
        }

        static Type? LoadType( IActivityMonitor monitor,
                               Assembly a1,
                               Assembly? a2,
                               string finalTypeName,
                               bool isOptional,
                               Func<string>? errorPrefix,
                               Func<string>? errorSuffix )
        {
            var t = SafeGetType( monitor, a1, finalTypeName );
            if( t == null && a2 != null ) t = SafeGetType( monitor, a2, finalTypeName );
            if( t == null && !isOptional )
            {
                monitor.Error( $"{errorPrefix?.Invoke()}" +
                                $"Unable to locate '{finalTypeName}' type from '{a1}'" +
                                $"{(a2 != null ? $" and '{a2}'" : "")}." +
                                $"{errorSuffix?.Invoke()}" );
            }
            return t;

            static Type? SafeGetType( IActivityMonitor monitor,
                                      Assembly assembly,
                                      string finalTypeName )
            {
                try
                {
                    return assembly.GetType( finalTypeName, throwOnError: false );
                }
                catch( Exception ex )
                {
                    monitor.Info( $"While getting type '{finalTypeName}' from '{assembly}'.", ex );
                    return null;
                }
            }
        }

        static Assembly? LoadAssembly( IActivityMonitor monitor, string name, Func<string>? errorPrefix, Func<string>? errorSuffix )
        {
            try
            {
                return Assembly.Load( name );
            }
            catch( Exception ex )
            {
                monitor.Error( $"{errorPrefix?.Invoke()}While loading assembly '{name}'.{errorSuffix?.Invoke()}", ex );
                return null;
            }
        }

        /// <summary>
        /// Tries to apply the given <paramref name="configuration"/> section to this <see cref="AssemblyConfiguration"/> and
        /// returns a new assembly configuration or null on error.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="configuration">The configuration to apply.</param>
        /// <returns>This or a new assembly configuration or null on error.</returns>
        public AssemblyConfiguration? Apply( IActivityMonitor monitor, ImmutableConfigurationSection? configuration )
        {
            Throw.CheckNotNullArgument( monitor );
            if( configuration == null || !configuration.HasChildren ) return this;
            string? defName = null;
            bool defRemoved = false;
            bool isLocked = _isLocked;
            Dictionary<string, string>? assemblies = null;
            bool success = true;
            using( monitor.OnError( () => success = false ) )
            {
                var cDef = configuration.TryGetSection( "DefaultAssembly" );
                if( cDef != null )
                {
                    if( isLocked )
                    {
                        WarnOnLocked( monitor, cDef.Path );
                    }
                    else
                    {
                        var name = cDef.Value;
                        if( string.IsNullOrWhiteSpace( name ) )
                        {
                            monitor.Warn( $"Configuration '{cDef.Path}': invalid empty DefaultAssembly. Ignored." );
                        }
                        else
                        {
                            if( defRemoved = (name == "null") )
                            {
                                defName = null;
                            }
                            else
                            {
                                defName = name;
                            }
                        }
                    }
                }
                var cAss = configuration.TryGetSection( "Assemblies" );
                if( cAss != null )
                {
                    if( _isLocked )
                    {
                        WarnOnLocked( monitor, cAss.Path );
                    }
                    else
                    {
                        var currentAssemblies = _assemblies;
                        HandleAssemblies( monitor, ref assemblies, cAss, ref isLocked, ref currentAssemblies );
                    }
                }
            }
            if( !success )
            {
                return null;
            }
            if( !_isLocked )
            {
                var newDef = defName ?? (defRemoved ? null : _defaultAssemblyName);
                if( newDef != _defaultAssemblyName || assemblies != null || isLocked )
                {
                    return new AssemblyConfiguration( this, newDef, assemblies ?? _assemblies, isLocked );
                }
            }
            return this;

        }

        /// <summary>
        /// Trie to create a <see cref="AssemblyConfiguration"/> from the given <paramref name="configuration"/> section. 
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="configuration">The configuration to analyze.</param>
        /// <returns>The assembly configuration or null on error.</returns>
        public static AssemblyConfiguration? Create( IActivityMonitor monitor, ImmutableConfigurationSection? configuration )
        {
            Throw.CheckNotNullArgument( monitor );
            if( configuration == null ) return Empty;
            bool isLocked = false;
            Dictionary<string, string>? assemblies = null;
            // First, use LookupAllSection to handle "Assemblies:IsLocked" (or "Assemblies:Locked" or "Assemblies:Lock")
            // definition from the top most "Assemblies" that sets it to true.
            ImmutableConfigurationSection? locked = null;
            int lockedDistance = -1;
            bool success = true;
            using( monitor.OnError( () => success = false ) )
            {
                foreach( var (c,d) in configuration.LookupAllSectionWithDistance( "Assemblies" ) )
                {
                    var currentAssemblies = Empty._assemblies;
                    HandleAssemblies( monitor, ref assemblies, c, ref isLocked, ref currentAssemblies );
                    if( isLocked && locked == null )
                    {
                        locked = c;
                        lockedDistance = d;
                    }
                    // When isLocked is true, continue so that warning can be emitted on skipped "Assemblies".
                }
            }
            // If an error has been signaled, stops.
            if( !success )
            {
                return null;
            }
            // If a "Assembly:IsLocked" has been found above, looks up the "DefaultAssembly" from it (skip any configuration below),
            // else starts from this configuration.
            string? defaultAssembly = configuration.TryLookupValue( "DefaultAssembly", out var defDistance );
            if( locked != null && locked != configuration )
            {
                defaultAssembly = locked.TryLookupValue( "DefaultAssembly" );
                Throw.DebugAssert( lockedDistance > 0 );
                int delta = lockedDistance - defDistance;
                if( delta > 0 )
                {
                    WarnOnLocked( monitor, configuration.GetParentPath( delta ) );
                }
            }
            else
            {
                defaultAssembly = configuration.TryLookupValue( "DefaultAssembly" );
            }
            return new AssemblyConfiguration( Empty, defaultAssembly, assemblies ?? Empty._assemblies, isLocked );
        }

        static void HandleAssemblies( IActivityMonitor monitor,
                                      ref Dictionary<string, string>? assemblies,
                                      ImmutableConfigurationSection c,
                                      ref bool isLocked,
                                      ref IReadOnlyDictionary<string, string> currentAssemblies )
        {
            if( isLocked )
            {
                WarnOnLocked( monitor, c.Path );
                return;
            }
            // Fist try on the "Assemblies" section itself.
            // If we find a Value, it's an assembly -> assembly or the value may be "IsLocked" (or "Locked" or "Lock").
            // If we find a {Assembly:xxx(,Alias:yyyy)?} it's an assembly alias/assembly -> assembly (that
            // may also contain a "IsLocked: true" (or "Locked" or "Lock").
            if( TryAddAssemblyEntry( monitor, ref assemblies, c, ref currentAssemblies, ref isLocked, c.GetParentPath() ) )
            {
                return;
            }
            // If the section has chidren, then it must be an array of assembly entries
            // or pairs of "Alias": "Assembly" or an array of assembly entries.
            // In all of these "IsLocked" is handled.
            if( c.HasChildren )
            {
                foreach( var child in c.GetChildren() )
                {
                    if( !child.Exists() ) continue;
                    if( int.TryParse( child.Key, out var _ ) )
                    {
                        // Allows "IsLocked" to appear in the array or a "IsLocked: true" in a {} whether there's also
                        // ""Assembly/Alias" or not.
                        if( !TryAddAssemblyEntry( monitor, ref assemblies, child, ref currentAssemblies, ref isLocked, c.GetParentPath() ) )
                        {
                            WarnUreadable( monitor, child );
                        }
                    }
                    else
                    {
                        var assembly = child.Key.Trim();
                        // Allows a "IsLocked: true" to occur among the assemblies.
                        if( IsLockedTerm( assembly ) )
                        {
                            ParseIsLockedBooleanValue( monitor, c, ref isLocked, c.GetParentPath(), child.Value );
                        }
                        else if( assembly.Length == 0 )
                        {
                            monitor.Error( $"Configuration '{c.Path}': invalid assembly name." );
                        }
                        else
                        {
                            var alias = child.Value;
                            if( string.IsNullOrWhiteSpace( alias ) )
                            {
                                monitor.Error( $"Configuration '{c.Path}': must have a alias name value, no subordinated configuration is allowed." );
                            }
                            else
                            {
                                AddAssembly( monitor, ref assemblies, c, ref currentAssemblies, alias, assembly );
                            }
                        }
                    }
                }
            }
            else
            {
                WarnUreadable( monitor, c );
            }

            static void WarnUreadable( IActivityMonitor monitor, ImmutableConfigurationSection c )
            {
                monitor.Warn( $"Configuration '{c.Path}': unable to read an assembly name, an assembly name and an alias or a list of such entries. Ignored." );
            }

        }

        // Returns whether a "IsLocked: true/false" has been handled.
        static bool HandleIsLocked( IActivityMonitor monitor, ImmutableConfigurationSection c, ref bool isLocked, ReadOnlySpan<char> lockSectionPath )
        {
            var value = c["IsLocked"] ?? c["Locked"] ?? c["Lock"];
            if( value != null )
            {
                ParseIsLockedBooleanValue( monitor, c, ref isLocked, lockSectionPath, value );
                return true;
            }
            return false;
        }

        static void ParseIsLockedBooleanValue( IActivityMonitor monitor,
                                               ImmutableConfigurationSection c,
                                               ref bool isLocked,
                                               ReadOnlySpan<char> lockSectionPath,
                                               string? value )
        {
            if( !bool.TryParse( value, out var newLock ) )
            {
                monitor.Warn( $"Invalid '{c}:IsLocked' (or 'Locked' or 'Lock') value '{value}'. In doubt, considering it to be 'true'." );
                newLock = true;
            }
            if( !isLocked && newLock )
            {
                monitor.Info( $"Allowed assemblies are locked below '{lockSectionPath}'." );
                isLocked = true;
            }
        }

        static bool IsLockedTerm( string value )
        {
            return value.Equals( "IsLocked", StringComparison.OrdinalIgnoreCase )
                    || value.Equals( "Locked", StringComparison.OrdinalIgnoreCase )
                    || value.Equals( "Lock", StringComparison.OrdinalIgnoreCase );
        }

        static void WarnOnLocked( IActivityMonitor monitor, ReadOnlySpan<char> ignoredPath )
        {
            monitor.Warn( $"Assemblies is locked. Ignoring '{ignoredPath}'." );
        }

        static bool TryAddAssemblyEntry( IActivityMonitor monitor,
                                         ref Dictionary<string, string>? assemblies,
                                         ImmutableConfigurationSection c,
                                         ref IReadOnlyDictionary<string, string> currentAssemblies,
                                         ref bool isLocked,
                                         ReadOnlySpan<char> lockSectionPath )
        {
            string? alias = null;
            var assembly = c.Value;
            if( !string.IsNullOrWhiteSpace( assembly ) )
            {
                // Allows "Assemblies: IsLocked".
                if( IsLockedTerm( assembly ) )
                {
                    if( !isLocked )
                    {
                        isLocked = true;
                        monitor.Info( $"Allowed assemblies are locked below '{lockSectionPath}'." );
                    }
                    return true;
                }
                alias = assembly;
            }
            else if( c.HasChildren )
            {
                assembly = c["Assembly"];
                if( !string.IsNullOrWhiteSpace( assembly ) )
                {
                    alias = c["Alias"];
                    if( string.IsNullOrWhiteSpace( alias ) )
                    {
                        alias = assembly;
                    }
                }
                // Allows a "IsLocked: true" to occur.
                // If we only read the "IsLocked" and there is no "Assembly", this is handled.
                if( HandleIsLocked( monitor, c, ref isLocked, lockSectionPath ) && alias == null )
                {
                    return true;
                }
            }
            if( alias == null )
            {
                // Nothing has been handled.
                return false;
            }
            AddAssembly( monitor, ref assemblies, c, ref currentAssemblies, alias, assembly );
            return true;
        }

        static void AddAssembly( IActivityMonitor monitor,
                                 ref Dictionary<string, string>? assemblies,
                                 ImmutableConfigurationSection c,
                                 ref IReadOnlyDictionary<string, string> currentAssemblies,
                                 string alias,
                                 string? assembly )
        {
            assembly ??= alias;
            if( currentAssemblies.TryGetValue( alias, out var assemblyExists ) )
            {
                if( assemblyExists.Equals( assembly, StringComparison.OrdinalIgnoreCase ) )
                {
                    monitor.Info( $"Configuration '{c.Path}': assembly '{alias} -> {assembly}' already exists." );
                }
                else
                {
                    monitor.Info( $"Configuration '{c.Path}': existing assembly '{alias} -> {assemblyExists}' is now mapped to ' -> {assembly}'." );
                    assemblies ??= new Dictionary<string, string>( currentAssemblies );
                    assemblies[alias] = assembly;
                    currentAssemblies = assemblies;
                }
            }
            else
            {
                monitor.Info( $"Added assembly '{alias} -> {assembly}' from '{c.Path}'." );
                assemblies ??= new Dictionary<string, string>( currentAssemblies );
                assemblies.Add( alias, assembly );
                currentAssemblies = assemblies;
            }
        }
    }
}

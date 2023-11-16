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
    /// <para>
    /// There is currently no way to lock a configuration so that subordinated "DefaultAssembly" or "Assemblies" section
    /// are considered errors. A "LockAssemblies" (which valu amy be "All", "Default" or "Assemblies") flag can be implemented
    /// once if needed.
    /// </para>
    /// </summary>
    public sealed class AssemblyConfiguration
    {
        readonly string? _defaultAssemblyName;
        readonly IReadOnlyDictionary<string, string> _assemblies;
        readonly AssemblyConfiguration _parent;

        internal AssemblyConfiguration( AssemblyConfiguration? parent, string? defaultAssemblyName, IReadOnlyDictionary<string, string> assemblies )
        {
            _defaultAssemblyName = defaultAssemblyName;
            _assemblies = assemblies;
            _parent = parent ?? this;
        }

        /// <summary>
        /// The empty configuration singleton.
        /// </summary>
        public readonly static AssemblyConfiguration Empty = new AssemblyConfiguration( null, null, ImmutableDictionary<string, string>.Empty );

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
        /// <param name="allowOtherNamespace">
        /// True to allow type names in other namespaces than <paramref name="typeNamespace"/>.
        /// </param>
        /// <param name="familyTypeNameSuffix">
        /// Optional type suffix that will be appended to the <paramref name="typeName"/> if it doesn't
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
                return LoadType( monitor, fallbackDefaultAssembly, null, finalTypeName, errorPrefix, errorSuffix );
            }
            if( isDefaultAssembly && fallbackDefaultAssembly != null )
            {
                return LoadType( monitor, assembly, fallbackDefaultAssembly, finalTypeName, errorPrefix, errorSuffix );
            }
            return LoadType( monitor, assembly, null, finalTypeName, errorPrefix, errorSuffix );
        }

        static Type? LoadType( IActivityMonitor monitor,
                               Assembly a1,
                               Assembly? a2,
                               string finalTypeName,
                               Func<string>? errorPrefix,
                               Func<string>? errorSuffix )
        {
            var t = SafeGetType( monitor, a1, finalTypeName );
            if( t == null && a2 != null ) t = SafeGetType( monitor, a2, finalTypeName );
            if( t == null )
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
            Dictionary<string, string>? assemblies = null;
            bool success = true;
            using( monitor.OnError( () => success = false ) )
            {
                foreach( var c in configuration.GetChildren() )
                {
                    if( !c.Exists() ) continue;
                    if( c.Key.Equals( "DefaultAssembly", StringComparison.OrdinalIgnoreCase ) )
                    {
                        var name = c.Value;
                        if( String.IsNullOrWhiteSpace( name ) )
                        {
                            monitor.Warn( $"Configuration '{c.Path}': invalid empty DefaultAssembly. Ignored." );
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
                    else if( c.Key.Equals( "Assemblies", StringComparison.OrdinalIgnoreCase ) )
                    {
                        var currentAssemblies = _assemblies;
                        HandleAssemblies( monitor, ref assemblies, c, ref currentAssemblies );
                    }
                }
            }
            if( !success )
            {
                return null;
            }
            var newDef = defName ?? (defRemoved ? null : _defaultAssemblyName);
            if( newDef != _defaultAssemblyName || assemblies != null )
            {
                return new AssemblyConfiguration( this, newDef, assemblies ?? _assemblies );
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
            Dictionary<string, string>? assemblies = null;
            bool success = true;
            using( monitor.OnError( () => success = false ) )
            {
                foreach( var c in configuration.LookupAllSection( "Assemblies" ) )
                {
                    var currentAssemblies = Empty._assemblies;
                    HandleAssemblies( monitor, ref assemblies, c, ref currentAssemblies );
                }
            }
            if( !success )
            {
                return null;
            }
            var def = configuration.TryLookupValue( "DefaultAssembly" );
            return new AssemblyConfiguration( Empty, def, assemblies ?? Empty._assemblies );
        }

        static void HandleAssemblies( IActivityMonitor monitor,
                                      ref Dictionary<string, string>? assemblies,
                                      ImmutableConfigurationSection c,
                                      ref IReadOnlyDictionary<string, string> currentAssemblies )
        {
            // Fist try on the "Assemblies" section itself.
            // If we find a Value, it's an assembly -> assembly.
            // If we find a {Assembly:xxx(,Alias:yyyy)?} it's an assembly alias/assembly -> assembly.
            if( TryAddAssemblyEntry( monitor, ref assemblies, c, ref currentAssemblies ) )
            {
                return;
            }
            // If the section has chidren, then it must be an array of assembly entries
            // or pairs of "Alias": "Assembly" or an array of assembly entries.
            if( c.HasChildren )
            {
                foreach( var child in c.GetChildren() )
                {
                    if( !child.Exists() ) continue;
                    if( int.TryParse( child.Key, out var _ ) )
                    {
                        if( !TryAddAssemblyEntry( monitor, ref assemblies, child, ref currentAssemblies ) )
                        {
                            WarnUreadable( monitor, child );
                        }
                    }
                    else
                    {
                        var assembly = child.Key.Trim();
                        if( assembly.Length == 0 )
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

        static bool TryAddAssemblyEntry( IActivityMonitor monitor,
                                         ref Dictionary<string, string>? assemblies,
                                         ImmutableConfigurationSection c,
                                         ref IReadOnlyDictionary<string, string> currentAssemblies )
        {
            string? alias = null;
            var assembly = c.Value;
            if( !string.IsNullOrWhiteSpace( assembly ) )
            {
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
            }
            if( alias == null )
            {
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

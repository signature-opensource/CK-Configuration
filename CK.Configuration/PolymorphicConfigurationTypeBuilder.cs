using Microsoft.Extensions.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;

namespace CK.Core
{
    /// <summary>
    /// Helper that can create configured instances for one or more family types.
    /// <para>
    /// Caution: this is a stateful object, concurrency is not supported.
    /// </para>
    /// </summary>
    public sealed partial class PolymorphicConfigurationTypeBuilder
    {
        readonly List<TypeResolver> _resolvers;
        AssemblyConfiguration _assemblyConfiguration;
        int _createDepth;
        List<(int Depth, int Index, TypeResolver Resolver)>? _hiddenResolvers;

        /// <summary>
        /// Initializes a new <see cref="PolymorphicConfigurationTypeBuilder"/>.
        /// </summary>
        public PolymorphicConfigurationTypeBuilder()
        {
            _resolvers = new List<TypeResolver>();
            _assemblyConfiguration = AssemblyConfiguration.Empty;
        }

        /// <summary>
        /// Initializes a new <see cref="PolymorphicConfigurationTypeBuilder"/> with an <see cref="AssemblyConfiguration"/>
        /// and <see cref="Resolvers"/>.
        /// <para>
        /// This is typically used by placeholders to restore the creation state at their position.
        /// </para>
        /// </summary>
        /// <param name="assemblyConfiguration">Known initial assembly configuration to use.</param>
        /// <param name="resolvers">Known resolvers to register.</param>
        public PolymorphicConfigurationTypeBuilder( AssemblyConfiguration assemblyConfiguration,
                                                    ImmutableArray<TypeResolver> resolvers )
        {
            _resolvers = resolvers.ToList();
            _assemblyConfiguration = assemblyConfiguration;
        }

        /// <summary>
        /// Gets whether at least one <see cref="Create(IActivityMonitor, Type, IConfigurationSection)"/> has been called.
        /// </summary>
        public bool IsCreating => _createDepth > 0;

        /// <summary>
        /// Gets the current list of resolvers.
        /// </summary>
        public IReadOnlyList<TypeResolver> Resolvers => _resolvers;

        /// <summary>
        /// Gets or sets the assembly configuration.
        /// <para>
        /// This is automatically restored to the initial value by <see cref="Create(IActivityMonitor, Type, IConfigurationSection)"/>.
        /// </para>
        /// </summary>
        public AssemblyConfiguration AssemblyConfiguration
        {
            get => _assemblyConfiguration;
            set
            {
                Throw.CheckNotNullArgument( value );
                _assemblyConfiguration = value;
            }
        }

        /// <summary>
        /// Adds a type resolver. 
        /// <para>
        /// Adding a resolver can be done while <see cref="IsCreating"/> is true. In this case the added resolver
        /// temporarily hides a previously registered resolver for the <see cref="TypeResolver.BaseType"/> until
        /// the currently called <see cref="Create(IActivityMonitor, Type, IConfigurationSection)"/> call ends.
        /// </para>
        /// <para>
        /// When <see cref="IsCreating"/> is false, the <see cref="TypeResolver.BaseType"/> must not already be
        /// handled by an already registered resolver otherwise an <see cref="ArgumentException"/> is thrown.
        /// </para>
        /// <para>
        /// Note that the same resolver instance can always be added muliple times.
        /// </para>
        /// </summary>
        /// <remarks>
        /// <see cref="TypeResolver.BaseType"/> is the key. Resolvers must be added from the most specific to the most
        /// general <see cref="TypeResolver.BaseType"/> (following <see cref="Type.IsAssignableFrom(Type?)"/>): this is like a
        /// switch case pattern matching. Please note that even if this works, this is a weird edge case: families' base type are
        /// usually independent from each other (B1.IsAssignableFrom(B2) and B2.IsAssignableFrom(B1) are both false).
        /// </remarks>
        /// <param name="resolver">The resolver to add.</param>
        public void AddResolver( TypeResolver resolver )
        {
            Throw.CheckNotNullArgument( resolver );
            int idx = _resolvers.IndexOf( b => resolver.BaseType.IsAssignableFrom( b.BaseType ) );
            if( idx < 0 )
            {
                // No conflict: always append the resolver, Create will truncate the list at its initial length.
                _resolvers.Add( resolver );
            }
            else
            {
                // Conflict?
                // Allows the same resolver instance to be added multiple times: this is a no-op idempotent
                // operation.
                if( resolver != _resolvers[idx] )
                {
                    if( !IsCreating )
                    {
                        Throw.ArgumentException( nameof( resolver ), $"A resolver for base type '{resolver.BaseType:N}' is already registered." );
                    }
                    // Registers the override and substitutes the new resolver.
                    _hiddenResolvers ??= new List<(int, int, TypeResolver)>();
                    _hiddenResolvers.Add( (_createDepth, idx, _resolvers[idx]) );
                    _resolvers[idx] = resolver;
                }
            }
        }

        /// <summary>
        /// Typed version of the <see cref="Create(IActivityMonitor, Type, IConfigurationSection)"/> method.
        /// </summary>
        /// <typeparam name="T">Type of the expected instance.</typeparam>
        /// <param name="monitor">The monitor that will be used to signal errors and warnings.</param>
        /// <param name="configuration">The configuration to process.</param>
        /// <returns>The configured object or null if any error occurred.</returns>
        public T? Create<T>( IActivityMonitor monitor, IConfigurationSection configuration )
        {
            return (T?)Create( monitor, typeof( T ), configuration );
        }

        /// <summary>
        /// Typed version of the <see cref="CreateItems(IActivityMonitor, Type, ImmutableConfigurationSection)"/> method.
        /// </summary>
        /// <typeparam name="T">Base type of the expected instances.</typeparam>
        /// <param name="monitor">The monitor that will be used to signal errors and warnings.</param>
        /// <param name="configuration">The composite configuration.</param>
        /// <param name="requiresItemsFieldName">
        /// True to requires the "Items" (or <paramref name="alternateItemsFieldName"/>) field name.
        /// By default even if no "Items" appears in the <paramref name="configuration"/>, an empty list is returned.
        /// </param>
        /// <param name="alternateItemsFieldName">
        /// Optional "Items" field names with the subordinated items. When let to null or when not found, the default composite "Items" field name
        /// used by this resolver must be used.
        /// </param>
        /// <returns>The resulting list or null if any error occurred.</returns>
        public IReadOnlyList<T>? CreateItems<T>( IActivityMonitor monitor,
                                                 ImmutableConfigurationSection configuration,
                                                 bool requiresItemsFieldName = false,
                                                 string? alternateItemsFieldName = null )
        {
            Throw.CheckNotNullArgument( configuration );
            TypeResolver resolver = FindRequiredResolver( monitor, typeof(T) );
            return (IReadOnlyList<T>?)resolver.CreateItems( monitor, this, configuration, requiresItemsFieldName, alternateItemsFieldName );
        }

        /// <summary>
        /// Attempts to instantiate items of the composite type based on the <see cref="Resolvers"/> and
        /// the <paramref name="configuration"/> configuration.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors and warnings.</param>
        /// <param name="baseType">The items base type.</param>
        /// <param name="configuration">The items configuration.</param>
        /// <param name="requiresItemsFieldName">
        /// True to requires the "Items" (or <paramref name="alternateItemsFieldName"/>) field name.
        /// By default even if no "Items" appears in the <paramref name="configuration"/>, an empty list is returned.
        /// </param>
        /// <param name="alternateItemsFieldName">
        /// Optional "Items" field names with the subordinated items. When let to null or when not found, the default composite "Items" field name
        /// used by this resolver must be used.
        /// </param>
        /// <returns>The resulting list or null if any error occurred.</returns>
        public IReadOnlyList<object>? CreateItems( IActivityMonitor monitor,
                                                   Type baseType,
                                                   ImmutableConfigurationSection configuration,
                                                   bool requiresItemsFieldName = false,
                                                   string? alternateItemsFieldName = null )
        {
            Throw.CheckNotNullArgument( configuration );
            TypeResolver resolver = FindRequiredResolver( monitor, baseType );
            return (IReadOnlyList<object>?)resolver.CreateItems( monitor, this, configuration, requiresItemsFieldName, alternateItemsFieldName );
        }

        /// <summary>
        /// Attempts to instantiate an object of type <paramref name="type"/> based on the <see cref="Resolvers"/> and the <paramref name="configuration"/>.
        /// <para>
        /// Note that <see cref="AssemblyConfiguration"/> and <see cref="Resolvers"/> are restored to their initial values once the call ends:
        /// these can be altered before any reentrant calls without side effects.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor that will be used to signal errors and warnings.</param>
        /// <param name="type">The expected resulting instance type.</param>
        /// <param name="configuration">The configuration to process.</param>
        /// <returns>The configured object or null on error.</returns>
        public object? Create( IActivityMonitor monitor, Type type, IConfigurationSection configuration )
        {
            Throw.CheckNotNullArgument( configuration );
            TypeResolver resolver = FindRequiredResolver( monitor, type );
            if( configuration is not ImmutableConfigurationSection config )
            {
                config = new ImmutableConfigurationSection( configuration );
            }
            var initialAssembly = _assemblyConfiguration;
            var initialResolverCount = _resolvers.Count;
            ++_createDepth;
            bool success = true;
            using var detector = monitor.OnError( () => success = false );
            try
            {
                var result = resolver.Create( monitor, this, config );
                if( result == null )
                {
                    // Ensures that an error has been emitted.
                    if( success ) monitor.Error( $"Resolver returned a null '{resolver.BaseType:C}' instance." );
                }
                else if( !resolver.BaseType.IsAssignableFrom( result.GetType() ) )
                {
                    monitor.Error( $"Resolver created a '{result.GetType():C}' instance. Expected a '{resolver.BaseType:C}' instance." );
                    result = null;
                }
                Throw.DebugAssert( success == (result != null) );
                return result;
            }
            catch( Exception ex )
            {
                monitor.Error( $"While creating '{type:C}'.", ex );
                return null;
            }
            finally
            {
                // Restores the initial assembly configuration, truncates the resolver list to its initial size
                // and handles the temporarily substituted resolvers if any.
                _assemblyConfiguration = initialAssembly;
                _resolvers.RemoveRange( initialResolverCount, _resolvers.Count - initialResolverCount );
                if( _hiddenResolvers != null )
                {
                    for( int i = _hiddenResolvers.Count - 1; i >= 0; i-- )
                    {
                        var (depth, index, previous) = _hiddenResolvers[ i ];
                        Throw.DebugAssert( depth <= _createDepth );
                        if( depth < _createDepth ) break;
                        _resolvers[index] = previous;
                        _hiddenResolvers.RemoveAt( index );
                    }
                }
                --_createDepth;
            }
        }

        private TypeResolver FindRequiredResolver( IActivityMonitor monitor, Type type )
        {
            Throw.CheckNotNullArgument( monitor );
            Throw.CheckNotNullArgument( type );
            var resolver = _resolvers.FirstOrDefault( r => r.BaseType.IsAssignableFrom( type ) );
            if( resolver == null )
            {
                Throw.ArgumentException( nameof( type ), $"Unable to find a resolver for '{type:C}' ({_resolvers.Count} resolvers)." );
            }

            return resolver;
        }
    }
}

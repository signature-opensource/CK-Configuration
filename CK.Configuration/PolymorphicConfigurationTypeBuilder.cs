using Microsoft.Extensions.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CK.Core
{
    /// <summary>
    /// Helper that can create configured instances for one or more family types.
    /// <para>
    /// Caution: this is a stateful object, concurrency is not supported.
    /// </para>
    /// <para>
    /// This can be specialized, typically to offer more context while instatiating objects.
    /// </para>
    /// </summary>
    public partial class PolymorphicConfigurationTypeBuilder
    {
        readonly List<TypeResolver> _resolvers;
        AssemblyConfiguration _assemblyConfiguration;
        int _createDepth;

        /// <summary>
        /// Initializes a new <see cref="PolymorphicConfigurationTypeBuilder"/>.
        /// </summary>
        /// <param name="initialAssemblyConfiguration">Optional root assembly configuration to use.</param>
        public PolymorphicConfigurationTypeBuilder( AssemblyConfiguration? initialAssemblyConfiguration = null )
        {
            _resolvers = new List<TypeResolver>();
            _assemblyConfiguration = initialAssemblyConfiguration ?? AssemblyConfiguration.Empty;
        }

        /// <summary>
        /// Gets the current list of resolvers.
        /// <para>
        /// Adding a new <see cref="TypeResolver"/> is done by the TypeResolver's constructor.
        /// Resolvers must be added from the most specific to the most general <see cref="TypeResolver.BaseType"/>
        /// (following <see cref="Type.IsAssignableFrom(Type?)"/>): this is like a switch case pattern matching.
        /// </para>
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
        /// Gets whether at least one <see cref="Create(IActivityMonitor, Type, IConfigurationSection)"/> has been called.
        /// </summary>
        public bool IsCreating => _createDepth > 0;

        /// <summary>
        /// Typed version of the <see cref="Create(IActivityMonitor, Type, IConfigurationSection)"/> method.
        /// </summary>
        /// <typeparam name="T">Type of the expected instance.</typeparam>
        /// <param name="monitor">The monitor that will be used to signal errors and warnings.</param>
        /// <param name="configuration">The configuration to process.</param>
        /// <returns>The configured object or null on error.</returns>
        public T? Create<T>( IActivityMonitor monitor, IConfigurationSection configuration )
        {
            return (T?)Create( monitor, typeof( T ), configuration );
        }

        /// <summary>
        /// Instantiate an object of type <paramref name="type"/> based on the <see cref="Resolvers"/> and the <paramref name="configuration"/>.
        /// <para>
        /// Note that <see cref="AssemblyConfiguration"/> and <see cref="Resolvers"/> are restored to their initial values once the call ends:
        /// these can be altered before any reentrant calls without side effects.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor that will be used to signal errors and warnings.</param>
        /// <param name="type">The expected resulting instance type.</param>
        /// <param name="configuration">The configuration to process.</param>
        /// <returns>The configured object or null on error.</returns>
        public virtual object? Create( IActivityMonitor monitor,
                                       Type type,
                                       IConfigurationSection configuration )
        {
            Throw.CheckNotNullArgument( monitor );
            Throw.CheckNotNullArgument( type );
            Throw.CheckNotNullArgument( configuration );
            var resolver = _resolvers.FirstOrDefault( r => r.BaseType.IsAssignableFrom( type ) );
            if( resolver == null )
            {
                Throw.ArgumentException( nameof( type ), $"Unable to find a resolver for '{type:C}' ({_resolvers.Count} resolvers)." );
            }
            if( configuration is not ImmutableConfigurationSection config )
            {
                config = new ImmutableConfigurationSection( configuration );
            }
            var initialAssembly = _assemblyConfiguration;
            var initialResolverCount = _resolvers.Count;
            ++_createDepth;
            bool success = true;
            using var detector = monitor.OnError( () => success = false );
            _assemblyConfiguration = _assemblyConfiguration.Apply( monitor, config ) ?? _assemblyConfiguration;
            try
            {
                var result = resolver.Create( monitor, config );
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
                --_createDepth;
                _assemblyConfiguration = initialAssembly;
                _resolvers.RemoveRange( initialResolverCount, _resolvers.Count - initialResolverCount );
            }
        }
    }
}

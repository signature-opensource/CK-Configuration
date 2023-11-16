using Microsoft.Extensions.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CK.Core
{
    public partial class PolymorphicConfigurationTypeBuilder
    {
        readonly Stack<AssemblyConfiguration> _assemblyConfigurations;
        readonly List<TypeResolver> _resolvers;

        /// <summary>
        /// Initializes a new <see cref="PolymorphicConfigurationTypeBuilder"/>.
        /// </summary>
        /// <param name="initialAssemblyConfiguration">Optional root assembly configuration to use.</param>
        public PolymorphicConfigurationTypeBuilder( AssemblyConfiguration? initialAssemblyConfiguration = null )
        {
            _assemblyConfigurations = new Stack<AssemblyConfiguration>();
            _assemblyConfigurations.Push( initialAssemblyConfiguration ?? AssemblyConfiguration.Empty );
            _resolvers = new List<TypeResolver>();
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
        /// Adds a <see cref="StandardTypeResolver"/>.
        /// </summary>
        /// <param name="baseType">
        /// The base type that generalizes all the types that will be handled by this resolver.
        /// </param>
        /// <param name="fieldName">The name of the "Type" field (usually "Type").</param>
        /// <param name="typeNamespace">
        /// Required namespace that will be prepended to the type name read from <paramref name="fieldName"/> if
        /// there is no '.' in it. This must not be empty or whitespace.
        /// </param>
        /// <param name="allowOtherNamespace">
        /// True to allow type names in other namespaces than <paramref name="typeNamespace"/>.
        /// </param>
        /// <param name="familyTypeNameSuffix">
        /// Optional type suffix that will be appended to the type name read from <paramref name="fieldName"/>
        /// if it doesn't already end with it.
        /// <para>
        /// Example: with "Strategy", a "Simple" type name will be "SimpleStrategyConfiguration"
        /// (the default <paramref name="typeNameSuffix"/> being "Configuration").
        /// </para>
        /// </param>
        /// <param name="compositeBaseType">Optional specialized type that is the default composite.</param>
        /// <param name="compositeItemsFieldName">Required field name of a composite items.</param>
        /// <param name="typeNameSuffix">
        /// Required type name suffix. This is automatically appended to <paramref name="typeName"/> if missing.
        /// </param>
        public void AddStandardTypeResolver( Type baseType,
                                             string fieldName,
                                             string typeNamespace,
                                             bool allowOtherNamespace = false,
                                             string? familyTypeNameSuffix = null,
                                             Type? compositeBaseType = null,
                                             string compositeItemsFieldName = "Items",
                                             string typeNameSuffix = "Configuration" )
        {
            var _ = new StandardTypeResolver( this,
                                               baseType,
                                              fieldName,
                                              typeNamespace,
                                              allowOtherNamespace,
                                              familyTypeNameSuffix,
                                              compositeBaseType,
                                              compositeItemsFieldName,
                                              typeNameSuffix );
        }

        /// <summary>
        /// Gets the current assembly configuration.
        /// </summary>
        public AssemblyConfiguration CurrentAssemblyConfiguration => _assemblyConfigurations.Peek();

        /// <summary>
        /// This can be called when entering a section that can define "Assemblies" and "DefaultAssembly" configurations.
        /// <see cref="PopAssemblyConfiguration"/> must be called to restore the <see cref="CurrentAssemblyConfiguration"/>.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="section">The entered section.</param>
        /// <returns>The new <see cref="CurrentAssemblyConfiguration"/>.</returns>
        public AssemblyConfiguration PushAssemblyConfiguration( IActivityMonitor monitor, ImmutableConfigurationSection section )
        {
            var c = CurrentAssemblyConfiguration;
            var a = c.Apply( monitor, section ) ?? c;
            _assemblyConfigurations.Push( a );
            return a;
        }

        /// <summary>
        /// Restores the <see cref="CurrentAssemblyConfiguration"/> before the last
        /// call to <see cref="PushAssemblyConfiguration(IActivityMonitor, ImmutableConfigurationSection)"/>.
        /// </summary>
        public void PopAssemblyConfiguration()
        {
            _assemblyConfigurations.Pop();
        }

        /// <summary>
        /// Typed version of the <see cref="TryCreate(IActivityMonitor, Type, IConfigurationSection, out object?, bool)"/> method.
        /// </summary>
        /// <typeparam name="T">Type of the expected instance.</typeparam>
        /// <param name="monitor">The monitor that will be used to signal errors and warnings.</param>
        /// <param name="configuration">The configuration to process.</param>
        /// <param name="result">The resolved instance or null on error or if the configuration should not give birth to an instance.</param>
        /// <param name="required">False to allow the configuration to be "null" or empty.</param>
        /// <returns>True on success, false on error.</returns>
        public bool TryCreate<T>( IActivityMonitor monitor, IConfigurationSection configuration, out T? result, bool required = true )
        {
            if( TryCreate( monitor, typeof(T), configuration, out var r, required ) )
            {
                result = (T?)r;
                return true;
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Tries to instantiate an object of type <paramref name="type"/> from the <see cref="Resolvers"/> and the <paramref name="configuration"/>.
        /// <para>
        /// Error management relies on the <paramref name="monitor"/>. Use <see cref="ActivityMonitorExtension.OnError(IActivityMonitor, Action)"/>
        /// for instance to track any error.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor that will be used to signal errors and warnings.</param>
        /// <param name="type">The expected resulting instance type.</param>
        /// <param name="configuration">The configuration to process.</param>
        /// <param name="result">The resolved instance or null on error or if the configuration should not give birth to an instance.</param>
        /// <param name="required">False to allow the configuration to be "null" or empty.</param>
        /// <returns>True on success, false on error.</returns>
        public bool TryCreate( IActivityMonitor monitor,
                               Type type,
                               IConfigurationSection configuration,
                               out object? result,
                               bool required = true )
        {
            Throw.CheckNotNullArgument( monitor );
            Throw.CheckNotNullArgument( type );
            Throw.CheckNotNullArgument( configuration );
            result = null;
            if( !configuration.Exists() || configuration.Value == "null" )
            {
                if( required ) monitor.Error( $"Required configuration '{configuration.Path}' (must be a '{type:C}')." );
                return false;
            }
            if( configuration is not ImmutableConfigurationSection config )
            {
                config = new ImmutableConfigurationSection( configuration );
            }
            var resolver = _resolvers.FirstOrDefault( r => r.BaseType.IsAssignableFrom( type ) );
            if( resolver == null )
            {
                Throw.ArgumentException( nameof( type ), $"Unable to find a resolver for '{type:C}' ({_resolvers.Count} resolvers)." );
            }
            bool success = true;
            using var detector = monitor.OnError( () => success = false );
            PushAssemblyConfiguration( monitor, config );
            try
            {
                var o = resolver.Create( monitor, config );
                if( o != null && !resolver.BaseType.IsAssignableFrom( o.GetType() ) )
                {
                    monitor.Error( $"Resolver created a '{o.GetType():C}' instance. Expected a '{resolver.BaseType:C}' instance." );
                }
                if( success )
                {
                    result = o;
                    return true;
                }
                return false;
            }
            catch ( Exception ex )
            {
                monitor.Error( $"While creating '{type:C}'.", ex );
                return false;
            }
            finally
            {
                PopAssemblyConfiguration();
            }
        }
    }
}

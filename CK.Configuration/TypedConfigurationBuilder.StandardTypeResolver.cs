using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CK.Core
{
    public sealed partial class TypedConfigurationBuilder
    {
        /// <summary>
        /// Standard type resolver.
        /// Note that composite support is optional.
        /// </summary>
        public sealed class StandardTypeResolver : TypeResolver
        {
            readonly string _typeFieldName;
            readonly string _typeNamespace;
            readonly bool _allowOtherNamespace;
            readonly string? _familyTypeNameSuffix;
            readonly string _typeNameSuffix;
            readonly TypeResolver? _fallback;
            readonly Type? _defaultCompositeBaseType;
            readonly Func<IActivityMonitor, string, TypedConfigurationBuilder, ImmutableConfigurationSection, object?>? _tryCreateFromTypeName;

            /// <summary>
            /// Initializes a new <see cref="StandardTypeResolver"/>.
            /// </summary>
            /// <param name="baseType">
            /// The base type that generalizes all the types that will be handled by this resolver.
            /// </param>
            /// <param name="typeNamespace">
            /// Required namespace that will be prepended to the type name read from <paramref name="typeFieldName"/> if
            /// there is no '.' in it. This must not be empty or whitespace.
            /// </param>
            /// <param name="allowOtherNamespace">
            /// True to allow type names in other namespaces than <paramref name="typeNamespace"/>.
            /// </param>
            /// <param name="familyTypeNameSuffix">
            /// Type suffix that will be appended to the type name read from <paramref name="typeFieldName"/>
            /// if it doesn't already end with it.
            /// <para>
            /// Example: with "Strategy", a "Simple" type name will be "SimpleStrategyConfiguration"
            /// (the default <paramref name="typeNameSuffix"/> being "Configuration").
            /// </para>
            /// </param>
            /// <param name="tryCreateFromTypeName">
            /// Optional factory that can create a configured object only from its type name. This enables
            /// shortcuts to be implemented.
            /// <para>
            /// This factory must either:
            /// <list type="bullet">
            /// <item>Returns null AND emit an error or a fatal log into the monitor.</item>
            /// <item>OR returns a non null object AND NOT emit any error.</item>
            /// </list>
            /// </para>
            /// </param>
            /// <param name="defaultCompositeBaseType">Optional specialized type that is the default composite.</param>
            /// <param name="compositeItemsFieldName">Required field name of a composite items.</param>
            /// <param name="typeFieldName">The name of the "Type" field.</param>
            /// <param name="typeNameSuffix">
            /// Required type name suffix. This is automatically appended to the type name read from <paramref name="typeFieldName"/> if missing.
            /// </param>
            /// <param name="fallback">
            /// Optional fallback if the type cannot be resolved. This is an advanced usage.
            /// </param>
            public StandardTypeResolver( Type baseType,
                                         string typeNamespace,
                                         bool allowOtherNamespace = false,
                                         string? familyTypeNameSuffix = null,
                                         Func<IActivityMonitor, string, TypedConfigurationBuilder, ImmutableConfigurationSection, object?>? tryCreateFromTypeName = null,
                                         Type? defaultCompositeBaseType = null,
                                         string compositeItemsFieldName = "Items",
                                         string typeFieldName = "Type",
                                         string typeNameSuffix = "Configuration",
                                         TypeResolver? fallback = null )
                : base( baseType, compositeItemsFieldName )
            {
                Throw.CheckNotNullArgument( baseType );
                Throw.CheckNotNullOrWhiteSpaceArgument( typeFieldName );
                Throw.CheckNotNullOrWhiteSpaceArgument( typeNamespace );
                Throw.CheckArgument( defaultCompositeBaseType == null || (defaultCompositeBaseType.IsClass && !defaultCompositeBaseType.IsAbstract && baseType.IsAssignableFrom( defaultCompositeBaseType )) );
                _typeFieldName = typeFieldName;
                _typeNamespace = typeNamespace;
                _allowOtherNamespace = allowOtherNamespace;
                _familyTypeNameSuffix = familyTypeNameSuffix;
                _tryCreateFromTypeName = tryCreateFromTypeName;
                _defaultCompositeBaseType = defaultCompositeBaseType;
                _typeNameSuffix = typeNameSuffix;
                _fallback = fallback;
            }

            /// <summary>
            /// Attempts to create an instance from a configuration.
            /// </summary>
            /// <param name="monitor">The monitor that must be used to signal errors and warnings.</param>
            /// <param name="builder">The calling builder for which the configuration must be resolved.</param>
            /// <param name="configuration">The configuration to analyze.</param>
            /// <returns>The resulting instance or null if any error occurred.</returns>
            internal protected override object? Create( IActivityMonitor monitor,
                                                        TypedConfigurationBuilder builder,
                                                        ImmutableConfigurationSection configuration )
            {
                return DoCreateWithFallback( monitor, builder, configuration, _defaultCompositeBaseType != null );
            }

            object? DoCreateWithFallback( IActivityMonitor monitor,
                                          TypedConfigurationBuilder builder,
                                          ImmutableConfigurationSection configuration,
                                          bool allowDefaultComposite )
            {
                object? o;
                bool hasError = false;
                using( monitor.OnError( () => hasError = true ) )
                {
                    o = DoCreate( monitor, builder, configuration, allowDefaultComposite );
                }
                if( hasError ) return null;
                if( o == null && _fallback != null )
                {
                    o = _fallback.Create( monitor, builder, configuration );
                }
                return o;
            }

            object? DoCreate( IActivityMonitor monitor,
                              TypedConfigurationBuilder builder,
                              ImmutableConfigurationSection configuration,
                              bool allowDefaultComposite )
            {
                // If it's a section then it may define assemblies.
                var previousAssemblies = builder.AssemblyConfiguration;
                builder.AssemblyConfiguration = builder.AssemblyConfiguration.Apply( monitor, configuration ) ?? previousAssemblies;
                try
                {
                    // First, check if the configuration is a value and if it is the case, we have no other choice
                    // to use the optional tryCreateFromTypeName. 
                    if( configuration.Value != null )
                    {
                        return CreateFromValue( monitor, builder, configuration, configuration.Value );
                    }
                    // Lookup the "Type" field.
                    var typeName = configuration[_typeFieldName];
                    // If it exists and the optional tryCreateFromTypeName exists then give it a try.
                    if( typeName != null && _tryCreateFromTypeName != null )
                    {
                        object? result = TryCreateFromTypeName( monitor, builder, configuration, typeName, out bool hasError );
                        // If the result has been created or an error occurred, we're done.
                        if( result != null || hasError ) return result;
                    }
                    if( typeName == null )
                    {
                        // When no "Type" field is specified and if the family has a composite, we consider the default composite.
                        return CreateWithNoTypeName( monitor, builder, configuration, allowDefaultComposite );
                    }
                    return CreateWithTypeName( monitor, builder, configuration, typeName );
                }
                catch( Exception ex )
                {
                    monitor.Error( $"While instantiating from configuration '{configuration.Path}.", ex );
                    return null;
                }
                finally
                {
                    builder.AssemblyConfiguration = previousAssemblies;
                }
            }
        
            object? CreateWithTypeName( IActivityMonitor monitor,
                                        TypedConfigurationBuilder builder,
                                        ImmutableConfigurationSection configuration,
                                        string typeName )
            {
                var type = builder.AssemblyConfiguration.TryResolveType( monitor,
                                                                         typeName,
                                                                         _typeNamespace,
                                                                         isOptional: _fallback != null,
                                                                         fallbackAssembly: BaseType.Assembly,
                                                                         _allowOtherNamespace,
                                                                         _familyTypeNameSuffix,
                                                                         _typeNameSuffix,
                                                                         null,
                                                                         () => $" (Configuration '{configuration.Path}:{_typeFieldName}'.)" );
                if( type == null ) return null;
                if( !BaseType.IsAssignableFrom( type ) )
                {
                    monitor.Error( ActivityMonitor.Tags.ToBeInvestigated,
                                   $"The '{typeName}' type name resolved to '{type:N}' but this type is not compatible with '{BaseType:N}'. " +
                                   $"(Configuration '{configuration.Path}:{_typeFieldName}'.)" );
                    return null;
                }
                return CoreCreate( monitor, builder, configuration, type, null );
            }

            object? CreateWithNoTypeName( IActivityMonitor monitor,
                                          TypedConfigurationBuilder builder,
                                          ImmutableConfigurationSection configuration,
                                          bool allowDefaultComposite )
            {
                if( _defaultCompositeBaseType != null )
                {
                    // When allowDefaultComposite (root Create call), if there's no "Items" field
                    // we consider the configuration itself to be the composite items.
                    // This allows array (or even keyed objects) to be handled. This trick is safe
                    // since we check that the configuration has children and in such case, children
                    // configurations must be valid definitions.
                    var itemsField = configuration.TryGetSection( CompositeItemsFieldName );
                    if( itemsField == null && allowDefaultComposite )
                    {
                        if( !configuration.HasChildren )
                        {
                            monitor.Error( $"Configuration '{configuration.Path}' must have children to be considered a default '{_defaultCompositeBaseType:C}'." );
                            return null;
                        }
                        itemsField = configuration;
                    }
                    if( itemsField != null )
                    {
                        return CoreCreate( monitor, builder, configuration, _defaultCompositeBaseType, itemsField );
                    }
                    monitor.Warn( $"Missing '{configuration.Path}:{CompositeItemsFieldName}' to define a composite." );
                }
                monitor.Error( $"Missing required '{configuration.Path}:{_typeFieldName}' type name." );
                return null;
            }

            object? TryCreateFromTypeName( IActivityMonitor monitor,
                                           TypedConfigurationBuilder builder,
                                           ImmutableConfigurationSection configuration,
                                           string typeName,
                                           out bool hasError )
            {
                object? result;
                bool error = false;
                using( monitor.OnError( () => error = true ) )
                {
                    result = DoTryCreateFromTypeName( monitor, builder, typeName, configuration );
                }
                hasError = error;
                // If an error has been raised, forgets the result (even if it is not null).
                if( error )
                {
                    if( result != null )
                    {
                        monitor.Warn( ActivityMonitor.Tags.ToBeInvestigated, $"The tryCreateFromTypeName function returned a '{result.GetType()}' but emits an error. " +
                                                                             $"This is invalid: the returned result is discarded." );
                    }
                    return null;
                }
                return result;
            }

            object? CreateFromValue( IActivityMonitor monitor,
                                     TypedConfigurationBuilder builder,
                                     ImmutableConfigurationSection configuration,
                                     string value )
            {
                // We ensure that, even if tryCreateFromTypeName has been called, if we have a null result, at least
                // one error has been logged.
                object? result = null;
                bool errorEmitted = false;
                if( _tryCreateFromTypeName != null )
                {
                    using( monitor.OnError( () => errorEmitted = true ) )
                    {
                        result = DoTryCreateFromTypeName( monitor, builder, value, configuration );
                    }
                }
                if( result == null && !errorEmitted )
                {
                    monitor.Error( $"Unable to create a '{BaseType:C}' from '{configuration.Path} = {value}'." );
                }
                return result;
            }

            object? DoTryCreateFromTypeName( IActivityMonitor monitor,
                                             TypedConfigurationBuilder builder,
                                             string typeName,
                                             ImmutableConfigurationSection configuration )
            {
                Throw.DebugAssert( _tryCreateFromTypeName != null );
                object? result = _tryCreateFromTypeName( monitor, typeName, builder, configuration );
                if( result != null && !BaseType.IsAssignableFrom( result.GetType() ) )
                {
                    monitor.Error( ActivityMonitor.Tags.ToBeInvestigated,
                                   $"The '{typeName}' type name resolved to '{result.GetType():N}' but this type is not compatible with '{BaseType:N}'. " +
                                   $"(Configuration '{(configuration.Value != null
                                                        ? configuration.Path
                                                        : $"{configuration.Path}:{_typeFieldName}")}'.)" );
                    return null;
                }
                return result;
            }

            object? CoreCreate( IActivityMonitor monitor,
                                TypedConfigurationBuilder builder,
                                ImmutableConfigurationSection configuration,
                                Type type,
                                ImmutableConfigurationSection? knownItemsField )
            {
                var f = builder.GetInstanceFactory( monitor, BaseType, type );
                if( f == null ) return null;
                if( f.IsCallWithComposite )
                {
                    Array? a;
                    if( knownItemsField != null )
                    {
                        a = builder.CreateItems( monitor, f.ItemType, knownItemsField );
                    }
                    else
                    {
                        a = builder.FindItemsSectionAndCreateItems( monitor, configuration, f.ItemType, f.ItemsFieldName );
                    }
                    if( a == null ) return null;
                    return f.CreateComposite( monitor, builder, configuration, a );
                }
                return f.Create( monitor, builder, configuration );
            }
        }
    }
}

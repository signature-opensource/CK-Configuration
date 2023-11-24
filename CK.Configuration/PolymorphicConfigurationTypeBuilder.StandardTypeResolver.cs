using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CK.Core
{
    public sealed partial class PolymorphicConfigurationTypeBuilder
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
            readonly Type? _compositeBaseType;
            readonly string _compositeItemsFieldName;
            readonly Func<IActivityMonitor, string, PolymorphicConfigurationTypeBuilder, ImmutableConfigurationSection, object?>? _tryCreateFromTypeName;

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
            /// <param name="compositeBaseType">Optional specialized type that is the default composite.</param>
            /// <param name="compositeItemsFieldName">Required field name of a composite items.</param>
            /// <param name="typeFieldName">The name of the "Type" field.</param>
            /// <param name="typeNameSuffix">
            /// Required type name suffix. This is automatically appended to the type name read from <paramref name="typeFieldName"/> if missing.
            /// </param>
            public StandardTypeResolver( Type baseType,
                                         string typeNamespace,
                                         bool allowOtherNamespace = false,
                                         string? familyTypeNameSuffix = null,
                                         Func<IActivityMonitor, string, PolymorphicConfigurationTypeBuilder, ImmutableConfigurationSection, object?>? tryCreateFromTypeName = null,
                                         Type? compositeBaseType = null,
                                         string compositeItemsFieldName = "Items",
                                         string typeFieldName = "Type",
                                         string typeNameSuffix = "Configuration" )
                : base( baseType )
            {
                Throw.CheckNotNullArgument( baseType );
                Throw.CheckNotNullOrWhiteSpaceArgument( typeFieldName );
                Throw.CheckNotNullOrWhiteSpaceArgument( typeNamespace );
                Throw.CheckArgument( compositeBaseType == null || (compositeBaseType.IsClass && !compositeBaseType.IsAbstract && baseType.IsAssignableFrom( compositeBaseType )) );
                Throw.CheckNotNullOrWhiteSpaceArgument( compositeItemsFieldName );
                _typeFieldName = typeFieldName;
                _typeNamespace = typeNamespace;
                _allowOtherNamespace = allowOtherNamespace;
                _familyTypeNameSuffix = familyTypeNameSuffix;
                _tryCreateFromTypeName = tryCreateFromTypeName;
                _compositeBaseType = compositeBaseType;
                _compositeItemsFieldName = compositeItemsFieldName;
                _typeNameSuffix = typeNameSuffix;
            }

            internal protected override object? Create( IActivityMonitor monitor,
                                                        PolymorphicConfigurationTypeBuilder builder,
                                                        ImmutableConfigurationSection configuration )
            {
                return DoCreate( monitor, builder, configuration, _compositeBaseType != null );
            }

            internal protected override Array? CreateItems( IActivityMonitor monitor,
                                                            PolymorphicConfigurationTypeBuilder builder,
                                                            ImmutableConfigurationSection composite,
                                                            bool requiresItemsFieldName,
                                                            string? alternateItemsFieldName )
            {
                return DoCreateItems( monitor, builder, composite, null, alternateItemsFieldName, requiresItemsFieldName );
            }

            object? DoCreate( IActivityMonitor monitor,
                              PolymorphicConfigurationTypeBuilder builder,
                              ImmutableConfigurationSection configuration,
                              bool allowDefaultComposite )
            {
                // First, check if the configuration is a value and if it is the case, we have no other choice
                // to use the optional tryCreateFromTypeName. 
                if( configuration.Value != null )
                {
                    return CreateFromValue( monitor, builder, configuration, configuration.Value );
                }
                // If it's a section then it may define assemblies.
                var previousAssemblies = builder.AssemblyConfiguration;
                builder.AssemblyConfiguration = builder.AssemblyConfiguration.Apply( monitor, configuration ) ?? previousAssemblies;
                try
                {
                    // Else, lookup the "Type" field.
                    var typeName = configuration[_typeFieldName];
                    // If it exists and the optional tryCreateFromTypeName exists then give it a try.
                    if( typeName != null && _tryCreateFromTypeName != null )
                    {
                        object? result = TryCreateFromTypeName( monitor, builder, configuration, typeName );
                        // If the result has been created, we're done.
                        if( result != null ) return result;
                    }
                    if( typeName == null )
                    {
                        // When no "Type" field is specified and if the family has a composite, we consider the default composite.
                        return CreateWithNoTypeName( monitor, builder, configuration, allowDefaultComposite );
                    }
                    return CreateWithTypeName( monitor, builder, configuration, typeName );
                }
                finally
                {
                    builder.AssemblyConfiguration = previousAssemblies;
                }
            }

            object? CreateWithTypeName( IActivityMonitor monitor,
                                        PolymorphicConfigurationTypeBuilder builder,
                                        ImmutableConfigurationSection configuration,
                                        string typeName )
            {
                var type = builder.AssemblyConfiguration.TryResolveType( monitor,
                                                                         typeName,
                                                                         _typeNamespace,
                                                                         BaseType.Assembly,
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
                                   $" (Configuration '{configuration.Path}:{_typeFieldName}'.)" );
                    return null;
                }
                try
                {
                    if( _compositeBaseType != null && _compositeBaseType.IsAssignableFrom( type ) )
                    {
                        return DoCreateComposite( monitor, builder, configuration, type, null );
                    }
                    return Activator.CreateInstance( type, monitor, builder, configuration );
                }
                catch( Exception ex )
                {
                    monitor.Error( $"While instantiating '{type:C}'. (Configuration '{configuration.Path}:{_typeFieldName} = {typeName}'.)", ex );
                    return null;
                }
            }

            object? CreateWithNoTypeName( IActivityMonitor monitor,
                                          PolymorphicConfigurationTypeBuilder builder,
                                          ImmutableConfigurationSection configuration,
                                          bool allowDefaultComposite )
            {
                if( _compositeBaseType != null )
                {
                    Throw.DebugAssert( _compositeBaseType != null );
                    // When allowDefaultComposite (root Create call), if there's no "Items" field
                    // we consider the configuration itself to be the composite items.
                    // This allows array (or even keyed objects) to be handled. This trick is safe
                    // since we check that the configuration has children and in such case, children
                    // configurations must be valid definitions.
                    var itemsField = configuration.TryGetSection( _compositeItemsFieldName );
                    if( itemsField == null && allowDefaultComposite )
                    {
                        if( !configuration.HasChildren )
                        {
                            monitor.Error( $"Configuration '{configuration.Path}' must have children to be considered a default '{_compositeBaseType:C}'." );
                            return null;
                        }
                        itemsField = configuration;
                    }
                    if( itemsField != null )
                    {
                        try
                        {
                            return DoCreateComposite( monitor, builder, configuration, _compositeBaseType, itemsField );
                        }
                        catch( Exception ex )
                        {
                            monitor.Error( $"While instantiating '{_compositeBaseType:C}' from '{configuration.Path}'.", ex );
                            return null;
                        }
                    }
                    monitor.Warn( $"Missing '{configuration.Path}:{_compositeItemsFieldName}' to define a composite." );
                }
                monitor.Error( $"Missing required '{configuration.Path}:{_typeFieldName}' type name." );
                return null;
            }

            object? TryCreateFromTypeName( IActivityMonitor monitor,
                                           PolymorphicConfigurationTypeBuilder builder,
                                           ImmutableConfigurationSection configuration,
                                           string typeName )
            {
                object? result;
                bool success = true;
                using( monitor.OnError( () => success = false ) )
                {
                    result = DoTryCreateFromTypeName( monitor, builder, typeName, configuration );
                }
                // If an error has been raised, forgets the result (even if it is not null).
                if( !success )
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
                                     PolymorphicConfigurationTypeBuilder builder,
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
                                             PolymorphicConfigurationTypeBuilder builder,
                                             string typeName,
                                             ImmutableConfigurationSection configuration )
            {
                Throw.DebugAssert( _tryCreateFromTypeName != null );
                object? result = _tryCreateFromTypeName( monitor, typeName, builder, configuration );
                if( result != null && !BaseType.IsAssignableFrom( result.GetType() ) )
                {
                    monitor.Error( ActivityMonitor.Tags.ToBeInvestigated,
                                   $"The '{typeName}' type name resolved to '{result.GetType():N}' but this type is not compatible with '{BaseType:N}'. " +
                                   $" (Configuration '{(configuration.Value != null
                                                        ? configuration.Path
                                                        : $"{configuration.Path}:{_typeFieldName}")}'.)" );
                    return null;
                }
                return result;
            }

            object? DoCreateComposite( IActivityMonitor monitor,
                                       PolymorphicConfigurationTypeBuilder builder,
                                       ImmutableConfigurationSection configuration,
                                       Type type,
                                       ImmutableConfigurationSection? itemsField )
            {
                Array? a = DoCreateItems( monitor, builder, configuration, itemsField, null, false );
                return a != null ? Activator.CreateInstance( type, monitor, builder, configuration, a ) : null;
            }

            Array? DoCreateItems( IActivityMonitor monitor,
                                  PolymorphicConfigurationTypeBuilder builder,
                                  ImmutableConfigurationSection configuration,
                                  ImmutableConfigurationSection? itemsField,
                                  string? alternateItemsName,
                                  bool requiresItemsFieldName )
            {
                if( itemsField == null )
                {
                    if( alternateItemsName != null )
                    {
                        itemsField = configuration.TryGetSection( alternateItemsName );
                    }
                    itemsField ??= configuration.TryGetSection( _compositeItemsFieldName );
                }
                if( itemsField == null )
                {
                    if( requiresItemsFieldName )
                    {
                        if( alternateItemsName != null )
                        {
                            monitor.Error( $"Missing composite required items '{configuration.Path}:{alternateItemsName}' or ':{_compositeItemsFieldName}'." );
                        }
                        else
                        {
                            monitor.Error( $"Missing composite required items '{configuration.Path}:{_compositeItemsFieldName}'." );
                        }
                        return null;
                    }
                    return Array.CreateInstance( BaseType, 0 );
                }
                // We must use a correctly typed array for reflection binding.
                var children = itemsField.GetChildren();
                var a = Array.CreateInstance( BaseType, children.Count );
                bool success = true;
                for( int i = 0; i < a.Length; i++ )
                {
                    var o = DoCreate( monitor, builder, children[i], false );
                    if( o == null ) success = false;
                    a.SetValue( o, i );
                }
                return success ? a : null;
            }

        }
    }
}

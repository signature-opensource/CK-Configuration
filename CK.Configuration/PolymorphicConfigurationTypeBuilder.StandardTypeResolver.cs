using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CK.Core
{
    public partial class PolymorphicConfigurationTypeBuilder
    {
        internal sealed class StandardTypeResolver<TBuilder> : TypeResolver where TBuilder : PolymorphicConfigurationTypeBuilder
        {
            readonly string _typeFieldName;
            readonly string _typeNamespace;
            readonly bool _allowOtherNamespace;
            readonly string? _familyTypeNameSuffix;
            readonly string _typeNameSuffix;
            readonly Type? _compositeBaseType;
            readonly string _compositeItemsFieldName;
            readonly Func<IActivityMonitor, string, ImmutableConfigurationSection, object?>? _tryCreateFromTypeName;


            internal StandardTypeResolver( TBuilder builder,
                                           Type baseType,
                                           string typeNamespace,
                                           bool allowOtherNamespace,
                                           string? familyTypeNameSuffix,
                                           Func<IActivityMonitor, string, ImmutableConfigurationSection, object?>? tryCreateFromTypeName,
                                           Type? compositeBaseType,
                                           string compositeItemsFieldName,
                                           string typeFieldName,
                                           string typeNameSuffix )
                : base( builder, baseType )
            {
                Throw.CheckNotNullArgument( builder );
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

            new TBuilder Builder => Unsafe.As<TBuilder>( base.Builder );

            internal protected override object? Create( IActivityMonitor monitor, ImmutableConfigurationSection configuration )
            {
                return DoCreate( monitor, configuration, _compositeBaseType != null );
            }

            object? DoCreate( IActivityMonitor monitor, ImmutableConfigurationSection configuration, bool allowDefaultComposite )
            {
                // First, check if the configuration is a value and if it is the case, we have no other choice
                // to use the optional tryCreateFromTypeName. 
                if( configuration.Value != null )
                {
                    // We ensure that, even if tryCreateFromTypeName has been called, if we have a null result, at least
                    // one error has been logged.
                    object? result = null;
                    bool errorEmitted = false;
                    if( _tryCreateFromTypeName != null )
                    {
                        using( monitor.OnError( () => errorEmitted = true ) )
                        {
                            result = TryCreateFromTypeName( monitor, configuration.Value, configuration );
                        }
                    }
                    if( result == null && !errorEmitted )
                    {
                        monitor.Error( $"Unable to create a '{BaseType:C}' from '{configuration.Path} = {configuration.Value}'." );
                    }
                    return result;
                }
                // Else, lookup the "Type" field.
                var typeName = configuration[_typeFieldName];
                // If it exists and the optional tryCreateFromTypeName exists then give it a try.
                if( typeName != null && _tryCreateFromTypeName != null )
                {
                    object? result = null;
                    bool success = true;
                    if( _tryCreateFromTypeName != null )
                    {
                        using( monitor.OnError( () => success = false ) )
                        {
                            result = TryCreateFromTypeName( monitor, typeName, configuration );
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
                        // If the result has been created, we're done.
                        if( result != null ) return result;
                    }
                }
                // When no "Type" field is specified and we are on a root call, we consider the configuration
                // to be the default composite (if the family has one). Either the "Items" field is specified
                // or we fallback on the configuration object itself.
                if( typeName == null )
                {
                    if( allowDefaultComposite )
                    {
                        Throw.DebugAssert( _compositeBaseType != null );
                        // When allowDefaultComposite (root Create call), if there's no "Items" field
                        // we consider the configuration itself to be the composite items.
                        // This allows array (or even keyed objects) to be handled. This trick is safe
                        // since we check that the configuration has children and in such case, children
                        // configurations must be valid definitions.
                        var itemsField = configuration.TryGetSection( _compositeItemsFieldName );
                        if( itemsField == null )
                        {
                            if( !configuration.HasChildren )
                            {
                                monitor.Error( $"Configuration '{configuration.Path}' must have children to be considered a default '{_compositeBaseType:C}'." );
                                return null;
                            }
                            itemsField = configuration;
                        }
                        try
                        {
                            return DoCreateComposite( monitor, configuration, _compositeBaseType, itemsField );
                        }
                        catch( Exception ex )
                        {
                            monitor.Error( $"While instantiating '{_compositeBaseType:C}' from '{configuration.Path}'.", ex );
                            return null;
                        }
                    }
                    monitor.Error( $"Missing required '{configuration.Path}:{_typeFieldName}' type name." );
                    return null;
                }
                var type = Builder.AssemblyConfiguration.TryResolveType( monitor,
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
                        return DoCreateComposite( monitor, configuration, type, null );
                    }
                    return Activator.CreateInstance( type, monitor, Builder, configuration );
                }
                catch( Exception ex )
                {
                    monitor.Error( $"While instantiating '{type:C}'. (Configuration '{configuration.Path}:{_typeFieldName} = {typeName}'.)", ex );
                    return null;
                }
            }

            object? TryCreateFromTypeName( IActivityMonitor monitor, string typeName, ImmutableConfigurationSection configuration )
            {
                Throw.DebugAssert( _tryCreateFromTypeName != null );
                object? result = _tryCreateFromTypeName( monitor, typeName, configuration );
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

            object? DoCreateComposite( IActivityMonitor monitor, ImmutableConfigurationSection configuration, Type type, ImmutableConfigurationSection? itemsField )
            {
                itemsField ??= configuration.TryGetSection( _compositeItemsFieldName );
                if( itemsField == null )
                {
                    monitor.Error( $"Missing composite required items '{configuration.Path}:{_compositeItemsFieldName}'." );
                    return null;
                }
                // We must use a correctly typed array for reflection binding.
                var children = itemsField.GetChildren();
                var a = Array.CreateInstance( BaseType, children.Count );
                bool success = true;
                for( int i = 0; i < children.Count; i++ )
                {
                    var o = DoCreate( monitor, children[i], false );
                    if( o == null ) success = false;
                    a.SetValue( o, i );
                }
                return success ? Activator.CreateInstance( type, monitor, Builder, configuration, a ) : null;
            }

        }
    }
}

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

            internal StandardTypeResolver( TBuilder builder,
                                           Type baseType,
                                           string typeNamespace,
                                           bool allowOtherNamespace,
                                           string? familyTypeNameSuffix,
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
                var typeName = configuration[_typeFieldName];
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
                var type = Builder.CurrentAssemblyConfiguration.TryResolveType( monitor,
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
                    monitor.Error( $"The '{typeName}' type name resolved to '{type:N}' but this type is not compatible with '{BaseType:N}'. " +
                                   $" (Configuration '{configuration.Path}:{_compositeItemsFieldName}'.)" );
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

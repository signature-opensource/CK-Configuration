using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Core
{
    public partial class PolymorphicConfigurationTypeBuilder
    {
        /// <summary>
        /// Standard type resolver implementation that optionnaly supports composite types.
        /// <para>
        /// Use <see cref="AddStandardTypeResolver(Type, string, string, bool, string?, Type?, string, string)"/>
        /// to create and add a resolver.
        /// </para>
        /// </summary>
        public sealed class StandardTypeResolver : TypeResolver
        {
            readonly string _fieldName;
            readonly string _typeNamespace;
            readonly bool _allowOtherNamespace;
            readonly string? _familyTypeNameSuffix;
            readonly string _typeNameSuffix;
            readonly Type? _compositeBaseType;
            readonly string _compositeItemsFieldName;

            internal StandardTypeResolver( PolymorphicConfigurationTypeBuilder builder,
                                           Type baseType,
                                           string fieldName,
                                           string typeNamespace,
                                           bool allowOtherNamespace = false,
                                           string? familyTypeNameSuffix = null,
                                           Type? compositeBaseType = null,
                                           string compositeItemsFieldName = "Items",
                                           string typeNameSuffix = "Configuration" )
                : base( builder, baseType )
            {
                Throw.CheckNotNullArgument( builder );
                Throw.CheckNotNullArgument( baseType );
                Throw.CheckNotNullOrWhiteSpaceArgument( fieldName );
                Throw.CheckNotNullOrWhiteSpaceArgument( typeNamespace );
                Throw.CheckArgument( compositeBaseType == null || (compositeBaseType.IsClass && !compositeBaseType.IsAbstract && baseType.IsAssignableFrom( compositeBaseType )) );
                Throw.CheckNotNullOrWhiteSpaceArgument( compositeItemsFieldName );
                _fieldName = fieldName;
                _typeNamespace = typeNamespace;
                _allowOtherNamespace = allowOtherNamespace;
                _familyTypeNameSuffix = familyTypeNameSuffix;
                _compositeBaseType = compositeBaseType;
                _compositeItemsFieldName = compositeItemsFieldName;
                _typeNameSuffix = typeNameSuffix;
            }

            protected internal override object? Create( IActivityMonitor monitor, ImmutableConfigurationSection configuration )
            {
                return DoCreate( monitor, configuration, _compositeBaseType != null );

            }

            private object? DoCreate( IActivityMonitor monitor, ImmutableConfigurationSection configuration, bool allowDefaultComposite )
            {
                var typeName = configuration[_fieldName];
                if( typeName == null )
                {
                    if( allowDefaultComposite )
                    {
                        Throw.DebugAssert( _compositeBaseType != null );
                        // For root (default) composite, we lookup the "Items" field or fallback on
                        // the configuration itself.
                        var itemsField = configuration.TryGetSection( _compositeItemsFieldName );
                        var composite = itemsField ?? configuration;
                        try
                        {
                            var items = CreateCompositeItems( monitor, composite ).ToArray();
                            return DoCreateComposite( monitor, configuration, _compositeBaseType, items );
                        }
                        catch( Exception ex )
                        {
                            monitor.Error( $"While instantiating '{_compositeBaseType:C}' from '{composite.Path}'.", ex );
                            return null;
                        }
                    }
                    if( _compositeBaseType == null )
                    {
                        monitor.Error( $"Missing required '{configuration.Path}:{_fieldName}' type name." );
                    }
                    else
                    {
                        monitor.Error( $"Missing required '{configuration.Path}:{_fieldName}' type name (and default composite is not allowed here since we are in a Composite)." );
                    }
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
                                                                                () => $" (Configuration '{configuration.Path}:{_fieldName}'.)" );
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
                        // Typed composite: we expect the "Items" field but if it is not here, we create
                        // an empty composite but emit a warning: a composite should have at least one item.
                        // If a "composite" with sub components must be done, it must use the 
                        // builder to instantiate its sub components.
                        var itemsField = configuration.TryGetSection( _compositeItemsFieldName );
                        if( itemsField == null )
                        {
                            monitor.Warn( $"Missing '{configuration.Path}:{_compositeItemsFieldName}'. Instantiating an empty '{type:C}' composite." );
                        }
                        var items = itemsField == null ? Array.Empty<object>() : CreateCompositeItems( monitor, itemsField ).ToArray();
                        return DoCreateComposite( monitor, configuration, type, items );
                    }
                    return Activator.CreateInstance( type, monitor, Builder, configuration );
                }
                catch( Exception ex )
                {
                    monitor.Error( $"While instantiating '{type:C}'. (Configuration '{configuration.Path}:{_fieldName} = {typeName}'.)", ex );
                    return null;
                }
            }

            object? DoCreateComposite( IActivityMonitor monitor, ImmutableConfigurationSection configuration, Type type, object[] items )
            {
                var a = Array.CreateInstance( BaseType, items.Length );
                Array.Copy( items, a, items.Length );
                return Activator.CreateInstance( type, monitor, Builder, configuration, a );
            }

            IEnumerable<object> CreateCompositeItems( IActivityMonitor monitor, ImmutableConfigurationSection configuration )
            {
                foreach( var c in configuration.GetChildren() )
                {
                    var o = DoCreate( monitor, c, false );
                    if( o != null ) yield return o;
                }
            }
        }
    }
}

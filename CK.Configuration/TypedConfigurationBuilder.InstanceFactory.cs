using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace CK.Core;

public sealed partial class TypedConfigurationBuilder
{
    static readonly Type[] _argTypes = new Type[] { typeof( IActivityMonitor ),
                                                    typeof( TypedConfigurationBuilder ),
                                                    typeof( ImmutableConfigurationSection ) };

    /// <summary>
    /// Utility class that encapsulates constructor or public static Create factory methods
    /// and handles the instantiation.
    /// </summary>
    public sealed class Factory
    {
        readonly FactoryKey _key;
        readonly MethodInfo? _createMethod;
        readonly Type? _itemType;
        readonly string? _itemsFieldName;

        internal Factory( FactoryKey key, MethodInfo? method, string? itemsFieldName, Type? itemType )
        {
            _key = key;
            _createMethod = method;
            _itemsFieldName = itemsFieldName;
            _itemType = itemType;
        }

        /// <summary>
        /// Gets the case type of the family.
        /// </summary>
        public Type BaseType => _key.BaseType;

        /// <summary>
        /// Gets the type that must be instantiated.
        /// </summary>
        public Type Type => _key.Type;

        /// <summary>
        /// Gets whether the Create static method will be used.
        /// </summary>
        public bool UseCreateMethod => _createMethod != null;

        /// <summary>
        /// Gets whether the call to instantiate <see cref="Type"/> requires
        /// subordinated items to be provided.
        /// <para>
        /// When false, this doesn't imply that the Type is not a composite. The constructor
        /// or Create method can resolves as many subordinates items fields as they want.
        /// </para>
        /// </summary>
        [MemberNotNullWhen( true, nameof( ItemType ), nameof( ItemsFieldName ) )]
        public bool IsCallWithComposite => _itemType != null;

        /// <summary>
        /// Gets the composite item type.
        /// </summary>
        public Type? ItemType => _itemType;

        /// <summary>
        /// Gets the "Items" field name: this is the parameter names of the subordinated
        /// items name in the constructor or Create factory method.
        /// </summary>
        public string? ItemsFieldName => _itemsFieldName;

        /// <summary>
        /// Creates a non composite object.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="builder">The builder.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The created instance or null on error.</returns>
        public object? Create( IActivityMonitor monitor,
                               TypedConfigurationBuilder builder,
                               ImmutableConfigurationSection configuration )
        {
            Throw.CheckState( !IsCallWithComposite );
            return DoCreate( monitor, new object[] { monitor, builder, configuration } );
        }

        /// <summary>
        /// Creates a composite object.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="builder">The builder.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="items">The subordinated items.</param>
        /// <returns>The created instance or null on error.</returns>
        public object? CreateComposite( IActivityMonitor monitor,
                                        TypedConfigurationBuilder builder,
                                        ImmutableConfigurationSection configuration,
                                        Array items )
        {
            Throw.CheckState( IsCallWithComposite );
            return DoCreate( monitor, new object[] { monitor, builder, configuration, items } );
        }

        object? DoCreate( IActivityMonitor monitor, object[] parameters )
        {
            var o = _createMethod != null
                        ? _createMethod.Invoke( null, parameters )
                        : Activator.CreateInstance( _key.Type, parameters );
            if( o == null || !_key.BaseType.IsAssignableFrom( o.GetType() ) )
            {
                monitor.Error( $"Invalid created instance for base type '{_key.BaseType:N}'. Got '{o ?? "<null>"}'." );
                return null;
            }
            return o;
        }
    }

    /// <summary>
    /// Gets an instance factory.
    /// No check that <paramref name="t"/> is a <paramref name="baseType"/> is done here.
    /// </summary>
    /// <param name="monitor">The monitor.</param>
    /// <param name="baseType">The family's base type.</param>
    /// <param name="t">The type to instantiate.</param>
    /// <returns>The factory or null on error.</returns>
    public Factory? GetInstanceFactory( IActivityMonitor monitor, Type baseType, Type t )
    {
        var k = new FactoryKey( baseType, t );
        if( !_factories.TryGetValue( k, out var f ) )
        {
            f = CreateFactory( monitor, baseType, k );
            _factories.Add( k, f );
        }
        return f;
    }

    Factory? CreateFactory( IActivityMonitor monitor, Type baseType, FactoryKey key )
    {
        var t = key.Type;
        var (ctor, itemsFieldName, itemType) = FindCtor( monitor, baseType, t );
        if( ctor != null )
        {
            monitor.Trace( $"Using public constructor for {(itemType != null ? "composite " : "")}'{t:N}'." );
            return new Factory( key, null, itemsFieldName, itemType );
        }
        MethodInfo? method;
        (method, itemsFieldName, itemType) = FindCreate( monitor, baseType, t );
        if( method != null )
        {
            monitor.Trace( $"Using public static Create factory method for {(itemType != null ? "composite " : "")} '{t:N}'." );
            return new Factory( key, method, itemsFieldName, itemType );
        }

        monitor.Error( $"Unable to find a public constructor or public static Create factory method. Expected:{Environment.NewLine}" +
                        $"'public {t.Name}( IActiviyMonitor monitor, {nameof( TypedConfigurationBuilder )} builder, ImmutableConfigurationSection configuration[, IReadOnlyList<{baseType:C}> items ])'{Environment.NewLine}" +
                        $" or 'public static object? Create( ... )' in type '{t:N}'." );
        return null;
    }

    (ConstructorInfo?, string?, Type?) FindCtor( IActivityMonitor monitor, Type baseType, Type t )
    {
        var ctors = t.GetConstructors();
        foreach( var c in ctors )
        {
            if( ExtractItemType( monitor, c, baseType, out var itemsFieldName, out var itemType ) )
            {
                return (c, itemsFieldName, itemType);
            }
        }
        return (null, null, null);
    }

    (MethodInfo?, string?, Type?) FindCreate( IActivityMonitor monitor, Type baseType, Type t )
    {
        var creates = t.GetMethods( BindingFlags.Public | BindingFlags.Static ).Where( m => m.Name == "Create" );
        foreach( var m in creates )
        {
            if( ExtractItemType( monitor, m, baseType, out var itemsFieldName, out var itemType ) )
            {
                return (m, itemsFieldName, itemType);
            }
        }
        return (null, null, null);
    }

    bool ExtractItemType( IActivityMonitor monitor, MethodBase m, Type baseType, out string? itemsFieldName, out Type? itemType )
    {
        var parameters = m.GetParameters();
        if( (parameters.Length == 3 || parameters.Length == 4)
            && parameters[0].ParameterType == typeof( IActivityMonitor )
            && parameters[1].ParameterType == typeof( TypedConfigurationBuilder )
            && parameters[2].ParameterType == typeof( ImmutableConfigurationSection ) )
        {
            if( parameters.Length == 3 )
            {
                itemsFieldName = null;
                itemType = null;
                return true;
            }
            var list = parameters[3].ParameterType;
            if( list.IsGenericType
                && list.GetGenericTypeDefinition() == typeof( IReadOnlyList<> )
                && baseType.IsAssignableFrom( itemType = list.GenericTypeArguments[0] ) )
            {
                itemsFieldName = parameters[3].Name;
                return true;
            }
            bool isCtor = m is ConstructorInfo;
            monitor.Warn( $"{(isCtor ? "Constructor" : "Factory method")} '{m.DeclaringType:N}{(isCtor ? "" : m.Name)}( " +
                          $"IActivityMonitor, {nameof( TypedConfigurationBuilder )}, ImmutableConfigurationSection, {list:C} {parameters[3].Name} )' " +
                          $"has invalid 4th parameter. It must be a IReadOnlyList<{baseType:C}>.{Environment.NewLine}" +
                          $"This is ignored." );
        }
        itemsFieldName = null;
        itemType = null;
        return false;
    }

}

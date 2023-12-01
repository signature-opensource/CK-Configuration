using System;
using System.Collections.Generic;
using System.Reflection;

namespace CK.Core
{
    public sealed partial class PolymorphicConfigurationTypeBuilder
    {
        static readonly Type[] _argTypes = new Type[] { typeof( IActivityMonitor ),
                                                        typeof( PolymorphicConfigurationTypeBuilder ),
                                                        typeof( ImmutableConfigurationSection ) };

        /// <summary>
        /// Utility class that encapsulates constructor or public static Create factory methods
        /// and handles the instantiation.
        /// </summary>
        public sealed class Factory
        {
            readonly FactoryKey _key;
            readonly MethodInfo? _createMethod;
            readonly bool _isComposite;

            internal Factory( FactoryKey key, MethodInfo? method, bool isComposite )
            {
                _key = key;
                _createMethod = method;
                _isComposite = isComposite;
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
            /// Gets whether this <see cref="Type"/> is a composite.
            /// </summary>
            public bool IsComposite => _isComposite;

            /// <summary>
            /// Creates a non composite object.
            /// </summary>
            /// <param name="monitor">The monitor to use.</param>
            /// <param name="builder">The builder.</param>
            /// <param name="configuration">The configuration.</param>
            /// <returns>The created instance or null on error.</returns>
            public object? Create( IActivityMonitor monitor,
                                   PolymorphicConfigurationTypeBuilder builder,
                                   ImmutableConfigurationSection configuration )
            {
                Throw.CheckState( !IsComposite );
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
                                            PolymorphicConfigurationTypeBuilder builder,
                                            ImmutableConfigurationSection configuration,
                                            Array items )
            {
                Throw.CheckState( IsComposite );
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
            var ctor = t.GetConstructor( _argTypes );
            if( ctor != null )
            {
                monitor.Trace( $"Using public constructor for non composite '{t:N}'." );
                return new Factory( key, null, false );
            }
            var method = t.GetMethod( "Create", BindingFlags.Public| BindingFlags.Static, _argTypes );
            if( method != null )
            {
                monitor.Trace( $"Using public Create factory method for non composite '{t:N}'." );
                return new Factory( key, method, false );
            }

            var composite = typeof(IReadOnlyList<>).MakeGenericType( baseType );
            var args = new Type[4];
            _argTypes.CopyTo( args, 0 );
            args[3] = composite;

            ctor = t.GetConstructor( args );
            if( ctor != null )
            {
                monitor.Trace( $"Using public constructor for composite '{t:N}'." );
                return new Factory( key, null, true );
            }
            method = t.GetMethod( "Create", BindingFlags.Public | BindingFlags.Static, args );
            if( method != null )
            {
                monitor.Trace( $"Using public Create factory method for composite '{t:N}'." );
                return new Factory( key, method, true);
            }

            monitor.Error( $"Unable to find a public constructor or static Create factory method. Expected:{Environment.NewLine}" +
                            $"'public {t.Name}( IActiviyMonitor monitor, {nameof(PolymorphicConfigurationTypeBuilder)} builder, ImmutableConfigurationSection configuration[, {composite:C} items ])'{Environment.NewLine}" +
                            $" or 'public static object? Create( ... )' in type '{t:N}'." );
            return null;
        }
    }
}

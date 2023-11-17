using System;
using System.Linq;

namespace CK.Core
{
    public partial class PolymorphicConfigurationTypeBuilder
    {
        /// <summary>
        /// A resolver can create an instance of a <see cref="BaseType"/> from a <see cref="ImmutableConfigurationSection"/>.
        /// <para>
        /// Other type resolver than the standard one (see <see cref="PolymorphicConfigurationTypeBuilderExtensions.AddStandardTypeResolver{TBuilder}(TBuilder, Type, string, bool, string?, Type?, string, string, string)"/>)
        /// can be created, but the standard one should cover all needs.
        /// </para>
        /// <para>
        /// Adding a resolver to <see cref="Resolvers"/> is done by the TypeResolver constructor.
        /// When <see cref="PolymorphicConfigurationTypeBuilder.IsCreating"/> is true, the new resolver only applies until the current call
        /// to <see cref="PolymorphicConfigurationTypeBuilder.Create(IActivityMonitor, Type, Microsoft.Extensions.Configuration.IConfigurationSection)"/>
        /// ends.
        /// </para>
        /// </summary>
        public abstract class TypeResolver
        {
            readonly PolymorphicConfigurationTypeBuilder _builder;
            readonly Type _baseType;

            /// <summary>
            /// Initializes a new type resolver and adds it to the <paramref name="builder"/>.
            /// </summary>
            /// <param name="builder">The builder into which this resolver will be added.</param>
            /// <param name="baseType">The <see cref="BaseType"/>.</param>
            public TypeResolver( PolymorphicConfigurationTypeBuilder builder, Type baseType )
            {
                Throw.CheckArgument( builder != null && builder.Resolvers.Any( b => baseType.IsAssignableFrom( b.BaseType ) ) is false );
                Throw.CheckNotNullArgument( baseType );
                _builder = builder;
                _baseType = baseType;
                builder._resolvers.Add( this );
            }

            /// <summary>
            /// Gets the builder that uses this resolver.
            /// </summary>
            protected PolymorphicConfigurationTypeBuilder Builder => _builder;

            /// <summary>
            /// Gets the base type handled by this resolver.
            /// </summary>
            public Type BaseType => _baseType;

            /// <summary>
            /// Attempts to create an instance from a configuration using any possible strategies
            /// to resolve its type and activating an instance.
            /// </summary>
            /// <param name="monitor">The monitor that must be used to signal errors and warnings.</param>
            /// <param name="configuration">The configuration to analyze.</param>
            /// <returns>The resulting instanceor null on errors.</returns>
            internal protected abstract object? Create( IActivityMonitor monitor, ImmutableConfigurationSection configuration );
        }
    }
}

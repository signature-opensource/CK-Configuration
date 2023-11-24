using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.Core
{
    public sealed partial class PolymorphicConfigurationTypeBuilder
    {
        /// <summary>
        /// A resolver can create an instance of a <see cref="BaseType"/> from a <see cref="ImmutableConfigurationSection"/>.
        /// <para>
        /// Other type resolver than the standard one (see <see cref="PolymorphicConfigurationTypeBuilderExtensions.AddStandardTypeResolver{TBuilder}(TBuilder, Type, string, bool, string?, Type?, string, string, string)"/>)
        /// can be created, but the standard one should cover all needs.
        /// </para>
        /// <para>
        /// Adding a resolver to <see cref="Resolvers"/> is done by the TypeResolver constructor.
        /// When <see cref="PolymorphicConfigurationTypeBuilder.IsCreating"/> is true, the added resolver only applies until the current call
        /// to <see cref="PolymorphicConfigurationTypeBuilder.Create(IActivityMonitor, Type, Microsoft.Extensions.Configuration.IConfigurationSection)"/>
        /// ends.
        /// </para>
        /// <para>
        /// Resolver implementations must be stateless: once created with their own configuration they must be callable by different
        /// builder instances (this enables configuration extensibilty typically via placeholders).
        /// </para>
        /// </summary>
        public abstract class TypeResolver
        {
            readonly Type _baseType;

            /// <summary>
            /// Initializes a new type resolver.
            /// </summary>
            /// <param name="baseType">The <see cref="BaseType"/>.</param>
            protected TypeResolver( Type baseType )
            {
                Throw.CheckNotNullArgument( baseType );
                _baseType = baseType;
            }

            /// <summary>
            /// Gets the base type handled by this resolver.
            /// </summary>
            public Type BaseType => _baseType;

            /// <summary>
            /// Attempts to create an instance from a configuration using any possible strategies
            /// to resolve its type and activating an instance.
            /// </summary>
            /// <param name="monitor">The monitor that must be used to signal errors and warnings.</param>
            /// <param name="builder">The calling builder for which the configuration must be resolved.</param>
            /// <param name="configuration">The configuration to analyze.</param>
            /// <returns>The resulting instance or null if any error occurred.</returns>
            internal protected abstract object? Create( IActivityMonitor monitor,
                                                        PolymorphicConfigurationTypeBuilder builder,
                                                        ImmutableConfigurationSection configuration );

            /// <summary>
            /// Attempts to instantiate items of the composite type from a composite configuration.
            /// <para>
            /// This may easily be implemented by calling <see cref="Create(IActivityMonitor, ImmutableConfigurationSection)"/> for
            /// each item but this is modeled for 2 reasons:
            /// <list type="bullet">
            /// <item>
            /// Creating the content of a composite may not be exactly the same as creating a composite from the items built "from the outside"
            /// (even locating the "Items" field to consider is specific to this resolver).
            /// </item>
            /// <item>
            /// The returned items must be assignable to a <see cref="IReadOnlyList{T}"/> of <see cref="BaseType"/> an we have not generic at this level.
            /// Using the rarely used <see cref="Array"/> enables to create a correctly typed list without using <see cref="Type.MakeGenericType(Type[])"/>.
            /// </item>
            /// </list>
            /// </para>
            /// </summary>
            /// <param name="monitor">The monitor that must be used to signal errors and warnings.</param>
            /// <param name="builder">The calling builder for which the configuration must be resolved.</param>
            /// <param name="composite">The composite configuration.</param>
            /// <param name="requiresItemsFieldName">
            /// True to requires the "Items" (or <paramref name="alternateItemsFieldName"/>) field name.
            /// By default even if no "Items" appears in the <paramref name="configuration"/>, an empty list is returned.
            /// </param>
            /// <param name="alternateItemsFieldName">
            /// Optional "Items" field names with the subordinated items. When let to null or when not found, the default composite "Items" field name
            /// used by this resolver must be used.
            /// </param>
            /// <returns>The resulting list or null if any error occurred.</returns>
            internal protected abstract Array? CreateItems( IActivityMonitor monitor,
                                                            PolymorphicConfigurationTypeBuilder builder,
                                                            ImmutableConfigurationSection composite,
                                                            bool requiresItemsFieldName = false, 
                                                            string? alternateItemsFieldName = null );
        }
    }
}

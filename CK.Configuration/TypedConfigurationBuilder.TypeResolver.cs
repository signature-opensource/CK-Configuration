using System;

namespace CK.Core;

public sealed partial class TypedConfigurationBuilder
{
    /// <summary>
    /// A resolver can create an instance of a <see cref="BaseType"/> from a <see cref="ImmutableConfigurationSection"/>.
    /// <para>
    /// Other type resolver than the standard one (see <see cref="StandardTypeResolver"/>)
    /// can be created, but the standard one should cover all needs.
    /// </para>
    /// <para>
    /// Adding a resolver to <see cref="Resolvers"/> is done by the TypeResolver constructor.
    /// When <see cref="TypedConfigurationBuilder.IsCreating"/> is true, the added resolver only applies until the current call
    /// to <see cref="TypedConfigurationBuilder.Create(IActivityMonitor, Type, Microsoft.Extensions.Configuration.IConfigurationSection)"/>
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
        readonly string _compositeItemsFieldName;

        /// <summary>
        /// Initializes a new type resolver.
        /// </summary>
        /// <param name="baseType">The <see cref="BaseType"/>.</param>
        /// <param name="compositeItemsFieldName">Required field name of a composite items.</param>
        protected TypeResolver( Type baseType, string compositeItemsFieldName = "Items" )
        {
            Throw.CheckNotNullArgument( baseType );
            Throw.CheckNotNullOrWhiteSpaceArgument( compositeItemsFieldName );
            _baseType = baseType;
            _compositeItemsFieldName = compositeItemsFieldName;
        }

        /// <summary>
        /// Gets the base type handled by this resolver.
        /// </summary>
        public Type BaseType => _baseType;

        /// <summary>
        /// Gets the compsite item field name. Defaults to "Items".
        /// </summary>
        public string CompositeItemsFieldName => _compositeItemsFieldName;

        /// <summary>
        /// Attempts to create an instance from a configuration using any possible strategies
        /// to resolve its type and activating an instance.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors and warnings.</param>
        /// <param name="builder">The calling builder for which the configuration must be resolved.</param>
        /// <param name="configuration">The configuration to analyze.</param>
        /// <returns>The resulting instance or null if any error occurred.</returns>
        internal protected abstract object? Create( IActivityMonitor monitor,
                                                    TypedConfigurationBuilder builder,
                                                    ImmutableConfigurationSection configuration );
    }
}

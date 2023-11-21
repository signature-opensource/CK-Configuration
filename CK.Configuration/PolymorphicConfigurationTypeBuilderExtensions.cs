using System;

namespace CK.Core
{
    /// <summary>
    /// Extends <see cref="PolymorphicConfigurationTypeBuilder"/>.
    /// </summary>
    public static class PolymorphicConfigurationTypeBuilderExtensions
    {
        /// <summary>
        /// Adds a standard <see cref="PolymorphicConfigurationTypeBuilder.TypeResolver"/>.
        /// </summary>
        /// <remarks>
        /// This is implemented as an extension method to capture the actual builder type (no need to specify it).
        /// </remarks>
        /// <param name="b">This builder.</param>
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
        public static void AddStandardTypeResolver<TBuilder>( this TBuilder b,
                                                              Type baseType,
                                                              string typeNamespace,
                                                              bool allowOtherNamespace = false,
                                                              string? familyTypeNameSuffix = null,
                                                              Func<IActivityMonitor, string, ImmutableConfigurationSection, object?>? tryCreateFromTypeName = null,
                                                              Type? compositeBaseType = null,
                                                              string compositeItemsFieldName = "Items",
                                                              string typeFieldName = "Type",
                                                              string typeNameSuffix = "Configuration" )
            where TBuilder : PolymorphicConfigurationTypeBuilder
        {
            var _ = new PolymorphicConfigurationTypeBuilder.StandardTypeResolver<TBuilder>(
                b,
                baseType,
                typeNamespace,
                allowOtherNamespace,
                familyTypeNameSuffix,
                tryCreateFromTypeName,
                compositeBaseType,
                compositeItemsFieldName,
                typeFieldName,
                typeNameSuffix );
        }
    }
}

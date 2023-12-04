using CK.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Immutable;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Predicate placeholder.
    /// This generates an empty (null) predicate.
    /// </summary>
    public sealed class PlaceholderPredicateConfiguration : ObjectPredicateConfiguration
    {
        readonly ImmutableConfigurationSection _configuration;
        readonly AssemblyConfiguration _assemblies;
        readonly ImmutableArray<PolymorphicConfigurationTypeBuilder.TypeResolver> _resolvers;

        /// <summary>
        /// Initializes a new placeholder.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        /// <param name="builder">The builder.</param>
        /// <param name="configuration">The placeholder configuration.</param>
        public PlaceholderPredicateConfiguration( IActivityMonitor monitor,
                                                  PolymorphicConfigurationTypeBuilder builder,
                                                  ImmutableConfigurationSection configuration )
            : base( configuration.Path )
        {
            _configuration = configuration;
            _assemblies = builder.AssemblyConfiguration;
            _resolvers = builder.Resolvers.ToImmutableArray();
        }

        /// <summary>
        /// Always creates the empty (null) predicate.
        /// </summary>
        /// <param name="services">The services.</param>
        /// 
        /// <returns>The empty predicate (null).</returns>
        public override Func<object, bool>? CreatePredicate( IServiceProvider services )
        {
            return null;
        }

        /// <summary>
        /// Returns this or a new predicate configuration if <paramref name="configuration"/> is a child
        /// of this configuration.
        /// <para>
        /// Errors are emitted in the monitor. On error, this instance is returned. 
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="configuration">The configuration that will potentially replaces this placeholder.</param>
        /// <returns>A new predicate configuration or this if the section is not a child or if an error occurred.</returns>
        public override ObjectAsyncPredicateConfiguration SetPlaceholder( IActivityMonitor monitor,
                                                                          IConfigurationSection configuration )
        {
            if( configuration.GetParentPath().Equals( ConfigurationPath, StringComparison.OrdinalIgnoreCase ) )
            {
                var builder = new PolymorphicConfigurationTypeBuilder( _assemblies, _resolvers );
                if( configuration is not ImmutableConfigurationSection config )
                {
                    config = new ImmutableConfigurationSection( configuration, lookupParent: _configuration );
                }
                var newC = builder.HasBaseType<ObjectAsyncPredicateConfiguration>()
                            ? builder.Create<ObjectAsyncPredicateConfiguration>( monitor, config )
                            : builder.Create<ObjectPredicateConfiguration>( monitor, config );
                if( newC != null ) return newC;
            }
            return this;
        }

    }

}

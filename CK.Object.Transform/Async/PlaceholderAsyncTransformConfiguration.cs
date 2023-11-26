using CK.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace CK.Object.Transform
{
    /// <summary>
    /// Transform placeholder.
    /// This always generates a null transform function that is the identity function.
    /// </summary>
    public sealed class PlaceholderAsyncTransformConfiguration : ObjectAsyncTransformConfiguration
    {
        readonly AssemblyConfiguration _assemblies;
        readonly ImmutableArray<PolymorphicConfigurationTypeBuilder.TypeResolver> _resolvers;

        /// <summary>
        /// Initializes a new placeholder.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        /// <param name="builder">The builder.</param>
        /// <param name="configuration">The placeholder configuration.</param>
        public PlaceholderAsyncTransformConfiguration( IActivityMonitor monitor,
                                                       PolymorphicConfigurationTypeBuilder builder,
                                                       ImmutableConfigurationSection configuration )
            : base( configuration )
        {
            _assemblies = builder.AssemblyConfiguration;
            _resolvers = builder.Resolvers.ToImmutableArray();
        }

        /// <summary>
        /// Always creates the (null) identity function.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">The services.</param>
        /// <returns>The identity function (null).</returns>
        public override Func<object, ValueTask<object>>? CreateTransform( IActivityMonitor monitor, IServiceProvider services )
        {
            return null;
        }

        /// <summary>
        /// Returns this or a new transform configuration if <paramref name="configuration"/> is a child
        /// of this configuration.
        /// <para>
        /// Errors are emitted in the monitor. On error, this instance is returned. 
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="configuration">The configuration that will potentially replaces this placeholder.</param>
        /// <returns>A new transform configuration or this if the section is not a child or if an error occurred.</returns>
        public override ObjectAsyncTransformConfiguration SetPlaceholder( IActivityMonitor monitor,
                                                                          IConfigurationSection configuration )
        {
            if( configuration.GetParentPath().Equals( Configuration.Path, StringComparison.OrdinalIgnoreCase ) )
            {
                var builder = new PolymorphicConfigurationTypeBuilder( _assemblies, _resolvers );
                if( configuration is not ImmutableConfigurationSection config )
                {
                    config = new ImmutableConfigurationSection( configuration, lookupParent: Configuration );
                }
                var newC = builder.Create<ObjectAsyncTransformConfiguration>( monitor, config );
                if( newC != null ) return newC;
            }
            return this;
        }

    }

}
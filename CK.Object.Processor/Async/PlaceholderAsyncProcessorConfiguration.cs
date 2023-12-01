using CK.Core;
using CK.Object.Predicate;
using CK.Object.Transform;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace CK.Object.Processor
{
    /// <summary>
    /// Processor placeholder. A placholder is not allowed to have a <see cref="ObjectProcessorConfiguration.Condition"/>
    /// or a <see cref="ObjectProcessorConfiguration.Transform"/>.
    /// <para>
    /// This always generates a null processor (the void processor).
    /// </para>
    /// </summary>
    public sealed class PlaceholderAsyncProcessorConfiguration : ObjectAsyncProcessorConfiguration
    {
        readonly AssemblyConfiguration _assemblies;
        readonly ImmutableArray<PolymorphicConfigurationTypeBuilder.TypeResolver> _resolvers;

        /// <summary>
        /// Initializes a new placeholder.
        /// "Condition" and "Transform" are forbidden.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        /// <param name="builder">The builder.</param>
        /// <param name="configuration">The placeholder configuration.</param>
        public PlaceholderAsyncProcessorConfiguration( IActivityMonitor monitor,
                                                       PolymorphicConfigurationTypeBuilder builder,
                                                       ImmutableConfigurationSection configuration )
            : base( monitor, builder, configuration )
        {
            if( Condition != null || Transform != null )
            {
                monitor.Error( $"A processor Placeholder cannot define a 'Condition' or an 'Transform' (Configuration '{configuration.Path}')." );
            }
            _assemblies = builder.AssemblyConfiguration;
            _resolvers = builder.Resolvers.ToImmutableArray();
        }

        /// <summary>
        /// Always creates the (null) void processor.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">The services.</param>
        /// <returns>The void processor (null).</returns>
        public override Func<object, ValueTask<object?>>? CreateProcessor( IActivityMonitor monitor, IServiceProvider services )
        {
            return null;
        }

        /// <summary>
        /// Returns this or a new processor configuration if <paramref name="configuration"/> is a child
        /// of this configuration.
        /// <para>
        /// Errors are emitted in the monitor. On error, this instance is returned. 
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="configuration">The configuration that will potentially replaces this placeholder.</param>
        /// <returns>A new processor configuration or this if the section is not a child or if an error occurred.</returns>
        protected override ObjectAsyncProcessorConfiguration DoSetPlaceholder( IActivityMonitor monitor,
                                                                               IConfigurationSection configuration,
                                                                               ObjectPredicateConfiguration? condition,
                                                                               ObjectAsyncTransformConfiguration? action )
        {
            Throw.DebugAssert( condition == null && action == null ); 
            if( configuration.GetParentPath().Equals( Configuration.Path, StringComparison.OrdinalIgnoreCase ) )
            {
                var builder = new PolymorphicConfigurationTypeBuilder( _assemblies, _resolvers );
                if( configuration is not ImmutableConfigurationSection config )
                {
                    config = new ImmutableConfigurationSection( configuration, lookupParent: Configuration );
                }
                var newC = builder.Create<ObjectAsyncProcessorConfiguration>( monitor, config );
                if( newC != null ) return newC;
            }
            return this;
        }

    }

}

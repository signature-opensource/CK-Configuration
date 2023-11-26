using CK.Core;
using CK.Object.Predicate;
using CK.Object.Transform;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Immutable;

namespace CK.Object.Processor
{
    /// <summary>
    /// Processor placeholder.
    /// This always generates a null processor that is the void processor.
    /// </summary>
    public sealed class PlaceholderProcessorConfiguration : ObjectProcessorConfiguration
    {
        readonly AssemblyConfiguration _assemblies;
        readonly ImmutableArray<PolymorphicConfigurationTypeBuilder.TypeResolver> _resolvers;

        /// <summary>
        /// Initializes a new placeholder.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        /// <param name="builder">The builder.</param>
        /// <param name="configuration">The placeholder configuration.</param>
        public PlaceholderProcessorConfiguration( IActivityMonitor monitor,
                                                  PolymorphicConfigurationTypeBuilder builder,
                                                  ImmutableConfigurationSection configuration )
            : base( monitor, builder, configuration )
        {
            if( Condition != null || Action != null )
            {
                monitor.Error( $"A processor Placeholder cannot define a 'Condition' or an 'Action' (Configuration '{configuration.Path}')." );
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
        public override Func<object, object>? CreateProcessor( IActivityMonitor monitor, IServiceProvider services )
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
        protected override ObjectProcessorConfiguration DoSetPlaceholder( IActivityMonitor monitor,
                                                                          IConfigurationSection configuration,
                                                                          ObjectPredicateConfiguration? condition,
                                                                          ObjectTransformConfiguration? action )
        {
            Throw.DebugAssert( condition == null && action == null ); 
            if( configuration.GetParentPath().Equals( Configuration.Path, StringComparison.OrdinalIgnoreCase ) )
            {
                var builder = new PolymorphicConfigurationTypeBuilder( _assemblies, _resolvers );
                if( configuration is not ImmutableConfigurationSection config )
                {
                    config = new ImmutableConfigurationSection( configuration, lookupParent: Configuration );
                }
                var newC = builder.Create<ObjectProcessorConfiguration>( monitor, config );
                if( newC != null ) return newC;
            }
            return this;
        }

    }

}
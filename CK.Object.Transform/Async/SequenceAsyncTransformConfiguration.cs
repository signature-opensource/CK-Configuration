using CK.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace CK.Object.Transform
{
    /// <summary>
    /// Composite for synchronous transform functions: the subordinated <see cref="Transforms"/> are
    /// called in sequence (a pipeline of transformations).
    /// </summary>
    public sealed class SequenceAsyncTransformConfiguration : ObjectAsyncTransformConfiguration, ISequenceTransformConfiguration
    {
        readonly IReadOnlyList<ObjectAsyncTransformConfiguration> _transforms;

        /// <summary>
        /// Required constructor.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors and warnings.</param>
        /// <param name="builder">The builder. Can be used to instantiate other children as needed.</param>
        /// <param name="configuration">The configuration for this object.</param>
        /// <param name="transforms">The subordinated items.</param>
        public SequenceAsyncTransformConfiguration( IActivityMonitor monitor,
                                              PolymorphicConfigurationTypeBuilder builder,
                                              ImmutableConfigurationSection configuration,
                                              IReadOnlyList<ObjectAsyncTransformConfiguration> transforms )
            : base( configuration )
        {
            _transforms = transforms;
        }

        internal SequenceAsyncTransformConfiguration( ImmutableConfigurationSection configuration,
                                                      IReadOnlyList<ObjectAsyncTransformConfiguration> transforms )
            : base( configuration )
        {
            _transforms = transforms;
        }

        IReadOnlyList<IObjectTransformConfiguration> ISequenceTransformConfiguration.Transforms => _transforms;

        /// <inheritdoc cref="ISequenceTransformConfiguration.Transforms"/>
        public IReadOnlyList<ObjectAsyncTransformConfiguration> Transforms => _transforms;

        /// <summary>
        /// Composite mutator.
        /// <para>
        /// Errors are emitted in the monitor. On error, this instance is returned. 
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use to signal errors.</param>
        /// <param name="configuration">Configuration of the replaced placeholder.</param>
        /// <returns>A new configuration or this instance if an error occurred or the placeholder has not been found.</returns>
        public override ObjectAsyncTransformConfiguration SetPlaceholder( IActivityMonitor monitor,
                                                                          IConfigurationSection configuration )
        {
            Throw.CheckNotNullArgument( monitor );
            Throw.CheckNotNullArgument( configuration );

            // Bails out early if we are not concerned.
            if( !Configuration.IsChildPath( configuration.Path ) )
            {
                return this;
            }
            ImmutableArray<ObjectAsyncTransformConfiguration>.Builder? newItems = null;
            for( int i = 0; i < _transforms.Count; i++ )
            {
                var item = _transforms[i];
                var r = item.SetPlaceholder( monitor, configuration );
                if( r != item )
                {
                    if( newItems == null )
                    {
                        newItems = ImmutableArray.CreateBuilder<ObjectAsyncTransformConfiguration>( _transforms.Count );
                        newItems.AddRange( _transforms.Take( i ) );
                    }
                }
                newItems?.Add( r );
            }
            return newItems != null
                    ? new SequenceAsyncTransformConfiguration( Configuration, newItems.ToImmutableArray() )
                    : this;
        }

        /// <inheritdoc />
        public override ObjectAsyncTransformHook? CreateHook( IActivityMonitor monitor, ITransformEvaluationHook hook, IServiceProvider services )
        {
            ImmutableArray<ObjectAsyncTransformHook> transforms = _transforms.Select( c => c.CreateHook( monitor, hook, services ) )
                                                                             .Where( s => s != null )
                                                                             .ToImmutableArray()!;
            if( transforms.Length == 0 ) return null;
            if( transforms.Length == 1 ) return transforms[0];
            return new SequenceAsyncTransformHook( hook, this, transforms );
        }

        /// <inheritdoc />
        public override Func<object,ValueTask<object>>? CreateTransform( IActivityMonitor monitor, IServiceProvider services )
        {
            ImmutableArray<Func<object, ValueTask<object>>> transformers = _transforms.Select( c => c.CreateTransform( monitor, services ) )
                                                                                  .Where( s => s != null )        
                                                                                  .ToImmutableArray()!;
            if( transformers.Length == 0 ) return null;
            if( transformers.Length == 1 ) return transformers[0];
            return o => Apply( transformers, o );

            static async ValueTask<object> Apply( ImmutableArray<Func<object, ValueTask<object>>> transformers, object o )
            {
                foreach( var t in transformers )
                {
                    o = await t( o ).ConfigureAwait( false );
                }
                return o;
            }

        }
    }

}

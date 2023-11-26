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
    public sealed class SequenceTransformConfiguration : ObjectTransformConfiguration, ISequenceTransformConfiguration
    {
        readonly IReadOnlyList<ObjectTransformConfiguration> _transforms;

        /// <summary>
        /// Required constructor.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors and warnings.</param>
        /// <param name="builder">The builder. (Unused but required by the builder).</param>
        /// <param name="configuration">The configuration for this object.</param>
        /// <param name="transformers">The subordinated items.</param>
        public SequenceTransformConfiguration( IActivityMonitor monitor,
                                               PolymorphicConfigurationTypeBuilder builder,
                                               ImmutableConfigurationSection configuration,
                                               IReadOnlyList<ObjectTransformConfiguration> transformers )
            : base( configuration )
        {
            _transforms = transformers;
        }

        internal SequenceTransformConfiguration( ImmutableConfigurationSection configuration,
                                                 IReadOnlyList<ObjectTransformConfiguration> predicates )
            : base( configuration )
        {
            _transforms = predicates;
        }

        IReadOnlyList<IObjectTransformConfiguration> ISequenceTransformConfiguration.Transforms => _transforms;

        /// <inheritdoc cref="ISequenceTransformConfiguration.Transforms" />
        public IReadOnlyList<ObjectTransformConfiguration> Transforms => _transforms;

        /// <inheritdoc />
        public override ObjectTransformHook? CreateHook( IActivityMonitor monitor, ITransformEvaluationHook hook, IServiceProvider services )
        {
            ImmutableArray<ObjectTransformHook> items = _transforms.Select( c => c.CreateHook( monitor, hook, services ) )
                                                                   .Where( s => s != null )
                                                                   .ToImmutableArray()!;
            if( items.Length == 0 ) return null;
            if( items.Length == 1 ) return items[0];
            return new SequenceTransformHook( hook, this, items );
        }

        /// <inheritdoc />
        public override Func<object, object>? CreateTransform( IActivityMonitor monitor, IServiceProvider services )
        {
            ImmutableArray<Func<object, object>> items = _transforms.Select( c => c.CreateTransform( monitor, services ) )
                                                               .Where( f => f != null )
                                                               .ToImmutableArray()!;
            if( items.Length == 0 ) return null;
            if( items.Length == 1 ) return items[0];
            return o => Apply( items, o );

            static object Apply( ImmutableArray<Func<object, object>> transformers, object o )
            {
                foreach( var t in transformers )
                {
                    o = t( o );
                }
                return o;
            }
        }

        /// <summary>
        /// Composite mutator.
        /// <para>
        /// Errors are emitted in the monitor. On error, this instance is returned. 
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use to signal errors.</param>
        /// <param name="configuration">Configuration of the replaced placeholder.</param>
        /// <returns>A new configuration or this instance if an error occurred or the placeholder has not been found.</returns>
        public override ObjectTransformConfiguration SetPlaceholder( IActivityMonitor monitor,
                                                                     IConfigurationSection configuration )
        {
            Throw.CheckNotNullArgument( monitor );
            Throw.CheckNotNullArgument( configuration );

            // Bails out early if we are not concerned.
            if( !Configuration.IsChildPath( configuration.Path ) )
            {
                return this;
            }
            ImmutableArray<ObjectTransformConfiguration>.Builder? newItems = null;
            for( int i = 0; i < _transforms.Count; i++ )
            {
                var item = _transforms[i];
                var r = item.SetPlaceholder( monitor, configuration );
                if( r != item )
                {
                    if( newItems == null )
                    {
                        newItems = ImmutableArray.CreateBuilder<ObjectTransformConfiguration>( _transforms.Count );
                        newItems.AddRange( _transforms.Take( i ) );
                    }
                }
                newItems?.Add( r );
            }
            return newItems != null
                    ? new SequenceTransformConfiguration( Configuration, newItems.ToImmutableArray() )
                    : this;
        }

    }

}

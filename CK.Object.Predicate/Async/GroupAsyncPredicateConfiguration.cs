using CK.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Composite for synchronous predicates. Defaults to "And" connector but can be "Or" or specify
    /// a "AtLeast" count (for n among at least 3 conditions).
    /// </summary>
    public sealed class GroupAsyncPredicateConfiguration : ObjectAsyncPredicateConfiguration, IGroupPredicateConfiguration
    {
        readonly ImmutableArray<ObjectAsyncPredicateConfiguration> _predicates;
        readonly int _atLeast;
        readonly int _atMost;

        /// <summary>
        /// Required constructor.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors and warnings.</param>
        /// <param name="builder">The builder. Can be used to instantiate other children as needed.</param>
        /// <param name="configuration">The configuration for this object.</param>
        /// <param name="predicates">The subordinated items.</param>
        public GroupAsyncPredicateConfiguration( IActivityMonitor monitor,
                                                 PolymorphicConfigurationTypeBuilder builder,
                                                 ImmutableConfigurationSection configuration,
                                                 IReadOnlyList<ObjectAsyncPredicateConfiguration> predicates )
            : base( configuration )
        {
            _predicates = predicates.ToImmutableArray();
            (_atLeast,_atMost) = GroupPredicateConfiguration.ReadAtLeastAtMost( monitor, configuration, predicates.Count );
        }

        internal GroupAsyncPredicateConfiguration( int knownAtLeast,
                                                   int knownAtMost,
                                                   ImmutableConfigurationSection configuration,
                                                   ImmutableArray<ObjectAsyncPredicateConfiguration> predicates )
            : base( configuration )
        {
            Throw.DebugAssert( knownAtLeast >= 0 && (predicates.Length < 2 || knownAtLeast < predicates.Length) );
            Throw.DebugAssert( knownAtMost == 0 || knownAtMost >= knownAtLeast );
            _predicates = predicates;
            _atLeast = knownAtLeast;
            _atMost = knownAtMost;
        }

        /// <inheritdoc />
        public bool Any => _atLeast == 1 && _atMost == 0;

        /// <inheritdoc />
        public bool All => _atLeast == 0 && _atMost == 0;

        /// <inheritdoc />
        public bool Single => _atLeast == 0 && _atMost == 1;

        /// <inheritdoc />
        public int AtLeast => _atLeast;

        /// <inheritdoc />
        public int AtMost => _atMost;

        IReadOnlyList<IObjectPredicateConfiguration> IGroupPredicateConfiguration.Predicates => _predicates;

        /// <inheritdoc cref="IGroupPredicateConfiguration.Predicates"/>
        public IReadOnlyList<ObjectAsyncPredicateConfiguration> Predicates => _predicates;

        /// <summary>
        /// Composite mutator.
        /// <para>
        /// Errors are emitted in the monitor. On error, this instance is returned. 
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use to signal errors.</param>
        /// <param name="configuration">Configuration of the replaced placeholder.</param>
        /// <returns>A new configuration or this instance if an error occurred or the placeholder has not been found.</returns>
        public override ObjectAsyncPredicateConfiguration SetPlaceholder( IActivityMonitor monitor,
                                                                          IConfigurationSection configuration )
        {
            Throw.CheckNotNullArgument( monitor );
            Throw.CheckNotNullArgument( configuration );

            // Bails out early if we are not concerned.
            if( !Configuration.IsChildPath( configuration.Path ) )
            {
                return this;
            }
            ImmutableArray<ObjectAsyncPredicateConfiguration>.Builder? newItems = null;
            for( int i = 0; i < _predicates.Length; i++ )
            {
                var item = _predicates[i];
                var r = item.SetPlaceholder( monitor, configuration );
                if( r != item )
                {
                    if( newItems == null )
                    {
                        newItems = ImmutableArray.CreateBuilder<ObjectAsyncPredicateConfiguration>( _predicates.Length );
                        newItems.AddRange( _predicates, i );
                    }
                }
                newItems?.Add( r );
            }
            return newItems != null
                    ? new GroupAsyncPredicateConfiguration( _atLeast, _atMost, Configuration, newItems.ToImmutable() )
                    : this;
        }

        /// <inheritdoc />
        public override ObjectAsyncPredicateHook? CreateAsyncHook( IActivityMonitor monitor, PredicateHookContext hook, IServiceProvider services )
        {
            ImmutableArray<ObjectAsyncPredicateHook> predicates = _predicates.Select( c => c.CreateAsyncHook( monitor, hook, services ) )
                                                                             .Where( s => s != null )
                                                                             .ToImmutableArray()!;
            if( predicates.Length == 0 ) return null;
            if( predicates.Length == 1 ) return predicates[0];
            return new GroupAsyncPredicateHook( hook, this, predicates );
        }

        /// <inheritdoc />
        public override Func<object,ValueTask<bool>>? CreateAsyncPredicate( IActivityMonitor monitor, IServiceProvider services )
        {
            ImmutableArray<Func<object, ValueTask<bool>>> predicates = _predicates.Select( c => c.CreateAsyncPredicate( monitor, services ) )
                                                                                  .Where( s => s != null )        
                                                                                  .ToImmutableArray()!;
            if( predicates.Length == 0 ) return null;
            if( predicates.Length == 1 ) return predicates[0];
            if( _atMost == 0 )
            {
                return _atLeast switch
                {
                    0 => o => AllAsync( predicates, o ),
                    1 => o => AnyAsync( predicates, o ),
                    _ => o => AtLeastAsync( predicates, o, _atLeast )
                };
            }
            return o => MatchBetweenAsync( predicates, o, _atLeast, _atMost );
        }

        static async ValueTask<bool> AllAsync( ImmutableArray<Func<object, ValueTask<bool>>> predicates, object o )
        {
            foreach( var p in predicates )
            {
                if( !await p( o ) ) return false;
            }
            return true;
        }

        static async ValueTask<bool> AnyAsync( ImmutableArray<Func<object, ValueTask<bool>>> predicates, object o )
        {
            foreach( var p in predicates )
            {
                if( await p( o ) ) return true;
            }
            return false;
        }

        static async ValueTask<bool> AtLeastAsync( ImmutableArray<Func<object, ValueTask<bool>>> predicates, object o, int atLeast )
        {
            int c = 0;
            foreach( var p in predicates )
            {
                if( await p( o ) )
                {
                    if( ++c == atLeast ) return true; 
                }
            }
            return false;
        }

        static async ValueTask<bool> MatchBetweenAsync( ImmutableArray<Func<object, ValueTask<bool>>> predicates, object o, int atLeast, int atMost )
        {
            int c = 0;
            foreach( var p in predicates )
            {
                if( await p( o ) )
                {
                    if( ++c > atMost ) return false;
                }
            }
            return c >= atLeast;
        }

    }

}

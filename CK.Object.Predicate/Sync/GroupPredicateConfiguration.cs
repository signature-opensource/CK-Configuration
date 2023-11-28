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
    public sealed class GroupPredicateConfiguration : ObjectPredicateConfiguration, IGroupPredicateConfiguration
    {
        readonly ImmutableArray<ObjectPredicateConfiguration> _predicates;
        readonly int _atLeast;
        readonly int _atMost;

        /// <summary>
        /// Required constructor.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors and warnings.</param>
        /// <param name="builder">The builder. (Unused but required by the builder).</param>
        /// <param name="configuration">The configuration for this object.</param>
        /// <param name="predicates">The subordinated items.</param>
        public GroupPredicateConfiguration( IActivityMonitor monitor,
                                            PolymorphicConfigurationTypeBuilder builder,
                                            ImmutableConfigurationSection configuration,
                                            IReadOnlyList<ObjectPredicateConfiguration> predicates )
            : base( configuration )
        {
            _predicates = predicates.ToImmutableArray();
            (_atLeast,_atMost) = ReadAtLeastAtMost( monitor, configuration, predicates.Count );
        }

        internal GroupPredicateConfiguration( int knownAtLeast,
                                              int knownAtMost,
                                              ImmutableConfigurationSection configuration,
                                              ImmutableArray<ObjectPredicateConfiguration> predicates )
            : base( configuration )
        {
            Throw.DebugAssert( knownAtLeast >= 0 && (predicates.Length < 2 || knownAtLeast < predicates.Length) );
            Throw.DebugAssert( knownAtMost == 0 || knownAtMost >= knownAtLeast );
            _predicates = predicates;
            _atLeast = knownAtLeast;
            _atMost = knownAtMost;
        }

        internal static (int, int) ReadAtLeastAtMost( IActivityMonitor monitor, ImmutableConfigurationSection configuration, int predicatesCount )
        {
            int atLeast = 0;
            int atMost = 0;
            var cAny = configuration.TryGetBooleanValue( monitor, "Any" );
            if( cAny.HasValue && cAny.Value )
            {
                atLeast = 1;
                if( configuration["AtLeast"] != null || configuration["AtMost"] != null || configuration["Single"] != null )
                {
                    monitor.Warn( $"Configuration '{configuration.Path}:Any' is true. 'AtLeast', 'AtMost' and 'Single' are ignored." );
                }
            }
            else
            {
                var cSingle = configuration.TryGetBooleanValue( monitor, "Single" );
                if( cSingle.HasValue && cSingle.Value )
                {
                    atMost = atLeast = 1;
                    if( configuration["AtLeast"] != null || configuration["AtMost"] != null )
                    {
                        monitor.Warn( $"Configuration '{configuration.Path}:Single' is true. 'AtLeast' and 'AtMost' are ignored." );
                    }
                }
                else
                {
                    var fM = configuration.TryGetIntValue( monitor, "AtMost", 1 );
                    if( fM.HasValue )
                    {
                        atMost = fM.Value;
                        if( atMost >= predicatesCount )
                        {
                            atMost = 0;
                            monitor.Warn( $"Configuration '{configuration.Path}:AtMost = {fM.Value}' exceeds number of predicates ({predicatesCount}. This is useless." );
                        }
                    }
                    var fL = configuration.TryGetIntValue( monitor, "AtLeast" );
                    if( fL.HasValue )
                    {
                        atLeast = fL.Value;
                        if( atLeast >= predicatesCount )
                        {
                            atLeast = 0;
                            monitor.Warn( $"Configuration '{configuration.Path}:AtLeast = {fL.Value}' exceeds number of predicates ({predicatesCount}. This is useless." );
                        }
                    }
                    if( atMost > 0 && atMost < atLeast )
                    {
                        atMost = atLeast;
                        monitor.Warn( $"Configuration '{configuration.Path}:AtMost' is lower than 'AtLeast'. Considering exactly {atLeast} conditions." );
                    }
                }

            }
            return (atLeast, atMost);
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

        /// <inheritdoc cref="IGroupPredicateConfiguration.Predicates" />
        public IReadOnlyList<ObjectPredicateConfiguration> Predicates => _predicates;

        /// <inheritdoc />
        public override ObjectPredicateHook? CreateHook( IActivityMonitor monitor, PredicateHookContext hook, IServiceProvider services )
        {
            ImmutableArray<ObjectPredicateHook> items = _predicates.Select( c => c.CreateHook( monitor, hook, services ) )
                                                                   .Where( s => s != null )
                                                                   .ToImmutableArray()!;
            if( items.Length == 0 ) return null;
            if( items.Length == 1 ) return items[0];
            return new GroupPredicateHook( hook, this, items );
        }

        /// <inheritdoc />
        public override Func<object, bool>? CreatePredicate( IActivityMonitor monitor, IServiceProvider services )
        {
            ImmutableArray<Func<object, bool>> items = _predicates.Select( c => c.CreatePredicate( monitor, services ) )
                                                               .Where( f => f != null )
                                                               .ToImmutableArray()!;
            if( items.Length == 0 ) return null;
            if( items.Length == 1 ) return items[0];
            // Easy case.
            if( _atMost == 0 )
            {
                return _atLeast switch
                {
                    0 => o => items.All( f => f( o ) ),
                    1 => o => items.Any( f => f( o ) ),
                    _ => o => AtLeastMatch( items, o, _atLeast )
                };
            }
            else
            {
                return _atLeast switch
                {
                    0 => o => AtMostMatch( items, o, _atMost ),
                    _ => o => MatchBetween( items, o, _atLeast, _atMost )
                };
            }

            static bool AtLeastMatch( ImmutableArray<Func<object, bool>> predicates, object o, int atLeast )
            {
                int c = 0;
                foreach( var f in predicates )
                {
                    if( f( o ) )
                    {
                        if( ++c == atLeast ) return true;
                    }
                }
                return false;
            }

            static bool AtMostMatch( ImmutableArray<Func<object, bool>> predicates, object o, int atMost )
            {
                int c = 0;
                foreach( var f in predicates )
                {
                    if( f( o ) )
                    {
                        if( ++c > atMost ) return false;
                    }
                }
                return true;
            }

            static bool MatchBetween( ImmutableArray<Func<object, bool>> predicates, object o, int atLeast, int atMost )
            {
                int c = 0;
                foreach( var f in predicates )
                {
                    if( f( o ) )
                    {
                        if( ++c > atMost ) return false;
                        if( c == atLeast ) return true;
                    }
                }
                return false;
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
        public override ObjectPredicateConfiguration SetPlaceholder( IActivityMonitor monitor,
                                                                     IConfigurationSection configuration )
        {
            Throw.CheckNotNullArgument( monitor );
            Throw.CheckNotNullArgument( configuration );

            // Bails out early if we are not concerned.
            if( !Configuration.IsChildPath( configuration.Path ) )
            {
                return this;
            }
            ImmutableArray<ObjectPredicateConfiguration>.Builder? newItems = null;
            for( int i = 0; i < _predicates.Length; i++ )
            {
                var item = _predicates[i];
                var r = item.SetPlaceholder( monitor, configuration );
                if( r != item )
                {
                    if( newItems == null )
                    {
                        newItems = ImmutableArray.CreateBuilder<ObjectPredicateConfiguration>( _predicates.Length );
                        newItems.AddRange( _predicates, i );
                    }
                }
                newItems?.Add( r );
            }
            return newItems != null
                    ? new GroupPredicateConfiguration( _atLeast, _atMost, Configuration, newItems.ToImmutable() )
                    : this;
        }

    }

}

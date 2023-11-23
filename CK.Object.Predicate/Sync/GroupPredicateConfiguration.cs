using CK.Core;
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
        readonly IReadOnlyList<ObjectPredicateConfiguration> _predicates;
        readonly int _atLeast;

        /// <summary>
        /// Required constructor.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors and warnings.</param>
        /// <param name="builder">The builder. Can be used to instantiate other children as needed.</param>
        /// <param name="configuration">The configuration for this object.</param>
        /// <param name="predicates">The subordinated items.</param>
        public GroupPredicateConfiguration( IActivityMonitor monitor,
                                            PolymorphicConfigurationTypeBuilder builder,
                                            ImmutableConfigurationSection configuration,
                                            IReadOnlyList<ObjectPredicateConfiguration> predicates )
            : base( monitor, builder, configuration )
        {
            _predicates = predicates;
            _atLeast = ReadAtLeast( monitor, configuration, predicates.Count );
        }

        internal GroupPredicateConfiguration( IActivityMonitor monitor,
                                              int knownAtLeast,
                                              PolymorphicConfigurationTypeBuilder builder,
                                              ImmutableConfigurationSection configuration,
                                              IReadOnlyList<ObjectPredicateConfiguration> predicates )
            : base( monitor, builder, configuration )
        {
            _predicates = predicates;
            _atLeast = knownAtLeast;
        }

        internal static int ReadAtLeast( IActivityMonitor monitor, ImmutableConfigurationSection configuration, int predicatesCount )
        {
            int atLeast = 0;
            var cAny = configuration.TryGetBooleanValue( monitor, "Any" );
            if( cAny.HasValue && cAny.Value )
            {
                atLeast = 1;
                if( configuration["AtLeast"] != null )
                {
                    monitor.Warn( $"Configuration '{configuration.Path}:Any' is true. 'AtLeast' is ignored." );
                }
            }
            else
            {
                var f = configuration.TryGetIntValue( monitor, "AtLeast" );
                if( f.HasValue )
                {
                    atLeast = f.Value;
                    if( atLeast >= predicatesCount )
                    {
                        atLeast = 0;
                        monitor.Warn( $"Configuration '{configuration.Path}:AtLeast = {f.Value}' exceeds number of predicates ({predicatesCount}. This is a 'All'." );
                    }
                }
            }
            return atLeast;
        }

        /// <inheritdoc />
        public bool Any => _atLeast == 1;

        /// <inheritdoc />
        public bool All => _atLeast == 0;

        /// <inheritdoc />
        public int AtLeast => _atLeast;

        IReadOnlyList<IObjectPredicateConfiguration> IGroupPredicateConfiguration.Predicates => _predicates;

        /// <inheritdoc cref="IGroupPredicateConfiguration.Predicates" />
        public IReadOnlyList<ObjectPredicateConfiguration> Predicates => _predicates;

        /// <inheritdoc />
        public override ObjectPredicateHook? CreateHook( IActivityMonitor monitor, IPredicateEvaluationHook hook, IServiceProvider services )
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
            return _atLeast switch
            {
                0 => o => items.All( f => f( o ) ),
                1 => o => items.Any( f => f( o ) ),
                _ => o => AtLeastMatch( items, o, _atLeast )
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
    }

}

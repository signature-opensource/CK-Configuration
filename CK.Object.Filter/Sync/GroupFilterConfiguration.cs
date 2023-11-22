using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace CK.Object.Filter
{
    /// <summary>
    /// Composite for synchronous filters. Defaults to "And" connector but can be "Or" or specify
    /// a "FilterCount" (for n among at least 3 conditions).
    /// </summary>
    public sealed class GroupFilterConfiguration : ObjectFilterConfiguration, IGroupFilterConfiguration
    {
        readonly IReadOnlyList<ObjectFilterConfiguration> _filters;
        readonly int _atLeast;

        /// <summary>
        /// Required constructor.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors and warnings.</param>
        /// <param name="builder">The builder. Can be used to instantiate other children as needed.</param>
        /// <param name="configuration">The configuration for this object.</param>
        /// <param name="filters">The subordinated items.</param>
        public GroupFilterConfiguration( IActivityMonitor monitor,
                                               PolymorphicConfigurationTypeBuilder builder,
                                               ImmutableConfigurationSection configuration,
                                               IReadOnlyList<ObjectFilterConfiguration> filters )
            : base( monitor, builder, configuration )
        {
            _filters = filters;
            _atLeast = ReadCount( monitor, configuration, filters.Count );
        }

        internal GroupFilterConfiguration( IActivityMonitor monitor,
                                                 int knownAtLeast,
                                                 PolymorphicConfigurationTypeBuilder builder,
                                                 ImmutableConfigurationSection configuration,
                                                 IReadOnlyList<ObjectFilterConfiguration> filters )
            : base( monitor, builder, configuration )
        {
            Throw.DebugAssert( knownAtLeast >= 0 && knownAtLeast < filters.Count );
            _filters = filters;
            _atLeast = knownAtLeast;
        }

        internal static int ReadCount( IActivityMonitor monitor, ImmutableConfigurationSection configuration, int filtersCount )
        {
            int filterCount = 0;
            var cAny = configuration.TryGetBooleanValue( monitor, "Any" );
            if( cAny.HasValue && cAny.Value )
            {
                filterCount = 1;
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
                    filterCount = f.Value;
                    if( filterCount >= filtersCount )
                    {
                        filterCount = 0;
                        monitor.Warn( $"Configuration '{configuration.Path}:AtLeast = {f.Value}' exceeds number of filters ({filtersCount}. This is a 'All'." );
                    }
                }
            }
            return filterCount;
        }

        /// <inheritdoc />
        public bool Any => _atLeast == 1;

        /// <inheritdoc />
        public bool All => _atLeast == 0;

        /// <inheritdoc />
        public int AtLeast => _atLeast;

        IReadOnlyList<IObjectFilterConfiguration> IGroupFilterConfiguration.Filters => _filters;

        /// <inheritdoc cref="IGroupFilterConfiguration.Filters" />
        public IReadOnlyList<ObjectFilterConfiguration> Filters => _filters;

        /// <summary>
        /// Overridden to return a <see cref="GroupFilterHook"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="hook">The evaluation hook.</param>
        /// <param name="services">The services.</param>
        /// <returns>A configured hook for this group bound to the <paramref name="hook"/>.</returns>
        public override ObjectFilterHook CreateHook( IActivityMonitor monitor, EvaluationHook hook, IServiceProvider services )
        {
            var items = _filters.Select( c => c.CreateHook( monitor, hook, services ) )
                                .ToImmutableArray()!;
            return new GroupFilterHook( hook, this, items );
        }

        /// <inheritdoc />
        public override Func<object, bool> CreatePredicate( IActivityMonitor monitor, IServiceProvider services )
        {
            var items = CreateFilters( monitor, services );
            return _atLeast switch
            {
                0 => o => items.All( f => f( o ) ),
                1 => o => items.Any( f => f( o ) ),
                _ => o => AtLeastMatch( items, o, _atLeast )
            };
        }

        static bool AtLeastMatch( ImmutableArray<Func<object, bool>> filters, object o, int atLeast )
        {
            int c = 0;
            foreach( var f in filters )
            {
                if( f( o ) )
                {
                    if( ++c == atLeast ) return true;
                }
            }
            return false;
        }


        ImmutableArray<Func<object, bool>> CreateFilters( IActivityMonitor monitor, IServiceProvider services )
        {
            return _filters.Select( c => c.CreatePredicate( monitor, services ) )
                           .ToImmutableArray();
        }
    }

}

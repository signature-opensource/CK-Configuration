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
    public sealed class GroupAsyncFilterConfiguration : ObjectAsyncFilterConfiguration, IGroupFilterConfiguration
    {
        readonly IReadOnlyList<ObjectAsyncFilterConfiguration> _filters;
        readonly int _atLeast;

        /// <summary>
        /// Required constructor.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors and warnings.</param>
        /// <param name="builder">The builder. Can be used to instantiate other children as needed.</param>
        /// <param name="configuration">The configuration for this object.</param>
        /// <param name="filters">The subordinated items.</param>
        public GroupAsyncFilterConfiguration( IActivityMonitor monitor,
                                              PolymorphicConfigurationTypeBuilder builder,
                                              ImmutableConfigurationSection configuration,
                                              IReadOnlyList<ObjectAsyncFilterConfiguration> filters )
            : base( configuration )
        {
            _filters = filters;
            _atLeast = GroupFilterConfiguration.ReadCount( monitor, configuration, filters.Count );
        }

        internal GroupAsyncFilterConfiguration( IActivityMonitor monitor,
                                                int knownAtLeast,
                                                ImmutableConfigurationSection configuration,
                                                IReadOnlyList<ObjectAsyncFilterConfiguration> filters )
            : base( configuration )
        {
            _filters = filters;
            _atLeast = knownAtLeast;
        }

        /// <inheritdoc />
        public bool Any => _atLeast == 1;

        /// <inheritdoc />
        public bool All => _atLeast == 0;

        /// <inheritdoc />
        public int AtLeast => _atLeast;

        IReadOnlyList<IObjectFilterConfiguration> IGroupFilterConfiguration.Filters => _filters;

        /// <inheritdoc cref="IGroupFilterConfiguration.Filters"/>
        public IReadOnlyList<ObjectAsyncFilterConfiguration> Filters => _filters;

        /// <summary>
        /// Overridden to return a <see cref="GroupAsyncFilterHook"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="hook">The evaluation hook.</param>
        /// <param name="services">The services.</param>
        /// <returns>A configured filter for this group bound to the evaluation hook or null for an empty filter.</returns>
        public override ObjectAsyncFilterHook? CreateHook( IActivityMonitor monitor, EvaluationHook hook, IServiceProvider services )
        {
            ImmutableArray<ObjectAsyncFilterHook> items = _filters.Select( c => c.CreateHook( monitor, hook, services ) )
                                                                  .Where( s => s != null )
                                                                  .ToImmutableArray()!;
            if( items.Length == 0 ) return null;
            if( items.Length == 1 ) return items[1];
            return new GroupAsyncFilterHook( hook, this, items );
        }

        /// <inheritdoc />
        public override Func<object,ValueTask<bool>>? CreatePredicate( IActivityMonitor monitor, IServiceProvider services )
        {
            ImmutableArray<Func<object, ValueTask<bool>>> items = _filters.Select( c => c.CreatePredicate( monitor, services ) )
                                                                          .Where( s => s != null )        
                                                                          .ToImmutableArray()!;
            if( items.Length == 0 ) return null;
            if( items.Length == 1 ) return items[1];
            return _atLeast switch
            {
                0 => o => AllAsync( items, o ),
                1 => o => AnyAsync( items, o ),
                _ => o => AtLeastAsync( items, o, _atLeast )
            };
        }

        static async ValueTask<bool> AllAsync( ImmutableArray<Func<object, ValueTask<bool>>> filters, object o )
        {
            foreach( var f in filters )
            {
                if( !await f( o ) ) return false;
            }
            return true;
        }

        static async ValueTask<bool> AnyAsync( ImmutableArray<Func<object, ValueTask<bool>>> filters, object o )
        {
            foreach( var f in filters )
            {
                if( await f( o ) ) return true;
            }
            return false;
        }

        static async ValueTask<bool> AtLeastAsync( ImmutableArray<Func<object, ValueTask<bool>>> filters, object o, int filterCount )
        {
            int c = 0;
            foreach( var f in filters )
            {
                if( await f( o ) )
                {
                    if( ++c == filterCount ) return true; 
                }
            }
            return false;
        }
    }

}

using CK.Core;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Hook implementation for group of synchronous predicates.
    /// </summary>
    public class GroupPredicateHook : ObjectPredicateHook, IGroupPredicateHook
    {
        readonly ImmutableArray<ObjectPredicateHook> _items;

        /// <summary>
        /// Initializes a new wrapper without specific behavior.
        /// </summary>
        /// <param name="configuration">The predicate configuration.</param>
        /// <param name="hook">The evaluation hook.</param>
        /// <param name="items">The subordinated predicates.</param>
        public GroupPredicateHook( IPredicateEvaluationHook hook, IGroupPredicateConfiguration configuration, ImmutableArray<ObjectPredicateHook> items )
            : base( hook, configuration )
        {
            Throw.CheckNotNullArgument( items );
            _items = items;
        }

        /// <inheritdoc />
        public new IGroupPredicateConfiguration Configuration => Unsafe.As<IGroupPredicateConfiguration>( base.Configuration );

        ImmutableArray<IObjectPredicateHook> IGroupPredicateHook.Predicates => ImmutableArray<IObjectPredicateHook>.CastUp( _items );

        /// <inheritdoc cref="IGroupPredicateHook.Predicates" />
        public ImmutableArray<ObjectPredicateHook> Items => _items;

        protected override bool DoEvaluate( object o )
        {
            var atLeast = Configuration.AtLeast;
            switch( atLeast )
            {
                case 0: return _items.All( i => i.Evaluate( o ) );
                case 1: return _items.Any( i => i.Evaluate( o ) );
                default:
                    int c = 0;
                    foreach( var i in _items )
                    {
                        if( i.Evaluate( o ) )
                        {
                            if( ++c == atLeast ) return true;
                        }
                    }
                    return false;
            };
        }
    }

}

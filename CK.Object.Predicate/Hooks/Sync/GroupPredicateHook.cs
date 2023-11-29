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
        readonly ImmutableArray<ObjectPredicateHook> _predicates;

        /// <summary>
        /// Initializes a new hook.
        /// </summary>
        /// <param name="configuration">The predicate configuration.</param>
        /// <param name="context">The hook context.</param>
        /// <param name="predicates">The subordinated predicates.</param>
        public GroupPredicateHook( PredicateHookContext context,
                                   IGroupPredicateConfiguration configuration,
                                   ImmutableArray<ObjectPredicateHook> predicates )
            : base( context, configuration )
        {
            Throw.CheckNotNullArgument( predicates );
            _predicates = predicates;
        }

        /// <inheritdoc />
        public new IGroupPredicateConfiguration Configuration => Unsafe.As<IGroupPredicateConfiguration>( base.Configuration );

        ImmutableArray<IObjectPredicateHook> IGroupPredicateHook.Predicates => ImmutableArray<IObjectPredicateHook>.CastUp( _predicates );

        /// <inheritdoc cref="IGroupPredicateHook.Predicates" />
        public ImmutableArray<ObjectPredicateHook> Predicates => _predicates;

        /// <inheritdoc />
        protected override bool DoEvaluate( object o )
        {
            var atLeast = Configuration.AtLeast;
            var atMost = Configuration.AtMost;
            if( atMost == 0 )
            {
                switch( atLeast )
                {
                    case 0: return _predicates.All( i => i.Evaluate( o ) );
                    case 1: return _predicates.Any( i => i.Evaluate( o ) );
                    default:
                        int c = 0;
                        foreach( var i in _predicates )
                        {
                            if( i.Evaluate( o ) )
                            {
                                if( ++c == atLeast ) return true;
                            }
                        }
                        return false;
                };
            }
            else
            {
                int c = 0;
                foreach( var i in _predicates )
                {
                    if( i.Evaluate( o ) )
                    {
                        if( ++c > atMost ) return false;
                    }
                }
                return c >= atLeast;
            }
        }
    }

}

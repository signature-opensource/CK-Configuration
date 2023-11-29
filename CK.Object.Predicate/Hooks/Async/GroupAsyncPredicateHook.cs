using CK.Core;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Hook implementation for group of asynchronous predicates.
    /// </summary>
    public class GroupAsyncPredicateHook : ObjectAsyncPredicateHook, IGroupPredicateHook
    {
        readonly ImmutableArray<ObjectAsyncPredicateHook> _predicates;

        /// <summary>
        /// Initializes a new hook.
        /// </summary>
        /// <param name="context">The hook context.</param>
        /// <param name="configuration">The predicate configuration.</param>
        /// <param name="predicates">The subordinated predicates.</param>
        public GroupAsyncPredicateHook( PredicateHookContext context, IGroupPredicateConfiguration configuration, ImmutableArray<ObjectAsyncPredicateHook> predicates )
            : base( context, configuration )
        {
            Throw.CheckNotNullArgument( predicates );
            _predicates = predicates;
        }

        /// <inheritdoc />
        public new IGroupPredicateConfiguration Configuration => Unsafe.As<IGroupPredicateConfiguration>( base.Configuration );

        ImmutableArray<IObjectPredicateHook> IGroupPredicateHook.Predicates => ImmutableArray<IObjectPredicateHook>.CastUp( _predicates );

        /// <inheritdoc cref="IGroupPredicateHook.Predicates" />
        public ImmutableArray<ObjectAsyncPredicateHook> Predicates => _predicates;

        /// <inheritdoc />
        protected override ValueTask<bool> DoEvaluateAsync( object o )
        {
            var atLeast = Configuration.AtLeast;
            var atMost = Configuration.AtMost;
            if( atMost == 0 )
            {
                return atLeast switch
                {
                    0 => AllAsync( _predicates, o ),
                    1 => AnyAsync( _predicates, o ),
                    _ => AtLeastAsync( _predicates, o, atLeast )
                };
            }
            return MatchBetweenAsync( _predicates, o, atLeast, atMost );
        }

        static async ValueTask<bool> AllAsync( ImmutableArray<ObjectAsyncPredicateHook> items, object o )
        {
            foreach( var p in items )
            {
                if( !await p.EvaluateAsync( o ).ConfigureAwait( false ) ) return false;
            }
            return true;
        }

        static async ValueTask<bool> AnyAsync( ImmutableArray<ObjectAsyncPredicateHook> items, object o )
        {
            foreach( var p in items )
            {
                if( await p.EvaluateAsync( o ).ConfigureAwait( false ) ) return true;
            }
            return false;
        }

        static async ValueTask<bool> AtLeastAsync( ImmutableArray<ObjectAsyncPredicateHook> items, object o, int atLeast )
        {
            int c = 0;
            foreach( var p in items )
            {
                if( await p.EvaluateAsync( o ).ConfigureAwait( false ) )
                {
                    if( ++c == atLeast ) return true;
                }
            }
            return false;
        }

        static async ValueTask<bool> MatchBetweenAsync( ImmutableArray<ObjectAsyncPredicateHook> items, object o, int atLeast, int atMost )
        {
            int c = 0;
            foreach( var p in items )
            {
                if( await p.EvaluateAsync( o ).ConfigureAwait( false ) )
                {
                    if( ++c > atMost ) return false;
                }
            }
            return c >= atLeast;
        }

    }

}

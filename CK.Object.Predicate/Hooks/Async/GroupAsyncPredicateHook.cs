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
        /// Initializes a new wrapper without specific behavior.
        /// </summary>
        /// <param name="hook">The evaluation hook.</param>
        /// <param name="configuration">The predicate configuration.</param>
        /// <param name="predicates">The subordinated predicates.</param>
        public GroupAsyncPredicateHook( IPredicateEvaluationHook hook, IGroupPredicateConfiguration configuration, ImmutableArray<ObjectAsyncPredicateHook> predicates )
            : base( hook, configuration )
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
        protected override async ValueTask<bool> DoEvaluateAsync( object o )
        {
            var atLeast = Configuration.AtLeast;
            var r = atLeast switch
            {
                0 => await AllAsync( _predicates, o ).ConfigureAwait( false ),
                1 => await AnyAsync( _predicates, o ).ConfigureAwait( false ),
                _ => await AtLeastAsync( _predicates, o, atLeast ).ConfigureAwait( false )
            };
            return r;
        }

        static async ValueTask<bool> AllAsync( ImmutableArray<ObjectAsyncPredicateHook> items, object o )
        {
            foreach( var i in items )
            {
                if( !await i.EvaluateAsync( o ).ConfigureAwait( false ) ) return false;
            }
            return true;
        }

        static async ValueTask<bool> AnyAsync( ImmutableArray<ObjectAsyncPredicateHook> items, object o )
        {
            foreach( var i in items )
            {
                if( await i.EvaluateAsync( o ).ConfigureAwait( false ) ) return true;
            }
            return false;
        }

        static async ValueTask<bool> AtLeastAsync( ImmutableArray<ObjectAsyncPredicateHook> items, object o, int atLeast )
        {
            int c = 0;
            foreach( var i in items )
            {
                if( await i.EvaluateAsync( o ).ConfigureAwait( false ) )
                {
                    if( ++c == atLeast ) return true;
                }
            }
            return false;
        }

    }

}

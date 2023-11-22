using CK.Core;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CK.Object.Filter
{
    /// <summary>
    /// Hook implementation for group of asynchronous predicates.
    /// </summary>
    public class GroupAsyncFilterHook : ObjectAsyncFilterHook, IGroupFilterHook
    {
        readonly ImmutableArray<ObjectAsyncFilterHook> _items;

        /// <summary>
        /// Initializes a new wrapper without specific behavior.
        /// </summary>
        /// <param name="hook">The evaluation hook.</param>
        /// <param name="configuration">The filter configuration.</param>
        /// <param name="items">The subordinated predicates.</param>
        public GroupAsyncFilterHook( EvaluationHook hook, IGroupFilterConfiguration configuration, ImmutableArray<ObjectAsyncFilterHook> items )
            : base( hook, configuration )
        {
            Throw.CheckNotNullArgument( items );
            _items = items;
        }

        /// <inheritdoc />
        public new IGroupFilterConfiguration Configuration => Unsafe.As<IGroupFilterConfiguration>( base.Configuration );

        ImmutableArray<IObjectFilterHook> IGroupFilterHook.Items => ImmutableArray<IObjectFilterHook>.CastUp( _items );

        /// <inheritdoc cref="IGroupFilterHook.Items" />
        public ImmutableArray<ObjectAsyncFilterHook> Items => _items;

        /// <inheritdoc />
        protected override async ValueTask<bool> DoEvaluateAsync( object o )
        {
            var atLeast = Configuration.AtLeast;
            var r = atLeast switch
            {
                0 => await AllAsync( _items, o ).ConfigureAwait( false ),
                1 => await AnyAsync( _items, o ).ConfigureAwait( false ),
                _ => await AtLeastAsync( _items, o, atLeast ).ConfigureAwait( false )
            };
            return r;
        }

        static async ValueTask<bool> AllAsync( ImmutableArray<ObjectAsyncFilterHook> items, object o )
        {
            foreach( var i in items )
            {
                if( !await i.EvaluateAsync( o ).ConfigureAwait( false ) ) return false;
            }
            return true;
        }

        static async ValueTask<bool> AnyAsync( ImmutableArray<ObjectAsyncFilterHook> items, object o )
        {
            foreach( var i in items )
            {
                if( await i.EvaluateAsync( o ).ConfigureAwait( false ) ) return true;
            }
            return false;
        }

        static async ValueTask<bool> AtLeastAsync( ImmutableArray<ObjectAsyncFilterHook> items, object o, int filterCount )
        {
            int c = 0;
            foreach( var i in items )
            {
                if( await i.EvaluateAsync( o ).ConfigureAwait( false ) )
                {
                    if( ++c == filterCount ) return true;
                }
            }
            return false;
        }

    }

}

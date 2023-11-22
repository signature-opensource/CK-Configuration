using CK.Core;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CK.Object.Filter
{
    /// <summary>
    /// Hook implementation for group of synchronous predicates.
    /// </summary>
    public class GroupFilterHook : ObjectFilterHook, IGroupFilterHook
    {
        readonly ImmutableArray<ObjectFilterHook> _items;

        /// <summary>
        /// Initializes a new wrapper without specific behavior.
        /// </summary>
        /// <param name="configuration">The filter configuration.</param>
        /// <param name="hook">The evaluation hook.</param>
        /// <param name="items">The subordinated predicates.</param>
        public GroupFilterHook( EvaluationHook hook, IGroupFilterConfiguration configuration, ImmutableArray<ObjectFilterHook> items )
            : base( hook, configuration )
        {
            Throw.CheckNotNullArgument( items );
            _items = items;
        }

        /// <inheritdoc />
        public new IGroupFilterConfiguration Configuration => Unsafe.As<IGroupFilterConfiguration>( base.Configuration );

        ImmutableArray<IObjectFilterHook> IGroupFilterHook.Items => ImmutableArray<IObjectFilterHook>.CastUp( _items );

        /// <inheritdoc cref="IGroupFilterHook.Items" />
        public ImmutableArray<ObjectFilterHook> Items => _items;

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

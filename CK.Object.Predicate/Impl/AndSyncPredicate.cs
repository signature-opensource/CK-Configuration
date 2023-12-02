using CK.Core;
using System;

namespace CK.Object.Predicate
{
    sealed class AndSyncPredicate : ObjectPredicateConfiguration, IGroupPredicateDescription
    {
        readonly ObjectPredicateConfiguration _left;
        readonly ObjectPredicateConfiguration _right;

        public bool All => true;

        public bool Any => false;

        public bool Single => false;

        public int AtLeast => 0;

        public int AtMost => 0;

        public int PredicateCount => 2;

        public AndSyncPredicate( string configurationPath, ObjectPredicateConfiguration left, ObjectPredicateConfiguration right  )
            : base( configurationPath )
        {
            _left = left;
            _right = right;
        }

        public override Func<object, bool>? CreatePredicate( IActivityMonitor monitor, IServiceProvider services )
        {
            var l = _left.CreatePredicate( monitor, services );
            var r = _right.CreatePredicate( monitor, services );
            if( l != null )
            {
                if( r != null )
                {
                    return o => l(o) && r(o);
                }
                return l;
            }
            return r;
        }

        public override ObjectPredicateHook? CreateHook( IActivityMonitor monitor, PredicateHookContext context, IServiceProvider services )
        {
            var l = _left.CreateHook( monitor, context, services );
            var r = _right.CreateHook( monitor, context, services );
            if( l != null )
            {
                if( r != null )
                {
                    return new Pair( context, this, l, r, 0 );
                }
                return l;
            }
            return r;
        }
    }

}

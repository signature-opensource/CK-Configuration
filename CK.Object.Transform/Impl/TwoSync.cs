using CK.Core;
using System;
using System.Collections.Generic;

namespace CK.Object.Transform
{
    sealed class TwoSync : ObjectTransformConfiguration, ISequenceTransformConfiguration
    {
        readonly ObjectTransformConfiguration[] _t;

        public TwoSync( string configurationPath, ObjectTransformConfiguration first, ObjectTransformConfiguration second )
            : base( configurationPath ) 
        {
            _t = new[] { first, second };
        }

        public IReadOnlyList<IObjectTransformConfiguration> Transforms => _t;

        public override Func<object, object>? CreateTransform( IActivityMonitor monitor, IServiceProvider services )
        {
            var f = _t[0].CreateTransform( monitor, services );
            var s = _t[1].CreateTransform( monitor, services );
            if( f != null )
            {
                if( s != null )
                {
                    return o => s( f( o ) );
                }
                return f;
            }
            return s;
        }

        public override ObjectTransformHook? CreateHook( IActivityMonitor monitor, TransformHookContext context, IServiceProvider services )
        {
            var f = _t[0].CreateHook( monitor, context, services );
            var s = _t[1].CreateHook( monitor, context, services );
            if( f != null )
            {
                if( s != null )
                {
                    return new TwoHookSync( context, this, f, s );
                }
                return f;
            }
            return s;
        }
    }

}

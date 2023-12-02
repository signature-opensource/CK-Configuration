using CK.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CK.Object.Transform
{
    sealed class TwoAsync : ObjectAsyncTransformConfiguration, ISequenceTransformConfiguration
    {
        readonly ObjectAsyncTransformConfiguration[] _t;

        public TwoAsync( string configurationPath, ObjectAsyncTransformConfiguration first, ObjectAsyncTransformConfiguration second )
            : base( configurationPath )
        {
            _t = new[] { first, second };
        }

        public IReadOnlyList<IObjectTransformConfiguration> Transforms => _t;

        public override Func<object, ValueTask<object>>? CreateAsyncTransform( IActivityMonitor monitor, IServiceProvider services )
        {
            var f = _t[0].CreateAsyncTransform( monitor, services );
            var s = _t[1].CreateAsyncTransform( monitor, services );
            if( f != null )
            {
                if( s != null )
                {
                    return async o => await s( await f( o ).ConfigureAwait( false ) ).ConfigureAwait( false );
                }
                return f;
            }
            return s;
        }

        public override IObjectTransformHook? CreateAsyncHook( IActivityMonitor monitor, TransformHookContext context, IServiceProvider services )
        {
            var f = _t[0].CreateAsyncHook( monitor, context, services );
            var s = _t[1].CreateAsyncHook( monitor, context, services );
            if( f != null )
            {
                if( s != null )
                {
                    return new TwoHookAsync( context, this, f, s );
                }
                return f;
            }
            return s;
        }
    }

}

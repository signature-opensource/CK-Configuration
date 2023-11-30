using System.Threading.Tasks;

namespace CK.Object.Transform
{
    public partial class ObjectAsyncTransformHook
    {
        sealed class Pair : ObjectAsyncTransformHook
        {
            readonly ObjectAsyncTransformHook _first;
            readonly ObjectAsyncTransformHook _second;

            public Pair( TransformHookContext context,
                         IObjectTransformConfiguration configuration,
                         ObjectAsyncTransformHook first,
                         ObjectAsyncTransformHook second )
                : base( context, configuration )
            {
                _first = first;
                _second = second;
            }

            protected override async ValueTask<object> DoTransformAsync( object o )
            {
                return await _second.TransformAsync( await _first.TransformAsync( o ).ConfigureAwait( false ) ).ConfigureAwait( false );
            }
        }

        /// <summary>
        /// Creates a hook that combines two transformations.
        /// </summary>
        /// <param name="context">The hook context.</param>
        /// <param name="configuration">The configuration that defines both left and right.</param>
        /// <param name="first">The first transformation to be applied.</param>
        /// <param name="second">The second transformation to apply.</param>
        /// <returns>The combined transformation.</returns>
        public static ObjectAsyncTransformHook CreatePair( TransformHookContext context,
                                                           IObjectTransformConfiguration configuration,
                                                           ObjectAsyncTransformHook first,
                                                           ObjectAsyncTransformHook second )
        {
            return new Pair( context, configuration, first, second );
        }
    }

}

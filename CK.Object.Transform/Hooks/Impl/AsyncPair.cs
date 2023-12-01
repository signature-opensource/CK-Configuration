using System.Threading.Tasks;

namespace CK.Object.Transform
{
    sealed class AsyncPair : ObjectAsyncTransformHook
    {
        readonly IObjectTransformHook _first;
        readonly IObjectTransformHook _second;

        public AsyncPair( TransformHookContext context,
                          IObjectTransformConfiguration configuration,
                          IObjectTransformHook first,
                          IObjectTransformHook second )
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

}

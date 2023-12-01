namespace CK.Object.Transform
{
    sealed class Pair : ObjectTransformHook
    {
        readonly ObjectTransformHook _first;
        readonly ObjectTransformHook _second;

        public Pair( TransformHookContext context,
                     IObjectTransformConfiguration configuration,
                     ObjectTransformHook first,
                     ObjectTransformHook second )
            : base( context, configuration )
        {
            _first = first;
            _second = second;
        }

        protected override object DoTransform( object o )
        {
            return _second.Transform( _first.Transform( o ) );
        }
    }

}

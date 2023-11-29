namespace CK.Object.Transform
{
    public partial class ObjectTransformHook
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

        /// <summary>
        /// Creates a hook that combines two transformations.
        /// </summary>
        /// <param name="context">The hook context.</param>
        /// <param name="configuration">The configuration that defines both left and right.</param>
        /// <param name="first">The first transformation to be applied.</param>
        /// <param name="second">The second transformation to apply.</param>
        /// <returns>The combined transformation.</returns>
        public static ObjectTransformHook CreatePair( TransformHookContext context,
                                                      IObjectTransformConfiguration configuration,
                                                      ObjectTransformHook first,
                                                      ObjectTransformHook second )
        {
            return new Pair( context, configuration, first, second );
        }
    }

}

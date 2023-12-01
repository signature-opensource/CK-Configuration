namespace CK.Object.Transform
{
    public partial class ObjectAsyncTransformHook
    {
        /// <summary>
        /// Creates a hook that combines two transformations.
        /// </summary>
        /// <param name="context">The hook context.</param>
        /// <param name="configuration">The configuration that defines both left and right.</param>
        /// <param name="first">The first transformation to be applied.</param>
        /// <param name="second">The second transformation to apply.</param>
        /// <returns>The combined transformation.</returns>
        public static IObjectTransformHook CreatePair( TransformHookContext context,
                                                       IObjectTransformConfiguration configuration,
                                                       IObjectTransformHook first,
                                                       IObjectTransformHook second )
        {
            if( first is ObjectTransformHook sFirst && second is ObjectTransformHook sSecond )
            {
                return new Pair( context, configuration, sFirst, sSecond );
            }
            return new AsyncPair( context, configuration, first, second );
        }
    }

}

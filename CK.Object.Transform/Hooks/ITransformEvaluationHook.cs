using System;

namespace CK.Object.Transform
{
    /// <summary>
    /// Hook that can track each evaluation accross a <see cref="ObjectTransformHook"/> or <see cref="ObjectAsyncTransformHook"/>.
    /// </summary>
    public interface ITransformEvaluationHook
    {
        /// <summary>
        /// Called before evaluating each <see cref="ObjectTransformHook"/> or <see cref="ObjectAsyncTransformHook"/>.
        /// <para>
        /// When this method returns a non null object, the transformation is skipped and the result is the returned object.
        /// Implementation should almost always return null except when <paramref name="o"/> is an exception: in this case,
        /// the exception should be returned. This gently propagates any error received by <see cref="OnTransformError(IObjectTransformHook, object, Exception)"/>
        /// up the hooks.
        /// </para>
        /// </summary>
        /// <param name="source">The source transform hook.</param>
        /// <param name="o">The object to transform.</param>
        /// <returns>Null to continue the evaluation, a non null object to skip the transformation and substitute the result.</returns>
        object? OnBeforeTransform( IObjectTransformHook source, object o );

        /// <summary>
        /// Called if the evaluation raised an error.
        /// <para>
        /// When null is returned, the exception is rethrown.
        /// When a non null object is returned, it becomes the result of the transformation. 
        /// Implementations should return the exception: whith the help of <see cref="OnBeforeTransform(IObjectTransformHook, object)"/>
        /// the exception will be propagated up to the root hook.
        /// </para>
        /// </summary>
        /// <param name="source">The source transform hook.</param>
        /// <param name="o">The object that causes the error.</param>
        /// <param name="ex">The exception raised by the transformation.</param>
        /// <returns>Null to rethrow the exception, a non null object to swallow the result and substitute the result.</returns>
        object? OnTransformError( IObjectTransformHook source, object o, Exception ex );

        /// <summary>
        /// Called after transformation unless <see cref="OnTransformError(IObjectTransformHook, object, Exception)"/> has been called.
        /// This default implementation returns the <paramref name="result"/> but when overridden this may be changed (but this is unexpected).
        /// <para>
        /// Note that to avoid an illegal null object to be propagated, if this method returns null (that should not happen)
        /// the non null <paramref name="result"/> is returned instead.
        /// </para>
        /// </summary>
        /// <param name="source">The source transform hook.</param>
        /// <param name="o">The initial object.</param>
        /// <param name="result">The transformed object.</param>
        /// <returns>The <paramref name="result"/>.</returns>
        object OnAfterTransform( IObjectTransformHook source, object o, object result );
    }
}

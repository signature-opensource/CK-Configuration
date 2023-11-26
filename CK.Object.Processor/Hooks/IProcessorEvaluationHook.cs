using CK.Object.Predicate;
using CK.Object.Transform;
using System;

namespace CK.Object.Processor
{
    /// <summary>
    /// Hook that can track each object process accross a <see cref="ObjectProcessorHook"/> or <see cref="ObjectAsyncProcessorHook"/>.
    /// </summary>
    public interface IProcessorEvaluationHook
    {
        /// <summary>
        /// Gets the evaluation hook that will be used when evaluating <see cref="IObjectProcessorConfiguration.Condition"/>.
        /// </summary>
        IPredicateEvaluationHook ConditionEvaluationHook { get; }

        /// <summary>
        /// Gets the evaluation hook that will be used when evaluating <see cref="IObjectProcessorConfiguration.Action"/>.
        /// </summary>
        ITransformEvaluationHook ActionEvaluationHook { get; }

        /// <summary>
        /// Called before processing each <see cref="ObjectProcessorHook"/> or <see cref="ObjectAsyncProcessorHook"/>.
        /// <para>
        /// When this method returns a non null object, the transformation is skipped and the result is the returned object.
        /// Implementation should almost always return null except when <paramref name="o"/> is an exception: in this case,
        /// the exception should be returned. This gently propagates any error received by <see cref="OnProcessorError(IObjectProcessorHook, object, Exception)"/>
        /// up the hooks.
        /// </para>
        /// </summary>
        /// <param name="source">The source processor hook.</param>
        /// <param name="o">The object to transform.</param>
        /// <returns>Null to continue the process, a non null object to skip the processor and substitute the result.</returns>
        object? OnBeforeProcessor( IObjectProcessorHook source, object o );

        /// <summary>
        /// Called if the processing raised an error.
        /// <para>
        /// When null is returned, the exception is rethrown.
        /// When a non null object is returned, it becomes the result of the processing. 
        /// Implementations should return the exception: whith the help of <see cref="OnBeforeProcessor(IObjectProcessorHook, object)"/>
        /// the exception will be propagated up to the root hook.
        /// </para>
        /// </summary>
        /// <param name="source">The source processor hook.</param>
        /// <param name="o">The object that causes the error.</param>
        /// <param name="ex">The exception raised by the process.</param>
        /// <returns>Null to rethrow the exception, a non null object to swallow the result and substitute the result.</returns>
        object? OnProcessorError( IObjectProcessorHook source, object o, Exception ex );

        /// <summary>
        /// Called after processing unless <see cref="OnProcessorError(IObjectProcessorHook, object, Exception)"/> has been called.
        /// This default implementation returns the <paramref name="result"/> but when overridden this may be changed (but this is unexpected).
        /// </summary>
        /// <param name="source">The source processor hook.</param>
        /// <param name="o">The initial object.</param>
        /// <param name="result">The process result.</param>
        /// <returns>The <paramref name="result"/>.</returns>
        object? OnAfterProcessor( IObjectProcessorHook source, object o, object? result );
    }
}

using System;

namespace CK.Object.Filter
{
    /// <summary>
    /// Hook that can track each evaluation accross a <see cref="ObjectFilterHook"/> or <see cref="ObjectAsyncFilterHook"/>.
    /// </summary>
    public class EvaluationHook
    {
        /// <summary>
        /// Called before evaluating each filter.
        /// <para>
        /// When this method returns false, the evaluation is skipped with a false result.
        /// </para>
        /// </summary>
        /// <param name="source">The source filter.</param>
        /// <param name="o">The object to evaluate.</param>
        /// <returns>Always true at this level. When false is returned, the evaluation stops.</returns>
        internal protected virtual bool OnBeforeEvaluate( IObjectFilterHook source, object o )
        {
            return true;
        }

        /// <summary>
        /// Called if the evaluation raised an error.
        /// By default at this level this returns true: the exception is rethrown.
        /// </summary>
        /// <param name="source">The source filter.</param>
        /// <param name="o">The object.</param>
        /// <param name="ex">The exception raised by the evaluation.</param>
        /// <returns>True to rethrow the exception, false to swallow it.</returns>
        internal protected virtual bool OnEvaluationError( IObjectFilterHook source, object o, Exception ex )
        {
            return true;
        }

        /// <summary>
        /// Called after predicate evaluation unless <see cref="OnEvaluationError(IObjectFilterHook, object, Exception)"/> has been called.
        /// This can be overridden: evaluation result may be changed by this method
        /// (but this is unexpected).
        /// </summary>
        /// <param name="source">The source filter.</param>
        /// <param name="o">The object.</param>
        /// <param name="result">The evaluated result.</param>
        /// <returns>The <paramref name="result"/>.</returns>
        internal protected virtual bool OnAfterEvaluate( IObjectFilterHook source, object o, bool result )
        {
            return result;
        }

    }
}

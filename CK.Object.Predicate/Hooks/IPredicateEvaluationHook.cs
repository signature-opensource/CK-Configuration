using System;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Hook that can track each evaluation accross a <see cref="ObjectPredicateHook"/> or <see cref="ObjectAsyncPredicateHook"/>.
    /// </summary>
    public interface IPredicateEvaluationHook
    {
        /// <summary>
        /// Called before evaluating each <see cref="ObjectPredicateHook"/> or <see cref="ObjectAsyncPredicateHook"/>.
        /// <para>
        /// When this method returns false, the evaluation is skipped with a false result.
        /// Implementations should generally return true.
        /// </para>
        /// </summary>
        /// <param name="source">The source predicate.</param>
        /// <param name="o">The object to evaluate.</param>
        /// <returns>True to continue the evaluation, false to skip it and return a false result.</returns>
        bool OnBeforePredicate( IObjectPredicateHook source, object o );

        /// <summary>
        /// Called if the evaluation raised an error.
        /// When returning true the exception is rethrown.
        /// When returning false the exception is swallowed and the evaluation result is false.
        /// </summary>
        /// <param name="source">The source predicate.</param>
        /// <param name="o">The object.</param>
        /// <param name="ex">The exception raised by the evaluation.</param>
        /// <returns>True to rethrow the exception, false to swallow it and return a false result.</returns>
        bool OnPredicateError( IObjectPredicateHook source, object o, Exception ex );

        /// <summary>
        /// Called after predicate evaluation unless <see cref="OnPredicateError(IObjectPredicateHook, object, Exception)"/> has been called.
        /// Implementations should always return the <paramref name="result"/> but when overridden this may be changed (but this is unexpected).
        /// </summary>
        /// <param name="source">The source predicate.</param>
        /// <param name="o">The object.</param>
        /// <param name="result">The evaluated result.</param>
        /// <returns>The <paramref name="result"/>.</returns>
        bool OnAfterPredicate( IObjectPredicateHook source, object o, bool result );
    }
}

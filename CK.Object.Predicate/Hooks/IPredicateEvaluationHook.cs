using System;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Hook that can track each evaluation accross a <see cref="ObjectPredicateHook"/> or <see cref="ObjectAsyncPredicateHook"/>.
    /// <para>
    /// This is an interface with default implementation methods to ease composition (when possible).
    /// </para>
    /// </summary>
    public interface IPredicateEvaluationHook
    {
        /// <summary>
        /// Called before evaluating each <see cref="ObjectPredicateHook"/> or <see cref="ObjectAsyncPredicateHook"/>.
        /// <para>
        /// When this method returns false, the evaluation is skipped with a false result.
        /// Default implementation returns true.
        /// </para>
        /// </summary>
        /// <param name="source">The source predicate.</param>
        /// <param name="o">The object to evaluate.</param>
        /// <returns>True to continue the evaluation, false to skip it and return a false result.</returns>
        bool OnBeforePredicate( IObjectPredicateHook source, object o ) => true;

        /// <summary>
        /// Called if the evaluation raised an error.
        /// Default implementation returns true: the exception is rethrown.
        /// </summary>
        /// <param name="source">The source predicate.</param>
        /// <param name="o">The object.</param>
        /// <param name="ex">The exception raised by the evaluation.</param>
        /// <returns>True to rethrow the exception, false to swallow it.</returns>
        bool OnPredicateError( IObjectPredicateHook source, object o, Exception ex ) => true;

        /// <summary>
        /// Called after predicate evaluation unless <see cref="OnPredicateError(IObjectPredicateHook, object, Exception)"/> has been called.
        /// This default implementation returns the <paramref name="result"/> but when overridden this may be changed (but this is unexpected).
        /// </summary>
        /// <param name="source">The source predicate.</param>
        /// <param name="o">The object.</param>
        /// <param name="result">The evaluated result.</param>
        /// <returns>The <paramref name="result"/>.</returns>
        bool OnAfterPredicate( IObjectPredicateHook source, object o, bool result ) => result;
    }
}

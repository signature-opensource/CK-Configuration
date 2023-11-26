using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.ExceptionServices;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Hook that logs the evaluation details and capture errors.
    /// </summary>
    public class MonitoredPredicateEvaluationHook : IPredicateEvaluationHook
    {
        readonly IActivityMonitor _monitor;
        readonly CKTrait? _tags;
        readonly LogLevel _level;
        List<ExceptionDispatchInfo>? _errors;

        /// <summary>
        /// Initializes a new hook.
        /// </summary>
        /// <param name="monitor">The monitor that will receive evaluation details.</param>
        /// <param name="tags">Optional tags for log entries.</param>
        /// <param name="level">Default group level.</param>
        public MonitoredPredicateEvaluationHook( IActivityMonitor monitor,
                                                 CKTrait? tags = null,
                                                 LogLevel level = LogLevel.Trace )
        {
            Throw.CheckNotNullArgument( monitor );
            _monitor = monitor;
            _tags = tags;
            _level = level;
        }

        /// <summary>
        /// Gets the evaluation errors that occurred.
        /// </summary>
        public IReadOnlyList<ExceptionDispatchInfo> Errors => (IReadOnlyList<ExceptionDispatchInfo>?)_errors ?? ImmutableArray<ExceptionDispatchInfo>.Empty;

        /// <summary>
        /// Clears any <see cref="Errors"/>.
        /// </summary>
        public void ClearErrors() => _errors?.Clear();

        /// <summary>
        /// Opens a group.
        /// The object to evaluate is not logged.
        /// </summary>
        /// <param name="source">The source predicate.</param>
        /// <param name="o">The object to evaluate.</param>
        /// <returns>Always true to continue the evaluation.</returns>
        public virtual bool OnBeforePredicate( IObjectPredicateHook source, object o )
        {
            _monitor.OpenGroup( _level, _tags, $"Evaluating '{source.Configuration.Configuration.Path}'." );
            return true;
        }

        /// <summary>
        /// Emits an <see cref="LogLevel.Error"/> with the exception and the <paramref name="o"/> (its <see cref="object.ToString()"/>),
        /// captures the exception into <see cref="Errors"/>, close the currently opened group and returns false to prevent
        /// the exception to be rethrown.
        /// </summary>
        /// <param name="source">The source predicate.</param>
        /// <param name="o">The object.</param>
        /// <param name="ex">The exception raised by the evaluation.</param>
        /// <returns>Always false to prevent the exception to be rethrown.</returns>
        public virtual bool OnPredicateError( IObjectPredicateHook source, object o, Exception ex )
        {
            using( _monitor.OpenError( _tags, $"Predicate '{source.Configuration.Configuration.Path}' error while processing:", ex ) )
            {
                _monitor.Trace( _tags, o?.ToString() ?? "<null>" );
            }
            _errors ??= new List<ExceptionDispatchInfo>();
            _errors.Add( ExceptionDispatchInfo.Capture( ex ) );
            _monitor.CloseGroup();
            return false;
        }

        /// <summary>
        /// Closes the currently opened group with the result as a conclusion.
        /// </summary>
        /// <param name="source">The source predicate.</param>
        /// <param name="o">The object.</param>
        /// <param name="result">The evaluated result.</param>
        /// <returns>The <paramref name="result"/>.</returns>
        public virtual bool OnAfterPredicate( IObjectPredicateHook source, object o, bool result )
        {
            _monitor.CloseGroup( $"=> {result}" );
            return result;
        }
    }
}

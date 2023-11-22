using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.ExceptionServices;

namespace CK.Object.Filter
{
    /// <summary>
    /// Hook that logs the evaluation details and capture errors.
    /// </summary>
    public class MonitoredEvaluationHook : EvaluationHook
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
        public MonitoredEvaluationHook( IActivityMonitor monitor,
                                        CKTrait? tags = null,
                                        LogLevel level = LogLevel.Trace )
        {
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
        /// <param name="source">The source filter.</param>
        /// <param name="o">The object to evaluate.</param>
        /// <returns>Always true to continue the evaluation.</returns>
        protected internal override bool OnBeforeEvaluate( IObjectFilterHook source, object o )
        {
            _monitor.OpenGroup( _level, _tags, $"Evaluating '{source.Configuration.Configuration.Path}'." );
            return true;
        }

        /// <summary>
        /// Emits an <see cref="LogLevel.Error"/> with the exception and the <paramref name="o"/> (its <see cref="object.ToString()"/>),
        /// captures the exception into <see cref="Errors"/>, close the currently opened group and returns false to prevent
        /// the exception to be rethrown.
        /// </summary>
        /// <param name="source">The source filter.</param>
        /// <param name="o">The object.</param>
        /// <param name="ex">The exception raised by the evaluation.</param>
        /// <returns>Always false to prevent the exception to be rethrown.</returns>
        protected internal override bool OnEvaluationError( IObjectFilterHook source, object o, Exception ex )
        {
            using( _monitor.OpenError( _tags, $"Filter '{source.Configuration.Configuration.Path}' error while processing:", ex ) )
            {
                _monitor.Trace( _tags, o?.ToString() ?? "<null>" );
            }
            _errors ??= new List<ExceptionDispatchInfo>();
            _errors.Add( ExceptionDispatchInfo.Capture( ex ) );
            _monitor.CloseGroup();
            return false;
        }

        /// <summary>
        /// Closes the currently opened group withe result conclusion.
        /// </summary>
        /// <param name="source">The source filter.</param>
        /// <param name="o">The object.</param>
        /// <param name="result">The evaluated result.</param>
        /// <returns>The <paramref name="result"/>.</returns>
        protected internal override bool OnAfterEvaluate( IObjectFilterHook source, object o, bool result )
        {
            _monitor.CloseGroup( $"=> {result}" );
            return result;
        }
    }
}

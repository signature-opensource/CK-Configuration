using CK.Core;
using CK.Object.Predicate;
using CK.Object.Transform;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.ExceptionServices;

namespace CK.Object.Processor
{
    /// <summary>
    /// Hook that logs the evaluation details and capture errors.
    /// </summary>
    public class MonitoredProcessorEvaluationHook : IProcessorEvaluationHook
    {
        readonly IActivityMonitor _monitor;
        readonly IPredicateEvaluationHook _conditionEvaluationHook;
        readonly ITransformEvaluationHook _actionEvaluationHook;
        readonly CKTrait? _tags;
        readonly LogLevel _level;
        List<ExceptionDispatchInfo>? _errors;

        /// <summary>
        /// Initializes a new hook.
        /// </summary>
        /// <param name="monitor">The monitor that will receive evaluation details.</param>
        /// <param name="tags">Optional tags for log entries.</param>
        /// <param name="level">Default group level.</param>
        public MonitoredProcessorEvaluationHook( IActivityMonitor monitor,
                                                 CKTrait? tags = null,
                                                 LogLevel level = LogLevel.Trace,
                                                 IPredicateEvaluationHook? predicateEvaluationHook = null,
                                                 ITransformEvaluationHook? transformEvaluationHook = null )
        {
            Throw.CheckNotNullArgument( monitor );
            _monitor = monitor;
            _tags = tags;
            _level = level;
            _conditionEvaluationHook = predicateEvaluationHook ?? new MonitoredPredicateEvaluationHook( monitor, tags, level );
            _actionEvaluationHook = transformEvaluationHook ?? new MonitoredTransformEvaluationHook( monitor, tags, level );
        }

        /// <summary>
        /// Gets the process errors that occurred.
        /// </summary>
        public IReadOnlyList<ExceptionDispatchInfo> Errors => (IReadOnlyList<ExceptionDispatchInfo>?)_errors ?? ImmutableArray<ExceptionDispatchInfo>.Empty;

        public IPredicateEvaluationHook ConditionEvaluationHook => _conditionEvaluationHook;

        public ITransformEvaluationHook ActionEvaluationHook => _actionEvaluationHook;

        /// <summary>
        /// Clears any <see cref="Errors"/>.
        /// </summary>
        public void ClearErrors() => _errors?.Clear();

        /// <summary>
        /// Opens a group. The object to transform is not logged.
        /// <para>
        /// If <paramref name="o"/> is an exception, 
        /// </para>
        /// </summary>
        /// <param name="source">The source processor hook.</param>
        /// <param name="o">The object to transform.</param>
        /// <returns>
        /// Always null (to continue the transformation) except if the object to evaluate is an exception: it becomes the eventual
        /// transformation result.
        /// </returns>
        public virtual object? OnBeforeProcessor( IObjectProcessorHook source, object o )
        {
            if( o is Exception ) return o;
            _monitor.OpenGroup( _level, _tags, $"Processing '{source.Configuration.Configuration.Path}'." );
            return null;
        }

        /// <summary>
        /// Emits an <see cref="LogLevel.Error"/> with the exception and the <paramref name="o"/> (its <see cref="object.ToString()"/>),
        /// captures the exception into <see cref="Errors"/>, close the currently opened group and returns the exception as the
        /// process result.
        /// </summary>
        /// <param name="source">The source processor hook.</param>
        /// <param name="o">The object that causes the error.</param>
        /// <param name="ex">The exception raised by the transformation.</param>
        /// <returns>The exception.</returns>
        public object? OnProcessorError( IObjectProcessorHook source, object o, Exception ex )
        {
            using( _monitor.OpenError( _tags, $"Processor '{source.Configuration.Configuration.Path}' error while processing:", ex ) )
            {
                _monitor.Trace( _tags, o?.ToString() ?? "<null>" );
            }
            _errors ??= new List<ExceptionDispatchInfo>();
            _errors.Add( ExceptionDispatchInfo.Capture( ex ) );
            _monitor.CloseGroup();
            return ex;
        }

        /// <summary>
        /// Closes the currently opened group with "Skipped." (when result is null), "Error." (when result is an exception) or "Processed.".
        /// </summary>
        /// <param name="source">The source processor hook.</param>
        /// <param name="o">The initial object.</param>
        /// <param name="result">The process result.</param>
        /// <returns>The <paramref name="result"/>.</returns>
        public object? OnAfterProcessor( IObjectProcessorHook source, object o, object? result )
        {
            _monitor.CloseGroup( result switch { null => "Skipped.", Exception => "Error.", _ => "Processed." } );
            return result;
        }
    }
}

using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.ExceptionServices;

namespace CK.Object.Transform
{
    /// <summary>
    /// Hook that logs the evaluation details and capture errors.
    /// </summary>
    public class MonitoredTransformEvaluationHook : ITransformEvaluationHook
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
        public MonitoredTransformEvaluationHook( IActivityMonitor monitor,
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
        /// Opens a group. The object to transform is not logged.
        /// <para>
        /// If <paramref name="o"/> is an exception, it is returned as the transformation result.
        /// </para>
        /// </summary>
        /// <param name="source">The source transform hook.</param>
        /// <param name="o">The object to transform.</param>
        /// <returns>
        /// Always null (to continue the transformation) except if the object to evaluate is an exception: it becomes the eventual
        /// transformation result.
        /// </returns>
        public virtual object? OnBeforeTransform( IObjectTransformHook source, object o )
        {
            if( o is Exception ) return o;
            _monitor.OpenGroup( _level, _tags, $"Evaluating '{source.Configuration.Configuration.Path}'." );
            return null;
        }

        /// <summary>
        /// Emits an <see cref="LogLevel.Error"/> with the exception and the <paramref name="o"/> (its <see cref="object.ToString()"/>),
        /// captures the exception into <see cref="Errors"/>, close the currently opened group and returns the exception as the
        /// transformation result.
        /// </summary>
        /// <param name="source">The source transform hook.</param>
        /// <param name="o">The object that causes the error.</param>
        /// <param name="ex">The exception raised by the transformation.</param>
        /// <returns>The exception.</returns>
        public object? OnTransformError( IObjectTransformHook source, object o, Exception ex )
        {
            using( _monitor.OpenError( _tags, $"Transform '{source.Configuration.Configuration.Path}' error while processing:", ex ) )
            {
                _monitor.Trace( _tags, o?.ToString() ?? "<null>" );
            }
            _errors ??= new List<ExceptionDispatchInfo>();
            _errors.Add( ExceptionDispatchInfo.Capture( ex ) );
            _monitor.CloseGroup( "Error." );
            return ex;
        }

        /// <summary>
        /// Closes the currently opened group.
        /// </summary>
        /// <param name="source">The source transform hook.</param>
        /// <param name="o">The initial object.</param>
        /// <param name="result">The transformed object.</param>
        /// <returns>The <paramref name="result"/>.</returns>
        public object OnAfterTransform( IObjectTransformHook source, object o, object result )
        {
            _monitor.CloseGroup( result is Exception ? "Error." : null );
            return result;
        }
    }
}

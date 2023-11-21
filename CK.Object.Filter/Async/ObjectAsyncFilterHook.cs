using CK.Core;
using System;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;

namespace CK.Object.Filter
{
    /// <summary>
    /// Hook implementation for synchronous predicates.
    /// </summary>
    public class ObjectAsyncFilterHook : IObjectFilterHook
    {
        readonly IObjectFilterConfiguration _configuration;
        readonly Func<object, ValueTask<bool>> _predicate;

        /// <summary>
        /// Initializes a new wrapper without specific behavior.
        /// </summary>
        /// <param name="configuration">The filter configuration.</param>
        /// <param name="predicate">The predicate.</param>
        public ObjectAsyncFilterHook( IObjectFilterConfiguration configuration, Func<object, ValueTask<bool>> predicate )
        {
            Throw.CheckNotNullArgument( configuration );
            Throw.CheckNotNullArgument( predicate );
            _configuration = configuration;
            _predicate = predicate;
        }

        // Constructor for GroupAsyncFilter.
        internal ObjectAsyncFilterHook( IObjectFilterConfiguration configuration )
        {
            Throw.CheckNotNullArgument( configuration );
            _configuration = configuration;
            _predicate = null!;
        }

        /// <inheritdoc />
        public IObjectFilterConfiguration Configuration => _configuration;

        /// <inheritdoc />
        public event Action<IObjectFilterHook, object>? Before;

        /// <inheritdoc />
        public event Action<IObjectFilterHook, object, bool>? After;

        /// <summary>
        /// Evaluates the predicate.
        /// </summary>
        /// <param name="o">The object to evaluate.</param>
        /// <returns>The predicate result.</returns>
        public virtual async ValueTask<bool> EvaluateAsync( object o )
        {
            RaiseBefore( o );
            var r = await _predicate( o ).ConfigureAwait( false );
            RaiseAfter( o, r );
            return r;
        }

        /// <summary>
        /// Raise the <see cref="Before"/> event.
        /// Must be called before the evaluation.
        /// </summary>
        /// <param name="o">The object.</param>
        protected void RaiseBefore( object o )
        {
            Before?.Invoke( this, o );
        }

        /// <summary>
        /// Raise the <see cref="After"/> event.
        /// Must be called after the evaluation.
        /// </summary>
        /// <param name="o">The object.</param>
        protected void RaiseAfter( object o, bool r )
        {
            After?.Invoke( this, o, r );
        }

    }

}

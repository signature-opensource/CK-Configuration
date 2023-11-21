using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Object.Filter
{
    /// <summary>
    /// Hook implementation for synchronous predicate.
    /// </summary>
    public class ObjectFilterHook : IObjectFilterHook
    {
        readonly IObjectFilterConfiguration _configuration;
        readonly Func<object, bool> _predicate;

        /// <summary>
        /// Initializes a new wrapper without specific behavior.
        /// </summary>
        /// <param name="configuration">The filter configuration.</param>
        /// <param name="predicate">The predicate.</param>
        public ObjectFilterHook( IObjectFilterConfiguration configuration, Func<object, bool> predicate )
        {
            Throw.CheckNotNullArgument( configuration );
            Throw.CheckNotNullArgument( predicate );
            _configuration = configuration;
            _predicate = predicate;
        }

        // Constructor for GroupFilter.
        internal ObjectFilterHook( IObjectFilterConfiguration configuration )
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
        /// Calls <see cref="RaiseBefore(object)"/>, evaluates the predicate, and calls <see cref="RaiseAfter(object, bool)"/>
        /// with the result. 
        /// </summary>
        /// <param name="o">The object to evaluate.</param>
        /// <returns>The predicate result.</returns>
        public virtual bool Evaluate( object o )
        {
            RaiseBefore( o );
            var r = _predicate( o );
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

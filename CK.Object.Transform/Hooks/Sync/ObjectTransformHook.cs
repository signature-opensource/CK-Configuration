using CK.Core;
using System;

namespace CK.Object.Transform
{
    /// <summary>
    /// Hook implementation for synchronous transform functions.
    /// </summary>
    public class ObjectTransformHook : IObjectTransformHook
    {
        readonly ITransformEvaluationHook _hook;
        readonly IObjectTransformConfiguration _configuration;
        readonly Func<object, object> _transform;

        /// <summary>
        /// Initializes a new hook.
        /// </summary>
        /// <param name="hook">The evaluation hook.</param>
        /// <param name="configuration">The transform configuration.</param>
        /// <param name="transform">The transform function.</param>
        public ObjectTransformHook( ITransformEvaluationHook hook, IObjectTransformConfiguration configuration, Func<object, object> transform )
        {
            Throw.CheckNotNullArgument( hook );
            Throw.CheckNotNullArgument( configuration );
            Throw.CheckNotNullArgument( transform );
            _hook = hook;
            _configuration = configuration;
            _transform = transform;
        }

        /// <summary>
        /// Constructor used by <see cref="SequenceTransformHook"/>. Must be used by specialized hook when the transform
        /// configuration contains other <see cref="ObjectTransformConfiguration"/> to expose the internal function structure.
        /// </summary>
        /// <param name="hook">The evaluation hook.</param>
        /// <param name="configuration">This configuration.</param>
        internal ObjectTransformHook( ITransformEvaluationHook hook, IObjectTransformConfiguration configuration )
        {
            Throw.CheckNotNullArgument( hook );
            Throw.CheckNotNullArgument( configuration );
            _hook = hook;
            _configuration = configuration;
            _transform = null!;
        }

        /// <inheritdoc />
        public IObjectTransformConfiguration Configuration => _configuration;

        /// <summary>
        /// Applies the transformation.
        /// </summary>
        /// <param name="o">The object to transform.</param>
        /// <returns>The transformed object.</returns>
        public object Transform( object o )
        {
            object? r = _hook.OnBeforeTransform( this, o );
            if( r != null ) return r;
            try
            {
                r = DoTransform( o );
                if( r == null )
                {
                    Throw.InvalidOperationException( $"Transform '{_configuration.Configuration.Path}' returned a null reference." );
                }
                return _hook.OnAfterTransform( this, o, r ) ?? r;
            }
            catch( Exception ex )
            {
                r = _hook.OnTransformError( this, o, ex );
                if( r == null )
                {
                    throw;
                }
                return r;
            }
        }

        /// <summary>
        /// Actual application of the transform function.
        /// </summary>
        /// <param name="o">The object to transform.</param>
        /// <returns>The transformation result.</returns>
        protected virtual object DoTransform( object o ) => _transform( o );
    }

}

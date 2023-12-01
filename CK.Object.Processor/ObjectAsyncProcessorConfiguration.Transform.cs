using CK.Core;
using CK.Object.Predicate;
using CK.Object.Transform;
using System;
using System.Threading.Tasks;

namespace CK.Object.Processor
{
    public partial class ObjectAsyncProcessorConfiguration : IObjectTransformConfiguration
    {
        /// <inheritdoc />
        public ObjectAsyncTransformConfiguration? Transform => _transform;

        /// <summary>
        /// Creates the transformation that applies first the <see cref="CreateIntrinsicAsyncTransform(IActivityMonitor, IServiceProvider)"/>
        /// and then the configured <see cref="Transform"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">Services that may be required for some (complex) transform functions.</param>
        /// <returns>The transform function or null for the identity function.</returns>
        protected virtual Func<object, ValueTask<object>>? CreateAsyncTransform( IActivityMonitor monitor, IServiceProvider services )
        {
            var intrinsic = CreateIntrinsicAsyncTransform( monitor, services );
            var configured = _transform?.CreateAsyncTransform( monitor, services );
            if( intrinsic != null )
            {
                if( configured != null )
                {
                    return async o => await configured( await intrinsic( o ).ConfigureAwait( false ) ).ConfigureAwait( false );
                }
                return intrinsic;
            }
            return configured;
        }

        /// <summary>
        /// Creates "this" transform (null by default) that is applied before the configured <see cref="Transform"/>
        /// by <see cref="CreateAsyncTransform(IActivityMonitor, IServiceProvider)"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">Services that may be required for some (complex) predicates.</param>
        /// <returns>A configured transform function or null for an identity function.</returns>
        protected virtual Func<object, ValueTask<object>>? CreateIntrinsicAsyncTransform( IActivityMonitor monitor, IServiceProvider services )
        {
            return null;
        }

        /// <summary>
        /// Creates the transform hook object. 
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="context">The hook context.</param>
        /// <param name="services">Services that may be required for some (complex) transform functions.</param>
        /// <returns>The transform hook or null for the identity function.</returns>
        protected virtual IObjectTransformHook? CreateAsyncTransformHook( IActivityMonitor monitor,
                                                                          TransformHookContext context,
                                                                          IServiceProvider services )
        {
            var intrinsic = CreateIntrisincAsyncTransformHook( monitor, context, services );
            var configured = _transform?.CreateAsyncHook( monitor, context, services );
            if( intrinsic != null )
            {
                if( configured != null )
                {
                    return ObjectAsyncTransformHook.CreatePair( context, this, intrinsic, configured );
                }
                return intrinsic;
            }
            return configured;
        }

        /// <summary>
        /// Creates a transform hook based on the transform function created by <see cref="CreateIntrinsicAsyncTransform(IActivityMonitor, IServiceProvider)"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="context">The hook context.</param>
        /// <param name="services">Services that may be required for some (complex) transform functions.</param>
        /// <returns>The hook predicate or null for the empty predicate.</returns>
        protected virtual IObjectTransformHook? CreateIntrisincAsyncTransformHook( IActivityMonitor monitor,
                                                                                   TransformHookContext context,
                                                                                   IServiceProvider services )
        {
            var t = CreateIntrinsicAsyncTransform( monitor, services );
            return t != null ? new ObjectAsyncTransformHook( context, this, t ) : null;
        }

    }
}

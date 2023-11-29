using CK.Core;
using CK.Object.Predicate;
using CK.Object.Transform;
using System;

namespace CK.Object.Processor
{
    public partial class ObjectProcessorConfiguration
    {
        /// <inheritdoc />
        public ObjectTransformConfiguration? Transform => _transform;

        /// <summary>
        /// Creates the transformation that applies first the <see cref="CreateIntrinsicTransform(IActivityMonitor, IServiceProvider)"/>
        /// and then the configured <see cref="Transform"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">Services that may be required for some (complex) transform functions.</param>
        /// <returns>The transform function or null for the identity function.</returns>
        protected virtual Func<object, object>? CreateTransform( IActivityMonitor monitor, IServiceProvider services )
        {
            var intrinsic = CreateIntrinsicTransform( monitor, services );
            var configured = _transform?.CreateTransform( monitor, services );
            if( intrinsic != null )
            {
                if( configured != null )
                {
                    return o => configured( intrinsic( o ) );
                }
                return intrinsic;
            }
            return configured;
        }

        /// <summary>
        /// Creates "this" transform (null by default) that is applied before the configured <see cref="Transform"/>
        /// by <see cref="CreateTransform(IActivityMonitor, IServiceProvider)"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">Services that may be required for some (complex) predicates.</param>
        /// <returns>A configured transform function or null for an identity function.</returns>
        protected virtual Func<object, object>? CreateIntrinsicTransform( IActivityMonitor monitor, IServiceProvider services )
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
        protected virtual ObjectTransformHook? CreateTransformHook( IActivityMonitor monitor,
                                                                    TransformHookContext context,
                                                                    IServiceProvider services )
        {
            var intrinsic = CreateIntrisincTransformHook( monitor, context, services );
            var configured = _transform?.CreateHook( monitor, context, services );
            if( intrinsic != null )
            {
                if( configured != null )
                {
                    return ObjectTransformHook.CreatePair( context, this, intrinsic, configured );
                }
                return intrinsic;
            }
            return configured;
        }

        /// <summary>
        /// Creates a transform hook based on the transform function created by <see cref="CreateIntrinsicTransform(IActivityMonitor, IServiceProvider)"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="context">The hook context.</param>
        /// <param name="services">Services that may be required for some (complex) transform functions.</param>
        /// <returns>The hook predicate or null for the empty predicate.</returns>
        protected virtual ObjectTransformHook? CreateIntrisincTransformHook( IActivityMonitor monitor,
                                                                             TransformHookContext context,
                                                                             IServiceProvider services )
        {
            var t = CreateIntrinsicTransform( monitor, services );
            return t != null ? new ObjectTransformHook( context, this, t ) : null;
        }

    }
}

using CK.Core;
using CK.Object.Predicate;
using CK.Object.Transform;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CK.Object.Processor
{
    public partial class ObjectProcessorConfiguration
    {
        /// <summary>
        /// Gets the (necessarily) synchronous configured transform.
        /// </summary>
        public new ObjectTransformConfiguration? Transform => Unsafe.As<ObjectTransformConfiguration>( base.Transform );

        /// <summary>
        /// Adapts this synchronous transform.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">Services that may be required for some (complex) transform functions.</param>
        /// <returns>A configured transform function or null for an identity function.</returns>
        protected sealed override Func<object, ValueTask<object>>? CreateAsyncTransform( IActivityMonitor monitor, IServiceProvider services )
        {
            var p = CreateTransform( monitor, services );
            return p != null ? o => ValueTask.FromResult( p( o ) ) : null;
        }

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
            var configured = Transform?.CreateTransform( monitor, services );
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
        /// Definite relay to <see cref="CreateTransformHook(IActivityMonitor, TransformHookContext, IServiceProvider)"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="context">The hook context.</param>
        /// <param name="services">Services that may be required for some (complex) transform functions.</param>
        /// <returns>A wrapper bound to the hook context or null for the identity transform.</returns>
        protected sealed override IObjectTransformHook? CreateAsyncTransformHook( IActivityMonitor monitor, TransformHookContext context, IServiceProvider services )
        {
            return CreateTransformHook( monitor, context, services );
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
            var configured = Transform?.CreateHook( monitor, context, services );
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

using CK.Core;
using CK.Object.Predicate;
using CK.Object.Transform;
using System;
using System.Threading.Tasks;

namespace CK.Object.Processor
{
    public partial class ObjectAsyncProcessorConfiguration : IObjectPredicateConfiguration
    {
        /// <inheritdoc />
        public ObjectAsyncPredicateConfiguration? Condition => _condition;

        /// <summary>
        /// Creates the condition that combines the intrinsic and configured condition.
        /// By default, the intrinsic condition AND configured conditions (in this order) are challenged.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">Services that may be required for some (complex) predicates.</param>
        /// <returns>The predicate or null for the empty predicate.</returns>
        protected virtual Func<object, ValueTask<bool>>? CreateAsyncCondition( IActivityMonitor monitor, IServiceProvider services )
        {
            var intrinsic = CreateIntrinsicAsyncCondition( monitor, services );
            var configured = _condition?.CreateAsyncPredicate( monitor, services );
            if( intrinsic != null )
            {
                if( configured != null )
                {
                    return async o => await intrinsic( o ) && await configured( o );
                }
                return intrinsic;
            }
            return configured;
        }

        /// <summary>
        /// Creates "this" condition (null by default) that is combined with the configured <see cref="Condition"/>
        /// by <see cref="CreateAsyncCondition(IActivityMonitor, IServiceProvider)"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">Services that may be required for some (complex) predicates.</param>
        /// <returns>The predicate or null for the empty predicate.</returns>
        protected virtual Func<object, ValueTask<bool>>? CreateIntrinsicAsyncCondition( IActivityMonitor monitor, IServiceProvider services )
        {
            return null;
        }

        /// <summary>
        /// Creates the condition hook object that combines the intrinsic and configured condition. 
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="context">The hook context.</param>
        /// <param name="services">Services that may be required for some (complex) predicates.</param>
        /// <returns>The hook predicate or null for the empty predicate.</returns>
        protected virtual IObjectPredicateHook? CreateAsyncConditionHook( IActivityMonitor monitor,
                                                                          PredicateHookContext context,
                                                                          IServiceProvider services )
        {
            var intrinsic = CreateIntrisincAsyncConditionHook( monitor, context, services );
            var configured = _condition?.CreateAsyncHook( monitor, context, services );
            if( intrinsic != null )
            {
                if( configured != null )
                {
                    return ObjectAsyncPredicateHook.CreateAndHook( context, this, intrinsic, configured );
                }
                return intrinsic;
            }
            return configured;
        }

        /// <summary>
        /// Creates a predicate hook based on the predicate created by <see cref="CreateIntrinsicAsyncCondition(IActivityMonitor, IServiceProvider)"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="context">The hook context.</param>
        /// <param name="services">Services that may be required for some (complex) predicates.</param>
        /// <returns>The hook predicate or null for the empty predicate.</returns>
        protected virtual IObjectPredicateHook? CreateIntrisincAsyncConditionHook( IActivityMonitor monitor,
                                                                                   PredicateHookContext context,
                                                                                   IServiceProvider services )
        {
            var p = CreateIntrinsicAsyncCondition( monitor, services );
            return p != null ? new ObjectAsyncPredicateHook( context, this, p ) : null;
        }

    }
}

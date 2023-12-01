using CK.Core;
using CK.Object.Predicate;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CK.Object.Processor
{
    public partial class ObjectProcessorConfiguration
    {
        /// <summary>
        /// Gets the (necessarily) synchronous configured condition.
        /// </summary>
        public new ObjectPredicateConfiguration? Condition => Unsafe.As<ObjectPredicateConfiguration>( base.Condition );

        /// <summary>
        /// Creates the condition that combines the intrinsic and configured condition.
        /// By default, the intrinsic condition AND configured conditions (in this order) are challenged.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">Services that may be required for some (complex) predicates.</param>
        /// <returns>The predicate or null for the empty predicate.</returns>
        protected virtual Func<object, bool>? CreateCondition( IActivityMonitor monitor, IServiceProvider services )
        {
            var intrinsic = CreateIntrinsicCondition( monitor, services );
            var configured = Condition?.CreatePredicate( monitor, services );
            if( intrinsic != null )
            {
                if( configured != null )
                {
                    return o => intrinsic( o ) && configured( o );
                }
                return intrinsic;
            }
            return configured;
        }

        /// <summary>
        /// Creates "this" condition (null by default) that is combined with the configured <see cref="Condition"/>
        /// by <see cref="CreateCondition(IActivityMonitor, IServiceProvider)"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">Services that may be required for some (complex) predicates.</param>
        /// <returns>The predicate or null for the empty predicate.</returns>
        protected virtual Func<object, bool>? CreateIntrinsicCondition( IActivityMonitor monitor, IServiceProvider services )
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
        protected ObjectPredicateHook? CreateConditionHook( IActivityMonitor monitor,
                                                            PredicateHookContext context,
                                                            IServiceProvider services )
        {
            var intrinsic = CreateIntrisincConditionHook( monitor, context, services );
            var configured = Condition?.CreateHook( monitor, context, services );
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
        /// Creates a predicate hook based on the predicate created by <see cref="CreateIntrinsicCondition(IActivityMonitor, IServiceProvider)"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="context">The hook context.</param>
        /// <param name="services">Services that may be required for some (complex) predicates.</param>
        /// <returns>The hook predicate or null for the empty predicate.</returns>
        protected virtual ObjectPredicateHook? CreateIntrisincConditionHook( IActivityMonitor monitor,
                                                                             PredicateHookContext context,
                                                                             IServiceProvider services )
        {
            var p = CreateIntrinsicCondition( monitor, services );
            return p != null ? new ObjectPredicateHook( context, this, p ) : null;
        }

    }
}

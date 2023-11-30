using CK.Core;
using System;
using System.Threading.Tasks;

namespace CK.Object.Predicate
{
    public partial class ObjectAsyncPredicateHook
    {
        sealed class Pair : ObjectAsyncPredicateHook
        {
            readonly ObjectAsyncPredicateHook _left;
            readonly ObjectAsyncPredicateHook _right;
            readonly int _op;

            public Pair( PredicateHookContext hook,
                         IObjectPredicateConfiguration configuration,
                         ObjectAsyncPredicateHook left,
                         ObjectAsyncPredicateHook right,
                         int op )
                : base( hook, configuration )
            {
                Throw.CheckNotNullArgument( left );
                Throw.CheckNotNullArgument( right );
                _left = left;
                _right = right;
                _op = op;
            }

            protected override async ValueTask<bool> DoEvaluateAsync( object o )
            {
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
                return _op switch
                {
                    0 => await _left.EvaluateAsync( o ).ConfigureAwait( false ) && await _right.EvaluateAsync( o ).ConfigureAwait( false ),
                    1 => await _left.EvaluateAsync( o ).ConfigureAwait( false ) || await _right.EvaluateAsync( o ).ConfigureAwait( false ),
                    2 => await _left.EvaluateAsync( o ).ConfigureAwait( false ) ^ await _right.EvaluateAsync( o ).ConfigureAwait( false )
                };
#pragma warning restore CS8509
            }
        }

        /// <summary>
        /// Creates a "And" hook.
        /// </summary>
        /// <param name="context">The hook context.</param>
        /// <param name="configuration">The configuration that defines both left and right.</param>
        /// <param name="left">The left hook.</param>
        /// <param name="right">The right hook.</param>
        /// <returns>Left && right hook.</returns>
        public static ObjectAsyncPredicateHook CreateAndHook( PredicateHookContext context,
                                                              IObjectPredicateConfiguration configuration,
                                                              ObjectAsyncPredicateHook left,
                                                              ObjectAsyncPredicateHook right )
        {
            return new Pair( context, configuration, left, right, 0 );
        }

        /// <summary>
        /// Creates a "Or" hook.
        /// </summary>
        /// <param name="context">The hook context.</param>
        /// <param name="configuration">The configuration that defines both left and right.</param>
        /// <param name="left">The left hook.</param>
        /// <param name="right">The right hook.</param>
        /// <returns>Left || right hook.</returns>
        public static ObjectAsyncPredicateHook CreateOrHook( PredicateHookContext context,
                                                             IObjectPredicateConfiguration configuration,
                                                             ObjectAsyncPredicateHook left,
                                                             ObjectAsyncPredicateHook right )
        {
            return new Pair( context, configuration, left, right, 1 );
        }

        /// <summary>
        /// Creates a "XOr" hook.
        /// </summary>
        /// <param name="context">The hook context.</param>
        /// <param name="configuration">The configuration that defines both left and right.</param>
        /// <param name="left">The left hook.</param>
        /// <param name="right">The right hook.</param>
        /// <returns>Left ^ right hook.</returns>
        public static ObjectAsyncPredicateHook CreateXOrHook( PredicateHookContext context,
                                                              IObjectPredicateConfiguration configuration,
                                                              ObjectAsyncPredicateHook left,
                                                              ObjectAsyncPredicateHook right )
        {
            return new Pair( context, configuration, left, right, 2 );
        }
    }
}

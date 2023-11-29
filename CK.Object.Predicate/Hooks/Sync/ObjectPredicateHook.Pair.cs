using CK.Core;
using System;

namespace CK.Object.Predicate
{
    public partial class ObjectPredicateHook
    {
        sealed class Pair : ObjectPredicateHook
        {
            readonly ObjectPredicateHook _left;
            readonly ObjectPredicateHook _right;
            readonly int _op;

            public Pair( PredicateHookContext hook,
                         IObjectPredicateConfiguration configuration,
                         ObjectPredicateHook left,
                         ObjectPredicateHook right,
                         int op )
                : base( hook, configuration )
            {
                Throw.CheckNotNullArgument( left );
                Throw.CheckNotNullArgument( right );
                _left = left;
                _right = right;
                _op = op;
            }

            protected override bool DoEvaluate( object o )
            {
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
                return _op switch
                {
                    0 => _left.Evaluate( o ) && _right.Evaluate( o ),
                    1 => _left.Evaluate( o ) || _right.Evaluate( o ),
                    2 => _left.Evaluate( o ) ^ _right.Evaluate( o )
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
        public static ObjectPredicateHook CreateAndHook( PredicateHookContext context,
                                                         IObjectPredicateConfiguration configuration,
                                                         ObjectPredicateHook left,
                                                         ObjectPredicateHook right )
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
        public static ObjectPredicateHook CreateOrHook( PredicateHookContext context,
                                                        IObjectPredicateConfiguration configuration,
                                                        ObjectPredicateHook left,
                                                        ObjectPredicateHook right )
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
        public static ObjectPredicateHook CreateXOrHook( PredicateHookContext context,
                                                         IObjectPredicateConfiguration configuration,
                                                         ObjectPredicateHook left,
                                                         ObjectPredicateHook right )
        {
            return new Pair( context, configuration, left, right, 2 );
        }
    }

}

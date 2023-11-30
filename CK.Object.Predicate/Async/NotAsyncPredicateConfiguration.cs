using CK.Core;
using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Not operator.
    /// </summary>
    public sealed class NotAsyncPredicateConfiguration : ObjectAsyncPredicateConfiguration
    {
        [AllowNull]
        readonly ObjectAsyncPredicateConfiguration _operand;

        /// <summary>
        /// Required constructor.
        /// </summary>
        /// <param name="monitor">Unused monitor.</param>
        /// <param name="builder">Unused builder.</param>
        /// <param name="configuration">Captured configuration.</param>
        public NotAsyncPredicateConfiguration( IActivityMonitor monitor,
                                               PolymorphicConfigurationTypeBuilder builder,
                                               ImmutableConfigurationSection configuration )
            : base( configuration )
        {
            var cOperand = configuration.TryGetSection( "Operand" );
            if( cOperand == null )
            {
                monitor.Error( $"Missing '{configuration.Path}:Operand' configuration." );
            }
            else
            {
                _operand = builder.Create<ObjectAsyncPredicateConfiguration>( monitor, cOperand );
            }
        }

        /// <summary>
        /// Gets the operand that is negated.
        /// </summary>
        public ObjectAsyncPredicateConfiguration Operand => _operand;

        /// <inheritdoc />
        public override Func<object, ValueTask<bool>>? CreateAsyncPredicate( IActivityMonitor monitor, IServiceProvider services )
        {
            var p = _operand.CreateAsyncPredicate( monitor, services );
            return p != null ? o => NegateAsync( o, p ) : null;
        }

        static async ValueTask<bool> NegateAsync( object o, Func<object, ValueTask<bool>> operand )
        {
            return !await operand( o );
        }
    }


}

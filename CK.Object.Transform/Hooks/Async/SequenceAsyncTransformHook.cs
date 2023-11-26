using CK.Core;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CK.Object.Transform
{
    /// <summary>
    /// Hook implementation for sequence of asynchronous transform functions.
    /// </summary>
    public class SequenceAsyncTransformHook : ObjectAsyncTransformHook, ISequenceTransformHook
    {
        readonly ImmutableArray<ObjectAsyncTransformHook> _transforms;

        /// <summary>
        /// Initializes a new hook.
        /// </summary>
        /// <param name="configuration">The transform configuration.</param>
        /// <param name="hook">The evaluation hook.</param>
        /// <param name="transforms">The subordinated transform functions.</param>
        public SequenceAsyncTransformHook( ITransformEvaluationHook hook, ISequenceTransformConfiguration configuration, ImmutableArray<ObjectAsyncTransformHook> transforms )
            : base( hook, configuration )
        {
            Throw.CheckNotNullArgument( transforms );
            _transforms = transforms;
        }

        /// <inheritdoc />
        public new ISequenceTransformConfiguration Configuration => Unsafe.As<ISequenceTransformConfiguration>( base.Configuration );

        ImmutableArray<IObjectTransformHook> ISequenceTransformHook.Transforms => ImmutableArray<IObjectTransformHook>.CastUp( _transforms );

        /// <inheritdoc cref="ISequenceTransformHook.Transforms" />
        public ImmutableArray<ObjectAsyncTransformHook> Transforms => _transforms;

        /// <inheritdoc />
        protected override async ValueTask<object> DoTransformAsync( object o )
        {
            // Breaks on a null result: TransformAsync will throw the InvalidOperationException. 
            foreach( var i in _transforms )
            {
                o = await i.TransformAsync( o ).ConfigureAwait( false );
                if( o == null ) break;
            }
            return o!;
        }
    }

}

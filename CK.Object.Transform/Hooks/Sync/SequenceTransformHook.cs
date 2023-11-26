using CK.Core;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CK.Object.Transform
{
    /// <summary>
    /// Hook implementation for sequence of synchronous transformations.
    /// </summary>
    public class SequenceTransformHook : ObjectTransformHook, ISequenceTransformHook
    {
        readonly ImmutableArray<ObjectTransformHook> _transforms;

        /// <summary>
        /// Initializes a new hook.
        /// </summary>
        /// <param name="configuration">The transform configuration.</param>
        /// <param name="hook">The evaluation hook.</param>
        /// <param name="transforms">The subordinated transform functions.</param>
        public SequenceTransformHook( ITransformEvaluationHook hook, ISequenceTransformConfiguration configuration, ImmutableArray<ObjectTransformHook> transforms )
            : base( hook, configuration )
        {
            Throw.CheckNotNullArgument( transforms );
            _transforms = transforms;
        }

        /// <inheritdoc />
        public new ISequenceTransformConfiguration Configuration => Unsafe.As<ISequenceTransformConfiguration>( base.Configuration );

        ImmutableArray<IObjectTransformHook> ISequenceTransformHook.Transforms => ImmutableArray<IObjectTransformHook>.CastUp( _transforms );

        /// <inheritdoc cref="ISequenceTransformHook.Transforms" />
        public ImmutableArray<ObjectTransformHook> Transforms => _transforms;

        /// <inheritdoc />
        protected override object DoTransform( object o )
        {
            // Breaks on a null result: Transform will throw the InvalidOperationException. 
            foreach( var i in _transforms )
            {
                o = i.Transform( o );
                if( o == null ) break;
            }
            return o!;
        }
    }

}

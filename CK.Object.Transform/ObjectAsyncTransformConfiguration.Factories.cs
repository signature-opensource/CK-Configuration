using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Object.Transform
{
    public abstract partial class ObjectAsyncTransformConfiguration
    {
        internal static ObjectAsyncTransformConfiguration DoCreateGroup( string configurationPath,
                                                                         IReadOnlyList<ObjectAsyncTransformConfiguration> predicates )
        {
            if( predicates.All( p => p is ObjectTransformConfiguration ) )
            {
                var syncTransforms = predicates.Cast<ObjectTransformConfiguration>().ToImmutableArray();
                return new SequenceTransformConfiguration( configurationPath, syncTransforms );
            }
            return new SequenceAsyncTransformConfiguration( configurationPath, predicates.ToImmutableArray() );
        }
    }
}

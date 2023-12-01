using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Object.Predicate
{

    public abstract partial class ObjectAsyncPredicateConfiguration
    {

        internal static ObjectAsyncPredicateConfiguration DoCreateGroup( int knownAtLeast,
                                                                         int knownAtMost,
                                                                         string configurationPath,
                                                                         IReadOnlyList<ObjectAsyncPredicateConfiguration> predicates )
        {
            if( predicates.All( p => p is ObjectPredicateConfiguration ) )
            {
                var syncPredicates = predicates.Cast<ObjectPredicateConfiguration>().ToImmutableArray();
                return new GroupPredicateConfiguration( knownAtLeast, knownAtMost, configurationPath, syncPredicates );
            }
            return new GroupAsyncPredicateConfiguration( knownAtLeast, knownAtMost, configurationPath, predicates.ToImmutableArray() );
        }


    }
}

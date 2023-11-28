# CK.Object.Transform
This package is very similar to the [CK.Object.Predicate](../CK.Object.Predicate/README.md), only a bit
simpler.

It enables composition of transform functions `Func<object,object>` (and `Func<object,ValueTask<object>>`)
instead of object predicates (`Func<object,bool>` and `Func<object,ValueTask<bool>>`).

Transforms are available in Sync and Async just like Predicates. The naming follows exactly the same pattern and
a similar Async on Sync adapter [`AsyncTransformAdapterConfiguration<T>`](AsyncTransformAdapterConfiguration.cs) is
available.

They both support Placeholder (see [ExtensibleConfiguration](../Tests/ConfigurationPlugins/StrategyPlugin/ExtensibleConfiguration/README.md]).

The differences (other than the function signatures) are:
- The `null` function for Transform is the identity function whereas it is the "empty predicate" for Predicate.
- The composite for Transform is a simple [SequenceTransformConfiguration](Sync/SequenceTransformConfiguration.cs) of other
  transform functions acting as a pipeline of transfomations whereas for Predicate it is the bit more complex
  `GroupPredicateConfiguration` with its `All`, `Any` and `AtLeast` configuration.
- The default composite field name is "Transforms" (instead of "Predicates").
- Transform has no equivalent of `AlwaysTruePredicateConfiguration` and `AlwaysFalsePredicateConfiguration`.
- The [TransformHookContext](Hooks/TransformHookContext.cs) methods have different signatures that enable
  propagation of the evaluation exception up to the transformation root.

Transform functions have no defined behavior regarding the input object type. Mismatches
can trigger an `ArgumentException` to be thrown, can return an exception as the transformation
result (or any other special object). This is up to the concrete transformations to choose
one (or more?) pattern.

However, one can consider that constraints (among them the object's type) on the input object
should always be satisfied and that when they are not, simply throwing an exception is not
a bad pattern: this should never occur.

However, instead of throwing, simply returning the exception is also possible. Beacause there's
little chance that exceptions be valid input type, a returned exception is an error. Actually the
`TransformHookContext` handles this and let any exception bubbles up to the root.



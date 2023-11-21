# CK.Object.Filter

## Object predicates

This assembly provides [`ObjectFilterConfiguration`](Sync/ObjectFilterConfiguration.cs) and [`ObjectAsyncFilterConfiguration`](Async/ObjectAsyncFilterConfiguration.cs)
that can create respectively `Func<object,bool>` and `Func<object,ValueTask<bool>>` configured predicates. 

For both of them, a `IServiceProvider` can be provided to instantiate the predicates or, if the predicate family
doesn't require DI, an empty `IServiceProvider` is provided to the single abstract `CreateFilter` method.

The filter composite is [`GroupObjectFilterConfiguration`](Sync/GroupObjectFilterConfiguration.cs) (resp. [`GroupObjectAsyncFilterConfiguration`](Async/GroupObjectAsyncFilterConfiguration.cs)).
A group content is by default a `Filters` field (this can be changed).

A group defaults to 'All' (logical connector 'And'), but the configuration can specify `Any: true` or `AtLeast: <n>` where
`<n>` is the number of predicates that must be satisfied among the `Filters.Count` subordinated filters (this offers
a "n among m" condition). 

When there is no subordinated filters, a 'All' group always evaluates to false and a 'Any' group always
evaluates to true (this is the same as the Linq `All`/`Any` extension methods).

Created predicates are pure functions. When they are called, only the final result is observable, the decisions
taken are the result of the configuration without any explanations. Instead of pure functions, "hooks" object
can be created from a configuration: their `Evaluate( object )` and `EvaluateAsync( object )` enables the
decisions to be analyzed.

## Hooks for observability.





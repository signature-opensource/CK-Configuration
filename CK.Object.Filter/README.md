# CK.Object.Filter

## Object predicates

This assembly provides [`ObjectFilterConfiguration`](Sync/ObjectFilterConfiguration.cs) and [`ObjectAsyncFilterConfiguration`](Async/ObjectAsyncFilterConfiguration.cs)
that can create respectively `Func<object,bool>` and `Func<object,ValueTask<bool>>` configured predicates.

```csharp
public abstract class ObjectFilterConfiguration : IObjectFilterConfiguration
{
  /* ... */

  /// <summary>
  /// Creates a synchronous predicate that requires external services to do its job.
  /// </summary>
  /// <param name="monitor">The monitor that must be used to signal errors.</param>
  /// <param name="services">The services.</param>
  /// <returns>A configured object filter or null for an empty predicate.</returns>
  public abstract Func<object, bool>? CreatePredicate( IActivityMonitor monitor, IServiceProvider services );

  /// <summary>
  /// Creates a synchronous predicate that doesn't require any external service to do its job.
  /// <see cref="CreatePredicate(IActivityMonitor, IServiceProvider)"/> is called with an empty <see cref="IServiceProvider"/>.
  /// </summary>
  /// <param name="monitor">The monitor that must be used to signal errors.</param>
  /// <returns>A configured object filter or null for an empty predicate.</returns>
  public Func<object, bool> CreatePredicate( IActivityMonitor monitor ) => CreatePredicate( monitor, EmptyServiceProvider.Instance );
}
```
For simple scenario where filters don't need external services, an empty service provider is used. `ObjectAsyncFilterConfiguration`
has the same methods that return `Func<object,ValueTask<bool>>`.

The filter composite is [`GroupObjectFilterConfiguration`](Sync/GroupObjectFilterConfiguration.cs) (resp. [`GroupObjectAsyncFilterConfiguration`](Async/GroupObjectAsyncFilterConfiguration.cs)).
A group content is by default defined by a `Filters` field (this can be changed when registering the type resolver).

A group defaults to 'All' (logical connector 'And'), but the configuration can specify `Any: true` or `AtLeast: <n>` where
`<n>` is the number of predicates that must be satisfied among the `Filters.Count` subordinated filters (this offers
a "n among m" condition).

## Configuration sample.
A typical filter configuration looks like this (in json):
```jsonc
{
  "Condition": {
    // No type resolved to "All" ("And" connector).
    "Filters": [
      {
        "Filters": [
          {
              // This is the same as below.
              "Type": true,
          },
          {
              // An intrinsic AlwaysTrue object filter.
              // A "AlwaysFalse" is also available.
              "Type": "AlwaysTrue",
          },
          {
              // This (stupid) filter is implemented in this assembly.
              "Assemblies": {"CK.Object.Filter.Tests": "Tests"},
              "Type": "EnumerableMaxCount, Tests",
              "MaxCount": 5
          }
        ]},
        {
          // "Any" is the "Or" connector.
          "Type": "Any",
          "Assemblies": {"CK.Object.Filter.Tests": "P"},
          "Filters": [
            {
                "Type": "StringContains, P",
                "Content": "A"
            },
            {
                "Type": "StringContains, P",
                "Content": "B"
            }]
        },
        {
          // The "Group" with "AtLeast" enables a "n among m" condition.
          "Type": "Group",
          "AtLeast": 2,
          "Assemblies": {"CK.Object.Filter.Tests": "P"},
          "Filters": [
            {
                "Type": "StringContains, P",
                "Content": "x"
            },
            {
                "Type": "StringContains, P",
                "Content": "y"
            },
            {
                "Type": "StringContains, P",
                "Content": "z"
            }]
        }]
      }
   }
}
```

## Logical pitfalls of 'All' and 'Any' and the "empty predicate".

When there is no subordinated filters, what should a 'All' or 'Any' group answer? 
Linq `All`/`Any` extension methods answer are:
- An empty 'All' evaluates to false.
- An empty 'Any' evaluates to true.

This is a convention (that actually is the result of the code). One could have implemented this but instead
we rely on the fact that our `GroupObjectFilterConfiguration` is not the filter itself but a factory
of filter: we allow `CreatePredicate` to return a null function. This null function is the "empty predicate".
A empty predicate result is not true nor false: it **is not**. This could also have been modeled by returning
a `bool?` instead of a `bool`  but ternary logic is tedious and error prone.

An empty predicate is not an error, it's just that it has nothing to say and should simply be ignored
by callers. `GroupObjectFilterConfiguration` filters out its null children predicates and eventually
returns a null predicate if it has no actual child predicate.

Note that `GroupObjectFilterConfiguration` also avoids returning a stupidly complex predicate when
it has only one actual child predicate: it simply returns the single predicate since this works
for 'All' as well as 'Any' (and AtLeast requires at least 3 children).

## Sync vs. Async
Filters can be asynchronous or synchronous. In practice, mixing the sync and async filters in the same
environment (to handle the same objects) is not common and not that easy because a parent synchronous
filter forbids any subordinated asynchronous filter to exist (the reverse is not true). 

Moreover, an Async configuration may contain apsects specific to the asynchronous context (typically
a timeout).

We choose to implement 2 independent families (in the same `CK.Object.Filter` namespace).
This is cleaner than a "rich base class" that would force the developper to implement both the
sync and the async (or throws `NotImplementedException`).

However, Async on Sync is safe and rather easy to support so an adapter exists that can transform a
synchronous filter into an asynchronous one:
```csharp
public class AsyncFilterAdapterConfiguration<T> : ObjectAsyncFilterConfiguration where T : ObjectFilterConfiguration
{
    readonly T _sync;

    public AsyncFilterAdapterConfiguration( T sync ) : base( sync.Configuration )
    {
        _sync = sync;
    }

    public override Func<object, ValueTask<bool>>? CreatePredicate( IActivityMonitor monitor, IServiceProvider services )
    {
        Func<object, bool>? p = _sync.CreatePredicate( monitor, services );
        return p != null ? o => ValueTask.FromResult( p( o ) ) : null;
    }
}
```
There is currently no mechanism to configure the Async filter type resolver to automatically falls
back and instantiate such adapter. This must be done manualy. Below is a synchronous stupid filter
used by tests:
```csharp
public sealed class StringContainsFilterConfiguration : ObjectFilterConfiguration
{
    readonly string _content;

    public StringContainsFilterConfiguration( IActivityMonitor monitor, PolymorphicConfigurationTypeBuilder builder, ImmutableConfigurationSection configuration )
        : base( monitor, builder, configuration )
    {
        var c = configuration["Content"];
        if( c == null )
        {
            monitor.Error( $"Missing '{configuration.Path}:Content' value." );
        }
        _content = c!;
    }

    public override Func<object, bool> CreatePredicate( IActivityMonitor monitor, IServiceProvider services )
    {
        return o => o is string s && s.Contains( _content );
    }
}
```
And its asynchronous adapted type:
```csharp
public sealed class StringContainsAsyncFilterConfiguration : AsyncFilterAdapterConfiguration<StringContainsFilterConfiguration>
{
    public StringContainsAsyncFilterConfiguration( IActivityMonitor monitor, PolymorphicConfigurationTypeBuilder builder, ImmutableConfigurationSection configuration )
        : base( new StringContainsFilterConfiguration( monitor, builder, configuration ) )
    {
    }
}
```

## Hooks for observability.

Created predicates are pure functions. When they are called, only the final result is observable, the decisions
taken are the result of the configuration without any explanations. Instead of pure functions, a filter
object can be created from a configuration: their `Evaluate( object )` and `EvaluateAsync( object )` enables the
decisions to be analyzed.

The [`EvaluationHook`](Hooks/EvaluationHook.cs) enables to create wrapper rather than pure predicate
functions:
```csharp
public abstract class ObjectFilterConfiguration : IObjectFilterConfiguration
{
  /* ... */

  /// <summary>
  /// Creates a <see cref="ObjectFilterHook"/> with this configuration and a predicate obtained by
  /// calling <see cref="CreatePredicate(IActivityMonitor, IServiceProvider)"/>.
  /// <para>
  /// This should be overridden if this filter relies on other filters in order to hook all the filters.
  /// Failing to do so will hide some predicates to the evaluation hook.
  /// </para>
  /// </summary>
  /// <param name="monitor">The monitor that must be used to signal errors.</param>
  /// <param name="hook">The evaluation hook.</param>
  /// <param name="services">The services.</param>
  /// <returns>A configured filter hook bound to the evaluation hook or null for an empty filter.</returns>
  public virtual ObjectFilterHook? CreateHook( IActivityMonitor monitor, EvaluationHook hook, IServiceProvider services )
  {
      var p = CreatePredicate( monitor, services );
      return p != null ? new ObjectFilterHook( hook, this, p ) : null;
  }
}
```
[`ObjectFilterHook`](Hooks/Sync/ObjectFilterHook.cs) (and [`ObjectAsyncFilterHook`](Hooks/Async/ObjectAsyncFilterHook.cs))
give access to the filter's configuration and when they are [`GroupFilterHook`](Hooks/Sync/ObjectFilterHook.cs)
(and [`GroupAsyncFilterHook`](Hooks/Async/GroupAsyncFilterHook.cs)) they expose their subordinated
items: the whole resolved structure can be explored.

Note that filters follow the same "empty predicate" null management: a null filter is empty and should
be ignored.

The specialized [`MonitoredEvaluationHook`](Hooks/MonitoredEvaluationHook.cs) logs all the evaluator
along with their result and captures exceptions that evaluation may throw.

Sample usage (the evaluation of "Bzy" is logged into the `TestHelper.Monitor`):
```csharp
[Test]
public void complex_configuration_tree_with_EvaluationHook()
{
    MutableConfigurationSection config = GetComplexConfiguration();
    var builder = new PolymorphicConfigurationTypeBuilder();
    ObjectFilterConfiguration.AddResolver( builder );

    var fC = builder.Create<ObjectFilterConfiguration>( TestHelper.Monitor, config );
    Throw.DebugAssert( fC != null );

    var hook = new MonitoredEvaluationHook( TestHelper.Monitor );

    var f = fC.CreateHook( TestHelper.Monitor, hook );
    f.Evaluate( "Bzy" ).Should().Be( true );
}
```
This is of course a rather basic hook. Specialized `EvaluationHook` are easy to implement (there are
3 methods to override).

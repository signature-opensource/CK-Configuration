# CK.Object.Transform
This package is very similar to the [CK.Object.Predicate](../CK.Object.Predicate/README.md), only a bit
simpler.

It enables composition of transform functions `Func<object,object>` (and `Func<object,ValueTask<object>>`)
instead of object predicates (`Func<object,bool>` and `Func<object,ValueTask<bool>>`).

Transforms are available in Sync and Async just like Predicates. The naming follows exactly the same pattern.
They both enables Placeholder (see 

The differences (other than the function signatures) are:
- The `null` function for Transform is the identity function. It is the "empty predicate" for Predicate.
- The composite for Transform is a simple [SequenceTransformConfiguration](Sync/SequenceTransformConfiguration.cs) of other
  transform functions acting as a pipeline of transfomations whereas for Predicate it is the bit more complex
  `GroupPredicateConfiguration` with its `All`, `Any` and `AtLeast` configuration.
- Transform has no equivalent of `AlwaysTruePredicateConfiguration` and `AlwaysFalsePredicateConfiguration`.



## Object predicates

This assembly provides [`ObjectTransformConfiguration`](Sync/ObjectTransformConfiguration.cs) and [`ObjectAsyncTransformConfiguration`](Async/ObjectAsyncTransformConfiguration.cs)
that can create respectively `Func<object,bool>` and `Func<object,ValueTask<bool>>` configured predicates.

```csharp
public abstract class ObjectTransformConfiguration : IObjectTransformConfiguration
{
  /* ... */

  /// <summary>
  /// Creates a synchronous predicate that requires external services to do its job.
  /// </summary>
  /// <param name="monitor">The monitor that must be used to signal errors.</param>
  /// <param name="services">The services.</param>
  /// <returns>A configured object predicate or null for an empty predicate.</returns>
  public abstract Func<object, bool>? CreateTransform( IActivityMonitor monitor, IServiceProvider services );

  /// <summary>
  /// Creates a synchronous predicate that doesn't require any external service to do its job.
  /// <see cref="CreateTransform(IActivityMonitor, IServiceProvider)"/> is called with an empty <see cref="IServiceProvider"/>.
  /// </summary>
  /// <param name="monitor">The monitor that must be used to signal errors.</param>
  /// <returns>A configured object predicate or null for an empty predicate.</returns>
  public Func<object, bool> CreateTransform( IActivityMonitor monitor ) => CreateTransform( monitor, EmptyServiceProvider.Instance );
}
```
For simple scenario where predicates don't need external services, an empty service provider is used. `ObjectAsyncTransformConfiguration`
has the same methods that return `Func<object,ValueTask<bool>>`.

The predicate composite is [`GroupObjectTransformConfiguration`](Sync/GroupObjectTransformConfiguration.cs) (resp. [`GroupObjectAsyncTransformConfiguration`](Async/GroupObjectAsyncTransformConfiguration.cs)).
A group content is by default defined by a `Transforms` field (this can be changed when registering the type resolver).

A group defaults to 'All' (logical connector 'And'), but the configuration can specify `Any: true` or `AtLeast: <n>` where
`<n>` is the number of predicates that must be satisfied among the `Transforms.Count` subordinated items (this offers
a "n among m" condition).

## Configuration sample.
A typical predicate configuration looks like this (in json):
```jsonc
{
  "Condition": {
    // No type resolved to "All" ("And" connector).
    "Transform": [
      {
        "Transforms": [
          {
              // This is the same as below.
              "Type": true,
          },
          {
              // An intrinsic AlwaysTrue object predicate.
              // A "AlwaysFalse" is also available.
              "Type": "AlwaysTrue",
          },
          {
              // This (stupid) predicate is implemented in this assembly.
              "Assemblies": {"CK.Object.Transform.Tests": "Tests"},
              "Type": "EnumerableMaxCount, Tests",
              "MaxCount": 5
          }
        ]},
        {
          // "Any" is the "Or" connector.
          "Type": "Any",
          "Assemblies": {"CK.Object.Transform.Tests": "P"},
          "Transforms": [
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
          "Assemblies": {"CK.Object.Transform.Tests": "P"},
          "Transforms": [
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

When there is no subordinated predicates, what should a 'All' or 'Any' group answer? 
Linq `All`/`Any` extension methods answer are:
- An empty 'All' evaluates to false.
- An empty 'Any' evaluates to true.

This is a convention (that actually is the result of the code). One could have implemented this but instead
we rely on the fact that our `GroupObjectTransformConfiguration` is not the predicate itself but a factory
of predicates: we allow `CreateTransform` to return a null function. This null function is the "empty predicate".
A empty predicate result is not true nor false: it **is not**. This could also have been modeled by returning
a `bool?` instead of a `bool`  but ternary logic is tedious and error prone.

An empty predicate is not an error, it's just that it has nothing to say and should simply be ignored
by callers. `GroupObjectTransformConfiguration` filters out its null children predicates and eventually
returns a null predicate if it has no actual child predicate.

Note that `GroupObjectTransformConfiguration` also avoids returning a stupidly complex predicate when
it has only one actual child predicate: it simply returns the single predicate since this works
for 'All' as well as 'Any' (and AtLeast requires at least 3 children).

## Sync vs. Async
Transforms can be asynchronous or synchronous. In practice, mixing the sync and async predicates in the same
environment (to handle the same objects) is not common and not that easy because a parent synchronous
predicate forbids any subordinated asynchronous predicate to exist (the reverse is not true). 

Moreover, an Async configuration may contain apsects specific to the asynchronous context (typically
a timeout).

We choose to implement 2 independent families (in the same `CK.Object.Transform` namespace).
This is cleaner than a "rich base class" that would force the developper to implement both the
sync and the async (or throws `NotImplementedException`).

However, Async on Sync is safe and rather easy to support so an adapter exists that can transform a
synchronous predicate into an asynchronous one:
```csharp
public class AsyncTransformAdapterConfiguration<T> : ObjectAsyncTransformConfiguration where T : ObjectTransformConfiguration
{
    readonly T _sync;

    public AsyncTransformAdapterConfiguration( T sync ) : base( sync.Configuration )
    {
        _sync = sync;
    }

    public override Func<object, ValueTask<bool>>? CreateTransform( IActivityMonitor monitor, IServiceProvider services )
    {
        Func<object, bool>? p = _sync.CreateTransform( monitor, services );
        return p != null ? o => ValueTask.FromResult( p( o ) ) : null;
    }
}
```
There is currently no mechanism to configure the Async predicate type resolver to automatically falls
back and instantiate such adapter. This must be done manualy. Below is a synchronous stupid predicate
used by tests:
```csharp
public sealed class StringContainsTransformConfiguration : ObjectTransformConfiguration
{
    readonly string _content;

    public StringContainsTransformConfiguration( IActivityMonitor monitor, PolymorphicConfigurationTypeBuilder builder, ImmutableConfigurationSection configuration )
        : base( monitor, builder, configuration )
    {
        var c = configuration["Content"];
        if( c == null )
        {
            monitor.Error( $"Missing '{configuration.Path}:Content' value." );
        }
        _content = c!;
    }

    public override Func<object, bool> CreateTransform( IActivityMonitor monitor, IServiceProvider services )
    {
        return o => o is string s && s.Contains( _content );
    }
}
```
And its asynchronous adapted type:
```csharp
public sealed class StringContainsAsyncTransformConfiguration : AsyncTransformAdapterConfiguration<StringContainsTransformConfiguration>
{
    public StringContainsAsyncTransformConfiguration( IActivityMonitor monitor, PolymorphicConfigurationTypeBuilder builder, ImmutableConfigurationSection configuration )
        : base( new StringContainsTransformConfiguration( monitor, builder, configuration ) )
    {
    }
}
```

## Hooks for observability.

Created predicates are pure functions. When they are called, only the final result is observable, the decisions
taken are the result of the configuration without any explanations. Instead of pure functions, a predicate
object can be created from a configuration: their `Evaluate( object )` and `EvaluateAsync( object )` enables the
decisions to be analyzed.

The [`ITransformEvaluationHook`](Hooks/ITransformEvaluationHook.cs) enables to create wrapper rather than pure predicate
functions:
```csharp
public abstract class ObjectTransformConfiguration : IObjectTransformConfiguration
{
  /* ... */

  /// <summary>
  /// Creates a <see cref="ObjectTransformHook"/> with this configuration and a predicate obtained by
  /// calling <see cref="CreateTransform(IActivityMonitor, IServiceProvider)"/>.
  /// <para>
  /// This should be overridden if this predicate relies on other predicates in order to hook all of them.
  /// Failing to do so will hide some predicates to the evaluation hook.
  /// </para>
  /// </summary>
  /// <param name="monitor">The monitor that must be used to signal errors.</param>
  /// <param name="hook">The evaluation hook.</param>
  /// <param name="services">The services.</param>
  /// <returns>A wrapper bound to the evaluation hook or null for an empty predicate.</returns>
  public virtual ObjectTransformHook? CreateHook( IActivityMonitor monitor, ITransformEvaluationHook hook, IServiceProvider services )
  {
      var p = CreateTransform( monitor, services );
      return p != null ? new ObjectTransformHook( hook, this, p ) : null;
  }
}
```
[`ObjectTransformHook`](Hooks/Sync/ObjectTransformHook.cs) (and [`ObjectAsyncTransformHook`](Hooks/Async/ObjectAsyncTransformHook.cs))
give access to the predicate's configuration and when they are [`GroupTransformHook`](Hooks/Sync/ObjectTransformHook.cs)
(and [`GroupAsyncTransformHook`](Hooks/Async/GroupAsyncTransformHook.cs)) they expose their subordinated
items: the whole resolved structure can be explored.

Note that hook follow the same "empty predicate" null management: a null hook is empty and should
be ignored.

The specialized [`MonitoredTransformEvaluationHook`](Hooks/MonitoredTransformEvaluationHook.cs) logs all the evaluator
along with their result and captures exceptions that evaluation may throw.

Sample usage (the evaluation of "Bzy" is logged into the `TestHelper.Monitor`):
```csharp
[Test]
public void complex_configuration_tree_with_EvaluationHook()
{
    MutableConfigurationSection config = GetComplexConfiguration();
    var builder = new PolymorphicConfigurationTypeBuilder();
    ObjectTransformConfiguration.AddResolver( builder );

    var fC = builder.Create<ObjectTransformConfiguration>( TestHelper.Monitor, config );
    Throw.DebugAssert( fC != null );

    var hook = new MonitoredTransformEvaluationHook( TestHelper.Monitor );

    var f = fC.CreateHook( TestHelper.Monitor, hook );
    f.Evaluate( "Bzy" ).Should().Be( true );
}
```
This is of course a rather basic hook. `ITransformEvaluationHook` are easy to implement (there are 3
methods to override).

# CK.Object.Processor

A processor combines a condition on an object (a `Func<object,bool>` from
[CK.Object.Predicate](../CK.Object.Predicate/README.md)) and a transform function (a `Func<object,object>`
from [CK.Object.Transform](../CK.Object.Transform/README.md). The transform function is called
when the condition evaluates to true.

A processor is ultimately a `Func<object,object?>`: a `null` result captures the fact that the
condition failed.

## Configured Configuration and Transform and intrinsic ones.
Both the `Condition` and the `Transform` are optional: when they are not defined or results to
a (null) empty predicate and a (null) identity function, the processor is the void precessor that
is also `null`.

The [`ObjectProcessorConfiguration`](Sync/ObjectProcessorConfiguration.cs) is a concrete class:
by configuring its `Condition` with a predicate and its `Transform` with a transform function, it
is operational.

However, most often, we specialize this base class and overrides the `CreateIntrinsicCondition`
and `CreateIntrinsicTransform` (by default they return null). These methods can implement any
condition or transformation in addition to the potentially configured ones. Below is a
(rather stupid) processor that negates a double:
```csharp
public sealed class NegateDoubleProcessorConfiguration : ObjectProcessorConfiguration
{
    public NegateDoubleProcessorConfiguration( IActivityMonitor monitor,
                                                PolymorphicConfigurationTypeBuilder builder,
                                                ImmutableConfigurationSection configuration )
        : base( monitor, builder, configuration )
    {
    }

    protected override Func<object, bool>? CreateIntrinsicCondition( IActivityMonitor monitor, IServiceProvider services )
    {
        return static o => o is double;
    }

    protected override Func<object, object>? CreateIntrinsicTransform( IActivityMonitor monitor, IServiceProvider services )
    {
        return static o => -((double)o);
    }
}
```
The execution order is as follow:
- First, the intrinsic condition is tested. If it fails, the processor returns null.
- Then the configured `Condition` is tested. If it fails, the processor returns null.
- Once accepted, the object is transformed:
  - First, the intrinsic transform is applied.
  - Then the configured `Transform` is applied to the intrinsic result.

## Sequence of processors
The [`SequenceProcessorConfiguration`](Sync/SequenceProcessorConfiguration.cs) is the default composite
for this family types. It is defined by the "Processors" configuration key:
```jsonc
{
    "Assemblies": { "CK.Object.Processor.Tests": "Test"},
    "Processors": [/*...*/]
}
```
A `SequenceProcessorConfiguration` is a "first-wins" list of processors (a kind of switch-case).
A sequence is Processor with its configured `Condition` and `Transform` and this class is not
sealed, it may also be specialized to override `CreateIntrinsicCondition`
and/or `CreateIntrinsicTransform`.

The execution order is as follow:
- First, the intrinsic condition is tested. If it fails, the sequence processor returns null.
- Then the configured `Condition` is tested. If it fails, the sequence processor returns null.
- Once this first check done:
   - The accepted object is submitted to the subordinated processors.
   - If none of them processed it, the sequence processor returns null.
   - If the object has been processed, then we apply the sequence Transform to the processed result:
     - First, the intrinsic transform is applied.
     - Then the configured `Transform` is applied to the intrinsic result.


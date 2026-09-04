# CK.Configuration

Provides extensions and helpers to manipulate .NET configuration sections.

First, please read [MutableConfigurationSection and ImmutableConfigurationSection](ConfigurationSection/README.md)
that provide the basic mechanisms.

## AssemblyConfiguration
Handles "DefaultAssembly" and "Assemblies" sections. These configurations can appear
at different levels and are combined to provide a list of assemblies that typically
contain "plugins".

Assemblies can be aliased:

```json
{
  "DefaultAssembly": "MyPlugins",
  "Assemblies": [
    "Acme.Corp.Strategies",
    { "Assembly": "Too.Long.To.Repeat.Plugin.Assembly", "Alias": "A" },
    "Universal.StdPlugins",
  ]
}
```

Assemblies and aliases can also be expressed as:
```json
{
  "Assemblies": {
    "Acme.Corp.Strategies": "A",
    "Too.Long.To.Repeat.Plugin.Assembly": "B",
    "Universal.StdPlugins": "C"
  }
}
```

Assembly names have no version, culture, or token. Only the simple name is considered and this
is by design.

`AssemblyConfiguration.TryResolveType` method does the actual job of "finding a plugin".
A plugin is typically defined with a simple `Type = "XXX"` configuration:

- `Type = "My.Namespace.MyPluginComponentConfiguration"`: Will be searched in the DefaultAssembly if defined
  (or in the assembly that defines the "plugin family" - see the `TypedConfigurationBuilder` for this).
- `Type = "MyPlugin"`: The TypedConfigurationBuilder introduces a default namespace and
  automatically suffix the type name with its resolver's configuration.
- `Type = "MyPlugin, Acme.Corp.Strategies"`: The type will be search in the specified assembly (that must
  be explicitely allowed). 
- `Type = "MyPlugin, B"`: The type will be search in the specified assembly alias. 

The `AssemblyConfiguration` can be locked. When locked, subordinated  "DefaultAssembly" and "Assemblies" sections
are ignored (with a warning). No more external assemblies can enter the game. To lock the configuration,
multiple constructs are handled, **"IsLocked", "Lock" and "Locked"** are synonyims:
```json
{
  "Assemblies": {
    "Acme.Corp.Strategies": "A",
    "Too.Long.To.Repeat.Plugin.Assembly": "B",
    "Universal.StdPlugins": "C",
    "IsLocked": true
  }
}
```
Or:
```jsonc
{
  "Assemblies": "Lock",
}
```
Or:
```jsonc
{
  "Assemblies": [ "Locked", "ConsumerA.Strategy" ],
}
```


## TypedConfigurationBuilder

The [strategy design pattern](https://en.wikipedia.org/wiki/Strategy_pattern) encapsulates
variability behind an abstraction. Mixed with the [composite design pattern](https://en.wikipedia.org/wiki/Composite_pattern),
strategies are powerful tools.

This library provides a small framework that helps implementing a configuration layer that describes
"configured objects" (possibly implemented in external assemblies - plugins).

Configured objects are immutable and are the factories of actual objetcs that are typically instantiated
in a "unit of work", a DI Scope.

The [`TypedConfigurationBuilder`](TypedConfigurationBuilder.cs)
offers a simple and extensible way to instantiate one (or more) family of "configured objects".

A sample is available in [Tests/ConfigurationPlugins](../Tests/ConfigurationPlugins) that demonstrate
a simple strategy, its composite, and 2 sets of configuration objects, one of them being "extensible":
placeholders can be defined and "patched" with "dynamic configurations".

### What the caller actually writes.

Five lines of C#, and a configuration whose `Type` values may be spelled three different ways:

```json
{
    "DefaultAssembly": "ConsumerA.Strategy",
    "S1": {
        "Type": "Simple",
        "Action": "hello World!"
    },
    "S2": {
        "Type": "AnotherSimpleStrategy"
    },
    "S3": {
        "Type": "AnotherSimpleStrategyConfiguration"
    }
}
```

```csharp
var config = ImmutableConfigurationSection.CreateFromJson( "Root", theJsonAbove );

var builder = new TypedConfigurationBuilder();
IStrategyConfiguration.AddResolverWithoutComposite( builder );
builder.AssemblyConfiguration = AssemblyConfiguration.Create( monitor, config ) ?? AssemblyConfiguration.Empty;

var s1C = builder.Create<IStrategyConfiguration>( monitor, config.GetRequiredSection( "S1" ) );
// ... and the same for "S2" and "S3".
```

Two stages, and the split is the whole design: `builder.Create<T>` returns the **configured object**,
immutable and reusable, while the actual working object comes from the family's own factory method:

```csharp
var s1 = s1C.CreateStrategy( monitor );   // a null check on s1 elided
s1.GetType().FullName.ShouldBe( "ConsumerA.SimpleStrategy" );
s1.GetType().Assembly.GetName().Name.ShouldBe( "ConsumerA.Strategy" );
```

`CreateStrategy` is not framework API - it is a member of `IStrategyConfiguration`, this family's root
type. That is what "there is absolutely no constraint on this final type" means in practice.

All three `Type` spellings resolve, and the table is worth reading in two columns because the
configuration type and the object it builds are not the same thing:

| `Type` | configuration type resolved | strategy `CreateStrategy` returns |
|---|---|---|
| `"Simple"` | `Plugin.Strategy.SimpleStrategyConfiguration` | `ConsumerA.SimpleStrategy` |
| `"AnotherSimpleStrategy"` | `Plugin.Strategy.AnotherSimpleStrategyConfiguration` | `Plugin.Strategy.AnotherSimpleStrategy` |
| `"AnotherSimpleStrategyConfiguration"` | same as above | same as above |

So the `Configuration` suffix is optional in the file and a short name is enough: the resolver supplies
both the namespace and the suffix. Two constraints sit behind that, and they apply to different halves:

- **The configuration type's namespace is fixed by the resolver, not by `DefaultAssembly`.** This
  family registers itself with `typeNamespace: "Plugin.Strategy", allowOtherNamespace: false`, so every
  configuration type in it *must* live in `Plugin.Strategy` - which is exactly why a bare `"Simple"` is
  resolvable at all.
- **`DefaultAssembly` fixes the assembly**, and that is what the test asserts - `ConsumerA.Strategy`
  for all three, though only `s1` is shown above.
- **The strategy is under no such rule.** `SimpleStrategyConfiguration` sits in `Plugin.Strategy` and
  returns a `ConsumerA.SimpleStrategy` - it hand-writes `new ConsumerA.SimpleStrategy( this )`. The
  family constrains how configurations are *found*, never where the objects they build live.

Two resolver variants exist on the family root, and only the first is used above:
`AddResolverWithoutComposite`, and `AddResolver`, which additionally passes
`defaultCompositeBaseType: typeof( CompositeStrategyConfiguration )` and
`compositeItemsFieldName: "Strategies"` for a family that supports nesting.

From
[`TypedConfigurationBuilderTests`](../Tests/CK.Configuration.Tests/TypedConfigurationBuilderTests.cs),
whose plugin assemblies are the [`Tests/ConfigurationPlugins`](../Tests/ConfigurationPlugins) sample
mentioned above; `monitor` there is the test helper's.

### Configuration object family and Type resolution.
A family is defined by a root configuration type that is often abstract. This root type defines the
API of the family. A typical member of this API is a factory of actual "strategy" that is fully
configured and operational. There is absolutely no constraint on this final type and there can be more than one
kind of factories. A consumer of this library may for instance define two families side by side, one
producing `Func<object,ValueTask<bool>>` and a second, refining the first, producing the more efficient
`Func<object,bool>`.
  
The complicated stuff is done by the `TypedConfigurationBuilder` and its resolvers.
A configuration can contain multiple families simply by registering the family resolvers that
must be handled: families are composable, hence configurations are composable.


### Configuration "patching": the substituable placeholder.
Configurations are immutable by design for safety and security. But sometimes, islands of
more "dynamic" configurations in a globally immutable and stable configuration are welcome.

The design of a configuration system can introduce such "islands" with dedicated placeholders.
Placeholders are extension points that are "empty" but can be substituted by actual configuration
sections to create a new configuration structure that extends the original one.

The [Tests/ConfigurationPlugins/StrategyPlugin/ExtensibleConfiguration](../Tests/ConfigurationPlugins/StrategyPlugin/ExtensibleConfiguration/README.md)
documents this approach.

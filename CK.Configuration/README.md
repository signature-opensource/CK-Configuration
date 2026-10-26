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
  automatically suffixes the type name with its resolver's configuration.
- `Type = "MyPlugin, Acme.Corp.Strategies"`: The type will be searched in the specified assembly (that must
  be explicitly allowed). 
- `Type = "MyPlugin, B"`: The type will be searched in the specified assembly alias. 

The `AssemblyConfiguration` can be locked. When locked, subordinated  "DefaultAssembly" and "Assemblies" sections
are ignored (with a warning). No more external assemblies can enter the game. To lock the configuration,
multiple constructs are handled, **"IsLocked", "Lock" and "Locked"** are synonyms:
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

Configured objects are immutable and are the factories of actual objects that are typically instantiated
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


### Configuration "patching": the substitutable placeholder.
Configurations are immutable by design for safety and security. But sometimes, islands of
more "dynamic" configurations in a globally immutable and stable configuration are welcome.

The design of a configuration system can introduce such "islands" with dedicated placeholders.
Placeholders are extension points that are "empty" but can be substituted by actual configuration
sections to create a new configuration structure that extends the original one.

This pattern is optional: a configuration family supports it or not. It offers some strong guarantees:
- The hosting configuration itself cannot be altered in any way by the subsequent configurations.
- The placeholders are the only way to "extend" an existing configuration.
- Placeholder replacements work on fully immutable objects: there are no concurrency issues by design.
- Placeholder replacements can be constrained. For instance an extension may not be allowed to use any
  other plugin assemblies than the ones that have been defined in the context of the Placeholder by the
  "host configuration" (by locking the `AssemblyConfiguration`).

The `ImmutableConfigurationSection` is not changed: the immutable configuration objects built upon it give
birth to modified versions of themselves where a placeholder has been replaced (this is the classical
pattern with immutable structures).

#### Supporting placeholders in a family
The [`ISupportConfigurationPlaceholder<T>`](SupportConfigurationPlaceholder.cs) interface must be implemented
by the root type of the family, `T` being this root type:
```csharp
public interface ISupportConfigurationPlaceholder<T> where T : class
{
    T? SetPlaceholder( IActivityMonitor monitor, IConfigurationSection configuration );
}
```
`SetPlaceholder` is a mutator: it returns a new configuration object (or this object if nothing changed) and
should return null only if an error occurred. The `configuration.Path` must be a direct child of the placeholder
to replace. To support placeholders, a family:
- Must support the "empty configured object" pattern, either by implementing the
  [null object pattern](https://en.wikipedia.org/wiki/Null_object_pattern) or by simply allowing created
  configured objects to be null: a placeholder must be an "invisible" factory, with no impact, no "production" at all.
- Implements `SetPlaceholder` on its root type as a virtual method that returns this instance by default.
- Defines a Placeholder type (the convention is to name it "Placeholder") that captures its own configuration,
  the assemblies and the resolvers that apply where it is. Its `SetPlaceholder` override checks that the
  parent path of the proposed section is exactly the placeholder's path and, if it is, builds the configured
  object from the section with a new `TypedConfigurationBuilder` that uses the captured assemblies and resolvers.
- Has composites that override `SetPlaceholder` to forward the call to their items and return a new composite
  if any item changed.

A substituted section can also contain placeholders. In this case, the section name is crucial and must
uniquely identify the replaced section in its parent so that subsequent replacement paths can be properly resolved.

#### Replacing a placeholder
The `SupportConfigurationPlaceholderExtension` provides the `TrySetPlaceholder` extension methods to any
`ISupportConfigurationPlaceholder<T>`:
```csharp
public static T? TrySetPlaceholder<T>( this ISupportConfigurationPlaceholder<T> @this,
                                       IActivityMonitor monitor,
                                       IConfigurationSection configuration ) where T : class

public static T? TrySetPlaceholder<T>( this ISupportConfigurationPlaceholder<T> @this,
                                       IActivityMonitor monitor,
                                       IConfigurationSection configuration,
                                       out bool builderError ) where T : class
```
They call `SetPlaceholder` and return null if an error has been logged while doing so (`builderError` is true)
or if no placeholder has been replaced: when `SetPlaceholder` returns the same object, an error
"Unable to set placeholder: '...' doesn't exist or is not a placeholder." is logged (`builderError` is false).
A null returned by `SetPlaceholder` without any logged error is itself logged as an error.

For an example implementation, see the
[Tests/ConfigurationPlugins/StrategyPlugin/ExtensibleConfiguration](../Tests/ConfigurationPlugins/StrategyPlugin/ExtensibleConfiguration/README.md)
sample.

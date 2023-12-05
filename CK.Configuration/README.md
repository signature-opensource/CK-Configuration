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
    "Too.Long.To.Repeat.Plugin.Assembly": "B" },
    "Universal.StdPlugins": "C"
  }
}
```

Assembly names have no version, culture, or token. Only the simple name is considered and this
is by design.

`AssemblyConfiguration.TryResolveType` method does the actual job of "finding a plugin".
A plugin is typically defined with a simple `Type = "XXX"` configuration:

- `Type = "My.Namespace.MyPluginComponentConfiguration"`: Will be searched in the DefaultAssembly if defined
  (or in the assembly that defines the "plugin family" - see the `PolymorphicConfigurationTypeBuilder` for this).
- `Type = "MyPlugin"`: The PolymorphicConfigurationTypeBuilder introduces a default namespace and
  automatically suffix the type name with its resolver's configuration.
- `Type = "MyPlugin, Acme.Corp.Strategies"`: The type will be search in the specified assembly (that must
  be explicitely allowed). 
- `Type = "MyPlugin, B"`: The type will be search in the specified assembly alias. 

The `AssemblyConfiguration` can be locked. When locked, subordinated  "DefaultAssembly" and "Assemblies" sections
are ignored (with a warning). No more external assemblies can enter the game. To lock the configuration,
multiple constructs are handled, **"IsLocked", "Lock" and "Locked"** are synonims:
```json
{
  "Assemblies": {
    "Acme.Corp.Strategies": "A",
    "Too.Long.To.Repeat.Plugin.Assembly": "B" },
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
  "Assemblies": "Assemblies":[ "Locked", "ConsumerA.Strategy" ],
}
```


## PolymorphicConfigurationTypeBuilder

The [strategy design pattern](https://en.wikipedia.org/wiki/Strategy_pattern) encapsulates
variability behind an abstraction. Mixed with the [composite design pattern](https://en.wikipedia.org/wiki/Composite_pattern),
strategies are powerful tools.

This library provides a small framework that helps implementing a configuration layer that describes
"configured objects" (possibly implemented in external assemblies - plugins).

Configured objects are immutable and are the factories of actual objetcs that are typically instantiated
in a "unit of work", a DI Scope.

The [`PolymorphicConfigurationTypeBuilder`](PolymorphicConfigurationTypeBuilder.cs)
offers a simple and extensible way to instantiate one (or more) family of "configured objects".

A sample is available in [Tests/ConfigurationPlugins](Tests/ConfigurationPlugins) that demonstrate
a simple strategy, its composite, and 2 sets of configuration objects, one of them being "extensible":
placeholders can be defined and "patched" with "dynamic configurations".

### Configuration object family and Type resolution.
A family is defined by a root configuration type that is almost always abstract. This root type defines the
API of the family. A typical member of this API is a factory of actual "strategy" that is fully
configured and operational. There is absolutely no constraint on this final type and there can be more than one
kind of factories: [CK.Object.Predicate](../CK.Object.Predicate.README.md) for instance defines 2 configuration
families, producing 2 types of objects:
  - ObjectAsyncPredicate produce `Func<object,ValueTask<bool>>`.
  - ObjectPredicate (that are ObjectAsyncPredicate) can in addition produce more efficient `Func<object,bool>`.
  
The complicated stuff is done by the `PolymorphicConfigurationTypeBuilder` and its resolvers.
A configuration can contain multiple families simply by registering the family resolvers that
must be handled: families are composable, hence configurations are composable.


### Configuration "patching": the substituable placeholder.
Configurations are immutable by design for safety and security. But sometimes, islands of
more "dynamic" configurations in a globally immutable and stable configuration are welcome.

The design of a configuration system can introduce such "islands" with dedicated placeholders.
Placeholders are extension points that are "empty" but can be substituted by actual configuration
sections to create a new configuration structure that extends the original one.

The [Tests/ConfigurationPlugins/StrategyPlugin/ExtensibleConfiguration](..\Tests\ConfigurationPlugins\StrategyPlugin\ExtensibleConfiguration\README.md)
documents this approach.

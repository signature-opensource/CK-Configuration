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

The `AssemblyConfiguration.TryResolveType` is the method that does the actual job of "finding a plugin".

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

The [CK.Object.Predicate](../CK.Object.Predicate/README.md) is another concrete example.

## Configuration "patching": the substituable placeholder
Configurations are immutable by design for safety and security. But sometimes, islands of
more "dynamic" configurations in a globally immutable and stable configuration are welcome.

The design of a configuration system can introduce such "islands" with dedicated placeholders.
Placeholders are extension points that are "empty" but can be substituted by actual configuration
sections to create a new configuration structure that extends the original one.

`ImmutableConfigurationSection` on which any configuration object relies is deeply immutable. It's
not the section that changes but the immutable configuration objects built upon them that can give
birth to modified version of themselves (this is the classical pattern with immutable structures).

The [Tests/ConfigurationPlugins/StrategyPlugin/ExtensibleConfiguration](../Tests/ConfigurationPlugins/StrategyPlugin/ExtensibleConfiguration/README.md)
documents this approach.

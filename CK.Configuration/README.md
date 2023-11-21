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

The [`PolymorphicConfigurationTypeBuilder`](CK.Configuration/PolymorphicConfigurationTypeBuilder.cs)
offers a simple and extensible way to instantiate one (or more) family of "configured objects".

A sample is available in [Tests/ConfigurationPlugins](Tests/ConfigurationPlugins) that demonstrate
a simple strategy, its composite, and 2 sets of configuration objects.

The [CK.Object.Filter](../CK.Object.Filter/README.md) is another concrete example.

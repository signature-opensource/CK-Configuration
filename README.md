# CK.Configuration

Provides extensions and helpers to CK.Core [Mutable and ImmutableConfigurationSection](https://github.com/Invenietis/CK-Core/tree/develop/CK.Core/Configuration).

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
    { Assembly": "Too.Long.To.Repeat.Plugin.Assembly", "Alias": "A" },
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
    "Universal.StdPlugins": "C",
  }
}
```

Assembly names have no version, culture, or token. Only the simple name is considered and this
is by design.

## Strategies and polymorphic configurations

The [strategy design pattern](https://en.wikipedia.org/wiki/Strategy_pattern) encapsulates
variability behind an abstraction. Mixed with the [composite design pattern](https://en.wikipedia.org/wiki/Composite_pattern),
strategies are powerful tools.

This library provides a small framework that support strategies implemented in external assemblies
(plugins) and instantiating them from a configuration layer.

Configuration objects are immutable and are the factories of usable objetcs, typically instantiated
in a "unit of work", a DI Scope.

The [`PolymorphicConfigurationTypeBuilder`](CK.Configuration/PolymorphicConfigurationTypeBuilderTests.cs)
offers a simple way to instantiate a family of configured objects.

A sample is available in [Tests/ConfigurationPlugins](Tests/ConfigurationPlugins) that demonstrate
a simple strategy, its composite, and 2 sets of configuration objects.


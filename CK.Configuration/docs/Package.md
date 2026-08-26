Extensions and helpers to manipulate .NET configuration sections.

Provides `MutableConfigurationSection` and `ImmutableConfigurationSection`, `AssemblyConfiguration`
that handles the "DefaultAssembly" and "Assemblies" sections to locate plugin types, and
`TypedConfigurationBuilder`, a small extensible framework that turns a configuration into immutable
"configured objects" - the factories of the actual strategies.

Configurations are immutable by design. Substituable placeholders provide islands of dynamic
configuration inside an otherwise stable one.

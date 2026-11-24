Extensions and helpers to manipulate .NET configuration sections and to turn them into immutable, typed "configured objects".

Main types: `MutableConfigurationSection` and `ImmutableConfigurationSection`, `AssemblyConfiguration` that handles the
"DefaultAssembly" and "Assemblies" sections to locate plugin types, and `TypedConfigurationBuilder` that turns a
configuration into immutable "configured objects".
`ISupportConfigurationPlaceholder<T>` supports substitutable placeholders in an otherwise immutable configuration. `ConfigurationTemplate` instantiates a section for each of its instances (`{Index}`, `{Name}` placeholders). `ConfigurationOptionSelector` selects the options (`IConfigurationOption`) that the section of their owner enables.

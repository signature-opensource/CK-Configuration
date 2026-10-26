Extensions and helpers to manipulate .NET configuration sections: `MutableConfigurationSection` and
`ImmutableConfigurationSection`, `AssemblyConfiguration` that handles the "DefaultAssembly" and "Assemblies" sections
to locate plugin types, and `TypedConfigurationBuilder` that turns a configuration into immutable "configured objects".
`ISupportConfigurationPlaceholder<T>` supports substitutable placeholders in an otherwise immutable configuration.

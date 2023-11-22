# Mutable & ImmutableConfigurationSection

These 2 helpers implement [IConfigurationSection](https://learn.microsoft.com/fr-fr/dotnet/api/microsoft.extensions.configuration.iconfigurationsection).

The [MutableConfigurationSection](MutableConfigurationSection.cs) can be changed and JSON configuration can easily
be merged. It acts as a builder for immutable configuration.

[ImmutableConfigurationSection](ImmutableConfigurationSection.cs) captures once for all the content and path of any
other `IConfigurationSection`.

## The path/key ambiguity.
We use `path` instead of `key` parameter name to remind you that a relative path
is always available in the conffiguration API to address sub sections.

## The non existing section issue and the "default configuration".
A configuration section may not `Exists()`: it has no value nor children.
This is weird but this is how it has been designed. We respect this behavior:
`ImmutableConfigurationSection` captures such "non existing" sections.

The `Optional` below doesn't `Exists()`:
```jsonc
{
  // We want the Options with its default values...
  "Optional": {}
}
```
Moreover, any lookup to a sub section (`c.GetSetion( "Optional" )`) creates a non existing section:
relying on `Exists()` to safely detect an "optional" section requires a more explicit approach.

We must have a way to say "I want to activate this Options with its default configuration".

We recommend to apply the following pattern:
- Allow the section to support a "true" or "false" boolean value (when it has no children).
- Opt-out: when a section must exist "by default" (of course with a sensible default configuration):
  - When the section has children, consider them as the configuration.
  - a "false" value skips the section.
  - a "true" or a non existing section applies the defaults.
- Opt-in: when a section must not exist "by default" (but still has a sensible default configuration):
  - When the section has children, consider them as the configuration.
  - a "false" value or a non existing section skips the section.
  - a "true" value applies the defaults.

The `ShouldApplyConfiguration` extension methods (in [ConfigurationSectionExtension](ConfigurationSectionExtension.cs))
implements this once for all.



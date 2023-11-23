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

## Why is there no `IConfigurationSection.ToImmutableConfigurationSection()` extension method?
Short answer: because of the immutable section's lookup parent.

Long answer is that even if this extension method seems obvious, it is not.

When an `ImmutableConfigurationSection` is created it can be provided with a "lookup parent". This hidden parent
section supports `TryLookupXXX` methods that supports an important feature: hierarchical configurations.

Hierarchical configurations is a way to keep a configuration [DRY](https://en.wikipedia.org/wiki/Don%27t_repeat_yourself) and understandable.
Since immutable configurations are immutable, this "parent" is not a true parent, the parent has no knowledge of
any child that use them as a "lookup parent": that's the reason why the lookup parent is not exposed on an immutable
configuration, only accessible by the `TryLookup` methods.

Obtaining an immutable from a `IConfigurationSection` (that can be of any type) is done by the constructor
(`lookupParent` is null by default):
```csharp
var c = new ImmutableConfigurationSection( section, lookupParent: null );
```

Let the proposed extension method be:
```csharp
/// <summary>
/// Ensures that the <see cref="IConfigurationSection"/> is a <see cref="ImmutableConfigurationSection"/>
/// or creates an <see cref="ImmutableConfigurationSection"/> from this section.
/// </summary>
/// <param name="section">This section.</param>
/// <param name="lookupParent">Optional lookup parent.</param>
/// <returns>This or a immutable section.</returns>
public static ImmutableConfigurationSection ToImmutableConfigurationSection( this IConfigurationSection section,
                                                                             ImmutableConfigurationSection? lookupParent = null )
{
    if( section is not ImmutableConfigurationSection r )
    {
        r = new ImmutableConfigurationSection( section, lookupParent );
    }
    return r;
}
```
This is obviously broken: as soon as the section is an immutable, the lookup parent will be what it is. This is
clealrly not what we want. Is the following implementation better?
```csharp
public static ImmutableConfigurationSection ToImmutableConfigurationSection( this IConfigurationSection section,
                                                                             ImmutableConfigurationSection? lookupParent = null )
{
    if( section is not ImmutableConfigurationSection r )
    {
        return new ImmutableConfigurationSection( section, lookupParent );
    }
    if( r._internalLookupParent == lookupParent )
    {
        return r;
    }
    return new ImmutableConfigurationSection( section, lookupParent );
}
```
At least, it works without surprises. But the parameter is no more a "Optional lookup parent.", it's more
"Expected or lookup parent to consider", so what's the gain over the constructor?

_In fine_, this would be exactly like exposing a mutator 
`ImmutableConfigurationSection WithLookupParent( ImmutableConfigurationSection? lookupParent )` on the `ImmutableConfigurationSection`.
and this is both useless AND dangerous: configurations' anchors are important and must not be treated "dynamically" like this.

That's why this extension method is "missing".



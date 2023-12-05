# Extensible Configuration: the Placeholder Pattern

This pattern can be supported by a configuration family but is optional. It enables a configuration
to define "extension points" at specific positions that can be "patched" with more dynamic configurations
later.

This pattern offers some strong guaranties:
- The hosting configuration itself cannot be altered in any way by the subsequent configurations.
- The placeholders are totally optionnal and are the only way to "extend" an existing configuration.
- Placeholder replacements works on fully immutable objects, there are no concurrency issues by design.
- Placeholder replacements can be constrained. For instance an extension may not be allowed to use any
  other plugin assemblies than the ones that have been defined in the context of the Placeholder, by the
  "host configuration".

The `ImmutableConfigurationSection` on which any configuration object relies is deeply immutable.
The configuration objects are also immutable.

It's obviously not the `ImmutableConfigurationSection` that can be changed. The idea is to allow
the immutable configuration objects (built upon the `ImmutableConfigurationSection`) to give
birth to modified version of themselves based on the replacement of existing Placeholder (this is the
classical pattern with immutable structures).

## Implementation
To support placeholders and configuration extension, a family:
- Must support the "empty configured object" pattern, either by implementing the [null object pattern](https://en.wikipedia.org/wiki/Null_object_pattern)
  or by simply allows created configured objects to be null.

  In this [ExtensibleStrategyConfiguration](ExtensibleStrategyConfiguration.cs) family, a null `IStrategy` is the
  "empty configured object".

- The root family type must expose a mutator. The `ExtensibleStrategyConfiguration` defines:
```csharp
  /// <summary>
  /// Mutator default implementation: always returns this instance by default.
  /// </summary>
  /// <param name="monitor">The monitor to use to signal errors.</param>
  /// <param name="configuration">Configuration of the replaced placeholder.</param>
  /// <returns>A new configuration or this instance if an error occurred or the placeholder has not been found.</returns>
  public virtual ExtensibleStrategyConfiguration SetPlaceholder( IActivityMonitor monitor, IConfigurationSection configuration )
  {
      return this;
  }
```
- A Placeholder type must exist (convention is to name it "Placeholder"). Here the [PlaceholderStrategyConfiguration](PlaceholderStrategyConfiguration.cs)
does the job:
  - It captures its own configuration AND the assemblies and the resolvers that apply where it is.
  - It is "empty": it always generate null strategies.
  - It overrides the `SetPlaceholder` to:
    - Check if the proposed new section is anchored in itself: the section parent configuration
      path must be exactly the placeholder's path.
      If the section is a "child", then:
      - It creates a new `TypedConfigurationBuilder` that uses the captured assemblies and resolvers.
      - It ensures that the section is an immutable one or creates it (anchored at the right position).
      - It creates the configured object from the section.
      - If it fails (`Create` returns null), it does nothing (by returning itsef the placeholder is kept
        unchanged).
``` csharp
public sealed class PlaceholderStrategyConfiguration : ExtensibleStrategyConfiguration
{
    readonly AssemblyConfiguration _assemblies;
    readonly ImmutableArray<TypedConfigurationBuilder.TypeResolver> _resolvers;
    readonly ImmutableConfigurationSection _configuration;

    public PlaceholderStrategyConfiguration( IActivityMonitor monitor,
                                              TypedConfigurationBuilder builder,
                                              ImmutableConfigurationSection configuration )
    {
        _assemblies = builder.AssemblyConfiguration;
        _resolvers = builder.Resolvers.ToImmutableArray();
        _configuration = configuration;
    }

    public override IStrategy? CreateStrategy( IActivityMonitor monitor ) => null;

    public override ExtensibleStrategyConfiguration SetPlaceholder( IActivityMonitor monitor,
                                                                    IConfigurationSection configuration )
    {
        if( configuration.GetParentPath().Equals( _configuration.Path, StringComparison.OrdinalIgnoreCase ) )
        {
            var builder = new TypedConfigurationBuilder( _assemblies, _resolvers );
            if( configuration is not ImmutableConfigurationSection config )
            {
                config = new ImmutableConfigurationSection( configuration, lookupParent: _configuration );
            }
            var newC = builder.Create<ExtensibleStrategyConfiguration>( monitor, config );
            if( newC != null ) return newC;
        }
        return this;
    }
}
```
- Finally, the composite must handle the mutation.
  - A mutation constructor is often useful but not required (an existing private constructor can do the job):
```csharp
  /// <summary>
  /// Private mutation constructor.
  /// </summary>
  /// <param name="source">The original composite.</param>
  /// <param name="newItems">The new items.</param>
  ExtensibleCompositeStrategyConfiguration( ExtensibleCompositeStrategyConfiguration source,
                                            ImmutableArray<ExtensibleStrategyConfiguration> newItems )
  {
      _path = source._path;
      _items = newItems;
  }
``` 
  - This composite `SetPlaceholder` is correct (alloc-free if nothing changed).
```csharp
public override ExtensibleStrategyConfiguration SetPlaceholder( IActivityMonitor monitor,
                                                                IConfigurationSection configuration )
{
    ImmutableArray<ExtensibleStrategyConfiguration>.Builder? newItems = null;
    for( int i = 0; i < _items.Count; i++ )
    {
        var item = _items[i];
        var r = item.SetPlaceholder( monitor, configuration );
        if( r != item )
        {
            if( newItems == null )
            {
                newItems = ImmutableArray.CreateBuilder<ExtensibleStrategyConfiguration>( _items.Count );
                newItems.AddRange( _items.Take( i ) );
            }
        }
        newItems?.Add( r );
    }
    return newItems != null
            ? new ExtensibleCompositeStrategyConfiguration( this, newItems.ToImmutableArray() )
            : this;
}
```
And that's it.

Recall that errors are managed by the monitor during the `Create`. To handle this once for all, a helper method
`TrySetPlaceholder` can be on the root type that handles builder errors and optionnaly signals if the section
failed to find its target placeholder.

```csharp

/// <summary>
/// Tries to replace a placeholder.
/// <para>
/// The <paramref name="configuration"/>.Path must be a direct child of the placeholder to replace.
/// </para>
/// </summary>
/// <param name="monitor">The monitor to use.</param>
/// <param name="configuration">The configuration that should replace a placeholder.</param>
/// <returns>A new configuration or null if an error occurred or the placeholder was not found.</returns>
public ExtensibleStrategyConfiguration? TrySetPlaceholder( IActivityMonitor monitor,
                                                           IConfigurationSection configuration )
{
  return TrySetPlaceholder( monitor, configuration, out var _ );
}

/// <summary>
/// Tries to replace a placeholder.
/// <para>
/// The <paramref name="configuration"/>.Path must be a direct child of the placeholder to replace.
/// </para>
/// </summary>
/// <param name="monitor">The monitor to use.</param>
/// <param name="configuration">The configuration that should replace a placeholder.</param>
/// <param name="builderError">True if an error occurred while building the configuration, false if the placeholder was not found.</param>
/// <returns>A new configuration or null if a <paramref name="builderError"/> occurred or the placeholder was not found.</returns>
public ExtensibleStrategyConfiguration? TrySetPlaceholder( IActivityMonitor monitor,
                                                           IConfigurationSection configuration,
                                                           out bool builderError )
{
    builderError = false;
    ExtensibleStrategyConfiguration? result = null;
    var buildError = false;
    using( monitor.OnError( () => buildError = true ) )
    {
        result = SetPlaceholder( monitor, configuration );
    }
    if( !buildError && result == this )
    {
        monitor.Error( $"Unable to set placeholder: '{configuration.GetParentPath()}' " +
                        $"doesn't exist or is not a placeholder." );
        return null;
    }
    return (builderError = buildError) ? null : result;
}
```    

Note that nothing prevents a substituted section to also contains placeholders. In this
case, the section name is crucial and must uniquely identify the replaced section in its
parent so that subsequent replacement paths can be properly resolved.

## Usage

The example below has one placeholder that is replaced by another _ESpimple_ strategy.
```csharp
var config = ImmutableConfigurationSection.CreateFromJson( "Root",
    """
    {
        "DefaultAssembly": "ConsumerB.Plugins",
        "Strategies":
        [
            {
                "Type": "Placeholder"
            },
            {
                "Type": "ESimple"
            }
        ]
    }
    """ );
var builder = new TypedConfigurationBuilder();
ExtensibleStrategyConfiguration.AddResolver( builder );
var sC = builder.Create<ExtensibleStrategyConfiguration>( TestHelper.Monitor, config );

Throw.DebugAssert( sC != null );
var s = sC.CreateStrategy( TestHelper.Monitor );
Throw.DebugAssert( s != null );
s.DoSomething( TestHelper.Monitor, 0 ).Should().Be( 1, "There is only one ESimple strategy." );

// Inject another one in the placeholder:
var setFirst = new MutableConfigurationSection( "Root:Strategies:0", "<Dynamic>" );
setFirst["Type"] = "ESimple";
var sC2 = sC.TrySetPlaceholder( TestHelper.Monitor, setFirst );
Throw.DebugAssert( sC2 != null );

var s2 = sC2.CreateStrategy( TestHelper.Monitor );
Throw.DebugAssert( s2 != null );
s2.DoSomething( TestHelper.Monitor, 0 ).Should().Be( 2, "sC2 has now two ESimple strategies." );

```





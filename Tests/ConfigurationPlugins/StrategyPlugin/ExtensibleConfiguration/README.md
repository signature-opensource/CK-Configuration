# Extensible Configuration

## Implementation
To support placeholders and configuration extension, a family must:
- Support the "empty configured object" pattern, either by implementing the [null object pattern](https://en.wikipedia.org/wiki/Null_object_pattern)
  or by simply allows created configured objects to be null.

  In this Strategy family, a null `IStrategy` is the empty configured object.

- The root family type must expose a mutator. The `ExtensibleStrategyConfiguration` defines:
```csharp
  /// <summary>
  /// Mutator default implementation: always returns this instance by default.
  /// </summary>
  /// <param name="monitor">The monitor to use to signal errors.</param>
  /// <param name="configuration">Configuration of the replaced placeholder.</param>
  /// <returns>This, a new configuration, or null to remove this.</returns>
  public virtual ExtensibleStrategyConfiguration? SetPlaceholder( IActivityMonitor monitor, IConfigurationSection configuration )
  {
      return this;
  }
```
  Here the mutator can return `null`. This is actually not required: this is the standard way to handle
  removal of an item but in our case, we only want to replace a placeholder by an existing section.
  We won't use this null return but we keep the code with this feature.

- Define a Placeholder type. Here the [PlaceholderStrategyConfiguration](PlaceholderStrategyConfiguration.cs)
does the job:
  - It captures its own configuration AND the Assemblies configuration that applies where it is.
  - It is "empty": it always generate null strategies.
``` csharp
public sealed class PlaceholderStrategyConfiguration : ExtensibleStrategyConfiguration
{
    readonly AssemblyConfiguration _assemblies;
    readonly ImmutableConfigurationSection _configuration;

    public PlaceholderStrategyConfiguration( IActivityMonitor monitor,
                                              PolymorphicConfigurationTypeBuilder builder,
                                              ImmutableConfigurationSection configuration )
    {
        _assemblies = builder.AssemblyConfiguration;
        _configuration = configuration;
    }

    /// <summary>
    /// A placeholder obviously generates no strategy.
    /// </summary>
    public override IStrategy? CreateStrategy( IActivityMonitor monitor ) => null;
```

  - It overrides the `SetPlaceholder` to:
    - Check if the proposed new section is anchored in itself: the section parent configuration
    path must be exactly the placeholder's path (the section name - the key - doesn't matter,
    we'll use `<Dynamic>` for it).
    If the section is a "child", then:
    - It creates a new `PolymorphicConfigurationTypeBuilder` that will use the captured assemblies.
    - It adds the type resolver for its family to the builder.
    - It ensures that the section is an immutable one or creates it (anchored at the right position).
    - It creates the configured object from the section.
    - If it fails (`Create` returns null), it does nothing: by returning itsef the placeholder is kept
      here. (This where we may remove it by returning null but this makes no sense here.)
    - Otherwise, it simply returns the new configured object that will replace it.
``` csharp
public override ExtensibleStrategyConfiguration? SetPlaceholder( IActivityMonitor monitor,
                                                                 IConfigurationSection configuration )
{
    if( configuration.GetParentPath().Equals( _configuration.Path, StringComparison.OrdinalIgnoreCase ) )
    {
        var builder = new PolymorphicConfigurationTypeBuilder( _assemblies );
        ExtensibleStrategyConfiguration.AddResolver( builder );
        if( configuration is not ImmutableConfigurationSection config )
        {
            config = new ImmutableConfigurationSection( configuration, lookupParent: _configuration );
        }
        var newC = builder.Create<ExtensibleStrategyConfiguration>( monitor, config );
        if( newC != null ) return newC;
    }
    return this;
}
```
- Finaly, the `SetPlaceholder` mutator for the composite must be implemented.
  This implementation is correct (alloc-free if nothing changed). It handles removal
  (null return from `item.SetPlaceholder( monitor, configuration )`) but as we said
  this is not required.
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
        if( r != null && newItems != null ) newItems.Add( r );
    }
    return newItems != null
            ? new ExtensibleCompositeStrategyConfiguration( this, newItems.ToImmutableArray() )
            : this;
}
```
And that's it.

Recall that errors are managed by the monitor during the `Create`. To handle this once for all, a helper method
like the one below can be on the root type that also signals an error if the section failed to find its target
placeholder:
```csharp
public bool TrySetPlaceholder( IActivityMonitor monitor,
                                IConfigurationSection configuration,
                                out ExtensibleStrategyConfiguration? result )
{
    bool success = true;
    using( monitor.OnError( () => success = false ) )
    {
        result = SetPlaceholder( monitor, configuration );
        if( result == this )
        {
            monitor.Error( $"Unable to set placeholder: '{configuration.GetParentPath()}' " +
                            $"doesn't exist or is not a placeholder." );
        }
    }
    if( !success ) result = null;
    return success;
}
```    

Note that nothing prevents a substituted section to also contains placeholders.

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
var builder = new PolymorphicConfigurationTypeBuilder();
ExtensibleStrategyConfiguration.AddResolver( builder );
var sC = builder.Create<ExtensibleStrategyConfiguration>( TestHelper.Monitor, config );

Throw.DebugAssert( sC != null );
var s = sC.CreateStrategy( TestHelper.Monitor );
Throw.DebugAssert( s != null );
s.DoSomething( TestHelper.Monitor, 0 ).Should().Be( 1, "There is only one ESimple strategy." );

// Inject another one in the placeholder:
var setFirst = new MutableConfigurationSection( "Root:Strategies:0", "<Dynamic>" );
setFirst["Type"] = "ESimple";
sC.TrySetPlaceholder( TestHelper.Monitor, setFirst, out var sC2 ).Should().BeTrue();

Throw.DebugAssert( sC2 != null );
var s2 = sC2.CreateStrategy( TestHelper.Monitor );
Throw.DebugAssert( s2 != null );
s2.DoSomething( TestHelper.Monitor, 0 ).Should().Be( 2, "sC2 has now two ESimple strategies." );

```





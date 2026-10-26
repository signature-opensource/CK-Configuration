# Extensible Configuration: a Placeholder Pattern example

The `ExtensibleStrategyConfiguration` family implements the placeholder pattern described in
[CK.Configuration](../../../../CK.Configuration/README.md#configuration-patching-the-substitutable-placeholder):
a configuration can define "extension points" at specific positions that can be "patched" with more dynamic
configurations later.

## Implementation
- The "empty configured object": in this [ExtensibleStrategyConfiguration](ExtensibleStrategyConfiguration.cs)
  family, a null `IStrategy` is the "empty configured object".

- The root family type implements `ISupportConfigurationPlaceholder<ExtensibleStrategyConfiguration>` and exposes
  the virtual mutator:
```csharp
    /// <summary>
    /// Mutator default implementation: always returns this instance by default.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="configuration">Configuration of the replaced placeholder.</param>
    /// <returns>A new configuration (or this object if nothing changed). Should be null only if an error occurred.</returns>
    public virtual ExtensibleStrategyConfiguration? SetPlaceholder( IActivityMonitor monitor, IConfigurationSection configuration )
    {
        return this;
    }
```
- The [PlaceholderStrategyConfiguration](PlaceholderStrategyConfiguration.cs) is the Placeholder type:
  - It captures its own configuration AND the assemblies and the resolvers that apply where it is.
  - It is "empty": it always generates null strategies.
  - It overrides the `SetPlaceholder` to:
    - Check if the proposed new section is anchored in itself: the section parent configuration
      path must be exactly the placeholder's path.
      If the section is a "child", then:
      - It creates a new `TypedConfigurationBuilder` that uses the captured assemblies and resolvers.
      - It ensures that the section is an immutable one or creates it (anchored at the right position).
      - It creates the configured object from the section.
      - If it fails (`builder.Create` returns null), it returns null.
```csharp
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

    /// <summary>
    /// A placeholder obviously generates no strategy.
    /// </summary>
    public override IStrategy? CreateStrategy( IActivityMonitor monitor ) => null;


    public override ExtensibleStrategyConfiguration? SetPlaceholder( IActivityMonitor monitor,
                                                                    IConfigurationSection configuration )
    {
        if( configuration.GetParentPath().Equals( _configuration.Path, StringComparison.OrdinalIgnoreCase ) )
        {
            var builder = new TypedConfigurationBuilder( _assemblies, _resolvers );
            if( configuration is not ImmutableConfigurationSection config )
            {
                config = new ImmutableConfigurationSection( configuration, lookupParent: _configuration );
            }
            return builder.Create<ExtensibleStrategyConfiguration>( monitor, config );
        }
        return this;
    }
}
```
- Finally, the [composite](ExtensibleCompositeStrategyConfiguration.cs) handles the mutation.
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
  - This composite `SetPlaceholder` is alloc-free if nothing changed:
```csharp
    public override ExtensibleStrategyConfiguration? SetPlaceholder( IActivityMonitor monitor,
                                                                     IConfigurationSection configuration )
    {
        Throw.CheckNotNullArgument( monitor );
        Throw.CheckNotNullArgument( configuration );
        // Bails out early if we are not concerned.
        if( !ConfigurationSectionExtension.IsChildPath( _path, configuration.Path ) )
        {
            return this;
        }
        ImmutableArray<ExtensibleStrategyConfiguration>.Builder? newItems = null;
        for( int i = 0; i < _items.Count; i++ )
        {
            var item = _items[i];
            var r = item.SetPlaceholder( monitor, configuration );
            if( r == null ) return null;
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
        return newItems != null ? new ExtensibleCompositeStrategyConfiguration( this, newItems.ToImmutableArray() ) : this;
    }
```
And that's it.

These mutators don't check that an error has been logged or that a placeholder has actually been replaced:
this is done once, at the root of the substitution, by the `TrySetPlaceholder` extension methods that
[`ISupportConfigurationPlaceholder<T>`](../../../../CK.Configuration/SupportConfigurationPlaceholder.cs)
brings (see [Replacing a placeholder](../../../../CK.Configuration/README.md#replacing-a-placeholder)).

## Usage

The example below, from [ConfigurationWithPlaceholderTests](../../../CK.Configuration.Tests/ConfigurationWithPlaceholderTests.cs),
has three placeholders (one of them in a composite) that are replaced by other `ESimple` strategies.
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
            },
            {
                "Type": "Placeholder"
            },
            {
                "Type": "ExtensibleComposite",
                "Strategies": [
                    {
                        "Type": "ESimple"
                    },
                    {
                        "Type": "Placeholder"
                    },
                    {
                        "Type": "ESimple"
                    }
                ]
            },
            {
                "Type": "Placeholder"
            }
        ]
    }
    """ );
var builder = new TypedConfigurationBuilder();
ExtensibleStrategyConfiguration.AddResolver( builder );
var sC = builder.Create<ExtensibleStrategyConfiguration>( TestHelper.Monitor, config );
sC = CheckNotNullAndRun( sC, 3 );

var setFirst = new MutableConfigurationSection( "Root:Strategies:0", "<Dynamic>" );
setFirst["Type"] = "ESimple";

var sC2 = sC.TrySetPlaceholder( TestHelper.Monitor, setFirst, out var _ );
sC2 = CheckNotNullAndRun( sC2, 4 );

var setInComposite = new MutableConfigurationSection( "Root:Strategies:3:Strategies:1", "<Dynamic>" );
setInComposite["Type"] = "ESimple";
var sC3 = sC2.TrySetPlaceholder( TestHelper.Monitor, setInComposite, out var _ );
CheckNotNullAndRun( sC3, 5 );

// From sC.
var sC2bis = sC.TrySetPlaceholder( TestHelper.Monitor, setInComposite, out var _ );
CheckNotNullAndRun( sC2bis, 4 );

static ExtensibleStrategyConfiguration CheckNotNullAndRun( ExtensibleStrategyConfiguration? sC, int expected )
{
    Throw.DebugAssert( sC != null );
    var s = sC.CreateStrategy( TestHelper.Monitor );
    Throw.DebugAssert( s != null );
    s.DoSomething( TestHelper.Monitor, 0 ).ShouldBe( expected );
    return sC;
}
```

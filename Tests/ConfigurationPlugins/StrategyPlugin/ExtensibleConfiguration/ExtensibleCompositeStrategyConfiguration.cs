using CK.Core;
using Microsoft.Extensions.Configuration;
using StrategyPlugin;
using System.Collections.Immutable;

namespace Plugin.Strategy;

/// <summary>
/// Extensible composite configuration.
/// <para>
/// This must be in the "plugin" namespace so that it can be found by the resolver.
/// </para>
/// </summary>
public class ExtensibleCompositeStrategyConfiguration : ExtensibleStrategyConfiguration
{
    readonly string _path;
    readonly IReadOnlyList<ExtensibleStrategyConfiguration> _items;

    /// <summary>
    /// Required constructor.
    /// </summary>
    /// <param name="monitor">The monitor that must be used to signal errors and warnings.</param>
    /// <param name="builder">The builder. Can be used to instantiate other children as needed.</param>
    /// <param name="configuration">The configuration for this object.</param>
    /// <param name="strategies">The subordinated items.</param>
    public ExtensibleCompositeStrategyConfiguration( IActivityMonitor monitor,
                                                     TypedConfigurationBuilder builder,
                                                     ImmutableConfigurationSection configuration,
                                                     IReadOnlyList<ExtensibleStrategyConfiguration> strategies )
    {
        _path = configuration.Path;
        _items = strategies;
    }

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

    /// <summary>
    /// Typical mutator implementation that is alloc-free when nothing changes and simply returns this
    /// instance unchanged.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="configuration">Configuration to apply.</param>
    /// <returns>This or a new composite. May be null if an error occurred.</returns>
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

    /// <summary>
    /// Making this virtual enables any specialization.
    /// </summary>
    /// <param name="monitor"></param>
    /// <returns></returns>
    public override IStrategy? CreateStrategy( IActivityMonitor monitor )
    {
        var items = _items
                        .Select( c => c.CreateStrategy( monitor ) )
                        .Where( s => s != null )
                        .ToImmutableArray();
        return new CompositeStrategy( _path, items! );
    }
}

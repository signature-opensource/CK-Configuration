using CK.Core;
using Microsoft.Extensions.Configuration;
using System.Collections.Immutable;

namespace StrategyPlugin
{
    /// <summary>
    /// Typical composite implementation. This shoule be implemented at the abstraction level,
    /// close to its base <see cref="IStrategyConfiguration"/>.
    /// <para>
    /// This class is not be abstract: this is the default composite configuration that has no
    /// configuration for itself but nothing prevents other configurations to be more complex
    /// than a simple list of items.
    /// </para>
    /// </summary>
    public class CompositeStrategyConfiguration : IStrategyConfiguration
    {
        private readonly IReadOnlyList<IStrategyConfiguration> _items;

        /// <summary>
        /// The constructor of a composite accepts a read only list of base interfaces for its
        /// items. The other parameters are the same as base pattern.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors and warnings.</param>
        /// <param name="builder">The builder. Can be used to instantiate other children as needed.</param>
        /// <param name="configuration">The configuration for this object.</param>
        /// <param name="strategies">The subordinated items.</param>
        public CompositeStrategyConfiguration( IActivityMonitor monitor,
                                               PolymorphicConfigurationTypeBuilder builder,
                                               ImmutableConfigurationSection configuration,
                                               IReadOnlyList<IStrategyConfiguration> strategies )
        {
            Configuration = configuration;
            _items = strategies;
        }

        /// <inheritdoc />
        public ImmutableConfigurationSection Configuration { get; }

        /// <summary>
        /// Just like <see cref="Configuration"/>, exposing this is totally optional.
        /// </summary>
        public IReadOnlyList<IStrategyConfiguration> Items => _items;

        /// <inheritdoc />
        public virtual IStrategy? CreateStrategy( IActivityMonitor monitor )
        {
            var items = CreateStrategyItems( monitor );
            return items.Length > 0
                    ? new CompositeStrategy( Configuration.Path, items! )
                    : null;
        }

        protected ImmutableArray<IStrategy> CreateStrategyItems( IActivityMonitor monitor )
        {
            return _items.Select( c => c.CreateStrategy( monitor ) )
                         .Where( s => s != null )
                         .ToImmutableArray()!;
        }
    }
}

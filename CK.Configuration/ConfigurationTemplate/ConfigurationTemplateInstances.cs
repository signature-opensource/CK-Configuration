using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CK.Core;

/// <summary>
/// The configured instances of a template (see <see cref="ConfigurationTemplate.TryReadInstances"/>) and, when the
/// template is Opened (it has a <see cref="MaxInstanceCount"/>), the bound of its dynamic instances: the dynamic
/// instances are numbered after the configured ones, up to <see cref="MaxInstanceCount"/>, and named by their index.
/// </summary>
public sealed class ConfigurationTemplateInstances : IReadOnlyList<ConfigurationInstance>
{
    readonly IReadOnlyList<ConfigurationInstance> _configured;
    readonly string? _templateName;
    readonly int? _maxInstanceCount;

    internal ConfigurationTemplateInstances( IReadOnlyList<ConfigurationInstance> configured, string? templateName, int? maxInstanceCount )
    {
        _configured = configured;
        _templateName = templateName;
        _maxInstanceCount = maxInstanceCount;
    }

    /// <summary>
    /// Gets the name of the template (the qualifier of its placeholders), or null.
    /// </summary>
    public string? TemplateName => _templateName;

    /// <summary>
    /// Gets the maximal number of instances of an Opened template (the configured ones included), null for a Closed one.
    /// </summary>
    public int? MaxInstanceCount => _maxInstanceCount;

    /// <summary>
    /// Gets whether dynamic instances may exist: the template has a <see cref="MaxInstanceCount"/>.
    /// </summary>
    public bool IsOpened => _maxInstanceCount.HasValue;

    /// <summary>
    /// Gets whether an index is the one of a dynamic instance: above the configured ones, at most <see cref="MaxInstanceCount"/>.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>True for a dynamic index.</returns>
    public bool IsDynamicIndex( int index ) => index > _configured.Count && index <= _maxInstanceCount;

    /// <summary>
    /// Creates a dynamic instance: its name is its index.
    /// </summary>
    /// <param name="index">The index. It must satisfy <see cref="IsDynamicIndex(int)"/>.</param>
    /// <returns>The instance.</returns>
    public ConfigurationInstance CreateDynamic( int index )
    {
        Throw.CheckArgument( IsDynamicIndex( index ) );
        return new ConfigurationInstance( _templateName, index, index.ToString( CultureInfo.InvariantCulture ) );
    }

    /// <summary>
    /// Finds a configured instance by its name (case insensitive) or its index, or creates a dynamic one from its index.
    /// A configured name never starts with a digit: it can't be the name of a dynamic instance.
    /// </summary>
    /// <param name="key">The name or the index.</param>
    /// <returns>The instance or null.</returns>
    public ConfigurationInstance? Find( string key )
    {
        Throw.CheckNotNullArgument( key );
        key = key.Trim();
        var found = _configured.FirstOrDefault( i => i.Name.Equals( key, StringComparison.OrdinalIgnoreCase ) );
        if( found == null && int.TryParse( key, NumberStyles.None, CultureInfo.InvariantCulture, out var index ) )
        {
            found = index >= 1 && index <= _configured.Count
                    ? _configured[index - 1]
                    : IsDynamicIndex( index ) ? CreateDynamic( index ) : null;
        }
        return found;
    }

    /// <summary>
    /// Finds the dynamic instance whose resolved <paramref name="pattern"/> is <paramref name="value"/> (case insensitive):
    /// <c>"$Worker-3"</c> is the instance 3 of <c>"$Worker-{Name}"</c> (a dynamic instance is named by its index), as of
    /// <c>"$Worker-{Index}"</c>, and <c>"tcp:host:5403"</c> of <c>"tcp:host:{5400+Index}"</c>.
    /// The pattern must have at least one unqualified placeholder and no qualified one (the enclosing templates are resolved).
    /// </summary>
    /// <param name="pattern">The value of the template, with its placeholders.</param>
    /// <param name="value">The resolved value.</param>
    /// <returns>The dynamic instance or null.</returns>
    public ConfigurationInstance? MatchDynamic( string pattern, string value )
    {
        Throw.CheckNotNullArgument( pattern );
        Throw.CheckNotNullArgument( value );
        if( !IsOpened || !ConfigurationTemplate.TryMatchIndex( pattern, value, out var index ) || !IsDynamicIndex( index ) ) return null;
        return CreateDynamic( index );
    }

    /// <summary>
    /// Gets the number of configured instances.
    /// </summary>
    public int Count => _configured.Count;

    /// <summary>
    /// Gets a configured instance.
    /// </summary>
    /// <param name="index">The 0-based position (the instance's <see cref="ConfigurationInstance.Index"/> - 1).</param>
    /// <returns>The configured instance.</returns>
    public ConfigurationInstance this[int index] => _configured[index];

    /// <inheritdoc />
    public IEnumerator<ConfigurationInstance> GetEnumerator() => _configured.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

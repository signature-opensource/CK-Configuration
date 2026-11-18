namespace CK.Core;

/// <summary>
/// An instance of a configuration template (see <see cref="ConfigurationTemplate"/>): its 1-based <see cref="Index"/>
/// and its <see cref="Name"/> (the item of "Instances", or the index as a string with "InstanceCount").
/// </summary>
public sealed class ConfigurationInstance
{
    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="templateName">The name of the template ("Trolley"), used by the qualified placeholders. Null for a template that can't be qualified.</param>
    /// <param name="index">The 1-based index.</param>
    /// <param name="name">The name.</param>
    public ConfigurationInstance( string? templateName, int index, string name )
    {
        Throw.CheckArgument( index >= 1 );
        Throw.CheckNotNullOrWhiteSpaceArgument( name );
        TemplateName = templateName;
        Index = index;
        Name = name;
    }

    /// <summary>
    /// Gets the name of the template, the qualifier of its placeholders in an inner template (<c>{Trolley.Index}</c>).
    /// Null when the template can't be qualified.
    /// </summary>
    public string? TemplateName { get; }

    /// <summary>
    /// Gets the 1-based index of this instance.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the name of this instance.
    /// </summary>
    public string Name { get; }

    /// <inheritdoc />
    public override string ToString() => TemplateName != null ? $"{TemplateName} instance '{Name}' (#{Index})" : $"Instance '{Name}' (#{Index})";
}

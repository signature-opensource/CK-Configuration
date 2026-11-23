using System.Collections.Generic;

namespace CK.Core;

/// <summary>
/// An option that the configuration section of its owner switches: its key in that section is its <see cref="Name"/>.
/// <see cref="ConfigurationOptionSelector"/> selects the enabled options of an owner.
/// </summary>
public interface IConfigurationOption
{
    /// <summary>
    /// Gets the option name: its key in the configuration section of its owner (compared case insensitively).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets whether this option is enabled when the configuration of its owner doesn't say (an opt-out option). False
    /// for an opt-in option, enabled only when the configuration says so.
    /// </summary>
    bool IsEnabledByDefault { get; }

    /// <summary>
    /// Gets whether this option is always enabled: the configuration can't turn it off. A required option is enabled by
    /// default.
    /// </summary>
    bool IsRequired { get; }

    /// <summary>
    /// Gets the names of the options without which this one cannot be enabled.
    /// </summary>
    IReadOnlyList<string> Requires { get; }
}

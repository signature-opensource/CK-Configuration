using System;
using System.Collections.Generic;

namespace CK.Core;

public static partial class ConfigurationSectionExtension
{
    /// <summary>
    /// The name of the sections that hold the default values of the sections below them: "Defaults".
    /// </summary>
    public const string DefaultsKey = "Defaults";

    /// <summary>
    /// Tries to resolve a section: the <paramref name="path"/> in this section, otherwise in the <see cref="DefaultsKey"/>
    /// section of the nearest ancestor that has it. The keys of the ancestors themselves are never used: only their
    /// explicit "Defaults".
    /// <para>
    /// The "Defaults" child of this section is not considered (it holds the defaults of the sections below this one), and
    /// neither is a "Defaults" section that contains this one.
    /// </para>
    /// </summary>
    /// <param name="s">This section.</param>
    /// <param name="path">The configuration key or a path to a subordinated key.</param>
    /// <returns>The non null section if found.</returns>
    public static ImmutableConfigurationSection? TryResolveSection( this ImmutableConfigurationSection s, string path ) => TryResolveSection( s, path, out _ );

    /// <summary>
    /// Tries to resolve a section: the <paramref name="path"/> in this section, otherwise in the <see cref="DefaultsKey"/>
    /// section of the nearest ancestor that has it. See <see cref="TryResolveSection(ImmutableConfigurationSection, string)"/>.
    /// </summary>
    /// <param name="s">This section.</param>
    /// <param name="path">The configuration key or a path to a subordinated key.</param>
    /// <param name="distance">
    /// Outputs 0 when found in this section, otherwise the distance from this section to the ancestor whose "Defaults" has it.
    /// </param>
    /// <returns>The non null section if found.</returns>
    public static ImmutableConfigurationSection? TryResolveSection( this ImmutableConfigurationSection s, string path, out int distance )
    {
        MutableConfigurationSection.CheckPathArgument( path );
        distance = 0;
        var result = s.TryGetSection( path );
        if( result != null ) return result;
        // The "Defaults" of the ancestors, from the root down: the last one that has the path is the nearest.
        foreach( var (defaults, d) in s.LookupAllSectionWithDistance( DefaultsKey ) )
        {
            if( d > 0 && defaults.TryGetSection( path ) is { } found )
            {
                result = found;
                distance = d;
            }
        }
        return result;
    }

    /// <summary>
    /// Tries to resolve a value: see <see cref="TryResolveSection(ImmutableConfigurationSection, string)"/>.
    /// If the resolved section has no value (because it has children), this returns null.
    /// </summary>
    /// <param name="s">This section.</param>
    /// <param name="path">The configuration key or a path to a subordinated key.</param>
    /// <returns>The non null value if found.</returns>
    public static string? TryResolveValue( this ImmutableConfigurationSection s, string path ) => TryResolveSection( s, path, out _ )?.Value;

    /// <summary>
    /// Tries to resolve a value: see <see cref="TryResolveSection(ImmutableConfigurationSection, string, out int)"/>.
    /// If the resolved section has no value (because it has children), this returns null.
    /// </summary>
    /// <param name="s">This section.</param>
    /// <param name="path">The configuration key or a path to a subordinated key.</param>
    /// <param name="distance">
    /// Outputs 0 when found in this section, otherwise the distance from this section to the ancestor whose "Defaults" has it.
    /// </param>
    /// <returns>The non null value if found.</returns>
    public static string? TryResolveValue( this ImmutableConfigurationSection s, string path, out int distance ) => TryResolveSection( s, path, out distance )?.Value;

    /// <summary>
    /// Enumerates the <paramref name="path"/> sections of the <see cref="DefaultsKey"/> sections of the ancestors, from the
    /// root down, and finally the one of this section (at a 0 distance) if it exists. This is the resolution of the values
    /// that combine (like a set of allowed or disallowed items) instead of replacing each other.
    /// </summary>
    /// <param name="s">This section.</param>
    /// <param name="path">The configuration key or a path to a subordinated key.</param>
    /// <returns>The sections with their distance (the distance from this section to the ancestor whose "Defaults" has it).</returns>
    public static IEnumerable<(ImmutableConfigurationSection Section, int Distance)> ResolveAllSectionWithDistance( this ImmutableConfigurationSection s, string path )
    {
        MutableConfigurationSection.CheckPathArgument( path );
        foreach( var (defaults, d) in s.LookupAllSectionWithDistance( DefaultsKey ) )
        {
            if( d > 0 && defaults.TryGetSection( path ) is { } found ) yield return (found, d);
        }
        if( s.TryGetSection( path ) is { } own ) yield return (own, 0);
    }

    /// <summary>
    /// Enumerates the <paramref name="path"/> sections of the <see cref="DefaultsKey"/> sections of the ancestors, from the
    /// root down, and finally the one of this section if it exists.
    /// See <see cref="ResolveAllSectionWithDistance(ImmutableConfigurationSection, string)"/>.
    /// </summary>
    /// <param name="s">This section.</param>
    /// <param name="path">The configuration key or a path to a subordinated key.</param>
    /// <returns>The sections.</returns>
    public static IEnumerable<ImmutableConfigurationSection> ResolveAllSection( this ImmutableConfigurationSection s, string path )
    {
        foreach( var (section, _) in ResolveAllSectionWithDistance( s, path ) ) yield return section;
    }

    /// <summary>
    /// Resolves a "true"/"false" value in this section or in the "Defaults" sections above.
    /// <para>
    /// This never throws: if the value exists and cannot be parsed, emits a log warning
    /// and returns null.
    /// </para>
    /// </summary>
    /// <param name="s">This section.</param>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="path">The configuration key or a path.</param>
    /// <returns>The value or null.</returns>
    public static bool? TryResolveBooleanValue( this ImmutableConfigurationSection s,
                                               IActivityMonitor monitor,
                                               string path )
    {
        return TryReadBoolean( s, monitor, path, s.TryResolveValue( path ) );
    }

    /// <summary>
    /// Resolves an integer value in this section or in the "Defaults" sections above.
    /// <para>
    /// This never throws: if the value exists and cannot be parsed or falls outside
    /// of the <paramref name="min"/>-<paramref name="max"/> range, emits a log warning
    /// and returns null.
    /// </para>
    /// </summary>
    /// <param name="s">This section.</param>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="path">The configuration key or a path.</param>
    /// <param name="min">The minimal value to accept.</param>
    /// <param name="max">The maximal value to accept.</param>
    /// <returns>The value or null.</returns>
    public static int? TryResolveIntValue( this ImmutableConfigurationSection s,
                                          IActivityMonitor monitor,
                                          string path,
                                          int min = 0,
                                          int max = int.MaxValue )
    {
        return TryReadInt( s, monitor, path, min, max, s.TryResolveValue( path ) );
    }

    /// <summary>
    /// Resolves a <see cref="TimeSpan"/> value in this section or in the "Defaults" sections above.
    /// <para>
    /// This never throws: if the value exists and cannot be parsed or falls outside
    /// of the <paramref name="min"/>-<paramref name="max"/> range, emits a log warning
    /// and returns null.
    /// </para>
    /// </summary>
    /// <param name="s">This section.</param>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="path">The configuration key or a path.</param>
    /// <param name="min">The minimal value to accept.</param>
    /// <param name="max">The maximal value to accept.</param>
    /// <returns>The value or null.</returns>
    public static TimeSpan? TryResolveTimeSpanValue( this ImmutableConfigurationSection s,
                                                    IActivityMonitor monitor,
                                                    string path,
                                                    TimeSpan min,
                                                    TimeSpan max )
    {
        return TryReadTimeSpan( s, monitor, path, min, max, s.TryResolveValue( path ) );
    }

    /// <summary>
    /// Resolves a <see cref="DateTime"/> value in this section or in the "Defaults" sections above.
    /// <para>
    /// This never throws: if the value exists and cannot be parsed or falls outside
    /// of the <paramref name="min"/>-<paramref name="max"/> range, emits a log warning
    /// and returns null.
    /// </para>
    /// </summary>
    /// <param name="s">This section.</param>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="path">The configuration key or a path.</param>
    /// <param name="min">The minimal value to accept.</param>
    /// <param name="max">The maximal value to accept.</param>
    /// <returns>The value or null.</returns>
    public static DateTime? TryResolveDateTimeValue( this ImmutableConfigurationSection s,
                                                    IActivityMonitor monitor,
                                                    string path,
                                                    DateTime min,
                                                    DateTime max )
    {
        return TryReadDateTime( s, monitor, path, min, max, s.TryResolveValue( path ) );
    }

    /// <summary>
    /// Resolves a floating number value in this section or in the "Defaults" sections above.
    /// <para>
    /// This never throws: if the value exists and cannot be parsed or falls outside
    /// of the <paramref name="min"/>-<paramref name="max"/> range, emits a log warning
    /// and returns null.
    /// </para>
    /// </summary>
    /// <param name="s">This section.</param>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="path">The configuration key or a path.</param>
    /// <param name="min">The minimal value to accept.</param>
    /// <param name="max">The maximal value to accept.</param>
    /// <returns>The value or null.</returns>
    public static double? TryResolveDoubleValue( this ImmutableConfigurationSection s,
                                                IActivityMonitor monitor,
                                                string path,
                                                double min = 0.0,
                                                double max = double.MaxValue )
    {
        return TryReadDouble( s, monitor, path, min, max, s.TryResolveValue( path ) );
    }

    /// <summary>
    /// Resolves an enum value in this section or in the "Defaults" sections above.
    /// <para>
    /// This never throws: if the value exists and cannot be parsed, emits a log warning
    /// and returns null.
    /// </para>
    /// </summary>
    /// <param name="s">This section.</param>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="path">The configuration key or a path.</param>
    /// <returns>The value or null.</returns>
    public static T? TryResolveEnumValue<T>( this ImmutableConfigurationSection s,
                                            IActivityMonitor monitor,
                                            string path )
        where T : struct, Enum
    {
        return TryReadEnum<T>( s, monitor, path, s.TryResolveValue( path ) );
    }

    /// <summary>
    /// Resolves a "true"/"false" (case insensitive) boolean value in this section or in the "Defaults" sections above.
    /// <para>
    /// If the value exists and cannot be parsed, emits a log warning and returns the <paramref name="defaultValue"/>.
    /// </para>
    /// </summary>
    /// <param name="s">This section.</param>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="path">The configuration key or a path.</param>
    /// <param name="defaultValue">Returned default value.</param>
    /// <returns>The value.</returns>
    public static bool ResolveBooleanValue( this ImmutableConfigurationSection s,
                                            IActivityMonitor monitor,
                                            string path,
                                            bool defaultValue = false )
    {
        var a = s.TryResolveValue( path );
        if( a == null ) return defaultValue;
        if( !bool.TryParse( a, out var value ) )
        {
            return WarnWithDefault( s, monitor, path, "'true' or 'false'", defaultValue, a );
        }
        return value;
    }

    /// <summary>
    /// Resolves an integer value in this section or in the "Defaults" sections above.
    /// <para>
    /// This never throws: if the value exists and cannot be parsed, emits a log warning
    /// and returns the <paramref name="defaultValue"/>.
    /// </para>
    /// </summary>
    /// <param name="s">This section.</param>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="path">The configuration key or a path.</param>
    /// <param name="defaultValue">Returned default value.</param>
    /// <returns>The value.</returns>
    public static int ResolveIntValue( this ImmutableConfigurationSection s,
                                      IActivityMonitor monitor,
                                      string path,
                                      int defaultValue = 0 )
    {
        var a = s.TryResolveValue( path );
        if( a == null ) return defaultValue;
        if( !int.TryParse( a, out var value ) )
        {
            return WarnWithDefault( s, monitor, path, "an integer", defaultValue, a );
        }
        return value;
    }

    /// <summary>
    /// Resolves a <see cref="TimeSpan"/> value in this section or in the "Defaults" sections above.
    /// <para>
    /// This never throws: if the value exists and cannot be parsed, emits a log warning
    /// and returns the <paramref name="defaultValue"/>.
    /// </para>
    /// </summary>
    /// <param name="s">This section.</param>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="path">The configuration key or a path.</param>
    /// <param name="defaultValue">Returned default value.</param>
    /// <returns>The value.</returns>
    public static TimeSpan ResolveTimeSpanValue( this ImmutableConfigurationSection s,
                                                IActivityMonitor monitor,
                                                string path,
                                                TimeSpan defaultValue )
    {
        var a = s.TryResolveValue( path );
        if( a == null ) return defaultValue;
        if( !TimeSpan.TryParse( a, out var value ) )
        {
            return WarnWithDefault( s, monitor, path, "a TimeSpan", defaultValue, a );
        }
        return value;
    }

    /// <summary>
    /// Resolves a <see cref="DateTime"/> value in this section or in the "Defaults" sections above.
    /// <para>
    /// This never throws: if the value exists and cannot be parsed, emits a log warning
    /// and returns the <paramref name="defaultValue"/>.
    /// </para>
    /// </summary>
    /// <param name="s">This section.</param>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="path">The configuration key or a path.</param>
    /// <param name="defaultValue">Returned default value.</param>
    /// <returns>The value.</returns>
    public static DateTime ResolveDateTimeValue( this ImmutableConfigurationSection s,
                                                IActivityMonitor monitor,
                                                string path,
                                                DateTime defaultValue )
    {
        var a = s.TryResolveValue( path );
        if( a == null ) return defaultValue;
        if( !DateTime.TryParse( a, out var value ) )
        {
            return WarnWithDefault( s, monitor, path, "a DateTime", defaultValue, a );
        }
        return value;
    }

    /// <summary>
    /// Resolves a float value in this section or in the "Defaults" sections above.
    /// <para>
    /// This never throws: if the value exists and cannot be parsed, emits a log warning
    /// and returns the <paramref name="defaultValue"/>.
    /// </para>
    /// </summary>
    /// <param name="s">This section.</param>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="path">The configuration key or a path.</param>
    /// <param name="defaultValue">Returned default value.</param>
    /// <returns>The value.</returns>
    public static double ResolveDoubleValue( this ImmutableConfigurationSection s,
                                            IActivityMonitor monitor,
                                            string path,
                                            double defaultValue = 0.0 )
    {
        var a = s.TryResolveValue( path );
        if( a == null ) return defaultValue;
        if( !double.TryParse( a, out var value ) )
        {
            return WarnWithDefault( s, monitor, path, "a float number", defaultValue, a );
        }
        return value;
    }

    /// <summary>
    /// Resolves an enum value in this section or in the "Defaults" sections above.
    /// <para>
    /// This never throws: if the value exists and cannot be parsed, emits a log warning
    /// and returns the <paramref name="defaultValue"/>.
    /// </para>
    /// </summary>
    /// <param name="s">This section.</param>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="path">The configuration key or a path.</param>
    /// <param name="defaultValue">Returned default value.</param>
    /// <returns>The value.</returns>
    public static T? ResolveEnumValue<T>( this ImmutableConfigurationSection s,
                                         IActivityMonitor monitor,
                                         string path,
                                         T defaultValue )
          where T : struct, Enum
    {
        var a = s.TryResolveValue( path );
        if( a == null ) return defaultValue;
        if( !Enum.TryParse<T>( a, true, out var value ) )
        {
            return WarnWithDefault( s, monitor, path, $"a {typeof( T ).Name} value", defaultValue, a );
        }
        return value;
    }
}

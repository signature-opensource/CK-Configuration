using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;

namespace CK.Core
{
    /// <summary>
    /// Extends configuration objects.
    /// </summary>
    public static class ConfigurationSectionExtension
    {
        /// <summary>
        /// Handles opt-in or opt-out section that can have "true" or "false" value or children.
        /// <para>
        /// Note that for convenience, this extension method can be called on a null this <paramref name="parent"/>:
        /// the section doesn't obviously exists and <paramref name="optOut"/> value applies.
        /// </para>
        /// </summary>
        /// <param name="parent">This parent configuration. Can be null.</param>
        /// <param name="path">The configuration key or a path to a subordinated key.</param>
        /// <param name="optOut">
        /// <list type="bullet">
        ///   <item>
        ///     True to consider unexisting section to be the default configuration.
        ///     To skip the configuration, the section must have a "false" value.
        ///   </item>
        ///   <item>
        ///     False to ignore an unexisting section.
        ///     To apply the default configuration, the section must have a "true" value.
        ///   </item>
        /// </list>
        /// </param>
        /// <param name="content">Non null if the section has content: configuration applies.</param>
        /// <returns>
        /// True if the configuration applies (if <paramref name="content"/> is null, the default configuration must be applied),
        /// false if the configuration must be skipped.
        /// </returns>
        public static bool ShouldApplyConfiguration( this ImmutableConfigurationSection? parent,
                                                     string path,
                                                     bool optOut,
                                                     out ImmutableConfigurationSection? content )
        {
            content = parent?.TryGetSection( path );
            if( content == null ) return optOut;
            if( bool.TryParse( content.Value, out var b ) )
            {
                content = null;
                return b;
            }
            if( !content.HasChildren )
            {
                content = null;
                return optOut;
            }
            return true;
        }

        /// <summary>
        /// Handles opt-in or opt-out section that can have "true" or "false" value or children.
        /// <para>
        /// Note that for convenience, this extension method can be called on a null this <paramref name="parent"/>:
        /// the section doesn't obviously exists and <paramref name="optOut"/> value applies.
        /// </para>
        /// </summary>
        /// <param name="parent">This parent configuration. Can be null.</param>
        /// <param name="path">The configuration key or a path to a subordinated key.</param>
        /// <param name="optOut">
        /// <list type="bullet">
        ///   <item>
        ///     True to consider unexisting section to be the default configuration.
        ///     To skip the configuration, the section must have a "false" value.
        ///   </item>
        ///   <item>
        ///     False to ignore an unexisting section.
        ///     To apply the default configuration, the section must have a "true" value.
        ///   </item>
        /// </list>
        /// </param>
        /// <param name="content">Non null if the section has content: configuration applies.</param>
        /// <returns>
        /// True if the configuration applies (if <paramref name="content"/> is null, the default configuration must be applied),
        /// false if the configuration must be skipped.
        /// </returns>
        public static bool ShouldApplyConfiguration( this IConfiguration? parent,
                                                     string path,
                                                     bool optOut,
                                                     out IConfigurationSection? content )
        {
            content = parent?.GetSection( path );
            if( content == null || !content.Exists() )
            {
                content = null;
                return optOut;
            }
            if( bool.TryParse( content.Value, out var b ) )
            {
                content = null;
                return b;
            }
            return true;
        }

        /// <summary>
        /// Gets whether the <paramref name="path"/> is a child path (at any depth).
        /// </summary>
        /// <param name="section">This section.</param>
        /// <param name="path">The path that may be a child path.</param>
        /// <returns>True if the path is a child path.</returns>
        public static bool IsChildPath( this IConfigurationSection section, ReadOnlySpan<char> path )
        {
            string sPath = section.Path;
            return path.Length > sPath.Length + 1
                   && path[sPath.Length] == ':'
                   && path.Slice( 0, sPath.Length ).Equals( sPath, StringComparison.OrdinalIgnoreCase );
        }

        /// <summary>
        /// Gets this section's parent path (or any parent path).
        /// </summary>
        /// <param name="section">This section.</param>
        /// <param name="distance">Optional distance to the parent section.</param>
        /// <returns>This section's parent path.</returns>
        public static ReadOnlySpan<char> GetParentPath( this IConfigurationSection section, int distance = 1 )
        {
            if( distance <= 0 ) return section.Path;
            if( distance == 1 )
            {
                int len = section.Path.Length - section.Key.Length - 1;
                return len > 0
                        ? section.Path.AsSpan( 0, len )
                        : default;
            }
            ReadOnlySpan<char> p = section.Path;
            int idx = 0;
            while( --distance >= 0 && (idx = p.LastIndexOf( ':' )) >= 0 )
            {
                p = p.Slice( 0, idx );
            }
            return idx > 0 ? p : default;
        }

        /// <summary>
        /// Emits an error if the configuration key exists and returns false.
        /// </summary>
        /// <param name="s">This section.</param>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="path">The configuration key or a path.</param>
        /// <param name="reasonPhrase">The reason why this property must not be defined here.</param>
        /// <returns>True on success (no configuration), false if the key exists.</returns>
        public static bool CheckNotExist( this ImmutableConfigurationSection s, IActivityMonitor monitor, string path, string reasonPhrase )
        {
            if( s.TryGetSection( path ) != null )
            {
                monitor.Error( $"Invalid '{s.Path}:{path}' key: {reasonPhrase}" );
                return false;
            }
            return true;
        }

        /// <summary>
        /// Lookups a "true"/"false" value in this section or above.
        /// <para>
        /// This never throws: if the value exists and cannot be parsed, emits a log warning
        /// and returns null.
        /// </para>
        /// </summary>
        /// <param name="s">This section.</param>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="path">The configuration key or a path.</param>
        /// <returns>The value or null.</returns>
        public static bool? TryLookupBooleanValue( this ImmutableConfigurationSection s,
                                                   IActivityMonitor monitor,
                                                   string path )
        {
            return TryReadBoolean( s, monitor, path, s.TryLookupValue( path ) );
        }

        /// <summary>
        /// Tries to get a "true"/"false" value in this section.
        /// <para>
        /// This never throws: if the value exists and cannot be parsed, emits a log warning
        /// and returns null.
        /// </para>
        /// </summary>
        /// <param name="s">This section.</param>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="path">The configuration key or a path.</param>
        /// <returns>The value or null.</returns>
        public static bool? TryGetBooleanValue( this ImmutableConfigurationSection s,
                                                IActivityMonitor monitor,
                                                string path )
        {
            return TryReadBoolean( s, monitor, path, s[path] );
        }

        private static bool? TryReadBoolean( ImmutableConfigurationSection s, IActivityMonitor monitor, string path, string? a )
        {
            if( a == null ) return null;
            if( !bool.TryParse( a, out var value ) )
            {
                return WarnAndIgnore<bool>( s, monitor, path, "'true' or 'false'", a );
            }
            return value;
        }

        /// <summary>
        /// Lookups an integer value in this section or above.
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
        public static int? TryLookupIntValue( this ImmutableConfigurationSection s,
                                              IActivityMonitor monitor,
                                              string path,
                                              int min = 0,
                                              int max = int.MaxValue )
        {
            return TryReadInt( s, monitor, path, min, max, s.TryLookupValue( path ) );
        }

        /// <summary>
        /// Tries to get an integer value in this section.
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
        public static int? TryGetIntValue( this ImmutableConfigurationSection s,
                                           IActivityMonitor monitor,
                                           string path,
                                           int min = 0,
                                           int max = int.MaxValue )
        {
            return TryReadInt( s, monitor, path, min, max, s[path] );
        }

        private static int? TryReadInt( ImmutableConfigurationSection s, IActivityMonitor monitor, string path, int min, int max, string? a )
        {
            if( a == null ) return null;
            if( !int.TryParse( a, out var value ) )
            {
                return WarnAndIgnore<int>( s, monitor, path, "an integer", a );
            }
            if( value < min || value > max )
            {
                return WarnOutOfRangeAndIgnore<int>( s, monitor, path, value, min, max );
            }
            return value;
        }

        /// <summary>
        /// Lookups a <see cref="TimeSpan"/> value in this section or above.
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
        public static TimeSpan? TryLookupTimeSpanValue( this ImmutableConfigurationSection s,
                                                        IActivityMonitor monitor,
                                                        string path,
                                                        TimeSpan min,
                                                        TimeSpan max )
        {
            return TryReadTimeSpan( s, monitor, path, min, max, s.TryLookupValue( path ) );
        }

        /// <summary>
        /// Tries to get a <see cref="TimeSpan"/> value in this section.
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
        public static TimeSpan? TryGetTimeSpanValue( this ImmutableConfigurationSection s,
                                                     IActivityMonitor monitor,
                                                     string path,
                                                     TimeSpan min,
                                                     TimeSpan max )
        {
            return TryReadTimeSpan( s, monitor, path, min, max, s[path] );
        }

        static TimeSpan? TryReadTimeSpan( ImmutableConfigurationSection s, IActivityMonitor monitor, string key, TimeSpan min, TimeSpan max, string? a )
        {
            if( a == null ) return null;
            if( !TimeSpan.TryParse( a, out var value ) )
            {
                return WarnAndIgnore<TimeSpan>( s, monitor, key, "a TimeSpan", a );
            }
            if( value < min || value > max )
            {
                return WarnOutOfRangeAndIgnore<TimeSpan>( s, monitor, key, value, min, max );
            }
            return value;
        }

        /// <summary>
        /// Lookups a <see cref="DateTime"/> value in this section or above.
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
        public static DateTime? TryLookupDateTimeValue( this ImmutableConfigurationSection s,
                                                        IActivityMonitor monitor,
                                                        string path,
                                                        DateTime min,
                                                        DateTime max )
        {
            return TryReadDateTime( s, monitor, path, min, max, s.TryLookupValue( path ) );
        }

        /// <summary>
        /// Tries to get a <see cref="DateTime"/> value in this section.
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
        public static DateTime? TryGetDateTimeValue( this ImmutableConfigurationSection s,
                                                     IActivityMonitor monitor,
                                                     string path,
                                                     DateTime min,
                                                     DateTime max )
        {
            return TryReadDateTime( s, monitor, path, min, max, s[path] );
        }

        static DateTime? TryReadDateTime( ImmutableConfigurationSection s, IActivityMonitor monitor, string path, DateTime min, DateTime max, string? a )
        {
            if( a == null ) return null;
            if( !DateTime.TryParse( a, out var value ) )
            {
                return WarnAndIgnore<DateTime>( s, monitor, path, "a DateTime", a );
            }
            if( value < min || value > max )
            {
                return WarnOutOfRangeAndIgnore<DateTime>( s, monitor, path, value, min, max );
            }
            return value;
        }

        /// <summary>
        /// Lookups a floating number value in this section or above.
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
        public static double? TryLookupDoubleValue( this ImmutableConfigurationSection s,
                                                    IActivityMonitor monitor,
                                                    string path,
                                                    double min = 0.0,
                                                    double max = double.MaxValue )
        {
            return TryReadDouble( s, monitor, path, min, max, s.TryLookupValue( path ) );
        }

        /// <summary>
        /// Tries to get a floating number value in this section.
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
        public static double? TryGetDoubleValue( this ImmutableConfigurationSection s,
                                                 IActivityMonitor monitor,
                                                 string path,
                                                 double min = 0.0,
                                                 double max = double.MaxValue )
        {
            return TryReadDouble( s, monitor, path, min, max, s[path] );
        }

        static double? TryReadDouble( ImmutableConfigurationSection s, IActivityMonitor monitor, string path, double min, double max, string? a )
        {
            if( a == null ) return null;
            if( !double.TryParse( a, out var value ) )
            {
                return WarnAndIgnore<double>( s, monitor, path, "a number", a );
            }
            if( value < min || value > max )
            {
                return WarnOutOfRangeAndIgnore<double>( s, monitor, path, value, min, max );
            }
            return value;
        }

        /// <summary>
        /// Lookups an enum value in this section or above.
        /// <para>
        /// This never throws: if the value exists and cannot be parsed, emits a log warning
        /// and returns null.
        /// </para>
        /// </summary>
        /// <param name="s">This section.</param>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="path">The configuration key or a path.</param>
        /// <returns>The value or null.</returns>
        public static T? TryLookupEnumValue<T>( this ImmutableConfigurationSection s,
                                                IActivityMonitor monitor,
                                                string path )
            where T : struct, Enum
        {
            return TryReadEnum<T>( s, monitor, path, s.TryLookupValue( path ) );
        }

        /// <summary>
        /// Tries to get an enum value in this section.
        /// <para>
        /// This never throws: if the value exists and cannot be parsed, emits a log warning
        /// and returns null.
        /// </para>
        /// </summary>
        /// <param name="s">This section.</param>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="path">The configuration key or a path.</param>
        /// <returns>The value or null.</returns>
        public static T? TryGetEnumValue<T>( this ImmutableConfigurationSection s,
                                             IActivityMonitor monitor,
                                             string path )
            where T : struct, Enum
        {
            return TryReadEnum<T>( s, monitor, path, s[path] );
        }

        static T? TryReadEnum<T>( ImmutableConfigurationSection s, IActivityMonitor monitor, string path, string? a ) where T : struct, Enum
        {
            if( a == null ) return null;
            if( !Enum.TryParse<T>( a, true, out var value ) )
            {
                return WarnAndIgnore<T>( s, monitor, path, $"a {typeof( T ).Name} value", a );
            }
            return value;
        }

        static T? WarnOutOfRangeAndIgnore<T>( ImmutableConfigurationSection s, IActivityMonitor monitor, string path, T value, T min, T max )
        {
            monitor.Warn( $"Invalid '{s.Path}:{path}': value '{value}' must be between '{min}' and '{max}'. Ignored." );
            return default;
        }

        static T? WarnAndIgnore<T>( ImmutableConfigurationSection s,
                                    IActivityMonitor monitor,
                                    string path,
                                    string expected,
                                    string? a )
        {
            monitor.Warn( $"Unable to parse '{s.Path}:{path}' value, expected {expected} but got '{a}'. Ignored." );
            return default;
        }

        static T WarnWithDefault<T>( ImmutableConfigurationSection s,
                                     IActivityMonitor monitor,
                                     string path,
                                     string expected,
                                     T defaultValue,
                                     string? a )
        {
            monitor.Warn( $"Unable to parse '{s.Path}:{path}' value, expected {expected} but got '{a}'. Using default '{defaultValue}'." );
            return defaultValue;
        }



        /// <summary>
        /// Lookups a "true"/"false" (case insensitive) boolean value in this section or above.
        /// <para>
        /// If the value exists and cannot be parsed, emits a log warning and returns the <paramref name="defaultValue"/>.
        /// </para>
        /// </summary>
        /// <param name="s">This section.</param>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="path">The configuration key or a path.</param>
        /// <param name="defaultValue">Returned default value.</param>
        /// <returns>The value.</returns>
        public static bool LookupBooleanValue( this ImmutableConfigurationSection s,
                                                IActivityMonitor monitor,
                                                string path,
                                                bool defaultValue = false )
        {
            var a = s.TryLookupValue( path );
            if( a == null ) return defaultValue;
            if( !bool.TryParse( a, out var value ) )
            {
                return WarnWithDefault( s, monitor, path, "'true' or 'false'", defaultValue, a );
            }
            return value;
        }

        /// <summary>
        /// Lookups an integer value in this section or above.
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
        public static int LookupIntValue( this ImmutableConfigurationSection s,
                                          IActivityMonitor monitor,
                                          string path,
                                          int defaultValue = 0 )
        {
            var a = s.TryLookupValue( path );
            if( a == null ) return defaultValue;
            if( !int.TryParse( a, out var value ) )
            {
                return WarnWithDefault( s, monitor, path, "an integer", defaultValue, a );
            }
            return value;
        }

        /// <summary>
        /// Lookups a <see cref="TimeSpan"/> value in this section or above.
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
        public static TimeSpan LookupTimeSpanValue( this ImmutableConfigurationSection s,
                                                    IActivityMonitor monitor,
                                                    string path,
                                                    TimeSpan defaultValue )
        {
            var a = s.TryLookupValue( path );
            if( a == null ) return defaultValue;
            if( !TimeSpan.TryParse( a, out var value ) )
            {
                return WarnWithDefault( s, monitor, path, "a TimeSpan", defaultValue, a );
            }
            return value;
        }

        /// <summary>
        /// Lookups a <see cref="DateTime"/> value in this section or above.
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
        public static DateTime LookupDateTimeValue( this ImmutableConfigurationSection s,
                                                    IActivityMonitor monitor,
                                                    string path,
                                                    DateTime defaultValue )
        {
            var a = s.TryLookupValue( path );
            if( a == null ) return defaultValue;
            if( !DateTime.TryParse( a, out var value ) )
            {
                return WarnWithDefault( s, monitor, path, "a DateTime", defaultValue, a );
            }
            return value;
        }

        /// <summary>
        /// Lookups a float value in this section or above.
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
        public static double LookupDoubleValue( this ImmutableConfigurationSection s,
                                                IActivityMonitor monitor,
                                                string path,
                                                double defaultValue = 0.0 )
        {
            var a = s.TryLookupValue( path );
            if( a == null ) return defaultValue;
            if( !double.TryParse( a, out var value ) )
            {
                return WarnWithDefault( s, monitor, path, "a float number", defaultValue, a );
            }
            return value;
        }

        /// <summary>
        /// Lookups an enum value in this section or above.
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
        public static T? LookupEnumValue<T>( this ImmutableConfigurationSection s,
                                             IActivityMonitor monitor,
                                             string path,
                                             T defaultValue )
              where T : struct, Enum
        {
            var a = s.TryLookupValue( path );
            if( a == null ) return defaultValue;
            if( !Enum.TryParse<T>( a, true, out var value ) )
            {
                return WarnWithDefault( s, monitor, path, $"a {typeof( T ).Name} value", defaultValue, a );
            }
            return value;
        }

        /// <summary>
        /// Helper that reads a string array from a string value, a comma separated string, or children
        /// sections (with string value or comma separated string) that must have integer keys ("0", "1",...).
        /// Returns null on error (and the error is logged).
        /// </summary>
        /// <param name="s">This section.</param>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="path">The configuration key or a path.</param>
        /// <returns>The string array (empty if the key doesn't exist) or null on error.</returns>
        public static string[]? ReadStringArray( ImmutableConfigurationSection s, IActivityMonitor monitor, string path )
        {
            return s.TryGetSection( path ).ReadStringArray( monitor );
        }

        /// <summary>
        /// Helper that reads a string array from a string value, a comma separated string, or children
        /// sections (with string value or comma separated string) that must have integer keys ("0", "1",...).
        /// Returns null on error (and the error is logged).
        /// </summary>
        /// <param name="s">This section.</param>
        /// <param name="monitor">The monitor to use.</param>
        /// <returns>The string array (empty if the section is null) or null on error.</returns>
        public static string[]? ReadStringArray( this ImmutableConfigurationSection? s, IActivityMonitor monitor )
        {
            if( s != null )
            {
                if( s.Value != null )
                {
                    return s.Value.Split( ',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.RemoveEmptyEntries );
                }
                var result = new List<string>();
                foreach( var o in s.GetChildren() )
                {
                    var value = o.Value;
                    if( value == null || !int.TryParse( o.Key, out _ ) )
                    {
                        monitor.Error( $"Invalid array configuration for '{s.Path}': key '{o.Path}' is invalid." );
                        return null;
                    }
                    if( string.IsNullOrEmpty( value ) ) continue;
                    if( value.Contains( ',' ) ) result.AddRangeArray( value.Split( ',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.RemoveEmptyEntries ) );
                    else
                    {
                        value = value.Trim();
                        if( value.Length > 0 ) result.Add( value );
                    }
                }
                return result.ToArray();
            }
            return Array.Empty<string>();
        }

        /// <summary>
        /// Calls <see cref="ReadStringArray(ImmutableConfigurationSection, IActivityMonitor)"/> and ensures that
        /// strings are unique.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="s">The section.</param>
        /// <param name="comparer">Optional comparer.</param>
        /// <returns>A set of unique strings or null on error.</returns>
        public static HashSet<string>? ReadUniqueStringSet( this ImmutableConfigurationSection? s, IActivityMonitor monitor, StringComparer? comparer = null )
        {
            var a = ReadStringArray( s, monitor );
            if( a == null ) return null;
            var set = new HashSet<string>( a, comparer );
            if( set.Count != a.Length )
            {
                Throw.DebugAssert( s != null, "Since we found something." );
                monitor.Error( $"Duplicate found in '{s.Path}': {a.Except( set ).Concatenate()}." );
                return null;
            }
            return set;
        }

        /// <summary>
        /// Calls <see cref="ReadStringArray(ImmutableConfigurationSection, IActivityMonitor, string)"/> and ensures that
        /// strings are unique.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="s">The section.</param>
        /// <param name="key">The configuration key.</param>
        /// <param name="comparer">Optional comparer.</param>
        /// <returns>A set of unique strings or null on error.</returns>
        public static HashSet<string>? ReadUniqueStringSet( this ImmutableConfigurationSection s, IActivityMonitor monitor, string key, StringComparer? comparer = null )
        {
            return s.TryGetSection( key ).ReadUniqueStringSet( monitor, comparer );
        }
    }
}

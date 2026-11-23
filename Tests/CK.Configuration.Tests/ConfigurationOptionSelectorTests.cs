using CK.Core;
using NUnit.Framework;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using static CK.Testing.MonitorTestHelper;

namespace CK.Configuration.Tests;

[TestFixture]
public class ConfigurationOptionSelectorTests
{
    sealed record Option( string Name, bool IsEnabledByDefault, bool IsRequired, IReadOnlyList<string> Requires ) : IConfigurationOption
    {
        public static Option Optional( string name, params string[] requires ) => new Option( name, false, false, requires );
        public static Option ByDefault( string name, params string[] requires ) => new Option( name, true, false, requires );
        public static Option Required( string name, params string[] requires ) => new Option( name, true, true, requires );
    }

    static readonly Option[] _options = new[] {Option.ByDefault( "Header" ), Option.Optional( "Main" ), Option.ByDefault( "Compression", "Main" ) };

    static ImmutableConfigurationSection Section( string json )
        => new ImmutableConfigurationSection( new MutableConfigurationSection( "Owner" ).AddJson( json ) );

    static string[] Select( ImmutableConfigurationSection? section )
    {
        ConfigurationOptionSelector.TrySelect( TestHelper.Monitor, _options, section, out var selected ).ShouldBeTrue();
        return selected.Select( s => s.Option.Name ).ToArray();
    }

    [Test]
    public void selection_defaults()
    {
        // Compression is enabled by default but Main is disabled: Compression is disabled with it.
        Select( null ).ShouldBe( new[] { "Header" } );
        Select( Section( "{}" ) ).ShouldBe( new[] { "Header" } );
    }

    [Test]
    public void selection_true_false_and_section()
    {
        Select( Section( """{ "Main": true }""" ) ).ShouldBe( new[] { "Header", "Main", "Compression" } );
        Select( Section( """{ "Main": "True", "Header": false, "Compression": "false" }""" ) ).ShouldBe( new[] { "Main" } );

        ConfigurationOptionSelector.TrySelect( TestHelper.Monitor, _options, Section( """{ "Main": { "Level": 3 } }""" ), out var selected ).ShouldBeTrue();
        selected.Select( s => s.Option.Name ).ShouldBe( new[] { "Header", "Main", "Compression" } );
        selected[0].Configuration.ShouldBeNull();
        selected[1].Configuration.ShouldNotBeNull()["Level"].ShouldBe( "3" );
        selected[2].Configuration.ShouldBeNull();
    }

    [Test]
    public void selection_errors()
    {
        using( TestHelper.Monitor.CollectTexts( out var texts ) )
        {
            ConfigurationOptionSelector.TrySelect( TestHelper.Monitor, _options, Section( """{ "Main": "yes" }""" ), out var selected ).ShouldBeFalse();
            selected.ShouldBeEmpty();
            texts.ShouldContain( "Invalid option 'Owner:Main' = 'yes': true, false or a section is expected." );
        }
        using( TestHelper.Monitor.CollectTexts( out var texts ) )
        {
            ConfigurationOptionSelector.TrySelect( TestHelper.Monitor, _options, Section( """{ "Compression": true }""" ), out var selected ).ShouldBeFalse();
            texts.ShouldContain( "The option 'Compression' requires the option 'Main' that is disabled." );
        }
    }

    [Test]
    public void selection_of_required_and_redundant_values()
    {
        var options = new[] { Option.Optional( "Opt" ), Option.ByDefault( "ByDefault" ), Option.Required( "Must", "Opt" ) };
        // A required option is always enabled: here it requires "Opt" that is disabled, this is an error (not a silent drop).
        using( TestHelper.Monitor.CollectTexts( out var texts ) )
        {
            ConfigurationOptionSelector.TrySelect( TestHelper.Monitor, options, Section( "{}" ), out _ ).ShouldBeFalse();
            texts.ShouldContain( "The option 'Must' requires the option 'Opt' that is disabled." );
        }
        // It can't be turned off.
        using( TestHelper.Monitor.CollectTexts( out var texts ) )
        {
            ConfigurationOptionSelector.TrySelect( TestHelper.Monitor, options, Section( """{ "Opt": true, "Must": false }""" ), out _ ).ShouldBeFalse();
            texts.ShouldContain( "The option 'Owner:Must' is required: it can't be turned off." );
        }
        // A redundant true or false is a warning.
        using( TestHelper.Monitor.CollectTexts( out var texts ) )
        {
            ConfigurationOptionSelector.TrySelect( TestHelper.Monitor, options, Section( """{ "Opt": true, "ByDefault": true, "Must": true }""" ), out var selected ).ShouldBeTrue();
            selected.Select( s => s.Option.Name ).ShouldBe( new[] { "Opt", "ByDefault", "Must" } );
            texts.ShouldContain( "Redundant 'Owner:ByDefault' = true: the option is enabled by default." );
            texts.ShouldContain( "Redundant 'Owner:Must' = true: the option is required." );
        }
        using( TestHelper.Monitor.CollectTexts( out var texts ) )
        {
            ConfigurationOptionSelector.TrySelect( TestHelper.Monitor, options, Section( """{ "Opt": { "A": 1 }, "ByDefault": false }""" ), out var selected ).ShouldBeTrue();
            selected.Select( s => s.Option.Name ).ShouldBe( new[] { "Opt", "Must" } );
            texts.ShouldNotContain( t => t.StartsWith( "Redundant" ) );
        }
    }

    [Test]
    public void selection_cascade_of_defaults()
    {
        var options = new[] { Option.Optional( "A" ), Option.ByDefault( "B", "A" ), Option.ByDefault( "C", "B" ) };
        using( TestHelper.Monitor.CollectTexts( out var texts ) )
        {
            ConfigurationOptionSelector.TrySelect( TestHelper.Monitor, options, (ImmutableConfigurationSection?)null, out var selected ).ShouldBeTrue();
            selected.ShouldBeEmpty();
            texts.ShouldContain( "The option 'B' (enabled by default) is disabled: it requires the option 'A' that is disabled." );
            texts.ShouldContain( "The option 'C' (enabled by default) is disabled: it requires the option 'B' that is disabled." );
        }
        ConfigurationOptionSelector.TrySelect( TestHelper.Monitor, options, Section( """{ "A": true }""" ), out var all ).ShouldBeTrue();
        all.Select( s => s.Option.Name ).ShouldBe( new[] { "A", "B", "C" } );
    }
}

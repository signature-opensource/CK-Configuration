using CK.Core;
using NUnit.Framework;
using Shouldly;
using System.Linq;
using static CK.Testing.MonitorTestHelper;

namespace CK.Configuration.Tests;

[TestFixture]
public class DefaultsResolutionTests
{
    static readonly ImmutableConfigurationSection _root = ImmutableConfigurationSection.CreateFromJson( "Root", """
        {
            "Timeout": "Root's own: never a default",
            "Defaults": { "Timeout": "00:00:10", "Retry": "3" },
            "Group": {
                "Defaults": { "Timeout": "00:00:20" },
                "Party": {
                    "Name": "Party",
                    "Defaults": { "Timeout": "Party's defaults: not for itself" }
                },
                "Other": {
                    "Timeout": "00:00:01"
                }
            }
        }
        """ );

    static ImmutableConfigurationSection Party => _root.GetRequiredSection( "Group:Party" );

    [Test]
    public void the_nearest_Defaults_wins_and_the_ancestors_own_keys_are_never_used()
    {
        Party.TryResolveValue( "Timeout", out var distance ).ShouldBe( "00:00:20" );
        distance.ShouldBe( 1 );
        Party.TryResolveValue( "Retry", out distance ).ShouldBe( "3" );
        distance.ShouldBe( 2 );
        Party.TryResolveValue( "Name" ).ShouldBe( "Party" );
        Party.TryResolveValue( "Unknown" ).ShouldBeNull();
        _root.GetRequiredSection( "Group" ).TryResolveValue( "Timeout" ).ShouldBe( "00:00:10", "The Group's own Defaults are for the sections below it." );
    }

    [Test]
    public void the_own_key_wins()
    {
        _root.GetRequiredSection( "Group:Other" ).TryResolveValue( "Timeout", out var distance ).ShouldBe( "00:00:01" );
        distance.ShouldBe( 0 );
    }

    [Test]
    public void a_section_inside_a_Defaults_does_not_use_this_Defaults()
    {
        var c = ImmutableConfigurationSection.CreateFromJson( "Root", """
            {
                "Defaults": { "A": "RootDefault" },
                "Level": {
                    "Defaults": { "Sub": { "X": "1" }, "A": "LevelDefault" }
                }
            }
            """ );
        var sub = c.GetRequiredSection( "Level:Defaults:Sub" );
        // "Level:Defaults" contains "Sub": it is skipped, the root's Defaults is used.
        sub.TryResolveValue( "A" ).ShouldBe( "RootDefault" );
    }

    [Test]
    public void ResolveAllSection_enumerates_from_the_farthest_Defaults_to_the_own_key()
    {
        var c = ImmutableConfigurationSection.CreateFromJson( "Root", """
            {
                "Defaults": { "Set": "R" },
                "Group": {
                    "Defaults": { "Set": "G" },
                    "Party": { "Set": "P" }
                }
            }
            """ );
        var all = c.GetRequiredSection( "Group:Party" ).ResolveAllSectionWithDistance( "Set" ).ToList();
        all.Select( x => (x.Section.Value, x.Distance) ).ShouldBe( new[] { ("R", 2), ("G", 1), ("P", 0) } );
    }

    [Test]
    public void the_lookupParent_anchor_Defaults_are_used()
    {
        var anchor = ImmutableConfigurationSection.CreateFromJson( "Root", """{ "Defaults": { "Retry": "5" } }""" );
        var m = new MutableConfigurationSection( "Root:Dynamic" );
        m["Name"] = "Dynamic";
        var dynamic = new ImmutableConfigurationSection( m, anchor );
        dynamic.TryResolveValue( "Retry", out var distance ).ShouldBe( "5" );
        distance.ShouldBe( 1 );
    }

    [Test]
    public void typed_values_are_resolved()
    {
        Party.TryResolveIntValue( TestHelper.Monitor, "Retry" ).ShouldBe( 3 );
        Party.TryResolveTimeSpanValue( TestHelper.Monitor, "Timeout", System.TimeSpan.Zero, System.TimeSpan.MaxValue ).ShouldBe( System.TimeSpan.FromSeconds( 20 ) );
        Party.ResolveIntValue( TestHelper.Monitor, "Unknown", 42 ).ShouldBe( 42 );
    }

    [Test]
    public void AssemblyConfiguration_Resolve_combines_the_Defaults_and_honors_the_locking()
    {
        var c = ImmutableConfigurationSection.CreateFromJson( "Root", """
            {
                "Assemblies": { "Ignored": "I" },
                "Defaults": { "Assemblies": { "StrategyPlugin": "S" }, "DefaultAssembly": "StrategyPlugin" },
                "Party": {
                    "Assemblies": { "ConsumerA.Strategy": "A" }
                }
            }
            """ );
        var a = AssemblyConfiguration.Resolve( TestHelper.Monitor, c.GetRequiredSection( "Party" ) ).ShouldNotBeNull();
        a.Assemblies.Values.ShouldBe( new[] { "StrategyPlugin", "ConsumerA.Strategy" }, ignoreOrder: true );
        a.DefaultAssembly.ShouldBe( "StrategyPlugin" );
        a.IsLocked.ShouldBeFalse();

        var locked = ImmutableConfigurationSection.CreateFromJson( "Root", """
            {
                "Defaults": { "Assemblies": [ "IsLocked", "StrategyPlugin" ], "DefaultAssembly": "StrategyPlugin" },
                "Party": {
                    "Assemblies": { "ConsumerA.Strategy": "A" },
                    "DefaultAssembly": "ConsumerA.Strategy"
                }
            }
            """ );
        using( TestHelper.Monitor.CollectTexts( out var logs ) )
        {
            var l = AssemblyConfiguration.Resolve( TestHelper.Monitor, locked.GetRequiredSection( "Party" ) ).ShouldNotBeNull();
            l.IsLocked.ShouldBeTrue();
            l.Assemblies.Values.ShouldBe( new[] { "StrategyPlugin" } );
            l.DefaultAssembly.ShouldBe( "StrategyPlugin" );
            logs.ShouldContain( "Assemblies is locked. Ignoring 'Root:Party:Assemblies'." );
            logs.ShouldContain( "Assemblies is locked. Ignoring 'Root:Party:DefaultAssembly'." );
        }
    }
}

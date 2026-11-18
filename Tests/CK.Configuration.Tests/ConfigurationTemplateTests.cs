using CK.Core;
using NUnit.Framework;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using static CK.Testing.MonitorTestHelper;

namespace CK.Configuration.Tests;

[TestFixture]
public class ConfigurationTemplateTests
{
    static ImmutableConfigurationSection Json( string json ) => ImmutableConfigurationSection.CreateFromJson( "Root", json );

    [Test]
    public void InstanceCount_gives_indexed_instances_named_by_their_index()
    {
        var c = Json( """{ "InstanceCount": 3 }""" );
        ConfigurationTemplate.IsTemplate( c ).ShouldBeTrue();
        ConfigurationTemplate.TryReadInstances( TestHelper.Monitor, c, "Trolley", out var instances ).ShouldBeTrue();
        instances.ShouldNotBeNull().Select( i => (i.Index, i.Name) ).ShouldBe( new[] { (1, "1"), (2, "2"), (3, "3") } );
        instances[0].TemplateName.ShouldBe( "Trolley" );
    }

    [Test]
    public void Instances_are_named_and_indexed_in_their_order()
    {
        var c = Json( """{ "Instances": [ "Top", "Bottom", "Red" ] }""" );
        ConfigurationTemplate.TryReadInstances( TestHelper.Monitor, c, null, out var instances ).ShouldBeTrue();
        instances.ShouldNotBeNull().Select( i => (i.Index, i.Name) ).ShouldBe( new[] { (1, "Top"), (2, "Bottom"), (3, "Red") } );
        instances.Find( "red" ).ShouldBeSameAs( instances[2] );
        instances.Find( "2" ).ShouldBeSameAs( instances[1] );
        instances.Find( "Blue" ).ShouldBeNull();
        instances.Find( "4" ).ShouldBeNull();
        instances.IsOpened.ShouldBeFalse();
    }

    [Test]
    public void a_section_without_instances_is_not_a_template()
    {
        var c = Json( """{ "Local": {} }""" );
        ConfigurationTemplate.IsTemplate( c ).ShouldBeFalse();
        ConfigurationTemplate.TryReadInstances( TestHelper.Monitor, c, null, out var instances ).ShouldBeTrue();
        instances.ShouldBeNull();
    }

    [TestCase( """{ "InstanceCount": 0 }""", "A positive integer is expected" )]
    [TestCase( """{ "InstanceCount": "many" }""", "A positive integer is expected" )]
    [TestCase( """{ "InstanceCount": 2, "Instances": [ "A" ] }""", "a template has one of them" )]
    [TestCase( """{ "Instances": "A" }""", "a non empty array of instance names is expected" )]
    [TestCase( """{ "Instances": [ "A", "a" ] }""", "Duplicate instance name 'a'" )]
    [TestCase( """{ "Instances": [ "A B" ] }""", "Invalid instance name 'A B'" )]
    [TestCase( """{ "Instances": [ "$A" ] }""", "Invalid instance name '$A'" )]
    [TestCase( """{ "Instances": [ "3" ] }""", "Invalid instance name '3'" )]
    [TestCase( """{ "MaxInstanceCount": 0 }""", "A positive integer is expected" )]
    [TestCase( """{ "InstanceCount": 0, "MaxInstanceCount": "many" }""", "A positive integer is expected" )]
    [TestCase( """{ "InstanceCount": -1, "MaxInstanceCount": 3 }""", "A positive integer or 0 is expected" )]
    [TestCase( """{ "InstanceCount": 3, "MaxInstanceCount": 2 }""", "2 is lower than the 3 configured instance(s)" )]
    [TestCase( """{ "Instances": [ "A", "B" ], "MaxInstanceCount": 1 }""", "1 is lower than the 2 configured instance(s)" )]
    public void invalid_instances_are_errors( string json, string expected )
    {
        using( TestHelper.Monitor.CollectTexts( out var texts ) )
        {
            ConfigurationTemplate.TryReadInstances( TestHelper.Monitor, Json( json ), null, out var instances ).ShouldBeFalse();
            instances.ShouldBeNull();
            texts.ShouldContain( t => t.Contains( expected ), $"Expected: {expected}" );
        }
    }

    [Test]
    public void MaxInstanceCount_opens_a_template()
    {
        var c = Json( """{ "InstanceCount": 2, "MaxInstanceCount": 4 }""" );
        ConfigurationTemplate.TryReadInstances( TestHelper.Monitor, c, "Worker", out var instances ).ShouldBeTrue();
        instances.ShouldNotBeNull().Count.ShouldBe( 2 );
        instances.IsOpened.ShouldBeTrue();
        instances.MaxInstanceCount.ShouldBe( 4 );
        instances.IsDynamicIndex( 2 ).ShouldBeFalse();
        instances.IsDynamicIndex( 3 ).ShouldBeTrue();
        instances.IsDynamicIndex( 5 ).ShouldBeFalse();
        instances.Find( "2" ).ShouldBeSameAs( instances[1] );
        var third = instances.Find( "3" ).ShouldNotBeNull();
        (third.Index, third.Name, third.TemplateName).ShouldBe( (3, "3", "Worker") );
        instances.Find( "5" ).ShouldBeNull();
        instances.Find( "0" ).ShouldBeNull();
    }

    [TestCase( """{ "InstanceCount": 0, "MaxInstanceCount": 3 }""" )]
    [TestCase( """{ "MaxInstanceCount": 3 }""" )]
    public void an_Opened_template_may_have_no_configured_instance( string json )
    {
        var c = Json( json );
        ConfigurationTemplate.IsTemplate( c ).ShouldBeTrue();
        ConfigurationTemplate.TryReadInstances( TestHelper.Monitor, c, null, out var instances ).ShouldBeTrue();
        instances.ShouldNotBeNull().Count.ShouldBe( 0 );
        instances.Find( "1" ).ShouldNotBeNull().Name.ShouldBe( "1" );
        instances.Find( "3" ).ShouldNotBeNull();
        instances.Find( "4" ).ShouldBeNull();
    }

    [Test]
    public void dynamic_indices_follow_the_named_instances()
    {
        var c = Json( """{ "Instances": [ "Top", "Red" ], "MaxInstanceCount": 4 }""" );
        ConfigurationTemplate.TryReadInstances( TestHelper.Monitor, c, null, out var instances ).ShouldBeTrue();
        instances.ShouldNotBeNull().Find( "2" ).ShouldBeSameAs( instances[1] );
        instances.Find( "3" ).ShouldNotBeNull().Name.ShouldBe( "3" );
    }

    [Test]
    public void a_dynamic_instance_may_have_a_free_name()
    {
        var c = Json( """{ "Instances": [ "Top", "Red" ], "MaxInstanceCount": 4 }""" );
        ConfigurationTemplate.TryReadInstances( TestHelper.Monitor, c, "Tenant", out var instances ).ShouldBeTrue();
        Throw.DebugAssert( instances != null );
        var acme = instances.CreateDynamic( 3, "Acme" );
        (acme.Index, acme.Name, acme.TemplateName).ShouldBe( (3, "Acme", "Tenant") );
        instances.IsConfiguredName( "red" ).ShouldBeTrue();
        instances.IsConfiguredName( "Acme" ).ShouldBeFalse();
        // The index must be a dynamic one, the name a valid one that is not configured.
        Should.Throw<System.ArgumentException>( () => instances.CreateDynamic( 2, "Acme" ) );
        Should.Throw<System.ArgumentException>( () => instances.CreateDynamic( 5, "Acme" ) );
        Should.Throw<System.ArgumentException>( () => instances.CreateDynamic( 3, "RED" ) );
        Should.Throw<System.ArgumentException>( () => instances.CreateDynamic( 3, "3" ) );
        Should.Throw<System.ArgumentException>( () => instances.CreateDynamic( 3, "$Acme" ) );
        Should.Throw<System.ArgumentException>( () => instances.CreateDynamic( 3, "A B" ) );
    }

    [TestCase( "Acme", true )]
    [TestCase( "Acme-2_b", true )]
    [TestCase( "3", false )]
    [TestCase( "3Acme", false )]
    [TestCase( "$Acme", false )]
    [TestCase( "-Acme", false )]
    [TestCase( "A B", false )]
    public void IsValidInstanceName_is_the_rule_of_the_Instances( string name, bool expected )
    {
        ConfigurationTemplate.IsValidInstanceName( name ).ShouldBe( expected );
    }

    [TestCase( "$Worker-{Name}", "$Worker-3", 3 )]
    [TestCase( "$Worker-{Name}", "$WORKER-4", 4 )]
    [TestCase( "$Worker-{Index}", "$Worker-3", 3 )]
    [TestCase( "tcp:127.0.0.1:{37300+Index}", "tcp:127.0.0.1:37303", 3 )]
    [TestCase( "$W{Name}-{10+Index}", "$W3-13", 3 )]
    [TestCase( "$W{Name}-{10+Index}", "$W3-14", 0 )]
    [TestCase( "$Worker-{Name}", "$Worker-03", 0 )]
    [TestCase( "$Worker-{Name}", "$Worker-0", 0 )]
    [TestCase( "$Worker-{Name}", "$Worker-Top", 0 )]
    [TestCase( "$Worker-{Name}", "$Worker-3x", 0 )]
    [TestCase( "$Worker", "$Worker", 0 )]
    [TestCase( "$Worker-{Fleet.Name}", "$Worker-3", 0 )]
    public void TryMatchIndex_finds_a_dynamic_instance( string pattern, string value, int expected )
    {
        ConfigurationTemplate.TryMatchIndex( pattern, value, out var index ).ShouldBe( expected != 0 );
        index.ShouldBe( expected );
    }

    [Test]
    public void MatchDynamic_is_bound()
    {
        var c = Json( """{ "InstanceCount": 2, "MaxInstanceCount": 4 }""" );
        ConfigurationTemplate.TryReadInstances( TestHelper.Monitor, c, null, out var instances ).ShouldBeTrue();
        instances.ShouldNotBeNull().MatchDynamic( "$Worker-{Name}", "$Worker-2" ).ShouldBeNull();
        instances.MatchDynamic( "$Worker-{Name}", "$Worker-3" ).ShouldNotBeNull().Index.ShouldBe( 3 );
        instances.MatchDynamic( "$Worker-{Name}", "$Worker-5" ).ShouldBeNull();
        ConfigurationTemplate.TryReadInstances( TestHelper.Monitor, Json( """{ "InstanceCount": 2 }""" ), null, out var closed ).ShouldBeTrue();
        closed.ShouldNotBeNull().MatchDynamic( "$Worker-{Name}", "$Worker-3" ).ShouldBeNull();
    }

    [TestCase( "$Worker-{Name}", true )]
    [TestCase( "{5400+Index}", true )]
    [TestCase( "$Worker-{Fleet.Name}", false )]
    [TestCase( "$Worker", false )]
    public void HasPlaceholder_ignores_the_qualified_ones( string value, bool expected )
    {
        ConfigurationTemplate.HasPlaceholder( value ).ShouldBe( expected );
    }

    [TestCase( "http://localhost:{5400+Index}", "http://localhost:5403" )]
    [TestCase( "$Trolley-{Name}", "$Trolley-Red" )]
    [TestCase( "Logs/{name}-{index}", "Logs/Red-3" )]
    [TestCase( "{Trolley.Name}/{10+Trolley.Index}", "Red/13" )]
    [TestCase( "{-1+Index}", "2" )]
    [TestCase( "no {placeholder} here {Other.Thing}", "no {placeholder} here {Other.Thing}" )]
    public void the_placeholders_are_resolved( string value, string expected )
    {
        var red = new ConfigurationInstance( "Trolley", 3, "Red" );
        ConfigurationTemplate.TryResolve( TestHelper.Monitor, value, "Root:V", red, System.Array.Empty<ConfigurationInstance>(), out var r ).ShouldBeTrue();
        r.ShouldBe( expected );
    }

    [TestCase( "{Shop.Index}", "names no enclosing template ('Shop')" )]
    [TestCase( "{1+Name}", "only the Index can be offset" )]
    public void invalid_placeholders_are_errors( string value, string expected )
    {
        using( TestHelper.Monitor.CollectTexts( out var texts ) )
        {
            ConfigurationTemplate.TryResolve( TestHelper.Monitor, value, "Root:V", new ConfigurationInstance( "Trolley", 1, "1" ), System.Array.Empty<ConfigurationInstance>(), out _ ).ShouldBeFalse();
            texts.ShouldContain( t => t.Contains( expected ) );
        }
    }

    [Test]
    public void the_innermost_template_wins()
    {
        // A fleet App (Trolley) with a tenant template: in the tenant template, {Index} is the tenant's.
        var c = Json( """
            {
              "InstanceCount": 2,
              "Urls": "http://localhost:{5400+Index}",
              "Parties": [
                {
                  "Instances": [ "Paris", "Lyon" ],
                  "FullName": "Shops/$Shop-{Name}-of-{Trolley.Name}",
                  "Port": "{6000+Index}"
                }
              ]
            }
            """ );
        ConfigurationTemplate.TryReadInstances( TestHelper.Monitor, c, "Trolley", out var trolleys ).ShouldBeTrue();
        var second = trolleys.ShouldNotBeNull()[1];
        var changes = ConfigurationTemplate.ResolveSection( TestHelper.Monitor, c, second ).ShouldNotBeNull().ToDictionary();
        changes["Root:Urls"].ShouldBe( "http://localhost:5402" );
        // The outer pass resolves only the qualified placeholders of the inner template.
        changes["Root:Parties:0:FullName"].ShouldBe( "Shops/$Shop-{Name}-of-2" );
        changes.ContainsKey( "Root:Parties:0:Port" ).ShouldBeFalse();
        changes.Keys.ShouldNotContain( k => k.Contains( "Instance" ) );

        // The inner pass, on the result of the outer one.
        var resolved = new MutableConfigurationSection( "Root" );
        resolved.AddJson( """{ "Instances": [ "Paris", "Lyon" ], "FullName": "Shops/$Shop-{Name}-of-2", "Port": "{6000+Index}" }""" );
        var tenant = new ImmutableConfigurationSection( resolved );
        ConfigurationTemplate.TryReadInstances( TestHelper.Monitor, tenant, null, out var shops ).ShouldBeTrue();
        var lyon = ConfigurationTemplate.ResolveSection( TestHelper.Monitor, tenant, shops![1] ).ShouldNotBeNull().ToDictionary();
        lyon["Root:FullName"].ShouldBe( "Shops/$Shop-Lyon-of-2" );
        lyon["Root:Port"].ShouldBe( "6002" );
    }

    [Test]
    public void Instantiate_creates_the_section_of_an_instance_with_its_lookup_parent()
    {
        var root = Json( """
            {
              "Defaults": { "Retry": 3 },
              "Parties": [ { "Instances": [ "Paris", "Lyon" ], "MaxInstanceCount": 5, "PartyName": "$Shop-{Name}", "Local": { "Port": "{6000+Index}" } } ]
            }
            """ );
        var parties = root.GetRequiredSection( "Parties" );
        var template = parties.GetRequiredSection( "0" );
        ConfigurationTemplate.TryReadInstances( TestHelper.Monitor, template, null, out var shops ).ShouldBeTrue();
        var lyon = ConfigurationTemplate.Instantiate( TestHelper.Monitor, template, shops![1], parties ).ShouldNotBeNull();
        lyon.Path.ShouldBe( "Root:Parties:0" );
        lyon.GetChildren().Select( c => c.Key ).ShouldBe( new[] { "PartyName", "Local" } );
        lyon["PartyName"].ShouldBe( "$Shop-Lyon" );
        lyon["Local:Port"].ShouldBe( "6002" );
        // The inherited values are still found through the lookup parent.
        lyon.TryLookupValue( "Defaults:Retry" ).ShouldBe( "3" );
    }
}

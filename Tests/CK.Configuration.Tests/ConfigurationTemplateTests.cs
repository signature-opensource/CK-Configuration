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
        ConfigurationTemplate.FindInstance( instances, "red" ).ShouldBeSameAs( instances[2] );
        ConfigurationTemplate.FindInstance( instances, "2" ).ShouldBeSameAs( instances[1] );
        ConfigurationTemplate.FindInstance( instances, "Blue" ).ShouldBeNull();
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
    public void invalid_instances_are_errors( string json, string expected )
    {
        using( TestHelper.Monitor.CollectTexts( out var texts ) )
        {
            ConfigurationTemplate.TryReadInstances( TestHelper.Monitor, Json( json ), null, out var instances ).ShouldBeFalse();
            instances.ShouldBeNull();
            texts.ShouldContain( t => t.Contains( expected ), $"Expected: {expected}" );
        }
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
}

using CK.Core;
using Shouldly;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace CK.Configuration.Tests;

[TestFixture]
public class MutableAndImmutableConfigurationSectionTests
{
    [Test]
    public void GetParentPath_and_IsChildPath()
    {
        var mA = new MutableConfigurationSection( "A" );
        mA.GetParentPath( -1 ).ToString().ShouldBe( "A" );
        mA.GetParentPath( 0 ).ToString().ShouldBe( "A" );
        mA.GetParentPath().Length.ShouldBe( 0 );
        mA.GetParentPath( 1 ).Length.ShouldBe( 0 );
        mA.GetParentPath( 2 ).Length.ShouldBe( 0 );
        mA.IsChildPath( "" ).ShouldBeFalse();
        mA.IsChildPath( "A" ).ShouldBeFalse();
        mA.IsChildPath( "A:B" ).ShouldBeTrue();
        mA.IsChildPath( "A:B:C" ).ShouldBeTrue();

        var mAB = new MutableConfigurationSection( "A:B" );
        mAB.GetParentPath( -1 ).ToString().ShouldBe( "A:B" );
        mAB.GetParentPath( 0 ).ToString().ShouldBe( "A:B" );
        mAB.GetParentPath().ToString().ShouldBe( "A" );
        mAB.GetParentPath( 1 ).ToString().ShouldBe( "A" );
        mAB.GetParentPath( 2 ).Length.ShouldBe( 0 );
        mAB.GetParentPath( 3 ).Length.ShouldBe( 0 );
        mAB.IsChildPath( "" ).ShouldBeFalse();
        mAB.IsChildPath( "A" ).ShouldBeFalse();
        mAB.IsChildPath( "A:B" ).ShouldBeFalse();
        mAB.IsChildPath( "A:B:C" ).ShouldBeTrue();
        mAB.IsChildPath( "A:B:C:D" ).ShouldBeTrue();

        var mABC = new MutableConfigurationSection( "A:B", "C" );
        mABC.GetParentPath( -1 ).ToString().ShouldBe( "A:B:C" );
        mABC.GetParentPath( 0 ).ToString().ShouldBe( "A:B:C" );
        mABC.GetParentPath().ToString().ShouldBe( "A:B" );
        mABC.GetParentPath( 1 ).ToString().ShouldBe( "A:B" );
        mABC.GetParentPath( 2 ).ToString().ShouldBe( "A" );
        mABC.GetParentPath( 3 ).Length.ShouldBe( 0 );
    }


    [Test]
    public void ImmutableConfigurationSection_captures_everything()
    {
        using var config = new ConfigurationManager();
        config["X:A"] = "a";
        config["X:Nothing"] = "";
        config["X:Section"] = "Value for Section";
        config["X:Section:A"] = "a";
        config["X:Section:B"] = null;
        config["X:Section:C:More"] = "C more";

        CheckConfiguration( config.GetSection( "X" ) );
        var immutable = new ImmutableConfigurationSection( config.GetSection( "X" ) );
        CheckConfiguration( immutable );

        // Immutable captures non existing sections (no value and no children).
        var b = immutable.TryGetSection( "Section:B" );
        b.ShouldNotBeNull();
        b.Exists().ShouldBeFalse();

        static void CheckConfiguration( IConfigurationSection config )
        {
            config["A"].ShouldBe( "a" );
            config["Nothing"].ShouldBe( "" );
            config["Section"].ShouldBe( "Value for Section" );
            config["Section:A"].ShouldBe( "a" );
            config["Section:B"].ShouldBeNull();
            config["Section:C:More"].ShouldBe( "C more" );
            var sA = config.GetSection( "A" );
            sA.Value.ShouldBe( "a" );
            sA.GetChildren().ShouldBeEmpty();
            var sNothing = config.GetSection( "Nothing" );
            sNothing.Value.ShouldBe( "" );
            sNothing.GetChildren().ShouldBeEmpty();
            var sSection = config.GetSection( "Section" );
            sSection.Value.ShouldBe( "Value for Section" );
            sSection.GetChildren().Count().ShouldBe( 3 );
            sSection["A"].ShouldBe( "a" );
            sSection["B"].ShouldBeNull();
            sSection["C"].ShouldBeNull();
            sSection["C:More"].ShouldBe( "C more" );
            config["Section:C:More"].ShouldBe( "C more" );

            var sSectionC = sSection.GetSection( "C" );
            var sSectionC2 = config.GetSection( "Section:C" );
            sSectionC.Key.ShouldBe( sSectionC2.Key );
            sSectionC.Path.ShouldBe( sSectionC2.Path );
            sSectionC.Value.ShouldBe( sSectionC2.Value );
            sSectionC.GetChildren().Count().ShouldBe( sSectionC2.GetChildren().Count() );

            // Bad key behavior.
            sSection["::::"].ShouldBeNull();
            sSection.GetSection( "::::" ).Path.ShouldBe( "X:Section:::::" );
            sSection.GetSection( "::::" ).Key.ShouldBe( "" );

            sSection[":A"].ShouldBeNull();
            sSection.GetSection( ":A" ).Path.ShouldBe( "X:Section::A" );
            sSection.GetSection( ":A" ).Key.ShouldBe( "A" );

            sSection["A:"].ShouldBeNull();
            sSection.GetSection( "A:" ).Path.ShouldBe( "X:Section:A:" );
            sSection.GetSection( "A:" ).Key.ShouldBe( "" );

            sSection["NO:WAY"].ShouldBeNull();
            sSection.GetSection( "NO:WAY" ).Path.ShouldBe( "X:Section:NO:WAY" );
            sSection.GetSection( "NO:WAY" ).Key.ShouldBe( "WAY" );

            sSection["::NO:WAY::"].ShouldBeNull();
            sSection.GetSection( "::NO:WAY::" ).Path.ShouldBe( "X:Section:::NO:WAY::" );
            sSection.GetSection( "::NO:WAY::" ).Key.ShouldBe( "" );
        }

    }

    [Test]
    public void MutableConfigurationSection_invalid_parameters_check()
    {
        Util.Invokable( () => new MutableConfigurationSection( (IConfigurationSection?)null! ) ).ShouldThrow<ArgumentNullException>();
        Util.Invokable( () => new MutableConfigurationSection( (string)null! ) ).ShouldThrow<ArgumentNullException>();
        Util.Invokable( () => new MutableConfigurationSection( "" ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => new MutableConfigurationSection( ":" ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => new MutableConfigurationSection( "A::B" ) ).ShouldThrow<ArgumentException>();

        Util.Invokable( () => new MutableConfigurationSection( "A::B", "" ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => new MutableConfigurationSection( ":", "A" ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => new MutableConfigurationSection( ":A", "A" ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => new MutableConfigurationSection( "A:", "A" ) ).ShouldThrow<ArgumentException>();

        Util.Invokable( () => new MutableConfigurationSection( "A:B", "" ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => new MutableConfigurationSection( "A", "A:" ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => new MutableConfigurationSection( "A", "A:B" ) ).ShouldThrow<ArgumentException>();
    }

    [Test]
    public void MutableConfigurationSection_simple_tests()
    {
        var c = new MutableConfigurationSection( "X" );
        c["Y"] = "Value";
        c.GetMutableChildren().Count.ShouldBe( 1 );
        c.GetMutableChildren().Single().Path.ShouldBe( "X:Y" );
        c.GetMutableChildren().Single().Value.ShouldBe( "Value" );
        c["Y"].ShouldBe( "Value" );

        c["Z:A:B:C"] = "Another Value";
        c["Z:A:B:C"].ShouldBe( "Another Value" );
        c.GetRequiredSection( "Z" ).GetRequiredSection( "A" ).GetRequiredSection( "B" ).GetRequiredSection( "C" ).Value.ShouldBe( "Another Value" );

    }

    [Test]
    public void MutableConfigurationSection_cannot_set_a_value_above_an_existing_one()
    {
        var c = new MutableConfigurationSection( "X" );
        c["Z:A:B:C"] = "Another Value";
        Util.Invokable( () => c["Z:A:B"] = "No way" )
            .ShouldThrow<InvalidOperationException>()
                     .Message.ShouldBe( "Unable to set 'X:Z:A:B' value to 'No way' since at least 'X:Z:A:B:C' (with value 'Another Value') exists below." );

        Util.Invokable( () => c["Z"] = "No way" )
                     .ShouldThrow<InvalidOperationException>();
    }

    [Test]
    public void MutableConfigurationSection_cannot_set_a_value_below_an_existing_one()
    {
        var c = new MutableConfigurationSection( "Root" );
        c["Z"] = "A top Value";
        Util.Invokable( () => c["Z:A"] = "No way" )
            .ShouldThrow<InvalidOperationException>()
                     .Message.ShouldBe( "Unable to set 'Root:Z:A' value to 'No way' since 'Root:Z' above has value 'A top Value'." );

        Util.Invokable( () => c["Z:X:Y:Z"] = "No way 2" )
                     .ShouldThrow<InvalidOperationException>()
                     .Message.ShouldBe( "Unable to set 'Root:Z:X:Y:Z' value to 'No way 2' since 'Root:Z' above has value 'A top Value'." );

        // Free the Z!
        c["Z"] = null;
        c["Z:A"] = "It works now!";
        c["Z:X:Y:Z"] = "It works also here.";
    }

    [Test]
    public void MutableConfigurationSections_when_inexisting_dont_prevent_setting_a_value_above()
    {
        var c = new MutableConfigurationSection( "X" );
        var empty1 = c.GetMutableSection( "A:A:A" );
        var empty2 = c.GetMutableSection( "A:B:C" );
        var empty3 = c.GetMutableSection( "A:B:C:D:E:F" );

        c["A"] = "It works!";

        Util.Invokable( () => empty1.Value = "Pouf" )
            .ShouldThrow<InvalidOperationException>( "Sections MUST remain empty when below a value." )
            .Message.ShouldBe( "Unable to set 'X:A:A:A' value to 'Pouf' since 'X:A' above has value 'It works!'." );
    }

    [Test]
    public void ImmutableConfigurationSection_LookupAllSection_finds_sub_keys()
    {
        var c = new MutableConfigurationSection( "X" );
        c["Key"] = "Key";
        c["A:Key"] = "A-Key";
        c["A:A:Key"] = "A-A-Key";
        c["A:A:A:Key"] = "A-A-A-Key";
        c["A:A:A:A:Key"] = "A-A-A-A-Key";

        var i = new ImmutableConfigurationSection( c );
        var deepest = i.GetSection( "A:A:A:A" );
        deepest.LookupAllSection( "Key" ).Select( s => s.Value ).Concatenate()
            .ShouldBe( "Key, A-Key, A-A-Key, A-A-A-Key, A-A-A-A-Key" );
    }

    [Test]
    public void ImmutableConfigurationSection_LookupAllSection_finds_sub_paths()
    {
        var c = new MutableConfigurationSection( "X" );
        c["In:Key"] = "Key";
        c["A:In:Key"] = "A-Key";
        c["A:A:In:Key"] = "A-A-Key";
        c["A:A:A:In:Key"] = "A-A-A-Key";
        c["A:A:A:A:In:Key"] = "A-A-A-A-Key";

        var i = new ImmutableConfigurationSection( c );
        var deepest = i.GetSection( "A:A:A:A" );
        deepest.LookupAllSection( "In:Key" ).Select( s => s.Value ).Concatenate()
            .ShouldBe( "Key, A-Key, A-A-Key, A-A-A-Key, A-A-A-A-Key" );
    }

    [Test]
    public void ImmutableConfigurationSection_LookupAllSection_skips_sections_on_the_search_path_by_default()
    {
        {
            var c = new MutableConfigurationSection( "X" );
            c["Key"] = "Key";
            c["A:Key:Key"] = "A-Key";
            c["A:Key:A:Key"] = "A-A-Key";
            c["A:Key:A:A:Key"] = "A-A-A-Key";
            c["A:Key:A:A:A:Key"] = "A-A-A-A-Key";

            var i = new ImmutableConfigurationSection( c );
            var deepest = i.GetSection( "A:Key:A:A:A" );
            deepest.LookupAllSection( "Key" ).Select( s => s.Value ?? s.Path ).Concatenate()
                .ShouldBe( "Key, A-Key, A-A-Key, A-A-A-Key, A-A-A-A-Key" );
        }
        {
            var c = new MutableConfigurationSection( "X" );
            c["In:Key"] = "Key";
            c["A:In:Key:In:Key"] = "A-Key";
            c["A:In:Key:A:In:Key"] = "A-A-Key";
            c["A:In:Key:A:A:In:Key"] = "A-A-A-Key";
            c["A:In:Key:A:A:A:In:Key"] = "A-A-A-A-Key";

            var i = new ImmutableConfigurationSection( c );
            var deepest = i.GetSection( "A:In:Key:A:A:A" );
            deepest.LookupAllSection( "In:Key" ).Select( s => s.Value ?? s.Path ).Concatenate()
                .ShouldBe( "Key, A-Key, A-A-Key, A-A-A-Key, A-A-A-A-Key" );
        }
    }

    [Test]
    public void ImmutableConfigurationSection_LookupAllSection_can_return_sections_on_the_search_path()
    {
        {
            var c = new MutableConfigurationSection( "X" );
            c["Key"] = "Key";
            c["A:Key:Key"] = "A-Key";
            c["A:Key:A:Key"] = "A-A-Key";
            c["A:Key:A:A:Key"] = "A-A-A-Key";
            c["A:Key:A:A:A:Key"] = "A-A-A-A-Key";

            var i = new ImmutableConfigurationSection( c );
            var deepest = i.GetSection( "A:Key:A:A:A" );
            deepest.LookupAllSection( "Key", skipSectionsOnThisPath: false ).Select( s => s.Value ?? s.Path ).Concatenate()
                .ShouldBe( "Key, X:A:Key, A-Key, A-A-Key, A-A-A-Key, A-A-A-A-Key" );
        }
        {
            var c = new MutableConfigurationSection( "X" );
            c["In:Key"] = "Key";
            c["A:In:Key:In:Key"] = "A-Key";
            c["A:In:Key:A:In:Key"] = "A-A-Key";
            c["A:In:Key:A:A:In:Key"] = "A-A-A-Key";
            c["A:In:Key:A:A:A:In:Key"] = "A-A-A-A-Key";

            var i = new ImmutableConfigurationSection( c );
            var deepest = i.GetSection( "A:In:Key:A:A:A" );
            deepest.LookupAllSection( "In:Key", skipSectionsOnThisPath: false ).Select( s => s.Value ?? s.Path ).Concatenate()
                .ShouldBe( "Key, X:A:In:Key, A-Key, A-A-Key, A-A-A-Key, A-A-A-A-Key" );
        }
    }

    [Test]
    public void adding_json_configuration()
    {
        var c = new MutableConfigurationSection( "Root" );
        c.Exists().ShouldBeFalse();
        c.AddJson( "{}" );
        c.Exists().ShouldBeFalse();

        Util.Invokable( () => c.AddJson( null! ) ).ShouldThrow<ArgumentNullException>();
        Util.Invokable( () => c.AddJson( "" ) ).ShouldThrow<JsonException>();
        Util.Invokable( () => c.AddJson( "e" ) ).ShouldThrow<JsonException>();
        Util.Invokable( () => c.AddJson( "{ " ) ).ShouldThrow<JsonException>();
        c.Exists().ShouldBeFalse();
        // The "p" property has bee added...
        // Preventing this wold require to duplicate the whole structure and to wait until
        // the last moment to alter the MutableConfigurationSection. This is costly and we
        // prefer to keep the side effect.
        Util.Invokable( () => c.AddJson( """{ "p": """ ) ).ShouldThrow<JsonException>();
        c.Exists().ShouldBeTrue();

        c.AddJson( """{ "S": "A" }""" );
        c["S"].ShouldBe( "A" );

        c.AddJson( """{ "Num": 0.258e7 }""" );
        c["Num"].ShouldBe( "0.258e7" );

        c.AddJson( """{ "F": false }""" );
        c["F"].ShouldBe( "False" );

        c.AddJson( """{ "F": true }""" );
        c["F"].ShouldBe( "True" );

        c.AddJson( """{ "N": null }""" );
        c["N"].ShouldBeNull();
        c.GetMutableChildren().Single( sub => sub.Key == "N" ).Value.ShouldBeNull();
        c.GetMutableChildren().Single( sub => sub.Key == "N" ).Exists().ShouldBeFalse();

        c.AddJson( """{ "EmptyArray": [] }""" );
        c["EmptyArray"].ShouldBeNull();
        c.GetMutableChildren().Single( sub => sub.Key == "EmptyArray" ).Value.ShouldBeNull();
        c.GetMutableChildren().Single( sub => sub.Key == "EmptyArray" ).Exists().ShouldBeFalse();

        c.AddJson( """{ "O": { "One": 1, "Two": 2.0, "Three": "trois" } }""" );
        c["O:One"].ShouldBe( "1" );
        c["O:Two"].ShouldBe( "2.0" );
        c["O:Three"].ShouldBe( "trois" );

        c.AddJson( """{ "O": { "One": "1bis", "Two": 2.1, "Three": "trois.1", "AString": ["a","b", "cde", null, 12], "AO": [{ "In": true }, { "Out": 3712 } ]  } }""" );
        c["O:One"].ShouldBe( "1bis" );
        c["O:Two"].ShouldBe( "2.1" );
        c["O:Three"].ShouldBe( "trois.1" );
        c["O:AString:0"].ShouldBe( "a" );
        c["O:AString:1"].ShouldBe( "b" );
        c["O:AString:2"].ShouldBe( "cde" );
        c["O:AString:3"].ShouldBeNull();
        c["O:AString:4"].ShouldBe( "12" );
        c["O:AO:0:In"].ShouldBe( "True" );
        c["O:AO:1:Out"].ShouldBe( "3712" );
    }

    [Test]
    public void adding_json_configuration_can_skip_comments_when_they_are_allowed()
    {
        var c = new MutableConfigurationSection( "Root" );
        var r = new Utf8JsonReader( Encoding.UTF8.GetBytes( """
                                                            // c1
                                                            { /*c2*/ "A"
                                                            : /*c5*/"V" //c6
                                                            //c7
                                                            "B":true }

                                                            """ ),
                                    new JsonReaderOptions { CommentHandling = JsonCommentHandling.Allow } );
        c.AddJson( ref r );
        c["A"].ShouldBe( "V" );
        c["B"].ShouldBe( "True" );
    }

    [Test]
    public void empty_json_objects_are_read_and_result_in_inexisting_sections()
    {
        // We allow the trailing commas to appear when reading from a string.
        var c = new MutableConfigurationSection( "Root" )
                .AddJson( """
                          {
                            "Unexisting": {},
                          }
                          """ );
        c.GetMutableChildren().Count.ShouldBe( 1 );
        ((IConfigurationSection)c).GetChildren().Count().ShouldBe( 1 );

        var iC = new ImmutableConfigurationSection( c );

        iC.TryGetSection( "Unexisting" ).ShouldNotBeNull();
        iC.TryGetSection( "Unexisting" ).Exists().ShouldBeFalse();
    }

    [Test]
    public void ShouldApplyConfiguration_works()
    {
        var c = new MutableConfigurationSection( "Root" )
                .AddJson( """
                          {
                            "ExplicitTrue": true,
                            "ExplicitFalse": false,
                            "Existing": { "SomeValue": 0 }
                          }
                          """ );

        var iC = new ImmutableConfigurationSection( c );

        // Checks correct type inference.
        c.ShouldApplyConfiguration( "Existing", true, out var interfaceContent ).ShouldBeTrue();
        interfaceContent.ShouldBeOfType<MutableConfigurationSection>();
        iC.ShouldApplyConfiguration( "Existing", true, out var immutableContent ).ShouldBeTrue();
        immutableContent.ShouldBeOfType<ImmutableConfigurationSection>();

        c.ShouldApplyConfiguration( "Existing", false, out interfaceContent ).ShouldBeTrue();
        interfaceContent.ShouldNotBeNull();
        iC.ShouldApplyConfiguration( "Existing", false, out immutableContent ).ShouldBeTrue();
        immutableContent.ShouldNotBeNull();

        c.ShouldApplyConfiguration( "ExplicitTrue", true, out interfaceContent ).ShouldBeTrue();
        interfaceContent.ShouldBeNull();
        iC.ShouldApplyConfiguration( "ExplicitTrue", true, out immutableContent ).ShouldBeTrue();
        immutableContent.ShouldBeNull();
        c.ShouldApplyConfiguration( "ExplicitTrue", false, out interfaceContent ).ShouldBeTrue();
        interfaceContent.ShouldBeNull();
        iC.ShouldApplyConfiguration( "ExplicitTrue", false, out immutableContent ).ShouldBeTrue();
        immutableContent.ShouldBeNull();

        c.ShouldApplyConfiguration( "ExplicitFalse", true, out interfaceContent ).ShouldBeFalse();
        interfaceContent.ShouldBeNull();
        iC.ShouldApplyConfiguration( "ExplicitFalse", true, out immutableContent ).ShouldBeFalse();
        immutableContent.ShouldBeNull();
        c.ShouldApplyConfiguration( "ExplicitFalse", false, out interfaceContent ).ShouldBeFalse();
        interfaceContent.ShouldBeNull();
        iC.ShouldApplyConfiguration( "ExplicitFalse", false, out immutableContent ).ShouldBeFalse();
        immutableContent.ShouldBeNull();

        c.ShouldApplyConfiguration( "Unexisting", true, out interfaceContent ).ShouldBeTrue();
        interfaceContent.ShouldBeNull();
        iC.ShouldApplyConfiguration( "Unexisting", true, out immutableContent ).ShouldBeTrue();
        immutableContent.ShouldBeNull();
        c.ShouldApplyConfiguration( "Unexisting", false, out interfaceContent ).ShouldBeFalse();
        interfaceContent.ShouldBeNull();
        iC.ShouldApplyConfiguration( "Unexisting", false, out immutableContent ).ShouldBeFalse();
        immutableContent.ShouldBeNull();



    }
}

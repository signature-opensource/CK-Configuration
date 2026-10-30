using CK.Core;
using NUnit.Framework;
using Shouldly;
using static CK.Testing.MonitorTestHelper;

namespace CK.Configuration.Tests;

[TestFixture]
public class ReadStringArrayTests
{
    [TestCase( "A,B,C" )]
    [TestCase( "A, B, C" )]
    [TestCase( " A ,B , C " )]
    [TestCase( "A,, B,  ,C" )]
    public void a_comma_separated_value_is_trimmed_and_empty_entries_are_skipped( string value )
    {
        var m = new MutableConfigurationSection( "Root" );
        m["Names"] = value;
        var c = new ImmutableConfigurationSection( m );
        c.ReadStringArray( TestHelper.Monitor, "Names" ).ShouldBe( new[] { "A", "B", "C" } );
        c.ReadUniqueStringSet( TestHelper.Monitor, "Names" ).ShouldBe( new[] { "A", "B", "C" }, ignoreOrder: true );
    }

    [Test]
    public void the_items_of_an_array_are_trimmed_and_may_be_comma_separated()
    {
        var m = new MutableConfigurationSection( "Root" );
        m["Names:0"] = " A ";
        m["Names:1"] = "B, C";
        m["Names:2"] = " ";
        var c = new ImmutableConfigurationSection( m );
        c.ReadStringArray( TestHelper.Monitor, "Names" ).ShouldBe( new[] { "A", "B", "C" } );
    }
}

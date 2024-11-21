using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static CK.Testing.MonitorTestHelper;

namespace CK.Configuration.Tests;

/// <summary>
/// Base class. Does nothing special.
/// </summary>
public abstract class TestConfigurationBase
{
    readonly string _configurationPath;

    public TestConfigurationBase( string configurationPath )
    {
        _configurationPath = configurationPath;
    }

    public string ConfigurationPath => _configurationPath;

    public static void AddResolver( TypedConfigurationBuilder builder )
    {
        var r = new TypedConfigurationBuilder.StandardTypeResolver(
            typeof( TestConfigurationBase ),
            "CK.Configuration.Tests",
            defaultCompositeBaseType: typeof( DefaultCompositeConfiguration ) );
        builder.AddResolver( r );
    }
}

/// <summary>
/// DefaultComposite has ImmutableArray<TestConfigurationBase> Items.
/// </summary>
public class DefaultCompositeConfiguration : TestConfigurationBase
{
    readonly ImmutableArray<TestConfigurationBase> _items;

    public DefaultCompositeConfiguration( IActivityMonitor monitor,
                                          TypedConfigurationBuilder builder,
                                          ImmutableConfigurationSection configuration,
                                          IReadOnlyList<TestConfigurationBase> items )
        : this( configuration.Path, items.ToImmutableArray() )
    {
    }

    internal DefaultCompositeConfiguration( string configurationPath, ImmutableArray<TestConfigurationBase> items )
        : base( configurationPath )
    {
        _items = items;
    }

    public ImmutableArray<TestConfigurationBase> Items => _items;
}

/// <summary>
/// Does nothing special.
/// </summary>
public class SimpleConfiguration : TestConfigurationBase
{
    public SimpleConfiguration( IActivityMonitor monitor,
                                TypedConfigurationBuilder builder,
                                ImmutableConfigurationSection configuration )
        : base( configuration.Path )
    {
        var e = configuration["CtorError"];
        if( e != null )
        {
            monitor.Error( e );
        }
    }
}

/// <summary>
/// A collection of SimpleConfiguration.
/// </summary>
public class SimpleCollectionConfiguration : TestConfigurationBase
{
    readonly ImmutableArray<SimpleConfiguration> _simpleItems;

    public SimpleCollectionConfiguration( IActivityMonitor monitor,
                                          TypedConfigurationBuilder builder,
                                          ImmutableConfigurationSection configuration,
                                          IReadOnlyList<SimpleConfiguration> simpleItems )
        : base( configuration.Path )
    {
        _simpleItems = simpleItems.ToImmutableArray();
        var e = configuration["CtorError"];
        if( e != null )
        {
            monitor.Error( e );
        }
    }

    public ImmutableArray<SimpleConfiguration> SimpleItems => _simpleItems;
}


/// <summary>
/// This one has non empty ImmutableArray<SimpleCollectionConfiguration> SimpleItems
/// and a ImmutableArray<TestConfigurationBase> Items: it is like a composite. (we may have specialized
/// DefaultCompositeConfiguration instead of duplicating it).
/// Use the Create factory method to return a MoreComplexConfiguration or a DefaultCompositeConfiguration.
/// </summary>
public class MoreComplexConfiguration : TestConfigurationBase
{
    readonly ImmutableArray<SimpleCollectionConfiguration> _simpleItems;
    readonly ImmutableArray<TestConfigurationBase> _items;

    MoreComplexConfiguration( string configurationPath,
                              ImmutableArray<SimpleCollectionConfiguration> simpleItems,
                              ImmutableArray<TestConfigurationBase> items )
        : base( configurationPath )
    {
        _simpleItems = simpleItems;
        _items = items;
    }

    public ImmutableArray<SimpleCollectionConfiguration> SimpleItems => _simpleItems;

    public ImmutableArray<TestConfigurationBase> Items => _items;

    public static TestConfigurationBase? Create( IActivityMonitor monitor,
                                                 TypedConfigurationBuilder builder,
                                                 ImmutableConfigurationSection configuration )
    {
        IReadOnlyList<SimpleCollectionConfiguration>? subs = null;

        var e = configuration["CtorError"];
        if( e != null )
        {
            monitor.Error( e );
        }

        var simpleItems = configuration.TryGetSection( "SimpleItems" );
        if( simpleItems != null )
        {
            subs = builder.CreateItems<SimpleCollectionConfiguration>( monitor,
                                                                       simpleItems );
            if( subs == null ) return null;
            if( subs.Count == 0 ) subs = null;
        }
        var items = builder.FindItemsSectionAndCreateItems<TestConfigurationBase>( monitor, configuration );
        if( items == null ) return null;

        if( subs != null )
        {
            return new MoreComplexConfiguration( configuration.Path, subs.ToImmutableArray(), items.ToImmutableArray() );
        }
        return new DefaultCompositeConfiguration( configuration.Path, items.ToImmutableArray() );
    }

}

[TestFixture]
public class CompositeTests
{
    [Test]
    public void empty_default_root()
    {
        var config = ImmutableConfigurationSection.CreateFromJson( "Root",
            """
            {
                "DefaultAssembly": "CK.Configuration.Tests",
                "Items": []
            }
            """ );
        var builder = new TypedConfigurationBuilder();
        TestConfigurationBase.AddResolver( builder );
        var e = (DefaultCompositeConfiguration)builder.Create<TestConfigurationBase>( TestHelper.Monitor, config )!;
        e.Items.Should().BeEmpty();
    }

    [Test]
    public void SimpleCollection_at_root()
    {
        var config = ImmutableConfigurationSection.CreateFromJson( "Root",
            """
            {
                "DefaultAssembly": "CK.Configuration.Tests",
                "Type": "SimpleCollection",
                "SimpleItems": [
                    { "Type": "Simple" },
                    { "Type": "Simple" }
                ],
                // This one is ignored: the SimpleCollection constructor parameter name is "simpleItems"...
                "Items": [
                    { "Type": "Simple" }
                ]
            }
            """ );
        var builder = new TypedConfigurationBuilder();
        TestConfigurationBase.AddResolver( builder );
        var e = (SimpleCollectionConfiguration)builder.Create<TestConfigurationBase>( TestHelper.Monitor, config )!;
        e.SimpleItems.Should().HaveCount( 2 );
    }

    [Test]
    public void factory_Create_can_return_different_types()
    {
        var config = ImmutableConfigurationSection.CreateFromJson( "Root",
            """
            {
                "DefaultAssembly": "CK.Configuration.Tests",
                "Items": [
                    {
                        // This one will be a MoreComplexConfiguration.
                        "Type": "MoreComplex",
                        "SimpleItems": [
                            {
                                "Type": "SimpleCollection",
                                "SimpleItems": [
                                    { "Type": "Simple" },
                                    { "Type": "Simple" }
                                ],
                            },
                            { "Type": "SimpleCollection" }
                        ],
                        "Items": [
                            { "Type": "Simple" }
                        ]
                    },
                    {
                        // This one will be a DefaultCompositeConfiguration.
                        "Type": "MoreComplex",
                        "Items": [
                            { "Type": "Simple" }
                        ]
                    }
                ]
            }
            """ );
        var builder = new TypedConfigurationBuilder();
        TestConfigurationBase.AddResolver( builder );
        var e = (DefaultCompositeConfiguration)builder.Create<TestConfigurationBase>( TestHelper.Monitor, config )!;
        e.Items.Should().HaveCount( 2 );
        e.Items[0].Should().BeAssignableTo<MoreComplexConfiguration>();
        e.Items[1].Should().NotBeAssignableTo<MoreComplexConfiguration>()
                           .And.BeAssignableTo<DefaultCompositeConfiguration>();
    }

    [Test]
    public void error_in_constructor_returns_null_and_trigger_no_exception()
    {
        var config = ImmutableConfigurationSection.CreateFromJson( "Root",
            """
            {
                "DefaultAssembly": "CK.Configuration.Tests",
                "Type": "SimpleCollection",
                "CtorError": "Pouf!",
                "SimpleItems": [
                    { "Type": "Simple" },
                    { "Type": "Simple" }
                ],
                // This one is ignored: the SimpleCollection constructor parameter name is "simpleItems"...
                "Items": [
                    { "Type": "Simple" }
                ]
            }
            """ );
        var builder = new TypedConfigurationBuilder();
        TestConfigurationBase.AddResolver( builder );
        using( TestHelper.Monitor.CollectEntries( out var logs ) )
        {
            builder.Create<TestConfigurationBase>( TestHelper.Monitor, config ).Should().BeNull();
            logs.Where( e => e.Exception != null ).Should().BeEmpty();
        }
    }

    [Test]
    public void error_in_subordinated_constructor_returns_null_and_trigger_no_exception()
    {
        var config = ImmutableConfigurationSection.CreateFromJson( "Root",
            """
            {
                "DefaultAssembly": "CK.Configuration.Tests",
                "Type": "SimpleCollection",
                "SimpleItems": [
                    { "Type": "Simple" },
                    {
                        "Type": "Simple",
                        "CtorError": "Pouf!"
                    }
                ],
                // This one is ignored: the SimpleCollection constructor parameter name is "simpleItems"...
                "Items": [
                    { "Type": "Simple" }
                ]
            }
            """ );
        var builder = new TypedConfigurationBuilder();
        TestConfigurationBase.AddResolver( builder );
        using( TestHelper.Monitor.CollectEntries( out var logs ) )
        {
            builder.Create<TestConfigurationBase>( TestHelper.Monitor, config ).Should().BeNull();
            logs.Where( e => e.Exception != null ).Should().BeEmpty();
        }
    }


}

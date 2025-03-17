using CK.Core;
using Shouldly;
using NUnit.Framework;
using StrategyPlugin;
using static CK.Testing.MonitorTestHelper;

namespace CK.Configuration.Tests;

[TestFixture]
public class TypedConfigurationBuilderTests
{
    [Test]
    public void simple_configuration_with_DefaultAssembly()
    {
        var config = ImmutableConfigurationSection.CreateFromJson( "Root",
                $$"""
                {
                    "DefaultAssembly": "ConsumerA.Strategy",
                    "S1": {
                        "Type": "Simple",
                        "Action": "hello World!"
                    },
                    "S2": {
                        "Type": "AnotherSimpleStrategy"
                    },
                    "S3": {
                        "Type": "AnotherSimpleStrategyConfiguration"
                    }
                }
                """ );
        var builder = new TypedConfigurationBuilder();
        IStrategyConfiguration.AddResolverWithoutComposite( builder );
        builder.AssemblyConfiguration = AssemblyConfiguration.Create( TestHelper.Monitor, config ) ?? AssemblyConfiguration.Empty;
        var s1C = builder.Create<IStrategyConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "S1" ) );
        var s2C = builder.Create<IStrategyConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "S2" ) );
        var s3C = builder.Create<IStrategyConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "S3" ) );

        Throw.DebugAssert( s1C != null );
        Throw.DebugAssert( s2C != null );
        Throw.DebugAssert( s3C != null );

        var s1 = s1C.CreateStrategy( TestHelper.Monitor );
        Throw.DebugAssert( s1 != null );
        s1.GetType().FullName.ShouldBe( "ConsumerA.SimpleStrategy" );
        s1.GetType().Assembly.GetName().Name.ShouldBe( "ConsumerA.Strategy" );

        var s2 = s2C.CreateStrategy( TestHelper.Monitor );
        Throw.DebugAssert( s2 != null );
        s2.GetType().FullName.ShouldBe( "Plugin.Strategy.AnotherSimpleStrategy" );
        s2.GetType().Assembly.GetName().Name.ShouldBe( "ConsumerA.Strategy" );

        var s3 = s3C.CreateStrategy( TestHelper.Monitor );
        Throw.DebugAssert( s3 != null );
        s3.GetType().FullName.ShouldBe( "Plugin.Strategy.AnotherSimpleStrategy" );
        s3.GetType().Assembly.GetName().Name.ShouldBe( "ConsumerA.Strategy" );
    }

    [TestCase( """["ConsumerA.Strategy", "ConsumerB.Plugins"]""" )]
    [TestCase( """[{ "Assembly": "ConsumerA.Strategy" }, { "Assembly": "ConsumerB.Plugins"}]""" )]
    public void simple_configuration_with_multiple_Assemblies( string assemblies )
    {
        var config = ImmutableConfigurationSection.CreateFromJson( "Root",
                $$"""
                {
                    "Assemblies": {{assemblies}},
                    "S1": {
                        "Type": "Simple, ConsumerA.Strategy",
                        "Action": "hello World!"
                    },
                    "S2": {
                        "Type": "Simple, ConsumerB.Plugins"
                    },
                    "S3": {
                        "Type": "AnotherSimpleStrategyConfiguration, ConsumerA.Strategy"
                    }
                }
                """ );
        var builder = new TypedConfigurationBuilder();
        IStrategyConfiguration.AddResolverWithoutComposite( builder );
        builder.AssemblyConfiguration = AssemblyConfiguration.Create( TestHelper.Monitor, config ) ?? AssemblyConfiguration.Empty;
        var s1C = builder.Create<IStrategyConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "S1" ) );
        var s2C = builder.Create<IStrategyConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "S2" ) );
        var s3C = builder.Create<IStrategyConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "S3" ) );

        Throw.DebugAssert( s1C != null );
        Throw.DebugAssert( s2C != null );
        Throw.DebugAssert( s3C != null );

        var s1 = s1C.CreateStrategy( TestHelper.Monitor );
        Throw.DebugAssert( s1 != null );
        s1.GetType().FullName.ShouldBe( "ConsumerA.SimpleStrategy" );
        s1.GetType().Assembly.GetName().Name.ShouldBe( "ConsumerA.Strategy" );

        var s2 = s2C.CreateStrategy( TestHelper.Monitor );
        Throw.DebugAssert( s2 != null );
        s2.GetType().FullName.ShouldBe( "ConsumerB.SimpleStrategy" );
        s2.GetType().Assembly.GetName().Name.ShouldBe( "ConsumerB.Plugins" );

        var s3 = s3C.CreateStrategy( TestHelper.Monitor );
        Throw.DebugAssert( s3 != null );
        s3.GetType().FullName.ShouldBe( "Plugin.Strategy.AnotherSimpleStrategy" );
        s3.GetType().Assembly.GetName().Name.ShouldBe( "ConsumerA.Strategy" );
    }

    [TestCase( """
               {
                 "ConsumerA.Strategy": "C1",
                 "ConsumerB.Plugins": "C2"
               }
               """ )]
    [TestCase( """
                [
                    { "Assembly": "ConsumerA.Strategy", "Alias":"C1" },
                    { "Assembly": "ConsumerB.Plugins", "Alias": "C2"}
                ]
                """ )]
    public void simple_configuration_with_multiple_Assemblies_and_aliases( string assemblies )
    {
        var config = ImmutableConfigurationSection.CreateFromJson( "Root",
                $$"""
                {
                    "Assemblies": {{assemblies}},
                    "S1": {
                        "Type": "Simple, C1",
                        "Action": "hello World!"
                    },
                    "S2": {
                        "Type": "Simple, C2"
                    },
                    "S3": {
                        "Type": "AnotherSimpleStrategyConfiguration, C1"
                    }
                }
                """ );
        var builder = new TypedConfigurationBuilder();
        IStrategyConfiguration.AddResolverWithoutComposite( builder );
        builder.AssemblyConfiguration = AssemblyConfiguration.Create( TestHelper.Monitor, config ) ?? AssemblyConfiguration.Empty;
        var s1C = builder.Create<IStrategyConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "S1" ) );
        var s2C = builder.Create<IStrategyConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "S2" ) );
        var s3C = builder.Create<IStrategyConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "S3" ) );

        Throw.DebugAssert( s1C != null );
        Throw.DebugAssert( s2C != null );
        Throw.DebugAssert( s3C != null );

        var s1 = s1C.CreateStrategy( TestHelper.Monitor );
        Throw.DebugAssert( s1 != null );
        s1.GetType().FullName.ShouldBe( "ConsumerA.SimpleStrategy" );
        s1.GetType().Assembly.GetName().Name.ShouldBe( "ConsumerA.Strategy" );

        var s2 = s2C.CreateStrategy( TestHelper.Monitor );
        Throw.DebugAssert( s2 != null );
        s2.GetType().FullName.ShouldBe( "ConsumerB.SimpleStrategy" );
        s2.GetType().Assembly.GetName().Name.ShouldBe( "ConsumerB.Plugins" );

        var s3 = s3C.CreateStrategy( TestHelper.Monitor );
        Throw.DebugAssert( s3 != null );
        s3.GetType().FullName.ShouldBe( "Plugin.Strategy.AnotherSimpleStrategy" );
        s3.GetType().Assembly.GetName().Name.ShouldBe( "ConsumerA.Strategy" );
    }

    [Test]
    public void composite_at_root()
    {
        var config = ImmutableConfigurationSection.CreateFromJson( "Root",
            """
            {
                "Assemblies":
                [
                    { "Assembly": "ConsumerA.Strategy", "Alias":"C1" },
                    { "Assembly": "ConsumerB.Plugins", "Alias": "C2" }
                ],
                "Strategies":
                [
                    {
                        "Type": "Simple, C1",
                        "Action": "hello World!"
                    },
                    {
                        "Type": "Simple, C2"
                    },
                    {
                        "Type": "AnotherSimpleStrategyConfiguration, C1"
                    }
                ]
            }
            """ );
        var builder = new TypedConfigurationBuilder();
        IStrategyConfiguration.AddResolver( builder );
        var sC = builder.Create<IStrategyConfiguration>( TestHelper.Monitor, config );
        Throw.DebugAssert( sC != null );
        var s = sC.CreateStrategy( TestHelper.Monitor );
        Throw.DebugAssert( s != null );
        s.DoSomething( TestHelper.Monitor, 0 ).ShouldBe( 3 );
    }

    [Test]
    public void composite_at_root_with_sub_composite()
    {
        var config = ImmutableConfigurationSection.CreateFromJson( "Root",
            """
            {
                "Assemblies":
                [
                    { "Assembly": "ConsumerA.Strategy", "Alias":"C1" },
                    { "Assembly": "ConsumerB.Plugins", "Alias": "C2" }
                ],
                "Strategies":
                [
                    {
                        "Type": "Simple, C1",
                        "Action": "hello World!"
                    },
                    {
                        "Type": "Simple, C2"
                    },
                    {
                        "Strategies":
                        [
                            {
                                "Type": "Simple, C1",
                                "Action": "hello again..."
                            },
                            {
                                "Type": "Simple, C2"
                            }
                        ]
                    },
                    {
                        "Type": "AnotherSimpleStrategyConfiguration, C1"
                    }
                ]
            }
            """ );
        var builder = new TypedConfigurationBuilder();
        builder.AddResolver( new TypedConfigurationBuilder.StandardTypeResolver(
                                    baseType: typeof( IStrategyConfiguration ),
                                    typeNamespace: "Plugin.Strategy",
                                    allowOtherNamespace: false,
                                    familyTypeNameSuffix: "Strategy",
                                    defaultCompositeBaseType: typeof( CompositeStrategyConfiguration ),
                                    compositeItemsFieldName: "Strategies" ) );
        var sC = builder.Create<IStrategyConfiguration>( TestHelper.Monitor, config );
        Throw.DebugAssert( sC != null );
        var s = sC.CreateStrategy( TestHelper.Monitor );
        Throw.DebugAssert( s != null );
        s.DoSomething( TestHelper.Monitor, 0 )
            .ShouldBe( 5, "Each 'Simple' and 'AnotherSimple' increments the value." );
    }


    [TestCase( false )]
    [TestCase( true )]
    public void composite_specialized_in_composite( bool halfRun )
    {
        var config = ImmutableConfigurationSection.CreateFromJson( "Root",
                $$"""
                {
                    "Assemblies":
                    [
                        { "Assembly": "ConsumerA.Strategy", "Alias":"C1" },
                        { "Assembly": "ConsumerB.Plugins", "Alias": "C2" }
                    ],
                    "Strategies":
                    [
                        {
                            "Type": "Simple, C1",
                            "Action": "hello World!"
                        },
                        {
                            "Type": "Simple, C2"
                        },
                        {
                            "Type": "OneOutOfTwo, C2",
                            "HalfRun": {{(halfRun ? "true" : "false")}},
                            "Strategies": [
                                {
                                    "Type": "Simple, C1"
                                },
                                {
                                    "Type": "AnotherSimpleStrategyConfiguration, C1"
                                },
                                {
                                    "Type": "Simple, C1"
                                },
                                {
                                    "Type": "Simple, C1"
                                }
                            ]
                        }
                    ]
                }
                """ );
        var builder = new TypedConfigurationBuilder();
        IStrategyConfiguration.AddResolver( builder );
        var sC = builder.Create<IStrategyConfiguration>( TestHelper.Monitor, config );
        Throw.DebugAssert( sC != null );
        var s = sC.CreateStrategy( TestHelper.Monitor );
        Throw.DebugAssert( s != null );
        s.DoSomething( TestHelper.Monitor, 0 ).ShouldBe( halfRun ? 4 : 6 );
    }

    [Test]
    public void composite_like_component()
    {
        var config = ImmutableConfigurationSection.CreateFromJson( "Root",
            """
            {
                "DefaultAssembly": "ConsumerB.Plugins",
                "Strategies":
                [
                    {
                        "Type": "CompositeLike",
                        "Before": [
                            {
                                "Type": "Simple"
                            },
                            {
                                "Type": "Simple"
                            }
                        ],
                        "After": [
                            {
                                "Type": "Simple"
                            },
                            {
                                "Type": "Simple"
                            }
                        ]
                    },
                    {
                        "Type": "Simple"
                    }
                ]
            }
            """ );
        var builder = new TypedConfigurationBuilder();
        IStrategyConfiguration.AddResolver( builder );
        var sC = builder.Create<IStrategyConfiguration>( TestHelper.Monitor, config );
        Throw.DebugAssert( sC != null );
        var s = sC.CreateStrategy( TestHelper.Monitor );
        Throw.DebugAssert( s != null );
        s.DoSomething( TestHelper.Monitor, 0 ).ShouldBe( 5 );
    }


    [Test]
    public void unsuccessful_Create_returns_null()
    {
        var builder = new TypedConfigurationBuilder();
        IStrategyConfiguration.AddResolver( builder );

        using( TestHelper.Monitor.CollectTexts( out var logs ) )
        {
            var config = ImmutableConfigurationSection.CreateFromJson( "Root", "{}" );
            var sC = builder.Create<IStrategyConfiguration>( TestHelper.Monitor, config );
            sC.ShouldBeNull();
            logs.ShouldContain( "Configuration 'Root' must have children to be considered a default 'CompositeStrategyConfiguration'." );
        }

        using( TestHelper.Monitor.CollectTexts( out var logs ) )
        {
            var config = ImmutableConfigurationSection.CreateFromJson( "Root", """{ "SomeField": "Val" }""" );
            var sC = builder.Create<IStrategyConfiguration>( TestHelper.Monitor, config );
            sC.ShouldBeNull();
            logs.ShouldContain( "Unable to create a 'IStrategyConfiguration' from 'Root:SomeField = Val'." );
        }

        using( TestHelper.Monitor.CollectTexts( out var logs ) )
        {
            var config = ImmutableConfigurationSection.CreateFromJson( "Root", """{ "Strategies": [{}] }""" );
            var sC = builder.Create<IStrategyConfiguration>( TestHelper.Monitor, config );
            sC.ShouldBeNull();
            logs.ShouldContain( "Configuration 'Root:Strategies:0' must have children to be considered a default 'CompositeStrategyConfiguration'." );
        }
    }


}

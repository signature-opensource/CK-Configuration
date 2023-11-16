using CK.Core;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using StrategyPlugin;
using static CK.Testing.MonitorTestHelper;

namespace CK.Configuration.Tests
{
    [TestFixture]
    public class PolymorphicConfigurationTypeBuilderTests
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
            var builder = new PolymorphicConfigurationTypeBuilder();
            IStrategyConfiguration.ConfigureWithoutComposite( builder );
            builder.PushAssemblyConfiguration( TestHelper.Monitor, config );
            builder.TryCreate<IStrategyConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "S1" ), out var s1C ).Should().BeTrue();
            builder.TryCreate<IStrategyConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "S2" ), out var s2C ).Should().BeTrue();
            builder.TryCreate<IStrategyConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "S3" ), out var s3C ).Should().BeTrue();

            Throw.DebugAssert( s1C != null );
            Throw.DebugAssert( s2C != null );
            Throw.DebugAssert( s3C != null );

            var s1 = s1C.CreateStrategy( TestHelper.Monitor );
            Throw.DebugAssert( s1 != null );
            s1.GetType().FullName.Should().Be( "ConsumerA.SimpleStrategy" );
            s1.GetType().Assembly.GetName().Name.Should().Be( "ConsumerA.Strategy" );

            var s2 = s2C.CreateStrategy( TestHelper.Monitor );
            Throw.DebugAssert( s2 != null );
            s2.GetType().FullName.Should().Be( "Plugin.Strategy.AnotherSimpleStrategy" );
            s2.GetType().Assembly.GetName().Name.Should().Be( "ConsumerA.Strategy" );

            var s3 = s3C.CreateStrategy( TestHelper.Monitor );
            Throw.DebugAssert( s3 != null );
            s3.GetType().FullName.Should().Be( "Plugin.Strategy.AnotherSimpleStrategy" );
            s3.GetType().Assembly.GetName().Name.Should().Be( "ConsumerA.Strategy" );
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
            var builder = new PolymorphicConfigurationTypeBuilder();
            IStrategyConfiguration.ConfigureWithoutComposite( builder );
            builder.PushAssemblyConfiguration( TestHelper.Monitor, config );
            builder.TryCreate<IStrategyConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "S1" ), out var s1C ).Should().BeTrue();
            builder.TryCreate<IStrategyConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "S2" ), out var s2C ).Should().BeTrue();
            builder.TryCreate<IStrategyConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "S3" ), out var s3C ).Should().BeTrue();

            Throw.DebugAssert( s1C != null );
            Throw.DebugAssert( s2C != null );
            Throw.DebugAssert( s3C != null );

            var s1 = s1C.CreateStrategy( TestHelper.Monitor );
            Throw.DebugAssert( s1 != null );
            s1.GetType().FullName.Should().Be( "ConsumerA.SimpleStrategy" );
            s1.GetType().Assembly.GetName().Name.Should().Be( "ConsumerA.Strategy" );

            var s2 = s2C.CreateStrategy( TestHelper.Monitor );
            Throw.DebugAssert( s2 != null );
            s2.GetType().FullName.Should().Be( "ConsumerB.SimpleStrategy" );
            s2.GetType().Assembly.GetName().Name.Should().Be( "ConsumerB.Plugins" );

            var s3 = s3C.CreateStrategy( TestHelper.Monitor );
            Throw.DebugAssert( s3 != null );
            s3.GetType().FullName.Should().Be( "Plugin.Strategy.AnotherSimpleStrategy" );
            s3.GetType().Assembly.GetName().Name.Should().Be( "ConsumerA.Strategy" );
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
            var builder = new PolymorphicConfigurationTypeBuilder();
            IStrategyConfiguration.ConfigureWithoutComposite( builder );
            builder.PushAssemblyConfiguration( TestHelper.Monitor, config );
            builder.TryCreate<IStrategyConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "S1" ), out var s1C ).Should().BeTrue();
            builder.TryCreate<IStrategyConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "S2" ), out var s2C ).Should().BeTrue();
            builder.TryCreate<IStrategyConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "S3" ), out var s3C ).Should().BeTrue();

            Throw.DebugAssert( s1C != null );
            Throw.DebugAssert( s2C != null );
            Throw.DebugAssert( s3C != null );

            var s1 = s1C.CreateStrategy( TestHelper.Monitor );
            Throw.DebugAssert( s1 != null );
            s1.GetType().FullName.Should().Be( "ConsumerA.SimpleStrategy" );
            s1.GetType().Assembly.GetName().Name.Should().Be( "ConsumerA.Strategy" );

            var s2 = s2C.CreateStrategy( TestHelper.Monitor );
            Throw.DebugAssert( s2 != null );
            s2.GetType().FullName.Should().Be( "ConsumerB.SimpleStrategy" );
            s2.GetType().Assembly.GetName().Name.Should().Be( "ConsumerB.Plugins" );

            var s3 = s3C.CreateStrategy( TestHelper.Monitor );
            Throw.DebugAssert( s3 != null );
            s3.GetType().FullName.Should().Be( "Plugin.Strategy.AnotherSimpleStrategy" );
            s3.GetType().Assembly.GetName().Name.Should().Be( "ConsumerA.Strategy" );
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
            var builder = new PolymorphicConfigurationTypeBuilder();
            IStrategyConfiguration.Configure( builder );
            builder.PushAssemblyConfiguration( TestHelper.Monitor, config );
            builder.TryCreate<IStrategyConfiguration>( TestHelper.Monitor, config, out var sC ).Should().BeTrue();
            Throw.DebugAssert( sC != null );
            var s = sC.CreateStrategy( TestHelper.Monitor );
            Throw.DebugAssert( s != null );
            s.DoSomething( TestHelper.Monitor, 0 ).Should().Be( 3 );
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
            var builder = new PolymorphicConfigurationTypeBuilder();
            IStrategyConfiguration.Configure( builder );
            builder.PushAssemblyConfiguration( TestHelper.Monitor, config );
            builder.TryCreate<IStrategyConfiguration>( TestHelper.Monitor, config, out var sC ).Should().BeTrue();
            Throw.DebugAssert( sC != null );
            var s = sC.CreateStrategy( TestHelper.Monitor );
            Throw.DebugAssert( s != null );
            s.DoSomething( TestHelper.Monitor, 0 ).Should().Be( halfRun ? 4 : 6 );
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
            var builder = new PolymorphicConfigurationTypeBuilder();
            IStrategyConfiguration.Configure( builder );
            builder.PushAssemblyConfiguration( TestHelper.Monitor, config );
            builder.TryCreate<IStrategyConfiguration>( TestHelper.Monitor, config, out var sC ).Should().BeTrue();
            Throw.DebugAssert( sC != null );
            var s = sC.CreateStrategy( TestHelper.Monitor );
            Throw.DebugAssert( s != null );
            s.DoSomething( TestHelper.Monitor, 0 ).Should().Be( 5 );
        }

    }
}

using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.Object.Transform.Tests
{
    [TestFixture]
    public class TransformTests
    {
        static MutableConfigurationSection GetConfiguration()
        {
            var c = new MutableConfigurationSection( "Root" );
            c.AddJson( """
                {
                    "DefaultAssembly": "CK.Object.Transform.Tests",
                    "Transforms": [
                        {
                            "Transforms": [
                                {
                                    "Type": "ToString",
                                },
                                {
                                    "Type": "AddPrefix",
                                    "Prefix": "Before-"
                                },
                                {
                                    "Type": "AddSuffix",
                                    "Suffix": "-After"
                                }
                            ]
                        },
                        {
                            "Transforms": [
                                {
                                    "Type": "AddSuffix",
                                    "Suffix": "-OneMore"
                                }
                            ]
                        }
                    ]
                }
                """ );
            return c;
        }

        [Test]
        public void basic_tests()
        {
            var builder = new PolymorphicConfigurationTypeBuilder();
            ObjectTransformConfiguration.AddResolver( builder );
            var fC = builder.Create<ObjectTransformConfiguration>( TestHelper.Monitor, GetConfiguration() );
            Throw.DebugAssert( fC != null );
            var f = fC.CreateTransform( TestHelper.Monitor );
            Throw.DebugAssert( f != null );

            f( "Hello" ).Should().Be( "Before-Hello-After-OneMore" );
            f( 0 ).Should().Be( "Before-0-After-OneMore" );
        }

        [Test]
        public async Task basic_tests_Async()
        {
            var builder = new PolymorphicConfigurationTypeBuilder();
            ObjectAsyncTransformConfiguration.AddResolver( builder );

            var fC = builder.Create<ObjectAsyncTransformConfiguration>( TestHelper.Monitor, GetConfiguration() );
            Throw.DebugAssert( fC != null );

            var f = fC.CreateTransform( TestHelper.Monitor );
            Throw.DebugAssert( f != null );
            (await f( "Hello" )).Should().Be( "Before-Hello-After-OneMore" );
            (await f( 0 )).Should().Be( "Before-0-After-OneMore" );
        }

        static MutableConfigurationSection GetStringOnlyConfiguration()
        {
            var c = new MutableConfigurationSection( "Root" );
            c.AddJson( """
                {
                    "DefaultAssembly": "CK.Object.Transform.Tests",
                    "Transforms": [
                        {
                            "Transforms": [
                                {
                                    "Type": "AddPrefix",
                                    "Prefix": "Before-"
                                },
                                {
                                    "Type": "AddSuffix",
                                    "Suffix": "-After"
                                }
                            ]
                        },
                        {
                            "Transforms": [
                                {
                                    "Type": "AddSuffix",
                                    "Suffix": "-OneMore"
                                }
                            ]
                        }
                    ]
                }
                """ );
            return c;
        }



        [Test]
        public void evaluation_hook_finds_error()
        {
            var builder = new PolymorphicConfigurationTypeBuilder();
            ObjectTransformConfiguration.AddResolver( builder );
            var fC = builder.Create<ObjectTransformConfiguration>( TestHelper.Monitor, GetStringOnlyConfiguration() );
            Throw.DebugAssert( fC != null );
            var f = fC.CreateTransform( TestHelper.Monitor );
            Throw.DebugAssert( f != null );

            f( "Works with a string" ).Should().Be( "Before-Works with a string-After-OneMore" );

            FluentActions.Invoking( () => f( 0 ) ).Should().Throw<ArgumentException>();

            var hook = new MonitoredTransformEvaluationHook( TestHelper.Monitor );
            var fH = fC.CreateHook( TestHelper.Monitor, hook );
            Throw.DebugAssert( fH != null );

            fH.Transform( "Works with a string" ).Should().Be( "Before-Works with a string-After-OneMore" );

            using( TestHelper.Monitor.CollectTexts( out var logs ) )
            {
                var r = fH.Transform( 0 );
                r.Should().BeAssignableTo<ArgumentException>();
                ((ArgumentException)r).Message.Should().Be( "String expected, got 'int'." );

                logs.Should().Contain( "Transform 'Root:Transforms:0:Transforms:0' error while processing:" );
            }

        }

        [Test]
        public async Task evaluation_hook_finds_error_Async()
        {
            var builder = new PolymorphicConfigurationTypeBuilder();
            ObjectAsyncTransformConfiguration.AddResolver( builder );
            var fC = builder.Create<ObjectAsyncTransformConfiguration>( TestHelper.Monitor, GetStringOnlyConfiguration() );
            Throw.DebugAssert( fC != null );
            var f = fC.CreateTransform( TestHelper.Monitor );
            Throw.DebugAssert( f != null );

            (await f( "Works with a string" )).Should().Be( "Before-Works with a string-After-OneMore" );

            await FluentActions.Awaiting( async () => await f( 0 ) ).Should().ThrowAsync<ArgumentException>();

            var hook = new MonitoredTransformEvaluationHook( TestHelper.Monitor );
            var fH = fC.CreateHook( TestHelper.Monitor, hook );
            Throw.DebugAssert( fH != null );

            (await fH.TransformAsync( "Works with a string" ) ).Should().Be( "Before-Works with a string-After-OneMore" );

            using( TestHelper.Monitor.CollectTexts( out var logs ) )
            {
                var r = await fH.TransformAsync( 0 );
                r.Should().BeAssignableTo<ArgumentException>();
                ((ArgumentException)r).Message.Should().Be( "String expected, got 'int'." );

                logs.Should().Contain( "Transform 'Root:Transforms:0:Transforms:0' error while processing:" );
            }

        }


    }
}


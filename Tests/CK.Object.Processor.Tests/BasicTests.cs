using CK.Core;
using CK.Object.Predicate;
using CK.Object.Transform;
using FluentAssertions;
using Microsoft.VisualBasic;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.Object.Processor.Tests
{
    [TestFixture]
    public class BasicTests
    {
        [Test]
        public async Task basic_test_Async()
        {
            var config = ImmutableConfigurationSection.CreateFromJson( "Root",
                """
                {
                    "Assemblies": { "CK.Object.Processor.Tests": "Test"},
                    "Processors": [
                        {
                            "Type": "ToUpperCase, Test",
                        },
                        {
                            "Type": "NegateDouble, Test",
                        },
                        {
                            "Type": "AddRandomFriendToUser, Test",
                        }
                    ]
                }
                """ );
            var builder = new PolymorphicConfigurationTypeBuilder();
            ObjectProcessorConfiguration.AddResolver( builder );
            ObjectAsyncProcessorConfiguration.AddResolver( builder );

            var services = new SimpleServiceContainer();
            services.Add( new UserService() );

            // Sync
            {
                var fC = builder.Create<ObjectProcessorConfiguration>( TestHelper.Monitor, config );
                Throw.DebugAssert( fC != null );
                // Function
                {
                    var f = fC.CreateProcessor( TestHelper.Monitor, services );
                    Throw.DebugAssert( f != null );
                    f( 3712 ).Should().BeNull();
                    f( this ).Should().BeNull();
                    f( 3712.5 ).Should().Be( -3712.5 );
                    f( "Hello!" ).Should().Be( "HELLO!" );

                    var u = new UserRecord( "Zoe", 44, new List<UserRecord>() );
                    var uO = f( u );
                    uO.Should().BeSameAs( u );
                    u = (UserRecord)uO!;
                    u.Friends.Should().HaveCount( 1 );
                }
                // Hook
                var context = new MonitoredProcessorHookContext( TestHelper.Monitor );
                {
                    var fH = fC.CreateHook( TestHelper.Monitor, context, services );
                    Throw.DebugAssert( fH != null );
                    fH.Process( 3712 ).Should().BeNull();
                    fH.Process( this ).Should().BeNull();
                    fH.Process( 3712.5 ).Should().Be( -3712.5 );
                    fH.Process( "Hello!" ).Should().Be( "HELLO!" );

                    var u = new UserRecord( "Zoe", 44, new List<UserRecord>() );
                    var uO = fH.Process( u );
                    uO.Should().BeSameAs( u );
                    u = (UserRecord)uO!;
                    u.Friends.Should().HaveCount( 1 );
                }
            }
            // Async
            {
                var fC = builder.Create<ObjectAsyncProcessorConfiguration>( TestHelper.Monitor, config );
                Throw.DebugAssert( fC != null );
                // Function
                {
                    var f = fC.CreateProcessor( TestHelper.Monitor, services );
                    Throw.DebugAssert( f != null );
                    (await f( 3712 )).Should().BeNull();
                    (await f( this )).Should().BeNull();
                    (await f( 3712.5 )).Should().Be( -3712.5 );
                    (await f( "Hello!" )).Should().Be( "HELLO!" );

                    var u = new UserRecord( "Zoe", 44, new List<UserRecord>() );
                    var uO = await f( u );
                    uO.Should().BeSameAs( u );
                    u = (UserRecord)uO!;
                    u.Friends.Should().HaveCount( 1 );
                }
                // Hook
                var context = new MonitoredProcessorHookContext( TestHelper.Monitor );
                {
                    var fH = fC.CreateHook( TestHelper.Monitor, context, services );
                    Throw.DebugAssert( fH != null );
                    (await fH.ProcessAsync( 3712 )).Should().BeNull();
                    (await fH.ProcessAsync( this )).Should().BeNull();
                    (await fH.ProcessAsync( 3712.5 )).Should().Be( -3712.5 );
                    (await fH.ProcessAsync( "Hello!" )).Should().Be( "HELLO!" );

                    var u = new UserRecord( "Zoe", 44, new List<UserRecord>() );
                    var uO = await fH.ProcessAsync( u );
                    uO.Should().BeSameAs( u );
                    u = (UserRecord)uO!;
                    u.Friends.Should().HaveCount( 1 );
                }
            }
        }

        [Test]
        public async Task basic_with_conditions_and_final_transform_Async()
        {
            var config = ImmutableConfigurationSection.CreateFromJson( "Root",
                """
                {
                    "Assemblies": { "CK.Object.Processor.Tests": "Test"},
                    "Processors": [
                        {
                            "Condition":
                            {
                                "Type": "IsStringLongerThan, Test",
                                "Length": 10
                            },
                            "Type": "ToUpperCase, Test",
                        },
                        {
                            "Condition":
                            {
                                "Type": "IsStringShorterThan, Test",
                                "Length": "6"
                            },
                            "Type": "ToUpperCase, Test",
                        },
                        {
                            "Type": "NegateDouble, Test",
                        }
                    ],
                    "Transform":
                    {
                        "Type": "ToString, Test"
                    }
                }
                """ );
            var builder = new PolymorphicConfigurationTypeBuilder();
            ObjectProcessorConfiguration.AddResolver( builder );

            // Sync
            {
                var fC = builder.Create<ObjectProcessorConfiguration>( TestHelper.Monitor, config );
                Throw.DebugAssert( fC != null );
                // Function
                {
                    var f = fC.CreateProcessor( TestHelper.Monitor );
                    Throw.DebugAssert( f != null );
                    f( 3712 ).Should().BeNull();
                    f( this ).Should().BeNull();
                    f( 3712.5 ).Should().Be( "-3712.5" );

                    f( "Hello!" ).Should().BeNull( "Not processed (length is 6)." );
                    f( "Hello world!" ).Should().Be( "HELLO WORLD!" );
                    f( "Hell!" ).Should().Be( "HELL!" );
                }

                // Hook
                var context = new MonitoredProcessorHookContext( TestHelper.Monitor );
                {
                    var fH = fC.CreateHook( TestHelper.Monitor, context );
                    Throw.DebugAssert( fH != null );
                    fH.Process( 3712 ).Should().BeNull();
                    fH.Process( this ).Should().BeNull();
                    fH.Process( 3712.5 ).Should().Be( "-3712.5" );
                    fH.Process( "Hello!" ).Should().BeNull( "Not processed (length is 6)." );
                    fH.Process( "Hello world!" ).Should().Be( "HELLO WORLD!" );
                    fH.Process( "Hell!" ).Should().Be( "HELL!" );
                }

            }
            // Async
            {
                var fC = builder.Create<ObjectAsyncProcessorConfiguration>( TestHelper.Monitor, config );
                Throw.DebugAssert( fC != null );
                // Function
                {
                    var f = fC.CreateProcessor( TestHelper.Monitor );
                    Throw.DebugAssert( f != null );
                    (await f( 3712 )).Should().BeNull();
                    (await f( this )).Should().BeNull();
                    (await f( 3712.5 )).Should().Be( "-3712.5" );
                    (await f( "Hello!" )).Should().BeNull( "Not processed (length is 6)." );
                    (await f( "Hello world!" )).Should().Be( "HELLO WORLD!" );
                    (await f( "Hell!" )).Should().Be( "HELL!" );
                }

                // Hook
                var context = new MonitoredProcessorHookContext( TestHelper.Monitor );
                {
                    var fH = fC.CreateHook( TestHelper.Monitor, context );
                    Throw.DebugAssert( fH != null );
                    (await fH.ProcessAsync( 3712 )).Should().BeNull();
                    (await fH.ProcessAsync( this )).Should().BeNull();
                    (await fH.ProcessAsync( 3712.5 )).Should().Be( "-3712.5" );
                    (await fH.ProcessAsync( "Hello!" )).Should().BeNull( "Not processed (length is 6)." );
                    (await fH.ProcessAsync( "Hello world!" )).Should().Be( "HELLO WORLD!" );
                    (await fH.ProcessAsync( "Hell!" )).Should().Be( "HELL!" );
                }

            }
        }
    }
}

using CK.Core;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.Object.Predicate.Tests
{


    [TestFixture]
    public class PredicateTests
    {
        [Test]
        public void empty_configuration_fails()
        {
            var config = new MutableConfigurationSection( "Root" );

            var builder = new PolymorphicConfigurationTypeBuilder();
            ObjectPredicateConfiguration.AddResolver( builder );
            using( TestHelper.Monitor.CollectTexts( out var logs ) )
            {
                var fC = builder.Create<ObjectPredicateConfiguration>( TestHelper.Monitor, config );
                fC.Should().BeNull();
                logs.Should().Contain( "Configuration 'Root' must have children to be considered a default 'GroupPredicateConfiguration'." );
            }
        }

        [TestCase( true )]
        [TestCase( false )]
        public async Task type_can_be_true_or_false_Async( bool always )
        {
            var config = new MutableConfigurationSection( "Root" );
            config["Condition"] = always.ToString();
            var builder = new PolymorphicConfigurationTypeBuilder();
            ObjectPredicateConfiguration.AddResolver( builder );
            ObjectAsyncPredicateConfiguration.AddResolver( builder );

            {
                var fC = builder.Create<ObjectPredicateConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "Condition" ) );
                Throw.DebugAssert( fC != null );
                var f = fC.CreatePredicate( TestHelper.Monitor );
                Throw.DebugAssert( f != null );
                f( this ).Should().Be( always );
            }
            {
                var fC = builder.Create<ObjectAsyncPredicateConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "Condition" ) );
                Throw.DebugAssert( fC != null );
                var f = fC.CreatePredicate( TestHelper.Monitor );
                Throw.DebugAssert( f != null );
                (await f( this )).Should().Be( always );
            }
        }

        [TestCase( "All" )]
        [TestCase( "Any" )]
        public void type_can_be_All_or_Any( string t )
        {
            var config = new MutableConfigurationSection( "Root" );
            config["Condition"] = t;
            var builder = new PolymorphicConfigurationTypeBuilder();
            ObjectPredicateConfiguration.AddResolver( builder );
            ObjectAsyncPredicateConfiguration.AddResolver( builder );

            {
                var fC = builder.Create<ObjectPredicateConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "Condition" ) );
                Throw.DebugAssert( fC != null );

                fC.Should().BeAssignableTo<GroupPredicateConfiguration>();
                ((GroupPredicateConfiguration)fC).Predicates.Should().BeEmpty();
                ((GroupPredicateConfiguration)fC).All.Should().Be( t == "All" );
                ((GroupPredicateConfiguration)fC).Any.Should().Be( t == "Any" );
                ((GroupPredicateConfiguration)fC).AtLeast.Should().Be( t == "All" ? 0 : 1 );
            }
            {
                var fC = builder.Create<ObjectAsyncPredicateConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "Condition" ) );
                Throw.DebugAssert( fC != null );

                fC.Should().BeAssignableTo<GroupAsyncPredicateConfiguration>();
                ((GroupAsyncPredicateConfiguration)fC).Predicates.Should().BeEmpty();
                ((GroupAsyncPredicateConfiguration)fC).All.Should().Be( t == "All" );
                ((GroupAsyncPredicateConfiguration)fC).Any.Should().Be( t == "Any" );
                ((GroupAsyncPredicateConfiguration)fC).AtLeast.Should().Be( t == "All" ? 0 : 1 );
            }
        }

        [Test]
        public async Task default_group_is_All_Async()
        {
            var config = new MutableConfigurationSection( "Root" );
            config["Conditions:0:Type"] = "true";
            var builder = new PolymorphicConfigurationTypeBuilder();
            // Relaces default "Predicates" by "Conditions".
            ObjectPredicateConfiguration.AddResolver( builder, compositeItemsFieldName: "Conditions" );
            ObjectAsyncPredicateConfiguration.AddResolver( builder, compositeItemsFieldName: "Conditions" );

            {
                var fC = builder.Create<ObjectPredicateConfiguration>( TestHelper.Monitor, config );
                Throw.DebugAssert( fC != null );
                fC.Should().BeAssignableTo<GroupPredicateConfiguration>();
                ((GroupPredicateConfiguration)fC).Predicates.Should().HaveCount( 1 );
                ((GroupPredicateConfiguration)fC).All.Should().BeTrue();
                ((GroupPredicateConfiguration)fC).Any.Should().BeFalse();
                ((GroupPredicateConfiguration)fC).AtLeast.Should().Be( 0 );
                ((GroupPredicateConfiguration)fC).Predicates[0].Should().BeAssignableTo<AlwaysTruePredicateConfiguration>();

                var f = fC.CreatePredicate( TestHelper.Monitor );
                Throw.DebugAssert( f != null );
                f( this ).Should().BeTrue();
            }
            {
                var fC = builder.Create<ObjectAsyncPredicateConfiguration>( TestHelper.Monitor, config );
                Throw.DebugAssert( fC != null );
                fC.Should().BeAssignableTo<GroupAsyncPredicateConfiguration>();
                ((GroupAsyncPredicateConfiguration)fC).Predicates.Should().HaveCount( 1 );
                ((GroupAsyncPredicateConfiguration)fC).All.Should().BeTrue();
                ((GroupAsyncPredicateConfiguration)fC).Any.Should().BeFalse();
                ((GroupAsyncPredicateConfiguration)fC).AtLeast.Should().Be( 0 );
                ((GroupAsyncPredicateConfiguration)fC).Predicates[0].Should().BeAssignableTo<AlwaysTrueAsyncPredicateConfiguration>();

                var f = fC.CreatePredicate( TestHelper.Monitor );
                Throw.DebugAssert( f != null );
                (await f( this )).Should().BeTrue();
            }
        }

        static MutableConfigurationSection GetComplexConfiguration()
        {
            var complexJson = new MutableConfigurationSection( "Root" );
            complexJson.AddJson( """
                {
                    // No type resolved to "All" ("And" connector).
                    "Predicates": [
                        {
                            "Predicates": [
                                {
                                    // This is the same as below.
                                    "Type": true,
                                },
                                {
                                    // An intrinsic AlwaysTrue object predicate.
                                    // A "AlwaysFalse" is also available.
                                    "Type": "AlwaysTrue",
                                },
                                {
                                    // This (stupid) predicate is implemented in this assembly.
                                    "Assemblies": {"CK.Object.Predicate.Tests": "Tests"},
                                    "Type": "EnumerableMaxCount, Tests",
                                    "MaxCount": 5
                                }
                            ]
                        },
                        {
                            // "Any" is the "Or" connector.
                            "Type": "Any",
                            "Assemblies": {"CK.Object.Predicate.Tests": "P"},
                            "Predicates": [
                                {
                                    "Type": "StringContains, P",
                                    "Content": "A"
                                },
                                {
                                    "Type": "StringContains, P",
                                    "Content": "B"
                                },
                            ]
                        },
                        {
                            // The "Group" with "AtLeast" enables a "n among m" condition.
                            "Type": "Group",
                            "AtLeast": 2,
                            "Assemblies": {"CK.Object.Predicate.Tests": "P"},
                            "Predicates": [
                                {
                                    "Type": "StringContains, P",
                                    "Content": "x"
                                },
                                {
                                    "Type": "StringContains, P",
                                    "Content": "y"
                                },
                                {
                                    "Type": "StringContains, P",
                                    "Content": "z"
                                },
                            ]
                        },
                    ]
                }
                """ );
            return complexJson;
        }

        [Test]
        public void complex_configuration_tree()
        {
            MutableConfigurationSection config = GetComplexConfiguration();
            var builder = new PolymorphicConfigurationTypeBuilder();
            ObjectPredicateConfiguration.AddResolver( builder );

            var fC = builder.Create<ObjectPredicateConfiguration>( TestHelper.Monitor, config );
            Throw.DebugAssert( fC != null );

            var f = fC.CreatePredicate( TestHelper.Monitor );
            Throw.DebugAssert( f != null );
            f( 0 ).Should().Be( false );
            f( "Ax" ).Should().Be( false );
            f( "Axy" ).Should().Be( true );
            f( "Bzy" ).Should().Be( true );
            f( "Bzy but too long" ).Should().Be( false );
        }

        [Test]
        public async Task complex_configuration_tree_Async()
        {
            MutableConfigurationSection config = GetComplexConfiguration();
            var builder = new PolymorphicConfigurationTypeBuilder();
            ObjectAsyncPredicateConfiguration.AddResolver( builder );

            var fC = builder.Create<ObjectAsyncPredicateConfiguration>( TestHelper.Monitor, config );
            Throw.DebugAssert( fC != null );

            var f = fC.CreatePredicate( TestHelper.Monitor );
            Throw.DebugAssert( f != null );
            (await f( 0 )).Should().Be( false );
            (await f( "Ax" )).Should().Be( false );
            (await f( "Axy" )).Should().Be( true );
            (await f( "Bzy" )).Should().Be( true );
            (await f( "Bzy but too long" )).Should().Be( false );
        }

        [Test]
        public void complex_configuration_tree_with_EvaluationHook()
        {
            MutableConfigurationSection config = GetComplexConfiguration();
            var builder = new PolymorphicConfigurationTypeBuilder();
            ObjectPredicateConfiguration.AddResolver( builder );

            var fC = builder.Create<ObjectPredicateConfiguration>( TestHelper.Monitor, config );
            Throw.DebugAssert( fC != null );

            var hook = new MonitoredPredicateHookContext( TestHelper.Monitor );

            var f = fC.CreateHook( TestHelper.Monitor, hook );
            Throw.DebugAssert( f != null );
            f.Evaluate( 0 ).Should().Be( false );
            f.Evaluate( "Ax" ).Should().Be( false );
            f.Evaluate( "Axy" ).Should().Be( true );
            f.Evaluate( "Bzy" ).Should().Be( true );
            f.Evaluate( "Bzy but too long" ).Should().Be( false );
        }

        [Test]
        public async Task complex_configuration_tree_with_EvaluationHook_Async()
        {
            MutableConfigurationSection config = GetComplexConfiguration();
            var builder = new PolymorphicConfigurationTypeBuilder();
            ObjectAsyncPredicateConfiguration.AddResolver( builder );

            var fC = builder.Create<ObjectAsyncPredicateConfiguration>( TestHelper.Monitor, config );
            Throw.DebugAssert( fC != null );

            var hook = new MonitoredPredicateHookContext( TestHelper.Monitor );

            var f = fC.CreateHook( TestHelper.Monitor, hook );
            Throw.DebugAssert( f != null );
            (await f.EvaluateAsync( 0 )).Should().Be( false );
            (await f.EvaluateAsync( "Ax" )).Should().Be( false );
            (await f.EvaluateAsync( "Axy" )).Should().Be( true );
            (await f.EvaluateAsync( "Bzy" )).Should().Be( true );
            (await f.EvaluateAsync( "Bzy but too long" )).Should().Be( false );
        }
    }
}
